# コードレビュー・最適化の検証結果

対象はソリューション全5プロジェクト。既存C#ソース335ファイルの構成、危険な処理、アロケーション、ビルド警告、テスト実行とカバレッジを調査した。コメントアウトされたコードは保持した。Roslynによるコメント抽出比較でも、既存の空でないコメントの欠落がないことを確認した。

## 修正した不具合

- `MasterKey` の派生鍵生成が共有seedを一時変更していたため、同時実行で結果が変わる。固定長のスタック上のコピーで計算するよう修正。
- `MasterKey.TryFormat` が短いバッファで例外を投げる問題と、パース時のデコード長の確認漏れを修正。
- `CryptoKey.TryParse` が `()x` で範囲外アクセスする問題、および後ろにCreditが続く正しいCryptoOwnershipを全体の長さで拒否する問題を修正。
- 署名の有効期間が0以下でも署名する問題、負のmergerインデックスで範囲外アクセスする問題を修正。
- 2つのmergerしかないときに3つ目を読む署名処理、および存在しないmerger番号を検証で受理する問題を修正。
- `MergedProof` の検証を、実際のmerger数とValueの妥当性に基づくよう修正。
- `Evidence` から内側のProofへ検証オプションを伝達。負の検証インデックスを拒否。
- プールから再利用する `ContractableEvidence` に `IsPrimary` が設定されず、別のCreditの鍵で検証する問題を修正。異なる2つのCreditによる署名、再利用、シリアライズ往復、改ざんをテスト。
- 空または少数のピアからランダム選択するとリスト終端を越える問題を修正。
- `LpService` が壊れたPointを0として受理する問題、空のCreditからmergerを取得すると例外になる問題を修正。
- `LpService.ConnectAndAuthenticate` が返却直前に接続をDisposeしていた問題を修正。成功時は返却先へ所有権を渡し、失敗・例外時には解放する。
- `RobustConnection` の認証失敗時の接続解放漏れを修正し、再接続の抑制判定をロック内へ移動。
- `FullCredit` に非同期検索を追加して呼び出し元を更新。未完了のValueTaskから直接Resultを読む処理を除去。既存の同期APIは保持。
- `Order.Equals` が内容を比較せず常にtrueを返す問題、初期状態のOrderの検証でnull参照になる問題を修正。
- `VisceralOperator` がFieldInfoを保存せず、フィールドの読み書きに失敗する問題を修正。
- `Value` と `Credit` の文字列出力で、実際には足りるバッファを過大な推定長のために拒否する問題を修正。
- リモートベンチマークでループ変数をTask.Runのクロージャから参照する問題を修正。未使用のTask配列も除去。
- Domainの鍵がない状態でのnull参照を防止。

## アロケーション・性能

SeedphraseはSpanによる分割、固定上限のスタック領域とArrayPool、FrozenDictionaryの高速検索、string.Createによる結果への直接書き込みを使用する。従来のInvariantCultureIgnoreCase検索をフォールバックとして残し、大文字小文字や特殊文字の互換性を保つ。高速検索用の辞書は初期化時に一度だけ作成する。

SHA3入力のUTF-8配列と単語ごとの文字列を除去した。固定入力の既知seedをテストし、元の文字列から鍵を導出する仕様を維持した。作業領域は利用後にクリアする。

署名・ハッシュ検証ではwriterが所有するSpanを直接使用し、返却を忘れる可能性のある一時的なRentMemoryへの所有権移動を除去した。認証済み接続への再認証では不要な署名を作らない。リモートのストリーム用100,000バイト配列はプールで再利用する。

CleanupInputは短い入力でスタック確保を256文字以下に制限し、大きい入力は入力長に比例するスタック確保を行わず、必要な結果文字列だけを作る。変更不要なら元のstringを返す。

BenchmarkDotNet 0.15.8、.NET 10.0.11 x64、Release、InProcess、warmup 3回・測定5回。同一のベンチマークを修正前後で実行した。値はこの端末での局所的な測定であり、ネットワーク全体のスループットではない。

| 処理 | 修正前 | 修正後 | 割り当て前→後 |
| --- | ---: | ---: | ---: |
| シードフレーズ解析 | 2,084.82 ns | 600.48 ns | 1,432 → 56 B |
| シードフレーズ生成 | 139.98 ns | 67.35 ns | 1,232 → 360 B |
| Authorityキャッシュ参照 | 3.70 ns | 3.95 ns | 0 → 0 B |
| 変更不要な入力の整理 | 11.17 ns | 11.46 ns | 0 → 0 B |
| 制御文字を含む入力の整理 | 15.18 ns | 13.94 ns | 32 → 32 B |

Authorityのデリゲート確保はコード上で除去した。階層化コンパイルを無効にした別の簡易測定では64→0 Bだったが、上表の十分にウォームアップされたBenchmarkDotNet測定では修正前もJITにより0 Bになる。すべての処理が高速になったという結果ではない。

## テストとカバレッジ

既存11件は10成功・1失敗。失敗の原因は、現在のパーサーで読めない古いノード文字列を検証せず利用するテストデータだった。ノードを直接構築して検証するよう修正した。

最終結果は **70件すべて成功**。独立した修正前コピーでも回帰テストの一部60件を実行し、33件の失敗を確認した。大きい入力によるスタックオーバーフロー検査と新規非同期APIのテストはその比較から除外した。

| 行カバレッジ | 修正前 | 修正後 |
| --- | ---: | ---: |
| Lpの手書きコード（ファイル・行番号で重複排除） | 1,203 / 6,277 = 19.17% | 1,898 / 6,364 = 29.82% |

主な変更箇所ではMasterKey 98.18%、Seedphrase 94.38%、Order 84.38%、FullCredit 76.19%。ソリューションのReleaseビルド成功。通常ビルドでは既存のLpConsole/Program.csの空コメントに対するSA1120警告が残る。

ネットワーク接続・切断、ストリーム失敗時の解放、Domain/Mergerの状態機械、外部ストレージを伴う処理にはまだ未実行の分岐がある。全体カバレッジ100%や実運用負荷での検証は完了していない。実装途中のサービスの仕様を新たに決める変更は行っていない。

## 再実行

```powershell
dotnet test --project xUnitTest/xUnitTest.csproj -c Release --no-restore
dotnet build Lp.slnx -c Release --no-restore
./scripts/Measure-Coverage.ps1
dotnet run --project Benchmark/Benchmark.csproj -c Release --no-restore -- --filter '*LpAllocationBenchmark*' --job short --inProcess --warmupCount 3 --iterationCount 5
```

依存関係は事前にrestoreする。カバレッジスクリプトはキャッシュ済みのdotnet-coverageを利用し、必要なら `-CoverageTool` にdotnet-coverage.dllのパスを指定できる。

実測レポートは `artifacts/coverage.cobertura.xml`、`artifacts/coverage-source.csv`、`BenchmarkDotNet.Artifacts/results/Benchmark.LpAllocationBenchmark-report-github.md` に出力する。これらの一時生成物はGit管理対象外。比較用のコピーも `artifacts/baseline` 内だけに置いた。
