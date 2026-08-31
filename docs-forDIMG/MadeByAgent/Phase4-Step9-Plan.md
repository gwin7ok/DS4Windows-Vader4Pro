# フェーズ4-Step9 計画書: ViewModel DI 移行 (Pattern C: 実行時引数付き ViewModel - Factory DI) & 実機検証 Checkpoint 3

作成日: 2026-08-31
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §3.3, §4.1, §5, §6.6（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase4-Plan.md` §1.1.1, §2, §3 Step9（Phase4詳細計画書）
- `docs-forDIMG/MadeByAgent/Phase4-Status.md`（Phase4進捗管理）
- `docs-forDIMG/MadeByAgent/Phase4-Step8-Completion-Report.md`（Step8完了報告）
- `docs-forDIMG/MadeByAgent/Phase4-Step0-ViewModel-Inventory.md`（ViewModel直接生成29件棚卸し）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - 各 ViewModel は Factory 経由の生成を標準としつつ、直接コンストラクタ呼び出し互換用フォールバックを維持し、呼び出し元が壊れないようにする。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - プロファイル編集、マクロ記録、SpecialAction 編集、キー割り当て、自動プロファイル設定の全機能およびデータバインディングの挙動を 100% 維持する。
- **§2.3 ログ出力の厳格な維持**:
  - `AppLogger.LogToGui` 等の既存ログ出力を厳格に維持する。
- **§3.1 DI (Dependency Injection) の実装**:
  - Factory 契約は `DS4Windows/DI/IViewModelFactory.cs`（名前空間 `DS4Windows.DI`）に配置（**DI永続資産**）。
  - 実装クラスは `DS4Windows/DS4Control/Services/ViewModelFactory.cs`（名前空間 `DS4Windows`）に配置（**DI永続資産**）。
  - コンテナ登録は `DS4Windows/DI/ServiceRegistration.cs` に行う。
- **§3.2 巨大ファイルの編集方針**:
  - 各 View（`ProfileEditor.xaml.cs`, `RecordBox.xaml.cs`, `SpecialActionEditor.xaml.cs` 等）のインスタンス化箇所のみをピンポイントで置換する。
- **資材のライフサイクル識別**:
  - DI永続資産（残るもの）と過渡期シム（Strangler Fig 移行用）を明確に区別して管理する。

---

## 0. Step9の位置づけと現状分析

### 0.1 Step0〜Step8の成果とStep9のスコープ
- **Step1〜Step8 で完了したこと**:
  - バックエンド全 13 サービスおよび Composition Root の一本化が完了。
  - Step 7 で Pattern A（引数なし ViewModel: `Settings`, `Log`, `About`）の DI 移行が完了。
  - Step 8 で Pattern B（共有依存 ViewModel: `ControllersViewModel`, `MainWindowsViewModel`）の DI 移行が完了。
- **Step9 で行うこと**:
  - Step 0 で棚卸しした ViewModel 直接生成 29 件のうち、残る **Pattern C（実行時引数付き ViewModel）** に属する以下の ViewModel 群を Factory パターンにより DI 化する：
    1. `ProfileEditViewModel`（プロファイル編集画面: `slotIndex`, `profileName` 等）
    2. `RecordBoxViewModel`（マクロ記録画面: `device`, `controlSettings`, `recordMacro`, `extraHold`）
    3. `SpecialActionsViewModel`（Custom Action 編集画面: `deviceIndex`, `actionName` 等）
    4. `KBMEditorViewModel`（キー・マウス割り当て画面）
    5. `AutoProfileViewModel`（自動プロファイル画面）
  - 全 29 箇所の直接 `new ViewModel()` を全廃し、**全 ViewModel の DI 移行完了マイルストーンとして実機検証 Checkpoint 3 を実施** する。

### 0.2 全体4層モデルにおける責務境界と本Stepの位置づけ（全体計画書 §3.3 準拠）
全体計画書（`DI-App-Wide-Migration-Plan.md` §3.3）および Phase4 計画書（`Phase4-Plan.md` §1.1.1）で規定された **全体4層モデル（実行時3層 ＋ UI層）** に基づき、本Step（Step 9）の位置づけを以下のように整理する：

1. **第1層: 入力監視層**
   - コントローラーの機種差を吸収し、`DS4State` に正規化して上位へ渡す。
2. **第2層: 信号変換層（拡張版）**
   - 入力から「何を出力すべきか」を決定する（2-a 基本マッピング, 2-b SpecialAction判定, 2-c アクション選択, 2-d マクロ分解）。
3. **第3層: 信号出力層（拡張版）**
   - 決定された内容を実際に副作用として実行する（3-a 仮想コントローラー出力, 3-b KBM出力, 3-c アプリ内アクション実行）。
4. **第4層: UI層（制御面） 【★本Step対象（全ViewModel DI化の完成）】**
   - **4-a. View 【★本Step対象】**: WPF 画面・ダイアログ（`ProfileEditor`, `RecordBox`, `SpecialActionEditor`, `BindingWindow`, `AutoProfiles`）。
   - **4-b. ViewModel 【★本Step対象】**: Pattern C の各 ViewModel。Factory 経由で第4層 4-c サービスと実行時引数を結合注入。
   - **4-c. 設定／状態サービス & Factory 【★本Step対象】**: `IViewModelFactory` を Singleton 登録し、動的引数を伴う ViewModel の生成を一元管理。

---

## 1. 設計方針とアーキテクチャ

### 1.1 `IViewModelFactory` インターフェース設計 (第4層 4-c Factory)
契約インターフェースは `DS4Windows/DI/IViewModelFactory.cs`（名前空間 `DS4Windows.DI`）に定義する。

```csharp
namespace DS4Windows.DI
{
    public interface IViewModelFactory
    {
        ProfileEditViewModel CreateProfileEditViewModel(int deviceIndex, string profileName);
        RecordBoxViewModel CreateRecordBoxViewModel(int device, DS4ControlSettings controlSettings, bool recordMacro = true, bool extraHold = false);
        SpecialActionsViewModel CreateSpecialActionsViewModel(int deviceIndex, string actionName);
        KBMEditorViewModel CreateKBMEditorViewModel();
        AutoProfileViewModel CreateAutoProfileViewModel();
    }
}
```

### 1.2 `ViewModelFactory` 実装クラス設計
- `DS4Windows/DS4Control/Services/ViewModelFactory.cs`（新規作成、名前空間: `DS4Windows`）。
- コンストラクタで必要な第4層 4-c サービス（`IProfileSettingsService`, `IProfileRepository`, `ISpecialActionRepository`, `IPathService` 等）を注入。
- 実行時引数と注入サービスを組み合わせて ViewModel を生成・返却する。

### 1.3 View における Factory 解決と直接 new の全廃
各ダイアログ・ウィンドウオープン箇所において、直接 `new` せず `AppHost.GetService<IViewModelFactory>()` 経由で ViewModel を生成する。

```csharp
// RecordBox.xaml.cs 例
var factory = DS4WinWPF.AppHost.GetService<IViewModelFactory>();
recordBoxVM = factory != null
    ? factory.CreateRecordBoxViewModel(device, controlSettings, recordMacro, extraHold)
    : new RecordBoxViewModel(device, controlSettings, recordMacro, extraHold);
```

---

## 2. 成果物一覧

| ファイルパス | 種別 | ライフサイクル | 内容 |
|---|---|---|---|
| `DS4Windows/DI/IViewModelFactory.cs` | 新規 | **DI永続資産** | Pattern C ViewModel を動的生成する Factory 契約インターフェース |
| `DS4Windows/DS4Control/Services/ViewModelFactory.cs` | 新規 | **DI永続資産** | `IViewModelFactory` の本番実装クラス |
| `DS4Windows/DI/ServiceRegistration.cs` | 更新 | **DI永続資産** | `IViewModelFactory` の Singleton 登録 |
| `DS4Windows/DS4Forms/ProfileEditor.xaml.cs` | 更新 | **DI永続資産** | 直接 new を全廃し Factory 経由生成へ移行 |
| `DS4Windows/DS4Forms/RecordBox.xaml.cs` | 更新 | **DI永続資産** | 直接 new を全廃し Factory 経由生成へ移行 |
| `DS4Windows/DS4Forms/SpecialActionEditor.xaml.cs` | 更新 | **DI永続資産** | 直接 new を全廃し Factory 経由生成へ移行 |
| `DS4WindowsTests/PatternCViewModelTests.cs` | 新規 | **テスト資産** | Factory 経由での Pattern C ViewModel 生成・引数バインド単体テスト |
| `docs-forDIMG/MadeByAgent/Phase4-Step9-RealDevice-Verification-Checklist.md` | 新規 | ドキュメント | 実機動作確認チェックリスト CP3（全ViewModel DI化確認） |
| `docs-forDIMG/MadeByAgent/Phase4-Step9-Plan.md` | 新規 | ドキュメント | 本計画書 |
| `docs-forDIMG/MadeByAgent/Phase4-Step9-Completion-Report.md` | 新規 | ドキュメント | Step9完了報告書 |
| `docs-forDIMG/MadeByAgent/Phase4-Status.md` | 更新 | ドキュメント | Step9進捗ステータス更新 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step9-1: `IViewModelFactory` & `ViewModelFactory` の設計・作成
- `IViewModelFactory.cs`（`DS4Windows/DI/`）および `ViewModelFactory.cs`（`DS4Windows/DS4Control/Services/`）を作成。

### タスク Step9-2: DI コンテナ登録追加
- `DS4Windows/DI/ServiceRegistration.cs` に `IViewModelFactory` の Singleton 登録を追加。

### タスク Step9-3: 各 View（Dialog/Window）の Factory DI 化
- `ProfileEditor.xaml.cs`, `RecordBox.xaml.cs`, `SpecialActionEditor.xaml.cs` 等における直接 new を全廃し Factory 解決へ置換。

### タスク Step9-4: 単体テスト作成と自動テスト実行
- `DS4WindowsTests/PatternCViewModelTests.cs` を作成し、Factory 経由で各 ViewModel が正しく生成されることを検証。
- 回帰テスト（`Actions.Tests` 77件, `StandaloneTests` 13件, 全新設テスト）の通過を確認。

### タスク Step9-5: 実機動作確認 Checkpoint 3 の実施
- `Phase4-Step9-RealDevice-Verification-Checklist.md` を作成し、全画面 UI 結合の実機動作確認を実施。

### タスク Step9-6: ビルド検証、進捗更新、完了報告書の作成
- `dotnet build DS4WindowsWPF.sln --nologo` を実行し警告0・エラー0を確認。
- `Phase4-Status.md` を更新し、`Phase4-Step9-Completion-Report.md` を作成。

---

## 4. リスクと回避策

| リスク | 該当タスク | 回避策 |
|---|---|---|
| Factory 未初期化時のダイアログオープンクラッシュ | Step9-3 | 各 View 側で `AppHost.GetService<IViewModelFactory>()` が null の場合のフォールバック `new ViewModel(...)` を安全に保持する。 |
| 実行時パラメータの受け渡し漏れ | Step9-1, Step9-3 | 単体テスト（`PatternCViewModelTests`）で引数の完全なバインディングを自動検証する。 |

---

## 5. 完了判定基準

- [ ] `IViewModelFactory` が `DS4Windows/DI/` に定義されている（DI永続資産）。
- [ ] `ViewModelFactory` が `DS4Windows/DS4Control/Services/` に実装されている（DI永続資産）。
- [ ] `ServiceRegistration.cs` に `IViewModelFactory` が登録されている（DI永続資産）。
- [ ] 全 29 箇所の ViewModel 直接 new が全廃され、DI / Factory 解決に移行している。
- [ ] 新設した `PatternCViewModelTests` および既存の全回帰テストが成功する。
- [ ] 実機検証 Checkpoint 3（`Phase4-Step9-RealDevice-Verification-Checklist.md`）で全項目が合格する。
- [ ] ソリューションビルドが警告0・エラー0で成功する。
- [ ] `Phase4-Status.md` が更新され、`Phase4-Step9-Completion-Report.md` が作成されている。

