# Phase6-Step11 第 1 層: 残存する直接参照の分類台帳

作成日: 2026-10-15  
対象: `DS4Windows` 配下の全 `.cs`（コメントを除く。HEAD は Step10b 完了時点）  
関連: `Phase6-Step11-Plan.md` §0A・§2.2、`DS4WindowsTests/Step10FourGlobalRatchetGuardTests.cs`（基準値）、`scripts/audit/GlobalInventory`（Roslyn の台帳ツール）

---

## 1. 目的と合格基準

- 当初の「`Global` 直接参照ゼロ」のゲートは、薄いシム（`copilot-instructions.md` §2.1）の温存と、Phase7／Step12 の持ち越しがあるため、ゼロにならない。代わりに、次の 3 点を満たすことを、第 1 層の静的解析のゲートとする。
  1. 全域の件数が、基準値（`Global.*` 780、`AppHost.GetService|Services` 48、`Program.rootHub` 25）を超えていない（`Step10FourGlobalRatchetGuardTests` が成功している）。
  2. 残存の全件が、下記のいずれかの分類に属し、温存の根拠がある（未分類が 0）。
  3. 「定数・不変データ・純粋関数」以外で、UI・ViewModel（第 4 層）が `Global` を直接参照していない（下記の分類 H は、名前表・言語・マクロ値の温存項目として明示）。

## 2. 集計（2026-10-15）

- 走査の対象: 77 ファイル（`Global.*` を含む 75 ファイル、`AppHost` のみの 2 ファイルを含む）
- 全体: `Global.*` 780（うち定数・不変データ 312）、`AppHost` 48、`Program.rootHub` 25。
- 分類別（ファイル数／`Global.*`／うち定数／`AppHost`／`rootHub`）
  - A 定数のみ（UI・ViewModel）: 9／37／37／0／0
  - B 正本データ `BackingStore`: 1／182／136／1／0
  - C `Global` 自身: 1／47／4／11／9
  - D サービス実装の薄いシム: 19／320／3／0／0
  - E Composition Root・起動・隣接する画面: 7／39／36／30／14
  - F DTO・マイグレーション・永続化: 15／46／27／0／0
  - G 第 1〜2 層・Actions: 20／94／69／6／2
  - H UI の残り（名前表・言語・マクロ値）: 5／15／0／0／0

## 3. 温存の根拠と、今後の扱い

- **A（定数のみ）**: `RESOURCES_PREFIX`、`ASSEMBLY_RESOURCE_PREFIX`、`TEST_PROFILE_INDEX`、`MAX_DS4_CONTROLLER_COUNT`、`configFileDecimalCulture`、`Clamp` など。実行中に変わらないため、サービス化しても動作の利点がない（Step10 決定 1）。温存。
- **B（`BackingStore`）**: 設定値の正本（SSOT）。各サービスが内部で参照する。Step12 の防壁 1（削除禁止）。`TEST_PROFILE_ITEM_COUNT`／`MAX_DS4_CONTROLLER_COUNT` の定数が大半（136／182）。
- **C（`Global`）**: 静的ファサード。旧静的経路の入口。`Program.rootHub` 9 件と `AppHost` 11 件は、Composition Root への遅延アクセス（`ServiceProviderInstance` など）。Step12（呼び出し元 0 件のシムの削除）と Phase7（`Mapping` のインスタンス化）の対象。
- **D（サービス実装の薄いシム）**: `AppSettingsService`（185）が最大。サービスの契約を実装し、`Global`／`BackingStore` へ委譲する。Phase6 の方針（§2.1: 旧静的経路は、移行が完了し動作確認が取れるまで残し、薄いシムとして温存）。`ScpUtil` の実体の解体は Phase7 以降。
- **E（Composition Root・起動）**: `App.xaml.cs`（Pre-Host。`rootHub` 12）、`ServiceRegistration`、`AppHost`、`MainWindow.xaml.cs`（`AppHost` 17。`ProfileEditor` の生成と、保持済みサービスの解決）。Phase7 または Step12 で、引数による注入へ（K7-1）。
- **F（DTO ほか）**: XML のシリアライザーが使う `exeversion`、`CONFIG_VERSION`、名前表（`getDS4ControlsByName`／`getX360ControlString`）。変わらないデータ・純粋関数。温存。
- **G（第 1〜2 層・Actions）**: `Mapping.cs`（定数 50、`rootHub` 1、`AppHost` 6）、`SyntheticDispatcher`（`outputKBMHandler`／`outputKBMMapping`）、`DS4Device`／`DS4Devices`／`DS4SixAxis`（第 1 層。デバウンス、ムーンライト設定、DS3 の傾き補正）。Phase7（`Mapping` のインスタンス化）の持ち越し。
- **H（UI の残り）**: `LanguagePackViewModel`（`UseLang`、言語パックの定数）、`PressKeyViewModel`／`MappedControl`（`getX360ControlString`。名前表）、`RecordBoxViewModel`（`macroDS4Values`。マクロ値の表）。Step10 決定 1（静的データ表・純粋関数の温存）に該当。

## 4. 運用

- 基準値を超える変更は、`Step10FourGlobalRatchetGuardTests` が失敗する。減らす変更は許可されるため、減らした場合は、基準値を更新して固定する。
- 型の追加・分割では、新しいファイルへ件数を引き継ぐ（`.github/copilot-instructions.md` §3.3）。
- 分類の再集計は、`scripts/audit/GlobalInventory`、またはリポジトリの走査（コメントを除き、`Global.<名前>`、`AppHost.GetService|Services`、`Program.rootHub` を数える）で行う。

## 5. 分類別の台帳（ファイル別）
#### A. 定数・不変データのみ（UI・ViewModel）
- 合計: 9 ファイル、`Global.*` 37（うち定数 37）、`AppHost` 0、`Program.rootHub` 0
  - `DS4Forms/ImageLocationPaths.cs`: Global 6 — RESOURCES_PREFIX×6
  - `DS4Forms/ResourcePaths.cs`: Global 5 — RESOURCES_PREFIX×5
  - `DS4Forms/ViewModels/CompositeDeviceModel.cs`: Global 4 — RESOURCES_PREFIX×4
  - `DS4Forms/ViewModels/MacroStepItem.cs`: Global 6 — RESOURCES_PREFIX×6
  - `DS4Forms/ViewModels/ProfileSettingsViewModel.cs`: Global 1 — ASSEMBLY_RESOURCE_PREFIX×1
  - `DS4Forms/ViewModels/SpecialActions/CheckBatteryViewModel.cs`: Global 1 — configFileDecimalCulture×1
  - `DS4Forms/ViewModels/SpecialActions/LaunchProgramViewModel.cs`: Global 1 — configFileDecimalCulture×1
  - `DS4Forms/ViewModels/SpecialActions/SpecialActionViewModel.cs`: Global 1 — configFileDecimalCulture×1
  - `DS4Forms/ViewModels/TrayIconViewModel.cs`: Global 12 — RESOURCES_PREFIX×12

#### B. 正本データ（BackingStore。防壁 1）
- 合計: 1 ファイル、`Global.*` 182（うち定数 136）、`AppHost` 1、`Program.rootHub` 0
  - `DS4Control/BackingStore.cs`: Global 182、AppHost 1 — TEST_PROFILE_ITEM_COUNT×113, MAX_DS4_CONTROLLER_COUNT×21, activeOutDevType×8, appdatapath×6, ProfileSettingsServiceInstance×4, exeversion×4, exedirpath×3, XML_EXTENSION×3

#### C. Global 自身（静的ファサード。薄いシムの本体）
- 合計: 1 ファイル、`Global.*` 47（うち定数 4）、`AppHost` 11、`Program.rootHub` 9
  - `DS4Control/Global.cs`: Global 47、AppHost 11、rootHub 9 — RESOURCES_PREFIX×4, appdatapath×2, useDInputOnly×2, tempprofilename×2, tempprofileDistance×2, SpecialActionRepositoryInstance×2, ProfileXmlStoreInstance×2, useTempProfile×2

#### D. サービス実装の薄いシム（§2.1。Global の実体へ委譲）
- 合計: 19 ファイル、`Global.*` 320（うち定数 3）、`AppHost` 0、`Program.rootHub` 0
  - `DS4Control/Services/AppearanceSettingsService.cs`: Global 8 — BatteryChanged×2, UseCurrentTheme×2, UseIconChoice×2, iconChoiceResources×1, InvokeBatteryChanged×1
  - `DS4Control/Services/AppNotificationService.cs`: Global 6 — Notifications×4, FlashWhenLate×2
  - `DS4Control/Services/AppSettingsService.cs`: Global 185 — FakeExeName×4, AutoProfileRevertDefaultProfile×3, LogMaxArchiveFiles×3, LogMinLevel×3, MinToTaskbar×3, Notifications×3, ProcessPriority×3, ProfileChangedNotification×3
  - `DS4Control/Services/AutoProfileService.cs`: Global 3 — autoProfileSwitchNotifyChoice×2, CreateAutoProfiles×1
  - `DS4Control/Services/DefaultElevatedProcessLauncher.cs`: Global 1 — exelocation×1
  - `DS4Control/Services/DeviceStateService.cs`: Global 4 — IsFirstConnection×1, MarkConnected×1, OnDeviceSerialChange×1, ResetConnectionFlags×1
  - `DS4Control/Services/Ds4DeviceRegistryAdapter.cs`: Global 1 — IsHidHideInstalled×1
  - `DS4Control/Services/EnvironmentService.cs`: Global 21 — OLD_XINPUT_CONTROLLER_COUNT×2, AdminNeeded×1, RefreshHidHideInfo×1, RefreshFakerInputInfo×1, MAX_DS4_CONTROLLER_COUNT×1, IsWin8OrGreater×1, IsWin10OrGreater×1, IsViGEmBusInstalled×1
  - `DS4Control/Services/OutputKBMHandlerAdapter.cs`: Global 21 — outputKBMHandler×21
  - `DS4Control/Services/OutputKBMHandlerLifecycle.cs`: Global 8 — outputKBMHandler×6, InitOutputKBMMapping×1, InitOutputKBMHandler×1
  - `DS4Control/Services/OutputSlotService.cs`: Global 2 — activeOutDevType×1, outDevTypeTemp×1
  - `DS4Control/Services/PathService.cs`: Global 8 — appdatapath×2, appDataPpath×1, exedirpath×1, exelocation×1, multisavespots×1, SaveDefault×1, SaveWhere×1
  - `DS4Control/Services/ProfileApplicationService.cs`: Global 3 — ApplyProfile×3
  - `DS4Control/Services/ProfilePresetLoader.cs`: Global 10 — LoadBlankDevProfile×1, LoadBlankDS4Profile×1, LoadDefaultDS4GamepadGyroProfile×1, LoadDefaultDS4MixedControlsProfile×1, LoadDefaultDS4MixedGyroMouseProfile×1, LoadDefaultGamepadGyroProfile×1, LoadDefaultKBMGyroMouseProfile×1, LoadDefaultKBMProfile×1
  - `DS4Control/Services/ProfileRepository.cs`: Global 26 — store×5, SaveLinkedProfiles×2, LinkedProfileUI×2, SelectedProfileChanged×2, SelectedProfile×1, removeLinkedProfile×1, RaiseSelectedProfileChanged×1, ProfilePath×1
  - `DS4Control/Services/ProfileSettingsService.cs`: Global 5 — reverseX360ButtonMapping×2, defaultButtonMapping×1, RefreshActionAlias×1, RefreshExtrasButtons×1
  - `DS4Control/Services/ProfileSlotApplier.cs`: Global 1 — ApplyProfileToSlot×1
  - `DS4Control/Services/ProfileXmlStore.cs`: Global 3 — LoadControllerConfigs×1, SaveControllerConfigs×1, store×1
  - `DS4Control/Services/SpecialActionRepository.cs`: Global 4 — CreateStdActions×1, RemoveAction×1, SaveAction×1, SaveActions×1

#### E. Composition Root・起動・隣接する画面（AppHost／Program.rootHub）
- 合計: 7 ファイル、`Global.*` 39（うち定数 36）、`AppHost` 30、`Program.rootHub` 14
  - `App.xaml.cs`: Global 1、AppHost 4、rootHub 12 — MAX_DS4_CONTROLLER_COUNT×1
  - `DI/AppHost.cs`: AppHost 2
  - `DI/Compat/AppHost.cs`: AppHost 2
  - `DI/ServiceRegistration.cs`: Global 3、rootHub 1 — store×3
  - `DS4Control/ControlService.cs`: Global 2、AppHost 1 — MAX_DS4_CONTROLLER_COUNT×1, TEST_PROFILE_INDEX×1
  - `DS4Forms/MainWindow.xaml.cs`: Global 1、AppHost 17 — TEST_PROFILE_INDEX×1
  - `DS4Forms/ProfileEditor.xaml.cs`: Global 32、AppHost 4、rootHub 1 — ASSEMBLY_RESOURCE_PREFIX×29, TEST_PROFILE_INDEX×3

#### F. DTO・マイグレーション・永続化（設定値・バージョン・名前表）
- 合計: 15 ファイル、`Global.*` 46（うち定数 27）、`AppHost` 0、`Program.rootHub` 0
  - `DS4Control/Changelog.cs`: Global 4 — exeversion×2, LastVersionChecked×2
  - `DS4Control/DTOXml/ActionsDTO.cs`: Global 4 — configFileDecimalCulture×4
  - `DS4Control/DTOXml/AppSettingsDTO.cs`: Global 16 — MAX_DS4_CONTROLLER_COUNT×14, exeversion×1, APP_CONFIG_VERSION×1
  - `DS4Control/DTOXml/AutoProfileEntrySerializer.cs`: Global 4 — MAX_DS4_CONTROLLER_COUNT×4
  - `DS4Control/DTOXml/DS4ControlButtonAssignmentSerializer.cs`: Global 3 — getDS4ControlsByName×1, getX360ControlsByName×1, getX360ControlString×1
  - `DS4Control/DTOXml/DS4ControlExtrasAssignmentSerializer.cs`: Global 1 — getDS4ControlsByName×1
  - `DS4Control/DTOXml/DS4ControlKeyAssignmentSerializer.cs`: Global 1 — getDS4ControlsByName×1
  - `DS4Control/DTOXml/DS4ControlKeyTypeAssignmentSerializer.cs`: Global 1 — getDS4ControlsByName×1
  - `DS4Control/DTOXml/DS4ControlLightbarMacroAssignmentSerializer.cs`: Global 1 — getDS4ControlsByName×1
  - `DS4Control/DTOXml/DS4ControlMacroAssignmentSerializer.cs`: Global 1 — getDS4ControlsByName×1
  - `DS4Control/DTOXml/OutputSlotPersistDTO.cs`: Global 1 — exeversion×1
  - `DS4Control/DTOXml/ProfileDTO.cs`: Global 2 — CONFIG_VERSION×1, exeversion×1
  - `DS4Control/DTOXml/SpecialActionSerializer.cs`: Global 2 — configFileDecimalCulture×2
  - `DS4Control/OutputSlotPersist.cs`: Global 1 — exeversion×1
  - `DS4Control/ProfileMigration.cs`: Global 4 — exeversion×3, CONFIG_VERSION×1

#### G. 第 1〜2 層・Actions（定数、Phase7／Step12 の持ち越し）
- 合計: 20 ファイル、`Global.*` 94（うち定数 69）、`AppHost` 6、`Program.rootHub` 2
  - `Actions/ActionEntry.cs`: Global 1 — MAX_DS4_CONTROLLER_COUNT×1
  - `Actions/DefaultActionManager.cs`: Global 1 — MAX_DS4_CONTROLLER_COUNT×1
  - `Actions/DefaultMacroPlayer.cs`: Global 1 — MAX_DS4_CONTROLLER_COUNT×1
  - `BezierCurveEditor/BezierCurve.cs`: Global 1 — Clamp×1
  - `DS4Control/AppLogger.cs`: Global 1 — NotificationServiceInstance×1
  - `DS4Control/DS4LightBar.cs`: Global 7 — MAX_DS4_CONTROLLER_COUNT×7
  - `DS4Control/DS4OutDevices/DS4OutDeviceBasic.cs`: Global 1 — GetSASteeringWheelEmulationAxis×1
  - `DS4Control/DS4OutDevices/DS4OutDeviceExt.cs`: Global 1 — GetSASteeringWheelEmulationAxis×1
  - `DS4Control/Mapping.cs`: Global 53、AppHost 6、rootHub 1 — MAX_DS4_CONTROLLER_COUNT×24, Clamp×23, TEST_PROFILE_ITEM_COUNT×3, ApplyProfile×1, outputKBMHandler×1, ProfileSettingsServiceInstance×1
  - `DS4Control/Mouse.cs`: Global 2 — Clamp×2
  - `DS4Control/MouseCursor.cs`: Global 2 — Clamp×2
  - `DS4Control/OutputKBM/VirtualKBMFactory.cs`: Global 1 — fakerInputInstalled×1
  - `DS4Control/SyntheticDispatcher.cs`: Global 4 — outputKBMHandler×2, outputKBMMapping×2
  - `DS4Control/Util.cs`: Global 1 — IsAdministrator×1
  - `DS4Control/Xbox360OutDevice.cs`: Global 1 — GetSASteeringWheelEmulationAxis×1
  - `DS4Library/DS4Device.cs`: Global 3 — DebouncingMs×2, DebouncingMsChanged×1
  - `DS4Library/DS4Devices.cs`: Global 6 — CheckIfVirtualDevice×2, GetInstanceIdFromDevicePath×2, UseAdvancedMoonlight×1, UseMoonlight×1
  - `DS4Library/DS4SixAxis.cs`: Global 1、rootHub 1 — UseDs3PitchRollSim×1
  - `DS4Library/DS4State.cs`: Global 4 — Clamp×4
  - `HidLibrary/HidDevices.cs`: Global 2 — DeviceOptions×2

#### H. UI の残り（名前表・言語・マクロ値）
- 合計: 5 ファイル、`Global.*` 15（うち定数 0）、`AppHost` 0、`Program.rootHub` 0
  - `DS4Forms/LanguageSelectDialog.xaml.cs`: Global 1 — UseLang×1
  - `DS4Forms/ViewModels/LanguagePackViewModel.cs`: Global 3 — LANGUAGE_ASSEMBLY_NAME×1, PROBING_PATH×1, UseLang×1
  - `DS4Forms/ViewModels/MappedControl.cs`: Global 3 — getX360ControlString×3
  - `DS4Forms/ViewModels/RecordBoxViewModel.cs`: Global 1 — macroDS4Values×1
  - `DS4Forms/ViewModels/SpecialActions/PressKeyViewModel.cs`: Global 7 — getX360ControlString×7

