# コードレビュー・修正の検証結果 (2026-09-26)

ソリューション全5プロジェクト（Lp 本体 289 ファイル・約21,500行、テスト・ベンチマーク等を含む）を対象に、4つの監査を領域別（T3cs／Services・Misc／NetServices・Merger・Machines・LpUnit／SubCommands・アプリ・テスト）に並行して実施し、指摘はすべてコード・依存ライブラリのソース（`archi-Doc/*`）・NuGet の XML ドキュメント・再現テストで確認してから修正した。前回（09-06、09-10）までの修正・既知事項は対象外。前回レビュー後の依存ライブラリ更新と API リネーム（6411d4be）についても、意味が変わりうる箇所（`IsSuccess/IsNo`、`SetCompleted`、`WriteRaw`、`SharedSecretSize`、`MachineHandle.Terminate` など）を旧版と照合し、回帰がないことを確認した。

## 修正した不具合

### 署名・検証（セキュリティ）

- **`MergerProof`・`OrderProof`・`CryptoTransferProof`・`TransferProof2` が `Proof` の `TinyhandUnion` に未登録だった。** 署名は `SerializeObject<Proof>` で行うため、未登録型は内容に関係なく定数 `81 C0 C0`（3バイト）に署名していた。同じ鍵の別の MergerProof に署名をコピーすると検証が通る（偽造可能）ことを再現で確認。`Proof` として保存すると `null` に化ける問題も同じ原因。`ProofKey` の末尾に値を追加して登録した（既存番号は不変）。
- **`TrySign(Linkage)` と `Linkage.ValidateAndVerify` のシリアライズが食い違い、派生型（`LinkLinkage`・`AccountableLinkage` 等）のリンカー署名が常に検証失敗していた。** 署名側は基底型の静的シリアライザ（型IDも基底）、検証側は実行時型だった。署名側を検証側と同じ実行時型・同じレベルに揃えた。基底 `Linkage` の署名バイト列は従来と同一（旧方式で作った署名が検証でき、Ed25519 の決定性により新旧の署名が一致することをテストで確認）。
- **Evidence のマージャー署名検証が、マージャーを持たない Credit では署名ゼロで成功していた。** `TestLinkageProof`（常に Validate 成功）や `RequestMergeProof`（Credit 未検証）経由で到達する。`credit.Validate()` を必須にした。

### データ消失

- **起動途中の失敗で Vault からノード秘密鍵が消えていた。** `LpUnit.Save` は、鍵のロード前（`NodeSeedKey == null`）でも `AddObject(null)` を実行し、保存済みの鍵バイト列を捨てていた。`OnBeforeSerialize` が項目ごと削除するため、次回起動でノードIDが変わる。鍵が有効なときだけ格納するよう修正。
- **既存の Vault が新しい空の Vault で上書きされることがあった。** 読込の要否を「データディレクトリの有無（`IsFirstRun`）」で判定していたため、`VaultPath` で外部の Vault を指定して新しいデータディレクトリで起動すると、既存ファイルを読まずに新規作成し終了時に上書きしていた。また読み込みの失敗や、復号後のデシリアライズの失敗でも確認なしに新規作成へ進み、同様に上書きしていた。ファイルの存在で判定するようにした。読み込み自体の失敗（アクセス拒否・使用中）は破損を意味しないため、ファイルに触れず中断する。復号できたがデシリアライズできないファイルは `Vault.th.<UTC時刻>` へ退避してから新規作成する（退避できなければ中断）。新規作成のパスワード入力で終了要求された場合は、既存読込側と同じく中断する（従来は空パスワードの Vault を保存していた）。
- **`VaultControl.SaveAsync` が失敗を完全に握りつぶし、書き込みも非原子的だった。** 例外をログに出し、ディレクトリを作成し、一時ファイルへ `WriteThrough` で書いてから置換する（書き込み中断で唯一のコピーが壊れない）。置換は既存ファイルのメタデータを保つ（Windows は `File.Replace` で ACL・属性、Unix はパーミッションを引き継ぐ）。
- **`Vault.OnBeforeSerialize` が、再シリアライズで例外が出た項目を、保存済みの有効なデータがあっても削除していた。** 保存済みデータがある項目は残す。
- `VaultPass` が空文字列のとき、入力前に「パスワード不一致」を表示していた点も修正。

### 並行性・リソース

- **BigMachines のマシン4種（`DomainMachine` 等）を DI でシングルトン登録していた。** BigMachines はインスタンスごとに DI から生成し、同じインスタンスの二重アタッチで例外を投げる（例外文自体が transient 登録を要求）。2つ目のドメイン、終了したマシンの再生成で失敗する。transient に変更。
- **リモートUIで、キャンセル後に次のコマンドが来ると、前のコマンドの後処理が新しいコマンドのグループを消していた。** その後の Ctrl+Q は成功を返すが何もキャンセルしない。コマンドごとにグループを作り、自分のグループのときだけクリアする。
- `remotebench` の集計タスクがコマンドのトークンを使っており、コマンド終了（=トークンのキャンセル）で即終了して集計結果が一度も出なかった。コマンド内で集計を待つ。開始に失敗したランナーの記録を除去し、無駄な1分待ちをなくした。
- `RobustConnection` に Dispose がなく、`merger-admin` / `merger-client` の実行ごとに接続が残っていた。`IDisposable` を実装し、ネストセッション終了時に閉じる。
- `NodeControlMachine` が `GetActiveNodes()` で所有権を受け取ったプールのメモリを返却していなかった。
- `CredentialEvidence` の除去時に、自分が追加していないエイリアス（組み込みの `LpKey` 等を含む）まで削除していた。
- `DomainControl.DomainDataArray` のキャッシュは、追加と同時に読むと古い配列が残り得た。参照は2つのコマンドだけなのでキャッシュを廃止した。

### 機能不具合

- `Contract` の生成 Clone が `[Key]` を無視して空の Contract を返していた（Linkage 等を Clone すると証明が消える）。手書きの深いコピーを実装。
- `Vault.TryGetVault` が Vault 以外の項目にも復号を試み、パスワード不一致として返していた。`GetAuthority` のパスワード入力が抜けられないループになる。種別不一致を返す。
- `ReadPasswordAndConfirm` が不一致時に確認入力だけを再要求し、1回目の入力ミスだと成功しようがなかった（Vault 作成時は Esc も無効）。最初からやり直す。
- `LpDogmaMachine` が DomainKey のパスワード入力をキャンセルされると3秒ごとに再要求し、コンソール入力を奪い続けていた。Authority があるのに取得できない場合は停止する。
- `Value.TryParse` が所有者のエイリアスを拒否し（`ToString(Alias.Instance)` の結果を戻せない）、所有者の後ろの余分な文字を受理していた。`Credit.TryParse` と同じく消費文字数で判定。
- `remotedata` が 1MiB と宣言したストリームに約2MB（`1024 * 2024`）を送り、最初のアップロードが必ず `StreamLengthLimit` で失敗していた。
- `Merger.CreateCredit` がタイムアウト等も `AlreadyExists` と報告していた。
- `ModestLogger.NonConsecutive` が回復後もリセットされず、同じエラーの再発を記録しなかった。
- `OwnerFee` / `OrderFee` が `IEquatable<T>` を実装しながら `GetHashCode` を持たず、等しい `CreditColor`（record）のハッシュが一致しなかった。
- `add-relay` 系で中継側の拒否理由の代わりに常に `Success` を表示していた。`remove-vault` が「Authority が見つからない」と表示していた。ノード不足時のヒントが存在しないコマンド名（`add-net-node`）を案内していた。Debug レベルの色設定が無視されていた。

### 秘密情報

- `new-authority` が無効なシードフレーズ（1語違いなら残りから復元可能）をログファイルに書いていた。他のコマンドと同じくコンソールだけに出す。
- `MasterKey` の導出・解析で、マスターシードのコピーや導出鍵をスタックに残していた。作業領域をゼロクリアする。

## 品質・その他の改善

- CancellationToken の伝播（`restart-remote-container` の最大約40秒の待機、`export`、`add-relay`、`remotedata` 等）。`exit` 判定をカルチャ非依存に。
- 全ルール有効（`AnalysisMode=All`）のアナライザーで、Lp 本体の警告 598 → 577。CA2016（トークン未伝播）11 → 2、CA2251/CA1304/CA1310 解消、CA1067 9 → 7。新規警告なし。通常ビルドの警告は既存の SA1120 のみ。

## テスト

テストは **180件 → 209件**。すべて成功（Release、連続3回）。追加した回帰テストのうち不具合を対象とする17件は、修正を一時的に戻すと失敗することを確認した（残りは互換性・ガード目的で修正前後とも成功する設計）。保存時のメタデータ保持のテストは、単純な `File.Move` による置換では失敗することを確認した。

| 追加・変更 | 対象 |
| --- | --- |
| `ProofUnionTest` | ユニオン未登録の Proof 型の検出（リフレクション）、MergerProof の署名が内容に束縛されること、マージャーなし Credit の Evidence |
| `LinkageSignatureTest` | 派生 Linkage の署名検証、署名の型への束縛、基底 Linkage の署名の互換性、Clone 後の有効性 |
| `VaultControlTest` | 新しいデータディレクトリでの既存 Vault の読込、デシリアライズできない Vault の退避、読み込めない Vault を変更しないこと、新規作成時の中断、保存時のメタデータ保持 |
| `VaultResultTest` / `AuthorityControlTest` | 種別不一致、再シリアライズ失敗時の保持、誤パスワード後の再入力（既存テストは誤入力を一度も与えていなかった） |
| `UserInterfaceExtensionTest` | パスワード確認の不一致時のやり直し |
| `MachineRegistrationTest` | 終了したマシンの再生成 |
| `CredentialNodesTest` / `CreditTest` / `ContractTest` / `CreditColorTest` / `ModestLoggerTest` | エイリアス、Value の解析、Contract の Clone、等価性、ログ抑止のリセット |
| `FastClockFixture`（アセンブリフィクスチャ） | テストプロセスでは `Mics.FastCorrected` が更新されず、実行が5秒を超えると署名検証のテストが失敗し得た。アプリの NetSender と同様に定期更新する |

| 行カバレッジ | 修正前 | 修正後 |
| --- | ---: | ---: |
| Lp の手書きコード | 2,296 / 6,391 = 35.93% | 2,473 / 6,471 = 38.22% |

## 変更しなかったが報告する点

仕様判断・データ形式や通信契約の変更を伴うため、修正していない。

- **Linkage の片側を `StripProof` した場合の検証**：`ContractableEvidence.BaseProof` が相手側の証明にフォールバックするため、両側のマージャーが異なると検証が失敗する。逆に `IsPrimary` は署名対象外で、空・識別子の Contract では片側の署名をもう片側にコピーしても通り得る。
- **`CryptoKey.decrypted`** が通常のシリアライズに含まれ、受信データの値を復号結果として信用する（現状は未使用の経路）。`CertificateProof.Validate` は内包する `MergedProof` を検証しない。`TestLinkageProof`（常に検証成功）が本番のユニオンに登録されている。
- **遅延生成されるクリスタル**（Merger/RelayMerger/Linker の構成）では `RequiredForLoading` が効かず、壊れた構成ファイルは既定値で置き換えられ、次の保存で上書きされる。直すには `Initialize` を非同期化する API 変更が必要。
- **秘密情報の出力**：リモート実行したコマンド行（`-code` に秘密鍵を書ける）が送信側・受信側の Log.txt に残る、`export options` が VaultPass・MasterKey・導出した秘密鍵を平文で書く、`DomainAssignment.ToString()` が Code を含む、など。監査ログとしての意図もあり得るため方針の判断が必要。
- **リモートUIの契約**：キャンセルされた入力が空文字の成功として返り、失敗した `ReadYesNo` の RPC は既定値 `Success`（=Yes）になる。シングルトンのサービス（LpService・AuthorityControl）はリモートのコマンドでもサーバー側コンソールにパスワードを求める。
- `ReadYesNo`/`ReadPassword` の拡張メソッドや `AuthorityControl.GetAuthority` はトークンを受け取れず、Ctrl+Q 後も入力が残る。
- `EvolLinkage` が `LinkProof` へのキャストを残しており EvolProof では例外になる、`MergeableEvidence.Integrality` は更新を常に拒否する、`DomainData.DetermineRole` はロールを戻さない、`Merger.CreateCredit(CreateCreditParams)` は誰も持たない鍵でクレジットを返す（実装途中）。
- 起動時の例外（`LpUnit.Product.Run`）がログに残らない。終了時の保存中も BigMachine が動作している。
- DomainControl や多くのマシンはフィクスチャで CrystalControl が準備されないためテストできず、ネットワーク経路も未検証。共有フィクスチャの Vault に状態が蓄積する。
- 前回までの既知事項（`CredentialProof` の `TryGetCredit` 未実装 など）はそのまま。

## 再実行

```bash
dotnet build Lp.slnx -c Release
```

```bash
dotnet test --project xUnitTest/xUnitTest.csproj -c Release --no-build
```

```powershell
./scripts/Measure-Coverage.ps1
```
