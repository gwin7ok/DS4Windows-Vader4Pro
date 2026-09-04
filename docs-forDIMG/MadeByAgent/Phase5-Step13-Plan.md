# フェーズ5-Step13 計画書: UI層（ViewModels および MainWindow）のDIサービス接続・残存静的参照の撲滅

作成日: 2026-09-03（改訂日: 2026-09-05・実コード検証に基づき対象ファイルとスコープを修正）
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

#### ①' `ControllerListViewModel.cs`（メイン画面・プロファイル切替、実体725行）※2026-09-05訂正
- **訂正理由**: 当初 `ControllersViewModel.cs` を対象としていたが、実コード確認の結果、同ファイルは34行で `IDeviceStateService`／`IProfileSettingsService`／`IProfileRepository` が既にコンストラクタ注入済みの薄いクラスであり、変更の必要がないことが判明した。プロファイル切替・コントローラー一覧ロジック（`Global.` 参照41件）は実際には `ControllerListViewModel.cs`（725行）に存在するため、以降①の対象はこちらに読み替える。
- **置換前**: `Global.ProfilePath[deviceIndex]`、`Global.ApplyProfile(...)`、`Program.rootHub.DS4Controllers[i]` 等（`ControllerListViewModel.cs` 内、次回着手時に要再棚卸し）
- **置換後**: 注入された `_profileRepo.ProfilePath`、`_profileAppService.ApplyProfile(...)`、`_deviceRegistry.Devices`
- **`ControllersViewModel.cs`（34行）**: 既存のDI構成を維持し、本Stepでの追加変更は不要と判断する。

#### ② `SettingsViewModel.cs`（アプリ全体設定）※2026-09-05実コード確認により本Stepの先行実施対象
- **置換前**: `DS4Windows.Global.Save()` の直接呼び出し（5箇所）
- **置換後**: コンストラクタ注入された `IAppSettingsService _appSettings` インスタンスの `_appSettings.Save()` に置換する。
- **注記**: `IAppSettingsService` インターフェースに `RunAtStartup` プロパティは存在しない（実装済みは `StartMinimized`／`MinimizeToTaskbar`／`CloseMinimizes`／`CheckWhen`／`UseUdpServer`／`UdpServerPort`／`UdpServerListenAddress`／`UseExclusiveMode`／`AutoProfileRevertDefaultProfile` の9項目）。`RunAtStartup` 等の起動設定は `SettingsViewModel` 内部の別ロジック（`StartupMethods` 経由）で管理されており、本Stepでは対象外とする。

#### ③ `AutoProfilesViewModel.cs`（自動プロファイル切替設定）
- **置換前**: `AutoProfileChecker` 具象インスタンス直参照、静的設定保存
- **置換後**: 注入された `_autoProfileService` 経由での監視状態制御・ルール設定

#### ④ `SpecialActionsListViewModel.cs`（SpecialAction一覧）
- **置換前**: 静的 `Global.store.actions` 直引き
- **置換後**: 注入された `_specialActionRepo.Actions`、`AddAction`、`RemoveAction`

---

### 1.3 サブ ViewModels の静的参照撲滅
コア 4 画面の改修完了後、以下のサブ画面の残存参照を順次サービス経由に置換する。
- `RecordBoxViewModel.cs`: マクロ記録・再生（`IMacroPlayer` 活用）
- `ProfileSettingsViewModel.cs`: スロット別設定（`IProfileSettingsService` 活用）
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
| コアVM改修 | `DS4Windows/DS4Forms/ViewModels/SettingsViewModel.cs` | `IAppSettingsService` 接続、`Global.SaveSettings` 排除 |
| コアVM改修 | `DS4Windows/DS4Forms/ViewModels/AutoProfilesViewModel.cs` | `IAutoProfileService` 接続 |
| コアVM改修 | `DS4Windows/DS4Forms/ViewModels/SpecialActionsListViewModel.cs` | `ISpecialActionRepository` 接続 |
| View改修 | `DS4Windows/DS4Forms/MainWindow.xaml.cs` | `Program.rootHub` / `Global` 直参照の排除、DIサービス経由化 |
| クリーンアップ | `DS4Windows/App.xaml.cs` | 不要となった `rootHub` シムプロパティの完全削除 |
| サブVM改修 | `DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs` 等 | 残存静的参照の完全排除 |
| 単体テスト拡充 | `DS4WindowsTests/PatternAViewModelTests.cs` 等 | モックサービスを用いた各 ViewModel の完全自動テスト |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step13-1: `ViewModelFactory` の拡張
1. `IViewModelFactory` および `ViewModelFactory.cs` に新設サービス群を追加注入。
2. `ServiceRegistration.cs` における Factory の依存解決を確認。

### タスク Step13-2: `ControllerListViewModel` の DI 接続（コア①、※対象ファイル訂正・次回マイクロステップで実施）
1. `ControllerListViewModel.cs`（725行）を対象に、`Global.` 参照41件の棚卸しとコンストラクタへのサービス追加を行う。
2. `Global.ProfilePath`、`Global.ApplyProfile` などの静的アクセスを DI サービス呼び出しにピンポイント置換。
3. `ControllersViewModel.cs`（34行）は既存DI構成を維持し、追加変更は行わない。

### タスク Step13-3: `SettingsViewModel` の DI 接続（コア②、※本セッションで先行実施）
1. `SettingsViewModel.cs` に `IAppSettingsService` をコンストラクタ注入（インスタンスフィールド `_appSettings`）。
2. `DS4Windows.Global.Save()` の直接呼び出し5箇所を `_appSettings.Save()` に置換。

### タスク Step13-4: `AutoProfilesViewModel` の DI 接続（コア③）
1. `AutoProfilesViewModel.cs` に `IAutoProfileService` を注入し、自動切替連携を整備。

### タスク Step13-5: `SpecialActionsListViewModel` の DI 接続（コア④）
1. `SpecialActionsListViewModel.cs` に `ISpecialActionRepository` を注入し、実データ操作に一本化。

### タスク Step13-6: サブ ViewModels の静的参照撲滅
1. `RecordBoxViewModel`、`ProfileSettingsViewModel` 等の残存参照を精査・置換。

### タスク Step13-7: `MainWindow.xaml.cs` の静的参照排除
1. `MainWindow.xaml.cs` を精査し、`Program.rootHub` や `Global` への直アクセスを DI サービス経由へピンポイント置換する（§1.4）。

### タスク Step13-8: `App.xaml.cs` の `rootHub` シム完全削除
1. 全画面からの参照消滅を確認後、`App.xaml.cs` の `rootHub` プロパティを削除する。

### タスク Step13-9: ViewModel 単体テストの拡充と自動テスト実行
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