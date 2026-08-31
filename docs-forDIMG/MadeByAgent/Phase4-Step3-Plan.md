# フェーズ4-Step3 計画書: ISpecialActionRepository 分離

作成日: 2026-08-31
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §4.1, §5, §6.6（全体計画）
- `docs-forDIMG/MadeByAgent/Phase4-Plan.md` §1.2, §2, §3 Step3（Phase4詳細計画）
- `docs-forDIMG/MadeByAgent/Phase4-Status.md`（Phase4進捗管理）
- `docs-forDIMG/MadeByAgent/Phase4-Step2-Completion-Report.md`（Step2完了報告）
- `docs-forDIMG/MadeByAgent/Phase4-Step0-Global-Member-Inventory.md`（Global棚卸し一覧）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `Global` の SpecialAction 関連メンバー（`actions`, `LoadActions`, `SaveActions`, `GetAction` 等）は削除せず、新設する `ISpecialActionRepository` への薄いデリゲートシムとして残す。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - `Actions.xml` の XML スキーマ、SpecialAction の各種プロパティ（Macro, Key, Program, Profile, BatteryCheck 等の全アクション種別）、パス解決（`appdatapath\Actions.xml`）の挙動を 100% 維持する。
- **§2.3 ログ出力の厳格な維持**:
  - `AppLogger.LogToGui` 等の既存ログ出力を厳格に維持する。
- **§3.1 DI (Dependency Injection) の実装**:
  - インターフェース契約は `DS4Windows/DI/ISpecialActionRepository.cs`（名前空間 `DS4Windows.DI`）に配置（**DI永続資産**）。
  - 実装クラスは `DS4Windows/DS4Control/Services/SpecialActionRepository.cs`（名前空間 `DS4Windows`）に配置（**DI永続資産**）。
  - コンテナ登録は `DS4Windows/DI/ServiceRegistration.cs` に行う。
- **§3.2 巨大ファイルの編集方針**:
  - `ScpUtil.cs` は全体を再生成せず、対象メソッド・プロパティのみをピンポイントで置換する。
- **資材のライフサイクル識別**:
  - DI永続資産（残るもの）と過渡期シム（Strangler Fig 移行用）を明確に区別して管理する。

---

## 0. Step3の位置づけと現状分析

### 0.1 Step0〜Step2の成果とStep3のスコープ
- **Step1・Step2 で完了したこと**:
  - `IProfileSettingsService`（メモリ上の設定管理）および `IProfileRepository`（プロファイル XML 永続化・切替）が DI 化され稼働中。
- **Step3 で行うこと**:
  - `Global`（`ScpUtil.cs`）に集中している SpecialAction の管理リスト（`Global.actions` 26件）および `Actions.xml` の永続化・CRUD ロジックを `ISpecialActionRepository` / `SpecialActionRepository` として分離・DI化する。

### 0.2 4層モデルにおける責務境界（全体計画書 §3）
- **第4層（リポジトリ層）: `ISpecialActionRepository`**:
  - SpecialAction のデータ永続化（`Actions.xml` 読み書き）、CRUD 操作（一覧取得・追加・削除・検索・名前重複チェック）、アクション一覧変更イベントの通知。
- **第3層（信号・アクション実行層）: `ControlService` / Action Exec Engine**:
  - SpecialAction の実行（マクロ再生、プロファイル切替、通知音声再生等）。※実行ロジックはリポジトリの責務外として分離を維持。

---

## 1. 設計方針とアーキテクチャ

### 1.1 `ISpecialActionRepository` インターフェース設計
契約インターフェースは `DS4Windows/DI/ISpecialActionRepository.cs`（名前空間 `DS4Windows.DI`）に定義する。

```csharp
namespace DS4Windows.DI
{
    public interface ISpecialActionRepository
    {
        string ActionsPath { get; }
        IReadOnlyList<SpecialAction> Actions { get; }
        List<SpecialAction> ActionList { get; }

        bool LoadActions();
        bool SaveActions();

        SpecialAction GetAction(string actionName);
        int GetActionIndex(string actionName);
        bool ActionExists(string actionName);

        bool AddAction(SpecialAction action);
        bool RemoveAction(string actionName);
        bool ReplaceAction(string oldActionName, SpecialAction newAction);

        event EventHandler ActionsChanged;
    }
}
```

### 1.2 `SpecialActionRepository` 実装クラス設計
- `DS4Windows/DS4Control/Services/SpecialActionRepository.cs`（新規作成）。
- 内部に `List<SpecialAction>` を保持し、`Actions.xml` のシリアライズ/デシリアライズを安全に処理。
- スレッドセーフティ: リスト操作およびファイル I/O 時の競合を防ぐ `lock (_actionLock)` 排他制御。
- 変更通知: アクションの追加・削除・更新時に `ActionsChanged` イベントを発火。

### 1.3 `Global` (in `ScpUtil.cs`) シム設計
`Global.actions`, `Global.LoadActions()`, `Global.SaveActions()`, `Global.GetAction()` 等の静的メンバーは、`SpecialActionRepositoryInstance` を介する薄いシムとする。

```csharp
private static DS4Windows.DI.ISpecialActionRepository specialActionRepository = null;
private static readonly DS4Windows.DI.ISpecialActionRepository fallbackSpecialActionRepository = new SpecialActionRepository();

public static DS4Windows.DI.ISpecialActionRepository SpecialActionRepositoryInstance
{
    get
    {
        if (specialActionRepository != null) return specialActionRepository;
        try
        {
            var service = AppHost.GetService<DS4Windows.DI.ISpecialActionRepository>();
            if (service != null)
            {
                specialActionRepository = service;
                return specialActionRepository;
            }
        }
        catch { }
        return fallbackSpecialActionRepository;
    }
    set => specialActionRepository = value;
}

public static List<SpecialAction> actions
{
    get => SpecialActionRepositoryInstance.ActionList;
    set
    {
        if (value != null)
        {
            SpecialActionRepositoryInstance.ActionList.Clear();
            SpecialActionRepositoryInstance.ActionList.AddRange(value);
        }
    }
}
```

---

## 2. 成果物一覧

| ファイルパス | 種別 | ライフサイクル | 内容 |
|---|---|---|---|
| `DS4Windows/DI/ISpecialActionRepository.cs` | 新規 | **DI永続資産** | SpecialAction 管理・永続化の契約インターフェース |
| `DS4Windows/DS4Control/Services/SpecialActionRepository.cs` | 新規 | **DI永続資産** | `ISpecialActionRepository` の本番実装クラス |
| `DS4Windows/DI/ServiceRegistration.cs` | 更新 | **DI永続資産** | `ISpecialActionRepository` の Singleton 登録 |
| `DS4Windows/DS4Control/ScpUtil.cs` | 更新 | **過渡期シム** | `Global` の SpecialAction 関連メンバーを新サービスへのシムへピンポイント置換 |
| `DS4WindowsTests/SpecialActionRepositoryTests.cs` | 新規 | **テスト資産** | SpecialAction の CRUD、XML 永続化、シム同期の単体テスト |
| `docs-forDIMG/MadeByAgent/Phase4-Step3-Plan.md` | 新規 | ドキュメント | 本計画書 |
| `docs-forDIMG/MadeByAgent/Phase4-Step3-Completion-Report.md` | 新規 | ドキュメント | Step3完了報告書 |
| `docs-forDIMG/MadeByAgent/Phase4-Status.md` | 更新 | ドキュメント | Step3進捗ステータス更新 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step3-1: `ISpecialActionRepository` インターフェース定義（契約）
- `DS4Windows/DI/ISpecialActionRepository.cs` を新規作成（名前空間: `DS4Windows.DI`）。
- Actions 入出力、CRUD、イベント通知メソッドを定義。

### タスク Step3-2: `SpecialActionRepository` 実装クラス作成（実体）
- `DS4Windows/DS4Control/Services/SpecialActionRepository.cs` を新規作成（名前空間: `DS4Windows`）。
- `Actions.xml` の読込・保存・排他制御・CRUD 処理を実装。

### タスク Step3-3: DI コンテナ登録更新
- `DS4Windows/DI/ServiceRegistration.cs` に `ISpecialActionRepository` / `SpecialActionRepository` の Singleton 登録を追加。

### タスク Step3-4: `Global` (in `ScpUtil.cs`) ピンポイントシム化
- `ScpUtil.cs` の `Global.actions`, `Global.LoadActions`, `Global.SaveActions` 等を `SpecialActionRepositoryInstance` へのシム委譲へ置換。

### タスク Step3-5: 単体テスト作成と自動テスト実行
- `DS4WindowsTests/SpecialActionRepositoryTests.cs` を新規作成し、CRUD および XML 入出力動作を検証。
- 全テスト（`Actions.Tests` 31件, `StandaloneTests` 13件, 新設テスト）の通過を確認。

### タスク Step3-6: ビルド検証、進捗更新、完了報告書の作成
- `dotnet build DS4WindowsWPF.sln --nologo` を実行し警告0・エラー0を確認。
- `Phase4-Status.md` を更新し、`Phase4-Step3-Completion-Report.md` を作成。

---

## 4. リスクと回避策

| リスク | 該当タスク | 回避策 |
|---|---|---|
| `Actions.xml` の既存データ破損 | Step3-2 | 既存の XML スキーマ、要素名、属性マッピングを100%維持する。 |
| 呼び出し元の `Global.actions` リスト操作の非同期競合 | Step3-2, Step3-4 | `ActionList` プロパティの内部コレクション操作に `lock` 機構を内包する。 |
| 巨大ファイル `ScpUtil.cs` の編集によるコード欠損 | Step3-4 | ピンポイント置換を徹底し、対象メンバーブロックのみを書き換える。 |

---

## 5. 完了判定基準

- [ ] `ISpecialActionRepository` が `DS4Windows/DI/` に定義されている（DI永続資産）。
- [ ] `SpecialActionRepository` が `DS4Windows/DS4Control/Services/` に実装されている（DI永続資産）。
- [ ] `ServiceRegistration.cs` に登録され、`AppHost` から解決できる。
- [ ] `Global` の SpecialAction 関連メソッド・プロパティがシム化され、既存コードが無変更で動作する。
- [ ] 新設した `SpecialActionRepositoryTests` および既存の回帰テストが全件成功する。
- [ ] ソリューションビルドが警告0・エラー0で成功する。
- [ ] `Phase4-Status.md` が更新され、`Phase4-Step3-Completion-Report.md` が作成されている。
