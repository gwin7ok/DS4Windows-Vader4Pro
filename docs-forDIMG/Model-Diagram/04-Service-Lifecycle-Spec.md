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
   * プロファイル編集画面では、編集対象（プロファイル）と、プレビュー・校正に使う実機（`targetDevice`。-1 は実機なし）を分けて渡す。編集内容は作業領域（現行コードでは作業スロット `Global.TEST_PROFILE_INDEX`）に保持し、保存・適用を押すまで実機へは反映しない。実機を使うのは、ランブルテスト、ライトバーのプレビュー、入力読み取り表示、スティック・ジャイロ・ステアリングホイールの校正、マクロ記録など、一時的な確認機能だけとする（2026-09-26 Phase6-Step7b 決定3）。

---

## 2. 登録サービス完全一覧表

| 分類 | サービス（インターフェース） | 具象クラス（実装） | ライフタイム | 責務 / 役割 | 主な依存注入引数 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **基盤** | `IPathService` | `PathService` | **Singleton** | プロファイル・アプリデータ保存先パスの動的解決。プログラムフォルダ（`ProgramDirectory`。ジャンクション解決済みの実行ファイルの親フォルダで、`ExecutableDirectory`＝`AppContext.BaseDirectory` とは別） | なし |
| **基盤** | `IEnvironmentService` | `EnvironmentService` | **Singleton** | OS種別、管理者権限、ディスプレイ解像度情報、HidHide／FakerInput の導入状態、デバイスインスタンスID解決、コントローラースロット上限（同時に扱えるスロット数の上限。接続台数ではない）、ViGEmBus の導入・サポートバージョン・最小バージョン判定、プログラムフォルダへの書き込みに管理者権限が要るかの判定の提供 | なし |
| **基盤** | `INotificationService` | `AppNotificationService` | **Singleton** | OSトースト通知、ステータス通知の統一発行 | なし |
| **基盤** | `IStartupArguments` | `StartupArguments` | **Singleton** | アプリの起動引数（`ArgumentParser`）の保持役。`AppHost.CreateHost(config, parser)` が値を設定し、`ControlService` の生成時に渡す（Phase6-Step7-4） | なし |
| **基盤** | `XmlIoLock` | `XmlIoLock` | **Singleton** | プロセス内・スレッド間のファイル排他制御ロック | なし |
| **基盤** | `IAppSettingsService` | `AppSettingsService` | **Singleton** | `AppSettings.xml` の永続化・設定値管理（OSC・UDP・Steam フォルダ・偽装 exe 名・ログ保持数などを含む） | `IPathService`, `XmlIoLock` |
| **基盤** | `IAppearanceSettingsService`| `AppearanceSettingsService` | **Singleton** | UIテーマ（Dark/Default）、フォントスケール、トレイアイコン選択、バッテリー変化通知（発行 `InvokeBatteryChanged`／購読 `BatteryChanged`） | `IAppSettingsService` |
| **基盤** | `IProfileXmlStore` | `ProfileXmlStore` | **Singleton** | プロファイルXMLのシリアライズ・デシリアライズ | `IPathService`, `XmlIoLock` |
| **基盤** | `IProfileRepository` | `ProfileRepository` | **Singleton** | プロファイル名一覧の保持、CRUD、存在検証、リンクプロファイル解決、ブランクプロファイル読込（`LoadBlankProfile`） | `IProfileXmlStore`, `IPathService` |
| **基盤** | `IProfileSettingsService` | `ProfileSettingsService` | **Singleton** | ロード済みプロファイルのインメモリ設定値管理（出力 KBM マッピングによる仮想キーの別名更新 `RefreshActionAlias` を含む） | `IProfileXmlStore` |
| **基盤** | `IDeviceOptionRepository` | `DeviceOptionRepository` | **Singleton** | 機種別オプション（`*ControllerOptsDTO`）の管理 | `IPathService`, `XmlIoLock` |
| **基盤** | `ISpecialActionRepository` | `SpecialActionRepository` | **Singleton** | `Actions.xml` のCRUDおよび定義管理、名前によるアクション取得（未発見は placeholder を返す `GetActionOrPlaceholder`）、保存を伴う `SaveAction`／`RemoveActionAndSave`（`Global` への薄い委譲。メモリ上のリストだけを変える `AddAction`／`RemoveAction` とは別） | `IPathService`, `XmlIoLock` |
| **基盤** | `IOutputSlotStore` | `OutputSlotStore` | **Singleton** | スロット永続化設定（`OutputSlots.xml`）の管理 | `IPathService`, `XmlIoLock` |
| **基盤** | `ILogSettingsApplier` | `LogSettingsApplierProxy`（実体は `LoggerHolder`。`App` が生成直後に `Attach`） | **Singleton** | ログ設定（保存数・最小ログレベル）の変更を NLog の構成へ反映する窓口。`LoggerHolder` は設定の読み込み後にしか生成できないため、遅延接続のプロキシを登録する（接続前の呼び出しは何もしない） | なし |
| **基盤** | `IProfilePresetLoader` | `ProfilePresetLoader` | **Singleton** | プリセット（空・ゲームパッド＋ジャイロ・ミックス・ミックス＋ジャイロマウス・KBM・KBM＋ジャイロマウス。Xbox 360/DS4 別）の読み込み。`Global.Load…Profile` への薄い委譲（独自の状態なし） | なし |
| **1.入力**| `IDeviceHotplugMonitor` | `DeviceHotplugMonitor` | **Singleton** | Win32 RAW/HID デバイス到着・抜去イベントの検知 | なし |
| **1.入力**| `IDs4DeviceRegistry` | `Ds4DeviceRegistryAdapter` | **Singleton** | コントローラーの列挙・保持、通信ハンドルの監視 | `IDeviceHotplugMonitor` |
| **1.入力**| `IDeviceStateService` | `DeviceStateService` | **Singleton** | 初回接続判定・シリアル変更通知（デバイス一覧・電池残量は Phase7-Step2 W3 で削除。実体は `ControlService.DS4Controllers`） | なし |
| **1.入力**| `IInputReportParserFactory` | `InputReportParserFactory` | **Singleton** | デバイス種別に応じたパケット解析器（DS4/Vader等）の提供 | なし |
| **2.変換**| `IButtonProcessor` | `ButtonProcessor` | **Singleton** | ボタンリマップ、シフト変調（ステートレス計算） | なし |
| **2.変換**| `IStickProcessor` | `StickProcessor` | **Singleton** | デッドゾーン、カーブ（`StickOutCurve`）、AntiSnap計算 | なし |
| **2.変換**| `ITriggerProcessor` | `TriggerProcessor` | **Singleton** | トリガー2段階判定、モーター制御値計算 | なし |
| **2.変換**| `ITouchGyroProcessor` | `TouchGyroProcessor` | **Singleton** | タッチパッドジェスチャー、ジャイロ計算 | なし |
| **2.変換**| `IMouseEngine` | `MouseEngine` | **Singleton** | `Mouse.cs`解体先: マウスカーソル加速、OneEuroFilter平滑化 | なし |
| **2.変換**| `IInputMappingPipeline` | `InputMappingPipeline` | **Singleton** | スロット別 `MappingPipelineContext` を各Processorへ適用統括 | 各Processor群, `IMappingActionDispatcher` |
| **2.変換**| `IInputLoopCoordinator` | `InputLoopCoordinator` | **Singleton** | 毎秒250〜1000回の高速入力ポーリングループ実行 | `IDs4DeviceRegistry`, `IInputMappingPipeline` |
| **2.変換**| `IProfileApplicationService`| `ProfileApplicationService` | **Singleton** | プロファイルのスロット適用・復帰、Halt保護 | `IProfileRepository`, `IOutputSlotService` |
| **2.変換**| `IProfileSlotApplier` | `ProfileSlotApplier` | **Singleton** | 接続時などにスロットへプロファイルを適用する、`ControlService` 側の出力ポート（依存の逆転）。現状は `Global.ApplyProfileToSlot` へ委譲し、K1 是正後に `IProfileApplicationService` へ委譲する | なし（目標: `IProfileApplicationService`） |
| **2.変換**| `IProfileActionProvider` | `ProfileActionProvider` | **Singleton** | プロファイルに紐づくアクション定義の解決 | `ISpecialActionRepository` |
| **2.変換**| `IAutoProfileService` | `AutoProfileService` | **Singleton** | フォアグラウンドアプリ監視とプロファイル自動切替 | `IProcessInspector`, `IProfileApplicationService` |
| **2.変換**| `IManagedActionManager` | `DefaultActionManager` | **Singleton** | マクロ・キー・プロファイル切替アクションの実行統括。2026-09-04〜2026-10-14 は登録が欠落していた（Phase6-Step9b-2 で再登録） | `IActionFactory` |
| **2.変換**| `IControllerRegistry` | `DefaultControllerRegistry` | **Singleton** | `KeyButtonActionController` の登録と、デバイス単位・終了時の破棄（Phase6-Step9b-2 で再登録） | なし |
| **2.変換**| `IMapping` | `Mapping` | **Singleton** | 入力の評価（カーブ・割り当て・合成入力の確定・切断の後始末・マクロ）。旧: メンバーのほぼすべてが static（Phase7-Step8）。`DefaultActionManager`／`DefaultMacroPlayer` は `Func<IMapping>` で遅延して受ける | `SyntheticInputState`, `MacroRuntimeState`, `StickMotionState`, `IProfileSettingsService`, `IProfileApplicationService`, `IVirtualKBM`, `IKeyButtonControllerRegistry`, `IProcessLauncher`, `IProfileSwitcher`, `ISyntheticDispatcher`, `Func<IDeviceStateAccessor>`※1 |
| **2.変換**| `IKeyButtonControllerRegistry` | `KeyButtonControllerRegistry` | **Singleton** | デバイス × アクション名ごとの `KeyButtonActionController` の登録簿（生成・再利用・破棄）。旧: `Mapping` の静的フィールド | `IKeyButtonActionControllerFactory`, `IControllerRegistry` |
| **2.変換**| `ISyntheticDispatcher` | `SyntheticDispatcherService` | **Singleton** | 合成キーの押下・解放・タイミングのリセット。静的な `SyntheticDispatcher` は、Actions サブシステム用の暫定の橋渡し（Phase8 で削除） | `SyntheticInputState`, `OutputKBMHandlerAdapter`, `IProfileSettingsService` |
| **2.変換**| `SyntheticInputState` | `SyntheticInputState` | **Singleton** | 合成キー・マウス入力の状態（`GlobalState`／`DeviceState`／`SyncStateLock`） | なし |
| **2.変換**| `MacroRuntimeState` | `MacroRuntimeState` | **Singleton** | マクロの実行時状態（実行済みフラグ、二重実行防止、出力フラグ、実行 Task の連鎖） | なし |
| **2.変換**| `StickMotionState` | `StickMotionState` | **Singleton** | スティック・ジャイロ・トリガーの処理状態（フリック、2 段トリガー、ホイールのフィルターほか） | なし |
| **2.変換**| `ActionsLoadedRuntimeReset` | `ActionsLoadedRuntimeReset` | **Singleton** | `BackingStore.ActionsLoaded` の購読者。Actions.xml の読み込み後に、合成状態・トグル状態・マクロの実行済みフラグをリセット | `SyntheticInputState`, `MacroRuntimeState` |
| **2.変換**| `ControlService` | `ControlService` | **Singleton** | 入力・変換・出力パイプライン全体の開始・停止統括（ファサード） | `IDs4DeviceRegistry`, `IInputLoopCoordinator`, `IOutputSlotService`※1, `IProfileSettingsService`, `IAppSettingsService`, `IEnvironmentService`, `IPathService`, `IVirtualKBM`, `IVirtualKBMLifecycle`, `IProfileRepository`, `IDeviceStateService`, `IAppearanceSettingsService`, `IProfileXmlStore`, `IProfileActionProvider`, `IProfileSlotApplier` |
| **3.出力**| `IOutputSlotService` | `OutputSlotService` | **Singleton** | 仮想Xbox360/DS4（ViGEm）の生成、割当、切替 | `IOutputSlotStore` |
| **3.出力**| `IVirtualKBM` | `OutputKBMHandlerAdapter` | **Singleton** | SendInput / FakerInput によるキー・マウス送出。出力ハンドラの実体（`Handler`）を所有する（Phase7-Step8 W3。旧 `Global.outputKBMHandler`） | なし |
| **3.出力**| `IVirtualKBMLifecycle` | `OutputKBMHandlerLifecycle` | **Singleton** | KBM出力ハンドラの生成・破棄・フォールバック切替・マッピング初期化（送出は `IVirtualKBM` が担当） | `IEnvironmentService`, `OutputKBMHandlerAdapter`, `IProfileSettingsService` |
| **3.出力**| `IMacroPlayer` | `DefaultMacroPlayer` | **Singleton** | 時系列非同期マクロの再生・停止管理 | `IVirtualKBM` |
| **3.出力**| `ILightbarService` | `LightbarService` | **Singleton** | バッテリー・プロファイル色に応じたLED発光計算 | なし |
| **3.出力**| `IActionFactory` | `DefaultActionFactory` | **Singleton** | 各種アクションインスタンス（Key/Macro/Launch）の生成 | `IVirtualKBM`, `IMacroPlayer`, `IProcessLauncher` |
| **3.出力**| `IProcessLauncher` | `DefaultProcessLauncher` | **Singleton** | 外部プロセスの通常起動 | なし |
| **3.出力**| `IElevatedProcessLauncher` | `DefaultElevatedProcessLauncher` | **Singleton** | UAC昇格を伴う外部ツールの起動 | なし |
| **3.出力**| `IProcessInspector` | `DefaultProcessInspector` | **Singleton** | 実行中プロセスのパス・ウィンドウ名調査 | なし |
| **3.出力**| `IProfileSwitcher` | `DefaultProfileSwitcher` | **Singleton** | アクションからのプロファイル切替実行 | `IProfileApplicationService` |
| **3.出力**| `IUdpServerService` | `UdpServerService` | **Singleton** | Cemuhook 互換 UDP モーションデータ配信 | `IAppSettingsService` |
| **4.UI** | `IViewModelFactory` | `ViewModelFactory` | **Singleton** | パラメータ付きViewModel / 複合ViewModelの生成ファクトリ | `IProfileSettingsService`, `IProfileRepository`, `ISpecialActionRepository`, `IOutputSlotService`, `IEnvironmentService`, `IPathService`, `IAppSettingsService`, `Func<IAutoProfileService>`・`Func<ControlService>`（生成のたびに遅延解決） |
| **4.UI** | `IEditorWindowFactory` | `EditorWindowFactory` | **Singleton** | 補助ウィンドウ（ボタン割り当て・マクロ記録・プリセット・スティック校正・スペシャルアクション編集）の生成の窓口。UI スレッドで生成する | `IProfileSettingsService`, `IOutputSlotService`, `IPathService`, `IViewModelFactory`, `IProfilePresetLoader`, `Func<ControlService>`（生成のたびに遅延解決） |
| **4.UI** | `MainWindowsViewModel` | `MainWindowsViewModel` | **Singleton** | メインウィンドウ（ヘッダー、タブ切替、ステータス、ViGEmBus ドライバーの確認） | `IEnvironmentService`, `IPathService` |
| **4.UI** | `SettingsViewModel` | `SettingsViewModel` | **Transient** | アプリ全般設定画面（開くたびに最新値取得） | `IAppSettingsService`, `IAppearanceSettingsService`, `IEnvironmentService`, `IPathService`, `ILogSettingsApplier` |
| **4.UI** | `TrayIconViewModel` | `TrayIconViewModel` | **`MainWindow` が生成（アプリ存続期間）** | タスクトレイのアイコン・ツールチップ・コンテキストメニュー（バッテリー表示、プロファイル選択） | `ControlService`, `ProfileList`, `IAppearanceSettingsService`, `IProfileRepository`, `IEnvironmentService`, `IPathService`, UI スレッドの `Dispatcher` |
| **4.UI** | `LogViewModel` | `LogViewModel` | **Transient** | ログ表示画面 | なし |
| **4.UI** | `AboutViewModel` | `AboutViewModel` | **Transient** | バージョン・クレジット表示画面 | `IEnvironmentService` |
| **4.UI** | `ProfileSettingsViewModel` | `ProfileSettingsViewModel` | **Factory生成 (親Mediator)**| プロファイル編集画面（配下の各サブVMを調停・集約。編集スロットの保存・ブランク読込・キャッシュ更新） | サブVM群, `IProfileSettingsService`, `IProfileRepository`, `IOutputSlotService`, `IEnvironmentService`, `IPathService`, `ControlService`（ジャイロ系 setter への引数。null 許容） |
| **4.UI** | `StickSettingsSubViewModel` | `StickSettingsSubViewModel` | **Factory生成 (サブVM)** | スティック感度・カーブ・デッドゾーン設定タブ | `IProfileSettingsService` |
| **4.UI** | `TriggerSettingsSubViewModel`| `TriggerSettingsSubViewModel`| **Factory生成 (サブVM)** | トリガー感度・2段階設定・モーター設定タブ | `IProfileSettingsService` |
| **4.UI** | `ButtonMappingSubViewModel` | `ButtonMappingSubViewModel` | **Factory生成 (サブVM)** | ボタン割り当て・シフトモディファイア設定タブ | `IProfileSettingsService` |
| **4.UI** | `SpecialActionsSubViewModel`| `SpecialActionsSubViewModel`| **Factory生成 (サブVM)** | スペシャルアクション一覧・登録タブ（有効アクション名の編集スロットへの反映を含む） | `ISpecialActionRepository`, `IProfileRepository`, `IDs4DeviceRegistry` |

※1 `ControlService` は過渡期に `IOutputSlotService` を直接ではなく `Func<IOutputSlotService>`（遅延解決）で受け取る。理由は §4 を参照。目標構造は直接注入であり、変更しない。

---

## 3. リソース破棄と安全な終了手順

1. **破棄の依存順序制御（LIFO / 逆順破棄）:**
   * アプリ終了時、`AppHost` が破棄される際に、依存している側（上位）から順に `Dispose()` が呼ばれる。
   * `ControlService` / `InputLoopCoordinator` が停止してスレッドを安全に閉じた後に、`IDs4DeviceRegistry`（HIDデバイスハンドル）や `OutputSlotService`（ViGEmクライアント）が破棄されるため、リソース解放時の競合クラッシュが発生しない。
2. **ホットパスサービスのステートレス設計:**
   * `ButtonProcessor`、`StickProcessor` などの各変換プロセッサは、内部にミュータブルな状態を持たず、パイプラインコンテキスト `MappingPipelineContext` 経由で受け渡すステートレス設計とすることで、Singleton でありながら完全なスレッドセーフとゼロGCを両立する。

---

## 4. 過渡期の既知の逆依存（2026-09-19 Phase6-Step2 実地確認）

本仕様書の目標構造では、下位（出力・基盤）サービスは `ControlService` に依存しない。現状の実装には以下の逆依存が残っており、**技術的負債として管理する**（目標構造は変更しない）。

| 逆依存（現状） | 実装上の根拠 | 影響 | 暫定措置 | 解消予定 |
| :--- | :--- | :--- | :--- | :--- |
| `OutputSlotService` → `ControlService` | コンストラクタで `Func<OutputSlotManager>`（スロット管理の実体は `ControlService` が所有）と `Func<ControlService>` を受け取り、使用時に解決する（Phase6-Step10-4-3 で `Program.rootHub`／`AppHost`／`new` のフォールバックを廃止） | 生成時の循環と、`OutputSlotManager` の二重化は解消 | `Func` による遅延解決（現行） | 仮想スロット操作用の出力ポートの導入（Phase7 以降で検討） |
| `ProfileApplicationService` → `ControlService` | コンストラクタで `Func<ControlService>` を受け取り、使用時に解決する（Phase6-Step10-4-2a で `AppHost` のフォールバックを廃止） | 生成時の循環は解消 | `Func` による遅延解決（現行）。`ControlService` からは直接注入しない | `Global.ApplyProfile` の実装本体の移管（Phase7） |
| `ProfileRepository` → `ControlService` | コンストラクタで `Func<ControlService>` を受け取り、`LoadProfile` で使用時に解決する（Phase6-Step10-4-2b で `AppHost` の呼び出しを廃止） | Service Locator は解消 | `Func` による遅延解決（現行） | 未定 |

%% 注釈補強（2026-09-19 Phase6-Step2 決定O1=B／O4=B-2 反映）
%% - 新規サービス `IProfileSlotApplier` / `ProfileSlotApplier`（2.変換、Singleton）を追加。`ControlService` の依存に `IProfileSlotApplier` を追加。

%% 注釈補強（2026-09-19 Phase6-Step2 決定O2=C 反映）
%% - `IEnvironmentService` に「コントローラースロット上限」（`ControllerSlotLimit` / `UsingMaxControllers`）を追加。旧 `ControlService` の static フィールドをサービスの値へ移行する。未移行の呼び出し元向けの static 互換シムは Phase6-Step12 で削除予定。

%% 注釈補強（2026-09-19 Phase6-Step2 実地確認・決定D1〜D3反映）
%% - 新規サービス `IVirtualKBMLifecycle` / `OutputKBMHandlerLifecycle`（3.出力、Singleton）を追加（決定D3）。`IVirtualKBM`（送出専用）は変更しない。
%% - `ControlService` の主な依存注入引数を Step2 の実装計画（Phase6-Step2-Plan.md §3.1）に合わせて更新。新規引数はすべて必須（Pure DI、決定D2）。
%% - `IEnvironmentService` / `IAppearanceSettingsService` / `IProfileRepository` / `IDeviceStateService` の責務記述を、Step2 で追加されるシム（Phase6-Step2-Plan.md §4.1）に合わせて補足。
%% - 過渡期の逆依存（§4）は現状実装の負債であり、目標構造の変更ではない。

%% 注釈補強（2026-09-18監査結果反映、構造変更なし）
%% - ライフタイム定義（Singleton/Transient/Factory生成）は現状のDI登録（ServiceRegistration.cs）と前提条件（Phase5完了）と一致。
%% - Singleton サービス（ControlService, InputLoopCoordinator, 各Processor, IProfileApplicationService等）のステートレス設計とZero-GC原則は現状監査（762参照、重複ゼロ・漏れゼロ証明済み）と一致。
%% - Transient サービス（SettingsViewModel, LogViewModel, AboutViewModel）の最新値取得原則は現状のUI層（App/MainWindow/ProfileEditor/SettingsVM、計273件）と一致。
%% - Factory生成（ProfileSettingsViewModel + SubVM群）のMediator調停構成は現状のProfileEditor（96件、契約差含む）と一致。契約差（ProfileApp契約差、SpecialAction編集契約差）はb要設計として記録済み（ABC-Classification.md 参照）。
%% - リソース破棄順序（LIFO/逆順破棄）は現状のガードレール（ドライバ破棄順序、切断時クリーンアップ）と一致。未適用項目はStep2以降に適用（Phase6-Status.md 参照）。
%% - c（除外 ≈ 304件）の改修対象外原則を維持（Classification-Metrics.md, C-Classification-Count.md 参照）。a（契約候補）とb（要設計 ≈ 458件）が改修対象。
%% - Phase5証跡（Step14/15未完了）の内容検証は別途必要（Phase5-Evidence-Check.md 参照）。本監査（Phase6-Step1）は中間成果として記録（報告書§7維持：Step2承認根拠には使用しない）。

%% 注釈補強（2026-09-21 Phase6-Step3 実地突き合わせ・論点3決定反映）
%% - `IDisplayCoordinateService`（`DisplayCoordinateService` が実装）を新設。`Mapping.cs` の画面座標変換処理（`absUseAllMonitors`／`TranslateCoorToAbsDisplay`）の移行先。`IEnvironmentService.PrepareAbsMonitorBounds` と同じ状態（`absDisplayBounds`/`fullDesktopBounds`/`absUseAllMonitors`）を扱うため、実装時に本サービスへ集約し `IEnvironmentService` 側は薄い委譲として残すか、両立が可能かを Step3-1 で確定する。
%% - `IProfileXmlStore` に `SaveControllerConfigsForDevice` を追加予定（対の `LoadControllerConfigsForDevice` は Phase6-Step2-2 で追加済み）。

%% 注釈補強（2026-09-23 Abs Mouse機能削除・ユーザー承認済み、copilot-instructions.md §2.2 例外規定）
%% - 上記の `IDisplayCoordinateService`／`DisplayCoordinateService`（Singleton登録）は、その唯一の利用者であったボタン割当のAbs Mouse機能およびタッチパッドのAbsolute Mouseモードが使用頻度の低さから削除されたことに伴い、DI登録ごと撤去した（§2 登録サービス完全一覧表から行を削除済み）。
%% 注釈補強（2026-09-25 Phase6-Step7-4 決定7＝案H 反映）
%% - `IStartupArguments`（`StartupArguments` が実装）を新設し、§2 の登録サービス一覧に追加した。アプリの起動引数（`ArgumentParser`）を `ControlService` の生成へ受け渡すだけの保持役。
%% - 背景: 現在の起動順序では、ホストは `Global` の静的初期化（`ScpUtil.cs` の `fallbackProfileRepository`）の中で起動引数なしに先に暗黙構築され、`App.xaml.cs` の `AppHost.CreateHost(config, parser)` は既存のホストを返すだけになっている。構築済みのコンテナには登録を追加できないため、`CreateHost(config, parser)` は登録済みの本サービスへ起動引数を設定する（`Phase6-Step7-Plan.md` §0.4 の訂正・決定7）。
%% - `ControlService` 自体の依存（コンストラクタ引数）は変わらない。`ServiceRegistration` の生成処理が本サービスから起動引数を取り出して渡す。
%% - ホストが Pre-Host 領域で暗黙構築される問題そのもの（案R）は持ち越し事項（`Phase6-Status.md` §6.5 K7-1）。

%% 注釈補強（2026-09-26 Phase6-Step7b 決定3＝案M1 反映）
%% - §1-3（Factory 生成）に、プロファイル編集画面では編集対象と実機（`targetDevice`）を分けて渡す旨を追記した。`IViewModelFactory` のライフタイム・依存（§2）は変わらない。現行コードの契約は `CreateProfileSettingsViewModel(int device, int targetDevice = -1)`／`CreateRecordBoxViewModel(…, int targetDevice = -1)`。
%% - 詳細は `03-Class-Interface-Diagram.md` の 2026-09-26 の注釈と `Phase6-Step7b-Plan.md` §2A 決定3。
%% 注釈補強（2026-10-13 Phase6-Step8 反映）
%% - §2 の `IProfileRepository`（ブランクプロファイル読込）、`ISpecialActionRepository`（`GetActionOrPlaceholder`）の責務記述と、`ProfileSettingsViewModel`・`SpecialActionsSubViewModel` の依存（`IProfileRepository` を追加）を現行コードに合わせて更新した。ライフタイム（Singleton／Factory生成）は変わらない。
%% - `ProfileEditor`（View）は、`IAppSettingsService`（レイアウト・言語）と `IProfileSettingsService`（編集スロットの補正値・出力種別）をコンストラクタ引数で受け取る。`MainWindow` が保持済みのサービスを渡す。View 自体はライフタイム管理の対象ではない（Window／UserControl）。
%% - 保存の再適用は、保存の通知を受けた `MainWindow`（`IProfileApplicationService` 経由）に残している（Step7b 決定8）。ViewModel の `SaveProfile` は編集スロットの保存だけを行う。
%% - 詳細は `03-Class-Interface-Diagram.md` の 2026-10-13 の注釈と `Phase6-Step8-Plan.md` §0A。
%% 注釈補強（2026-10-14 Phase6-Step9 反映）
%% - §2 の `MainWindowsViewModel`・`SettingsViewModel`・`ProfileSettingsViewModel`・`IViewModelFactory` の依存を、現行コードに合わせて更新した（旧記載の `MainWindowsViewModel`＝`IAppSettingsService`, `ControlService` は実際には使っていなかった）。`TrayIconViewModel` の行を追加した（DI 登録ではなく、`MainWindow` が生成する）。
%% - 4 つの ViewModel の依存はすべてコンストラクタの必須引数。`ViewModelFactory` は `Func<ControlService>` で `ControlService` を生成のたびに遅延解決する（`ServiceRegistration` に `Func<ControlService>` を登録）。ライフタイムは変わらない。
%% - `IPathService`／`IEnvironmentService`／`IAppearanceSettingsService`／`IAppSettingsService`／`IProfileSettingsService` の責務記述に、Step9 で追加した契約（`ProgramDirectory`、ViGEmBus 判定と `AdminNeeded`、`BatteryChanged`、Steam フォルダ・偽装 exe 名・ログ保持数、`RefreshActionAlias`）を追記した。
%% - 詳細は `03-Class-Interface-Diagram.md` の 2026-10-14 の注釈と `Phase6-Step9-Plan.md` §0A。
%% 注釈補強（2026-10-14 Phase6-Step10-2 反映）
%% - §2 に `IProfilePresetLoader`（基盤・Singleton。`Global.Load…Profile` への薄い委譲）と `IEditorWindowFactory`（4.UI・Singleton）を追加し、`IViewModelFactory` の依存に `IAppSettingsService` と `Func<IAutoProfileService>` を追加した。
%% - `EditorWindowFactory` は、`ControlService` を生成のたびに `Func` で遅延解決する（明示的なホスト構築より前に解決しない。`ViewModelFactory` と同じ形）。補助ウィンドウは UI スレッドで生成する。
%% - `IAutoProfileService` は生成時に `AutoProfileHolder` を読むため、設定の保存先が決まる前に解決しない（`ViewModelFactory` は `Func` で遅延解決する）。
%% - 詳細は `03-Class-Interface-Diagram.md` の 2026-10-14（Step10-2）の注釈と `Phase6-Step10-2-Plan.md`。
%% 注釈補強（2026-10-14 Phase6-Step10-3 反映）
%% - §2 の `ISpecialActionRepository` に、保存を伴う `SaveAction`／`RemoveActionAndSave` を追記した（ライフタイム・依存は変わらない）。`IViewModelFactory` の生成口に `CreateSpecialActionsListViewModel` が加わった。
%% - `EditorWindowFactory` の依存に `ISpecialActionRepository` が加わった。
%% - 詳細は `03-Class-Interface-Diagram.md` の 2026-10-14（Step10-3）の注釈と `Phase6-Step10-3-Plan.md`。
%% 注釈補強（2026-10-15 Phase6-Step10-4 反映）
%% - 削除: `IProfileActionChainService`／`ProfileActionChainService`（連鎖の廃止。切替後は、トリガーの押し直しまで同じトリガーのアクションを実行しない）、`IMappingActionDispatcher`／`MappingActionDispatcher`（連鎖の廃止で呼出元が 0 件）、`AutoProfileChecker`（廃止済みの旧部品の残骸）。
%% - 依存の必須化（`AppHost`／`Program.rootHub`／`new` のフォールバックを廃止）: `ProfileRepository`、`SpecialActionRepository`、`ProfileSettingsService`、`ProfileApplicationService`、`AutoProfileService`、`AutoProfileHolder`、`ProfileActionProvider`、`OutputSlotService`／`OutputSlotStore`／`OutputSlotManager`、`DefaultActionFactory`／`DefaultActionManager`／`DefaultMacroPlayer` と各アクション。`ControlService` は `Func<ControlService>` で遅延解決する（§4 の逆依存の表）。
%% - `BackingStore` は DI 未登録のため、`ServiceRegistration` が `Global.store` を渡すファクトリー登録とする（`IProfileSettingsService`、`ISpecialActionRepository`、`IProfileActionProvider`）。
%% - `IEnvironmentService` に `ViGEmBusVersion` を追加（`Global.vigemBusVersionInfo` への薄い委譲）。`IProfileRepository` に `ReplaceLinkedProfileName` を追加（名前変更時のリンクプロファイルの旧名の置換）。
%% - 全域の退行ガード: `Step10FourGlobalRatchetGuardTests`（ファイルごとの `Program.rootHub`／`AppHost`／`Global.*` の件数が基準値以下）。
%% - 詳細は `Phase6-Step10-4-Plan.md`。
%%
%% Phase7-Step1 W4（2026-10-17）: 追跡の対象
%% - Service Locator は 2 系統ある。1 系統目: `AppHost.GetService|Services` と `Program.rootHub`（`Step10FourGlobalRatchetGuardTests`）。2 系統目: `ServiceProviderHolder.Provider`（23 件。`Phase7Step1ServiceLocatorRatchetGuardTests` を追加）。
%% - 2 系統目のうち `Actions/` の 17 件は Phase8 の持ち越し。`Mapping`（5）と `App`（1）は Phase7-Step5 で注入へ置き換える。
%% - 台帳: `docs-forDIMG/MadeByAgent/Phase7-Residual-Ledger.md`。
%%
%% Phase7-Step2 W3（2026-10-17）: `ControllersViewModel`（`HasControllers` だけを持つ使われない殻）を削除。`IDeviceStateService` は、デバイス一覧・接続状態・電池残量などの取得（内部配列が常に空で、本体で一度も設定されなかった孤立実装）を削除し、初回接続判定（`IsFirstConnection`／`MarkConnected`／`ResetConnectionFlags`）とシリアル変更通知（`OnDeviceSerialChange`）だけを持つ。接続中のデバイスの実体は `ControlService.DS4Controllers`（`IDeviceStateAccessor` 経由）。%%
%% Phase7-Step3（2026-10-17）: `BackingStore`（設定の唯一の実体）を DI のシングルトンとして登録した（`services.AddSingleton<BackingStore>(_ => Global.store)`。実体は `Global.store` と同一で、`Global` 側の生成は Step10 まで残す）。
%% - 上の 2026-10-15 の注釈にある「`BackingStore` は DI 未登録のため、`Global.store` を渡すファクトリー登録」は、この登録に置き換わった（`ProfileSettingsService`／`SpecialActionRepository`／`ProfileActionProvider` のファクトリーは `GetRequiredService<BackingStore>()` を渡す）。
%% - `BackingStore` を必須の依存として受け取るサービス: `ProfileXmlStore`、`ProfileRepository`、`AppSettingsService`、`AppearanceSettingsService`、`DeviceStateService`、`AppNotificationService`、`AutoProfileService`（`ProfileSettingsService`、`SpecialActionRepository`、`ProfileActionProvider` は以前から）。任意引数や `Global.store` へのフォールバックは廃止した。
%% - `AppSettingsService` が `UDPServerSmoothing*Changed` イベントの発火元になった（静的イベントから、サービスのインスタンスイベントへ。購読者は `ControlService`）。`FakeExeName` の検証、リンクプロファイルのシリアル正規化もサービスへ移した。
%% - 詳細は `Phase7-Step3-Plan.md`、`Phase7-Step3-Completion-Report.md`、台帳は `Phase7-Residual-Ledger.md` §5。
%%
%% Phase7-Step3c（2026-10-17）: プロファイル適用の処理を `Global` から DI サービスへ移した。
%% - `ProfileApplicationService`: 適用の核（読み込み → 状態の更新 → ログ → `SelectedProfileChanged` → アクション関連の破棄と作り直し）を持つ。入口は 3 つ: `ApplyProfile`（Halt あり。`MappingAction` のときは Halt しない）、`ApplyFromAction`、`ApplyProfileWithoutHalt`（Halt なし。接続処理中など、呼び出し元がすでに停止している経路用）。依存に `IProfileXmlStore`／`IProfileActionProvider`／`BackingStore` が加わった（`ControlService` は従来どおり `Func` で遅延解決）。
%% - `ProfileSlotApplier`: `IProfileApplicationService.ApplyProfileWithoutHalt` へ委譲する（スロット範囲・名前の検査は旧 `Global.ApplyProfileToSlot` と同じ）。`ControlService` は引き続き `IProfileSlotApplier` だけを知る（依存の逆転は変わらない）。
%% - `ProfileRepository`: `LoadTempProfile` を実装として持ち、`SelectedProfileChanged` は自身のインスタンスイベントになった（発火は `ProfileApplicationService` が `RaiseSelectedProfileChanged` を呼ぶ。購読者は `ControllerListViewModel`）。`IProfileRepository.LoadBlankProfile` は削除（`IProfilePresetLoader.LoadBlank` に統合）。
%% - `ProfilePresetLoader`: `BackingStore` と `IProfileSettingsService` を注入で受け、10 種のプリセットを直接読み込む。`BackingStore` が `loggedInvalidActions` を持つ。
%% - `Mapping.cs` の特殊アクションの切替は、`Global.ApplyProfile` ではなく `IProfileApplicationService.ApplyProfile`（`MappingAction`）を呼ぶ。
%% - 詳細は `Phase7-Step3c-Plan.md`、`Phase7-Step3c-Completion-Report.md`、台帳は `Phase7-Residual-Ledger.md` §6。
%%
%% Phase7-Step4（2026-10-17）: 起動・終了の手順が変わった。
%% - ブートストラップ: `Application_Startup` の最初（コンテナの構築より前）に、`PathService` と `EnvironmentService` を作る。保存場所の決定（`FindConfigLocation`）、ログのローテーション、ViGEmBus 情報の取得は、このインスタンスを使う。`AppHost.CreateHost(config, parser, pathService, environmentService)` が、同じインスタンスをコンテナへ登録する（Singleton のライフタイムは変わらない）。状態（保存先、初回起動、導入状態ほか）は両クラスの static で、どのインスタンスでも同じ値を見る。
%% - `-driverinstall` 分岐: 設定の読込の前に、起動引数つきで `CreateHost` を明示的に呼ぶ（暗黙のホスト構築はなくなった）。`-reenableDevice` 分岐は、ホストを作らず、`IDs4DeviceRegistry`（`Ds4DeviceRegistryAdapter`）の契約を通す。
%% - 終了: `CleanShutdown` の最後（`rootHub` の停止、設定の保存、コントローラーレジストリの破棄の後、ログのフラッシュの前）に `AppHost.Dispose()` を呼ぶ。Dispose の後は、`GetService` が暗黙にホストを作り直さない。
%% - ViGEmBus／HidHide／FakerInput／OS の調べ物とその状態は `EnvironmentService` が持つ（`Global` から移管）。
%% - 詳細は `Phase7-Step4-Plan.md`、`Phase7-Step4-Completion-Report.md`、台帳は `Phase7-Residual-Ledger.md` §7。
%%
%% Phase7-Step5（2026-10-17）: `Program.rootHub` を削除し、Service Locator の大半を注入に置き換えた。
%% - `ControlService` の保持: `App` のフィールド（`_controlService`）が持つ。`ServiceRegistration` の登録は常に新規生成（`rootHub ??` を削除）。生成は従来どおり `controlThread`（別スレッド）で、`ResolvePostHostService` を通す。
%% - 画面の生成: `IMainWindowFactory`（`MainWindowFactory`。Singleton）を新設。`App` は生成の直前に 1 件解決して `Create(parser)` を呼び、`MainWindow` は必要なサービスをコンストラクター引数で受け取る（`ControlService` と `IAutoProfileService` は `Func` で遅延解決）。`ProfileEditor` は `IEditorWindowFactory.CreateProfileEditor` が生成する。
%% - 出力スロットの永続化: `App`（起動後の読込）と `ControlService.ShutDown`（保存）が `IOutputSlotService.LoadOutputSlots`／`SaveOutputSlots` を通す。
%% - 通知: `AppLogger.NotificationService`（静的）を `App` が起動時に `INotificationService` のシングルトンへ設定する（`Global.NotificationServiceInstance` を削除）。
%% - `ControlService` のコンストラクターに `IElevatedProcessLauncher` が加わった。`BackingStore.ProcessInspector` は、`ServiceRegistration` の `BackingStore` の登録が設定する。
%% - `DS4SixAxis.GyroControlsTriggerActive`: `ControlService` がデバイスをスロットへ接続するときに設定する（DS3 のジャイロトリガー）。
%% - 残る Locator は `Mapping`（Step7〜9）、`Actions/`（Phase8）、互換 `AppHost`（Step9）、`Global.ProfileSettingsServiceInstance`、`App` の解決の窓口 1 件。
%% - 詳細は `Phase7-Step5-Plan.md`、`Phase7-Step5-Completion-Report.md`、台帳は `Phase7-Residual-Ledger.md` §8。
%%
%% Phase7-Step6（2026-10-17）: 出力デバイス種別の配列の所有権を Global から OutputSlotService へ移した。
%% - 所有: `OutputSlotService`（Singleton）が `OutDevTypeTemp`（編集画面の未確定値。初期値は全スロット X360）と `ActiveOutDevType`（スロットごとの接続中の種別。初期値は全スロット None）を readonly の配列で持つ。参照は再代入しない。要素の読み書きにロックはない（従来どおり）。
%% - 書き込み: `ControlService`（`ActiveOutDevType`。初回に 1 回だけ配列の参照を取得して保持）、編集画面の ViewModel（`OutDevTypeTemp`）。
%% - 読み取り: `BackingStore.ActiveOutDevType`（プロパティ）。`IOutputSlotService` の登録が、`OutputSlotService` の生成時に同じ配列を設定する。設定前（コンテナ構築前の起動処理、コンテナを通らないテスト）は既定の全 None の配列を読む。
%% - 禁止: `BackingStore` の登録の中で `IOutputSlotService` を解決しない（`IOutputSlotStore` → `IPathService` → `BackingStore` の循環でスタックオーバーフロー）。
%% - 持ち越し: `Global.outputKBMHandler`／`outputKBMMapping` は Step7〜9（`Mapping` の instance 化）で扱う。
%% - 詳細は `Phase7-Step6-Plan.md`、`Phase7-Step6-Completion-Report.md`、台帳は `Phase7-Residual-Ledger.md` §9。
%%
%% Phase7-Step7（2026-10-17）: Mapping の静的な状態のうち、外から触られるものを保持クラスへ切り出した。
%% - 保持クラス（Singleton。実体は各クラスの Shared と同じ instance を DI にも登録）: SyntheticInputState（合成キー・マウス入力の状態。globalState／deviceState／syncStateLock）、MacroRuntimeState（マクロの実行時状態。macrodone／二重実行防止のフラグとロック／macroControl／macroCount／macroTaskQueue）、StickMotionState（スティック・ジャイロ・トリガーの処理状態のうち外から触られるもの）。
%% - Mapping の同名メンバーは保持クラスへの転送のみ（状態を持たない）。Mouse、ControlService、SyntheticDispatcher は保持クラスを直接使う。ホットパスでは、Mapping が型の初期化時に参照を 1 度だけ保持し、毎レポートの割り当てを増やさない。
%% - Actions.xml の読み込み後のリセット: BackingStore.LoadActions が ActionsLoaded イベントを発行し、ServiceRegistration が登録した購読者 ActionsLoadedRuntimeReset.Reset が、合成状態・ActionManager のトグル状態・macrodone をリセットする（従来の順序とログのまま）。BackingStore は Mapping を参照しない。AppHost.CreateHost が複数回呼ばれても購読は 1 つ。
%% - Step8 で Mapping が instance 化されたら、各保持クラスの Shared（暫定）を削除し、コンストラクター注入にする。
%% - 詳細は Phase7-Step7-Plan.md、Phase7-Step7-Completion-Report.md、台帳は Phase7-Residual-Ledger.md §10。
%%
%% Phase7-Step8（2026-10-17）: Mapping の instance 化と、依存の注入。
%% - Mapping は DI の Singleton（IMapping）。Mapping の AppHost（6）と ServiceProviderHolder（5）は 0 になった。AppHost.GetService 11 → 5、ServiceProviderHolder 22 → 17（残りは Actions/ の 17 件で Phase8）。
%% - 循環の避け方: Mapping → IDeviceStateAccessor（＝ControlService）は Func で遅延。DefaultActionManager／DefaultMacroPlayer → Mapping は Func<IMapping> で遅延（PreallocateActionEntries がホスト構築直後に DefaultActionManager を解決するため）。Mapping ⇔ ProfileApplicationService の循環は、登録簿 KeyButtonControllerRegistry を切り出して解消。
%% - 保持クラス 3 つの Shared を削除（コンテナの Singleton を共有）。BackingStore の登録は、ActionsLoadedRuntimeReset（instance）を購読する。Global.store が静的なため、コンテナの作り直しでも重複購読しないよう、前回の購読者を外してから登録する。
%% - 出力 KBM: IVirtualKBM と IVirtualKBMLifecycle は同じ OutputKBMHandlerAdapter を共有する（ハンドラの実体はそこ）。ISyntheticDispatcher の登録が、作成時に静的な橋渡し SyntheticDispatcher.Bind を呼び、AppHost.CreateHost がホスト構築直後に解決して有効にする。
%% - ControlService は IMapping／StickMotionState／SyntheticInputState をコンストラクターで受け、IMapping を MappingService として公開する（ControllerReadingsControl、RecordBoxViewModel が使う）。
%% - 詳細は Phase7-Step8-Plan.md、Phase7-Step8-Completion-Report.md、台帳は Phase7-Residual-Ledger.md §11。
%%
%% Phase7-Step9（2026-10-18）: 互換 AppHost の削除と、Global 参照の解消。
%% - 互換 DS4Windows.AppHost を削除した。AppHost.GetService|Services の残りは 4 件（App.xaml.cs の解決の窓口 1、DI/AppHost.cs 自身のログ 2、Global.cs 1）。ServiceProviderHolder は Actions/ の 17 件（Phase8）。
%% - ControllerLimits／NumericUtil は静的クラス（DI 登録なし。状態を持たない定数・純粋計算）。
%% - MacroRuntimeState（Singleton）に TryBeginDispatch／EndDispatch を追加（K3 の二重実行防止。挙動は同一）。
%% - AppHost の暗黙構築と CreationNote は触っていない（Step10 で判断）。
%%
%% Phase7-Step10a（2026-10-18）: Global の定数・資源パス・Clamp の再配置（DI 登録の変更なし）。
%% - ControllerLimits／NumericUtil／ResourcePaths の定数は静的（状態を持たない定数・純粋計算）。Global.* は 355 → 107。AppHost.GetService|Services 4、ServiceProviderHolder 17 は変更なし。
%%
%% Phase7-Step10b（2026-10-18）: DI の構成の変更。
%% - ProfileApplicationService のコンストラクターから IDeviceStateService を外した（8 引数）。AutoProfileHolder は IPathService に加えて IEnvironmentService を受ける。AutoProfilesViewModel（ViewModelFactory 経由）も IEnvironmentService を受ける。
%% - 静的クラス（DI 登録なし）: X360ControlTables／DS4ControlNames／ColorUtil／TrayIconResources。Global.* は 107 → 93。AppHost.GetService|Services 4、ServiceProviderHolder 17 は変更なし。
%%
%% Phase7-Step10c（2026-10-18）: DI の構成の変更（永続化の型の登録）。
%% - Singleton（具象クラス）: SpecialActionsXmlSerializer、LinkedProfilesXmlSerializer、ControllerConfigsXmlSerializer、AppSettingsXmlSerializer、LegacyProfileXmlSerializer、ProfilePresets（いずれも BackingStore などをコンストラクターで受ける）。
%% - コンストラクターの変更: ProfileXmlStore（+ControllerConfigs／AppSettings／LegacyProfile）、ProfileRepository（+LinkedProfiles／SpecialActions／LegacyProfile）、SpecialActionRepository（+SpecialActions）、ProfilePresetLoader（+ProfilePresets）。
%% - Global.* は 93 → 69。AppHost.GetService|Services 4、ServiceProviderHolder 17 は変更なし。
