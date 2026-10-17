# フェーズ6 Step10b 計画書: 1ファイル1型（原則1）の全数是正

作成日: 2026-09-20  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`（Step10 と Step11 の間に新設）  
関連: `Phase6-Step13-Plan.md`（配置統一。本 Step の**前**に完了させる）、`.github/copilot-instructions.md` §3.3 原則1・原則2  
実地確認の基準: HEAD `949c957`（2026-09-20）。Step13 の 13-1〜13-5 は未実施の状態


> **Step9b（PR #1 の Copilot レビュー）からの申し送り（2026-10-14 追記）**
> - Copilot が、本 Step の台帳に既にある次の 2 ファイルを、1ファイル1型（`copilot-instructions.md` §3.3 原則1）の違反として指摘した（Low）。**F12**: `DS4Windows/DI/INotificationService.cs`（`NotificationEventArgs` と `INotificationService`。`NotificationEventArgs.cs` へ分割）、**F13**: `DS4Windows/DS4Control/Trigger.cs`（`TriggerContext`、`TriggerType`、`ITrigger`、`SingleButtonTrigger`。ファイル名がどの型名とも不一致）。いずれも本 Step の付録 B の分割対象（Step9b では変更しない）。
> - 詳細は `Phase6-Step9b-Plan.md` §1。

> **Step10（完了。2026-10-15）からの申し送り**
> - Step10 で作成した新規ファイル（`ButtonSpecialActionOutput.cs`、各テストファイル）は 1 ファイル 1 型。既存の複数型ファイルは Step10 では分割していない（過渡期ルール）。
> - Step10 で改修した複数型ファイル（`ScpUtil.cs`、`Mapping.cs`、`Mouse.cs` ほか）に、新しい型は追加していない（定数・メソッドのみ）。
> - 型の分割を、機能変更を含む PR に混ぜないこと。
> - 分割で新設するファイルは、`Step10FourGlobalRatchetGuardTests` の基準値に無いファイルとして扱われる。**分割元の件数を新ファイルへ引き継いで、基準値を更新する**こと（件数の合計が増えないこと。`Phase6-Step10-4-Plan.md` §34・§36）。
> - `Mouse.cs`・`ProfileEditor.xaml.cs` は、Step10 の最後に実機確認済みの状態（`Phase6-Step10-Completion-Report.md` §4 の 9〜12）。

## 0A. Step10b-0 台帳（2026-10-15 作成。現行コードの走査による再集計。付録 A（2026-09-20 版）は参考扱いとし、本節を正とする）

### 0A.1 走査の方法と結果
- **方法**: DS4Windows・DS4WindowsTests・StandaloneTests の全 .cs（obj／in／自動生成を除く）について、コメント・文字列を除去し、波括弧の深さで、トップレベル型（class／struct／interface／enum／delegate）を数えた。ネスト型は数えない。走査スクリプトは作業用（リポジトリには含めない）。
- **結果**: 型を持つファイル 423、トップレベル型 619。**複数型を含むファイルは 55**（DS4Windows 52、DS4WindowsTests 3）。分割で新設するファイルは **196**（各ファイルの型数 − 1 の合計）。
- **付録 A（2026-09-20 版: 55 ファイル・新設 203）との差**
  - 複数型でなくなった: SettingsViewModel.cs（MonitorChoiceListing を Step9 で削除。1 型）。
  - 新たに複数型になった（過渡期ルールの逸脱）: DS4WindowsTests/Step10FourTwoGlobalReferenceGuardTests.cs（Step10-4 で NullProcessInspector を同居。もとは 1 型）。W1 で分割する。
  - 型数が減った: ControllerRegDeviceOptsViewModel.cs 7→6、ControlServiceDeviceOptions.cs 13→11（Vader 4 Pro の削除）、ProfileDTO.cs 22→20、ProfilePropGroups.cs 28→26（Abs Mouse の削除）。
- **ファイル名と一致する型が無い複数型ファイル（元ファイルを全型の移動後に削除、または最大の型を git mv で改名）**: TestActionControllerTests.cs、ToggleAndPressedOnceTests.cs、ReaderWriteLockSlimWrappers.cs、Log.cs、ProfilePropGroups.cs、ScpUtil.cs、Trigger.cs、DS4Sixaxis.cs。
- **単一型だがファイル名が型名と異なる（原則2。改名のみ）**: ApiDTO/GitHubRelease.cs（GithubRelease）、DS4Forms/ViewModels/Util/EventHandlers.cs（PropertyChangingHandler）、DS4Library/Crc32.cs（Crc32Algorithm）、DS4WindowsTests/Specialactionslistviewmodelsorttests.cs（SpecialActionsListViewModelSortTests。大文字・小文字のみの差で、今回の走査で追加）。
- **新設ファイル名の衝突**: 1 件（DS4Library/DS4SixAxis.cs。既存 DS4Sixaxis.cs と、大文字・小文字のみの差。Windows の既定のファイルシステムでは同一ファイルになるため、git mv で DS4Sixaxis.cs を DS4SixAxis.cs へ改名する）。同一フォルダ内の新設ファイル同士の重複: 0 件。
- **特殊ケース（§7-1）**: DI/AppHost.cs の DS4Windows.AppHost（互換ラッパー）は、DS4Windows 名前空間内の約 12 ファイルが、修飾なしの AppHost.GetService で使用している（Mapping.cs、ControlService.cs、ScpUtil.cs、MainWindow.xaml.cs、ProfileEditor.xaml.cs ほか）。呼び出し元が 0 件ではないため、(b)（別フォルダ DI/Compat/AppHost.cs。名前空間は変更しない）を採る。
- **ソース走査テストへの影響**: 分割対象のファイル名を参照するテストが約 34 ファイル（MainWindow.xaml.cs 7、ProfileEditor.xaml.cs 8、ProfileSettingsViewModel.cs 4、ScpUtil.cs 2 ほか）。型を別ファイルへ移すと、型の文字列を元のファイルから探すテストが失敗する。各ウェーブで、参照するテストを洗い出し、検査先を更新する。Step10FourGlobalRatchetGuardTests は、ファイルごとの基準値のため、新設ファイルへ件数を引き継ぐ（合計が変わらないことを確認する）。

### 0A.2 現行の全数台帳（ウェーブ別。パスは DS4Windows／DS4WindowsTests 直下からの相対）
#### W1（20 ファイル・新設 96 ファイル）
- `DS4Windows/Core/Utilities/OneEuroFilter.cs`: 2 型・106 行 — OneEuroFilter、LowpassFilter
- `DS4Windows/Core/Utilities/ReaderWriteLockSlimWrappers.cs`: 2 型・63 行（ファイル名と一致する型なし） — ReadLocker、WriteLocker
- `DS4Windows/DS4Control/ControlServiceDeviceOptions.cs`: 11 型・561 行 — ControlServiceDeviceOptions、ControllerOptionsStore、DS4DeviceOptions、DS3DeviceOptions、DS4ControllerOptions、DualSenseDeviceOptions、DualSenseControllerOptions、SwitchProDeviceOptions、SwitchProControllerOptions、JoyConDeviceOptions、JoyConControllerOptions
- `DS4Windows/DS4Control/DS4OutDevices/DS4OutDeviceExtras.cs`: 5 型・164 行 — DS4_TOUCH、DS4_REPORT_UNION、DS4_REPORT_EX、DS4OutputBufferData、DS4OutDeviceExtras
- `DS4Windows/DS4Control/DTOXml/ActionsDTO.cs`: 2 型・360 行 — ActionsDTO、SpecialActionSerializer
- `DS4Windows/DS4Control/DTOXml/AppSettingsDTO.cs`: 9 型・1235 行 — AppSettingsDTO、UDPSrvSmoothingOptionsGroup、InputDeviceOptions、BaseInputDeviceSettingsGroup、DS4SupportSettingsGroup、DS3SupportSettings、DualSenseSupportSettings、SwitchProSupportSettings、JoyConSupportSettings
- `DS4Windows/DS4Control/DTOXml/AutoProfilesDTO.cs`: 2 型・190 行 — AutoProfilesDTO、AutoProfileEntrySerializer
- `DS4Windows/DS4Control/DTOXml/LinkedProfilesDTO.cs`: 2 型・123 行 — LinkedProfilesDTO、LinkedProfileItem
- `DS4Windows/DS4Control/DTOXml/OutputSlotPersistDTO.cs`: 2 型・101 行 — OutputSlotPersistDTO、OutputSlotSerializer
- `DS4Windows/DS4Control/DTOXml/ProfileDTO.cs`: 20 型・3713 行 — ProfileDTO、StickAxialDeadOptionsSerializer、StickDeltaAccelSettings、SASteeringWheelSmoothingOptions、GyroControlsSettings、GyroMouseStickSmoothingSettings、GyroMouseSmoothingSettings、GyroSwipeSettings、StickModeOutputSettings、FlickStickSettings、DualSenseControllerSettings、TouchpadMouseStickSerializer、DS4ControlAssignementSerializer、DS4ControlAssignmentSerializerBase、DS4ControlButtonAssignmentSerializer、DS4ControlKeyAssignmentSerializer、DS4ControlMacroAssignmentSerializer、DS4ControlKeyTypeAssignmentSerializer、DS4ControlExtrasAssignmentSerializer、DS4ControlLightbarMacroAssignmentSerializer
- `DS4Windows/DS4Control/DTOXml/XmlDataUtilities.cs`: 2 型・46 行 — XmlDataUtilities、Utf8StringWriter
- `DS4Windows/DS4Control/MacroParser.cs`: 2 型・286 行 — MacroParser、MacroStep
- `DS4Windows/DS4Control/PresetOption.cs`: 7 型・190 行 — PresetOption、GamepadPreset、GamepadGyroCamera、MixedPreset、MixedGyroMousePreset、KBMPreset、KBMGyroMouse
- `DS4Windows/DS4Control/ProfilePropGroups.cs`: 26 型・1475 行（ファイル名と一致する型なし） — ProfileActions、Sensitivity、SquareStickInfo、StickDeadZoneInfo、StickAntiSnapbackInfo、TriggerDeadZoneZInfo、GyroMouseInfo、GyroMouseStickInfo、GyroDirectionalSwipeInfo、GyroControlsInfo、ButtonMouseInfo、LightbarMode、LightbarDS4WinInfo、LightbarSettingInfo、SteeringWheelSmoothingInfo、TouchpadRelMouseSettings、TouchMouseStickInfo、StickMode、TriggerMode、TwoStageTriggerMode、FlickStickSettings、StickControlSettings、StickModeSettings、StickOutputSetting、TriggerOutputSettings、RumbleSettings
- `DS4Windows/DS4Control/Profiles/AutoProfileHolder.cs`: 2 型・251 行 — AutoProfileHolder、AutoProfileEntity
- `DS4Windows/DS4Control/Trigger.cs`: 4 型・55 行（ファイル名と一致する型なし） — TriggerContext、TriggerType、ITrigger、SingleButtonTrigger
- `DS4Windows/VJoyFeeder/vJoyFeeder.cs`: 8 型・720 行 — HID_USAGES、VjdStat、FFBPType、FFBEType、FFB_CTRL、FFBOP、VJoy、vJoyFeeder
- `DS4WindowsTests/ActionControllerTests/TestActionControllerTests.cs`: 3 型・105 行（ファイル名と一致する型なし） — MockRepeater、TestActionController、ActionControllerTests
- `DS4WindowsTests/ActionControllerTests/ToggleAndPressedOnceTests.cs`: 3 型・326 行（ファイル名と一致する型なし） — TestServiceProvider、FakeKBMHandler、ToggleAndToggledOnTests
- `DS4WindowsTests/Step10FourTwoGlobalReferenceGuardTests.cs`: 2 型・442 行 — NullProcessInspector、Step10FourTwoGlobalReferenceGuardTests

#### W2（5 ファイル・新設 5 ファイル）
- `DS4Windows/DI/AppHost.cs`: 2 型・212 行 — AppHost、AppHost
- `DS4Windows/DI/IDeviceStateService.cs`: 2 型・44 行 — DeviceStateChangedEventArgs、IDeviceStateService
- `DS4Windows/DI/INotificationService.cs`: 2 型・32 行 — NotificationEventArgs、INotificationService
- `DS4Windows/DI/IOutputSlotService.cs`: 2 型・72 行 — OutputSlotChangedEventArgs、IOutputSlotService
- `DS4Windows/DI/IProfileSettingsService.cs`: 2 型・283 行 — ProfileSettingChangedEventArgs、IProfileSettingsService

#### W3（19 ファイル・新設 32 ファイル）
- `DS4Windows/DS4Forms/BindingWindow.xaml.cs`: 3 型・1021 行 — BindingWindow、BindingWinResourcePaths、IntToBoolConverter
- `DS4Windows/DS4Forms/LanguageSelectDialog.xaml.cs`: 2 型・152 行 — LanguageInfo、LanguageSelectDialog
- `DS4Windows/DS4Forms/MainWindow.xaml.cs`: 2 型・2280 行 — MainWindow、ImageLocationPaths
- `DS4Windows/DS4Forms/ProfileEditor.xaml.cs`: 3 型・2552 行 — ProfileEditor、ResourcePaths、ControlIndexCheck
- `DS4Windows/DS4Forms/RecordBox.xaml.cs`: 2 型・653 行 — RecordBox、RecordBoxResourcePaths
- `DS4Windows/DS4Forms/StickCalibrationWindow.xaml.cs`: 2 型・68 行 — StickCalibrationWindow、Stick
- `DS4Windows/DS4Forms/ViewModels/AutoProfilesViewModel.cs`: 3 型・723 行 — AutoProfilesViewModel、ProgramItem、NativeMethods2
- `DS4Windows/DS4Forms/ViewModels/BindingWindowViewModel.cs`: 4 型・953 行 — BindingWindowViewModel、BindAssociation、OutBinding、LightbarMacro
- `DS4Windows/DS4Forms/ViewModels/ControllerListViewModel.cs`: 2 型・744 行 — ControllerListViewModel、CompositeDeviceModel
- `DS4Windows/DS4Forms/ViewModels/ControllerRegDeviceOptsViewModel.cs`: 6 型・357 行 — ControllerRegDeviceOptsViewModel、DeviceListItem、DS4ControllerOptionsWrapper、DualSenseControllerOptionsWrapper、SwitchProControllerOptionsWrapper、JoyConControllerOptionsWrapper
- `DS4Windows/DS4Forms/ViewModels/CurrentOutDeviceViewModel.cs`: 2 型・493 行 — CurrentOutDeviceViewModel、SlotDeviceEntry
- `DS4Windows/DS4Forms/ViewModels/LanguagePackViewModel.cs`: 2 型・145 行 — LanguagePackViewModel、LangPackItem
- `DS4Windows/DS4Forms/ViewModels/LightbarMacroViewModel.cs`: 3 型・92 行 — LightbarMacroViewModel、LightbarMacroElement、LightbarMacroTrigger
- `DS4Windows/DS4Forms/ViewModels/MappingListViewModel.cs`: 2 型・367 行 — MappingListViewModel、MappedControl
- `DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs`: 5 型・4174 行 — ProfileSettingsViewModel、PresetMenuHelper、TriggerModeChoice、TwoStageChoice、TriggerEffectChoice
- `DS4Windows/DS4Forms/ViewModels/RecordBoxViewModel.cs`: 2 型・588 行 — RecordBoxViewModel、MacroStepItem
- `DS4Windows/DS4Forms/ViewModels/SpecialActionsListViewModel.cs`: 2 型・531 行 — SpecialActionsListViewModel、SpecialActionItem
- `DS4Windows/DS4Forms/ViewModels/TrayIconViewModel.cs`: 2 型・504 行 — TrayIconViewModel、ControllerHolder
- `DS4Windows/DS4Forms/WelcomeDialog.xaml.cs`: 2 型・525 行 — WelcomeDialog、WelcomeDialogResourcePaths

#### W4（10 ファイル・新設 35 ファイル）
- `DS4Windows/Actions/Action.cs`: 2 型・29 行 — MappingContext、Action
- `DS4Windows/Actions/ActionManager.cs`: 3 型・618 行 — ActionInstanceState、ActionEntry、ActionManager
- `DS4Windows/DS4Control/Log.cs`: 3 型・155 行（ファイル名と一致する型なし） — AppLogger、ProfileChangeSource、ProfileChangedEventArgs
- `DS4Windows/DS4Control/UdpServer.cs`: 7 型・1051 行 — DsState、DsConnection、DsModel、DsBattery、DualShockPadMeta、PadDataRspPacket、UdpServer
- `DS4Windows/DS4Library/DS4Device.cs`: 6 型・2170 行 — DS4Color、ConnectionType、DS4ForceFeedbackState、DS4LightbarState、DS4HapticState、DS4Device
- `DS4Windows/DS4Library/DS4Devices.cs`: 10 型・622 行 — VidPidFeatureSet、VidPidInfo、RequestElevationArgs、RequestElevationDelegate、CheckVirtualInfo、CheckVirtualDelegate、CheckConnectionDelegate、PrepareInitDelegate、CheckPendingDevice、DS4Devices
- `DS4Windows/DS4Library/DS4Sixaxis.cs`: 6 型・649 行（ファイル名と一致する型なし） — SixAxisHandler、SixAxisEventArgs、SixAxis、CalibData、GyroAverageWindow、DS4SixAxis
- `DS4Windows/DS4Library/DS4Touchpad.cs`: 3 型・323 行 — TouchpadEventArgs、Touch、DS4Touchpad
- `DS4Windows/DS4Library/InputDevices/InputDeviceFactory.cs`: 2 型・70 行 — InputDeviceType、InputDeviceFactory
- `DS4Windows/DS4Library/InputDevices/TriggerEffects.cs`: 3 型・49 行 — TriggerEffects、TriggerId、TriggerEffectSettings

#### W5（1 ファイル・新設 28 ファイル）
- `DS4Windows/DS4Control/ScpUtil.cs`: 29 型・11194 行（ファイル名と一致する型なし） — DS4KeyType、Ds3PadId、DS4Controls、X360Controls、SASteeringWheelEmulationAxisType、OutContType、GyroOutMode、TouchpadOutMode、TrayIconChoice、AppThemeChoice、ControlActionData、AutoProfileDisplayProfileSwitchChoices、DS4TriggerOutputMode、DS4ControlSettings、ControlSettingsGroup、DebugEventArgs、MappingDoneEventArgs、ReportEventArgs、BatteryReportArgs、ControllerRemovedArgs、DeviceStatusChangeEventArgs、SerialChangeArgs、OneEuroFilterPair、OneEuroFilter3D、SelectedProfileChangedEventArgs、Global、Changelog、BackingStore、SpecialAction




### 0A.3 決定事項（2026-10-15、決定 1〜6 すべて推奨案で承認）
- **決定1（提供方法・実施者）**: A（推奨）＝分割ツールを PowerShell で作り、**エージェントが手元で実行**する（ファイルの読み書きのみ。ビルド・テストはユーザーが実行）。ツールは、(1) 宣言テキスト（トリビア込み）の一致、(2) 型インベントリの一致、(3) 1 ファイル 1 型・命名の一致を、ウェーブごとにレポートする。B＝Roslyn のコンソールアプリを作り、ユーザーが実行（NuGet の取得と、意味診断まで確認できる。ただしユーザーの作業が増える）。C＝手作業で 1 ファイルずつ分割（203 ファイルは現実的でない）。
- **決定2（ウェーブの順序と単位）**: W1（データ型・補助型）→ W2（DI 層）→ W3（UI 層）→ W4（デバイス・コア層）→ W5（ScpUtil.cs）。1 ウェーブ＝1 回のビルド・テスト・コミット。W5 は独立。
- **決定3（AppHost）**: 互換ラッパーを DI/Compat/AppHost.cs（名前空間は DS4Windows のまま）へ分ける（案 b）。
- **決定4（大文字・小文字のみの改名）**: git mv を 2 段階（一時名を経由）で行う。DS4Sixaxis.cs→DS4SixAxis.cs、Specialactionslistviewmodelsorttests.cs→SpecialActionsListViewModelSortTests.cs、GitHubRelease.cs→GithubRelease.cs。付録 B の他 2 件（EventHandlers.cs、Crc32.cs）も改名。
- **決定5（W1 の追加）**: Step10FourTwoGlobalReferenceGuardTests.cs の NullProcessInspector を、W1 で別ファイルへ分ける。
- **決定6（ガードテストの扱い）**: Step10FourGlobalRatchetGuardTests の基準値は、分割後の各新設ファイルの件数へ機械的に置き換える（ツールが出力）。件数の合計が変わらないことを、ウェーブごとに確認する。

### 0A.4 分割ツール（2026-10-15 作成）
- 場所: `docs-forDIMG/MadeByAgent/tools/Step10b/`（`README.md` に手順）。PowerShell 7 に同梱の Roslyn で、型の宣言（属性・コメント・`#if` を含む前後のトリビア）を一字一句そのまま別ファイルへ移す。ビルドは不要。
- 実行ごとの自己検証: 出力ファイルを再読み込みし、(1) トップレベル型が 1 つ、(2) 宣言テキストが分割前と一致、(3) 構文エラー 0、(4) BOM が一致、を確認する。分割前後の型名の一覧を走査スクリプトで比較する。
- 元ファイルの扱い: ファイル名と一致する型があれば、それを元ファイルに残す（履歴を維持）。無ければ元ファイルを削除し、全型を別名のファイルへ書く（最大の型は、内容の類似により git が改名として検出する見込み）。

### 0A.5 進捗
- **W1（2026-10-15 適用済み。ビルド・テストの結果待ち）**: 20 ファイルを分割し、新設 96 ファイル（元ファイルの削除 5 件を含む出力 101 ファイル）。型名の一覧は分割前後で一致（618＝618）、複数型ファイルは 55 → 35。
  - 新設・変更の内訳: `OneEuroFilter`、`ReaderWriteLockSlimWrappers`（`ReadLocker`／`WriteLocker`）、`ControlServiceDeviceOptions`、`DS4OutDeviceExtras`、DTOXml（`ActionsDTO`、`AppSettingsDTO`、`AutoProfilesDTO`、`LinkedProfilesDTO`、`OutputSlotPersistDTO`、`ProfileDTO`、`XmlDataUtilities`）、`MacroParser`、`PresetOption`、`ProfilePropGroups`（元ファイルを削除。26 型）、`AutoProfileHolder`、`Trigger`（元ファイルを削除。4 型）、`vJoyFeeder`、テスト 3 ファイル（`NullProcessInspector` ほか）。
  - ラチェットの基準値: `ActionsDTO` 6＝4＋2（`SpecialActionSerializer`）、`AutoProfilesDTO` 4＝0＋4（`AutoProfileEntrySerializer`）、`ProfileDTO` 10＝2＋8（`DS4Control*Serializer` ほか）へ引き継いだ。合計は不変。
  - **W1 の結果（2026-10-15）**: ビルド・テストビルド・テスト実行成功。
- **W2（2026-10-15 適用済み。ビルド・テストの結果待ち）**: 5 ファイルを分割し、新設 5 ファイル。複数型ファイルは 35 → 30。型名の一覧は一致（618＝618）。
  - `DI/AppHost.cs`: 互換ラッパー `DS4Windows.AppHost` を `DI/Compat/AppHost.cs`（名前空間は `DS4Windows` のまま）へ分離。元ファイルは `DS4WinWPF.AppHost` のみ。ツールでは扱えない（名前空間が複数）ため、テキストの切り出しで実施（宣言は一字一句同じ）。
  - `IDeviceStateService`／`INotificationService`／`IOutputSlotService`／`IProfileSettingsService`: イベント引数クラス（`DeviceStateChangedEventArgs`、`NotificationEventArgs`、`OutputSlotChangedEventArgs`、`ProfileSettingChangedEventArgs`）を別ファイルへ。
  - ラチェットの基準値: `DI/AppHost.cs` 4＝2＋`DI/Compat/AppHost.cs` 2（合計は不変。`Step10FourGlobalRatchetGuardTests` の失敗を受けて更新）。
  - ソース走査テストの影響: W2 の対象を参照するテストは無い。
  - **W2 の結果（2026-10-15）**: ビルド・テストビルド・テスト実行成功。
- **W3（2026-10-15 適用済み。ビルド・テストの結果待ち）**: 19 ファイルを分割し、新設 32 ファイル。複数型ファイルは 30 → 11。型名の一覧は一致（618＝618）。ツールの自己検証は全件 `VERIFY-OK`。
  - 対象: `BindingWindow.xaml.cs`、`LanguageSelectDialog.xaml.cs`、`MainWindow.xaml.cs`、`ProfileEditor.xaml.cs`、`RecordBox.xaml.cs`、`StickCalibrationWindow.xaml.cs`、`WelcomeDialog.xaml.cs`、ViewModels 12 ファイル。`.xaml.cs` は、`partial class` を元ファイルに残し、同居していた別の型だけを新設ファイルへ移した（XAML は変更なし）。
  - ラチェットの基準値: `MainWindow` 7＝1＋`ImageLocationPaths` 6、`ProfileEditor` 37＝32＋`ResourcePaths` 5、`ControllerListViewModel` 4＝0＋`CompositeDeviceModel` 4、`MappingListViewModel` 3＝0＋`MappedControl` 3、`RecordBoxViewModel` 7＝1＋`MacroStepItem` 6（合計は不変）。
  - ソース走査テストの影響: `ProfileSettingsViewModelGlobalReferenceGuardTests` が、`PresetMenuHelper` のコンストラクターを `ProfileSettingsViewModel.cs` から探していたため、`PresetMenuHelper.cs` を読むよう更新。あわせて、分離した型（`PresetMenuHelper`、各 `*Choice`）にも、定数以外の `Global`／`AppHost`／`Program.rootHub` が無いことの検査を広げた。他のガードテストは「許可リスト以外が無い」検査のみのため変更不要。
  - **W3 の結果（2026-10-15）**: ビルド・テストビルド・テスト実行成功、起動スモーク問題なし。
- **W4（2026-10-15 適用済み。ビルド・テストの結果待ち）**: 10 ファイルを分割し、新設 35 ファイル（`Log.cs`・`DS4Sixaxis.cs` は元ファイルを削除して全型を別名へ）。複数型ファイルは 11 → 1（残りは `ScpUtil.cs` のみ）。型名の一覧は一致（618＝618）。ツールの自己検証は全件 `VERIFY-OK`。
  - 対象: `Actions/Action.cs`（`MappingContext`）、`Actions/ActionManager.cs`（`ActionInstanceState`、`ActionEntry`）、`DS4Control/Log.cs`（→ `AppLogger.cs`、`ProfileChangeSource.cs`、`ProfileChangedEventArgs.cs`）、`UdpServer.cs`（6 型）、`DS4Device.cs`（5 型）、`DS4Devices.cs`（9 型。デリゲート 4 件を含む）、`DS4Sixaxis.cs`（→ `DS4SixAxis.cs` ほか 6 ファイル）、`DS4Touchpad.cs`、`InputDeviceFactory.cs`、`TriggerEffects.cs`。
  - ラチェットの基準値: `ActionManager.cs` の 1 件は `ActionEntry.cs` へ、`Log.cs` の 1 件は `AppLogger.cs` へ、`DS4Sixaxis.cs` の (1,0,1) は `DS4SixAxis.cs` へ（`Dictionary` が大文字・小文字を区別するためキーを更新）。合計は不変。
  - **大文字・小文字のみの改名**（`DS4Sixaxis.cs` → `DS4SixAxis.cs`）: `core.ignorecase=true` のため、通常の `git add` では、インデックスの名前が旧名のまま更新される。コミット前に、次を実行すること: `git rm --cached DS4Windows/DS4Library/DS4Sixaxis.cs` → `git add DS4Windows/DS4Library/DS4SixAxis.cs`。
  - ソース走査テストの影響: W4 の対象のファイル名を参照するテストは、ラチェットの基準値と、`Step10FourTwoGlobalReferenceGuardTests` の許可リスト（`ActionManager.cs`。不在の検査のみのため変更不要）だけ。
  - **W4 の結果（2026-10-15）**: ビルド・テストビルド・テスト実行成功、起動スモーク問題なし。
- **W5 と改名（2026-10-15 適用済み。ビルド・テストの結果待ち）**: `ScpUtil.cs`（29 型・約 11,500 行）を型ごとに分割し、元ファイルを削除（新設 29 ファイル。`Global.cs` 約 3,470 行、`BackingStore.cs` ほか）。複数型ファイルは 1 → **0**。型名の一覧は一致（618＝618）。ツールの自己検証は `VERIFY-OK`。
  - 静的初期化の順序: 型の宣言を変更せず別ファイルへ移しただけで、型ごとの静的コンストラクター（`beforefieldinit`）の評価は、ファイルの分割に影響されない。Step11 の起動確認で確認する。
  - ラチェットの基準値: `ScpUtil.cs` の (9,12,233) を `Global.cs` (9,11,47)＋`BackingStore.cs` (0,1,182)＋`Changelog.cs` (0,0,4) へ引き継いだ（合計は不変）。基準値の走査の起点（アンカー）も、`ScpUtil.cs` から `Global.cs` へ変更した（旧パスのままでは、検査が省略されて合格になるため）。
  - 付録 B の改名（原則 2）: `GitHubRelease.cs`→`GithubRelease.cs`、`EventHandlers.cs`→`PropertyChangingHandler.cs`、`Crc32.cs`→`Crc32Algorithm.cs`、`Specialactionslistviewmodelsorttests.cs`→`SpecialActionsListViewModelSortTests.cs`。大文字・小文字のみの改名（1 件目と 4 件目）は、コミット前に `git rm --cached` と `git add` が必要（W4 と同じ）。
  - **最終確認（走査）**: ファイル 624、トップレベル型 618。複数型ファイル 0。単一型でファイル名と型名が（大文字・小文字を含めて）一致しないファイル 0。型を持たないファイルは 6（`GlobalSuppressions.cs`、`AssemblyInfo.cs`、`Themes/_.cs`、`UpdaterInstallTrace.cs`［プレースホルダー］、`StressDispatchRaceTests.cs`、`IElevatedProcessLauncher.cs`［走査の誤判定。二重 BOM のため］）。
  - 残り: 完了条件 5（起動スモーク）、`.github/copilot-instructions.md` §3.3 の過渡期ルールの更新（原則 1 の厳格適用）、完了報告書。
  - （W1 の補足）ソース走査テストの影響: 確認した範囲では、W1 の対象を参照するテスト（`Step10FourTwoGlobalReferenceGuardTests`、`Step10GroupTwoGlobalReferenceGuardTests`）は、元ファイルに残った型のみを検査しており、変更不要。

---
---

## 0. 位置づけ

### 0.1 決定事項（2026-09-20）

| # | 論点 | 決定 |
|---|---|---|
| 1 | 位置 | **Step10 と Step11 の間**（案C）。Step2〜Step10 の Global 参照置換が終わった後、Step11（総合検証）と Step12（旧シム削除）の前 |
| 2 | 番号 | **Step10b**（Step11・Step12 の番号とファイル名は変更しない） |
| 3 | 対象 | リポジトリ全体の `.cs`（`DS4Windows`、`DS4WindowsTests`、`StandaloneTests`。自動生成コードと `externals/` を除く） |
| 4 | 規約 | `copilot-instructions.md` §3.3 原則1 に**過渡期ルール**を追加（§4）。許可済み |
| 5 | 実施順序 | Step13（13-1〜13-5）→ Step10b。移動してから分割することで、新設する型のファイルが最終の配置先に作られる |
| 6 | 提供方法 | **未決**（着手時に決定。§8） |

### 0.2 位置を Step10 と Step11 の間にした理由

1. Step2〜Step10 の各計画書は、対象ファイルの行番号を1行単位で確定している。Step10b をそれより前に行うと、対象ファイルの行番号台帳が広く無効になる。Step10 の完了後なら、置換は全て終わっているため台帳が不要になる。
2. Step11 の総合検証（自動テスト＋実機マトリクスの GO ゲート）が、分割後のコードを1回で検証できる。Step12 の後に分割すると、検証済みの状態を再度変更するため再検証が必要になる。
3. Step12 は `ScpUtil.cs` のシム削除だが、Step10b で `Global` を単独ファイルにしておくことで、削除対象のレビューが見通しやすくなる。
4. 全数の分割は、後続の案4（層別再編）の前提になる（1型1ファイルなら、型単位でフォルダへ移せる）。

---

## 1. 背景と全数調査の結果

### 1.1 背景
`copilot-instructions.md` §3.3 原則1（1ファイル＝1型）は「新規作成・改修するファイル」を対象にしていたため、既存の複数型ファイルが残っている。Phase6 の Step2 でも、複数型を含む既存ファイル（`IProfileSettingsService.cs`、`ProfileSettingsService.cs`、`IDeviceStateService.cs`）を改修したが分割していない。規約の文言に曖昧さがあったため、過渡期ルールで明確にする（§4）。

### 1.2 全数調査（Roslyn による抽出、トップレベル型のみ）

| 項目 | 値 |
|---|---:|
| `.cs` ファイル数（型を持つもの） | 353 |
| トップレベル型の総数 | 556 |
| **複数型を含むファイル** | **55**（`DS4Windows` 53、`DS4WindowsTests` 2） |
| **分割で新設されるファイル数** | **203**（class 150、enum 34、struct 12、delegate 6、interface 1） |
| 単一型だがファイル名が型名と異なるファイル（原則2） | 3（付録B） |
| 新設ファイル名が既存ファイルと衝突するもの | 0 |
| 同一ファイル内に同名の型が2つあるもの | 1（`DI/AppHost.cs`、§7-1） |

---

## 2. スコープとルール

| 項目 | 内容 |
|---|---|
| 対象とする型 | トップレベルの `class`／`struct`／`interface`／`enum`／`delegate`／`record` のすべて。**種別による例外は設けない**（`EventArgs` や `enum` も別ファイル） |
| 対象外 | 自動生成コード（`*.g.cs`、`*.g.i.cs`、`*.Designer.cs`）、`externals/`（サブモジュール）、**ネスト型**（親型の実装詳細として同居を許可） |
| `partial` 型 | 同一型の複数ファイルへの分割は違反ではない。1つのファイルに別々の型が同居している場合のみ対象 |
| 命名（原則2） | 新設ファイルのファイル名は型名と完全一致させる（`.xaml.cs` は `X.xaml.cs`）。ジェネリック型は型名のみ（`Foo.cs`）とする |
| 名前空間・フォルダ | **変更しない**（原則3の過渡期ルール）。新設ファイルは元ファイルと同じフォルダ、同じ名前空間に作る |
| 内容の保持 | 型の宣言（属性・XML ドキュメント・コメント・`#if` 領域を含む）は一字一句そのまま移す。必要な `using` は元ファイルから複製する（未使用の `using` の整理は行わない） |
| 履歴の保持 | 元ファイルに残す型は、そのファイルを最も大きい型か、ファイル名と同じ名前の型にする。どの型ともファイル名が一致しないときは、最も大きい型を `git mv` で改名して履歴を引き継ぎ、残りを新設する |

---

## 3. 他の Step との関係

| Step | 関係 |
|---|---|
| Step2〜Step10 | 本 Step の**前**。各 Step の PR では、既存の複数型ファイルの分割を行わず（§4）、新規に作る型は必ず別ファイルにする |
| Step13（配置統一） | 本 Step の**前**。13-1〜13-5 で移動されるファイルは、付録A に移動後のパスを併記した |
| Step11（総合検証） | 本 Step の**後**。分割後のコードで自動テスト・実機マトリクスを検証する |
| Step12（旧シム削除） | 本 Step の**後**。`Global` が単独ファイルになった状態で削除を行う |
| Phase7（`Mapping.cs` の instance 化） | `Mapping.cs` は単一型のため影響なし |
| 最終クリーンアップ（案4） | 本 Step の成果（1型1ファイル）が前提になる |

---

## 4. 規約の過渡期ルール（`copilot-instructions.md` §3.3 原則1 へ追記）

1. **新規作成するファイル**には複数の型を同居させない（従来どおり厳格）。
2. **既存の複数型ファイル**は、Phase6-Step10b で一括して分割する。それまでの PR では、既存の複数型ファイルを改修しても**分割は行わない**（行番号台帳と差分の肥大化を避けるため）。
3. ただし、既存の複数型ファイルに**新しい型を追加してはならない**。新しい型は必ず新規ファイルに作る。
4. 型の分割は、機能変更を含む PR に混ぜない。

---

## 5. ウェーブ構成

| ウェーブ | 内容 | ファイル数 | 新設ファイル数 | リスク |
|---|---|---:|---:|---|
| W1: データ型・補助型（Global 参照なし・低リスク） | 19 | 101 |
| W2: DI 層・サービス層 | 5 | 5 |
| W3: UI 層（View／ViewModel） | 20 | 34 |
| W4: デバイス・コア層 | 10 | 35 |
| W5: `ScpUtil.cs`（`Global`／`BackingStore` ほか） | 1 | 28 |

| ウェーブ | 順序の理由 |
|---|---|
| W1 | Global 参照を持たないデータ型・補助型。依存が少なく、ツールと検証手順の初回確認に適している |
| W2 | DI 層。型数が少なく、W1 の後に手順を確立してから行う |
| W3 | UI 層。XAML のコードビハインド（`partial`）を含むため、W1・W2 で手順を固めてから行う |
| W4 | デバイス・コア層。入力ループに関わるクラスを含むため、検証を厚くする |
| W5 | `ScpUtil.cs`（29型・約11,500行）。最後に単独の PR で行う。`Global`（静的クラス）、`BackingStore` ほかを個別ファイルに分ける。元のファイルは、最大の型の改名で履歴を引き継ぐ |

- 1ウェーブ = 1 PR（W5 は独立した PR）とし、各 PR でビルド・テスト・検証レポートを確認してからコミットする。
- 各ウェーブの開始時に、Roslyn による全数調査を再実行して台帳を再作成する（付録A は 2026-09-20 時点）。

---

## 6. 検証方式（機械検証）

内容を変えずに型を別ファイルへ移すだけであることを、次の方法で機械的に検証する。

| # | 検証 | 合格条件 |
|---|---|---|
| 1 | 型インベントリの一致 | 分割前後で、完全修飾名と種別の集合が完全に一致する |
| 2 | 宣言テキストの一致 | 型ごとの宣言テキスト（トリビア込み、空白正規化なし）のハッシュが、分割前後で一致する |
| 3 | 意味診断の一致 | Roslyn の意味診断（メソッド本体を含む）の集合が、分割前後で一致する（新規エラー 0 件） |
| 4 | 1ファイル1型・命名 | 対象ファイルのすべてが、トップレベル型を1つだけ持ち、ファイル名が型名と一致する（付録B の改名を含む） |
| 5 | ビルド・テスト | `dotnet build -c Release`（警告数が同じ）、テストビルド、`dotnet test` の全件成功 |
| 6 | 起動スモーク | アプリを起動し、コントローラーを接続して入力が反映され、終了できること。W3 の後は主要画面を1周する |

- W5 の後は、Step11 で自動テストと実機マトリクスを実施する。
- 検証 1〜4 は検証ツールが出力するレポートで確認する（提供方法は §8）。

---

## 7. 特殊ケース

1. **`DI/AppHost.cs`（同名の型が2つ）**: 名前空間 `DS4WinWPF` の `AppHost`（Composition Root）と、名前空間 `DS4Windows` の互換ラッパー `AppHost` が同居している。同一フォルダに同名のファイルは作れない。着手時に、ラッパーの呼び出し元を調査し、(a) 呼び出し元が 0 件なら Step12 の削除対象へ回す、(b) 残るなら別フォルダ（例: `DI/Compat/AppHost.cs`）に分ける、のいずれかを決定する。
2. **`ScpUtil.cs`**: `Global` を含む29型。分割後は `Global.cs` などになり、`ScpUtil.cs` は消える。`Global` の静的コンストラクタ・静的フィールドの初期化順序に依存する箇所は、型の宣言をそのまま移すため変わらない（検証 1〜3 で確認）。
3. **ファイル名が型名と一致しない単一型ファイル**: 付録B の3件は、`git mv` による改名のみを行う。
4. **`.xaml.cs`**: `partial class` の宣言のみ、XAML と対で残す。同居する別の型を新設ファイルへ移す。
5. **テストプロジェクトの2ファイル**: モックやフェイクの型を別ファイルへ移す。

---

## 8. 提供方法（未決。着手時に決定）

| 案 | 内容 | メリット | デメリット |
|---|---|---|---|
| A（推奨） | 分割ツール（Roslyn ベースの C# コンソールアプリ）と実行手順、検証レポートの出力を提供する。利用者は手元で実行する | 203ファイル・約11,500行の分割を、ドライランと検証レポート付きで再現性高く実施できる。ウェーブごとに再実行できる | 手元での実行が必要。ツール自体の確認が要る |
| B | 変更全体を `git apply` 可能なパッチ（`.patch`）で提供する | 利用者は適用するだけで済む | パッチが大きい。ウェーブ間の台帳更新に追随しにくい |
| C | 変更後のファイルをすべて提供する | 特別な操作が不要 | 新設203ファイルを含む提供は現実的でない |

---

## 9. リスクと対策

| リスク | 対策 |
|---|---|
| 分割で `using` の不足・`#if` 領域の取りこぼしが起きる | 検証 2・3（宣言テキストと意味診断の一致）で機械的に検出する |
| `Global` の静的初期化順序が変わる | 型の宣言を変更せず移すため変わらない。W5 の後に起動スモークと Step11 で確認する |
| 進行中の他の作業との競合 | 直列の PR で実施し、着手前に未マージの変更がないか確認する |
| XAML のコードビハインドの破損 | W3 の後に主要画面を1周して確認する |
| 過渡期ルールの逸脱（Step2〜10 で新しい型を既存ファイルに追加してしまう） | §4 の 3. を規約に明記し、各 PR のレビューで確認する |

---

## 10. 完了条件

1. 対象の全ファイルが、トップレベル型を1つだけ持ち、ファイル名が型名と一致していること（§6 の検証 1〜4）。
2. 検証 5（ビルド・テスト）と 6（起動スモーク）が合格していること。
3. `copilot-instructions.md` §3.3 の過渡期ルールが、完了に合わせて「原則1の厳格適用（既存ファイルを含む）」へ更新されていること。
4. 本 Step の完了報告書に、分割前後の型インベントリと検証レポートが添付されていること。

**見積り**: 約2〜3日（ウェーブ W1〜W5、ツール準備を含む）。

---

## 付録A: 全数台帳（2026-09-20 時点、複数型を含むファイル 55 件）

### W1: データ型・補助型（Global 参照なし・低リスク）

| ファイル（現行パス） | 型数 | 新設ファイル数 | 行数 | 型 | 備考 |
|---|---:|---:|---:|---|---|
| `DS4Windows/AutoProfileHolder.cs` → Step13 後 `DS4Windows/DS4Control/Profiles/AutoProfileHolder.cs` | 2 | 1 | 211 | AutoProfileHolder, AutoProfileEntity |  |
| `DS4Windows/DS4Control/ControlServiceDeviceOptions.cs` | 13 | 12 | 662 | ControlServiceDeviceOptions, ControllerOptionsStore, DS4DeviceOptions, DS3DeviceOptions, DS4ControllerOptions, DualSenseDeviceOptions, DualSenseControllerOptions, SwitchProDeviceOptions, SwitchProControllerOptions, JoyConDeviceOptions, JoyConControllerOptions, Vader4ProDeviceOptions, Vader4ProControllerOptions |  |
| `DS4Windows/DS4Control/DS4OutDevices/DS4OutDeviceExtras.cs` | 5 | 4 | 162 | DS4_TOUCH, DS4_REPORT_UNION, DS4_REPORT_EX, DS4OutputBufferData, DS4OutDeviceExtras |  |
| `DS4Windows/DS4Control/DTOXml/ActionsDTO.cs` | 2 | 1 | 349 | ActionsDTO, SpecialActionSerializer |  |
| `DS4Windows/DS4Control/DTOXml/AppSettingsDTO.cs` | 9 | 8 | 1246 | AppSettingsDTO, UDPSrvSmoothingOptionsGroup, InputDeviceOptions, BaseInputDeviceSettingsGroup, DS4SupportSettingsGroup, DS3SupportSettings, DualSenseSupportSettings, SwitchProSupportSettings, JoyConSupportSettings |  |
| `DS4Windows/DS4Control/DTOXml/AutoProfilesDTO.cs` | 2 | 1 | 188 | AutoProfilesDTO, AutoProfileEntrySerializer |  |
| `DS4Windows/DS4Control/DTOXml/LinkedProfilesDTO.cs` | 2 | 1 | 121 | LinkedProfilesDTO, LinkedProfileItem |  |
| `DS4Windows/DS4Control/DTOXml/OutputSlotPersistDTO.cs` | 2 | 1 | 99 | OutputSlotPersistDTO, OutputSlotSerializer |  |
| `DS4Windows/DS4Control/DTOXml/ProfileDTO.cs` | 22 | 21 | 3850 | ProfileDTO, StickAxialDeadOptionsSerializer, StickDeltaAccelSettings, SASteeringWheelSmoothingOptions, GyroControlsSettings, GyroMouseStickSmoothingSettings, GyroMouseSmoothingSettings, GyroSwipeSettings, StickModeOutputSettings, FlickStickSettings, DualSenseControllerSettings, TouchpadAbsMouseSettingsSerialize, TouchpadMouseStickSerializer, AbsMouseRegionSettingsSerializer, DS4ControlAssignementSerializer, DS4ControlAssignmentSerializerBase, DS4ControlButtonAssignmentSerializer, DS4ControlKeyAssignmentSerializer, DS4ControlMacroAssignmentSerializer, DS4ControlKeyTypeAssignmentSerializer, DS4ControlExtrasAssignmentSerializer, DS4ControlLightbarMacroAssignmentSerializer |  |
| `DS4Windows/DS4Control/DTOXml/XmlDataUtilities.cs` | 2 | 1 | 44 | XmlDataUtilities, Utf8StringWriter |  |
| `DS4Windows/DS4Control/MacroParser.cs` | 2 | 1 | 284 | MacroParser, MacroStep |  |
| `DS4Windows/DS4Control/PresetOption.cs` | 7 | 6 | 180 | PresetOption, GamepadPreset, GamepadGyroCamera, MixedPreset, MixedGyroMousePreset, KBMPreset, KBMGyroMouse |  |
| `DS4Windows/DS4Control/ProfilePropGroups.cs` | 28 | 27 | 1569 | ProfileActions, Sensitivity, SquareStickInfo, StickDeadZoneInfo, StickAntiSnapbackInfo, TriggerDeadZoneZInfo, GyroMouseInfo, GyroMouseStickInfo, GyroDirectionalSwipeInfo, GyroControlsInfo, ButtonMouseInfo, ButtonAbsMouseInfo, LightbarMode, LightbarDS4WinInfo, LightbarSettingInfo, SteeringWheelSmoothingInfo, TouchpadRelMouseSettings, TouchpadAbsMouseSettings, TouchMouseStickInfo, StickMode, TriggerMode, TwoStageTriggerMode, FlickStickSettings, StickControlSettings, StickModeSettings, StickOutputSetting, TriggerOutputSettings, RumbleSettings | ファイル名がどの型名とも不一致（元ファイルは全型を新設ファイルへ移した後に削除） |
| `DS4Windows/DS4Control/Trigger.cs` | 4 | 3 | 53 | TriggerContext, TriggerType, ITrigger, SingleButtonTrigger | ファイル名がどの型名とも不一致（元ファイルは全型を新設ファイルへ移した後に削除） |
| `DS4Windows/OneEuroFilter.cs` → Step13 後 `DS4Windows/Core/Utilities/OneEuroFilter.cs` | 2 | 1 | 104 | OneEuroFilter, LowpassFilter |  |
| `DS4Windows/ReaderWriteLockSlimWrappers.cs` → Step13 後 `DS4Windows/Core/Utilities/ReaderWriteLockSlimWrappers.cs` | 2 | 1 | 62 | ReadLocker, WriteLocker | ファイル名がどの型名とも不一致（元ファイルは全型を新設ファイルへ移した後に削除） |
| `DS4Windows/VJoyFeeder/vJoyFeeder.cs` | 8 | 7 | 718 | HID_USAGES, VjdStat, FFBPType, FFBEType, FFB_CTRL, FFBOP, VJoy, vJoyFeeder |  |
| `DS4WindowsTests/ActionControllerTests/TestActionControllerTests.cs` | 3 | 2 | 103 | MockRepeater, TestActionController, ActionControllerTests | ファイル名がどの型名とも不一致（元ファイルは全型を新設ファイルへ移した後に削除） |
| `DS4WindowsTests/ActionControllerTests/ToggleAndPressedOnceTests.cs` | 3 | 2 | 324 | TestServiceProvider, FakeKBMHandler, ToggleAndToggledOnTests | ファイル名がどの型名とも不一致（元ファイルは全型を新設ファイルへ移した後に削除） |

### W2: DI 層・サービス層

| ファイル（現行パス） | 型数 | 新設ファイル数 | 行数 | 型 | 備考 |
|---|---:|---:|---:|---|---|
| `DS4Windows/DI/AppHost.cs` | 2 | 1 | 185 | AppHost, AppHost | 同名の型が2つ（§7-1） |
| `DS4Windows/DI/IDeviceStateService.cs` | 2 | 1 | 37 | DeviceStateChangedEventArgs, IDeviceStateService |  |
| `DS4Windows/DI/INotificationService.cs` | 2 | 1 | 31 | NotificationEventArgs, INotificationService |  |
| `DS4Windows/DI/IOutputSlotService.cs` | 2 | 1 | 80 | OutputSlotChangedEventArgs, IOutputSlotService |  |
| `DS4Windows/DI/IProfileSettingsService.cs` | 2 | 1 | 261 | ProfileSettingChangedEventArgs, IProfileSettingsService |  |

### W3: UI 層（View／ViewModel）

| ファイル（現行パス） | 型数 | 新設ファイル数 | 行数 | 型 | 備考 |
|---|---:|---:|---:|---|---|
| `DS4Windows/DS4Forms/BindingWindow.xaml.cs` | 3 | 2 | 1019 | BindingWindow, BindingWinResourcePaths, IntToBoolConverter |  |
| `DS4Windows/DS4Forms/LanguageSelectDialog.xaml.cs` | 2 | 1 | 146 | LanguageInfo, LanguageSelectDialog |  |
| `DS4Windows/DS4Forms/MainWindow.xaml.cs` | 2 | 1 | 2218 | MainWindow, ImageLocationPaths |  |
| `DS4Windows/DS4Forms/ProfileEditor.xaml.cs` | 3 | 2 | 2514 | ProfileEditor, ResourcePaths, ControlIndexCheck |  |
| `DS4Windows/DS4Forms/RecordBox.xaml.cs` | 2 | 1 | 649 | RecordBox, RecordBoxResourcePaths |  |
| `DS4Windows/DS4Forms/StickCalibrationWindow.xaml.cs` | 2 | 1 | 66 | StickCalibrationWindow, Stick |  |
| `DS4Windows/DS4Forms/ViewModels/AutoProfilesViewModel.cs` | 3 | 2 | 728 | AutoProfilesViewModel, ProgramItem, NativeMethods2 |  |
| `DS4Windows/DS4Forms/ViewModels/BindingWindowViewModel.cs` | 4 | 3 | 929 | BindingWindowViewModel, BindAssociation, OutBinding, LightbarMacro |  |
| `DS4Windows/DS4Forms/ViewModels/ControllerListViewModel.cs` | 2 | 1 | 739 | ControllerListViewModel, CompositeDeviceModel |  |
| `DS4Windows/DS4Forms/ViewModels/ControllerRegDeviceOptsViewModel.cs` | 7 | 6 | 375 | ControllerRegDeviceOptsViewModel, DeviceListItem, DS4ControllerOptionsWrapper, DualSenseControllerOptionsWrapper, SwitchProControllerOptionsWrapper, Vader4ProControllerOptionsWrapper, JoyConControllerOptionsWrapper |  |
| `DS4Windows/DS4Forms/ViewModels/CurrentOutDeviceViewModel.cs` | 2 | 1 | 502 | CurrentOutDeviceViewModel, SlotDeviceEntry |  |
| `DS4Windows/DS4Forms/ViewModels/LanguagePackViewModel.cs` | 2 | 1 | 139 | LanguagePackViewModel, LangPackItem |  |
| `DS4Windows/DS4Forms/ViewModels/LightbarMacroViewModel.cs` | 3 | 2 | 92 | LightbarMacroViewModel, LightbarMacroElement, LightbarMacroTrigger |  |
| `DS4Windows/DS4Forms/ViewModels/MappingListViewModel.cs` | 2 | 1 | 360 | MappingListViewModel, MappedControl |  |
| `DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs` | 5 | 4 | 4257 | ProfileSettingsViewModel, PresetMenuHelper, TriggerModeChoice, TwoStageChoice, TriggerEffectChoice |  |
| `DS4Windows/DS4Forms/ViewModels/RecordBoxViewModel.cs` | 2 | 1 | 554 | RecordBoxViewModel, MacroStepItem |  |
| `DS4Windows/DS4Forms/ViewModels/SettingsViewModel.cs` | 2 | 1 | 760 | SettingsViewModel, MonitorChoiceListing |  |
| `DS4Windows/DS4Forms/ViewModels/SpecialActionsListViewModel.cs` | 2 | 1 | 444 | SpecialActionsListViewModel, SpecialActionItem |  |
| `DS4Windows/DS4Forms/ViewModels/TrayIconViewModel.cs` | 2 | 1 | 480 | TrayIconViewModel, ControllerHolder |  |
| `DS4Windows/DS4Forms/WelcomeDialog.xaml.cs` | 2 | 1 | 515 | WelcomeDialog, WelcomeDialogResourcePaths |  |

### W4: デバイス・コア層

| ファイル（現行パス） | 型数 | 新設ファイル数 | 行数 | 型 | 備考 |
|---|---:|---:|---:|---|---|
| `DS4Windows/DS4Control/Action.cs` → Step13 後 `DS4Windows/Actions/Action.cs` | 2 | 1 | 27 | MappingContext, Action |  |
| `DS4Windows/DS4Control/ActionManager.cs` → Step13 後 `DS4Windows/Actions/ActionManager.cs` | 3 | 2 | 615 | ActionInstanceState, ActionEntry, ActionManager |  |
| `DS4Windows/DS4Control/Log.cs` | 3 | 2 | 154 | AppLogger, ProfileChangeSource, ProfileChangedEventArgs | ファイル名がどの型名とも不一致（元ファイルは全型を新設ファイルへ移した後に削除） |
| `DS4Windows/DS4Control/UdpServer.cs` | 7 | 6 | 1049 | DsState, DsConnection, DsModel, DsBattery, DualShockPadMeta, PadDataRspPacket, UdpServer |  |
| `DS4Windows/DS4Library/DS4Device.cs` | 6 | 5 | 2169 | DS4Color, ConnectionType, DS4ForceFeedbackState, DS4LightbarState, DS4HapticState, DS4Device |  |
| `DS4Windows/DS4Library/DS4Devices.cs` | 10 | 9 | 640 | VidPidFeatureSet, VidPidInfo, RequestElevationArgs, RequestElevationDelegate, CheckVirtualInfo, CheckVirtualDelegate, CheckConnectionDelegate, PrepareInitDelegate, CheckPendingDevice, DS4Devices |  |
| `DS4Windows/DS4Library/DS4Sixaxis.cs` | 6 | 5 | 648 | SixAxisHandler, SixAxisEventArgs, SixAxis, CalibData, GyroAverageWindow, DS4SixAxis | ファイル名がどの型名とも不一致（元ファイルは全型を新設ファイルへ移した後に削除） |
| `DS4Windows/DS4Library/DS4Touchpad.cs` | 3 | 2 | 321 | TouchpadEventArgs, Touch, DS4Touchpad |  |
| `DS4Windows/DS4Library/InputDevices/InputDeviceFactory.cs` | 2 | 1 | 72 | InputDeviceType, InputDeviceFactory |  |
| `DS4Windows/DS4Library/InputDevices/TriggerEffects.cs` | 3 | 2 | 47 | TriggerEffects, TriggerId, TriggerEffectSettings |  |

### W5: `ScpUtil.cs`（`Global`／`BackingStore` ほか）

| ファイル（現行パス） | 型数 | 新設ファイル数 | 行数 | 型 | 備考 |
|---|---:|---:|---:|---|---|
| `DS4Windows/DS4Control/ScpUtil.cs` | 29 | 28 | 11478 | DS4KeyType, Ds3PadId, DS4Controls, X360Controls, SASteeringWheelEmulationAxisType, OutContType, GyroOutMode, TouchpadOutMode, TrayIconChoice, AppThemeChoice, ControlActionData, AutoProfileDisplayProfileSwitchChoices, DS4TriggerOutputMode, DS4ControlSettings, ControlSettingsGroup, DebugEventArgs, MappingDoneEventArgs, ReportEventArgs, BatteryReportArgs, ControllerRemovedArgs, DeviceStatusChangeEventArgs, SerialChangeArgs, OneEuroFilterPair, OneEuroFilter3D, SelectedProfileChangedEventArgs, Global, Changelog, BackingStore, SpecialAction | ファイル名がどの型名とも不一致（元ファイルは全型を新設ファイルへ移した後に削除） |

---

## 付録B: 単一型だがファイル名が型名と異なるファイル（原則2、`git mv` で改名）

| ファイル | 型名 |
|---|---|
| `DS4Windows/ApiDTO/GitHubRelease.cs` | `GithubRelease` |
| `DS4Windows/DS4Forms/ViewModels/Util/EventHandlers.cs` | `PropertyChangingHandler` |
| `DS4Windows/DS4Library/Crc32.cs` | `Crc32Algorithm` |