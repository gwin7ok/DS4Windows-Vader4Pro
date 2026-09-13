# クラス図 / インターフェース関連図
**（Class & Interface Specification）**

本ドキュメントは、DI化されたコア機能のインターフェース設計、および主要クラスの関連性を定義する。

---

## 1. 主要クラス・インターフェース関連図

```mermaid
classDiagram
    %% ==============================
    %% インターフェース定義
    %% ==============================
    class IDs4DeviceRegistry {
        <<interface>>
        +getDevices() IEnumerable~DS4Device~
        +findDeviceByMac(string mac) DS4Device
        +startScan()
        +stopScan()
    }

    class IProfileApplicationService {
        <<interface>>
        +ApplyProfile(int slot, string profileName, bool isTemporary) bool
        +RestorePreviousProfile(int slot) bool
        +ClearSlot(int slot)
    }

    class IMappingActionDispatcher {
        <<interface>>
        +DispatchMappingAction(int slot, TriggerContext ctx) bool
        +SuppressActionsForSlot(int slot)
    }

    class IManagedActionManager {
        <<interface>>
        +DispatchTriggerEstablished(Action action, TriggerContext ctx) bool
        +DispatchTriggerReleased(Action action, TriggerContext ctx) bool
        +HaltAllActions(int slot)
    }

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

    class IMacroPlayer {
        <<interface>>
        +PlayMacro(int slot, MacroStep[] steps)
        +StopMacro(int slot)
    }

    class IProfileXmlStore {
        <<interface>>
        +LoadProfileXml(string path) ProfileDTO
        +SaveProfileXml(string path, ProfileDTO dto) bool
    }

    class IAppSettingsService {
        <<interface>>
        +Load() AppSettingsDTO
        +Save(AppSettingsDTO dto) bool
        +GetSetting~T~(string key) T
    }

    class IViewModelFactory {
        <<interface>>
        +CreateProfileSettingsVM(string profileName) ProfileSettingsViewModel
        +CreateSpecialActEditorVM(int slot) SpecialActEditorViewModel
    }

    %% ==============================
    %% 具象クラス定義
    %% ==============================
    class ControlService {
        -IDs4DeviceRegistry _deviceRegistry
        -IProfileSettingsService _profileSettings
        -IOutputSlotService _outputSlotService
        -IMappingActionDispatcher _dispatcher
        +Start()
        +Stop()
        +UpdateInputLoop()
    }

    class Ds4DeviceRegistryAdapter {
        -List~DS4Device~ _activeDevices
        +getDevices() IEnumerable~DS4Device~
        +findDeviceByMac(string mac) DS4Device
    }

    class ProfileApplicationService {
        -IProfileRepository _repository
        -IProfileActionChainService _actionChain
        -IOutputSlotService _slotService
        +ApplyProfile(int slot, string profileName, bool isTemporary) bool
        +RestorePreviousProfile(int slot) bool
    }

    class OutputSlotService {
        -OutputSlotManager _slotManager
        -IOutputSlotStore _slotStore
        +AttachDevice(int slot, OutContType type) bool
        +DetachDevice(int slot) bool
    }

    class MappingActionDispatcher {
        -IManagedActionManager _actionManager
        +DispatchMappingAction(int slot, TriggerContext ctx) bool
    }

    class OutputKBMHandlerAdapter {
        +PerformKeyPress(uint key, bool useScan)
        +MoveMouse(int dx, int dy)
    }

    class ProfileXmlStore {
        -XmlIoLock _fileLock
        +LoadProfileXml(string path) ProfileDTO
        +SaveProfileXml(string path, ProfileDTO dto) bool
    }

    class ViewModelFactory {
        -IServiceProvider _serviceProvider
        +CreateProfileSettingsVM(string profileName) ProfileSettingsViewModel
    }

    %% ==============================
    %% 関係性 (実線: 依存, 点線: 実装)
    %% ==============================
    IDs4DeviceRegistry <|.. Ds4DeviceRegistryAdapter : implements
    IProfileApplicationService <|.. ProfileApplicationService : implements
    IOutputSlotService <|.. OutputSlotService : implements
    IMappingActionDispatcher <|.. MappingActionDispatcher : implements
    IVirtualKBM <|.. OutputKBMHandlerAdapter : implements
    IProfileXmlStore <|.. ProfileXmlStore : implements
    IViewModelFactory <|.. ViewModelFactory : implements

    ControlService --> IDs4DeviceRegistry : uses
    ControlService --> IOutputSlotService : uses
    ControlService --> IProfileApplicationService : uses
    ControlService --> IMappingActionDispatcher : uses

    ProfileApplicationService --> IOutputSlotService : uses
    ProfileApplicationService --> IProfileXmlStore : uses
    MappingActionDispatcher --> IManagedActionManager : uses
    IManagedActionManager --> IVirtualKBM : uses
    IManagedActionManager --> IMacroPlayer : uses

    ViewModelFactory --> IProfileApplicationService : injects
    ViewModelFactory --> IAppSettingsService : injects
```

---

## 2. 適用されたデザインパターン

1. **Adapter パターン:**
   * `Ds4DeviceRegistryAdapter`: レガシーな静的デバイス管理と新インターフェース `IDs4DeviceRegistry` の橋渡し。
   * `OutputKBMHandlerAdapter`: Win32ネイティブ `SendInput` および `FakerInput` を `IVirtualKBM` として統一ラップ。
2. **Abstract Factory パターン:**
   * `IViewModelFactory`: パラメータ（プロファイル名やスロット番号）を受け取って ViewModel を生成する純粋なDIファクトリ。
3. **Repository パターン:**
   * `IProfileRepository` / `ISpecialActionRepository`: ドメイン層に対してデータ永続化（XML読み書き）の詳細を隠蔽。
4. **Dispatcher パターン:**
   * `IMappingActionDispatcher`: 毎フレームの高速マッピング処理から非同期アクション（マクロ実行、プロセス起動）を分離・安全に委譲。