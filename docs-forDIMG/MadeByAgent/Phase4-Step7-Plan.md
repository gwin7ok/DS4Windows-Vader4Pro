# フェーズ4-Step7 計画書: ViewModel DI 移行 (Pattern A: 引数なし ViewModel)

作成日: 2026-08-31
最終更新日: 2026-08-31
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §3.3, §4.1, §5, §6.6（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase4-Plan.md` §1.1.1, §2, §3 Step7（Phase4詳細計画書）
- `docs-forDIMG/MadeByAgent/Phase4-Status.md`（Phase4進捗管理）
- `docs-forDIMG/MadeByAgent/Phase4-Step6-Completion-Report.md`（Step6完了報告）
- `docs-forDIMG/MadeByAgent/Phase4-Step0-ViewModel-Inventory.md`（ViewModel直接生成29件棚卸し）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - 各 ViewModel は DI コンテナからの解決を標準としつつ、直接コンストラクタ呼び出し互換用フォールバック（引数なしコンストラクタで `AppHost.GetService<T>()` を解決）を維持し、呼び出し元が壊れないようにする。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - WPF のデータバインディング、コマンド処理、プロパティ変更通知（`INotifyPropertyChanged`）、画面遷移の挙動を 100% 維持する。
- **§2.3 ログ出力の厳格な維持**:
  - `AppLogger.LogToGui` 等の既存ログ出力を厳格に維持する。
- **§3.1 DI (Dependency Injection) の実装**:
  - ViewModel は `DS4Windows/DI/ServiceRegistration.cs` に登録する（**DI永続資産**）。
  - 第4層 4-c サービス（`IProfileSettingsService`, `IEnvironmentService`, `IPathService` 等）をコンストラクタ注入する。
- **§3.2 巨大ファイルの編集方針**:
  - 各 View（`SettingsUserControl.xaml.cs`, `LogUserControl.xaml.cs`, `AboutUserControl.xaml.cs` 等）の `DataContext` 設定箇所のみをピンポイントで置換する。
- **資材のライフサイクル識別**:
  - DI永続資産（残るもの）と過渡期シム（Strangler Fig 移行用）を明確に区別して管理する。

---

## 0. Step7の位置づけと現状分析

### 0.1 Step0〜Step6の成果とStep7のスコープ
- **Step1〜Step6 で完了したこと**:
  - 第1層〜第3層および第4層 4-c の全 13 バックエンドサービスが DI 化され、`AppHost.CreateHost()` を唯一の Composition Root として一本化された。
- **Step7 で行うこと**:
  - Step 0 の調査（`Phase4-Step0-ViewModel-Inventory.md`）で棚卸しした ViewModel 直接生成 29 件のうち、**Pattern A（引数なし ViewModel / 単純 ViewModel）** に属する以下の ViewModel 群を DI 化する：
    1. `SettingsViewModel`（設定画面）
    2. `LogViewModel`（ログ画面）
    3. `AboutViewModel`（アプリ情報画面）
  - ※なお、`RecordBoxViewModel` はコンストラクタで 4 つの実行時引数 `(int device, DS4ControlSettings controlSettings, bool recordMacro, bool extraHold)` を必要とするため、引数なし ViewModel（Pattern A）ではなく、**Step 9（Pattern C: 実行時引数付き ViewModel の Factory 移行）の対象** として正式に引継ぎを行う。
  - View（UserControl）内の直接 `new ViewModel()` を全廃し、DI コンテナ（`AppHost.GetService<T>()`）経由の注入に切り替える。

### 0.2 全体4層モデルにおける責務境界と本Stepの位置づけ（全体計画書 §3.3 準拠）
全体計画書（`DI-App-Wide-Migration-Plan.md` §3.3）および Phase4 計画書（`Phase4-Plan.md` §1.1.1）で規定された **全体4層モデル（実行時3層 ＋ UI層）** に基づき、本Step（Step 7）の位置づけを以下のように整理する：

1. **第1層: 入力監視層**
   - コントローラーの機種差を吸収し、`DS4State` に正規化して上位へ渡す。
2. **第2層: 信号変換層（拡張版）**
   - 入力から「何を出力すべきか」を決定する（2-a 基本マッピング, 2-b SpecialAction判定, 2-c アクション選択, 2-d マクロ分解）。
3. **第3層: 信号出力層（拡張版）**
   - 決定された内容を実際に副作用として実行する（3-a 仮想コントローラー出力, 3-b KBM出力, 3-c アプリ内アクション実行）。
4. **第4層: UI層（制御面） 【★本Step対象】**
   - ユーザーが設定・プロファイル・状態を操作し、サービス経由で実行時3層へ設定を反映する。
   - **4-a. View 【★本Step対象】**: WPF の画面・UserControl（`SettingsUserControl`, `LogUserControl`, `AboutUserControl`）。
   - **4-b. ViewModel 【★本Step対象】**: Pattern A の各 ViewModel。第4層 4-c サービスをコンストラクタ注入して動作。
   - **4-c. 設定／状態サービス**: Step 1〜5 で構築済みの各 DI サービス（注入元）。

---

## 1. 設計方針とアーキテクチャ

### 1.1 Pattern A ViewModel のコンストラクタ注入設計
各 ViewModel に第4層 4-c サービスを受け取るコンストラクタを追加し、既存互換用フォールバックコンストラクタ（引数なし）も維持する。

```csharp
// SettingsViewModel 例
public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly IEnvironmentService _envService;
    private readonly IPathService _pathService;
    private readonly IProfileSettingsService _profileSettings;

    // DI用コンストラクタ
    public SettingsViewModel(
        IEnvironmentService envService,
        IPathService pathService,
        IProfileSettingsService profileSettings)
    {
        _envService = envService;
        _pathService = pathService;
        _profileSettings = profileSettings;
    }

    // 既存互換用フォールバックコンストラクタ
    public SettingsViewModel() : this(
        DS4WinWPF.AppHost.GetService<IEnvironmentService>(),
        DS4WinWPF.AppHost.GetService<IPathService>(),
        DS4WinWPF.AppHost.GetService<IProfileSettingsService>())
    {
    }
}
```

### 1.2 View における DataContext バインディングの DI 化
各 UserControl の Code-Behind または XAML において、直接 `new` せず `AppHost.GetService<T>()` をバインドする。

```csharp
// SettingsUserControl.xaml.cs 例
public partial class SettingsUserControl : UserControl
{
    public SettingsUserControl()
    {
        InitializeComponent();
        DataContext = DS4WinWPF.AppHost.GetService<SettingsViewModel>() ?? new SettingsViewModel();
    }
}
```

### 1.3 `ServiceRegistration.cs` への登録
`ServiceRegistration.RegisterServices` に Pattern A ViewModel を追加する：

```csharp
// 第4層 4-b ViewModel (Pattern A)
services.AddTransient<SettingsViewModel>();
services.AddTransient<LogViewModel>();
services.AddTransient<AboutViewModel>();
```

---

## 2. 成果物一覧

| ファイルパス | 種別 | ライフサイクル | 内容 |
|---|---|---|---|
| `DS4Windows/DS4Forms/ViewModels/AboutViewModel.cs` | 新規 | **DI永続資産** | 全画面の MVVM 統一のため新設。バージョン文字列・リンクプロパティを公開 |
| `DS4Windows/DS4Forms/ViewModels/LogViewModel.cs` | 更新 | **DI永続資産** | 引数なし既定コンストラクタを追加 |
| `DS4Windows/DS4Forms/SettingsUserControl.xaml.cs` | 更新 | **DI永続資産** | 直接 new を全廃し DI 解決へ移行 |
| `DS4Windows/DS4Forms/LogUserControl.xaml.cs` | 更新 | **DI永続資産** | 直接 new を全廃し DI 解決へ移行 |
| `DS4Windows/DS4Forms/AboutUserControl.xaml.cs` | 更新 | **DI永続資産** | DataContext に AboutViewModel をバインド |
| `DS4Windows/DI/ServiceRegistration.cs` | 更新 | **DI永続資産** | Pattern A ViewModel の Transient 登録 |
| `DS4WindowsTests/PatternAViewModelTests.cs` | 新規 | **テスト資産** | Pattern A ViewModel の DI 解決・サービス注入検証単体テスト |
| `docs-forDIMG/MadeByAgent/Phase4-Step7-Plan.md` | 更新 | ドキュメント | 本計画書（RecordBoxViewModel の Step 9 引継ぎを反映） |
| `docs-forDIMG/MadeByAgent/Phase4-Step7-Completion-Report.md` | 新規 | ドキュメント | Step7完了報告書 |
| `docs-forDIMG/MadeByAgent/Phase4-Status.md` | 更新 | ドキュメント | Step7進捗ステータス更新（Step9対象明記） |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step7-1: Pattern A ViewModel のコンストラクタ DI 化
- `SettingsViewModel`, `LogViewModel`, `AboutViewModel` に DI コンストラクタと安全なフォールバックコンストラクタを整備。

### タスク Step7-2: DI コンテナ登録追加
- `DS4Windows/DI/ServiceRegistration.cs` に Pattern A ViewModel の `AddTransient` 登録を追加。

### タスク Step7-3: View（UserControl）の DataContext DI 化
- `SettingsUserControl`, `LogUserControl`, `AboutUserControl` における直接 `new ViewModel()` を全廃し、`AppHost.GetService<T>()` 経由へピンポイント置換。

### タスク Step7-4: 単体テスト作成と自動テスト実行
- `DS4WindowsTests/PatternAViewModelTests.cs` を作成し、各 ViewModel が DI コンテナから正常解決され、サービスが注入されることを検証。
- 回帰テスト（`Actions.Tests` 75件, `StandaloneTests` 13件, 全新設テスト）の通過を確認。

### タスク Step7-5: ビルド検証、進捗更新、完了報告書の作成
- `dotnet build DS4WindowsWPF.sln --nologo` を実行し警告0・エラー0を確認。
- `Phase4-Status.md` を更新し、`Phase4-Step7-Completion-Report.md` を作成。

---

## 4. リスクと回避策

| リスク | 該当タスク | 回避策 |
|---|---|---|
| XAML デザイナー表示時の NullReference | Step7-1, Step7-3 | ViewModel 側にフォールバックコンストラクタを残し、`DesignerProperties.GetIsInDesignMode` 時でも安全に動作させる。 |
| データバインディング切断 | Step7-1 | 既存プロパティ名および `INotifyPropertyChanged` の実装を一切変更せず 100% 維持する。 |
| 巨大 XAML / Code-Behind のコード欠損 | Step7-3 | ピンポイント置換を徹底し、コンストラクタ初期化部分のみを書き換える。 |

---

## 5. 完了判定基準

- [ ] Pattern A ViewModel（Settings, Log, About）が DI コンストラクタを備えている（DI永続資産）。
- [ ] `ServiceRegistration.cs` に Pattern A ViewModel が登録されている（DI永続資産）。
- [ ] 各 View における直接 `new ViewModel()` が全廃され、DI 解決に移行している。
- [ ] `RecordBoxViewModel` が Step 9（Pattern C）の対象として計画書・進捗表に正式に記録されている。
- [ ] 新設した `PatternAViewModelTests` および既存の全回帰テストが成功する。
- [ ] ソリューションビルドが警告0・エラー0で成功する。
- [ ] `Phase4-Status.md` が更新され、`Phase4-Step7-Completion-Report.md` が作成されている。

