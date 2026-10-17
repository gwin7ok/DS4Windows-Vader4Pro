# Phase6-Step10 計画書: 残存小型UI・小型ViewModel・補助クラスの機能ドメイン別ペアPure DI化

作成日: 2026-09-11  
改訂日: 2026-09-18（Phase6-Step1 再監査エビデンスおよびStep9引き継ぎ合意に基づく推奨案［機能ドメイン別3グループ分割・View/ViewModelペアPure DI化］採用・全面拡充改訂）  
状態: 計画書改訂・承認待ち（実装未着手）  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`, `docs-forDIMG/DI-App-Wide-Migration-Plan.md`, `docs-forDIMG/Model-Diagram/02-Layer-Architecture-Diagram.md`  
参照エビデンス:  
  - `Phase6-Step1-UI-Reference-Evidence.md` §2〜§4（UI層・小型ViewModel層 実地監査記録）  
  - `Phase6-Step1-ABC-Classification.md`（a/b/c分類台帳）  
  - `Phase6-Step1-Unique-ID-Manifest.md`（一意ID台帳）  
  - `Phase6-Step9-Plan.md`（主要ViewModel移行計画・残存小型ViewModel引き継ぎ台帳）  

---

> **Step9 からの申し送り（2026-10-14 追記。Step10-0 で必ず確認すること）**
> - **台帳は作り直しが必要**: 本書の行番号・件数は 2026-09-18 版。Step9 の変更（下記）で変わった箇所がある。Step10-0 で、現行コードから再集計すること（Step9 のやり方は `Phase6-Step9-Plan.md` §0A）。
> - **Step9 で追加した契約（Step10 の置換先として使う）**:
>   - `IPathService.ProgramDirectory`（`Global.exedirpath` と同じ値）: `SaveWhere.xaml.cs`（5 件）、`Util.cs`（2 件、`Global.exedirpath`）、`StartupMethods`（4 件、静的クラス）の置換先。`IPathService.ExecutableDirectory`（`AppContext.BaseDirectory`）は値が異なるため、`exedirpath` の置換には使わない。
>   - `IProfileSettingsService.RefreshActionAlias(DS4ControlSettings, bool)`: `BindingWindowViewModel.cs` の `Global.RefreshActionAlias`（706、770 行）の置換先。
>   - `IAppSettingsService`: `UseCustomSteamFolder`、`CustomSteamFolder`（`AutoProfiles.xaml.cs` の 80〜82 行付近の置換先）、`FakeExeName`、`LogMaxArchiveFiles`。
>   - `IEnvironmentService`: `IsViGEmBusInstalled`、`IsRunningSupportedViGEmBus`、`IsUsingMinViGEm117333`、`AdminNeeded`。
>   - `IAppearanceSettingsService.BatteryChanged`（購読）。
> - **`MainWindow.xaml.cs` の生成呼び出しが変わった**（Step9 で、保持済みのサービスを渡す形に合わせた）: `new SettingsViewModel(appSettingsService, appearanceSettingsService, environmentService, pathService)`、`new TrayIconViewModel(controlService, profileListHolder, appearanceSettingsService, profileRepo, environmentService, pathService)`、`new MainWindowsViewModel(environmentService, pathService)`（フォールバック。通常は DI から解決）。コンストラクタ内の `AppHost.GetService` の並び（サービス解決 10 件以上）と `Program.rootHub`／`Global.*Instance` のフォールバックは Step10 の範囲のまま。
> - **`ViewModelFactory` のコンストラクタが必須引数になった**（`IProfileSettingsService`、`IProfileRepository`、`ISpecialActionRepository`、`IOutputSlotService`、`IEnvironmentService`、`IPathService`、`Func<ControlService>`）。`CreateRecordBoxViewModel`／`CreateSpecialActEditorViewModel`／`CreateAutoProfilesViewModel` は変更していない（Step10 でそれぞれの ViewModel の依存を必須にする際に、ファクトリの必須引数へ追加する）。`ProfileEditor.xaml.cs` のレガシーフォールバック（`new ProfileSettingsViewModel`）は削除済み。
> - **Step9 で削除したもの**: `MainWindowsViewModel` の `RunUpdaterCheck`、`DownloadUpstreamVersionInfo`、`LauchDS4Updater`、`IsNET8Available`、`DownloadUpstreamUpdaterVersion`、`updaterExe`、`SettingsViewModel_CheckForUpdatesChanged`（いずれも呼出元 0 件）。更新の機能は `UpdaterWindow`（本 Step の対象）が担う。
> - **Step9 で触れていない静的結合（Step10 の台帳）**: `SettingsViewModel` の `App.logHolder`（5 箇所）・`StartupMethods.*`（静的クラス、16 行）・`Util.GetHidHideClientPath()`、`TrayIconViewModel` の `App.Current.Dispatcher`。
> - **維持が必要なソース走査ガード**: `SettingsViewModelGlobalReferenceGuardTests`、`MainWindowsAndTrayViewModelGlobalReferenceGuardTests`、`ProfileSettingsViewModelGlobalReferenceGuardTests`（`ViewModelFactory` のコンストラクタに `AppHost` と `= null` が無いことを検査）。`ViewModelFactory` に引数を足すときは、ガードの `Func<ControlService>` の検査（コンストラクタの文字列範囲）を確認すること。
> - **持ち越し**: K9-3（プリセットメニューのサブメニュー）。`ProfileEditor.xaml`／`MainWindow.xaml` を Step10 で触るときに、単一モニター・同一 DPI の環境で再現するかを確認する案（`Phase6-Status.md` §6.5）。
> - 詳細は `Phase6-Step9-Plan.md` §0A、`Phase6-Step9-Completion-Report.md`。

> **Step9b（PR #1 の Copilot レビュー）からの申し送り（2026-10-14 追記。Step10-0 で台帳に組み込むこと）**
> - **F09**: `ControllersViewModel` のコンストラクタが任意引数（`= null`）＋ `AppHost.GetService` のフォールバック（`ControllersViewModel.cs:18〜23`）。Step9 の決定1＝S9-A と同じ方針（依存を必須引数にし、null を拒否する）で解消する。DI には Singleton で登録済み。
> - **F08**: サービス実装のコンストラクタにも同じフォールバックがある。`ProfileActionChainService.cs:15`（`?? AppHost.GetService<IMappingActionDispatcher>() ?? new MappingActionDispatcher()`）、`ProfileApplicationService`（`_deviceState`／`_control`／`_profileRepo` など）。Step10-0 で、サービス実装のコンストラクタの `AppHost.GetService`／`new` フォールバックを洗い出し、グループとして台帳に載せる（`copilot-instructions.md` §3.1 は「ViewModel やサービス」の両方を対象にしている）。
> - **付随の整理項目（Step9b で判明。Step10-0 の台帳へ）**: (1) `SpecialActionsListViewModel` の `actionManager` フィールドは代入のみで未使用（`IManagedActionManager` を `AppHost.GetService` で任意引数のフォールバック取得。F09 と同じ Pure DI の整理対象）。(2) `ProfileApplicationService` の `deviceIndex >= 4` の拒否（既知の K1。F05 と同じ「4 スロット固定」の前提で、8 スロット拡張時に影響）。(3) `IManagedActionManager`／`IControllerRegistry` は Step9b-2 で再登録済み（`ServiceRegistration`）。F08 の洗い出しでは、`DefaultActionManager` のコンストラクタの `AppHost.GetService<IActionFactory>() ?? new DefaultActionFactory()` も対象に含めること。
> - 詳細は `Phase6-Step9b-Plan.md` §1。

## 0A. Step10-0 台帳（2026-10-14 作成。現行コードの grep による再集計。§2 の旧台帳［72箇所］は参考扱いとし、本節を正とする）

### 0A.1 決定事項（2026-10-14、すべて推奨案で承認）
- **決定1＝A**: 変わらない静的データ表・純粋関数（`ds4inputNames`、`xboxDefaultNames`、`ds4DefaultNames`、`macroDS4Values`、`getX360ControlString`、`configFileDecimalCulture`、`Clamp`、`getDS4ControlsByName` など）は、定数と同じ扱いで「温存項目」とし、置換しない。理由は、実行中に値が変わらず、サービス化しても動作の利点がないため。
- **決定2＝B**: F08（サービス実装のコンストラクタのフォールバック）は、View・ViewModel（Step10-1〜3）の完了後に、Step10-4 として個別に扱う。
- **決定3＝A**: View の依存は、親のウィンドウが保持するサービスをコンストラクタの必須引数で渡す。`AppHost.GetService` のフォールバックは置かない。
- **決定4＝A**: `StartupMethods`（静的クラス）は静的のまま、パスを引数で受け取る形にする。呼び出し側が `IPathService.ProgramDirectory` を渡す。
- **決定5＝B**: §8（`PluginSlot`／`UnplugSlot` の接続）は Step10-5 として、Step10-4 の後に行う。
- **決定6＝分類を進める**: 「全域ゼロ」は、下記 0A.6 の分類の結果、「温存項目を除いてゼロ」と読み替える。

### 0A.2 グループ1【起動・環境系】
- `WelcomeDialog.xaml.cs`: 8 件（`IsWin10OrGreater`×2、`IsWin8OrGreater`×2、`RefreshViGEmBusInfo`、`IsViGEmBusInstalled`、`IsHidHideInstalled`、`IsFakerInputInstalled`）。除外 2 件（`FindConfigLocation`、`Load`。Pre-Host の手動分岐）。定数 `RESOURCES_PREFIX` は温存。
- `SaveWhere.xaml.cs`: 10 行（`AdminNeeded`、`SaveWhere()`×2、`SaveDefault`×2、`exedirpath`×5、`appDataPpath`×4）。置換先: `IEnvironmentService.AdminNeeded`、`IPathService.ProgramDirectory`（`exedirpath`）。`SaveWhere()`／`SaveDefault()`／`appDataPpath` の契約は無い（契約追加が必要。`IPathService.RoamingAppDataPath` の値を確認してから決める）。
- `About.xaml.cs`、`AboutViewModel.cs`、`LogViewModel.cs`: 各 1 件（`exeversion` → `IEnvironmentService.ApplicationVersion`）。`AboutViewModel` の `_pathService` の `AppHost` フォールバックも外す。
- `UpdaterWindow.xaml.cs`: 2 件（`IsAdministrator` → `IEnvironmentService`）。
- `UpdaterWindowViewModel.cs`: 2 件（`LastVersionChecked`。`IAppSettingsService` には `LastVersionCheckedNum` しかない。文字列の契約を追加）。
- `LanguageSelectDialog.xaml.cs`: 3 行（`UseLang`、`SetCulture`、ログ内の `UseLang`）。`SetCulture` の契約は無い。
- `LanguagePackControl.xaml.cs`: 1 件（`Save`）。`LanguagePackViewModel.cs`: 3 件（`UseLang`）。定数 `PROBING_PATH`・`LANGUAGE_ASSEMBLY_NAME` は温存。
- 静的結合（Step9 から）: `SettingsViewModel` の `App.logHolder`×5、`StartupMethods.*`×16、`Util.GetHidHideClientPath()`、`TrayIconViewModel` の `App.Current.Dispatcher`。`StartupMethods.cs` 自身: `exedirpath`×5、`exelocation`×3、`exeFileName`×1。`Util.cs`: `exedirpath`×2 ほか。

### 0A.3 グループ2【プロファイル・マッピング系】
- `BindingWindow.xaml.cs`: Global 7 件（`ds4inputNames`［温存］、`xboxDefaultNames`［温存］、`ds4DefaultNames`［温存］、`defaultButtonMapping`×3、`InverseRumbleMotors`）と `Program.rootHub` フォールバック 1。
- `BindingWindowViewModel.cs`: 3 件（`outDevTypeTemp` → `IOutputSlotService.OutDevTypeTemp`、`RefreshActionAlias`×2 → `IProfileSettingsService.RefreshActionAlias`）。
- `AutoProfiles.xaml.cs`: 4 件（`CreateAutoProfiles`、`UseCustomSteamFolder`、`CustomSteamFolder`×2）。`PathServiceInstance`／`AppHost` のフォールバック。
- `AutoProfilesViewModel.cs`: `?? Global.…` のフォールバック 4 件と `Program.rootHub`。
- `DupBox.xaml.cs`: 2 件、`RenameProfileViewModel.cs`: 1 件（いずれも `appdatapath` → `IPathService.AppDataPath`）。
- `RecordBox.xaml.cs`: 2 件（`appdatapath`）＋ `rootHub` フォールバック。`RecordBoxViewModel.cs`: `ProfileSettingsServiceInstance` 1、`macroDS4Values`［温存］、`rootHub` フォールバック。
- `TouchButtonUserControlViewModel.cs`: 2 件（`TouchpadButtonMode`）。
- `MappingListViewModel.cs`: `GetDS4CSetting`、`defaultButtonMapping`×2、`getX360ControlString`［温存］×3。
- `ControllerListViewModel.cs`: `*Instance` フォールバック 4、`AppHost` 5。
- `ControllerRegDeviceOptsViewModel.cs`: 7 件（`UseMoonlight`×3、`UseAdvancedMoonlight`×3、`SaveControllerConfigs`）。契約は `UseMoonlight`／`UseAdvancedMoonlight` が無い。`SaveControllerConfigsForDevice` は `IProfileXmlStore` にある。
- `ControllersViewModel.cs`（F09）: 任意引数＋`AppHost` 3 件。
- `PresetOption.cs`: `Load*Profile`×10＋`Program.rootHub` フォールバック。`IProfileRepository` には `LoadBlankProfile` しかない。他の 9 種の契約を確認する。
- 旧台帳に無かったもの: `ControllerReadingsControl.xaml.cs`、`StickCalibrationWindow.xaml.cs` の `Program.rootHub` フォールバック（各 1）。

### 0A.4 グループ3【スペシャルアクション系】
- `SpecialActionEditor.xaml.cs`: `RemoveAction` 1。`SpecialActEditorViewModel.cs`: `GetActions` 1。
- `SpecialActionsListViewModel.cs`: `*Instance` フォールバック 3、`RemoveAction`、`GetActions`。未使用の `actionManager` フィールドも整理（F09 と同じ）。
- `SpecialActions/*`: `SaveAction`×8（Check、LaunchProgram、LoadProfile、Macro、MultiActButton、PressKey×2、SpecialActionViewModel）。契約が無い（追加）。`configFileDecimalCulture`［温存］×3。`PressKeyViewModel`: `getX360ControlString`［温存］、`OutContType`×3、`outDevTypeTemp`×1。

### 0A.5 補助グループ（F08。Step10-4）
- `AutoProfileService`（`new AutoProfileHolder()`、`new EnvironmentService()`、`AppHost`×3）、`OutputSlotService`（`new OutputSlotManager`、`new OutputSlotStore`）、`OutputSlotStore`（`new PathService`）、`ProfileActionChainService`、`ProfileApplicationService`（`AppHost`×5。K1 の `deviceIndex >= 4` を含む）、`ProfileSettingsService`（`new EnvironmentService`）、`ProfileRepository`（遅延取得）、`DefaultActionManager`、`DefaultMacroPlayer`、`LaunchProcessAction`、`MacroAction`（3）、`MouseOutputAction`、`ProfileSwitchAction`（3）、`AutoProfileChecker`。
- `Actions/*Action` は、利用側が `new` する箇所と DI の外で生成される経路を突き合わせてから判断する（必須引数化するか、理由を書いて温存するか）。

### 0A.6 グループ外の Global 参照の暫定分類（決定6。Step10-4 の最終走査で確定する）
- **温存（定数・不変データ・純粋関数）**: `MAX_DS4_CONTROLLER_COUNT`、`TEST_PROFILE_INDEX`／`TEST_PROFILE_ITEM_COUNT`、`RESOURCES_PREFIX`、`ASSEMBLY_RESOURCE_PREFIX`、`CONFIG_VERSION`／`APP_CONFIG_VERSION`、`OLD_XINPUT_CONTROLLER_COUNT`、`Clamp`、`getDS4ControlsByName`、`getX360ControlsByName`、`configFileDecimalCulture`、各名前表。
- **温存（Pre-Host。ホスト生成前に動く処理）**: `App.xaml.cs`（`FindConfigLocation`、`Load`、`RefreshViGEmBusInfo`、`UseLang`／`SetCulture`、`appdatapath` など）、`LoggerHolder.cs`（`appdatapath`、`LogMaxArchiveFiles`、`LogMinLevel`）。
- **温存（§2.1 の薄いシム。サービス実装が Global の実体へ委譲）**: `AppSettingsService`、`AppearanceSettingsService`、`EnvironmentService`、`PathService`、`ProfileSettingsService`、`ProfileRepository`、`SpecialActionRepository`、`OutputKBMHandlerAdapter`／`OutputKBMHandlerLifecycle`、`DeviceStateService`、`ProfileActionProvider`、`ProfileApplicationService`、`OutputSlotService`、`AutoProfileService`、`AppNotificationService`、各 DTO（`ProfileDTO`、`AppSettingsDTO`、`ActionsDTO` など）。`ScpUtil.cs` の実体の解体は Phase6 の範囲外。
- **要個別判断（Step10-4 で、各ファイルの既存ガードテストと突き合わせて確定。Step12 へ送る候補を含む）**:
  - `ControlService.cs`: `ProfileSettingsServiceInstance`、`ApplyProfileToSlot`、`activeOutDevType`、`tempprofile*`／`useTempProfile`、`vigem*`、`LightbarSettingsInfo`、`TouchOutMode`、`getOSCServerPortNum`
  - `Mapping.cs`: `ProfileSettingsServiceInstance`×2、`outputKBMHandler`×2、`ApplyProfile`、`ProfileActions`、`TouchOutMode`、`IsUsingSAForControls`、`GetGyroMouseStickInfo`／`GetTouchMouseStickInfo`
  - `Mouse.cs`: `getTrackballMode`、`GyroMouseStickInf`、`GetGyroMouseStickInfo`
  - `DS4Devices.cs`: `UseMoonlight`／`UseAdvancedMoonlight`、`CheckIfVirtualDevice`、`GetInstanceIdFromDevicePath`
  - `DS4Device.cs`: `DebouncingMs`。`DS4Sixaxis.cs`: `UseDs3PitchRollSim`。`DS4OutDeviceBasic`／`DS4OutDeviceExt`: `GetSASteeringWheelEmulationAxis`
  - `OutputSlotManager.cs`: `vigemBusVersionInfo`。`OutputSlotPersist.cs`: `appdatapath`、`exeversion`。`AutoProfileHolder.cs`: `appdatapath`。`AutoProfileService.cs`: `ProfilePath`
  - `MainWindow.xaml.cs`: `*Instance`×5、`ApplyProfileToSlot`、`ProfilePath`、`OlderProfilePath`、`CompileVersionNumberFromString`（Step9 のガードテストの許容範囲を確認）
  - `Log.cs`: `NotificationServiceInstance`。`DefaultElevatedProcessLauncher.cs`: `exelocation`。`ProfileEditor.xaml.cs`: `LSModInfo`
  - 以上を、ファイル別に「温存＋理由」「Step10 で置換」「Step12 送り」へ確定する。

### 0A.7 契約が存在しない置換先（Step10 で契約の追加が必要）
- `SaveWhere()`／`SaveDefault()`／`appDataPpath`（`SaveWhere` 画面）。`exelocation`／`exeFileName`（`StartupMethods`）
- `IAppSettingsService`: `LastVersionChecked`（文字列）、`SetCulture`、`UseMoonlight`、`UseAdvancedMoonlight`
- `ISpecialActionRepository`: `SaveAction`、`GetActions`（`ActionList` はある）
- `IAutoProfileService`（または他）: `CreateAutoProfiles`
- `IProfileRepository`: `LoadBlankProfile` 以外の `Load*Profile` 9 種
- 新しい契約は、`Global` への薄い委譲にし、契約テストで同じ値・同じ動作を固定する（Step9 と同じ）。

### 0A.8 集計
- Step10-1〜3 の Global 直接参照: 約 95 箇所（旧台帳 72。うち温存の静的表が約 12）。フォールバック（`rootHub`／`AppHost`／`*Instance`）は別に約 50 箇所。
- マイクロステップ: Step10-1（グループ1）→ Step10-2（グループ2）→ Step10-3（グループ3）→ Step10-4（F08＋全域の再走査・ガードテスト）→ Step10-5（§8）。

---

## 0. 背景と改訂の経緯

### 0.1 旧計画書からの精査とStep9からの引き継ぎ合意
旧計画書（2026-09-11）では、「`WelcomeDialog`, `PresetOption`, `SaveWhere`, `BindingWindow` の4ファイル・暫定29件のみ」をスコープとしていた。  
しかし、2026-09-18の Step1 精密再監査（`Phase6-Step1-UI-Reference-Evidence.md` §2〜§4）および Step9 計画策定における重要合意により、**Phase6 の最終実装ステップである本 Step10 の真の全容**が以下の通り確定した：

1. **Step9 から正式に引き継がれた残存小型 ViewModel 群（12ファイル・実参照約30箇所）**:  
   `AboutViewModel`, `AutoProfilesViewModel`, `BindingWindowViewModel`, `ControllerListViewModel`, `ControllerRegDeviceOptsViewModel`, `LanguagePackViewModel`, `LogViewModel`, `MappingListViewModel`, `RecordBoxViewModel`, `RenameProfileViewModel`, `TouchButtonUserControlViewModel`, `UpdaterWindowViewModel`
2. **残存する小型 View（XAML コードビハインド 11ファイル・実参照約35箇所）**:  
   `WelcomeDialog.xaml.cs`, `SaveWhere.xaml.cs`, `BindingWindow.xaml.cs`, `About.xaml.cs`, `AutoProfiles.xaml.cs`, `DupBox.xaml.cs`, `LanguagePackControl.xaml.cs`, `LanguageSelectDialog.xaml.cs`, `RecordBox.xaml.cs`, `SpecialActionEditor.xaml.cs`, `UpdaterWindow.xaml.cs`
3. **スペシャルアクション系 ViewModel 群（7ファイル・実参照約10箇所）**:  
   `SpecialActionsListViewModel.cs`（残存フォールバックF）, `CheckBatteryViewModel`, `MacroViewModel`, `PressKeyViewModel` 等の `SaveAction` / `configFileDecimalCulture`
4. **補助データクラス（1ファイル・実参照約10箇所）**:  
   `PresetOption.cs`（プリセット初期化時のコントローラー種別・マッピング参照）

### 0.2 方針の確定: 【推奨案: 選択肢A（機能ドメイン別 3グループ分割・View/ViewModel ペア Pure DI 化）】
本ステップは、**Phase6 において Global 直接参照を完全に根絶する最終防衛ライン**である。残存ファイルを先送りせず、かつ過渡期のバインディング切れを防ぐため、以下の推奨方針を採用する：

- **View と ViewModel のペア改修**:  
  画面（View）とビューモデル（ViewModel）を同時に Pure DI 化することで、WPF のデータバインディング整合性を 100% 保証し、画面単位での結合動作確認を可能とする。
- **機能ドメイン別 3グループ分割**:  
  全約25ファイルを「①起動・システム・環境系」「②プロファイル・入力マッピング系」「③スペシャルアクション編集系」の3つの論理ドメインに分割し、安全・確実にサブPRで消化する。
- **Phase6 目標の完全達成**:  
  本ステップ完了をもって、除外項目（const定数・Pre-Host等）を除く**DS4Windows全体の全クラスから Global 直接参照が文字通り 100% 根絶**される。

---

## 1. 目的

1. 残存するすべての小型 View（11ファイル）、小型 ViewModel（12ファイル＋SpecialActions）、および `PresetOption.cs` において、コンストラクタ経由の Pure DI 構造を確立する。
2. 対象全ファイル内に存在する全実利用 Global 直接参照（計約72箇所）を、注入された DI サービス経由の呼び出しへ完全置換する。
3. 機能ドメイン別に View と ViewModel をペアで移行し、UI バインディング切れや画面クラッシュのリスクを完全に排除する。
4. Phase6 の大目標である「アプリケーション全域からの Global 直接参照根絶」を完全に達成し、Step11（実機総合検証）および Step12（完了判定）への完全な引き渡しを行う。

---

## 2. 全件参照台帳（全3グループ・完全カタログ）

---

### 表1: グループ1【起動・環境・システム設定系】（実参照計23箇所、除外3箇所）

| ID | ファイル名 | 行番号 | 参照メンバ | 分類 | 移行先サービス・メンバ |
|---|---|---|---|---|---|
| C10-G1-01 | `WelcomeDialog.xaml.cs` | 28, 31 | `IsWin10OrGreater()`, `IsWin8OrGreater()` | (b) | `_environmentService.IsWin10OrGreater()`, `.IsWin8OrGreater()` |
| C10-G1-02 | `WelcomeDialog.xaml.cs` | 38, 104 | `RefreshViGEmBusInfo()` | (b) | `_environmentService.RefreshViGEmBusInfo()` |
| C10-G1-03 | `WelcomeDialog.xaml.cs` | 39, 105 | `IsViGEmBusInstalled()` | (b) | `_environmentService.IsViGEmBusInstalled()` |
| C10-G1-04 | `WelcomeDialog.xaml.cs` | 45 | `IsHidHideInstalled` | (b) | `_environmentService.HidHideInstalled` |
| C10-G1-05 | `WelcomeDialog.xaml.cs` | 52 | `IsFakerInputInstalled` | (b) | `_environmentService.IsFakerInputInstalled` |
| C10-G1-06 | `SaveWhere.xaml.cs` | 18 | `AdminNeeded()` | (b) | `_environmentService.AdminNeeded()` |
| C10-G1-07 | `SaveWhere.xaml.cs` | 29, 36 | `SaveWhere`（代入・判定） | (b) | `_pathService.SaveWhere` |
| C10-G1-08 | `SaveWhere.xaml.cs` | 42〜62 | `exedirpath`, `appDataPpath`（7箇所） | (a)/(b) | `_pathService.ExecutablePath`, `_pathService.DefaultAppDataPath` |
| C10-G1-09 | `SaveWhere.xaml.cs` | 69 | `SaveDefault(SaveWhere)` | (b) | `_pathService.SaveDefault(...)` |
| C10-G1-10 | `SaveWhere.xaml.cs` | 87, 88 | `appdatapath`（2箇所） | (b) | `_pathService.AppDataPath` |
| C10-G1-11 | `About.xaml.cs` | 21 | `exeversion` | (a) | `_environmentService.ApplicationVersion` |
| C10-G1-12 | `AboutViewModel.cs` | 19 | `exeversion` | (a) | `_environmentService.ApplicationVersion` |
| C10-G1-13 | `UpdaterWindow.xaml.cs` | 27, 35 | `IsAdministrator()`（2箇所） | (a) | `_environmentService.IsAdministrator()` |
| C10-G1-14 | `UpdaterWindowViewModel.cs`| 42, 58 | `LastVersionChecked`（2箇所） | (b) | `_appSettingsService.LastVersionChecked` |
| C10-G1-15 | `LanguageSelectDialog.xaml.cs`| 31 | `UseLang` | (b) | `_appSettingsService.UseLang` |
| C10-G1-16 | `LanguageSelectDialog.xaml.cs`| 32, 42 | `SetCulture(...)`（2箇所） | (b) | `_appSettingsService.SetCulture(...)` |
| C10-G1-17 | `LanguagePackControl.xaml.cs` | 49 | `Save()` | (a) | `_appSettingsService.Save()` |
| C10-G1-18 | `LanguagePackViewModel.cs` | 45〜60 | `UseLang`（3箇所） | (b) | `_appSettingsService.UseLang` |
| C10-G1-19 | `LogViewModel.cs` | 28 | `exeversion` | (a) | `_environmentService.ApplicationVersion` |
| C10-EX-01 | `WelcomeDialog.xaml.cs` | 19, 20 | `FindConfigLocation`, `Load` | 除外 | Pre-Host ブートストラップ処理（手動呼出分岐） |
| C10-EX-02 | `WelcomeDialog.xaml.cs` | 80 | `BLANK_VIGEM_BUS_VERSION` | 除外 | 真の定数（`const string`） |

---

### 表2: グループ2【プロファイル・入力マッピング・プリセット系】（実参照計38箇所、除外5箇所）

| ID | ファイル名 | 行番号 | 参照メンバ | 分類 | 移行先サービス・メンバ |
|---|---|---|---|---|---|
| C10-G2-01 | `BindingWindow.xaml.cs` | 65 | `ds4inputNames` | (a) | `_profileSettings.Ds4InputNames` |
| C10-G2-02 | `BindingWindow.xaml.cs` | 69, 71 | `xboxDefaultNames`, `ds4DefaultNames` | (a) | `_profileSettings.XboxDefaultNames`, `.Ds4DefaultNames` |
| C10-G2-03 | `BindingWindow.xaml.cs` | 142 | `InverseRumbleMotors[device]` | (a) | `_profileSettings.InverseRumbleMotors[device]` |
| C10-G2-04 | `BindingWindowViewModel.cs`| 41 | `outDevTypeTemp[device]` | (a) | `_outputSlotService.OutDevTypeTemp[device]` |
| C10-G2-05 | `BindingWindowViewModel.cs`| 89, 94 | `RefreshActionAlias(...)`（2箇所） | (b) | `_specialActionRepository.RefreshActionAlias(...)` |
| C10-G2-06 | `AutoProfiles.xaml.cs` | 46〜143 | `appdatapath`（5箇所） | (b) | `_pathService.AppDataPath` |
| C10-G2-07 | `AutoProfiles.xaml.cs` | 88 | `CreateAutoProfiles(...)` | (a) | `_autoProfileService.CreateAutoProfiles(...)` |
| C10-G2-08 | `AutoProfiles.xaml.cs` | 112, 118 | `UseCustomSteamFolder`, `CustomSteamFolder` | (b) | `_appSettingsService.UseCustomSteamFolder`, `.CustomSteamFolder` |
| C10-G2-09 | `AutoProfilesViewModel.cs` | 42, 53 | `AutoProfileRevertDefaultProfile`（2箇所） | (b) | `_appSettingsService.AutoProfileRevertDefaultProfile` |
| C10-G2-10 | `AutoProfilesViewModel.cs` | 65, 78 | `autoProfileSwitchNotifyChoice`（2箇所） | (b) | `_appSettingsService.AutoProfileSwitchNotifyChoice` |
| C10-G2-11 | `DupBox.xaml.cs` | 31, 48 | `appdatapath`（2箇所） | (b) | `_pathService.AppDataPath` |
| C10-G2-12 | `RecordBox.xaml.cs` | 45, 58 | `appdatapath`（2箇所） | (b) | `_pathService.AppDataPath` |
| C10-G2-13 | `RecordBoxViewModel.cs` | 29 | `ProfileSettingsServiceInstance` | (a) | コンストラクタ注入 `_profileSettings` へ統一 |
| C10-G2-14 | `RecordBoxViewModel.cs` | 73 | `macroDS4Values` | (a) | `_profileSettings.MacroDS4Values` |
| C10-G2-15 | `RenameProfileViewModel.cs`| 34 | `appdatapath` | (b) | `_pathService.AppDataPath` |
| C10-G2-16 | `TouchButtonUserControlViewModel.cs`| 28, 44 | `TouchpadButtonMode`（2箇所） | (a) | `_profileSettings.TouchpadButtonMode` |
| C10-G2-17 | `MappingListViewModel.cs` | 52, 58 | `GetDS4CSetting(...)`（2箇所） | (a) | `_profileSettings.GetDS4CSetting(...)` |
| C10-G2-18 | `MappingListViewModel.cs` | 74, 80 | `getX360ControlString(...)`（2箇所） | (a) | `_profileSettings.GetX360ControlString(...)` |
| C10-G2-19 | `MappingListViewModel.cs` | 105, 112 | `defaultButtonMapping`（2箇所） | (a) | `_profileSettings.DefaultButtonMapping` |
| C10-G2-20 | `ControllerListViewModel.cs` | 35〜44 | `ProfileSettingsServiceInstance` 等（4箇所） | (a) | 注入サービス利用へ統一（フォールバック削除） |
| C10-G2-21 | `ControllerRegDeviceOptsViewModel.cs` | 40〜64 | `UseMoonlight`, `UseAdvancedMoonlight`（4箇所） | (b) | `_appSettingsService.UseMoonlight`, `.UseAdvancedMoonlight` |
| C10-G2-22 | `ControllerRegDeviceOptsViewModel.cs` | 112〜138 | `SaveControllerConfigs(...)`（3箇所） | (b) | `_profileXmlStore.SaveControllerConfigsForDevice(...)` |
| C10-G2-23 | `PresetOption.cs` | 45〜68 | `OutContType`（4箇所） | (a) | メソッド引数または `_profileSettings.OutContType` |
| C10-G2-24 | `PresetOption.cs` | 74〜114 | デフォルトマッピング設定（6箇所） | (a) | `_profileSettings.DefaultButtonMapping` 参照 |
| C10-EX-03 | `BindingWindow.xaml.cs` | 42〜57 | `TEST_PROFILE_INDEX`（4箇所） | 除外 | 真の定数（`const int`） |
| C10-EX-04 | `RecordBox.xaml.cs` | 30 | `TEST_PROFILE_INDEX` | 除外 | 真の定数（`const int`） |

---

### 表3: グループ3【スペシャルアクション編集系】（実参照計11箇所）

| ID | ファイル名 | 行番号 | 参照メンバ | 分類 | 移行先サービス・メンバ |
|---|---|---|---|---|---|
| C10-G3-01 | `SpecialActionEditor.xaml.cs` | 85 | `RemoveAction(...)` | (a) | `_specialActionRepository.RemoveAction(...)` |
| C10-G3-02 | `SpecialActionsListViewModel.cs`| 42 | `ProfileSettingsServiceInstance` | (a) | コンストラクタ注入 `_profileSettings` へ統一 |
| C10-G3-03 | `SpecialActionViewModel.cs` | 各所 | `SaveAction(...)` | (a) | `_specialActionRepository.SaveAction(...)` |
| C10-G3-04 | `CheckBatteryViewModel.cs` | 各所 | `configFileDecimalCulture`（2箇所） | (b) | `_environmentService.ConfigFileDecimalCulture` |
| C10-G3-05 | `MacroViewModel.cs` | 各所 | `configFileDecimalCulture`（2箇所）, `SaveAction` | (a)/(b) | `_environmentService.*`, `_specialActionRepository.*` |
| C10-G3-06 | `MultiActButtonViewModel.cs` | 各所 | `SaveAction` | (a) | `_specialActionRepository.SaveAction(...)` |
| C10-G3-07 | `PressKeyViewModel.cs` | 各所 | `SaveAction` | (a) | `_specialActionRepository.SaveAction(...)` |
| C10-G3-08 | 他サブアクション ViewModel | 各所 | アクション保存・設定読込（4箇所） | (a) | `_specialActionRepository.*` |

---

## 3. アーキテクチャ設計・View/ViewModel ペア Pure DI 化

すべての小型ダイアログおよびコントロールにおいて、以下の一貫した設計ルールを適用する。

### 3.1 ViewModel の Pure DI コンストラクタ拡張
すべての小型 ViewModel のコンストラクタで、必要な DI サービスを明示的に受け取り、`readonly` フィールドに格納する。
```csharp
// BindingWindowViewModel.cs の設計例
public class BindingWindowViewModel
{
    private readonly IOutputSlotService _outputSlotService;
    private readonly ISpecialActionRepository _specialActionRepository;

    public BindingWindowViewModel(
        IOutputSlotService outputSlotService = null,
        ISpecialActionRepository specialActionRepository = null)
    {
        _outputSlotService = outputSlotService ?? AppHost.GetService<IOutputSlotService>();
        _specialActionRepository = specialActionRepository ?? AppHost.GetService<ISpecialActionRepository>();
    }
    ...
}
```

### 3.2 View（XAML コードビハインド）の DataContext ペア配線
View はコンストラクタで対応する ViewModel を受け取り、`DataContext` に設定する。自身が直接必要とするサービス（`IPathService` 等）もコンストラクタで受け取る。
```csharp
// SaveWhere.xaml.cs の設計例
public partial class SaveWhere : Window
{
    private readonly IPathService _pathService;
    private readonly IEnvironmentService _environmentService;

    public SaveWhere(
        IPathService pathService = null,
        IEnvironmentService environmentService = null)
    {
        InitializeComponent();
        _pathService = pathService ?? AppHost.GetService<IPathService>();
        _environmentService = environmentService ?? AppHost.GetService<IEnvironmentService>();
    }
    ...
}
```

---

## 4. PR分割計画とマイクロステップ

変更の安全性と追跡性を最大化するため、以下の**5つのサブPR（マイクロステップ）**に分割して進める。

```text
【Phase6-Step10 マイクロステップ構成】
├─ Step10-0: 着手前提検証・ベースライン記録（実装なし）
├─ Step10-1 (PR-1): グループ1【起動・環境・システム設定系】View/ViewModelペアPure DI化（23箇所）
├─ Step10-2 (PR-2): グループ2【プロファイル・マッピング・プリセット系】View/ViewModelペアPure DI化（38箇所）
├─ Step10-3 (PR-3): グループ3【スペシャルアクション編集系】View/ViewModelペアPure DI化（11箇所）
└─ Step10-4 (PR-4): 全体クリーンアップ・未検知参照ゼロ検証・完了報告書作成
```

---

### Step10-0: 着手前提検証・ベースライン記録（実装なし）
1. Step9 完了後の作業ツリー状態、HEAD コミットハッシュ、未コミット差分ゼロを確認。
2. ソリューション全体のビルド（`dotnet build -c Release`）および全単体テスト（`dotnet test`）が 100% グリーンであることを記録。

---

### Step10-1 (PR-1): グループ1【起動・環境・システム設定系】のペア Pure DI 化
- **対象**: 表1に定義された全ファイル（View 6ファイル、ViewModel 4ファイル、実参照計23箇所）
- **作業内容**:
  1. `WelcomeDialog`, `SaveWhere`, `About`, `UpdaterWindow`, `LanguageSelectDialog`, `LanguagePackControl` のコンストラクタを拡張。
  2. `AboutVM`, `UpdaterWindowVM`, `LanguagePackVM`, `LogVM` のコンストラクタを拡張。
  3. 各ファイル内の `Global.*` を注入フィールド経由へ置換。
- **検証**:
  - `dotnet test` 全件合格を確認。
  - Welcome画面、保存先選択画面、言語選択画面の正常表示を確認。

---

### Step10-2 (PR-2): グループ2【プロファイル・マッピング・プリセット系】のペア Pure DI 化
- **対象**: 表2に定義された全ファイル（View 4ファイル、ViewModel 8ファイル、`PresetOption.cs`、実参照計38箇所）
- **作業内容**:
  1. `BindingWindow`, `AutoProfiles`, `DupBox`, `RecordBox` のコードビハインドを拡張。
  2. `BindingWindowVM`, `AutoProfilesVM`, `MappingListVM`, `RecordBoxVM`, `RenameProfileVM`, `TouchButtonVM`, `ControllerListVM`, `ControllerRegDeviceOptsVM` を Pure DI 化。
  3. `PresetOption.cs` のマッピング参照を引数渡し・サービス経由へ置換。
- **検証**:
  - `dotnet test` 全件合格を確認。
  - ボタン割り当て画面、自動プロファイル編集画面が正常に動作することを確認。

---

### Step10-3 (PR-3): グループ3【スペシャルアクション編集系】のペア Pure DI 化
- **対象**: 表3に定義された全ファイル（View 1ファイル、ViewModel 8ファイル、実参照計11箇所）
- **作業内容**:
  1. `SpecialActionEditor.xaml.cs` のコンストラクタ拡張と置換。
  2. `SpecialActionsListViewModel` および `SpecialActions/` 配下の各サブViewModelの Pure DI 化。
  3. アクション保存（`SaveAction`）、削除（`RemoveAction`）、カルチャ参照を置換。
- **検証**:
  - `dotnet test` 全件合格を確認。
  - スペシャルアクションの作成・編集・保存・削除が正常に動作することを確認。

---

### Step10-4 (PR-4): 全体クリーンアップ・未検知参照ゼロ検証・完了報告
- **作業内容**:
  1. 静的コード解析を実行し、除外リスト（const定数・Pre-Host等）以外の `Global.` 直接参照がソリューション全体から完全にゼロになったことを確認。
  2. `DS4WindowsTests` および `StandaloneTests` の全自動テストを実行。
  3. 完了報告書（`Phase6-Step10-Completion-Report.md`）を作成。
- **検証**:
  - 全自動テスト 100% 合格を確認。

---

## 5. テスト・実機動作検証計画

### 5.1 自動単体テスト計画
`DS4WindowsTests` に以下のテストクラスを追加する（1ファイル1型）：
1. **`SmallViewModelsDiWiringTests.cs`**:
   - `AutoProfilesViewModel`, `BindingWindowViewModel`, `MappingListViewModel`, `UpdaterWindowViewModel` 等が全モックを受け取って正常にインスタンス化されること。
2. **`SpecialActionEditorDiTests.cs`**:
   - 各種スペシャルアクション ViewModel の `SaveAction` 実行時に、`ISpecialActionRepository` へ正しくデータが渡ることを検証。
3. **`SaveWherePathServiceTests.cs`**:
   - `SaveWhere` ダイアログの選択結果が `IPathService` に正確に反映されること。

### 5.2 画面別手動動作検証手順（Phase6-Step11連携）
1. **起動・ダイアログ画面**:
   - 初回起動ダイアログ（`SaveWhere`）での「Appdata」「Program Folder」の選択と保存確認。
   - `WelcomeDialog` での ViGEm / HidHide インストール状態表示確認。
2. **ボタン割り当て・自動プロファイル画面**:
   - コントローラーのボタンをクリックして `BindingWindow` を開き、ボタンマッピングを変更・保存。
   - `AutoProfiles` 画面でアプリケーションとプロファイルの紐付けを作成・保存。
3. **スペシャルアクション画面**:
   - 特殊アクション（バッテリー確認、マクロ、キーコンボ等）を新規作成し、正常に保存・一覧表示されること。

---

## 6. ロールバック方針

特定画面の起動失敗やバインディング切れが発生した場合は、該当PRを直ちに `git revert` して直前の健全コミットに復旧する。

---

## 7. 完了判定チェックリスト

- [ ] 表1（グループ1: 23箇所）、表2（グループ2: 38箇所）、表3（グループ3: 11箇所）に定義された全72箇所の実利用 Global 直接参照が完全に解消されていること。
- [ ] 各表に定義された明示的除外項目（const定数、Pre-Hostブートストラップ）以外の Global 直接参照が対象ファイル内に存在しないこと。
- [ ] すべての小型 View と ViewModel がペアで Pure DI 化され、バインディング切れや画面クラッシュが発生しないこと。
- [ ] `PresetOption.cs` のプリセット初期化処理が DI サービス経由で正常に機能すること。
- [ ] ソリューション全体において、Phase6 対象外と規定されたもの以外の Global 直接参照が完全にゼロ（根絶）であること。
- [ ] `dotnet build -c Release` でエラー・警告が0件であること。
- [ ] `DS4WindowsTests` および `StandaloneTests` の全自動テストが100%成功を維持していること。
- [ ] 全画面（Welcome、SaveWhere、BindingWindow、AutoProfiles、SpecialActionEditor等）の手動動作確認が正常であること。
- [ ] §8 の追加作業（出力スロット管理の `IOutputSlotService` 経由化）が完了していること。

---

## 8. 追加作業: 出力スロット管理の `IOutputSlotService` 経由化（2026-09-24 追加、Phase6-Step6 の決定1＝案R による）

### 8.1 背景
- `IOutputSlotService.PluginSlot`／`UnplugSlot` は、Phase5-Step12 で「UI からの仮想スロット操作の正式な入口」として追加されたが、UI 側の切り替え（Phase5-Step12 タスク Step12-5 の当初の意図）が行われず、呼出元 0 件のまま残っている。
- 実際の呼び出し側は、`ControlService.AttachUnboundOutDev`／`DetachUnboundOutDev` を直接呼んでいる:
  - 出力スロット管理画面: `DS4Forms/ViewModels/CurrentOutDeviceViewModel.cs`（`OutSlot_PluginRequest`／`OutSlot_UnplugRequest`）と、それに生の `ControlService`／`OutputSlotManager` を渡す `OutputSlotManagerControl.xaml.cs`（`SetupDataContext`）。
  - UDP コマンド: `DS4Forms/MainWindow.xaml.cs` の `outputslot.<n>.unplug／plugds4／plugx360`（1403〜1407 行付近）。
- 詳細な差分（スレッド、事前条件と種別、依存の取り方、イベント）は `Phase6-Step6-Plan.md` §3.1 を参照。

### 8.2 作業内容（着手時に現行コードと突き合わせて詳細化する）
1. `OutputSlotService.PluginSlot`／`UnplugSlot` を、画面側の現行の挙動と同等にする: `ControlService.EventDispatcher` へのディスパッチ、事前条件（接続状態・入力割り当て）の確認、プラグイン時の種別の決め方。
2. `OutputSlotService` の `ControlService` 取得を、`Program.rootHub` フォールバックから Pure DI へ移す。循環依存（`ControlService` → `Func<IOutputSlotService>`）を避けるため、`Func<ControlService>` による遅延解決、または仮想スロット操作用の出力ポートを導入する（Step2 決定D1 と同じ考え方）。
3. `CurrentOutDeviceViewModel`／`OutputSlotManagerControl` を、`IOutputSlotService` 経由の操作へ切り替える（View/ViewModel ペアの Pure DI 化。画面更新のイベントを `OutputSlotManager.SlotAssigned`／`SlotUnassigned` のままにするか、`OutputSlotChanged` にするかを決める）。
4. `MainWindow.xaml.cs` の UDP コマンドの plug／unplug を、`IOutputSlotService` 経由へ切り替える（`MainWindow` は Step8／Step9 でも扱うため、実施順は着手時に調整する）。
5. テスト: `PluginSlot`／`UnplugSlot` の事前条件とディスパッチ、画面 ViewModel がサービスを呼ぶこと。実機確認: 出力スロット管理画面での手動の抜き差し、恒久スロット（Permanent）の保存、UDP コマンド（環境があれば）。

### 8.3 ガードレール
- ViGEm ネイティブドライバ保護（Phase5-Plan §5.5）: `OutputSlotManager` の非同期キュー（`DeferredPlugin`／`DeferredRemoval`）と破棄順序には手を触れず、`ControlService` の正規 API への中継に留める。
---

## 進捗の記録（2026-10-14 時点）
- **完了**: Step10-0（台帳の作り直しと決定）、Step10-1（グループ1。`Phase6-Step10-1-Plan.md`）、Step10-2（グループ2。`Phase6-Step10-2-Plan.md` §10）、Step10-3（グループ3。`Phase6-Step10-3-Plan.md` §14）。付随: Vader 4 Pro のデバイスサポート削除（`Phase6-Step10-V4P-Removal-Plan.md`）、Step10-3b（アクションの名前変更・削除の全プロファイル反映。`Phase6-Step10-3b-Plan.md` §9）。
- **残り**:
  - **Step10-3c**: SA の「キー押下」でコントローラーのボタンを割り当てられない件の調査（計画書は `Phase6-Step10-3c-Plan.md`）。
  - **Step10-4**: F08（サービス実装のコンストラクタのフォールバック 12 クラス）と、全域の再走査・分類・ガードテスト。あわせて、`ISpecialActionRepository.RemoveAction`（メモリのみ）の呼び出し元の有無と、Step10-2／10-3 で `ProfileEditor` 本体に残る `AppHost`／`Program.rootHub` の解決（`profileRepository`、`profileSwitcher`、`controlService`、`vmFactory`）の分類。
  - **Step10-5**: §8（`PluginSlot`／`UnplugSlot` の接続）。
  - **完了報告書**: `Phase6-Step10-Completion-Report.md`、`Phase6-Step10b-Plan.md`・`Phase6-Step11-Plan.md` への引き継ぎ（複数型ファイルの分割は Step10b。`PresetOption.cs`、`MappingListViewModel.cs` など）。
- **先送り台帳**: `Phase6-Step11-Plan.md` §3.3（Step10-1-3 の更新通知、Step10-3-2 の SA キー押下のボタン割り当て）。
- **追記（2026-10-15）**: Step10-3c の調査で、SA の「キー押下」のボタンの割り当ては動作していることを確認し、表示の更新と SwitchMode の保存・読込の不具合を是正した（`Phase6-Step10-3c-Plan.md` §5〜6、実機確認済み）。実行時の Toggle の未実装（`Phase6-Step10-3c-Plan.md` §7）は、**Step10-4・10-5 の後に、KBM の Toggle と共通化した形で実装する**ことに決定した（§8。計画書は実施時に作成）。
- **追記（2026-10-15、Step10-4 の完了）**: Step10-4（10-4-0〜10-4-6）が完了した。内容: 小さな置換（`WindowPlacementHelper`、`ProfileList`、`ProfileEntity`）、クラスタ 1（プロファイル系 6 クラスと `ProfileActionProvider`、`AutoProfileHolder` の依存の必須化）、K1 の是正（スロット 5〜8 の拒否、戻り値の破棄）、アクション連鎖の廃止、Fresh-press ガードの全型への適用（`Mapping.cs`）、クラスタ 2（出力スロット系）、クラスタ 3（`Actions/*`。`AutoProfileChecker`／`IMappingActionDispatcher` の削除）、`MainWindow`／`ControlService` の置換、名前変更時のリンクプロファイル・自動プロファイルの旧名の置換、全域の退行ガード（`Step10FourGlobalRatchetGuardTests`）。詳細・決定・実機確認は `Phase6-Step10-4-Plan.md` §8〜34。**残り: Step10-5（§8）、SA の Button の Toggle の実行時の実装（Step10-3c-Plan §7〜8）、完了報告書。**
- **追記（2026-10-15、Step10 の完了）**: Step10-5（出力スロットの抜き差しを `IOutputSlotService` 経由に。画面・UDP）、Step10-3d（ボタン種別 SA の Press／Toggle を Stage3 に一本化）、名前変更時のプロファイル切替 SA の切替先の置換（10-4-5d）が完了した。あわせて、実機確認で見つかった上流由来の不具合（Release モードの比較反転、タップ判定のずれ許容、タップクリックの保持、Left Touch のホバー表示）を、承認のうえ是正した。完了報告書: `Phase6-Step10-Completion-Report.md`。残る作業は、Step10b・Step11 への引き継ぎ（同報告書 §8）。
- **先送り台帳への追加（`Phase6-Step11-Plan.md` §3.3）**: Step10-4-2c（振動・自動停止時間の反映）、Step10-4-2f（スロット 5〜8 の切替・自動プロファイル）。
