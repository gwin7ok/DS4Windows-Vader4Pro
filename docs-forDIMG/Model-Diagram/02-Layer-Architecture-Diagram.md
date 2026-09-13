# レイヤー構成図（スタック図）
**（Layered Stack Architecture Specification）**

本ドキュメントは、巨大モノリスファイル解体（`ScpUtil`, `Mapping`, `ControlService`, `ProfileSettingsViewModel` 等の1クラス1ファイル化）を反映した、DS4Windows-Vader4Pro の実行時スタック構造、データパイプライン、およびレイヤー間の責務境界を定義する。

---

## 1. 実行時スタック構造図

```text
┌──────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                   第4層: UI層 (Presentation Layer)                               │
│  WPF Views (MainWindow, ProfileEditor, BindingWindow, etc.)                                      │
│  ├── Singleton ViewModels: MainWindowsViewModel / ControllersViewModel                           │
│  ├── Transient ViewModels: SettingsViewModel / LogViewModel / AboutViewModel                     │
│  └── ProfileEditor 分割ViewModels (親コンテナ ＋ サブVM群):                                       │
│       ProfileSettingsViewModel (親)                                                              │
│       ├── StickSettingsSubVM (スティック感度/軸/カーブ)     TriggerSettingsSubVM (トリガー/モーター) │
│       ├── ButtonMappingSubVM (ボタンリマップ/シフト)       SpecialActionsSubVM (アクション管理)   │
│       └── IViewModelFactory (動的パラメータ注入・サブVM生成)                                      │
└────────────────────────────────────────────────┬─────────────────────────────────────────────────┘
                                                 │ 依存 (UIバインド・要求発行・イベント購読)
                                                 ▼
┌──────────────────────────────────────────────────────────────────────────────────────────────────┐
│                               第2層: 信号変換層 (Domain & Application Core)                       │
│  【ループ統括】 ControlService (ファサード) ──> IInputLoopCoordinator (高速ループ実行エンジン)       │
│  【状態・切替】 IProfileApplicationService (適用/復帰/Halt) / IAutoProfileService (自動切替)     │
│  【アクション】 IManagedActionManager (アクション実行・トグル) ──> IMappingActionDispatcher        │
│  【マッピング】 IInputMappingPipeline (変換パイプライン親)                                        │
│                 ├── IButtonProcessor (ボタン変換・シフト)   IStickProcessor (軸/カーブ/デッドゾーン)│
│                 ├── ITriggerProcessor (2段階トリガー)      ITouchGyroProcessor (タッチ/ジャイロ)  │
│                 └── IMouseEngine (Mouse.cs解体先: マウス加速度/OneEuroFilter平滑化/Trackball)     │
└───────────────────────┬──────────────────────────────────────────────────┬───────────────────────┘
                        │                                                  │
          依存 (生データ要求)                                               │ 依存 (変換後データ送出)
                        ▼                                                  ▼
┌───────────────────────────────────────────────┐  ┌───────────────────────────────────────────────┐
│          第1層: 入力監視層 (Ingress)            │  │          第3層: 信号出力層 (Egress)            │
│  IDs4DeviceRegistry (デバイス検出・管理)       │  │  3-a 仮想パッド: IOutputSlotService           │
│  ├─ IDeviceHotplugMonitor (Win32挿抜検知)     │  │  │   └─ ViGEm Client (Xbox360 / DS4 OutDevice) │
│  └─ Input Device Units (生HIDレポート読取)     │  │  3-b KBM/マクロ: IVirtualKBM (SendInput/Faker)│
│     ├─ DS4Device (DualShock 4)                │  │  │   └─ IMacroPlayer (非同期時系列マクロ再生)  │
│     ├─ Vader4ProDevice (Flydigi)              │  │  3-c 副作用実行:                               │
│     └─ DualSenseDevice / JoyConDevice         │  │      ├─ IProcessLauncher / ElevatedLauncher    │
│                                               │  │      ├─ IProfileSwitcher (アクション内切替)     │
│                                               │  │      ├─ IUdpServerService (Cemuhook Motion)    │
│                                               │  │      └─ ILightbarService (LED/発光色計算)      │
└───────────────────────────────────────────────┘  └───────────────────────────────────────────────┘
  ▲                                                  │
  │ 【データパイプライン (Data Pipeline: 250Hz〜1000Hz)】 │
  └───────────── 生HIDパケット ───────────> 変換・補正 ───┴───────────> 仮想ゲームパッド / OS入力
                                              │
                                              ▼
┌──────────────────────────────────────────────────────────────────────────────────────────────────┐
│                          横断基盤・永続化層 (Cross-Cutting Infrastructure)                        │
│  【設定・プロファイル】 IProfileRepository / IProfileXmlStore (Profile.xml CRUD・シリアライズ)    │
│  【アプリ設定】         IAppSettingsService (AppSettings.xml 永続化)                              │
│  【スロット/デバイス】  IOutputSlotStore (OutputSlots.xml) / IDeviceOptionRepository (*OptsDTO)    │
│  【排他制御・OS基盤】   XmlIoLock (プロセス内/スレッド間ロック) / IPathService / IEnvironmentService│
└──────────────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. 各層の責務詳細と巨大ファイル解体マッピング

| 層番号 | レイヤー名 | 主な構成要素（解体後クラス群） | 責務と役割 |
| :--- | :--- | :--- | :--- |
| **第4層** | **UI層**<br>(Presentation) | Views (XAML), ViewModels, Sub-ViewModels, `IViewModelFactory` | **画面表示とユーザー入力の受付。**<br>・`ProfileSettingsViewModel` を親とし、各タブ単位で `StickSettingsSubVM`、`TriggerSettingsSubVM` 等に分割。<br>・各サブVMは単一の関心事のみを受け持ち、巨大コードビハインド（`ProfileEditor.xaml.cs`）を撤廃。 |
| **第2層** | **信号変換層**<br>(Domain/App) | `ControlService`, `IInputLoopCoordinator`, `IInputMappingPipeline`, 各Processors, `ActionManager` | **アプリケーションの頭脳（変換と分配）。**<br>・`Mapping.cs`（443KB）を `IInputMappingPipeline` と5つの専門Processorに分解。<br>・`Mouse.cs`（78KB）を `IMouseEngine` に独立化。<br>・`ControlService.cs`（145KB）からループ実行を `IInputLoopCoordinator` として切り出し。 |
| **第1層** | **入力監視層**<br>(Ingress) | `IDs4DeviceRegistry`, `IDeviceHotplugMonitor`, `DS4Device`, `Vader4ProDevice` | **物理コントローラーとの通信と切断検知。**<br>・USB/Bluetooth HID レポートの常時読み取り。<br>・Win32 RAW/デバイス到着イベントを `IDeviceHotplugMonitor` で検知。 |
| **第3層** | **信号出力層**<br>(Egress) | `IOutputSlotService`, `IVirtualKBM`, `IMacroPlayer`, `ILightbarService`, 各Launchers | **変換済みデータのOS側への反映・物理フィードバック。**<br>・ViGEm経由の仮想Xbox360/DS4パッド出力。<br>・SendInput/FakerInput によるキーボード・マウスエミュレーション。<br>・LED発光色計算を `ILightbarService` として独立化。 |
| **基盤** | **横断基盤・永続化層**<br>(Infrastructure) | `ProfileXmlStore`, `AppSettingsService`, `DeviceOptionRepository`, `OutputSlotStore`, `XmlIoLock` | **ファイルIOと排他制御（`ScpUtil.cs` / `Global` の解体先）。**<br>・XMLファイルのディスク読み書きと、`XmlIoLock` による完全なスレッドセーフ化。<br>・各DTO（`ProfileDTO`, `AppSettingsDTO`, `*ControllerOptsDTO`）の管理。 |

---

## 3. 「データフロー」と「依存関係」の比較対比

```text
【データの流れる方向 (Data Flow - 毎秒250〜1000回)】
  [物理コントローラー]
         │ (生HID入力)
         ▼
  [1. 入力監視層] (IDs4DeviceRegistry / 各Device)
         │ (InputReport)
         ▼
  [2. 信号変換層] (IInputMappingPipeline ──> 各Processor)
         │ (Transformed State / Actions)
         ▼
  [3. 信号出力層] (IOutputSlotService / IVirtualKBM)
         │ (仮想パッド / キーマウ)
         ▼
     [PCゲーム / OS]

【プログラムの依存方向 (Dependency Flow - DIP原則)】
  [4. UI層 (Views / ViewModels)]
         │
         │ (利用)
         ▼
  [2. 信号変換層] ────────────┐
   │            │             │
   │ (利用)     │ (利用)      │ (利用)
   ▼            ▼             ▼
  [1. 入力監視層] [3. 信号出力層] [横断基盤・永続化層]
```

### ホットパス（Hot Path）の実行時原則
* 毎秒250〜1000回呼び出される入力ループ（`IInputLoopCoordinator` $\rightarrow$ `IInputMappingPipeline` $\rightarrow$ `IOutputSlotService`）内では、**ヒープアロケーション（`new`）およびDIコンテナ解決（`GetService`）を一切行わない**。
* すべてのプロセッサや出力サービスは Singleton として初期化時に注入され、キャッシュされた参照経由で直接処理を実行する。