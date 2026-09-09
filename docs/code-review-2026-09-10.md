# コードレビュー・最適化の検証結果 (2026-09-10)

ソリューション全5プロジェクト、手書きC#ソース344ファイル（約32,600行）を対象に、不具合・アロケーション・不要コード・テストカバレッジを調査した。コメントアウトされたコードは1行も削っていない。`Obsolete/` 以下と `/* */` で囲まれた旧実装（`CryptoKey.cs` の後半約450行、`MaxHelper.cs` 全体、`LpDogmaMachine` の各 `Process*` など）はそのまま残した。

前回 (2026-09-06) のレビューで修正済みの箇所は再確認のみ行い、今回は未着手だった領域を中心に見た。

## 修正した不具合

- `Merger.GetOrCreateCredit` が新規作成時も `Created` に `false` を返していた。`LpDogmaMachine.Process` の「Credit created: Lp」は一度も出力されない状態だった。`AcquisitionMode.CreateOnly` で作成の成否を判定し、競合時のみ再取得するよう修正。
- `AuthorityControl.GetAuthority` が、有効期限切れの Authority に対してパスワード再入力を促さず `null` を返していた。期限切れで `vault` を捨てた際に `result` が `Success` のまま残り、直後のループ先頭の判定で即 `return default` していた。`VaultResult.PasswordRequired` を設定して再入力へ進むよう修正。Authority が存在しない場合の挙動は従来どおり。
- `CredentialNodes.CheckAuthorization(Credit)` が、merger を1つも持たない Credit（`Credit.Default` や逆シリアル化直後の Credit）に対して `true` を返していた。`foreach` が空回りするため認可が素通りする。0件を明示的に拒否するよう修正。
- `Vault.TryGet<T>` が、デシリアライズに失敗しても `result` に `VaultResult.Success` を残していた。戻り値だけ `false` になるため、`result` を見る呼び出し元が誤判定する。`DeserializationFailure` を設定するよう修正。
- `DomainControl.TryRemoveDomain(ulong)` がキャッシュ配列 `domainDataArray` を無効化していなかった。`Prepare()` はこのオーバーロードを直接呼ぶため、削除済みドメインが `DomainDataArray` に残り続ける。削除成功時に無効化するよう修正（名前指定オーバーロード側の重複した無効化はそのまま残した）。
- `EvolLinkage.Integrality` が `Integrality<LinkLinkage.GoshujinClass, LinkLinkage>` として宣言されていた（`LinkLinkage` からのコピー漏れ）。両方の型が存在するためコンパイルは通るが、`EvolLinkage.GoshujinClass` には使えない。`EvolLinkage` 型に修正。現時点で利用箇所はない。
- `Authority.GetHashCode()` が seed 長を確認せず `BitConverter.ToInt32` を呼んでいた。逆シリアル化用の `internal Authority()` は seed が空のままなので、その状態でハッシュを取ると `ArgumentOutOfRangeException` になる。長さ判定を追加。
- `CreditIdentity.ToString` が開き波括弧を閉じていなかった。
- `Vault.OnBeforeSerialize` が、ロックを取り直す公開版 `Remove` を呼んでいた（生成されたシリアライザが既に `lockObject` を保持している）。`RemoveInternal` に変更。`Lock` は再入可能なので動作は変わらない。

## アロケーション・性能

計測できた改善は Vault の名前一覧取得の2つ。BenchmarkDotNet 0.15.8、.NET 10.0.12 x64、Release、InProcess、warmup 5回・測定15回。同じベンチマークを修正前後で実行した。

| 処理 | 修正前 | 修正後 | 割り当て前→後 |
| --- | ---: | ---: | ---: |
| `Vault.GetNames()`（16件） | 180.74 ns | 31.46 ns | 256 → 152 B |
| `Vault.GetNames(prefix)`（16件） | 311.03 ns | 276.38 ns | 528 → 200 B |

`GetNames()` は `Select().ToArray()` をやめ、`Count` で正確な長さの配列を1回だけ確保する。`GetNames(prefix)` は範囲内のノードを先に数えてから配列を1回だけ確保し、`List<string>` の伸長と最後のコピーを除去した。`AuthorityControl.GetNames()` と `ListBatch` は、返ってきた配列をその場で書き換えて接頭辞を落とすようにし、LINQ の反復子と2本目の配列を除去した。

`LpService.GetSeedKeyFromCode` の "Merger" / "RelayMerger" / "Linker" の比較を `InvariantCultureIgnoreCase` から `OrdinalIgnoreCase` に変更した。ASCII 固定の比較対象なので結果は変わらず、照合テーブルを経由しなくなる。Vault と Seedphrase の辞書は大文字小文字・特殊文字の互換性のため `InvariantCultureIgnoreCase` のままにした。

`Proof.Equals` / `ContractableProof.Equals` / `OwnerToken.Equals` の `Signature.SequenceEqual(...)` を `Signature.AsSpan().SequenceEqual(...)` に変更した。**これは速度・割り当ての改善ではない**。.NET 10 の `Enumerable.SequenceEqual` は配列に対して内部で span 版へ委譲するため、計測すると修正前 2.12 ns / 0 B、修正後 2.09 ns / 0 B とほぼ同じだった。LINQ 経由であることを読み取りに頼らせない、という意図だけの変更。

`ToMergerString` に `mergers.Length > LpConstants.MaxMergers` のガードを追加した。従来は 3 を超える配列を渡すと `Span.Slice` で例外になる（`Credit` 側で 3 件までに制限されているので現状の呼び出し元では起きない）。ガード自体のコストは計測範囲でゼロ（48.66 ns → 48.64 ns）。なお stackalloc の確保量を `Credit.MaxStringLength` (265 chars) から必要最小の 159 chars へ減らす案も試したが、再現性のある形で 48.6 ns → 55.3 ns と遅くなったため採用していない。

## 削除した不要な実行コード

- `FullCredit.Contains(EvolProof)` — `ContainsAsync` を同期待ちするだけのメソッド。本番の呼び出し元は前回のレビューで `ContainsAsync` に移行済みで、残っていたのはテストのみ。ストレージ読み込み中にデッドロックしうるので削除し、テストを `ContainsAsync` に寄せた。
- `SeedKeyExtensions.TrySign(SeedKey, Linkage, long validMics)` の `validMics` — 本体で一切使われていなかった。Linkage の有効期限は evidence 側から決まるため、引数を削除して呼び出し元2箇所とテスト2箇所を更新した。
- `DomainControl.AddDomain` の未使用ローカル `domainData`。
- `Contract.Serialize` の未使用パターン変数 `proof`。
- `Evidence.ValidateAndVerify` の未使用ローカル `credit`（同じ `TryGetCredit` を直後の `ValidateAndVerifyExceptProof` でも呼んでいる）。

## テストとカバレッジ

テストは **70件 → 180件**。すべて成功する。新規に10ファイルを追加した。

| 追加したテスト | 対象 |
| --- | --- |
| `OrderedLineWriterTest` | リモートUIの行並べ替えバッファ（順序どおり／欠番待ち／重複／バッファ超過／500行シャッフル） |
| `VaultResultTest` | `VaultResult` の返り分け、名前一覧、子Vaultのパスワード、暗号化往復 |
| `AuthorityControlTest` | パスワード有無の取得、**期限切れ時の再入力**、入力キャンセル、未知の名前 |
| `AuthorityLifecycleTest` | **短いseedでのハッシュ**、Duration/Applicationの期限、鍵導出の決定性、往復 |
| `CreditStructureTest` | merger数の境界、`GetMergerIndex`／`GetMerger`、1〜3 mergerの文字列往復、短いバッファ、`CreditIdentity` |
| `ContractTest` | 空/Proof/Identifier の3状態、`StripProof` のハッシュ保存、`Partial`/`Total` のハッシュ寄与、往復 |
| `CredentialNodesTest` | **空Creditの非認可**、未知の merger、未署名evidenceの拒否 |
| `ProofEqualityTest` | 署名の内容比較（長さ違い・別鍵・改竄）、`OwnerToken` の等価性、credit を持たない evidence |
| `SeedKeySigningTest` | 署名の権限チェック、有効期間の境界、merger index の範囲、Linkage 生成の前提条件 |
| `StringHelperTest` | `AppendPrefix`、`ToMergerString`、`UnwrapQuote`、`CleanupInput`（256文字の分岐両側） |
| `DomainAssignmentTest` | ドメインハッシュの決定要因、検証、テキスト/バイナリ往復 |

期限切れ Authority と `Vault.TryGet<T>` のテストは、修正を一時的に戻すと実際に失敗することを確認した（180件中2件が失敗）。

| 行カバレッジ | 修正前 | 修正後 |
| --- | ---: | ---: |
| Lpの手書きコード（ファイル・行番号で重複排除） | 1,898 / 6,364 = 29.82% | 2,296 / 6,391 = 35.93% |

主な変更箇所の到達率は `Authority` 97.6%、`Contract` 98.9%、`StringHelper` 98.8%、`Credit` 89.1%、`OrderedLineWriter` 90.7%、`Vault` 87.4%、`Linkage` 88.0%、`AuthorityControl` 76.2%。

`OrderedLineWriter` は `internal` なので `Lp.csproj` に `InternalsVisibleTo("xUnitTest")` を追加した。

## 変更しなかったが報告する点

- **`CredentialProof` に `TryGetCredit` の override がなく、`CredentialNodes.TryAdd` が常に `false` を返す。** `Evidence.ValidateAndVerify` は proof から credit を取れることを前提に merger 署名を検証するが、`CredentialProof` は `ProofWithPublicKey` 派生なので基底の `TryGetCredit` （常に false）のままになっている。結果として net service の `AddCredentialEvidence` は必ず `InvalidData` を返す。`CredentialProof.UnderlyingCredit` というプロパティは定義されているものの、コード上どこからも読まれていない。`UnderlyingCredit` を返す override を入れれば繋がるが、これは認可経路の仕様を新たに決める変更になるため手を入れていない。`LpDogmaMachine.ProcessCredential` がコメントアウト中であることからも、この経路は実装途中と判断した。
- **`CredentialProof` のKey番号が `ProofWithSigner.ReservedKeyCount` を基準にしている。** 実際の基底は `ProofWithPublicKey`（`ReservedKeyCount` = 6）なので、キー6が空いたまま7から始まる。動作上の不具合はないが、直すと保存済みデータの形式が変わるため据え置いた。
- `Identifier.GetStringLength()` が実際の出力長より大きい値を返す（既定 Identifier で 48 に対し出力は1文字）。このため `Credit.GetStringLength()` も過大になる。`Credit.TryFormat` は逐次的に境界を見ているので安全だが、`CodeAndCredit.TryFormat` のように `GetStringLength()` を閾値に使う実装は、実際には足りるバッファを拒否しうる。Arc/Netsphere 側の実装なので本リポジトリでは変更していない。
- `Authority.GetSeedKey()` と `Authority.GetSeedKey(Credit.Default)` は同じキャッシュキー（`Credit.Default`）を共有するが、鍵の導出方法が異なる（前者は `SeedKey.NewSignature(seed)`、後者は seed と Credit の Blake3）。先に呼ばれた方の結果が残る。現状 merger 0件の Credit が `GetSeedKey(credit)` に渡る経路はないため実害はない。
- `LpConsole/Program.cs:97` の SA1120 警告（空コメント）はコメントなので残した。ビルドの警告はこの1件のみ。
- ネットワーク接続・切断、`RobustConnection`、`RemoteBenchControl`、`DomainControl`、各 Machine、外部ストレージを伴う経路は依然としてテストがない。`DomainControl` はテスト用フィクスチャで CrystalControl が Prepare されていないため解決自体ができず、副作用（データディレクトリへの書き込み）なしにテストする方法がなかった。

## 再実行

```bash
dotnet build Lp.slnx -c Release
```

```bash
dotnet test xUnitTest/xUnitTest.csproj -c Release --no-build
```

```powershell
./scripts/Measure-Coverage.ps1
```

```bash
dotnet run --project Benchmark/Benchmark.csproj -c Release --no-build -- --filter '*LpAllocationBenchmark*' --job short --inProcess --warmupCount 5 --iterationCount 15
```

計測結果は `artifacts/coverage.cobertura.xml`、`artifacts/coverage-source.csv`、`BenchmarkDotNet.Artifacts/results/Benchmark.LpAllocationBenchmark-report-github.md` に出力される。いずれも Git 管理外。
