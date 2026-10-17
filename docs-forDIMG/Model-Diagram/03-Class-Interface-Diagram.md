# クラス図 / インターフェース関連図
**（Class & Interface Specification）**

本ドキュメントは、巨大ファイルの解体、ステートレスパイプライン、パケット解析の分離、およびUIのMediator化を反映した、DS4Windows-Vader4Pro のインターフェース構成とクラス関連性を定義する。

---

## 1. 主要クラス・インターフェース関連図

```mermaid
classDiagram
    %% ==========================================
    %% 1. 入力監視層 (Ingress: 通信と解析の分離)
    %% ==========================================
    class IDs4DeviceRegistry {
        <<interface>>
        +getDevices() IEnumerable~IInputDevice~
        +findDeviceByMac(string mac) IInputDevice
        +startScan()
        +stopScan()
    }
    class IDeviceHotplugMonitor {
        <<interface>>
        +StartMonitoring()
        +StopMonitoring()
        +DeviceArrived event
        +DeviceRemoved event
    }
    class IHidTransport {
        <<interface>>
        +ReadReport(byte[] buffer) int
        +WriteReport(byte[] buffer) bool
        +IsConnected bool
    }
    class IInputReportParser {
        <<interface>>
        +ParseInputReport(byte[] rawReport, ref RawInputState outState) bool
    }
    class IOutputReportEncoder {
        <<interface>>
        +BuildOutputReport(FeedbackState feedback, byte[] outBuffer) int
    }

    class Ds4DeviceRegistryAdapter {
        -List~IInputDevice~ _activeDevices
    }
    class DS4ReportParser {
        +ParseInputReport(byte[] rawReport, ref RawInputState outState) bool
    }
    class Vader4ProReportParser {
        +ParseInputReport(byte[] rawReport, ref RawInputState outState) bool
    }

    IDs4DeviceRegistry <|.. Ds4DeviceRegistryAdapter : implements
    IInputReportParser <|.. DS4ReportParser : implements
    IInputReportParser <|.. Vader4ProReportParser : implements
    Ds4DeviceRegistryAdapter --> IDeviceHotplugMonitor : uses
    Ds4DeviceRegistryAdapter --> IHidTransport : uses

    %% ==========================================
    %% 2. 信号変換層 (Mapping Pipeline & Context)
    %% ==========================================
    class MappingPipelineContext {
        +int SlotIndex
        +RawInputState CurrentRaw
        +SlotHistoryState History
        +TransformedOutput Output
        +ResetForTick()
    }

    class IInputMappingPipeline {
        <<interface>>
        +ProcessPipeline(MappingPipelineContext ctx, ProfileEntity profile)
    }
    class IButtonProcessor {
        <<interface>>
        +ProcessButtons(MappingPipelineContext ctx, ButtonMappingConfig cfg)
    }
    class IStickProcessor {
        <<interface>>
        +ProcessStick(MappingPipelineContext ctx, StickConfig cfg)
    }
    class ITriggerProcessor {
        <<interface>>
        +ProcessTriggers(MappingPipelineContext ctx, TriggerConfig cfg)
    }
    class ITouchGyroProcessor {
        <<interface>>
        +ProcessTouchAndGyro(MappingPipelineContext ctx, MotionConfig cfg)
    }
    class IMouseEngine {
        <<interface>>
        +ProcessMousePointer(MappingPipelineContext ctx, MouseConfig cfg)
    }

    class InputMappingPipeline {
        -IButtonProcessor _btnProc
        -IStickProcessor _stickProc
        -ITriggerProcessor _trigProc
        -ITouchGyroProcessor _touchGyroProc
        -IMouseEngine _mouseEngine
        +ProcessPipeline(MappingPipelineContext ctx, ProfileEntity profile)
    }
    IInputMappingPipeline <|.. InputMappingPipeline : implements
    InputMappingPipeline o-- IButtonProcessor
    InputMappingPipeline o-- IStickProcessor
    InputMappingPipeline o-- ITriggerProcessor
    InputMappingPipeline o-- ITouchGyroProcessor
    InputMappingPipeline o-- IMouseEngine
    InputMappingPipeline ..> MappingPipelineContext : mutates

    class ControlService {
        -IDs4DeviceRegistry _deviceRegistry
        -IInputLoopCoordinator _loopCoordinator
        -IInputMappingPipeline _pipeline
        -IOutputSlotService _outputSlotService
        -IVirtualKBM _virtualKBM
        -IVirtualKBMLifecycle _kbmLifecycle
        -IProfileSlotApplier _profileSlotApplier
        -IProfileSettingsService _profileSettings
        -IAppSettingsService _appSettings
        -IEnvironmentService _environmentService
        -MappingPipelineContext[] _contextPool
        +Start()
        +Stop()
    }
    ControlService --> IDs4DeviceRegistry : uses
    ControlService --> IInputMappingPipeline : uses
    ControlService o-- MappingPipelineContext : pools per slot

    %% ==========================================
    %% 3. 信号出力層 (Egress)
    %% ==========================================
    class IOutputSlotService {
        <<interface>>
        +AttachDevice(int slot, OutContType type) bool
        +DetachDevice(int slot) bool
        +GetOutDevice(int slot) OutputDevice
        +SyncSlotPersist()
    }
    class IVirtualKBM {
        <<interface>>
        +PerformKeyPress(uint key, bool useScan)
        +PerformKeyRelease(uint key, bool useScan)
        +MoveMouse(int dx, int dy)
    }
    class IVirtualKBMLifecycle {
        <<interface>>
        +ReleaseHandler()
        +DetermineHandler(string identifier)
        +SwitchToFallbackHandler()
        +ApplyFakerInputVersion()
        +InitializeMapping(string identifier)
    }
    class IProfileSlotApplier {
        <<interface>>
        +ApplyToSlot(int slotIndex, string profileName, ProfileChangeSource source) bool
    }
    class ProfileSlotApplier
    IProfileSlotApplier <|.. ProfileSlotApplier : implements
    class OutputKBMHandlerAdapter {
        +VirtualKBMBase Handler
        +PerformKeyPress(uint key)
        +Sync()
    }
    class OutputKBMHandlerLifecycle {
        -IEnvironmentService _environment
        -OutputKBMHandlerAdapter _handlerOwner
        -IProfileSettingsService _profileSettings
    }
    IVirtualKBM <|.. OutputKBMHandlerAdapter : implements and owns the handler
    IVirtualKBMLifecycle <|.. OutputKBMHandlerLifecycle : implements
    OutputKBMHandlerLifecycle --> IEnvironmentService : reads FakerInput version
    OutputKBMHandlerLifecycle --> OutputKBMHandlerAdapter : sets Handler
    OutputKBMHandlerLifecycle --> IProfileSettingsService : sets OutputKBMMapping
    class IMapping {
        <<interface>>
        +SetCurveAndDeadzone(int device, DS4State cState, DS4State dState, IProfileSettingsService settings) DS4State
        +MapCustom(int device, DS4State cState, DS4State mapped, DS4StateExposed eState, Mouse tp, ControlService ctrl)
        +TempMouseJoystick(int device, DS4State mapped)
        +Commit(int device, IProfileSettingsService settings)
        +GetBoolMappingExternal(int device, DS4Controls control, DS4State cState, DS4StateExposed eState, Mouse tp) bool
        +HandleDeviceDisconnect(int device)
        +PlayMacroDirect(int device, SpecialAction action)
        +EndMacroDirect(int device)
    }
    class Mapping {
        -SyntheticInputState _syntheticInput
        -MacroRuntimeState _macroRuntime
        -StickMotionState _stickMotion
    }
    class IKeyButtonControllerRegistry {
        <<interface>>
        +GetOrCreate(int device, Mode mode, string actionName) KeyButtonActionController
        +GetOrCreate(int device, SpecialAction sa) KeyButtonActionController
        +GetOrCreateForAction(int device, SpecialAction sa) IActionController
        +ClearForDevice(int device)
    }
    class KeyButtonControllerRegistry
    class ISyntheticDispatcher {
        <<interface>>
        +ResolveNativeKey(ushort kvpKey) uint
        +SendPress(int device, ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM handler)
        +SendRelease(int device, ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM handler)
        +ResetKeyTiming(int device, ushort kvpKey)
    }
    class SyntheticDispatcherService
    class SyntheticInputState
    class MacroRuntimeState
    class StickMotionState
    IMapping <|.. Mapping : implements
    IKeyButtonControllerRegistry <|.. KeyButtonControllerRegistry : implements
    ISyntheticDispatcher <|.. SyntheticDispatcherService : implements
    Mapping --> SyntheticInputState : ctor injection
    Mapping --> MacroRuntimeState : ctor injection
    Mapping --> StickMotionState : ctor injection
    Mapping --> IProfileSettingsService
    Mapping --> IProfileApplicationService
    Mapping --> IVirtualKBM
    Mapping --> IKeyButtonControllerRegistry
    Mapping --> ISyntheticDispatcher
    ProfileApplicationService --> IKeyButtonControllerRegistry : ClearForDevice
    SyntheticDispatcherService --> SyntheticInputState
    SyntheticDispatcherService --> OutputKBMHandlerAdapter : default output (Handler)
    SyntheticDispatcherService --> IProfileSettingsService : OutputKBMMapping
    ControlService --> IMapping : MappingService
    class IMacroPlayer {
        <<interface>>
        +PlayMacro(int slot, MacroStep[] steps)
        +StopMacro(int slot)
    }
    class ILightbarService {
        <<interface>>
        +ComputeColor(int slot, BatteryStatus battery, LightbarConfig cfg) DS4Color
    }
    InputMappingPipeline --> IOutputSlotService : outputs pad
    IMouseEngine --> IVirtualKBM : outputs mouse
    ControlService --> ILightbarService : updates LED
    ControlService --> IVirtualKBM : identify / Sync
    ControlService --> IVirtualKBMLifecycle : manages KBM handler lifecycle
    ControlService --> IProfileSlotApplier : applies connect-time profile

    %% ==========================================
    %% 4. UI層 (Sub-ViewModels Mediator構成)
    %% ==========================================
    class ProfileSettingsViewModel {
        +StickSettingsSubViewModel StickSubVM
        +TriggerSettingsSubViewModel TriggerSubVM
        +ButtonMappingSubViewModel ButtonSubVM
        +SpecialActionsSubViewModel ActionSubVM
        -IProfileRepository _profileRepo
        -IEnvironmentService _environmentService
        -IPathService _pathService
        +SaveProfile(string profileName) bool
        +LoadBlankProfile(ControlService control)
        +ApplyPreset(PresetOption opt)
    }
    class StickSettingsSubViewModel {
        -IProfileSettingsService _profileSettings
        +LoadSettings(StickConfig cfg)
        +ApplyTo(StickConfig cfg)
    }
    class TriggerSettingsSubViewModel {
        -IProfileSettingsService _profileSettings
        +LoadSettings(TriggerConfig cfg)
        +ApplyTo(TriggerConfig cfg)
    }
    class ButtonMappingSubViewModel {
        -IProfileSettingsService _profileSettings
        +LoadSettings(ButtonMappingConfig cfg)
        +ApplyTo(ButtonMappingConfig cfg)
    }
    class SpecialActionsSubViewModel {
        -ISpecialActionRepository _actionRepo
        -IProfileRepository _profileRepo
        +LoadActions(string profileName)
        +GetActionOrPlaceholder(string actionName) SpecialAction
        +CommitEnabledActionsToProfile()
    }

    class MainWindowsViewModel {
        -IEnvironmentService _environmentService
        -IPathService _pathService
        +CheckDrivers()
    }
    class SettingsViewModel {
        -IAppSettingsService _appSettings
        -IAppearanceSettingsService _appearanceSettings
        -IEnvironmentService _environmentService
        -IPathService _pathService
        -ILogSettingsApplier _logSettingsApplier
    }
    class TrayIconViewModel {
        -IAppearanceSettingsService _appearanceSettings
        -IProfileRepository _profileRepository
        -IPathService _pathService
        -Dispatcher uiDispatcher
    }
    class ILogSettingsApplier {
        <<interface>>
        +UpdateLogSettings()
        +UpdateNLogConfig(int maxArchiveFiles)
        +RestoreArchiveSetting()
    }
    class LogSettingsApplierProxy {
        +Attach(ILogSettingsApplier applier)
    }
    class LoggerHolder
    ILogSettingsApplier <|.. LogSettingsApplierProxy : implements
    ILogSettingsApplier <|.. LoggerHolder : implements
    LogSettingsApplierProxy --> LoggerHolder : Attach 後に転送
    SettingsViewModel --> ILogSettingsApplier : ログ設定の反映

    class IProfilePresetLoader {
        <<interface>>
        +LoadBlank(deviceIndex, launchProgram, control, xinputChange, postLoad)
        +LoadBlankDS4(...)
        +LoadDefaultGamepadGyro(...)
        +LoadDefaultDS4GamepadGyro(...)
        +LoadDefaultMixedControls(...)
        +LoadDefaultDS4MixedControls(...)
        +LoadDefaultMixedGyroMouse(...)
        +LoadDefaultDS4MixedGyroMouse(...)
        +LoadDefaultKBM(...)
        +LoadDefaultKBMGyroMouse(...)
    }
    class ProfilePresetLoader
    IProfilePresetLoader <|.. ProfilePresetLoader : implements（Global.Load…Profile への薄い委譲）
    class IEditorWindowFactory {
        <<interface>>
        +CreateBindingWindow(deviceNum, settings, targetDevice, expose)
        +CreateRecordBox(deviceNum, controlSettings, shift, targetDevice, showscan, repeatable)
        +CreateRecordBoxWindow(deviceNum, settings, targetDevice, repeatable)
        +CreatePresetOptionWindow()
        +CreateStickCalibrationWindow(stick, device, profileSettingsViewModel)
        +CreateSpecialActionEditor(deviceNum, profileList, targetDevice, specialAction)
    }
    class EditorWindowFactory {
        -IProfileSettingsService _profileSettings
        -IOutputSlotService _outputSlotService
        -IPathService _pathService
        -IViewModelFactory _viewModelFactory
        -IProfilePresetLoader _presetLoader
        -Func~ControlService~ _controlServiceAccessor
    }
    IEditorWindowFactory <|.. EditorWindowFactory : implements
    EditorWindowFactory --> IProfilePresetLoader : プリセットを PresetOptionWindow へ渡す

    ProfileSettingsViewModel *-- StickSettingsSubViewModel : mediates
    ProfileSettingsViewModel *-- TriggerSettingsSubViewModel : mediates
    ProfileSettingsViewModel *-- ButtonMappingSubViewModel : mediates
    ProfileSettingsViewModel *-- SpecialActionsSubViewModel : mediates

    class IViewModelFactory {
        <<interface>>
        +CreateProfileSettingsVM(string profileName, int targetDevice) ProfileSettingsViewModel
        +CreateRecordBoxViewModel(device, controlSettings, recordMacro, extraHold, targetDevice) RecordBoxViewModel
        +CreateSpecialActEditorViewModel(device, action) SpecialActEditorViewModel
        +CreateAutoProfilesViewModel(autoProfileHolder, profileList) AutoProfilesViewModel
        +CreateSpecialActionsListViewModel(device) SpecialActionsListViewModel
    }
    class ViewModelFactory {
        -IProfileSettingsService _profileSettings
        -IProfileRepository _profileRepo
        -ISpecialActionRepository _actionRepo
        -IOutputSlotService _outputSlotService
        -IEnvironmentService _environmentService
        -IPathService _pathService
        -IAppSettingsService _appSettings
        -Func~IAutoProfileService~ _autoProfileServiceAccessor
        -Func~ControlService~ _controlServiceAccessor
    }
    IViewModelFactory <|.. ViewModelFactory : implements
    class ISpecialActionRepository {
        <<interface>>
        +ActionList List~SpecialAction~
        +AddAction(action) bool
        +RemoveAction(actionName) bool「メモリ上のリストのみ」
        +SaveAction(name, controls, mode, details, edit, delayTime, extras)「Actions.xml へ保存・読み直し・状態リセット」
        +RemoveActionAndSave(actionName) bool「Actions.xml へ保存・読み直し」
        +GetActionOrPlaceholder(actionName) SpecialAction
    }
|.. ViewModelFactory : implements
    ViewModelFactory ..> ProfileSettingsViewModel : creates

    %% ==========================================
    %% 横断基盤・永続化層 (Storage)
    %% ==========================================
    class IProfileXmlStore {
        <<interface>>
        +LoadProfileXml(int deviceIndex, bool launchProgram, ControlService control, ...) bool
        +SaveProfileXml(int deviceIndex, string profileName) bool
        +LoadAppSettingsXml() bool
        +SaveAppSettingsXml() bool
        +LoadControllerConfigsForDevice(DS4Device device) bool
        +SaveControllerConfigsForDevice(DS4Device device) bool
    }
    class IDeviceOptionRepository {
        <<interface>>
        +GetDeviceOptions~T~(string mac) T
        +SaveDeviceOptions~T~(string mac, T opts) bool
    }
    class XmlIoLock {
        -ReaderWriterLockSlim _lock
        +AcquireReadLock()
        +AcquireWriteLock()
    }
    class ProfileXmlStore {
        -XmlIoLock _lock
    }
    IProfileXmlStore <|.. ProfileXmlStore : implements
    ProfileXmlStore --> XmlIoLock : uses
```

---

## 2. 適用された主要アーキテクチャ・デザインパターン

1. **Pipeline & In-place Mutation パターン（信号変換層）：**
   * `IInputMappingPipeline` を起点に、各プロセッサが常駐バッファ `MappingPipelineContext` を順次更新（インプレースミューテーション）。参照型のアロケーションを完全に抑制し、Zero-GCホットパスを実現。
2. **Mediator パターン（UI層）：**
   * 親の `ProfileSettingsViewModel` が Mediator となり、分割された各サブViewModel（`StickSubVM`, `TriggerSubVM` 等）の設定値収集・反映・永続化呼び出しを一元管理。サブVM同士の結合度をゼロにする。
3. **Transport / Parser 分離パターン（入力監視層）：**
   * `IHidTransport`（OS依存の通信）と `IInputReportParser`（OS非依存のパケット解析）を切り離し、Vader 4 Pro や DS4 の生バイト列解析ロジックを実機なしで単体テスト可能にする。
4. **Repository & IoLock パターン（横断基盤層）：**
   * XMLファイルの入出力を `XmlIoLock` で完全にラップし、複数スレッドからの同時アクセスやプロファイル自動保存時のデッドロックを防止。

5. **送出とライフサイクルの分離（3-b KBM出力）：**
   * キー・マウスの送出（`IVirtualKBM`）と、出力ハンドラの生成・破棄・フォールバック切替・マッピング初期化（`IVirtualKBMLifecycle`）を別インターフェースに分ける。送出側（Actions・`IMouseEngine`）はライフサイクル操作を知らず、`ControlService` だけが両方を使う。

%% 注釈補強（2026-09-19 Phase6-Step2 決定O1=B／O4=B-2 反映）
%% - `IProfileSlotApplier`（`ProfileSlotApplier` が実装）を追加。`ControlService` は `IProfileApplicationService` に直接依存せず、自身が定義する出力ポートのみを知る（依存の逆転）。現状の実装は `Global.ApplyProfileToSlot` へ委譲し、`IProfileApplicationService` への委譲は K1 の是正後（フェーズG）に行う。

%% 注釈補強（2026-09-19 Phase6-Step2 実地確認・決定D1〜D3反映）
%% - `IVirtualKBMLifecycle`（`OutputKBMHandlerLifecycle` が実装）を追加（決定D3）。現行の `OutputKBMHandlerAdapter` は `Global.outputKBMHandler` への読み取り専用委譲であり、ハンドラの生成・差し替えの口を持たないため。図中の `IVirtualKBM` のメソッドは概念的な代表例で、実インターフェース（`DS4Windows.Services.IVirtualKBM`）とは一致しない。
%% - `ControlService` のフィールドは Step2 の主な依存のみ記載（全体は Phase6-Step2-Plan.md §3.1）。過渡期は `IOutputSlotService` を `Func<IOutputSlotService>` で遅延解決する（決定D1、04-Service-Lifecycle-Spec.md §4）。目標構造（直接注入）は変更しない。

%% 注釈補強（2026-09-18監査結果反映、構造変更なし）
%% - 各巨大ファイル（ScpUtil/Mapping/Mouse/ControlService/ProfileSettingsVM/DS4Device）の解体先（1クラス1責任）は現状監査（762参照、10ファイル網羅、重複ゼロ・漏れゼロ証明済み）と完全一致。
%% - ScpUtil.cs の255件は「参照」ではなく「静的宣言」として区別（§2抽出問題記録済み）。横断基盤層（XmlIoLock, PathService等）へのマッピングは理想構造と一致。
%% - ProfileSettingsViewModel の Mediator 構成（親VM + SubVM群）は現状の ProfileEditor（96件、SpecialAction契約差含む）と一致。契約差（GetAction/LoadActions正規化差、Save/Remove操作境界差）はb要設計として記録済み（ABC-Classification.md 参照）。
%% - Pipeline & In-place Mutation パターン（Zero-GC）は現状のホットパス（ControlService入力ループ、Mappingのreadonly配列含む69件）と一致。readonly配列はc除外として維持（自動除外しない：報告書§5維持）。
%% - Transport/Parser 分離（IHidTransport + IInputReportParser）は DS4Device（87件）の解体と一致。パケット解析の単体テスト担保は理想構造の責務として維持。
%% - Phase5証跡（Step14/15未完了）の前提条件を維持（証跡存在確認済み、内容検証別途必要）。
%% - ガードレール（10項目）の未適用項目はStep2以降の実装時に適用（Phase6-Status.md 参照）。

%% 注釈補強（2026-09-21 Phase6-Step3 実地突き合わせ・論点3決定反映）
%% - `IProfileXmlStore` のメンバ一覧を実装済みの現行シグネチャに合わせて更新（`LoadProfileXml`/`SaveProfileXml` は `(path, ProfileDTO)` ではなく `deviceIndex` 主体、`LoadAppSettingsXml`/`SaveAppSettingsXml`/`LoadControllerConfigsForDevice` を追加）。あわせて `SaveControllerConfigsForDevice`（Step3 で新設予定）を追加。
%% - `IDisplayCoordinateService`（`DisplayCoordinateService` が実装）を新設。`Mapping.cs` の `absUseAllMonitors`／`TranslateCoorToAbsDisplay`（Step1エビデンス §2 分類c）の移行先。既存 `IEnvironmentService.PrepareAbsMonitorBounds` は同じ画面座標状態（`absDisplayBounds`/`fullDesktopBounds`/`absUseAllMonitors`）を更新する処理のため本サービスへ集約し、`IEnvironmentService` 側は薄い委譲として残す（Step3-1 で実装時に確定）。

%% 注釈補強（2026-09-23 Abs Mouse機能削除・ユーザー承認済み、copilot-instructions.md §2.2 例外規定）
%% - 上記の `IDisplayCoordinateService`／`DisplayCoordinateService` は、その唯一の利用者であったボタン割当のAbs Mouse機能（`Mapping.cs` の `MapCustom`／`GetAbsMouseMapping`）およびタッチパッドのAbsolute Mouseモード（`TouchpadAbsMouseSettings`）が、使用頻度が極めて低いとの判断により削除されたことに伴い、丸ごと撤去した。`IEnvironmentService` 側への影響はない（もともと薄い委譲のみ）。
%% - あわせて `IProfileSettingsService.ButtonAbsMouseInfos`／`TouchAbsMouse`、`IVirtualKBM.MoveAbsoluteMouse`（本図には個別メンバーとして未記載だったため図の変更なし）も削除済み。
%% 注釈補強（2026-09-25 Phase6-Step7-4 決定7＝案H 反映）
%% - Composition Root の補助として `IStartupArguments`（`StartupArguments` が実装）を新設した。アプリの起動引数（`ArgumentParser`）を保持するだけのサービスで、`AppHost.CreateHost(config, parser)` が値を設定し、`ServiceRegistration` が `ControlService` を生成するときに取り出して渡す。`ControlService` のクラス定義（依存）には変更がないため、図本体には追加していない。詳細は `04-Service-Lifecycle-Spec.md` の 2026-09-25 の注釈と `Phase6-Step7-Plan.md` 決定7。
%% 注釈補強（2026-09-26 Phase6-Step7b 決定3＝案M1 反映）
%% - `IViewModelFactory.CreateProfileSettingsVM` の理想形のシグネチャに `int targetDevice` を追加した。プロファイル編集画面では、編集対象（プロファイル。編集内容は作業領域に保持し、保存・適用を押すまで実機へ反映しない）と、ランブルテスト・ライトバーのプレビュー・校正などで使う実機（`targetDevice`。-1 は実機なし）を分けて渡す。現行コードは `CreateProfileSettingsViewModel(int device, int targetDevice = -1)`（`device` は作業スロット `Global.TEST_PROFILE_INDEX`）で、`CreateRecordBoxViewModel` にも同じ `targetDevice` を追加した。
%% - Step8 以降の MVVM 移設でも、編集スロットと実機を同じ番号で兼用しないこと（兼用すると、編集中の変更が保存前にコントローラーへ即時反映される。Phase6-Status.md K4-3）。詳細は `Phase6-Step7b-Plan.md` §2A 決定3、`04-Service-Lifecycle-Spec.md` §1-3。
%% 注釈補強（2026-10-13 Phase6-Step8 反映）
%% - `ProfileSettingsViewModel` に、編集スロットに対するバックエンド操作（`SaveProfile(profileName)`、`LoadBlankProfile(control)`、キャッシュ更新）を持たせた。保存はコントローラーのスロットへの再適用をしない（再適用は保存の通知［`ProfileSaved`］を受けた `MainWindow` の責務。Step7b 決定8）。`SpecialActionsSubViewModel` には、有効なアクション名を編集スロットの ProfileActions へ反映する `CommitEnabledActionsToProfile()`、名前からの取得（未発見は placeholder）の `GetActionOrPlaceholder`、依存の `IProfileRepository` を追加した。クラス名は理想形の名称で、現行コードでは `SpecialActionsListViewModel` が相当する。
%% - `IProfileRepository` に `LoadBlankProfile`（空のプロファイルの読み込み。既定アクション作成・一時プロファイルのクリアを含む）、`ISpecialActionRepository` に `GetActionOrPlaceholder`（前後の空白と大文字小文字を無視し、未発見は `null` ではなく placeholder を返す）を追加した。どちらも現行は `Global` への薄い委譲。本図にはこれらのインターフェースのメンバー一覧がないため、一覧の変更はない。
%% - View（`ProfileEditor`）は、レイアウト・言語の設定（`IAppSettingsService`）と、編集スロットの LS／RS 補正値・出力種別（`IProfileSettingsService`）をコンストラクタ引数で受け取る（Pure DI）。生成は `MainWindow.ShowProfileEditor` の 1 箇所。詳細は `Phase6-Step8-Plan.md` §0A、`Phase6-Step8-Completion-Report.md`。
%% 注釈補強（2026-10-14 Phase6-Step9 反映）
%% - 主要 4 大 ViewModel（`SettingsViewModel`、`MainWindowsViewModel`、`TrayIconViewModel`、`ProfileSettingsViewModel`）の依存を、コンストラクタの必須引数（Pure DI）にした。`AppHost.GetService`／`Global.*Instance`／`Program.rootHub` のフォールバックは削除。図に `MainWindowsViewModel`・`SettingsViewModel`・`TrayIconViewModel` のクラスを追加し、`ProfileSettingsViewModel` に `IEnvironmentService`・`IPathService` を追加した。
%% - `ViewModelFactory` の依存を、実コードに合わせて明示した（`IServiceProvider` の保持ではなく、必須のコンストラクタ引数）。`ControlService` は `Func<ControlService>` で生成のたびに遅延解決する（Step2 決定D1 と同じ形）。`IViewModelFactory` の契約（`CreateProfileSettingsViewModel(int device, int targetDevice = -1)`）は変更なし。
%% - 追加した契約: `IAppSettingsService`（`UseCustomSteamFolder`、`CustomSteamFolder`、`FakeExeName`、`LogMaxArchiveFiles`）、`IEnvironmentService`（`IsViGEmBusInstalled`、`IsRunningSupportedViGEmBus`、`IsUsingMinViGEm117333`、`AdminNeeded`）、`IPathService.ProgramDirectory`（`Global.exedirpath` と同じ値。`ExecutableDirectory`＝`AppContext.BaseDirectory` とは値が異なりうる）、`IAppearanceSettingsService.BatteryChanged`（購読。発行の `InvokeBatteryChanged` と対）、`IProfileSettingsService.RefreshActionAlias`。いずれも `Global` への薄い委譲。
%% - 詳細は `Phase6-Step9-Plan.md` §0A、`Phase6-Step9-Completion-Report.md`。
%% 注釈補強（2026-10-14 Phase6-Step10-1 反映）
%% - グループ1（起動・環境系）の View／ViewModel（`SaveWhere`、`WelcomeDialog`、`LanguageSelectDialog`、`About`、`UpdaterWindow`、`LanguagePackControl`、`LogViewModel`、`UpdaterWindowViewModel`、`LanguagePackViewModel`、`SettingsViewModel`、`TrayIconViewModel`）の依存を、コンストラクタの必須引数（Pure DI）にした。`LanguagePackControl` は XAML から生成されるため、引数なしコンストラクタを残し `Initialize(IAppSettingsService)` で受け取る。
%% - 追加した契約: `IEnvironmentService`（`IsWin10OrGreater`、`IsWin8OrGreater`、`IsFakerInputInstalled`、`IsHidHideInstalled`［その場で確認。キャッシュ値の `HidHideInstalled` とは別物］、`RefreshViGEmBusInfo`）、`IPathService`（`SetSaveLocation`＝`Global.SaveWhere`［`AppDataPath` の setter とは別物］、`CreateEmptyProfilesFile`＝`Global.SaveDefault`）、`IAppSettingsService`（`LastVersionChecked`、`SetCulture`）、`ILogSettingsApplier`（`LoggerHolder` が実装。DI へは遅延接続の `LogSettingsApplierProxy` を登録し、`App` が `LoggerHolder` の生成直後に `Attach` する）。いずれも `Global` への薄い委譲。
%% - `StartupMethods`（静的クラス）は静的のまま、プログラムフォルダと実行ファイルの場所を引数で受け取る形にした。`TrayIconViewModel` は UI スレッドの `Dispatcher` をコンストラクタの引数で受け取る。
%% - 詳細は `Phase6-Step10-1-Plan.md`。
%% 注釈補強（2026-10-14 Phase6-Step10-2 反映）
%% - グループ2（プロファイル・マッピング系）の View／ViewModel（`ControllersViewModel`、`ControllerListViewModel`、`ControllerRegDeviceOptsViewModel`、`ControllerRegisterOptionsWindow`、`AutoProfiles`、`AutoProfilesViewModel`、`DupBox`、`RenameProfileWindow`、`RenameProfileViewModel`、`BindingWindow`、`BindingWindowViewModel`、`RecordBox`、`RecordBoxViewModel`、`StickCalibrationWindow`、`MappingListViewModel`、`TouchButtonUserControl(ViewModel)`、`PresetOption` 一式、`ControllerReadingsControl`）の依存を、コンストラクタの必須引数（XAML から生成される `AutoProfiles`／`DupBox`／`ControllerReadingsControl` は `Initialize`）にした。`AppHost.GetService`／`Global.*Instance`／`Program.rootHub` のフォールバックは削除。
%% - 追加した契約: `IProfilePresetLoader`（`PresetOption` が使うプリセットの読み込み 10 メソッド。`Global.Load…Profile` への薄い委譲）、`IEditorWindowFactory`（`ProfileEditor`／`BindingWindow`／`RecordBoxWindow`／`SpecialActionEditor` が補助ウィンドウを生成する窓口。実装 `EditorWindowFactory` が必要なサービスを必須引数で保持し、`ControlService` は生成のたびに `Func` で遅延解決する）。`IAppSettingsService`（`UseMoonlight`、`UseAdvancedMoonlight`）、`IAutoProfileService`（`CreateAutoProfilesFile`）。`ViewModelFactory` に `IAppSettingsService` と `Func<IAutoProfileService>` を追加した。
%% - 温存: `RESOURCES_PREFIX`／`ASSEMBLY_RESOURCE_PREFIX`（定数）、`ds4inputNames`／`xboxDefaultNames`／`ds4DefaultNames`／`macroDS4Values`（不変の静的表）、`getX360ControlString`（純粋関数）。
%% - 付随の機能廃止: Flydigi Vader 4 Pro のデバイスサポートを削除した（ユーザー承認。`Phase6-Step10-V4P-Removal-Plan.md`）。
%% - 詳細は `Phase6-Step10-2-Plan.md`。
%% 注釈補強（2026-10-14 Phase6-Step10-3 反映）
%% - グループ3（スペシャルアクション系）の View／ViewModel（`SpecialActionEditor`、`SpecialActEditorViewModel`、`SpecialActionsListViewModel`、`SpecialActions/*` の種別ごとの ViewModel 7 クラス）の依存を、コンストラクタの必須引数（Pure DI）にした。`AppHost.GetService`／`Global.*Instance`／`Program.rootHub` のフォールバックは削除。
%% - 追加した契約: `ISpecialActionRepository.SaveAction`（`Global.SaveAction` への薄い委譲。Actions.xml への保存、読み直しと検証、キー種別のランタイム状態のリセットを含む）、`RemoveActionAndSave`（`Global.RemoveAction` への薄い委譲。ファイルへの保存と読み直しを含む）。メモリ上のリストだけを変える `AddAction`／`RemoveAction` とは動作が別のため、別メソッドにした。`IViewModelFactory.CreateSpecialActionsListViewModel`。`EditorWindowFactory` に `ISpecialActionRepository` を追加した。
%% - 削除: `SpecialActionsListViewModel` の未使用の依存（`IManagedActionManager`、`IOutputSlotService`、`IProfileSettingsService`）。
%% - 温存: `Global.configFileDecimalCulture`（不変の静的値）、`Global.getX360ControlString`（純粋関数）。
%% - 先送り: SA の「キー押下」でコントローラーのボタンを割り当てられない件（`Phase6-Step10-3-Plan.md` §12。Step10-3c で調査）。
%% - 詳細は `Phase6-Step10-3-Plan.md`。
%% 注釈補強（2026-10-15 Phase6-Step10-4 反映）
%% - 削除した型: `IProfileActionChainService`／`ProfileActionChainService`、`IMappingActionDispatcher`／`MappingActionDispatcher`（`InputMappingPipeline` の `_dispatcher` を図から除去）、`AutoProfileChecker`。
%% - コンストラクターの変更（依存は全て必須。null は `ArgumentNullException`）:
%%   - `ProfileApplicationService(IProfileSettingsService, IDeviceStateService, Func<ControlService>, IProfileRepository, IEnvironmentService)`（スロット上限は `ControllerSlotLimit`。K1 の是正）。
%%   - `ProfileRepository(IProfileSettingsService, IProfileXmlStore, IPathService, Func<ControlService>)`。
%%   - `AutoProfileService(AutoProfileHolder, IProfileApplicationService, IProfileSettingsService, IProcessInspector, IEnvironmentService, IAppSettingsService, IProfileRepository, Func<ControlService>)`。
%%   - `OutputSlotService(Func<OutputSlotManager>, IOutputSlotStore, Func<ControlService>)`、`OutputSlotStore(IPathService)`、`OutputSlotManager(Func<Version>)`。
%%   - `DefaultActionFactory(IMacroPlayer, IProcessLauncher, IProfileSwitcher)`、`DefaultActionManager(IActionFactory)`、`DefaultMacroPlayer(IVirtualKBM)`。
%%   - `SpecialActionRepository(BackingStore, IPathService)`、`ProfileSettingsService(BackingStore, IEnvironmentService, Func<ControlService>)`、`ProfileActionProvider(BackingStore)`、`AutoProfileHolder(IPathService)`、`ProfileList(IPathService)`、`ProfileEntity(IPathService)`。
%% - 追加した契約: `IEnvironmentService.ViGEmBusVersion`、`IProfileRepository.ReplaceLinkedProfileName`、`AutoProfileHolder.ReplaceProfileName`。`WindowPlacementHelper.ApplyPlacement` は `IAppSettingsService` を引数に取る。
%% - 温存: `Global.ApplyProfile`（`ProfileApplicationService` の実装本体。Phase7）、静的 `ActionFactory`／`ActionManager` の `ServiceProviderHolder`（D4-4。Phase7／Phase8）。
%% - 詳細は `Phase6-Step10-4-Plan.md`。
%%
%% Phase7-Step2（2026-10-17）: DI インターフェースの、本体の呼び出しが 0 件だったメンバー（削除 51 メンバー）を整理した。詳細は `Phase7-Step2-Plan.md` §2・§8。
%% - `IProfileSettingsService`: 色・配列の重複アクセサ、`ProfileSettingChanged`／`Wire/UnwireSubSettingsEvents`、`ResetToDefaults` ほかを削除。
%% - `IDeviceStateService`: デバイス一覧・接続状態・電池残量（常に空の孤立実装）を削除。初回接続判定とシリアル変更通知だけを持つ。`ControllersViewModel` を削除。
%% - `IOutputSlotService`: `OutputDevices`／`GetOutputDevice`／`IsSlotPlugin` を削除。`IProfileRepository`、`ISpecialActionRepository`（メモリ上だけの `AddAction`／`ReplaceAction`／`RemoveAction`）、`IDs4DeviceRegistry`、`IVirtualKBM`、`IUdpServerService`、`INotificationService`、`IPathService` の孤立・重複メンバーを削除。
%% - 「使うべきだが未接続」として TODO 付きで残した契約: `IEnvironmentService.UsingMaxControllers`、`IDs4DeviceRegistry.ReEnableDevice`、`IOutputSlotService.LoadOutputSlots`／`SaveOutputSlots`、`OutputSlotChanged`（温存）。
%%
%% Phase7-Step3（2026-10-17）: `BackingStore` が DI のシングルトンになり、サービスの構成が変わった（クラス間の関係線は変わらない。依存が `Global` 経由から `BackingStore` の直接注入に変わった）。
%%   - 追加された注入: `ProfileXmlStore(BackingStore)`、`ProfileRepository(BackingStore, IProfileSettingsService, IProfileXmlStore, IPathService, Func<ControlService>)`、`AppSettingsService(BackingStore, IProfileXmlStore, IPathService)`、`AppearanceSettingsService(BackingStore)`、`DeviceStateService(BackingStore)`、`AppNotificationService(BackingStore)`、`AutoProfileService(…, Func<ControlService>, BackingStore)`。
%%   - `IAppSettingsService` の `UDPServerSmoothingMincutoffChanged`／`BetaChanged` の発火元は、`AppSettingsService` のインスタンスになった（`Global` の静的イベントを削除）。
%%   - `Global` から削除した転送メンバー 76 名の一覧は `Phase7Step3RemovedGlobalForwardersGuardTests` を参照。
%%
%% Phase7-Step3c（2026-10-17）: プロファイル適用の構成が変わった（クラス間の関係線の変更は下記のとおり）。
%%   - `ProfileSlotApplier` → `IProfileApplicationService`（新しい依存）。`ProfileApplicationService` → `IProfileXmlStore`／`IProfileActionProvider`／`BackingStore`（新しい依存）。
%%   - `IProfileApplicationService` に `ApplyProfileWithoutHalt` を追加。`IProfileRepository` から `LoadBlankProfile` を削除。`ProfileSettingsViewModel`／`ViewModelFactory` が `IProfilePresetLoader` を受け取る。
%%   - `Global` から、プロファイル適用・プリセット・`SelectedProfileChanged` ほか 24 名を削除した（一覧は `Phase7Step3RemovedGlobalForwardersGuardTests`）。
%%
%% Phase7-Step4（2026-10-17）: 環境・保存場所の構成が変わった。
%%   - `EnvironmentService` が ViGEmBus／HidHide／FakerInput の調べ物（`FindViGEmDeviceInfo`／`CheckForSysDevice` ほか）と導入状態の static 状態を持つ（`Global` の 27 名を削除）。`VirtualKBMFactory` は `EnvironmentService.FakerInputInstalledState` を読む。
%%   - `PathService` が実行ファイルの情報、保存場所（`AppDataPathState`）、初回起動・保存場所が 2 か所の判定、`FindConfigLocation`／`SaveWhere`／空のプロファイル一覧の作成を持つ。`PathService(string appDataPath = null, BackingStore config = null)`（`SaveWhere` が `BackingStore` のファイルパスを更新するため）。`Global` の `appdatapath`／`exedirpath`／`exelocation`／`exeversion` は、`PathService` の状態を指す窓口として残る。
%%   - `AppSettingsService.RunHotPlug` はインスタンスの状態、`FirstRun` は `PathService` の状態。`IDs4DeviceRegistry.ReEnableDevice` は `App`（`-reenableDevice` 分岐）が呼ぶ。`AppHost` は `CreateHost(string[])` を削除し、`CreateHost(config, parser, pathService, environmentService)` でブートストラップのインスタンスを登録する。
%%
%% Phase7-Step5（2026-10-17）: 画面の生成と依存の受け渡しの構成が変わった。
%%   - 新設: `IMainWindowFactory` ← `MainWindowFactory`（`MainWindow` を生成。17 サービスと `Func<ControlService>`／`Func<IAutoProfileService>` を保持）。`IEditorWindowFactory` に `CreateProfileEditor(int)` を追加（`EditorWindowFactory` が `IAppSettingsService`／`IProfileRepository`／`IProfileSwitcher` を保持）。
%%   - `MainWindow`（コンストラクター引数 18）、`ProfileEditor`（コンストラクター引数 8）は `AppHost` を参照しない。`ControlService` は `IElevatedProcessLauncher` を受ける。`BackingStore.ProcessInspector`（`IProcessInspector`）はプロパティ注入。`AppLogger.NotificationService`（`INotificationService`）は静的プロパティ。
%%   - 削除: `Program`（`Program.rootHub`）、`Global.ProfileXmlStoreInstance`／`NotificationServiceInstance`／`SaveControllerConfigs`／`LoadControllerConfigs`、`ControlService.LoadPermanentSlotsConfig`。`ProfileXmlStore` は `BackingStore.Load/SaveControllerConfigsForDevice` を直接呼ぶ。
%%
%% Phase7-Step6（2026-10-17）: 出力デバイス種別の配列の所有権の移動。
%%   - `OutputSlotService` が `OutDevTypeTemp`／`ActiveOutDevType` の配列の実体を持つ（IOutputSlotService の契約は変更なし）。
%%   - `BackingStore` に `ActiveOutDevType`（OutContType[]、プロパティ注入。Step10 でコンストラクター注入へ）を追加。
%%   - 削除: `Global.outDevTypeTemp`、`Global.activeOutDevType`。
%%
%% Phase7-Step7（2026-10-17）: Mapping の状態の保持クラスと、トップレベルへ出した型。
%%   - 新設: SyntheticInputState（globalState／deviceState／syncStateLock を保持）、MacroRuntimeState、StickMotionState。いずれも Shared（暫定の静的な窓口）を持ち、DI の Singleton としても登録される。
%%   - トップレベルへ移した型（旧 Mapping の入れ子）: SyntheticState、PostMapStickData、FlickStickMappingData、TwoStageTriggerMappingData、DeltaSettingsProcessor、DeltaSettingsProcessorGroup、Click。
%%   - 新設: ActionsLoadedRuntimeReset（Actions.xml 読み込み後のリセットの購読者。静的クラス）、BackingStore.ActionsLoaded（イベント）。依存方向: BackingStore → （イベント）→ ActionsLoadedRuntimeReset → 保持クラス。BackingStore から Mapping への参照はない。
%%
%% Phase7-Step8（2026-10-17）: Mapping の instance 化。
%%   - 新設: IMapping（8 メンバーの窓口）、Mapping（IMapping の実装。DI の Singleton）、IKeyButtonControllerRegistry／KeyButtonControllerRegistry（旧 Mapping の静的な登録簿）、ISyntheticDispatcher／SyntheticDispatcherService（旧 静的クラス SyntheticDispatcher の処理）。
%%   - 状態の保持クラス 3 つ（SyntheticInputState／MacroRuntimeState／StickMotionState）の Shared（暫定の静的な窓口）を削除。Mapping／Mouse／ControlService／SyntheticDispatcherService／ActionsLoadedRuntimeReset がコンストラクターで受ける。
%%   - ActionsLoadedRuntimeReset は静的クラスから instance へ（保持クラスをコンストラクターで受ける）。
%%   - OutputKBMHandlerAdapter が出力ハンドラの実体（Handler）を持つ。削除: Global.outputKBMHandler／outputKBMMapping／InitOutputKBMHandler／InitOutputKBMMapping。KBM マッピングは IProfileSettingsService.OutputKBMMapping を使う。
%%   - 静的な SyntheticDispatcher は、Actions サブシステム（Phase8 の範囲）が静的に呼ぶための暫定の橋渡し（状態もフォールバックも持たない。DI が作った instance を Bind で受ける）。Phase8 で削除する。
%%   - DefaultActionManager／DefaultMacroPlayer は Func<IMapping>（遅延）を受ける（ホスト構築時に Mapping を作らない。Phase5-Plan §5.4）。
%%   - 削除: Mapping の AppHost／ServiceProviderHolder／Global のフォールバック、MouseOutputAction の無効な検索、呼び出しが 0 件だった内部メンバー 4 つ（Step8 W1）。
%%   - 詳細は Phase7-Step8-Plan.md、Phase7-Step8-Completion-Report.md、台帳は Phase7-Residual-Ledger.md §11。
%%
%% Phase7-Step9（2026-10-18）: 互換 AppHost の削除と、Mapping の Global 参照の解消。
%%   - 削除: 互換 DS4Windows.AppHost（DI/Compat/AppHost.cs。呼び出し元 0 件）。正本は DS4WinWPF.AppHost のみ。
%%   - 新設: ControllerLimits（コントローラー数・スティックの中点などの定数。静的クラス）、NumericUtil（Clamp。静的クラス）。Mapping と状態の保持クラス 3 つはこれを使い、Global を参照しない。Global の同名の定数は別名、Global.Clamp は委譲するシムとして残す（Step10 で利用箇所を移して削除）。
%%   - MacroRuntimeState: 二重実行防止のロックと確保を TryBeginDispatch／EndDispatch に包んだ（DispatchLock は非公開）。Mapping はフラグの配列もロックも持たない。恒久の再設計（実行層の一本化）は Phase8。
%%   - 詳細は Phase7-Step9-Plan.md、Phase7-Step9-Completion-Report.md、台帳は Phase7-Residual-Ledger.md §12。
%%
%% Phase7-Step10a（2026-10-18）: Global の定数・資源パス・Clamp の再配置。
%%   - Global から削除: MAX_DS4_CONTROLLER_COUNT／TEST_PROFILE_ITEM_COUNT／TEST_PROFILE_INDEX／OLD_XINPUT_CONTROLLER_COUNT／DS4_STICK_AXIS_MIDPOINT（→ ControllerLimits）、ASSEMBLY_RESOURCE_PREFIX／RESOURCES_PREFIX（→ 画面側の ResourcePaths.AssemblyResourcePrefix／ResourcesPrefix）、Clamp（→ NumericUtil.Clamp）。
%%   - 全利用箇所（本体・テスト）を新しい型へ切り替えた。ControlService.MAX_DS4_CONTROLLER_COUNT（公開）と ProfileSettingsService の再宣言は ControllerLimits から値を取る。
%%   - 一時的な参照: Global.iconChoiceResources（トレイアイコンの表）が ResourcePaths の定数を参照している（Step10b で画面側へ移す）。
%%   - 詳細は Phase7-Step10a-Plan.md、Phase7-Step10a-Completion-Report.md、台帳は Phase7-Residual-Ledger.md §13。
%%
%% Phase7-Step10b（2026-10-18）: Global の表・色の関数の再配置と、持ち越しの契約の整理。
%%   - 新設（静的クラス。状態を持たない表・純粋計算）: X360ControlTables（ボタンの既定割り当て・名前の表・getX360ControlString）、DS4ControlNames（DS4 入力の表示名・マクロ記録用の値）、ColorUtil（getTransitionedColor ほか）、TrayIconResources（画面側。トレイアイコンの資源パスの表）。
%%   - Global から削除: 上記の表・関数。BackingStore の 1 引数の getX360ControlString も X360ControlTables へ移した。
%%   - 削除: ProfileSubSettingBase.OnSubPropertyChanged（購読者 0 件）、ProfileApplicationService の未使用の依存 IDeviceStateService、ControlService.USING_MAX_CONTROLLERS（静的シム）、孤立定数 4 件。
%%   - 接続: IEnvironmentService.UsingMaxControllers を AutoProfileHolder（コンストラクター注入）・AutoProfilesViewModel（同）・ProgramItem（生成元が値を渡す）が使う。AutoProfilesDTO.MapFrom は source.UsingMaxControllers を読む。
%%   - 温存: IOutputSlotService.OutputSlotChanged（Phase8 以降の UI 通知の拡張点）。AppearanceSettingsService → TrayIconResources（サービスから画面側の型への参照。Phase8 以降で整理）。
%%   - 詳細は Phase7-Step10b-Plan.md、Phase7-Step10b-Completion-Report.md、台帳は Phase7-Residual-Ledger.md §14。
%%
%% Phase7-Step10c（2026-10-18）: BackingStore の永続化の切り出し（保存形式は変えない）。
%%   - BackingStore は、設定の状態（公開のフィールド・プロパティ）と、名前引き・後処理の補助だけを持つ型に絞った（6,522 行 → 1,660 行）。BackingStore は永続化の型を知らない（依存の向きは、シリアライザー → BackingStore）。
%%   - 新設（DS4Control/Services/、具象クラスの Singleton。BackingStore を store として受ける）: SpecialActionsXmlSerializer（Actions.xml）、LinkedProfilesXmlSerializer（LinkedProfiles.xml）、ControllerConfigsXmlSerializer（ControllerConfigs.xml）、AppSettingsXmlSerializer（Profiles.xml。IPathService・IEnvironmentService）、LegacyProfileXmlSerializer（プロファイル XML の読込・保存。IPathService・IProfileSettingsService・IEnvironmentService）、ProfilePresets（プリセット 10 種）。静的: VersionUtil。
%%   - 呼び出し元: ProfileXmlStore（IProfileXmlStore の実装。排他ロック XmlIoLock を持つ）→ AppSettings／LegacyProfile／ControllerConfigs、ProfileRepository → LinkedProfiles／SpecialActions／LegacyProfile、SpecialActionRepository → SpecialActions、ProfilePresetLoader → ProfilePresets。
%%   - Global のアクション保存系のシム（SaveAction ほか）と CreateStdActions を削除。ActionsLoaded イベントの発行元は BackingStore のまま（SpecialActionsXmlSerializer が RaiseActionsLoaded を呼ぶ）。
%%   - 詳細は Phase7-Step10c-Plan.md、Phase7-Step10c-Completion-Report.md、台帳は Phase7-Residual-Ledger.md §15。
