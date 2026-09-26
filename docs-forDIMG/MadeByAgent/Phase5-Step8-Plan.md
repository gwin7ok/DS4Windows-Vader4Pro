# フェーズ5-Step8 計画書: プロファイルアクション解決・連鎖処理の責務分離

作成日: 2026-09-02（改訂日: 2026-09-03・Step8へ再編）
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md` §2, §3 Step8（Phase5詳細計画書）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理）
- `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`（Step1監査結果）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - 既存の呼び出し元シグネチャを破壊しないよう、コンストラクタには既定値フォールバックを設ける。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - アクション連鎖の発火条件（`uTrigger.Count == 0`、`controls` の一致判定）を100%維持する。
- **§2.3 ログ出力の厳格な維持**:
  - 既存のログレベルおよびログ出力を維持する。
- **§3.1 DI (Dependency Injection) の実装**:
  - コンテナ登録は `DS4Windows/DI/ServiceRegistration.cs` に行う。
- **§3.2 巨大ファイルの編集方針（最重要）**:
  - 8,800行を超えるモンスターファイル `Mapping.cs` の内部実装や循環依存には深入りせず、呼び出し境界を薄いインターフェース `IMappingActionDispatcher` でラップする（Strangler Fig パターン）。

---

## 0. Step8の位置づけと現状分析

### 0.1 Step1監査結果に基づく対象範囲
`Phase5-Step1-legacy-delegation-audit-report.md` §2, §4-1 に基づき、以下の**2つのDIサービス**を対象とする。

- `IProfileActionProvider` → `ProfileActionProvider`（#8）: `Global.getProfileActions` / `Global.GetProfileAction` への静的迂回
- `IProfileActionChainService` → `ProfileActionChainService`（#9）: 静的 `Mapping.DispatchProfileActionEdge` への直接依存

### 0.2 現状のコード構造（GitHub実コード確認済み）
- **`ProfileActionProvider`**:
  - 実態は `Global.store`（`BackingStore`）のフィールド `profileActions` / `profileActionDict` を参照しているだけの薄いゲッター。
  - 二重管理などのバグはなく、単に静的 `Global` を経由しているだけであるため、コンストラクタで `BackingStore` を直接受け取るようにすれば安全にクリーン化できる。
- **`ProfileActionChainService`**:
  - 既に `IProfileActionProvider` が注入されており設計は健全。
  - 唯一の課題は、連鎖アクションを発火する際に静的 `Mapping.DispatchProfileActionEdge(nextAction, deviceIndex, true)` を直接呼んでいる点。

### 0.3 `Mapping.cs` に対する防衛境界
`Mapping.cs` は `ControlService` との循環依存を持つ巨大ファイルであり、本体の解体はリスクが極めて高い。
本Stepのスコープは、`DispatchProfileActionEdge` の呼び出し境界を薄いインターフェース `IMappingActionDispatcher` で包むことに限定し、`Mapping.cs` の内部には手を触れない。

### 0.4 全体4層モデルにおける位置づけ
いずれも**第4層 4-c**（アクション・プロファイルサービス）に属する。

---

## 1. 設計方針とアーキテクチャ

### 1.1 `ProfileActionProvider` の `Global` 迂回路排除
`BackingStore config = null`（既定値 `Global.store`）をコンストラクタで受け取り、`_config.profileActions` / `_config.profileActionDict` を直接参照する。

### 1.2 `IMappingActionDispatcher` による静的呼び出しの境界化
`DS4Windows/DI/IMappingActionDispatcher.cs` を新設し、静的呼び出しをラップする。

```csharp
namespace DS4Windows.DI
{
    public interface IMappingActionDispatcher
    {
        void DispatchProfileActionEdge(SpecialAction action, int deviceIndex, bool state);
    }
}
```

```csharp
// DS4Windows/DS4Control/Services/MappingActionDispatcher.cs 実装イメージ
namespace DS4Windows
{
    public class MappingActionDispatcher : IMappingActionDispatcher
    {
        public void DispatchProfileActionEdge(SpecialAction action, int deviceIndex, bool state)
        {
            Mapping.DispatchProfileActionEdge(action, deviceIndex, state);
        }
    }
}
```

### 1.3 `ProfileActionChainService` のリファクタリングと既存互換
`IMappingActionDispatcher` を注入し、直接の静的呼び出しを置換する。既存コードとの互換性（§2.1）を保つため、コンストラクタに既定値フォールバックを持たせる。

```csharp
public ProfileActionChainService(
    IProfileActionProvider actionProvider,
    IMappingActionDispatcher mappingDispatcher = null)
{
    _actionProvider = actionProvider ?? throw new ArgumentNullException(nameof(actionProvider));
    _mappingDispatcher = mappingDispatcher ?? new MappingActionDispatcher();
}
```

---

## 2. 成果物一覧

| 種別 | ファイルパス | 変更内容 |
|---|---|---|
| インターフェース | `DS4Windows/DI/IMappingActionDispatcher.cs` | `Mapping.DispatchProfileActionEdge` をラップする新規契約 |
| サービス実装 | `DS4Windows/DS4Control/Services/MappingActionDispatcher.cs` | 薄い委譲ラッパー実装 |
| プロバイダー改修 | `DS4Windows/DS4Control/Services/ProfileActionProvider.cs` | `BackingStore` 直接参照化、`Global` 依存排除 |
| 連鎖サービス改修 | `DS4Windows/DS4Control/Services/ProfileActionChainService.cs` | `IMappingActionDispatcher` 注入と既存互換コンストラクタ |
| DI 登録 | `DS4Windows/DI/ServiceRegistration.cs` | `IMappingActionDispatcher` → `MappingActionDispatcher` の Singleton 登録 |
| 単体テスト拡充 | `DS4WindowsTests/ProfileActionChainServiceTests.cs` | モックを使用した連鎖発火条件の完全自動テスト新設 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step8-1: `ProfileActionProvider` の `BackingStore` 直接参照化
1. `ProfileActionProvider.cs` のコンストラクタに `BackingStore config = null` を追加。
2. `Global.getProfileActions` / `GetProfileAction` 呼び出しを `_config` 参照に置換。

### タスク Step8-2: `IMappingActionDispatcher` & 実装クラスの新設
1. `DS4Windows/DI/IMappingActionDispatcher.cs` を新規作成。
2. `DS4Windows/DS4Control/Services/MappingActionDispatcher.cs` を新規作成し、委譲処理を記述。

### タスク Step8-3: DI コンテナ登録追加
1. `DS4Windows/DI/ServiceRegistration.cs` に `services.AddSingleton<IMappingActionDispatcher, MappingActionDispatcher>();` を追加。

### タスク Step8-4: `ProfileActionChainService` のリファクタリング
1. `ProfileActionChainService.cs` に `IMappingActionDispatcher` を注入。
2. `Mapping.DispatchProfileActionEdge` の直接呼び出しを `_mappingDispatcher.DispatchProfileActionEdge` に置換。

### タスク Step8-5: 単体テスト作成と自動テスト実行
1. `IMappingActionDispatcher` をモック化し、連鎖アクションが期待通りディスパッチされるかをテスト。
2. `dotnet test` でリグレッションがないことを確認。

### タスク Step8-6: ビルド検証、進捗更新、完了報告書の作成
1. Debug / Release ビルド成功を確認。
2. `Phase5-Status.md` を更新。
3. `Phase5-Step8-Completion-Report.md` を作成。

---

## 4. リスクと回避策

| リスク | 影響度 | 回避策 |
|---|---|---|
| **Mapping.cs の循環依存** | 高 | `Mapping.cs` 本体は一切解体せず、`IMappingActionDispatcher` 1枚を挟むだけに留める。 |
| **既存呼び出し元の破損** | 低 | コンストラクタに `mappingDispatcher = null` の既定値を設け、手動 new 時も動作させる。 |

---

## 5. 完了判定基準

- [ ] `ProfileActionProvider.cs` 内から `Global.getProfileActions` 等への依存が 0 件になっていること。
- [ ] `IMappingActionDispatcher` が新設され、DIコンテナに登録されていること。
- [ ] `ProfileActionChainService.cs` 内から静的 `Mapping.DispatchProfileActionEdge` への直接参照が 0 件になっていること。
- [ ] 連鎖条件のモック単体テストが成功すること。
- [ ] ビルドエラー・警告の増加がないこと。
