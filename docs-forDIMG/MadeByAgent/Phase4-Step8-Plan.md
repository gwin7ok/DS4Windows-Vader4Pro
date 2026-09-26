# フェーズ4-Step8 計画書: ViewModel DI 移行 (Pattern B: 共有依存 ViewModel)

作成日: 2026-08-31
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §3.3, §4.1, §5, §6.6（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase4-Plan.md` §1.1.1, §2, §3 Step8（Phase4詳細計画書）
- `docs-forDIMG/MadeByAgent/Phase4-Status.md`（Phase4進捗管理）
- `docs-forDIMG/MadeByAgent/Phase4-Step7-Completion-Report.md`（Step7完了報告）
- `docs-forDIMG/MadeByAgent/Phase4-Step0-ViewModel-Inventory.md`（ViewModel直接生成29件棚卸し）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - 各 ViewModel は DI コンテナからの解決を標準としつつ、直接コンストラクタ呼び出し互換用フォールバック（引数なしコンストラクタで `DS4WinWPF.AppHost.GetService<T>()` を解決）を維持し、呼び出し元が壊れないようにする。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - WPF のデータバインディング、コントローラースロット更新（0〜7スロット）、プロファイルドロップダウン連動、バッテリー表示、ウィンドウステータス更新の挙動を 100% 維持する。
- **§2.3 ログ出力の厳格な維持**:
  - `AppLogger.LogToGui` 等の既存ログ出力を厳格に維持する。
- **§3.1 DI (Dependency Injection) の実装**:
  - ViewModel は `DS4Windows/DI/ServiceRegistration.cs` に登録する（**DI永続資産**）。
  - 第4層 4-c サービス（`IDeviceStateService`, `IProfileSettingsService`, `IProfileRepository`, `IEnvironmentService` 等）をコンストラクタ注入する。
- **§3.2 巨大ファイルの編集方針**:
  - 各 View（`ControllersUserControl.xaml.cs`, `MainWindow.xaml.cs` 等）の `DataContext` 設定箇所のみをピンポイントで置換する。
- **資材のライフサイクル識別**:
  - DI永続資産（残るもの）と過渡期シム（Strangler Fig 移行用）を明確に区別して管理する。

---

## 0. Step8の位置づけと現状分析

### 0.1 Step0〜Step7の成果とStep8のスコープ
- **Step1〜Step7 で完了したこと**:
  - 第1層〜第3層および第4層 4-c の全 13 バックエンドサービスが DI 化され、Composition Root が一本化された。
  - Step 7 で Pattern A（引数なし ViewModel: `SettingsViewModel`, `LogViewModel`, `AboutViewModel`）の DI 移行が完了。
- **Step8 で行うこと**:
  - Step 0 の調査（`Phase4-Step0-ViewModel-Inventory.md`）で棚卸しした ViewModel のうち、**Pattern B（共有依存 ViewModel / アプリケーション状態共有 ViewModel）** に属する以下の ViewModel 群を DI 化する：
    1. `ControllersViewModel`（コントローラー一覧画面: スロット状態、バッテリー、プロファイル選択）
    2. `MainWindowsViewModel`（メインウィンドウ全体制御: サービス稼働状態、トレイ制御）
  - View（`ControllersUserControl`, `MainWindow`）内の直接 `new ViewModel()` を全廃し、DI コンテナ（`AppHost.GetService<T>()`）経由の注入に切り替える。

### 0.2 全体4層モデルにおける責務境界と本Stepの位置づけ（全体計画書 §3.3 準拠）
全体計画書（`DI-App-Wide-Migration-Plan.md` §3.3）および Phase4 計画書（`Phase4-Plan.md` §1.1.1）で規定された **全体4層モデル（実行時3層 ＋ UI層）** に基づき、本Step（Step 8）の位置づけを以下のように整理する：

1. **第1層: 入力監視層**
   - コントローラーの機種差を吸収し、`DS4State` に正規化して上位へ渡す（`IDeviceStateService` 経由で第4層へ提供）。
2. **第2層: 信号変換層（拡張版）**
   - 入力から「何を出力すべきか」を決定する（2-a 基本マッピング, 2-b SpecialAction判定, 2-c アクション選択, 2-d マクロ分解）。
3. **第3層: 信号出力層（拡張版）**
   - 決定された内容を実際に副作用として実行する（3-a 仮想コントローラー出力 `IOutputSlotService`, 3-b KBM出力 `IVirtualKBM`, 3-c アプリ内アクション実行）。
4. **第4層: UI層（制御面） 【★本Step対象】**
   - ユーザーが設定・プロファイル・状態を操作し、サービス経由で実行時3層へ設定を反映する。
   - **4-a. View 【★本Step対象】**: WPF の画面・UserControl（`ControllersUserControl`, `MainWindow`）。
   - **4-b. ViewModel 【★本Step対象】**: Pattern B の各 ViewModel。第4層 4-c サービスをコンストラクタ注入して共有状態を管理。
   - **4-c. 設定／状態サービス**: Step 1〜5 で構築済みの各 DI サービス（注入元）。

---

## 1. 設計方針とアーキテクチャ

### 1.1 Pattern B ViewModel のコンストラクタ注入設計
各 ViewModel に第4層 4-c サービスを受け取るコンストラクタを整備し、既存互換用フォールバックコンストラクタ（引数なし）も維持する。

```csharp
// ControllersViewModel 例
public class ControllersViewModel : INotifyPropertyChanged
{
    private readonly IDeviceStateService _deviceStateService;
    private readonly IProfileSettingsService _profileSettingsService;
    private readonly IProfileRepository _profileRepository;

    // DI用コンストラクタ
    public ControllersViewModel(
        IDeviceStateService deviceStateService,
        IProfileSettingsService profileSettingsService,
        IProfileRepository profileRepository)
    {
        _deviceStateService = deviceStateService;
        _profileSettingsService = profileSettingsService;
        _profileRepository = profileRepository;
    }

    // 既存互換用フォールバックコンストラクタ
    public ControllersViewModel() : this(
        DS4WinWPF.AppHost.GetService<IDeviceStateService>(),
        DS4WinWPF.AppHost.GetService<IProfileSettingsService>(),
        DS4WinWPF.AppHost.GetService<IProfileRepository>())
    {
    }
}
```

### 1.2 View における DataContext バインディングの DI 化
各 UserControl / Window の Code-Behind において、直接 `new` せず `DS4WinWPF.AppHost.GetService<T>()` をバインドする。

```csharp
// ControllersUserControl.xaml.cs 例
public partial class ControllersUserControl : UserControl
{
    public ControllersUserControl()
    {
        InitializeComponent();
        DataContext = DS4WinWPF.AppHost.GetService<ControllersViewModel>() ?? new ControllersViewModel();
    }
}
```

### 1.3 `ServiceRegistration.cs` への登録
`ServiceRegistration.RegisterServices` に Pattern B ViewModel を追加する（共有状態維持のため Singleton または必要に応じた Transient）：

```csharp
// 第4層 4-b ViewModel (Pattern B: 共有依存 ViewModel)
services.AddSingleton<ControllersViewModel>();
services.AddSingleton<MainWindowsViewModel>();
```

---

## 2. 成果物一覧

| ファイルパス | 種別 | ライフサイクル | 内容 |
|---|---|---|---|
| `DS4Windows/DS4Forms/ViewModels/ControllersViewModel.cs` | 更新 | **DI永続資産** | 第4層 4-c サービス（デバイス状態・プロファイル設定）のコンストラクタ注入対応 |
| `DS4Windows/DS4Forms/ViewModels/MainWindowsViewModel.cs` | 更新 | **DI永続資産** | 第4層 4-c サービス（環境・通知）のコンストラクタ注入対応 |
| `DS4Windows/DS4Forms/ControllersUserControl.xaml.cs` | 更新 | **DI永続資産** | 直接 new を全廃し DI 解決へ移行 |
| `DS4Windows/DS4Forms/MainWindow.xaml.cs` | 更新 | **DI永続資産** | 直接 new を全廃し DI 解決へ移行 |
| `DS4Windows/DI/ServiceRegistration.cs` | 更新 | **DI永続資産** | Pattern B ViewModel の Singleton 登録 |
| `DS4WindowsTests/PatternBViewModelTests.cs` | 新規 | **テスト資産** | Pattern B ViewModel の DI 解決・サービス注入検証単体テスト |
| `docs-forDIMG/MadeByAgent/Phase4-Step8-Plan.md` | 新規 | ドキュメント | 本計画書 |
| `docs-forDIMG/MadeByAgent/Phase4-Step8-Completion-Report.md` | 新規 | ドキュメント | Step8完了報告書 |
| `docs-forDIMG/MadeByAgent/Phase4-Status.md` | 更新 | ドキュメント | Step8進捗ステータス更新 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step8-1: Pattern B ViewModel のコンストラクタ DI 化
- `ControllersViewModel`, `MainWindowsViewModel` に DI コンストラクタと安全なフォールバックコンストラクタを整備。

### タスク Step8-2: DI コンテナ登録追加
- `DS4Windows/DI/ServiceRegistration.cs` に Pattern B ViewModel の `AddSingleton` 登録を追加。

### タスク Step8-3: View（UserControl/Window）の DataContext DI 化
- `ControllersUserControl`, `MainWindow` における直接 `new ViewModel()` を全廃し、`DS4WinWPF.AppHost.GetService<T>()` 経由へピンポイント置換。

### タスク Step8-4: 単体テスト作成と自動テスト実行
- `DS4WindowsTests/PatternBViewModelTests.cs` を作成し、各 ViewModel が DI コンテナから正常解決され、サービスが注入されることを検証。
- 回帰テスト（`Actions.Tests` 75件, `StandaloneTests` 13件, 全新設テスト）の通過を確認。

### タスク Step8-5: ビルド検証、進捗更新、完了報告書の作成
- `dotnet build DS4WindowsWPF.sln --nologo` を実行し警告0・エラー0を確認。
- `Phase4-Status.md` を更新し、`Phase4-Step8-Completion-Report.md` を作成。

---

## 4. リスクと回避策

| リスク | 該当タスク | 回避策 |
|---|---|---|
| コントローラースロット状態の更新連動漏れ | Step8-1 | `ControllersViewModel` 内の既存プロパティ変更通知およびイベント購読を 100% 維持する。 |
| XAML デザイナー表示時の NullReference | Step8-1, Step8-3 | ViewModel 側にフォールバックコンストラクタを残し、`DesignerProperties.GetIsInDesignMode` 時でも安全に動作させる。 |
| 巨大 MainWindow.xaml.cs のコード欠損 | Step8-3 | ピンポイント置換を徹底し、初期化部分のみを書き換える。 |

---

## 5. 完了判定基準

- [ ] Pattern B ViewModel（Controllers, MainWindows）が DI コンストラクタを備えている（DI永続資産）。
- [ ] `ServiceRegistration.cs` に Pattern B ViewModel が登録されている（DI永続資産）。
- [ ] 各 View における直接 `new ViewModel()` が全廃され、DI 解決に移行している。
- [ ] 新設した `PatternBViewModelTests` および既存の全回帰テストが成功する。
- [ ] ソリューションビルドが警告0・エラー0で成功する。
- [ ] `Phase4-Status.md` が更新され、`Phase4-Step8-Completion-Report.md` が作成されている。

