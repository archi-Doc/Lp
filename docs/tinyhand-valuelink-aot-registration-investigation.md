# Tinyhand／ValueLink 更新後の静的登録エラー調査

調査日: 2026-09-05  
対象: Lp。ソースジェネレーター実装者向けの原因分析・修正提案。

## 1. 結論

直接原因は **Tinyhand の `StaticRegistrationGenerator` が、実装を持たない `[TinyhandObject(External = true)]` 型にも `GeneratedResolver.RegisterObject<T>()` を生成すること**。

Lp では `MergedEvidence.GoshujinClass` が該当する。外側の `[ValueLinkObject]` がコメントアウトされ、ValueLink による所有クラスの実装生成がなくなっている一方、内側の `External = true` 属性が残っている。そのため登録メソッドの3つのジェネリック制約を満たせず、`CS0311` が3件発生する。

次の2点は分けて扱う必要がある。

- **アプリ側の状態:** 外部実装を供給する仕組みが無効なのに、外部実装型であることを示す属性が残っている。
- **ジェネレーター側の問題:** 属性の存在だけを実装の保証として扱い、未使用の型宣言からもコンパイル不能な静的登録を生成する。旧版ではこの宣言だけでビルドは失敗しなかった。

ValueLink を参照しない最小プロジェクトでも再現したため、今回の直接の修正対象は **Tinyhand の静的登録判定**。ValueLink の実装生成が有効なケースは現行版でビルドに成功した。

推奨方針は、**Tinyhand が実装を生成する型と、外部から実装を供給される型の登録責任を分けること**。正常な ValueLink 所有クラスについては、ValueLink 自身が生成する静的登録を維持する。

## 2. 確認した環境とエラー

| 項目 | 値 |
| --- | --- |
| SDK | .NET SDK 10.0.400 / MSBuild 18.9.6 |
| OS / 対象 | Windows x64 / `net10.0` |
| Lp の言語設定 | `LangVersion=Preview` |
| Tinyhand | 0.143.3 → **0.144.2** |
| ValueLink | 0.117.2 → **0.118.3** |
| Lp の基準コミット | `c1d4b8260ca7789eb73cfda256940fbbb7d3340e` + 調査開始時の未コミット変更 |

上記更新前後のバージョンは `Lp/Lp.csproj` の差分から確認した。他の依存パッケージも更新されていたため、原因の特定には後述の独立した最小再現を使用した。

通常のコンパイルで再現する。`PublishAot=true` を指定する必要はなく、NativeAOT のリンク・トリミング段階のエラーではない。

```powershell
dotnet build Lp/Lp.csproj --no-restore -v:minimal `
  -p:UseSharedCompilation=false `
  -p:EmitCompilerGeneratedFiles=true `
  -p:CompilerGeneratedFilesOutputPath=obj/investigation-generated
```

生成ファイル:

```text
Lp/obj/investigation-generated/TinyhandGenerator/
  Tinyhand.Generator.StaticRegistrationGenerator/Tinyhand.StaticRegistration.g.cs
```

192行目の問題の登録:

```csharp
global::Tinyhand.Resolvers.GeneratedResolver
    .RegisterObject<global::Lp.T3cs.MergedEvidence.GoshujinClass>();
```

`RegisterObject<T>()` は次の制約を要求するが、この型にはいずれも存在しない。

```csharp
where T : ITinyhandSerializable<T>,
          ITinyhandReconstructable<T>,
          ITinyhandCloneable<T>
```

同じ登録行に対して、各インターフェースへの暗黙の参照変換がないという `CS0311` が1件ずつ、計3件報告される。

## 3. Lp での発生経路

対象ソース: `Lp/T3cs/Base/Merge/MergedEvidence.cs` の7～16行付近。

```csharp
[TinyhandObject]
// [ValueLinkObject(Integrality = true)]
public partial class MergedEvidence : Evidence
{
    [TinyhandObject(External = true)]
    public partial class GoshujinClass
    {
    }
    // 以下省略
}
```

1. Tinyhand の実装生成側は `External = true` を認識し、この内側の型のシリアライズ実装生成をスキップする。これは外部実装を利用するという属性の意味に合っている。
2. 外側に有効な `[ValueLinkObject]` がないため、ValueLink は `MergedEvidence.GoshujinClass` のインターフェース・実装・所有クラス登録を生成しない。
3. Tinyhand の静的登録側は型宣言も収集する。シリアライズ呼び出しの有無に関係なく、内側の型が登録候補に入る。
4. 属性の存在を根拠として、実装のない型への `RegisterObject<T>()` が生成される。
5. 最終コンパイルで3つの制約違反が発生する。

実際に出力した生成コードでは、外側の `MergedEvidence` には Tinyhand の実装が存在したが、内側の型には存在しなかった。また `gen.ValueLinkLoader.cs` には `MergeableEvidence.GoshujinClass` など正常な所有クラスの登録があり、`MergedEvidence.GoshujinClass` の登録はなかった。`MergeableEvidence` と `MergedEvidence` は別の型である。

## 4. 最小再現と比較結果

再現用ファイルを [repro/tinyhand-external-registration](repro/tinyhand-external-registration/Repro.csproj) に同梱した。ソリューションには追加していない。

最小条件は以下のみ。継承、入れ子、ValueLink、実際のシリアライズ呼び出しは不要。

```csharp
using Tinyhand;

[TinyhandObject(External = true)]
public partial class Model { }
```

| ケース | Tinyhand | ValueLink | 実測結果 |
| --- | --- | --- | --- |
| 空クラス + `External = true` | 0.144.2 | 参照なし | `CS0311` × 3 |
| 同じコード | 0.143.3 | 参照なし | ビルド成功 |
| 空クラス + 通常の `[TinyhandObject]` | 0.144.2 | 参照なし | ビルド成功 |
| 空クラス、属性なし | 0.144.2 | 参照なし | ビルド成功 |
| 有効な `[ValueLinkObject]` + 外側の `[TinyhandObject]` + 内側の `External = true` | 0.144.2 | 0.118.3 | ビルド成功 |

リポジトリルートから実行する。各コマンドは条件に応じて復元も行うため、ケースを変更するときは `--no-restore` を付けない。

```powershell
$p = 'docs/repro/tinyhand-external-registration/Repro.csproj'
dotnet build $p -p:Case=Orphan
dotnet build $p -p:Case=Orphan -p:TinyhandVersion=0.143.3
dotnet build $p -p:Case=Normal
dotnet build $p -p:Case=Plain
dotnet build $p -p:Case=Owner
```

`Orphan` は現行版で失敗することが期待される再現ケース。成功したビルドは、外部実装の存在や実行時シリアライズの成功まで意味しない。特に旧版の空の `External` 型は「宣言だけならコンパイルできる」と確認したにとどまる。

さらに、Lp から出力した全ジェネレーターの生成コードを `obj` 内にコピーし、問題の登録1行だけを削除して、生成処理を停止した状態でそのコピーをコンパイルした。この実験は **0エラー、既存の `CS8602` 警告1件** で成功した。`Lp.csproj` やアプリのソースは変更していない。

これは問題の登録行が現在の Lp プロジェクトのコンパイルを阻害していることを確認する実験であり、修正版ジェネレーターの検証や、通常の全ソリューションビルド成功を意味しない。

## 5. ソースジェネレーター実装上の原因

ローカルにある Tinyhand / ValueLink リポジトリを読み取り、実装位置を確認した。

| 対象 | 調査した実装位置 | 内容 |
| --- | --- | --- |
| Tinyhand | `TinyhandGenerator/StaticRegistrationGenerator.cs:29` | `TypeDeclarationSyntax` を含む候補収集 |
| Tinyhand | 同ファイル `:439` | 属性または Serializable インターフェースの存在による登録判定 |
| Tinyhand | 同ファイル `:447` | `RegisterObject<T>()` の出力 |
| Tinyhand | `TinyhandGenerator/TinyhandObject.cs:756` | `External` を認識して設定処理から復帰 |
| Tinyhand | 同ファイル `:2043` | 外部実装型の生成をスキップ |
| Tinyhand | `Tinyhand/Resolvers/GeneratedResolver.Collections.cs:267` | 3つの自己型インターフェース制約 |
| ValueLink | `ValueLinkGenerator/StaticOwnerRegistration.cs:195` | ValueLink 属性と Tinyhand 属性による所有クラスの選定 |
| ValueLink | 同ファイル `:100` | 所有クラスの静的登録出力 |

静的登録側の条件は概略として次の形になっており、`External` を考慮していない。

```csharp
hasTinyhandObjectOrUnionAttribute ||
named.AllInterfaces.Any(i =>
    MetadataName(i) == "Tinyhand.ITinyhandSerializable`1")
```

この条件には今回の外部実装型の問題に加えて、Serializable だけで残り2つの制約を確認していない点、インターフェースの型引数が登録対象自身か確認していない点がある。後者2点はコードレビュー上の追加リスクであり、今回の Lp の3エラーは前者だけで説明できる。回帰テストに追加することを推奨する。

**ソースの版に関する注意:** 読み取ったローカル HEAD は Tinyhand `f84247652e4549da8a9228638331297215c49996`、ValueLink `d2b2af1fff61be300a57c6eb4a45bc25e382fde3`。Tinyhand には調査開始前から別ファイルの未コミット変更があった。一方、NuGet の nuspec が示すコミットはそれぞれ `cb3fb74fffa094fcb92b04f0107f04ed89551165` と `c6cbc3e94e8a8b844ce39217af593593a32b2a0f` で、ローカル Git のオブジェクトにはなかった。したがって上記行番号はローカル実装の位置を示すもので、配布 DLL とソースの完全一致を保証しない。原因の実測根拠は、指定 NuGet バージョンを用いたビルドと、その生成コードである。

## 6. 推奨修正

### 6.1 登録可能性と依存型探索を分離する

まず `External` の判定を名前付き引数から取得する。`External` 未指定は `false` とし、属性の完全修飾名またはシンボルを用いて判定する。

登録判断を、少なくとも次のように分ける。

| 型の種類 | Tinyhand 側の扱い |
| --- | --- |
| 自分の実装生成処理が対応する通常の Tinyhand 型 / Union | 既存の生成条件と整合させ、静的登録する |
| 必要な3インターフェースを自己型で実装済みの型 | 静的登録できる。手書き実装や参照アセンブリも対象 |
| 同一コンパイル内の ValueLink 生成所有クラス | ValueLink 自身の静的登録を利用する |
| 実装が確認できない `External` 型 | 属性だけを根拠とする `RegisterObject<T>()` は出力しない |

通常のオブジェクト／Union の対象判定は、単なる属性有無のコピーではなく、可能な範囲で実装生成側と共有する。`External` と Union など複数設定が組み合わさる場合も、実装を生成しないのに登録だけを生成する状態を作らない。

インターフェース実装を根拠に登録する場合には、`ITinyhandSerializable<T>`、`ITinyhandReconstructable<T>`、`ITinyhandCloneable<T>` の**全て**について、型引数が `named` と一致することを `SymbolEqualityComparer.Default` で確認する。`Derived : Base` が `ITinyhandSerializable<Base>` を継承していても、`RegisterObject<Derived>()` の根拠にはならない。

単純に `if (External) return;` を追加して `Process` 全体を終了する実装には注意が必要。現在は登録処理の同じ分岐で属性・メンバー型などの依存を収集している。登録を委譲した型でも必要な依存型探索は維持し、閉じたジェネリック型やコレクションの登録が欠落しないようにする。`AddImmutable` 等の付随する登録についても、実装の生成有無と整合させる。

### 6.2 生成後のインターフェースが見えるとは仮定しない

全型を一律に `AllInterfaces` の3制約チェックだけでフィルタすると、Tinyhand / ValueLink が同じコンパイルでこれから実装する正常な型まで除外し得る。

通常のソースジェネレーター出力は、別のジェネレーターの入力にはならない。実行順序で解決する設計にはできない。この制約は [Roslyn の Source Generators Cookbook](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md#proposal) にも明記されている。

Tinyhand は自分の生成予定を根拠に通常型を登録し、ValueLink は自分の生成予定を根拠に所有クラスを登録する設計が自然である。実測した正常な Owner ケースでは、すでに `gen.ValueLinkLoader.cs` に次の登録と ModuleInitializer からの呼び出しが存在した。

```csharp
global::Tinyhand.Resolvers.GeneratedResolver
    .RegisterObject<global::ExternalRegistrationRepro.Model.@GoshujinClass>();
```

この経路を残せば、Tinyhand 側で外部所有クラスの属性だけに基づく重複登録を抑制できる。ただし、この構成での実行時動作と NativeAOT は修正後のテストで確認する必要がある。

第三者ジェネレーターとの連携も同じ考え方にする。外部実装の供給元が登録も生成するか、入力側で共有できる明示的な契約を設ける。リフレクションによる実装探索へのフォールバックは、今回の AOT 対応の修正方針には適さない。

### 6.3 診断は誤検知を避ける

未使用の孤立した `External` 宣言には、不正な登録コードを出力しないことで旧版のコンパイル互換性を保てる。

一方、明示的な登録要求や実際に必要な型に実装・formatter がない場合は、元ソース位置で理解できる専用診断を検討する。ただし、ジェネレーター実行時の `AllInterfaces` だけでは「他のジェネレーターも実装しない」と確定できない。検証には生成コードを含む最終 Compilation を調べる analyzer、または外部実装・登録契約の検証が必要である。カスタム formatter という別の正当な供給経路も考慮する。

登録コードをスキップする変更で、明示的な `[assembly: TinyhandRegister(typeof(...))]` が黙って無効になることは避ける。明示 root の扱いと外部供給契約は、修正時に仕様として決めてテストする。

## 7. ジェネレーター側の回帰テスト案

以下は修正時の受け入れ条件。第4節の実測ケース以外は今回未実施。

| ケース | 確認内容 |
| --- | --- |
| 未使用の空の `External` 型、入れ子型 | 不正な `RegisterObject<T>()` がなく、最終コンパイルが成功 |
| 通常の Tinyhand 型、Union、struct | 必要な登録が引き続き生成される |
| 有効な ValueLink owner | Tinyhand と ValueLink を同時に動かして最終コンパイルが成功し、ValueLink 側の登録がある |
| カスタム所有クラス名、`Owner<int>.GoshujinClass` | 正しい閉じた型の登録があり、既定名の文字列判定に依存しない |
| `External` + 手書きの完全実装 / 参照 DLL の完全実装 | 自己型の3制約を満たす型は登録される |
| Serializable のみ、自己型が異なる継承 | 制約不一致の登録を生成しない |
| 外部型のメンバー・コレクション・Union 依存 | 登録委譲後も必要な依存登録が欠落しない |
| 外部型の明示 root / カスタム formatter | 採用した供給契約と診断仕様に従う |
| 通常実行 | resolver 経由の Serialize / Deserialize / Reconstruct / Clone が成功 |
| NativeAOT | 実行可能プロジェクトを PublishAot で発行し、所有クラスと閉じたジェネリック型の上記操作を実行 |
| Lp | 元ソースを維持した通常のビルドで今回の3エラーが消える |

生成文字列の検査だけでは、今回のようなジェネリック制約違反を見逃す。`RunGeneratorsAndUpdateCompilation` 等で生成後の Compilation のエラーも検査し、連携テストでは Tinyhand と ValueLink の双方を実行する。

## 8. アプリ側の暫定策と今回の変更範囲

使用していない所有クラスなら、残っている `External` 属性または空のクラス宣言を整理することで、当該誤登録を避けられる。ValueLink の所有クラスとして使用する意図なら、外側の `[ValueLinkObject]` と必要なリンク定義を復元して、実装が生成される状態に戻す。

`External = false` への機械的な変更は推奨しない。空の通常オブジェクトのシリアライザーを生成できても、本来の ValueLink コレクションの意味・データ形式にはならない。生成ファイルの直接編集も次回ビルドで失われるため、対策としては採用しない。

今回追加したのは本書と独立した再現用プロジェクトのみ。Lp のアプリソース、既存のパッケージ更新、Tinyhand / ValueLink リポジトリには修正を加えていない。

通常の Lp ビルドは現時点でも失敗する。修正ジェネレーターのビルド・全ソリューションの通過・実行時シリアライズ・NativeAOT 発行は未検証であり、上記受け入れテストとして残す。

調査環境では初回のソリューションビルドに出力 XML へのアクセス拒否 `CS0016` も混在した。これは `CS0311` とは別件で、生成コードの削除実験では出力を `obj` 内に分離し `UseSharedCompilation=false` を指定してコンパイルできた。また最小再現の復元ではローカルキャッシュと空の packageSources 設定を使用し、ユーザー NuGet 設定の読み取り制限を解消した実行で検証した。これらの環境上の失敗は、第4節の比較結果には含めていない。
