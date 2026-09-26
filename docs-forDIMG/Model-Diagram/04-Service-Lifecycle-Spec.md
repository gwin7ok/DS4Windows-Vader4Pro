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
| **基盤** | `IPathService` | `PathService` | **Singleton** | プロファイル・アプリデータ保存先パスの動的解決 | なし |
| **基盤** | `IEnvironmentService` | `EnvironmentService` | **Singleton** | OS種別、管理者権限、ディスプレイ解像度情報、HidHide／FakerInput の導入状態、デバイスインスタンスID解決、コントローラースロット上限（同時に扱えるスロット数の上限。接続台数ではない）の提供 | なし |
| **基盤** | `INotificationService` | `AppNotificationService` | **Singleton** | OSトースト通知、ステータス通知の統一発行 | なし |
| **基盤** | `IStartupArguments` | `StartupArguments` | **Singleton** | アプリの起動引数（`ArgumentParser`）の保持役。`AppHost.CreateHost(config, parser)` が値を設定し、`ControlService` の生成時に渡す（Phase6-Step7-4） | なし |
| **基盤** | `XmlIoLock` | `XmlIoLock` | **Singleton** | プロセス内・スレッド間のファイル排他制御ロック | なし |
| **基盤** | `IAppSettingsService` | `AppSettingsService` | **Singleton** | `AppSettings.xml` の永続化・設定値管理 | `IPathService`, `XmlIoLock` |
| **基盤** | `IAppearanceSettingsService`| `AppearanceSettingsService` | **Singleton** | UIテーマ（Dark/Default）、フォントスケール、トレイアイコン選択・バッテリー変化通知 | `IAppSettingsService` |
| **基盤** | `IProfileXmlStore` | `ProfileXmlStore` | **Singleton** | プロファイルXMLのシリアライズ・デシリアライズ | `IPathService`, `XmlIoLock` |
| **基盤** | `IProfileRepository` | `ProfileRepository` | **Singleton** | プロファイル名一覧の保持、CRUD、存在検証、リンクプロファイル解決 | `IProfileXmlStore`, `IPathService` |
| **基盤** | `IProfileSettingsService` | `ProfileSettingsService` | **Singleton** | ロード済みプロファイルのインメモリ設定値管理 | `IProfileXmlStore` |
| **基盤** | `IDeviceOptionRepository` | `DeviceOptionRepository` | **Singleton** | 機種別オプション（`*ControllerOptsDTO`）の管理 | `IPathService`, `XmlIoLock` |
| **基盤** | `ISpecialActionRepository` | `SpecialActionRepository` | **Singleton** | `Actions.xml` のCRUDおよび定義管理 | `IPathService`, `XmlIoLock` |
| **基盤** | `IOutputSlotStore` | `OutputSlotStore` | **Singleton** | スロット永続化設定（`OutputSlots.xml`）の管理 | `IPathService`, `XmlIoLock` |
| **1.入力**| `IDeviceHotplugMonitor` | `DeviceHotplugMonitor` | **Singleton** | Win32 RAW/HID デバイス到着・抜去イベントの検知 | なし |
| **1.入力**| `IDs4DeviceRegistry` | `Ds4DeviceRegistryAdapter` | **Singleton** | コントローラーの列挙・保持、通信ハンドルの監視 | `IDeviceHotplugMonitor` |
| **1.入力**| `IDeviceStateService` | `DeviceStateService` | **Singleton** | 接続中デバイスのバッテリー残量、通信種別管理、初回接続判定・シリアル変更通知 | `IDs4DeviceRegistry` |
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
| **2.変換**| `IProfileActionChainService` | `ProfileActionChainService` | **Singleton** | アクションの連鎖実行・ライフサイクル管理 | `IProfileActionProvider` |
| **2.変換**| `IAutoProfileService` | `AutoProfileService` | **Singleton** | フォアグラウンドアプリ監視とプロファイル自動切替 | `IProcessInspector`, `IProfileApplicationService` |
| **2.変換**| `IMappingActionDispatcher` | `MappingActionDispatcher` | **Singleton** | パイプラインからのアクション発火要求の非同期分配 | `IManagedActionManager` |
| **2.変換**| `IManagedActionManager` | `DefaultActionManager` | **Singleton** | マクロ・キー・プロファイル切替アクションの実行統括 | `IActionFactory` |
| **2.変換**| `ControlService` | `ControlService` | **Singleton** | 入力・変換・出力パイプライン全体の開始・停止統括（ファサード） | `IDs4DeviceRegistry`, `IInputLoopCoordinator`, `IOutputSlotService`※1, `IProfileSettingsService`, `IAppSettingsService`, `IEnvironmentService`, `IPathService`, `IVirtualKBM`, `IVirtualKBMLifecycle`, `IProfileRepository`, `IDeviceStateService`, `IAppearanceSettingsService`, `IProfileXmlStore`, `IProfileActionProvider`, `IProfileSlotApplier` |
| **3.出力**| `IOutputSlotService` | `OutputSlotService` | **Singleton** | 仮想Xbox360/DS4（ViGEm）の生成、割当、切替 | `IOutputSlotStore` |
| **3.出力**| `IVirtualKBM` | `OutputKBMHandlerAdapter` | **Singleton** | SendInput / FakerInput によるキー・マウス送出 | なし |
| **3.出力**| `IVirtualKBMLifecycle` | `OutputKBMHandlerLifecycle` | **Singleton** | KBM出力ハンドラの生成・破棄・フォールバック切替・マッピング初期化（送出は `IVirtualKBM` が担当） | `IEnvironmentService` |
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
| `OutputSlotService` → `ControlService` | コンストラクタで `_control = control ?? Program.rootHub`、`_slotManager` を `_control?.OutputslotMan` から取得 | `ControlService` 生成中に解決すると `_control` が null のまま固定され、`OutputSlotManager` が二重化する恐れ | `ControlService` は `Func<IOutputSlotService>` で遅延解決し、`ActiveOutDevType` 配列参照を初回にキャッシュ（決定D1） | Phase6-Step5（`OutputSlotService` 全面SSOT統合）。解消後は直接注入へ戻す |
| `ProfileApplicationService` → `ControlService` | コンストラクタで `control ?? AppHost.GetService<ControlService>()` | `ControlService` 生成中に解決すると再帰生成の恐れ | `ControlService` からは直接注入しない（C2-47 は決定O1=B／O4=B-2 により、`ControlService` が定義する出力ポート `IProfileSlotApplier` 経由） | 未定（Step2-2 の決定に従い、必要なら担当Stepを追加） |
| `ProfileRepository` → `ControlService` | メソッド内で `AppHost.GetService<ControlService>()` を実行時に呼ぶ（コンストラクタ依存ではない） | 生成順序の問題は無いが、Service Locator が残る | 現状維持 | 未定 |

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
