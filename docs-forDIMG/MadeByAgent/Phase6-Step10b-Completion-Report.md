# Phase6-Step10b 完了報告書: 1 ファイル 1 型（原則 1）の全数是正

完了日: 2026-10-15  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase6-Step10b-Plan.md`（台帳は §0A、進捗は §0A.5）  
分割ツール: `docs-forDIMG/MadeByAgent/tools/Step10b/`（`README.md` に手順）

---

## 1. 結果の要約

- 複数のトップレベル型を持つファイル **55 件を、すべて 1 ファイル 1 型へ分割**した（新設ファイル 196）。
- 単一型でファイル名が型名と異なるファイル 4 件を改名した（原則 2）。
- 分割前後で、型名の一覧が一致した（618＝618、差分 0）。宣言（属性・コメント・`#if` を含む前後のトリビア）は、一字一句そのまま移した。
- 各ウェーブで、ビルド・テストビルド・テスト実行がすべて成功し、起動スモークも問題なし（W3・W4・W5 の後に実施）。
- `.github/copilot-instructions.md` §3.3 原則 1 の過渡期ルールを終了し、既存ファイルを含む厳格適用へ更新した。

## 2. ウェーブ別の実績

- **W1（データ型・補助型）**: 20 ファイル → 新設 96。`ProfileDTO`（20 型）、`ProfilePropGroups`（26 型）、`ControlServiceDeviceOptions`（11 型）、`AppSettingsDTO`（9 型）、`vJoyFeeder`（8 型）など。テストの 3 ファイル（`NullProcessInspector` の同居の是正を含む）。
- **W2（DI 層）**: 5 ファイル → 新設 5。`AppHost` の互換ラッパーを `DI/Compat/AppHost.cs` へ分離（名前空間は変更なし）。
- **W3（UI 層）**: 19 ファイル → 新設 32。`MainWindow`、`ProfileEditor` ほか、ViewModel 12 ファイル。
- **W4（デバイス・コア層）**: 10 ファイル → 新設 35。`Log.cs`（→ `AppLogger.cs` ほか）、`DS4Sixaxis.cs`（→ `DS4SixAxis.cs` ほか）、`UdpServer`、`DS4Device`、`DS4Devices` など。
- **W5（`ScpUtil.cs`）**: 29 型 → 新設 29（元ファイルを削除。`Global.cs` 約 3,470 行、`BackingStore.cs` ほか）。
- **改名（原則 2）**: `GitHubRelease.cs`→`GithubRelease.cs`、`EventHandlers.cs`→`PropertyChangingHandler.cs`、`Crc32.cs`→`Crc32Algorithm.cs`、`Specialactionslistviewmodelsorttests.cs`→`SpecialActionsListViewModelSortTests.cs`。

## 3. 分割前後の型インベントリ

- 分割前（2026-10-15 の走査）: 型を持つファイル 423、トップレベル型 619、複数型ファイル 55。
- 分割後（最終走査）: ファイル 624、トップレベル型 618（走査上の数。`IElevatedProcessLauncher.cs` は二重 BOM のため走査が 0 型と誤判定しており、実際は 619 型）、複数型ファイル 0、単一型でファイル名と型名が（大文字・小文字を含めて）一致しないファイル 0。
- 型を持たないファイル: `GlobalSuppressions.cs`、`Properties/AssemblyInfo.cs`、`DS4Forms/Themes/_.cs`、`Updater/UpdaterInstallTrace.cs`（プレースホルダー）、`StressDispatchRaceTests.cs`。

## 4. 検証

- **ツールによる自己検証（各ファイルの出力ごと）**: 再読み込みして、(1) トップレベル型が 1 つ、(2) 型の宣言テキストが分割前と完全に一致、(3) 構文エラーが 0、(4) BOM が一致。全件 `VERIFY-OK`。
- **型インベントリの比較**: 各ウェーブの前後で、型名の一覧（ソート済み）が一致。
- **ビルド・テスト**: 各ウェーブでビルド、テストビルド、`dotnet test`（`DS4WindowsTests`、`StandaloneTests`）が成功。
- **退行ガードの引き継ぎ**: `Step10FourGlobalRatchetGuardTests` の基準値（ファイル別の `Global.*`／`AppHost`／`Program.rootHub` の件数）を、新設ファイルへ引き継いだ。合計は不変（現行値: `Global.*` 780、`AppHost` 48、`rootHub` 25。Step10-4 完了時の記録 778・51 は集計の誤り）。ガードの探索の起点も、`ScpUtil.cs` から `Global.cs` へ変更した（旧パスでは検査が省略され合格になるため）。

## 5. 見つかった問題と対応

- **W2**: `DI/Compat/AppHost.cs` の `AppHost.GetService` 2 件が、基準 0 でラチェットに失敗した（適用前に基準値を更新していなかった）。基準値を引き継いで解消。
- **W3**: `ProfileSettingsViewModelGlobalReferenceGuardTests` が、`PresetMenuHelper` のコンストラクターを `ProfileSettingsViewModel.cs` から探していたため、`PresetMenuHelper.cs` を読むよう更新。分離した型への「定数以外の `Global` が無い」検査も追加した。
- **W4**: `core.ignorecase=true` のため、`DS4Sixaxis.cs`→`DS4SixAxis.cs` の改名は、`git rm --cached` と `git add` が必要だった。基準値の `Dictionary` が大文字・小文字を区別するため、キーも更新した。
- **ツール**: 名前空間が複数のファイル（`DI/AppHost.cs`）は、ツールでは扱えない（ISSUE として報告）。テキストの切り出しで個別に対応した。

## 6. 後続への申し送り

- **Step11（総合検証）**: 分割後のコードで、自動テストと実機マトリクスを実施する。`Global`／`BackingStore` の静的初期化の順序（型ごとの静的コンストラクター）は、起動の確認で再確認する（宣言を変えずに移しただけのため、変化は想定していない）。`Phase6-Step11-Plan.md` に追記済み。
- **Step12（旧シム削除）**: `Global` が単独ファイル（`Global.cs`）になった状態で行う。削除対象のレビューは、`Global.cs` と `BackingStore.cs` で見通せる。
- **ラチェットの基準値**: ファイル分割後の基準値は、`Step10FourGlobalRatchetGuardTests` に、ファイル別の値として記録済み。型を追加・分割するときは、新しいファイルへ件数を引き継ぐ（規約 §3.3 に追記済み）。
- **最終クリーンアップ（案 4）**: 1 型 1 ファイルが前提になる。層別のフォルダ再編と、名前空間とフォルダの一致は、その時点で行う。
- **分割ツール**: `docs-forDIMG/MadeByAgent/tools/Step10b/` に保存した。今後、複数型ファイルが混入した場合の再分割にも使える（ただし、新規ファイルに複数型を置かないのが原則）。

## 7. 変更したファイル（主なもの）

- 本番コード: 分割した 55 ファイルと、新設の 196 ファイル、改名の 4 ファイル。内容（型の宣言）は変更していない。
- テスト: `Step10FourGlobalRatchetGuardTests`（基準値の引き継ぎ、起点の変更）、`ProfileSettingsViewModelGlobalReferenceGuardTests`（`PresetMenuHelper.cs` の参照）。
- 文書: `Phase6-Step10b-Plan.md`、`.github/copilot-instructions.md` §3.3、`Phase6-Status.md`、`Phase6-Step11-Plan.md`。
- ツール: `docs-forDIMG/MadeByAgent/tools/Step10b/`（新規）。
