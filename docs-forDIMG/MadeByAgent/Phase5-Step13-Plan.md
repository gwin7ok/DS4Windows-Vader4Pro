# フェーズ5-Step13 計画書: UI層（ViewModels および MainWindow）のDIサービス接続・残存静的参照の撲滅

作成日: 2026-09-03（改訂日: 2026-09-05・実コード検証に基づき対象ファイルとスコープを修正／同日第2版: Step13-2実装に伴いIProfileRepository拡張を反映／同日第3版: Step13-4実装に伴いIAutoProfileService拡張とAutoProfileHolder二重インスタンス問題の是正を反映／2026-09-06第4版: Step13-5実装に伴いIProfileRepository再拡張とGlobal.RemoveAction自動保存機能欠落の是正を反映／2026-09-06第5版: Step13-6実装（RecordBoxViewModel・ProfileSettingsViewModel）に伴いIOutputSlotService拡張を反映／2026-09-06第6版: Step13-7着手・IAppSettingsService孤立プロパティ根本修正とIAppSettingsService/IProfileRepository再拡張を反映（MainWindow.xaml.cs Tier1・Tier2一部完了、残り継続中）／第7版: Step13-7 Tier1・Tier2全件完了（Notifications/SwipeProfiles/LoadTempProfile/activeOutDevType対応）を反映。Tier3・Tier4は次マイクロステップへ）／第8版: Step13-7 Tier3完了（App.rootHub/Program.rootHub全67箇所をcontrolService注入済みフィールド参照に置換、ViGEmドライバ通信順序・サービスライフサイクルは無変更のためガードレール§5.5完全準拠）を反映。Tier4のみ残存）／第9版: Step13-7 Tier4精査完了（`useDInputOnly`→既存`IProfileSettingsService`、`Global.store.EmitMissingActionLogsForDevice`→`IProfileRepository`新設メソッドに置換。残り約20件は個別調査のうえ理由付きで対象外に確定）を反映。MainWindow.xaml.cs改修は`App.rootHub`他ファイル対応（Step13-8前提）を除き完了）／2026-09-07第10版: Step13-7完全完了を反映。残存6ファイルの`App.rootHub`対応完了、および`.csproj`のビルド阻害要因（WPF自動生成コード二重コンパイル）の発見・是正を追記）／2026-09-07第11版: Step13-8完了（`App.xaml.cs`の`rootHub`シムプロパティ完全削除、内部12箇所を`DS4Windows.Program.rootHub`直参照に置換、常にno-opだった冗長な再代入2箇所を削除）を反映
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md` §2, §3 Step13（Phase5詳細計画書）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理）
- `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`（Step1監査結果）
- `docs-forDIMG/MadeByAgent/Phase5-Step1-Addendum-Findings-Report.md`（追加監査・App.rootHub課題）
- Step 2〜Step 12 までの全個別計画書（本Stepの依存基盤）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - 各 ViewModel および `MainWindow` が公開している XAML バインディングプロパティ名・型は一切変更せず、内部の読み書きロジックのみを DI サービス呼び出しに差し替える。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - 画面上の全設定項目、トレイ最小化、通知表示、プロファイル選択ドロップダウンの即時反映、ログ表示を100%維持する。
- **§2.3 ログ出力の厳格な維持**:
  - UI 操作に伴うログ出力を維持する。
- **§3.1 DI (Dependency Injection) の実装（最重要）**:
  - **Pure DI（純粋コンストラクタ注入）の堅持**: ViewModel や View に `AppHost.Services`（Service Locator）を過度に持ち込ませず、`ViewModelFactory` を通じてサービスを明示的に注入する。
- **§3.2 巨大ファイルの編集方針**:
  - `MainWindow.xaml.cs`（1,887行）および各 ViewModel 内の改修は、静的アクセスを行っているメソッド単位でピンポイントに置換する。

---

## 0. Step13の位置づけと現状分析

### 0.1 対象範囲と現状の課題（GitHub実コード確認済み）
`Phase5-Step1-legacy-delegation-audit-report.md` §4-5 ④、および `Phase5-Step1-Addendum-Findings-Report.md`（発見1）に基づき、全 ViewModel 群、`IViewModelFactory`、および **`MainWindow.xaml.cs`（コードビハインド、1,887行）** を対象とする。

1. **外枠のみの DI 化（Phase 4 残余課題）**:
   Phase 4 で `IViewModelFactory` が導入されたが、ViewModel 内部の業務ロジックには依然として数百箇所の `Global.ProfilePath[...]`、`Global.SaveSettings()`、`Program.rootHub.DS4Controllers` が直参照として残存している。
2. **`MainWindow.xaml.cs`（View側コードビハインド）の大量残存**:
   1,887行ある `MainWindow.xaml.cs` は、サービス起動・停止、トレイアイコン制御、スロット管理コントロールの初期化において `Program.rootHub`（旧 `App.rootHub`）や `Global` を直接叩いている。
3. **バックエンドの完成と接続待機**:
   Step 2〜12 により、プロファイル・設定・アクション・デバイス・スロットの全バックエンドが純粋な DI 契約として完成した。本 Step 13 は、これらを UI 層（ViewModel および MainWindow）に接続し、静的アクセスを一掃する Phase 5 の総仕上げステップである。

### 0.2 全体4層モデルにおける位置づけ
本Stepは **第4層 4-a UI／プレゼンテーション層（最上位）** に属する。UI 層が直接第1層・第2層（`Global` / `Mapping`）へ逆流・短絡している最後の残存経路を完全に遮断する。

---

## 1. 設計方針とアーキテクチャ

事前検討に基づき、**論点1：案A（マイクロステップによるコア 4 大 ViewModel 優先の順次改修）** および **論点2：案1（Pure DI 原則の堅持: `ViewModelFactory` 経由の明示的コンストラクタ注入）** を採用し、さらに **`MainWindow.xaml.cs` の静的参照排除** を組み込む。

### 1.0 実コード検証による前提の修正（2026-09-05追記）
Step13着手前の実コード確認（GitHubリモート・ローカルクローン照合済み）により、以下の2点が判明したため、本計画の実施範囲・順序を修正する。

1. **①の対象ファイル誤り**: `ControllersViewModel.cs`（34行）は既に `IDeviceStateService` 等がコンストラクタ注入済みの薄いクラスであり、当初想定していたプロファイル切替ロジックの直参照は存在しない。実際にその処理を保持しているのは `ControllerListViewModel.cs`（725行、`Global.` 参照41件）であるため、以降①の対象はこちらに読み替える（詳細は §1.2①'）。
2. **1.3記載の `LogViewModel.cs` は対象外**: 実コード確認の結果、`Global.exeversion` 参照1件のみで、パス解決等の静的委譲ロジックは存在しないことが判明した。本Stepのサブ ViewModel 対象から除外する。

上記に伴い、マイクロタスクの実施順序を **Step13-3（SettingsViewModel）を先行実施 → Step13-2（ControllerListViewModel、対象訂正後）は次回マイクロステップで実施** に変更する。番号体系は追跡性維持のため変更しない。

### 1.1 Pure DI の堅持と `ViewModelFactory` の拡張
`ViewModelFactory` のコンストラクタで Step 2〜12 の新設サービスを受け取り、各ファクトリメソッド経由で ViewModel のコンストラクタへ手渡しで注入する。

```csharp
// ViewModelFactory.cs 実装イメージ
public class ViewModelFactory : IViewModelFactory
{
    private readonly IProfileRepository _profileRepo;
    private readonly IProfileApplicationService _profileAppService;
    private readonly IAppSettingsService _appSettingsService;
    private readonly IAutoProfileService _autoProfileService;
    private readonly ISpecialActionRepository _specialActionRepo;
    private readonly IDs4DeviceRegistry _deviceRegistry;
    private readonly IOutputSlotService _outputSlotService;

    public ViewModelFactory(
        IProfileRepository profileRepo,
        IProfileApplicationService profileAppService,
        IAppSettingsService appSettingsService,
        IAutoProfileService autoProfileService,
        ISpecialActionRepository specialActionRepo,
        IDs4DeviceRegistry deviceRegistry,
        IOutputSlotService outputSlotService)
    {
        _profileRepo = profileRepo;
        _profileAppService = profileAppService;
        _appSettingsService = appSettingsService;
        _autoProfileService = autoProfileService;
        _specialActionRepo = specialActionRepo;
        _deviceRegistry = deviceRegistry;
        _outputSlotService = outputSlotService;
    }

    public ControllersViewModel CreateControllersViewModel()
    {
        return new ControllersViewModel(_profileRepo, _profileAppService, _deviceRegistry);
    }

    public SettingsViewModel CreateSettingsViewModel()
    {
        return new SettingsViewModel(_appSettingsService);
    }
}
```

---

### 1.2 コア 4 大 ViewModel の接続方針（段階的置換）

#### ①' `ControllerListViewModel.cs`（メイン画面・プロファイル切替、実体725行）※2026-09-05訂正・実装済み
- **訂正理由**: 当初 `ControllersViewModel.cs` を対象としていたが、実コード確認の結果、同ファイルは34行で `IDeviceStateService`／`IProfileSettingsService`／`IProfileRepository` が既にコンストラクタ注入済みの薄いクラスであり、変更の必要がないことが判明した。プロファイル切替・コントローラー一覧ロジック（`Global.` 参照41件）は実際には `ControllerListViewModel.cs`（725行、`ControllerListViewModel`／`CompositeDeviceModel`の2クラス構成）に存在するため、以降①の対象はこちらに読み替える。
- **追加判明事項**: 調査の結果、`Global.ApplyProfile(...)` 呼び出しはこのファイルに存在せず、また `IProfileRepository`（Step2成果物）には `Global.ProfilePath` 等のスロット別実行時プロファイル配列や LinkedProfile 管理メソッドが未定義であることが判明した。これらは `m_Config`（BackingStore）への薄い公開アクセサ（`Global.ProfilePath` 等）として既に単一の実体で存在していたため、**`IProfileRepository` を拡張**して集約する設計とした（状態の複製は発生しない）。
- **拡張したメンバー（`IProfileRepository`）**:
  - `string[] ProfilePath { get; }` / `OlderProfilePath` / `SelectedProfile` / `LinkedProfileUI`
  - `event EventHandler<SelectedProfileChangedEventArgs> SelectedProfileChanged` / `RaiseSelectedProfileChanged(...)`
  - `ChangeLinkedProfile(...)` / `RemoveLinkedProfile(...)` / `SaveLinkedProfiles()`（LinkedProfile管理）
- **置換内容**: `ControllerListViewModel` と `CompositeDeviceModel` の両クラスに `IProfileRepository profileRepo`（オプショナル引数＋`Global.ProfileRepositoryInstance`フォールバック）を注入し、上記の `Global.*` 直参照をすべて `profileRepo.*` に置換。既に注入済みの `profileSettingsService.LightbarSettingsInfo[devIndex]` で代替可能だった `Global.LightbarSettingsInfo[devIndex]`（6箇所）も合わせて置換。`Global.Save()`（1箇所）は Step13-3 と同様のパターンで `IAppSettingsService` を注入し `appSettings?.Save()` に置換。
- **対象外とした参照**: `Global.RESOURCES_PREFIX`（コンパイル時定数、状態を持たないためDI化不要）。
- **`ControllersViewModel.cs`（34行）**: 既存のDI構成を維持し、本Stepでの追加変更は不要と判断する。
- **呼び出し元互換性**: `MainWindow.xaml.cs:128` の `new ControllerListViewModel(...)` は3引数呼び出しのままで、追加したオプショナル引数（`profileRepo`/`appSettings`）はデフォルト値`null`＋DI解決フォールバックにより無修正で動作する（§2.1原則遵守）。

#### ② `SettingsViewModel.cs`（アプリ全体設定）※2026-09-05実コード確認により本Stepの先行実施対象
- **置換前**: `DS4Windows.Global.Save()` の直接呼び出し（5箇所）
- **置換後**: コンストラクタ注入された `IAppSettingsService _appSettings` インスタンスの `_appSettings.Save()` に置換する。
- **注記**: `IAppSettingsService` インターフェースに `RunAtStartup` プロパティは存在しない（実装済みは `StartMinimized`／`MinimizeToTaskbar`／`CloseMinimizes`／`CheckWhen`／`UseUdpServer`／`UdpServerPort`／`UdpServerListenAddress`／`UseExclusiveMode`／`AutoProfileRevertDefaultProfile` の9項目）。`RunAtStartup` 等の起動設定は `SettingsViewModel` 内部の別ロジック（`StartupMethods` 経由）で管理されており、本Stepでは対象外とする。

#### ③ `AutoProfilesViewModel.cs`（自動プロファイル切替設定）※2026-09-05実装済み
- **置換前**: `Global.AutoProfileRevertDefaultProfile`／`Global.autoProfileSwitchNotifyChoice`／`App.rootHub.CheckHidHidePresence(...)` の静的直参照
- **置換後**: `IAppSettingsService`（`AutoProfileRevertDefaultProfile`）、`IAutoProfileService`（新設 `AutoProfileSwitchNotifyChoice`）、`ControlService`（コンストラクタ注入、`Program.rootHub`フォールバック）にそれぞれ置換。いずれもDI解決に失敗した場合のみ`Global`/`Program.rootHub`へフォールバックする二重安全構成とし、機能欠落を防止。
- **🔴重大発見: `AutoProfileHolder`二重インスタンス問題（Step1監査・Addendum未記載の新規発見）**:
  - DIコンテナが構築する`IAutoProfileService`（`AutoProfileService`）は、コンストラクタ引数`holder`が未指定のため自前で`new AutoProfileHolder()`を生成していた。
  - 一方、UI側（`AutoProfiles.xaml.cs`）も独立して別の`new AutoProfileHolder()`を生成しており、`AutoProfilesViewModel`はこちらを操作していた。UI層全体を検索しても`IAutoProfileService`への参照は0件。
  - **影響**: 設定画面でAutoProfileルールを追加・編集・保存しても、バックグラウンド監視（`AutoProfileService.CheckProfiles`）側のHolderには反映されず、アプリ再起動まで自動切替に反映されない可能性があった。
  - **是正内容**: `IAutoProfileService`に`AutoProfileHolder Holder { get; }`を追加して唯一の実体を公開し、`AutoProfiles.xaml.cs`はこの共有インスタンスを参照するよう変更（Reloadではなく単一実体の共有により根本解消）。

#### ④ `SpecialActionsListViewModel.cs`（SpecialAction一覧）※2026-09-06実装済み
- **置換前**: `Global.GetActions()`／`Global.ProfileActions`／`Global.OutContType`／`Global.RemoveAction(...)`／`Global.CacheExtraProfileInfo(...)` の静的直参照
- **置換後**: `ISpecialActionRepository`（`ActionList`／`RemoveAction`／`SaveActions`）、`IProfileRepository`（新設 `ProfileActions`／`CacheExtraProfileInfo`）、`IOutputSlotService`（既存 `GetOutputDeviceType`）、`IManagedActionManager`（`ClearAllEntries`）にそれぞれ置換。
- **🔴重大発見: `Global.RemoveAction`の隠れた自動保存機能**:
  - `Global.RemoveAction`は内部で`m_Config.RemoveAction(name)`を呼んでおり、これが削除と同時に`SaveActions()`（XML保存）まで自動実行していた。
  - 一方、Step7で作られた`ISpecialActionRepository.RemoveAction`はリストからの削除のみで自動保存を行わない設計だったため、単純に置き換えると「UIでアクションを削除してもXMLに保存されない」という機能後退が起きるところだった。
  - **是正内容**: `specialActionRepo.RemoveAction(...)`が`true`を返した場合に明示的に`specialActionRepo.SaveActions()`を呼ぶよう実装。また`Global.RemoveAction`が行っていた`ActionManager.ClearAllEntries()`（実体はStep9の`IManagedActionManager.ClearAllEntries()`への薄い委譲）も`IManagedActionManager`を直接注入して踏襲。削除後のActions.xml再読込・件数検証処理（`ReloadActionsAndVerify`）は、Step7でBackingStore一本化済みのため不要と判断し省略。
- **`IProfileRepository`の再拡張**: `List<string>[] ProfileActions { get; }`（デバイス別・割当済みSpecialAction名リスト）、`void CacheExtraProfileInfo(int deviceIndex)` を追加（`m_Config`への薄い公開アクセサ、状態複製なし）。
- **対象外とした参照**: `Global.NormalizeActionName`・`Global.getX360ControlString`（いずれも状態を持たない純粋な文字列処理のため）。

---

### 1.3 サブ ViewModels の静的参照撲滅
コア 4 画面の改修完了後、以下のサブ画面の残存参照を順次サービス経由に置換する。
- `RecordBoxViewModel.cs`※2026-09-06実装済み: `Global.TouchOutMode`→既存`IProfileSettingsService.TouchOutMode`、`Program.rootHub`→新規注入`ControlService`に置換。計画書記載の「`IMacroPlayer`活用」は実態と異なり（`IMacroPlayer`はマクロ*再生*用、本ファイルは*記録*用で無関係）誤記と判明。`Global.macroDS4Values`／`Global.RESOURCES_PREFIX`は状態を持たない定数のため対象外。
- `ProfileSettingsViewModel.cs`※2026-09-06実装済み（4248行、実質22箇所を精査）: `Global.LaunchProgram`／`GetDS4CSetting`／`GetSAMouseStickTriggerCond`／`SetSaMouseStickTriggerCond`は既存`IProfileSettingsService`シムを活用、`App.rootHub`（8箇所）は新規注入`ControlService`に、`Global.OutContType`は既存`IOutputSlotService.GetOutputDeviceType`に置換。`Global.outDevTypeTemp`（3箇所）は`IOutputSlotService`に`OutDevTypeTemp`を新設して対応、`Global.CacheProfileCustomsFlags`（1箇所）は`IProfileRepository`に追加して対応。
  - **対象外とした参照（要検証のうえ維持）**: `Global.RefreshActionAlias`（引数への純粋操作で状態を持たない）、`Global.IsUsingMinViGEm117333()`（読み取り専用のViGEmドライババージョン判定）、`Global.defaultButtonMapping`（コードベース全体で読み取りのみと確認済みの固定配列）、`Global.exedirpath`（`IPathService.ExecutableDirectory`実装が`AppContext.BaseDirectory`ベースであり`Directory.GetParent(exelocation)`と挙動が異なる可能性を排除できなかったため、未検証の置換によるパス不具合リスクを避けて維持）。
- ~~`LogViewModel.cs`~~: 2026-09-05実コード確認の結果、`Global.exeversion` 参照1件のみでパス解決等の静的委譲ロジックは存在しないことが判明したため、本Stepの対象から除外する。

---

### 1.4 `MainWindow.xaml.cs`（コードビハインド）の静的参照排除とシム完全削除
`MainWindow.xaml.cs` に残存する `Program.rootHub`（旧 `App.rootHub`）および `Global` への直アクセスを、DI サービス群（`IControlService`、`IDeviceStateService`、`IOutputSlotService`、`IUdpServerService`）経由にピンポイント置換する。

- **最終クリーンアップ**:
  `MainWindow.xaml.cs` からの直参照が完全に 0 件になったことを確認した時点で、`App.xaml.cs` に一時的に設けた `public static DS4Windows.ControlService rootHub => Program.rootHub;` シムプロパティを**完全に削除**する。

---

### 1.5 UI スレッド安全性の保証とイベント購読解除
- サービス側から発火される変更イベント（`SettingChanged`、`DeviceAttached` 等）を ViewModel や View で購読する際は、**必ず `Application.Current.Dispatcher.Invoke / BeginInvoke` を介して UI プロパティを更新**する。
- 画面クローズ時のメモリリークを防ぐため、`IDisposable` を実装してイベントハンドラーの解除（`-=`）を徹底する。

---

## 2. 成果物一覧

| 種別 | ファイルパス | 変更内容 |
|---|---|---|
| ファクトリ改修 | `DS4Windows/DI/IViewModelFactory.cs` / `ViewModelFactory.cs` | 新設DIサービスのコンストラクタ注入と ViewModel への手渡し配線 |
| コアVM改修 | `DS4Windows/DS4Forms/ViewModels/ControllerListViewModel.cs`（※2026-09-05訂正、旧`ControllersViewModel.cs`から対象変更） | プロファイル・デバイス操作を DI サービス経由に置換（`ControllersViewModel.cs` 自体は変更なし） |
| インターフェース拡張 | `DS4Windows/DI/IProfileRepository.cs` | スロット別実行時プロファイル配列（ProfilePath等）・SelectedProfileChangedイベント・LinkedProfile管理メソッドを追加（Step13-2実装に伴う拡張） |
| 実装改修 | `DS4Windows/DS4Control/Services/ProfileRepository.cs` | 上記拡張メンバーを実装（m_Config/Globalへの薄い公開アクセサとして、状態複製なし） |
| コアVM改修 | `DS4Windows/DS4Forms/ViewModels/SettingsViewModel.cs` | `IAppSettingsService` 接続、`Global.SaveSettings` 排除 |
| コアVM改修 | `DS4Windows/DS4Forms/ViewModels/AutoProfilesViewModel.cs` | `IAppSettingsService`／`IAutoProfileService`／`ControlService` 接続 |
| インターフェース拡張 | `DS4Windows/DI/IAutoProfileService.cs` | `Holder`（AutoProfileHolder二重インスタンス問題是正）・`AutoProfileSwitchNotifyChoice` を追加 |
| 実装改修 | `DS4Windows/DS4Control/Services/AutoProfileService.cs` | 上記拡張メンバーを実装 |
| View改修 | `DS4Windows/DS4Forms/AutoProfiles.xaml.cs` | 独自`AutoProfileHolder`生成を廃止し`IAutoProfileService.Holder`を共有 |
| コアVM改修 | `DS4Windows/DS4Forms/ViewModels/SpecialActionsListViewModel.cs` | `ISpecialActionRepository`／`IProfileRepository`／`IOutputSlotService`／`IManagedActionManager` 接続 |
| インターフェース再拡張 | `DS4Windows/DI/IProfileRepository.cs` | `ProfileActions`・`CacheExtraProfileInfo` を追加（Step13-5実装に伴う拡張） |
| 実装改修 | `DS4Windows/DS4Control/Services/ProfileRepository.cs` | 上記拡張メンバーを実装 |
| View改修 | `DS4Windows/DS4Forms/MainWindow.xaml.cs` | `Program.rootHub` / `Global` 直参照の排除、DIサービス経由化（※2026-09-06 Tier1・Tier2・Tier3全件完了、Tier4のみ残存） |
| 🔴根本修正 | `DS4Windows/DI/IAppSettingsService.cs` | `StartMinimized`／`MinimizeToTaskbar`／`CloseMinimizes`／`UseUdpServer`／`UdpServerPort`／`UdpServerListenAddress`／`UseExclusiveMode`が独立private fieldで`Global`と非連動だった「孤立重複状態」バグを是正。ウィンドウ位置・サイズ・列幅10種を追加 |
| 実装改修 | `DS4Windows/DS4Control/Services/AppSettingsService.cs` | 上記の根本修正および拡張メンバーを実装 |
| クリーンアップ | `DS4Windows/App.xaml.cs` | 不要となった `rootHub` シムプロパティの完全削除 |
| サブVM改修 | `DS4Windows/DS4Forms/ViewModels/RecordBoxViewModel.cs` | `IProfileSettingsService`／`ControlService` 接続 |
| サブVM改修 | `DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs` | `ControlService`／`IOutputSlotService`／`IProfileRepository` 接続、残存静的参照22箇所を置換 |
| インターフェース拡張 | `DS4Windows/DI/IOutputSlotService.cs` | `OutDevTypeTemp`（Profile Editor未確定出力タイプ）を追加 |
| 実装改修 | `DS4Windows/DS4Control/Services/OutputSlotService.cs` | 上記拡張メンバーを実装 |
| インターフェース再拡張 | `DS4Windows/DI/IProfileRepository.cs` | `CacheProfileCustomsFlags` を追加 |
| 実装改修 | `DS4Windows/DS4Control/Services/ProfileRepository.cs` | 上記拡張メンバーを実装 |
| 単体テスト拡充 | `DS4WindowsTests/PatternAViewModelTests.cs` 等 | モックサービスを用いた各 ViewModel の完全自動テスト |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step13-1: `ViewModelFactory` の拡張
1. `IViewModelFactory` および `ViewModelFactory.cs` に新設サービス群を追加注入。
2. `ServiceRegistration.cs` における Factory の依存解決を確認。

### タスク Step13-2: `ControllerListViewModel` の DI 接続（コア①、※対象ファイル訂正・実施済み 2026-09-05）
1. `ControllerListViewModel.cs`（725行、`ControllerListViewModel`／`CompositeDeviceModel`の2クラス）の `Global.` 参照41件を棚卸しし、`IProfileRepository`・`IAppSettingsService` をコンストラクタ注入。
2. `IProfileRepository` を拡張（ProfilePath/OlderProfilePath/SelectedProfile/LinkedProfileUI/SelectedProfileChanged/LinkedProfile管理メソッド）し、`ProfileRepository.cs` に実装。
3. `Global.ProfilePath`、`Global.LinkedProfileUI`、`Global.changeLinkedProfile` 等の静的アクセスを `profileRepo.*` に、`Global.LightbarSettingsInfo` を既存注入済み `profileSettingsService.LightbarSettingsInfo` に、`Global.Save()` を `appSettings?.Save()` にピンポイント置換。
4. `Global.RESOURCES_PREFIX`（定数）は対象外として維持。
5. `ControllersViewModel.cs`（34行）は既存DI構成を維持し、追加変更は行わない。
6. `MainWindow.xaml.cs` の呼び出し元は無修正（オプショナル引数フォールバックにより互換性維持）。

### タスク Step13-3: `SettingsViewModel` の DI 接続（コア②、※本セッションで先行実施）
1. `SettingsViewModel.cs` に `IAppSettingsService` をコンストラクタ注入（インスタンスフィールド `_appSettings`）。
2. `DS4Windows.Global.Save()` の直接呼び出し5箇所を `_appSettings.Save()` に置換。

### タスク Step13-4: `AutoProfilesViewModel` の DI 接続（コア③、※実施済み 2026-09-05）
1. `IAutoProfileService` を拡張し `Holder`（唯一のAutoProfileHolder実体を公開）・`AutoProfileSwitchNotifyChoice` を追加、`AutoProfileService.cs` に実装。
2. `AutoProfiles.xaml.cs` の独自 `new AutoProfileHolder()` を廃止し、`IAutoProfileService.Holder` を共有することで二重インスタンス問題を根本解消。
3. `AutoProfilesViewModel.cs` に `IAppSettingsService`・`IAutoProfileService`・`ControlService`（コンストラクタ注入、オプショナル引数＋フォールバック）を追加し、`Global.AutoProfileRevertDefaultProfile`／`Global.autoProfileSwitchNotifyChoice`／`App.rootHub.CheckHidHidePresence` を置換。
4. `IViewModelFactory.CreateAutoProfilesViewModel` および全呼び出し元は2引数のまま無修正（オプショナル引数フォールバックにより互換性維持）。

### タスク Step13-5: `SpecialActionsListViewModel` の DI 接続（コア④、※実施済み 2026-09-06）
1. `IProfileRepository` を再拡張（`ProfileActions`／`CacheExtraProfileInfo`）し、`ProfileRepository.cs` に実装。
2. `SpecialActionsListViewModel.cs` に `ISpecialActionRepository`・`IProfileRepository`・`IOutputSlotService`・`IManagedActionManager`（いずれもオプショナル引数＋フォールバック）を注入。
3. `Global.RemoveAction`の隠れた自動保存機能（`SaveActions()`）とトグル状態クリア（`ActionManager.ClearAllEntries()`）を、DIサービス直接呼び出し（`specialActionRepo.SaveActions()`＋`actionManager.ClearAllEntries()`）で明示的に踏襲し、機能欠落を防止。
4. `Global.GetActions()`／`Global.ProfileActions`／`Global.OutContType`をそれぞれ対応するDIサービスにピンポイント置換。`Global.NormalizeActionName`（状態を持たない純粋関数）は対象外として維持。
5. `ProfileEditor.xaml.cs`の呼び出し元は無修正（オプショナル引数フォールバックにより互換性維持）。

### タスク Step13-6: サブ ViewModels の静的参照撲滅（※実施済み 2026-09-06）
1. `RecordBoxViewModel.cs`: `IProfileSettingsService`（既存`TouchOutMode`活用）・`ControlService`（新規注入）に置換。
2. `IOutputSlotService`に`OutDevTypeTemp`、`IProfileRepository`に`CacheProfileCustomsFlags`をそれぞれ追加し、`OutputSlotService.cs`／`ProfileRepository.cs`に実装。
3. `ProfileSettingsViewModel.cs`（4248行）: `LaunchProgram`・`GetDS4CSetting`・`SAMouseStickTriggerCond`系は既存`IProfileSettingsService`シムへ、`App.rootHub`（8箇所）は新規注入`ControlService`へ、`OutContType`／`outDevTypeTemp`は`IOutputSlotService`へそれぞれ置換。
4. `RefreshActionAlias`・`IsUsingMinViGEm117333`・`defaultButtonMapping`・`exedirpath`は個別調査のうえ、状態を持たない／既存DI実装との挙動差異が排除できないことを理由に対象外として維持（詳細は§1.3参照）。
5. `ProfileEditor.xaml.cs`・`RecordBox.xaml.cs`の呼び出し元は無修正（オプショナル引数フォールバックにより互換性維持）。

### タスク Step13-7: `MainWindow.xaml.cs` の静的参照排除（※2026-09-06着手、Tier1・Tier2一部完了・継続中）
1. **事前調査の結果、166件（`Global.`102・`Program.rootHub`17・`App.rootHub`53、`ImageLocationPaths`内定数除く）という広範囲であることが判明**。リスクに応じ以下の3層に分類し、段階的に対応する。
   - **Tier1（低リスク・完了）**: `Global.appdatapath`→`IPathService.AppDataPath`、`Global.Form*`（4種）・`Global.Controller*ColWidth`（10種）→`IAppSettingsService`新設プロパティ、`Global.RESOURCES_PREFIX`は定数のため対象外。
   - **Tier2（中リスク・一部完了）**: `Global.ApplyProfile`呼び出し4箇所を、Halt保護・通知自動解決を内包する`IProfileApplicationService.ApplyProfile`へ簡略化（手動`HaltReportingRunAction`・手動通知読み取りが不要になり計16行削減）。`Global.StartMinimized`／`MinToTaskbar`／`CloseMini`→根本修正後の`IAppSettingsService`、`Global.ProfilePath`／`SelectedProfile`→既存`IProfileRepository`、`Global.OutContType`→既存`IOutputSlotService`を適用済み。`Global.Notifications`／`SwipeProfiles`は`IAppSettingsService`に、`Global.LoadTempProfile`は`IProfileRepository`に、`Global.activeOutDevType`は`IOutputSlotService`にそれぞれ追加して置換完了（Tier2全件完了）。`CheckUpdateStartupEnabled`系（SettingsViewModel Step13-3と同様、対応するDIサービスが無いため据え置き）・`LogMinLevel`は据え置き（Tier4相当として次のマイクロステップで検討）。
   - **🔴重大発見と根本修正**: `IAppSettingsService.StartMinimized`等7プロパティが独立privateフィールドを持ち`Global`側の実体と一切連動しない「孤立重複状態」バグを発見（Step13-3調査時の`ProfileSettingsService`類似問題と同種）。`AppSettingsService.cs`を修正し、全て`Global`への正規シムに統一。
   - **Tier3（高リスク・完了 2026-09-06）**: `Start`/`Stop`/`running`/`suspending`（サービスライフサイクル）、`OutputslotMan`/`AttachUnboundOutDev`/`DetachUnboundOutDev`（ViGEm §5.5ガードレール対象）、`DS4Controllers`（17件）を含む残存`App.rootHub`/`Program.rootHub`全67箇所（65行）を、既にコンストラクタ注入済みの`controlService`フィールド（`Program.rootHub`と同一実体）への参照に置換。**新しい抽象化層・インターフェースは一切導入せず、「同一オブジェクトへの到達経路を静的フィールドアクセスから注入済みフィールド参照に変えるだけ」の純粋な薄いアダプター境界**であるため、ViGEmドライバの通信キュー・破棄順序、サービスの起動停止シーケンスへの影響はゼロ（§5.5ガードレール完全準拠）。これにより`MainWindow.xaml.cs`からの`App.rootHub`参照は0件になった（他ファイル：`StickCalibrationWindow.xaml.cs`／`BindingWindow.xaml.cs`／`ProfileEditor.xaml.cs`／`AutoProfileService.cs`／`PresetOption.cs`／`DS4Sixaxis.cs`にはまだ残存していたが、2026-09-07に全6ファイル対応完了（下記参照）。

   **【Tier3追補】残存6ファイルの`App.rootHub`対応（2026-09-07完了、計30箇所）**:
   | ファイル | 対応内容 |
   |---|---|
   | `StickCalibrationWindow.xaml.cs`（1箇所） | `ControlService`をコンストラクタ注入（`Program.rootHub`フォールバック） |
   | `BindingWindow.xaml.cs`（1箇所） | 同上 |
   | `ProfileEditor.xaml.cs`（13箇所） | 同上。既存の`profileRepository`/`profileSwitcher`と同じ注入スタイルに統一 |
   | `DS4SixAxis.cs`（DS4Library、1箇所） | コンストラクタ注入。`DS4Device`生成時（`Program.rootHub`確実に設定済みのタイミング）に構築されるため安全と判断 |
   | `PresetOption.cs`（10箇所、6サブクラス） | **抽象基底クラス`PresetOption`に注入を集約**し、`GamepadPreset`等6サブクラスは無修正のまま恩恵を受ける設計 |
   | `AutoProfileService.cs`（7箇所） | ⚠️別対応: 本サービスはDIコンテナのSingletonとして構築されるため、コンストラクタ実行時点で`Program.rootHub`が未設定の可能性がありフィールドキャッシュはリスクありと判断。**フィールド化せず`App.rootHub`→`Program.rootHub`への単純な文字列置換のみ**を採用（都度ライブ評価を維持し、タイミング起因の不具合リスクをゼロにする）。 |

   これにより`App.rootHub`の外部参照は全コードベースでゼロになった（`App.xaml.cs`自体の内部利用とシム定義のみ残存、Step13-8で対応）。
   - **Tier4（個別要調査・完了 2026-09-06）**: 全項目を個別調査し、以下の通り確定した。
     - **置換（2件）**: `Global.useDInputOnly[tdevice]`は既に`ProfileSettingsServiceInstance.UseDInputOnlyArray`のシムであり、既に本ファイルへ注入済みの`profileSettingsService`で置換。`Global.store.EmitMissingActionLogsForDevice(...)`（`BackingStore`インスタンスメソッドへの直接アクセス）は対応するDIサービスが無かったため`IProfileRepository`に新設し置換。
     - **対象外で維持（理由付き、約18件）**:
       - `exelocation`／`exeversion`／`TEST_PROFILE_INDEX`／`iconChoiceResources`：起動時に一度だけ計算される、またはコンパイル時定数のため状態を持たない（`RESOURCES_PREFIX`と同種）。
       - `exedirpath`／`appDataPpath`：`Global.appdatapath`（既存`IPathService.AppDataPath`で対応済み）とは**別の変数**（`appDataPpath`は大文字Dとpのタイプミスのような別名で、`Environment.GetFolderPath`ベースの固定デフォルト値）。`IPathService.ExecutableDirectory`実装（`AppContext.BaseDirectory`）との挙動差異が未検証のため、誤った置換によるパス不具合を避けて維持。
       - `IsAdministrator()`／`RefreshHidHideInfo()`／`RefreshFakerInputInfo()`：OS権限・ドライバインストール状態を都度判定する環境プローブであり、永続化された設定ではない（`IsUsingMinViGEm117333`と同種、ProfileSettingsViewModel Step13-6で確立した判断基準を踏襲）。
       - `firstRun`／`runHotPlug`：`Global`／`ControlService`内部が自ら書き換える実行時フラグであり、本ファイルからは読み取りのみ。DIサービス経由にしても実体を書き換える主体は変わらないため据え置き。
       - `LastChecked`／`LastVersionCheckedNum`／`CompileVersionNumberFromString`：バージョンチェック関連状態。`CheckUpdateStartupEnabled`系（SettingsViewModel Step13-3）と同様、対応するDIサービスが無いため一貫して据え置き。
       - `UseCurrentTheme`／`UseIconChoice`：正真の永続化設定だが、`App.xaml.cs`／`SettingsViewModel.cs`／`TrayIconViewModel.cs`／`ControlService.cs`の計5ファイルにまたがって使用されており、本ファイル単体の修正では一貫性が取れない。テーマ設定のDI化は複数ファイルにまたがる別マイクロステップとして扱うべきと判断し、今回は対象外。

   - **🔴番外: `.csproj`のビルド阻害要因の発見・是正（2026-09-07）**: Step13-7の一連の改修とは無関係に、`DS4WinWPF.csproj`末尾の以下のItemGroupが原因で、`dotnet build`（VSCode/Cursorのターミナル経由）が全WPF画面で`CS0102`（重複定義）・`CS0111`（重複メンバー）・`CS8646`（重複実装）を大量発生させ続けていたことが判明した。
     ```xml
     <!-- Cursor / VS Code の言語サーバーが WPF 自動生成ファイルを見失う問題への対策 -->
     <ItemGroup Condition="'$(BuildingInsideVisualStudio)' != 'true'">
         <Compile Include="$(IntermediateOutputPath)**\*.g.i.cs" 
                  Condition="Exists('$(IntermediateOutputPath)')" 
                  Visible="false" />
     </ItemGroup>
     ```
     このItemGroupは以前のセッションでVSCode/CursorのIntelliSense向けに追加されたものだが、WPF SDKが自動生成・自動コンパイル対象化する実ビルド用`*.g.cs`と、ここで手動追加されたデザイン時専用`*.g.i.cs`が**同一の`InitializeComponent`等を二重定義**することになり、コマンドラインビルド（`BuildingInsideVisualStudio != true`）のたびに全WPF画面でビルドエラーとなっていた。`obj`/`bin`削除では`obj`再生成のたびに再発するため、これまでの複数回のビルド失敗の根本原因はこれだった。ユーザーの判断によりこのItemGroupを削除し、ビルド・テストビルド・テスト実行・リリースビルド全て成功を確認済み。IntelliSense側の見失い問題は「.NET: Restart Language Server」等のエディタ再起動で個別対応する方針とした。

### タスク Step13-8: `App.xaml.cs` の `rootHub` シム完全削除（※実施済み 2026-09-07）
1. 全画面（Step13-7完了により`App.rootHub`の外部参照は全コードベースでゼロ）を確認後、`App.xaml.cs`の`rootHub`シムプロパティ（`Program.rootHub`への薄い委譲）を削除。
2. `App.xaml.cs`内部で`rootHub`（裸の識別子、クラス内メンバーとして参照）を使用していた12箇所を`DS4Windows.Program.rootHub`への直接参照に置換。`App.xaml.cs`は`using DS4Windows;`を持たないため、既存の記述スタイルに合わせて完全修飾名で統一。
3. `CreateControlService`/`CreateBaseThread`内にあった`DS4Windows.Program.rootHub = rootHub;`（プロパティのgetter/setterが同じ実体を指すため、常に自己代入＝no-opだった行）を2箇所削除。
4. コードベース全体を再検索し、`App.rootHub`の参照が0件であることを確認。

### タスク Step13-9: ViewModel 単体テストの拡充と自動テスト実行
    * **Watchpoint 2 の確認（UIスレッド安全性・イベント購読解除の点検・テスト）**:
      * **スレッド安全性（Cross-thread UI 操作）**:
        * `AutoProfilesViewModel` や `ControllerListViewModel` がバックグラウンドスレッド発火のイベント（`AutoProfileSystemChange`, デバイス着脱等）を受信した際、`Dispatcher` を介して UI/コレクション（`ObservableCollection`）を安全に更新しているかのコード点検および単体テスト。
      * **イベント購読解除（Unsubscribe / メモリリーク防止）**:
        * `MainWindow` または各 ViewModel の破棄・終了時（`Closed` / `Unloaded` 等）に、Singleton DI サービス（`IAutoProfileService`, `IDs4DeviceRegistry` 等）に対するイベント購読（`+=`）が確実に解除（`-=`）されていることの点検とテスト。
1. 各 ViewModel のモックテストを実行し、全画面が DI 経由で正常に初期化・バインドできることを検証。
2. `dotnet test` で全テストパスを確認。

### タスク Step13-10: ビルド検証、進捗更新、完了報告書の作成
1. Debug / Release ビルド成功を確認。
2. `Phase5-Status.md` の Step13 を「計画書承認済」に更新。
3. `Phase5-Step13-Completion-Report.md` を作成。

---

## 4. リスクと回避策

| リスク | 影響度 | 回避策 |
|---|---|---|
| **MainWindow の画面イベント破壊** | 高 | 1,887行を一括置換せず、静的参照を呼んでいるイベントメソッド単位でピンポイント置換する（§3.2）。 |
| **XAML バインディング切断** | 高 | ViewModel の公開プロパティ名や型は一切変えず、内部ロジックのみを差し替える（§2.1）。 |
| **イベント購読によるメモリリーク** | 中 | `IDisposable` を実装し、画面終了時にイベントハンドラーの解除を徹底する（§1.5）。 |
| **Service Locator への堕落** | 中 | ViewModel に `AppHost.Services` を持ち込まず、`ViewModelFactory` による純粋注入を徹底する（§1.1）。 |

---

## 5. 完了判定基準

- [ ] コア 4 大 ViewModel および全サブ ViewModel 内部から `Global`／`Program.rootHub` 直参照が 0 件になっていること。
- [ ] `MainWindow.xaml.cs` 内から `Program.rootHub`／`Global` 直参照が 0 件になっていること（§1.4）。
- [ ] `App.xaml.cs` から `rootHub` プロパティが完全に削除されていること（§1.4）。
- [ ] すべての ViewModel が `ViewModelFactory` から Pure DI で生成されていること。
- [ ] UI 上でプロファイル切替、設定保存、SpecialAction編集、AutoProfileが従来通り正常動作すること。
- [ ] 単体テストがすべてパスし、ビルドエラー・警告増がないこと。