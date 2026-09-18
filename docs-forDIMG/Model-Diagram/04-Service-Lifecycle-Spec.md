# サービスライフサイクル（DI登録）仕様書
**（Service Lifecycle & DI Container Specification）**

本ドキュメントは、巨大ファイルの分解、ステートレスパイプライン、およびUI Mediator構成を反映した、各コンポーネントのライフタイム（寿命）、責務、および依存注入（DI）関係を定義する。すべての登録は `ServiceRegistration.cs` に集約される。

---

## 1. ライフタイム定義ポリシー

1. **Singleton（単一インスタンス）:**
   * アプリ起動から終了まで常駐するサービス。
   * 入力監視、デバイスレジストリ、マッピングパイプラインおよび各プロセッサ、仮想コントローラー出力、ファイル永続化リポジトリなど、スレッドセーフで状態を保持する全コアコンポーネントに適用。
   * **ホットパスのステートレス化：** プロセッサ（`StickProcessor` 等）はステートレスSingletonとし、状態はスロット別バッファ `MappingPipelineContext` 経由で受け渡す。
2. **Transient（一時インスタンス）:**
   * 要求されるたびに新しく生成されるサービス。
   * 開くたびに最新の設定値を読み込み直すダイアログ・独立画面（`SettingsViewModel`, `LogViewModel`, `AboutViewModel` 等）に適用。
3. **Factory生成（Pattern C / Composite Mediator）:**
   * `ProfileSettingsViewModel` などのパラメータ（プロファイル名）を伴う複合ViewModel。`IViewModelFactory` 経由で生成され、配下の各サブViewModel（`StickSubVM` 等）に必要な依存サービスを自動伝播してインスタンス化する。

---

## 2. 登録サービス完全一覧表

| 分類 | サービス（インターフェース） | 具象クラス（実装） | ライフタイム | 責務 / 役割 | 主な依存注入引数 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **基盤** | `IPathService` | `PathService` | **Singleton** | プロファイル・アプリデータ保存先パスの動的解決 | なし |
| **基盤** | `IEnvironmentService` | `EnvironmentService` | **Singleton** | OS種別、管理者権限、ディスプレイ解像度情報の提供 | なし |
| **基盤** | `INotificationService` | `AppNotificationService` | **Singleton** | OSトースト通知、ステータス通知の統一発行 | なし |
| **基盤** | `XmlIoLock` | `XmlIoLock` | **Singleton** | プロセス内・スレッド間のファイル排他制御ロック | なし |
| **基盤** | `IAppSettingsService` | `AppSettingsService` | **Singleton** | `AppSettings.xml` の永続化・設定値管理 | `IPathService`, `XmlIoLock` |
| **基盤** | `IAppearanceSettingsService`| `AppearanceSettingsService` | **Singleton** | UIテーマ（Dark/Default）、フォントスケール | `IAppSettingsService` |
| **基盤** | `IProfileXmlStore` | `ProfileXmlStore` | **Singleton** | プロファイルXMLのシリアライズ・デシリアライズ | `IPathService`, `XmlIoLock` |
| **基盤** | `IProfileRepository` | `ProfileRepository` | **Singleton** | プロファイル名一覧の保持、CRUD、存在検証 | `IProfileXmlStore`, `IPathService` |
| **基盤** | `IProfileSettingsService` | `ProfileSettingsService` | **Singleton** | ロード済みプロファイルのインメモリ設定値管理 | `IProfileXmlStore` |
| **基盤** | `IDeviceOptionRepository` | `DeviceOptionRepository` | **Singleton** | 機種別オプション（`*ControllerOptsDTO`）の管理 | `IPathService`, `XmlIoLock` |
| **基盤** | `ISpecialActionRepository` | `SpecialActionRepository` | **Singleton** | `Actions.xml` のCRUDおよび定義管理 | `IPathService`, `XmlIoLock` |
| **基盤** | `IOutputSlotStore` | `OutputSlotStore` | **Singleton** | スロット永続化設定（`OutputSlots.xml`）の管理 | `IPathService`, `XmlIoLock` |
| **1.入力**| `IDeviceHotplugMonitor` | `DeviceHotplugMonitor` | **Singleton** | Win32 RAW/HID デバイス到着・抜去イベントの検知 | なし |
| **1.入力**| `IDs4DeviceRegistry` | `Ds4DeviceRegistryAdapter` | **Singleton** | コントローラーの列挙・保持、通信ハンドルの監視 | `IDeviceHotplugMonitor` |
| **1.入力**| `IDeviceStateService` | `DeviceStateService` | **Singleton** | 接続中デバイスのバッテリー残量、通信種別管理 | `IDs4DeviceRegistry` |
| **1.入力**| `IInputReportParserFactory` | `InputReportParserFactory` | **Singleton** | デバイス種別に応じたパケット解析器（DS4/Vader等）の提供 | なし |
| **2.変換**| `IButtonProcessor` | `ButtonProcessor` | **Singleton** | ボタンリマップ、シフト変調（ステートレス計算） | なし |
| **2.変換**| `IStickProcessor` | `StickProcessor` | **Singleton** | デッドゾーン、カーブ（`StickOutCurve`）、AntiSnap計算 | なし |
| **2.変換**| `ITriggerProcessor` | `TriggerProcessor` | **Singleton** | トリガー2段階判定、モーター制御値計算 | なし |
| **2.変換**| `ITouchGyroProcessor` | `TouchGyroProcessor` | **Singleton** | タッチパッドジェスチャー、ジャイロ計算 | なし |
| **2.変換**| `IMouseEngine` | `MouseEngine` | **Singleton** | `Mouse.cs`解体先: マウスカーソル加速、OneEuroFilter平滑化 | なし |
| **2.変換**| `IInputMappingPipeline` | `InputMappingPipeline` | **Singleton** | スロット別 `MappingPipelineContext` を各Processorへ適用統括 | 各Processor群, `IMappingActionDispatcher` |
| **2.変換**| `IInputLoopCoordinator` | `InputLoopCoordinator` | **Singleton** | 毎秒250〜1000回の高速入力ポーリングループ実行 | `IDs4DeviceRegistry`, `IInputMappingPipeline` |
| **2.変換**| `IProfileApplicationService`| `ProfileApplicationService` | **Singleton** | プロファイルのスロット適用・復帰、Halt保護 | `IProfileRepository`, `IOutputSlotService` |
| **2.変換**| `IProfileActionProvider` | `ProfileActionProvider` | **Singleton** | プロファイルに紐づくアクション定義の解決 | `ISpecialActionRepository` |
| **2.変換**| `IProfileActionChainService` | `ProfileActionChainService` | **Singleton** | アクションの連鎖実行・ライフサイクル管理 | `IProfileActionProvider` |
| **2.変換**| `IAutoProfileService` | `AutoProfileService` | **Singleton** | フォアグラウンドアプリ監視とプロファイル自動切替 | `IProcessInspector`, `IProfileApplicationService` |
| **2.変換**| `IMappingActionDispatcher` | `MappingActionDispatcher` | **Singleton** | パイプラインからのアクション発火要求の非同期分配 | `IManagedActionManager` |
| **2.変換**| `IManagedActionManager` | `DefaultActionManager` | **Singleton** | マクロ・キー・プロファイル切替アクションの実行統括 | `IActionFactory` |
| **2.変換**| `ControlService` | `ControlService` | **Singleton** | 入力・変換・出力パイプライン全体の開始・停止統括（ファサード） | `IDs4DeviceRegistry`, `IInputLoopCoordinator`, `IOutputSlotService` |
| **3.出力**| `IOutputSlotService` | `OutputSlotService` | **Singleton** | 仮想Xbox360/DS4（ViGEm）の生成、割当、切替 | `IOutputSlotStore` |
| **3.出力**| `IVirtualKBM` | `OutputKBMHandlerAdapter` | **Singleton** | SendInput / FakerInput によるキー・マウス送出 | なし |
| **3.出力**| `IMacroPlayer` | `DefaultMacroPlayer` | **Singleton** | 時系列非同期マクロの再生・停止管理 | `IVirtualKBM` |
| **3.出力**| `ILightbarService` | `LightbarService` | **Singleton** | バッテリー・プロファイル色に応じたLED発光計算 | なし |
| **3.出力**| `IActionFactory` | `DefaultActionFactory` | **Singleton** | 各種アクションインスタンス（Key/Macro/Launch）の生成 | `IVirtualKBM`, `IMacroPlayer`, `IProcessLauncher` |
| **3.出力**| `IProcessLauncher` | `DefaultProcessLauncher` | **Singleton** | 外部プロセスの通常起動 | なし |
| **3.出力**| `IElevatedProcessLauncher` | `DefaultElevatedProcessLauncher` | **Singleton** | UAC昇格を伴う外部ツールの起動 | なし |
| **3.出力**| `IProcessInspector` | `DefaultProcessInspector` | **Singleton** | 実行中プロセスのパス・ウィンドウ名調査 | なし |
| **3.出力**| `IProfileSwitcher` | `DefaultProfileSwitcher` | **Singleton** | アクションからのプロファイル切替実行 | `IProfileApplicationService` |
| **3.出力**| `IUdpServerService` | `UdpServerService` | **Singleton** | Cemuhook 互換 UDP モーションデータ配信 | `IAppSettingsService` |
| **4.UI** | `IViewModelFactory` | `ViewModelFactory` | **Singleton** | パラメータ付きViewModel / 複合ViewModelの生成ファクトリ | `IServiceProvider` |
| **4.UI** | `MainWindowsViewModel` | `MainWindowsViewModel` | **Singleton** | メインウィンドウ（ヘッダー、タブ切替、ステータス） | `IAppSettingsService`, `ControlService` |
| **4.UI** | `ControllersViewModel` | `ControllersViewModel` | **Singleton** | コントローラー接続スロット一覧表示画面 | `IDs4DeviceRegistry`, `IProfileApplicationService` |
| **4.UI** | `SettingsViewModel` | `SettingsViewModel` | **Transient** | アプリ全般設定画面（開くたびに最新値取得） | `IAppSettingsService`, `IAppearanceSettingsService` |
| **4.UI** | `LogViewModel` | `LogViewModel` | **Transient** | ログ表示画面 | なし |
| **4.UI** | `AboutViewModel` | `AboutViewModel` | **Transient** | バージョン・クレジット表示画面 | `IEnvironmentService` |
| **4.UI** | `ProfileSettingsViewModel` | `ProfileSettingsViewModel` | **Factory生成 (親Mediator)**| プロファイル編集画面（配下の各サブVMを調停・集約） | サブVM群, `IProfileSettingsService` |
| **4.UI** | `StickSettingsSubViewModel` | `StickSettingsSubViewModel` | **Factory生成 (サブVM)** | スティック感度・カーブ・デッドゾーン設定タブ | `IProfileSettingsService` |
| **4.UI** | `TriggerSettingsSubViewModel`| `TriggerSettingsSubViewModel`| **Factory生成 (サブVM)** | トリガー感度・2段階設定・モーター設定タブ | `IProfileSettingsService` |
| **4.UI** | `ButtonMappingSubViewModel` | `ButtonMappingSubViewModel` | **Factory生成 (サブVM)** | ボタン割り当て・シフトモディファイア設定タブ | `IProfileSettingsService` |
| **4.UI** | `SpecialActionsSubViewModel`| `SpecialActionsSubViewModel`| **Factory生成 (サブVM)** | スペシャルアクション一覧・登録タブ | `ISpecialActionRepository`, `IDs4DeviceRegistry` |

---

## 3. リソース破棄と安全な終了手順

1. **破棄の依存順序制御（LIFO / 逆順破棄）:**
   * アプリ終了時、`AppHost` が破棄される際に、依存している側（上位）から順に `Dispose()` が呼ばれる。
   * `ControlService` / `InputLoopCoordinator` が停止してスレッドを安全に閉じた後に、`IDs4DeviceRegistry`（HIDデバイスハンドル）や `OutputSlotService`（ViGEmクライアント）が破棄されるため、リソース解放時の競合クラッシュが発生しない。
2. **ホットパスサービスのステートレス設計:**
   * `ButtonProcessor`、`StickProcessor` などの各変換プロセッサは、内部にミュータブルな状態を持たず、パイプラインコンテキスト `MappingPipelineContext` 経由で受け渡すステートレス設計とすることで、Singleton でありながら完全なスレッドセーフとゼロGCを両立する。

%% 注釈補強（2026-09-18監査結果反映、構造変更なし）
%% - ライフタイム定義（Singleton/Transient/Factory生成）は現状のDI登録（ServiceRegistration.cs）と前提条件（Phase5完了）と一致。
%% - Singleton サービス（ControlService, InputLoopCoordinator, 各Processor, IProfileApplicationService等）のステートレス設計とZero-GC原則は現状監査（762参照、重複ゼロ・漏れゼロ証明済み）と一致。
%% - Transient サービス（SettingsViewModel, LogViewModel, AboutViewModel）の最新値取得原則は現状のUI層（App/MainWindow/ProfileEditor/SettingsVM、計273件）と一致。
%% - Factory生成（ProfileSettingsViewModel + SubVM群）のMediator調停構成は現状のProfileEditor（96件、契約差含む）と一致。契約差（ProfileApp契約差、SpecialAction編集契約差）はb要設計として記録済み（ABC-Classification.md 参照）。
%% - リソース破棄順序（LIFO/逆順破棄）は現状のガードレール（ドライバ破棄順序、切断時クリーンアップ）と一致。未適用項目はStep2以降に適用（Phase6-Status.md 参照）。
%% - c（除外 ≈ 304件）の改修対象外原則を維持（Classification-Metrics.md, C-Classification-Count.md 参照）。a（契約候補）とb（要設計 ≈ 458件）が改修対象。
%% - Phase5証跡（Step14/15未完了）の内容検証は別途必要（Phase5-Evidence-Check.md 参照）。本監査（Phase6-Step1）は中間成果として記録（報告書§7維持：Step2承認根拠には使用しない）。