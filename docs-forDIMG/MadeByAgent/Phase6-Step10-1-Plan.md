# Phase6-Step10-1 計画書: グループ1【起動・環境系】の View／ViewModel ペア Pure DI 化

作成日: 2026-10-14  
状態: 計画案・承認待ち（実装未着手）  
上位計画書: `Phase6-Step10-Plan.md`（§0A.2 が本 Step の台帳）  
準拠: `.github/copilot-instructions.md` §2.1〜2.4、§3.1、§3.3、§3.4

---

## 1. 目的
- 起動・環境系の小型 View と ViewModel の `Global` 直接参照と、`AppHost.GetService`／`Program.rootHub` のフォールバックを解消する。
- 決定3（View の依存は親から必須引数）と決定4（`StartupMethods` は静的のまま引数化）に従う。
- 動作は変えない。新しい契約は `Global` への薄い委譲にし、契約テストで同じ値・同じ動作を固定する。

## 2. 対象（現行コード）
- `SaveWhere.xaml.cs`、`WelcomeDialog.xaml.cs`、`About.xaml.cs`、`UpdaterWindow.xaml.cs`
- `LanguageSelectDialog.xaml.cs`、`LanguagePackControl.xaml.cs`
- `AboutViewModel.cs`、`LogViewModel.cs`、`UpdaterWindowViewModel.cs`、`LanguagePackViewModel.cs`
- 静的結合: `SettingsViewModel` の `App.logHolder`×5・`StartupMethods.*`×16・`Util.GetHidHideClientPath`、`TrayIconViewModel` の `App.Current.Dispatcher`、`StartupMethods.cs`（`exedirpath`×5、`exelocation`×3、`exeFileName`×1）、`Util.cs`

## 3. 現行コードの調査結果（設計に影響するもの）
- **`SaveWhere` は Post-Host**（`App.xaml.cs:359`。`InitializePostHostServices` の後）。サービスを引数で渡せる。
- **`WelcomeDialog` は `-driverinstall` 分岐（Pre-Host）だけで生成される**（`App.xaml.cs:679`）。呼び出し側がすでに `FindConfigLocation`／`Load` を実行しており、ダイアログのコンストラクタ（`loadConfig = true`）でも同じ呼び出しが重ねて行われている。この 2 件は除外（Pre-Host の手動分岐）として温存し、動作は変えない。
- **`LanguageSelectDialog` は Post-Host の初回起動だけで生成される**（`App.xaml.cs:337`）。
- **`PathService.AppDataPath` の setter は罠**: `Global.SaveWhere()` を呼ばず、`PathService` 内部のフィールドを変えるだけ（`App.xaml.cs:643` のコメントの「孤立実装」）。`SaveWhere` 画面の置換に `AppDataPath` の setter を使ってはならない。`Global.SaveWhere(path)` は `m_Config` の 4 つのファイルパス（Profiles／Actions／LinkedProfiles／ControllerConfigs）も更新する。
- **`IPathService` で使える既存メンバー**: `ProgramDirectory`（`exedirpath`）、`ExecutablePath`（`exelocation`）、`RoamingAppDataPath`（`appDataPpath`）、`HasMultipleSaveLocations`。
- **`IEnvironmentService` に無いもの**: `IsWin10OrGreater`、`IsWin8OrGreater`、`IsFakerInputInstalled`、`RefreshViGEmBusInfo`。ほかに `IsViGEmBusInstalled`、`AdminNeeded`、`IsAdministrator`、`ApplicationVersion`、`HidHideInstalled`、`RefreshHidHideInfo` は既にある。
- **`AboutViewModel` は呼出元 0 件**: `About` 画面は `AboutViewModel` を使っておらず、`ServiceRegistration.cs:96` の登録と `PatternAViewModelTests` の解決テストだけが参照している。§2.4 の対象（後述の決定 D9）。
- **`LanguagePackControl` は `MainWindow.xaml:417` で XAML から生成される**ため、引数付きコンストラクタを使えない（後述の決定 D8）。
- **`LogViewModel` は引数なしコンストラクタを持つ**（`PatternAViewModelTests` が DI から解決する）。

## 4. 契約の追加（Global への薄い委譲。契約テストで固定）
- `IEnvironmentService`: `IsWin10OrGreater()`、`IsWin8OrGreater()`、`IsFakerInputInstalled()`、`RefreshViGEmBusInfo()`
  - 注意: `EnvironmentService` は静的初期化の循環を避けるため `IsWin8OrGreater` を内部で複製している（`EnvironmentServiceStaticInitGuardTests` の対象）。新規メンバーは `Global` のメソッドを呼ぶだけの委譲とし、静的初期化に影響しないことをガードテストで確認する。
- `IPathService`: 現在の保存先を決める操作（`Global.SaveWhere(path)` と同じ動作。名前は `SetSaveLocation(string path)` を案とする）と、空のプロファイル一覧ファイルを作る操作（`Global.SaveDefault(path)`。名前は `CreateEmptyProfilesFile(string path)` を案とする）
- `IAppSettingsService`: `string LastVersionChecked { get; set; }`（`LastVersionCheckedNum` は既存）、`void SetCulture(string culture)`（`UseLang` と同じ言語設定の仲間）
- 契約にメンバーを足したら、契約を実装するテスト用クラスがないかを確認する。

## 5. マイクロステップ
各ステップのあと、ビルド・テスト・実機確認の結果を待ってから次へ進む。

### Step10-1-1: 契約の追加と契約テスト
- 上記の契約を追加し、`Global` と同じ値・同じ動作であることを契約テスト（`Step10GroupOneContractTests` 案）で固定する。
- `SetSaveLocation` は、呼び出し後に `Global.appdatapath` と `Global.ProfilePath` 等が更新されることを検証する（`AppDataPath` setter と取り違えない）。

### Step10-1-2: 起動前後のダイアログ（`SaveWhere`／`WelcomeDialog`／`LanguageSelectDialog`）
- `SaveWhere(bool multisavespots, IPathService, IEnvironmentService)`: `AdminNeeded` → `IEnvironmentService`。`exedirpath` → `ProgramDirectory`、`appDataPpath` → `RoamingAppDataPath`、`SaveWhere`／`SaveDefault` → 新契約。`App.xaml.cs:359` の生成呼び出しに保持済みのサービスを渡す。
- `WelcomeDialog(IEnvironmentService, bool loadConfig = false)`: Win10／Win8／ViGEm／HidHide／FakerInput の 8 件を置換。`FindConfigLocation`／`Load` の 2 件は温存（理由を TODO コメントに記載）。`App.xaml.cs:679` で渡す（Pre-Host。ブートストラップ領域の `AppHost.GetService` はここだけ許容）。
- `LanguageSelectDialog(IAppSettingsService)`: `UseLang`／`SetCulture` を置換。`App.xaml.cs:337` で渡す。

### Step10-1-3: `About`／`UpdaterWindow` とその ViewModel
- `About(IEnvironmentService)`: `exeversion` → `ApplicationVersion`。`MainWindow` が保持する `environmentService` を渡す。
- `UpdaterWindow(string newversion, IEnvironmentService, IAppSettingsService)`: `IsAdministrator`×2 を置換。`UpdaterWindowViewModel` には `IAppSettingsService` を渡し、`LastVersionChecked` の 2 件を置換。
- `LogViewModel(ControlService, IEnvironmentService)`: `exeversion` を置換。引数なしコンストラクタの扱いは決定 D9 とあわせて確認する。
- `AboutViewModel`: 決定 D9 に従う。

### Step10-1-4: `LanguagePackControl`／`LanguagePackViewModel`
- 決定 D8 に従い、`MainWindow` から依存を渡す。
- `LanguagePackViewModel(IAppSettingsService)`: `UseLang` の 3 件を置換（定数 `PROBING_PATH`・`LANGUAGE_ASSEMBLY_NAME` は温存）。`LanguagePackControl` の `Global.Save()` → `IAppSettingsService.Save()`。

### Step10-1-5: 静的結合（`SettingsViewModel`／`StartupMethods`／`Util`／`TrayIconViewModel`）
- `StartupMethods`（静的クラスのまま）: `exedirpath`／`exelocation`／`exeFileName` を引数で受け取る形にする。呼び出し側（`SettingsViewModel`、`App.xaml.cs:694`）が `ProgramDirectory`／`ExecutablePath` を渡す。`exeFileName` は `Path.GetFileName(ExecutablePath)` と同値であることをテストで固定する。
- `SettingsViewModel` の `App.logHolder`×5: 決定 D7 に従う。
- `Util.GetHidHideClientPath` と `Util.cs` の `exedirpath`×2: 呼び出し元と値を突き合わせてから、引数化するか温存するかを確定する。
- `TrayIconViewModel` の `App.Current.Dispatcher`: 決定 D10 に従う。

### Step10-1-6: ガードテストと文書
- `Step10GroupOneGlobalReferenceGuardTests`（新規）: 対象ファイルに `Global.`（温存項目を除く）・`AppHost`・`Program.rootHub` が無いことをソース走査で固定する。既存の `SettingsViewModelGlobalReferenceGuardTests` と `MainWindowsAndTrayViewModelGlobalReferenceGuardTests` は維持する。
- `Phase6-Status.md`、`Phase6-Step10-Plan.md`、モデル図（03 のクラス図、04 の §2）を更新する。

## 6. 実機確認（案。実機確認ができない項目は `Phase6-Step11-Plan.md` §3.3 へ登録する）
- 既存の設定がある環境では確認できない項目: 初回起動の `LanguageSelectDialog`／`SaveWhere`（設定フォルダを退避してから起動する。実施方法を着手時に提示する）。
- 通常確認: About 画面のバージョン表示、ログタブの先頭行（バージョン）、言語パックの適用と保存、`Updater` の「スキップ」。
- `StartupMethods`（設定タブの「起動時に実行」）の作成・削除・場所の変更。
- `-driverinstall` 起動の `WelcomeDialog`（環境があれば）。

## 7. 決定が必要な事項
- **D7（`App.logHolder` の置換）**: 案A（推奨）= `LoggerHolder` が実装する小さな契約（`UpdateLogSettings`／`UpdateNLogConfig`／`RestoreArchiveSetting`。契約名は `ILogSettingsApplier` を案）を `DI/` に置き、`App` が `MainWindow` へ渡し、`MainWindow` が `SettingsViewModel` へ必須引数で渡す。案B = `Func<LoggerHolder>` を渡す（契約を作らない。具体クラスへの依存が残る）。
- **D8（`LanguagePackControl` の依存）**: 案A（推奨）= 引数なしのコンストラクタを残し、`Initialize(IAppSettingsService, ...)` を追加する。`MainWindow.xaml` にコントロールの名前を付け、`MainWindow` のコードビハインドで呼ぶ（一覧スキャンは `Initialize` の中へ移す）。案B = XAML からの生成をやめ、`MainWindow` のコードビハインドで生成して追加する（レイアウトの差分が増える）。
- **D9（`AboutViewModel` ほか呼出元 0 件・引数なしコンストラクタの扱い）**: `AboutViewModel` は `About` 画面で使われていない（登録とテストだけ）。案A（推奨）= §2.4 に従い、経緯を調べる（`git log -S`）。本来使うべき（About 画面を ViewModel 化する予定だった）なら温存して TODO を付け、`Global.exeversion` と `AppHost` のフォールバックだけは本 Step で是正する。案B = 削除する（機能廃止にはあたらない）が、承認が必要。`LogViewModel` の引数なしコンストラクタは、`PatternAViewModelTests` のためだけなら案A のとおりに扱う。
- **D10（`TrayIconViewModel` の `App.Current.Dispatcher`）**: 案A（推奨）= `Dispatcher` をコンストラクタの引数で受け取る（`MainWindow` が渡す）。案B = 温存する（WPF の UI スレッドの共用ディスパッチャで、状態ではない。理由を記録）。

### 7.1 決定（2026-10-14、すべて推奨案で承認）
- **D7＝A**: `LoggerHolder` が実装する契約（`ILogSettingsApplier` 案）を `DI/` に置き、`App` → `MainWindow` → `SettingsViewModel` へ必須引数で渡す。
- **D8＝A**: `LanguagePackControl` は引数なしコンストラクタを残し、`Initialize(...)` を追加して `MainWindow` のコンストラクタ内で呼ぶ。
- **D9＝A**: `AboutViewModel` は経緯を調べてから判断する（`git log -S`）。削除する場合は承認を得る。
- **D10＝A**: `TrayIconViewModel` は `Dispatcher` をコンストラクタの引数で受け取る。

### 7.2 進捗
- Step10-1-2: 完了（ビルド・テストビルド・テスト実行成功、コミット・リモート反映済み、実機確認問題なし）。
- **Step10-1-6（ガードテストと文書）: 実装済み・ビルド、テストの結果待ち。グループ1は本ステップの確認で完了。**
  - `Util.ElevatedCopyUpdater` を削除した（承認済み）。`Util.cs` の `Global.exedirpath` 参照は 0 件になった。
  - ガードテスト `Step10GroupOneGlobalReferenceGuardTests`（5 件）: 置換済みの 12 ファイルに温存項目（`WelcomeDialog` の `FindConfigLocation`／`Load` と定数、`LanguagePackViewModel` の定数、`TrayIconViewModel` の `RESOURCES_PREFIX`）以外の `Global.` が無いこと、`AppHost`／`Program.rootHub`／`logHolder`／`App.Current` が無いこと、`StartupMethods` が引数で受け取ること、`ElevatedCopyUpdater` が戻っていないこと、`MainWindow` が `LanguagePackControl.Initialize` を呼ぶこと。
  - モデル図: 03 のクラス図（`ILogSettingsApplier`／`LogSettingsApplierProxy`／`LoggerHolder`、`SettingsViewModel`・`TrayIconViewModel` の依存）と注釈、04 の §2（`ILogSettingsApplier` の行、`SettingsViewModel`・`TrayIconViewModel` の依存）を更新した。
  - 温存・持ち越し: `Util.StartProcessHelper`（`Global.IsAdministrator`。Step12 で判断）、`Util.GetHidHideClientPath`（純粋関数）、`LanguagePackControl` の `Application.Current as App`（`ApplyLanguageSettingPublic` の呼び出し。言語適用処理の整理は Phase7）、`MainWindow` の `AppHost.GetService` の並び（Step10 の別枠）、`AboutViewModel` の About 画面への接続（Step12）、「チェックなしの既定が削除」の `SaveWhere` の仕様（`Phase6-Step11-Plan.md` §3.3 の検討）。
- Step10-1-5（`SettingsViewModel`／`StartupMethods`／`TrayIconViewModel`、`Util` は判断のみ）: 完了（ビルド・テストビルド・テスト実行成功、コミット・リモート反映済み、実機確認問題なし）。
  - **`App.logHolder`（決定 D7＝A）**: 契約 `ILogSettingsApplier`（`DI/`）を新設し、`LoggerHolder` が実装した。`LoggerHolder` は設定の読み込み後にしか生成できない（コンストラクタが例外を投げる）ため DI コンテナで生成させず、遅延接続の `LogSettingsApplierProxy`（`DS4Control/Services/`）を登録し、`App` が `LoggerHolder` の生成直後に `Attach` する。接続前の呼び出しは何もしない（置換前の `App.logHolder?.…` と同じ）。`SettingsViewModel` は 5 番目の必須引数で受け取る。`MainWindow` は他のサービスと同じ並びで `AppHost.GetService` から解決する（`MainWindow` 全体の Pure DI 化は Step10 の別枠）。`App.logHolder` 静的フィールドは、`ProfileEditor.xaml.cs` が `Logger` を使うため温存（§2.1 のシム）。
  - **`StartupMethods`（決定 D4＝A）**: 静的クラスのまま、`exedirpath`／`exelocation`／`exeFileName` を引数で受け取る形にした（`WriteStartProgEntry(exeLocation, programDirectory)`、`DeleteOldTaskEntry(programDirectory)`、`WriteTaskEntry(programDirectory, exeFileName)`、`CheckStartupExeLocation(exeLocation)`）。呼び出し側の `SettingsViewModel` が `IPathService.ProgramDirectory`／`ExecutablePath` を渡す。静的フィールド `taskBatPath` は廃止し、呼び出しごとに計算する（値は同じ）。Task Scheduler・ショートカットを使うため、単体テストは作らず実機確認で確認する。
  - **`TrayIconViewModel`（決定 D10＝A）**: `Dispatcher` を 7 番目の必須引数で受け取る（`MainWindow` が自身の `Dispatcher` を渡す）。`App.Current.Dispatcher`／`Application.Current.Dispatcher` の 4 箇所を置換。
  - **`Util.GetHidHideClientPath`（判断: 温存）**: レジストリを読むだけの状態を持たない静的関数で、`Global` を参照しない（`MainWindow`／`SettingsViewModel` が呼ぶ）。決定1と同じ「純粋関数」として温存する。
  - **`Util.StartProcessHelper`（判断: 温存）**: `Global.IsAdministrator()` を 1 件参照。呼び出し元が 24 件の静的ユーティリティで、置換には全呼び出し元の引数化が必要なため、Step12 で判断する。
  - **`Util.ElevatedCopyUpdater`（§2.4・削除の提案。承認待ち）**: 【削除済み（2026-10-14 承認）】呼出元 0 件。`Global.exedirpath` を 2 件参照。旧 DS4Updater の差し替え用バッチを作る処理で、Step9 で削除した更新処理（`LauchDS4Updater` など）と同じ系統とみられる。経緯（`git log -S`）: 最後の呼び出し元は MainWindow のコードビハインドにあったが、コミット `b55d160b`（Updater 機能の改善）で `UpdaterWindow` の方式へ置き換えられ、本メソッドだけが孤立して残った。置換後の正規経路（`UpdaterWindow`）と重複する旧方式の残骸で、「使うべきでないもの」にあたるため、削除を提案し、承認を得て削除した。
  - テスト: `LogSettingsApplierProxyTests`（4 件）。`SettingsViewModelGlobalReferenceGuardTests`／`MainWindowsAndTrayViewModelGlobalReferenceGuardTests` のコンストラクタ呼び出しを更新（`ILogSettingsApplier`／`Dispatcher` の null 拒否を追加）。
- Step10-1-4（`LanguagePackControl`／`LanguagePackViewModel`）: 実装済み・ビルド、テスト、実機確認の結果待ち。決定 D8＝A: `LanguagePackControl` は引数なしコンストラクタを残し、`Initialize(IAppSettingsService)` を追加（二重呼び出しと null を拒否）。`MainWindow.xaml` にコントロールの名前（`languagePackControl`）を付け、`MainWindow` のコンストラクタが `settingsTab.DataContext` の設定直後に呼ぶ。一覧のスキャンは `Initialize` の中へ移した（呼び出しのタイミングは置換前と同じ）。`Global.Save()` → `IAppSettingsService.Save()`、`UseLang` を置換。テスト: `Phase6Step10GroupOneViewModelTests` に 2 件追加。
- Step10-1-4 の実機確認で判明した既存の不具合の是正（置換前からの不具合で、本 Step の回帰ではない）: 言語パックで「No (English UI)」（カルチャ名が空）を適用しても、再起動するまで UI が英語に切り替わらなかった。原因は `App.ApplyLanguageSetting` が空文字を無視して何も変えないこと（起動時は `SetUICulture` が空文字をインバリアントカルチャとして適用していた）。`App.ApplyLanguageSettingPublic` が空文字のとき `SetUICulture(string.Empty)` を呼ぶようにした。実機確認: 他の言語から「No (English UI)」を選んで適用すると、その場で英語 UI になること。
- Step10-1-3（`About`／`UpdaterWindow`／`LogViewModel`／`AboutViewModel`／`UpdaterWindowViewModel`）: 完了（ビルド・テストビルド・テスト実行成功、コミット・リモート反映済み。About 画面のバージョンとログタブの先頭行は実機確認済み。Updater の実機確認は `Phase6-Step11-Plan.md` §3.3 へ先送り）。D9 の調査結果: `AboutViewModel` は Phase4-Step7 で「全画面の MVVM 統一」のために新設されたが、About 画面は切り替えられていない（呼出元は DI 登録とテストのみ）。§2.4 の「使うべきもの」と判断して温存し、TODO を付与、`Global.exeversion` と未使用の `IPathService` の `AppHost` フォールバックのみ是正した（コンストラクタは `IEnvironmentService` の必須引数）。About 画面への接続は Phase6-Step12 で判断する。テスト: `Phase6Step10GroupOneViewModelTests`（5 件）。
- Step10-1-2（`SaveWhere`／`WelcomeDialog`／`LanguageSelectDialog`）: 実装済み・ビルド、テスト、実機確認の結果待ち。`IEnvironmentService.IsHidHideInstalled()` を追加（`Global.IsHidHideInstalled` の再確認。キャッシュ値の `HidHideInstalled` とは別物のため、`RefreshHidHideInfo` + `HidHideInstalled` では置き換えない）。`WelcomeDialog` の `FindConfigLocation`／`Load` は TODO コメント付きで温存。
- Step10-1-2 の実機確認で判明した既存不具合の是正（案A、2026-10-14 承認。置換前からの不具合で、本 Step の回帰ではない）: 複数の保存場所がある状態で「Appdata」を選ぶと、プログラムフォルダの `Actions.xml`／`ControllerConfigs.xml`／`OutputSlots.xml`（と `LinkedProfiles.xml`）が残った。`Profiles` フォルダが無いと `DirectoryNotFoundException`（未捕捉）にもなり得た。新規の `ProgramFolderSettingsCleaner`（`DS4Forms/`）へ削除を移し、6 つの設定ファイルと `Profiles` フォルダを個別に例外処理して削除する。`Macros`／`Logs` には触れない。テスト: `ProgramFolderSettingsCleanerTests`（5 件）。「チェックなしの既定が削除」の仕様は変更していない（`Phase6-Step11-Plan.md` §3.3 の検討項目）。
- Step10-1-1（契約の追加と契約テスト）: 完了（ビルド・テストビルド・テスト実行成功）。`IEnvironmentService`（4 件）、`IPathService`（`SetSaveLocation`、`CreateEmptyProfilesFile`）、`IAppSettingsService`（`LastVersionChecked`、`SetCulture`）と、`Phase6Step10GroupOneContractTests`（9 件）。

## 8. リスク
- `SetSaveLocation` と `AppDataPath` setter の取り違え（保存先が切り替わらず、設定が消えたように見える）。契約テストで防ぐ。
- `EnvironmentService` の静的初期化の循環（`EnvironmentServiceStaticInitGuardTests`）。新メンバーは `Global` へ委譲するだけにする。
- `LanguagePackControl` の初期化タイミングの変更（言語一覧が空のまま表示される）。`Initialize` を `MainWindow` のコンストラクタ内で呼ぶ。
- 初回起動ダイアログは通常の実機確認では通らない。設定フォルダの退避で確認する。

## 9. 完了条件
- 対象ファイルに、温存項目（`FindConfigLocation`／`Load`、定数）を除く `Global.`・`AppHost.GetService`・`Program.rootHub` が無い。
- 追加した契約の契約テストとガードテストが通り、`dotnet test` が全件合格する。
- 実機確認の結果が記録され、確認できない項目が先送り台帳へ登録されている。
