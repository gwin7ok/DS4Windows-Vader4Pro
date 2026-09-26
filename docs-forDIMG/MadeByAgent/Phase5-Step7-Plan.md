# フェーズ5-Step7 計画書: SpecialAction 永続化の責務分離

作成日: 2026-09-02（改訂日: 2026-09-03・Step7へ再編）
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md` §2, §3 Step7（Phase5詳細計画書）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理）
- `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`（Step1監査結果。本Stepの対象根拠）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `Global.LoadActions`／`Global.SaveActions` 等の古い経路は、新しいDI経由の実装が完成し動作確認が取れるまで削除しない。新旧を同時に複数経路実装することはしない。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - アクションのCRUD（追加・削除・更新）、UID生成、XML属性パース・シリアライズ、`Global.actions` リストとの同期を100%維持する。
- **§2.3 ログ出力の厳格な維持**:
  - `AppLogger.LogToGui`／`AppLogger.LogTrace`／`AppLogger.LogDebug` 等、既存のログ出力とログレベルを維持する。
- **§3.1 DI (Dependency Injection) の実装**:
  - コンテナ登録は `DS4Windows/DI/ServiceRegistration.cs` に行う。既存の `ISpecialActionRepository` のインターフェース名・登録ライフタイムは維持する。
- **§3.2 巨大ファイルの編集方針**:
  - `ScpUtil.cs`（`Global.LoadActions`／`Global.SaveActions`／`Global.SaveAction`／`Global.RemoveAction`）はピンポイント置換のみ行う。

---

## 0. Step7の位置づけと現状分析

### 0.1 Step1監査結果に基づく対象範囲
`Phase5-Step1-legacy-delegation-audit-report.md` §2 表の#12（`ISpecialActionRepository` → `SpecialActionRepository`）に基づき、以下を対象とする。

- `SpecialActionRepository.LoadActions()` 内部の `Global.LoadActions()` 呼び出し
- `SpecialActionRepository.SaveActions()` 内部の `Global.SaveActions()` 呼び出し

### 0.2 【重要発見】`SpecialActionRepository` と `BackingStore.actions` が二重管理・非同期の状態にある
実装コード（`SpecialActionRepository.cs`）を精査したところ、**極めて重大なアーキテクチャ上の問題**が確認された。

1. **二重管理の実態**:
   `SpecialActionRepository` は独自に `private readonly List<SpecialAction> _actions = new List<SpecialAction>();` を保持している。
   しかし、アプリ全体で実際に使用されているのは `Global.store.actions`（`BackingStore.actions`）である。この2つのリストは**完全に別インスタンスであり、同期されていない**。

2. **サイレント不全のメカニズム**:
   - `LoadActions()`: `Global.LoadActions()` を呼び出して `BackingStore.actions` を更新するが、`_actions` にはロード結果が反映されない。
   - `AddAction(SpecialAction action)`: `_actions.Add(action)` するだけで、`Global.SaveAction()`（`Actions.xml` への保存および `BackingStore.actions` への反映）を呼ばない。
   - `RemoveAction(string name)`: `_actions` から削除するだけで、`Global.RemoveAction()` を呼ばない。
   - `SaveActions()`: `Global.SaveActions()` を呼ぶが、これは `BackingStore.actions` を保存するだけであり、`_actions` の内容は保存されない。

3. **結論**:
   現在、`ISpecialActionRepository` の CRUD メソッド（`AddAction`／`RemoveAction`／`ReplaceAction`）を呼んでも、**アプリの実際の動作（`ActionManager` や `Actions.xml`）には一切反映されない状態**にある。

### 0.3 本Step着手前に必須の追加調査
`SpecialActionEditor.xaml.cs` や `SpecialActionsListViewModel.cs` が、現在 `SpecialActionRepository` を呼んでいるのか、それとも `Global.SaveAction`／`Global.RemoveAction`／`Global.store.actions` を直接呼んでいるのかをタスク Step7-1 で確認する。

### 0.4 全体4層モデルにおける位置づけ
`SpecialActionRepository` は **第4層 4-c 設定・プロファイル・アクション・環境・通知サービス** に属する。

---

## 1. 設計方針とアーキテクチャ

### 1.1 独自リスト `_actions` の廃止と `BackingStore.actions` への一本化
`SpecialActionRepository` が保持しているプライベートな `List<SpecialAction> _actions` を**完全廃止**する。
唯一の情報源（Single Source of Truth）として `BackingStore.actions` を参照する設計とする。

```csharp
public class SpecialActionRepository : ISpecialActionRepository
{
    private readonly BackingStore _config;

    public SpecialActionRepository(BackingStore config = null)
    {
        _config = config ?? Global.store;
    }

    public IReadOnlyList<SpecialAction> Actions => _config.actions.AsReadOnly();
}
```

### 1.2 CRUDメソッドの実データ操作への切替と排他制御
- **`AddAction` / `ReplaceAction`**:
  `Global.SaveAction`（`ScpUtil.cs`）を適切に呼び出し、`BackingStore.actions` への追加および `Actions.xml` への永続化を同時に行う。
- **全引数の安全なマッピング**:
  `SpecialAction` が保持する `delayBefore`、`extra`（引数・復帰条件）、`uID` などの固有パラメータが欠落しないよう、タスク Step7-2 でシグネチャを精査して呼び出す。
- **排他制御の競合防止**:
  `Global.SaveAction` 内部で行われている排他制御と競合して AB-BA デッドロックを起こさないよう、ロックの粒度を慎重に設計する。

### 1.3 `LoadActions`／`SaveActions` の整合性維持
- `LoadActions()`: `_config.LoadActions()`（または `Global.LoadActions()`）を呼び出す。
- `SaveActions()`: `_config.SaveActions()`（または `Global.SaveActions()`）を呼び出す。

### 1.4 `ActionManager` の runtime 再構築との境界
`Global.SaveAction` 実行後に行われる `ActionManager` のランタイム再構築通知は、既存の `Global.SaveAction` 内の処理をそのまま活かし、本Stepでは `ActionManager` 本体（Step9）に深入りしない。

---

## 2. 成果物一覧

| 種別 | ファイルパス | 変更内容 |
|---|---|---|
| リポジトリ改修 | `DS4Windows/DS4Control/Services/SpecialActionRepository.cs` | 独自 `_actions` 撤廃、`BackingStore.actions` 一本化、CRUD実データ化 |
| シム調整 | `DS4Windows/DS4Control/ScpUtil.cs` | `Global.LoadActions`／`SaveActions` 呼び出し経路のピンポイント整理 |
| 単体テスト拡充 | `DS4WindowsTests/SpecialActionRepositoryTests.cs` | 実データ操作および CRUD 成否の検証テスト拡充 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step7-1: `AddAction`/`RemoveAction`/`ReplaceAction` の既存呼び出し元調査【最優先・前提】
1. ソリューション全体を grep し、`ISpecialActionRepository` の呼び出し元を特定する。
2. UI（`SpecialActionEditor.xaml.cs` 等）が現在どこを呼んでいるかを特定する。

### タスク Step7-2: `Global.SaveAction` シグネチャ・排他ロックの精査
1. `ScpUtil.cs` の `Global.SaveAction` および `RemoveAction` の実装コードを読み込み、渡すべき全引数と内部ロックを確認する。

### タスク Step7-3: `SpecialActionRepository` の実データ一本化実装
1. `SpecialActionRepository.cs` から独自 `_actions` を削除。
2. コンストラクタで `BackingStore` を受け取るように変更。
3. `Actions` プロパティ、CRUD メソッドを `BackingStore.actions` / `Global.SaveAction` に接続。

### タスク Step7-4: UI呼び出し元の統一検討
1. Step7-1 の結果に基づき、UI が直接 `Global.SaveAction` を呼んでいる箇所を `ISpecialActionRepository` 経由に切り替えるか、Step13（UI統合）に委譲するかを判断・記録する。

### タスク Step7-5: 単体テスト作成と自動テスト実行
1. `SpecialActionRepositoryTests.cs` で CRUD が正しく反映されることをモック／インメモリテストで検証。
2. `dotnet test` でリグレッションがないことを確認。

### タスク Step7-6: ビルド検証、進捗更新、完了報告書の作成
1. Debug / Release ビルド成功を確認。
2. `Phase5-Status.md` を更新。
3. `Phase5-Step7-Completion-Report.md` を作成。

---

## 4. リスクと回避策

| リスク | 影響度 | 回避策 |
|---|---|---|
| **二重管理によるデータ不整合** | 高 | 独自リストを完全撤廃し、`BackingStore.actions` を唯一の情報源とする（§1.1）。 |
| **パラメータ欠落** | 中 | `Global.SaveAction` の全引数を精査し、遅延時間や引数が失われないようにする（§1.2）。 |
| **デッドロック** | 中 | `Global.SaveAction` 内部のロック構造を確認し、外側での不要な多重ロックを避ける。 |

---

## 5. 完了判定基準

- [ ] `SpecialActionRepository` の独自リスト `_actions` が削除され、`BackingStore.actions` に一本化されていること。
- [ ] `AddAction`／`RemoveAction`／`ReplaceAction` が実データおよび永続化ファイルに正常に反映されること。
- [ ] 単体テストが成功し、リグレッションがないこと。
- [ ] ビルドエラーおよび警告の増加がないこと。
