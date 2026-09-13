# サービスライフサイクル（DI登録）仕様書
**（Service Lifecycle & DI Container Specification）**

本ドキュメントは、`ServiceRegistration.cs` にて `Microsoft.Extensions.DependencyInjection` コンテナに登録される全サービスのライフタイム（寿命）と責務を定義する。

---

## 1. ライフタイム定義ルール

* **Singleton（単一インスタンス）:**
  * アプリ起動から終了まで生存。
  * スレッドセーフであること。
  * デバイス通信ハンドル、永続化キャッシュ、バックグラウンド監視タスクを持つサービスに適用。
* **Transient（一時インスタンス）:**
  * 要求されるたびに新規生成。
  * 画面遷移で開閉されるダイアログや、使い捨ての設定画面 ViewModel に適用。
* **Factory生成（Pattern C）:**
  * 実行時に動的な引数（プロファイル名やスロット番号）が必要な ViewModel に適用。`IViewModelFactory` 経由で解決。

---

## 2. 登録サービス完全一覧表

| 分類 | サービス（インターフェース） | 具象クラス（実装） | ライフタイム | 責務 / 役割 | 主な依存注入引数 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **基盤** | `IPathService` | `PathService` | **Singleton** | アプリデータパス、プロファイル保存パスの動的解決 | なし |
| **基盤** | `IEnvironmentService` | `EnvironmentService` | **Singleton** | OSバージョン、管理者権限有無、ディスプレイ情報 | なし |
| **基盤** | `INotificationService` | `AppNotificationService` | **Singleton** | トースト通知、OS通知の共通発行 | なし |
| **基盤** | `IAppearanceSettingsService` | `AppearanceSettingsService` | **Singleton** | UIテーマ（Dark/Default）、フォントサイズ設定 | `IAppSettingsService` |
| **基盤** | `IAppSettingsService` | `AppSettingsService` | **Singleton** | アプリ全体設定（`AppSettings.xml`）の永続化と状態保持 | `IPathService`, `XmlIoLock` |
| **基盤** | `IProfileXmlStore` | `ProfileXmlStore` | **Singleton** | プロファイルXMLのディスク読み書き（排他ロック内包） | `IPathService`, `XmlIoLock` |
| **基盤** | `IProfileSettingsService` | `ProfileSettingsService` | **Singleton** | 読み込み済みプロファイル設定値のインメモリ管理 | `IProfileXmlStore` |
| **基盤** | `IProfileRepository` | `ProfileRepository` | **Singleton** | プロファイル名一覧の管理、存在確認、CRUD | `IProfileXmlStore`, `IPathService` |
| **基盤** | `ISpecialActionRepository` | `SpecialActionRepository` | **Singleton** | SpecialAction定義（`Actions.xml`）のCRUD・永続化 | `IPathService` |
| **基盤** | `IOutputSlotStore` | `OutputSlotStore` | **Singleton** | スロット番号と仮想パッドの永続化（`OutputSlots.xml`） | `IPathService` |
| **1.入力**| `IDs4DeviceRegistry` | `Ds4DeviceRegistryAdapter` | **Singleton** | 物理HIDコントローラー（DS4/Vader4Pro）の列挙と監視 | なし |
| **1.入力**| `IDeviceStateService` | `DeviceStateService` | **Singleton** | 接続中デバイスのバッテリー残量、BT/USB状態管理 | `IDs4DeviceRegistry` |
| **2.変換**| `IProfileApplicationService`| `ProfileApplicationService` | **Singleton** | スロットへのプロファイル適用・復帰、Halt保護 | `IProfileRepository`, `IOutputSlotService` |
| **2.変換**| `IProfileActionProvider` | `ProfileActionProvider` | **Singleton** | プロファイルに紐づくアクション一覧の解決 | `ISpecialActionRepository` |
| **2.変換**| `IProfileActionChainService` | `ProfileActionChainService` | **Singleton** | アクションの連鎖実行・アンロード制御 | `IProfileActionProvider` |
| **2.変換**| `IAutoProfileService` | `AutoProfileService` | **Singleton** | フォアグラウンドウィンドウ監視と自動切替実行 | `IProcessInspector`, `IProfileApplicationService` |
| **2.変換**| `IMappingActionDispatcher` | `MappingActionDispatcher` | **Singleton** | `Mapping.cs` からのアクション実行要求の中継 | `IManagedActionManager` |
| **2.変換**| `ControlService` | `ControlService` | **Singleton** | 入力監視〜変換〜出力のコアパイプライン統括 | `IDs4DeviceRegistry`, `IProfileSettingsService`, `IOutputSlotService` |
| **3.出力**| `IOutputSlotService` | `OutputSlotService` | **Singleton** | 仮想Xbox360/DS4パッド（ViGEm）の生成・アタッチ・切替 | `IOutputSlotStore` |
| **3.出力**| `IVirtualKBM` | `OutputKBMHandlerAdapter` | **Singleton** | SendInput / FakerInput 経由のキー・マウス送出 | なし |
| **3.出力**| `IMacroPlayer` | `DefaultMacroPlayer` | **Singleton** | マクロの時系列非同期再生エンジン | `IVirtualKBM` |
| **3.出力**| `IActionFactory` | `DefaultActionFactory` | **Singleton** | アクション実行インスタンス（Key/Macro/Launch）の生成 | `IVirtualKBM`, `IMacroPlayer`, `IProcessLauncher` |
| **3.出力**| `IProcessLauncher` | `DefaultProcessLauncher` | **Singleton** | 通常プロセスの起動 | なし |
| **3.出力**| `IElevatedProcessLauncher` | `DefaultElevatedProcessLauncher` | **Singleton** | UAC昇格を伴う外部ツールの起動 | なし |
| **3.出力**| `IProcessInspector` | `DefaultProcessInspector` | **Singleton** | 実行中プロセスのパス・ウィンドウタイトル調査 | なし |
| **3.出力**| `IProfileSwitcher` | `DefaultProfileSwitcher` | **Singleton** | アクションからのプロファイル切替実行 | `IProfileApplicationService` |
| **3.出力**| `IUdpServerService` | `UdpServerService` | **Singleton** | Cemuhook 互換 UDP モーションデータ送信 | `IAppSettingsService` |
| **4.UI** | `IViewModelFactory` | `ViewModelFactory` | **Singleton** | パラメータ付きViewModel（Pattern C）生成ファクトリ | `IServiceProvider` |
| **4.UI** | `MainWindowsViewModel` | `MainWindowsViewModel` | **Singleton** | メインウィンドウ（ステータスバー、タブ統括）の状態保持 | `IAppSettingsService`, `ControlService` |
| **4.UI** | `ControllersViewModel` | `ControllersViewModel` | **Singleton** | コントローラー接続スロット一覧画面の状態保持 | `IDs4DeviceRegistry`, `IProfileApplicationService` |
| **4.UI** | `SettingsViewModel` | `SettingsViewModel` | **Transient** | 設定画面（開くたびに最新値を読み直して生成） | `IAppSettingsService`, `IAppearanceSettingsService` |
| **4.UI** | `LogViewModel` | `LogViewModel` | **Transient** | ログ表示画面 | なし |
| **4.UI** | `AboutViewModel` | `AboutViewModel` | **Transient** | バージョン・クレジット表示画面 | `IEnvironmentService` |
| **4.UI** | `ProfileSettingsViewModel` | `ProfileSettingsViewModel` | **Factory生成 (Pattern C)** | 個別プロファイル編集画面 | `IProfileSettingsService`, `IProfileXmlStore` |
| **4.UI** | `SpecialActEditorViewModel` | `SpecialActEditorViewModel` | **Factory生成 (Pattern C)** | スペシャルアクション設定・編集ダイアログ | `ISpecialActionRepository`, `IDs4DeviceRegistry` |

---

## 3. リソースの破棄（Dispose）ガイドライン

1. **`IDisposable` の自動連鎖：**
   * コンテナによって生成された Singleton サービス（`ControlService`, `OutputSlotService`, `UdpServerService` 等）は、アプリ終了時（`App.OnExit`）にコンテナが破棄されるのと連動して、安全な順序で `Dispose()` が呼び出される。
2. **スレッド/タスクの安全な停止：**
   * HID読み取りループやViGEmデバイスハンドルを持つサービスは、`Dispose()` 内で確実に `CancellationTokenSource.Cancel()` を発行し、スレッドの合流（Join / Wait）を待機してからネイティブハンドルを閉じる。