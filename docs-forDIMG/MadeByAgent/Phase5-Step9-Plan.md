# フェーズ5-Step9 計画書: Actions基盤とMacroPlayerの整理

作成日: 2026-09-03
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md` §2, §3 Step9（Phase5詳細計画書）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理）
- `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`（Step1監査結果。本Stepの対象根拠）
- `docs-forDIMG/MadeByAgent/Phase5-Step7-Plan.md`（SpecialAction永続化の責務分離）
- `docs-forDIMG/MadeByAgent/Phase5-Step8-Plan.md`（アクション連鎖処理の責務分離）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `ActionManager.cs` の静的メソッド群（`SetToggledOn` 等）は即座に削除・解体せず、内部で `IManagedActionManager` へ委譲する薄い互換シムとする。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - アクションのトグル状態の保持・通知、マクロ再生のキー押下・解放順序およびディレイ時間管理、コントローラー切断時のコントローラー破棄処理を100%維持する。
- **§2.3 ログ出力の厳格な維持**:
  - アクション発火、トグル変更、マクロ実行時の既存の `AppLogger` 出力をすべて維持する。
- **§3.1 DI (Dependency Injection) の実装**:
  - `DefaultActionManager` に `IActionFactory` を注入し、`DefaultMacroPlayer` に `IVirtualKBM` を注入する。
  - コンテナ登録は `DS4Windows/DI/ServiceRegistration.cs` に行う。
- **§3.2 巨大ファイルの編集方針**:
  - `Mapping.cs` や `ControlService.cs` からの静的呼び出しを破壊しないよう、シム互換を徹底する。

---

## 0. Step9の位置づけと現状分析

### 0.1 対象範囲と現状の課題（GitHub実コード確認済み）
`Phase5-Step1-legacy-delegation-audit-report.md` §2 表の#2（`IManagedActionManager` → `DefaultActionManager`）、および A案で統合された `DefaultMacroPlayer.cs` を対象とする。

1. **`DefaultActionManager` の静的3兄弟への丸投げ**:
   `IManagedActionManager` インターフェースは整備されているが、実装クラス `DefaultActionManager.cs` の内部は以下のように静的クラス群に直接依存している。
   - アクションのトグル状態更新: 静的 `ActionManager.SetToggledOn(...)`
   - アクションのインスタンス生成: 静的 `ActionFactory.CreateFrom(...)`（注入された `IActionFactory` が未使用）
   - アクション一覧の参照: 静的 `ActionRegistry.AllActions`
   - 切断時のクリア処理: 静的 `Mapping.ClearKeyButtonControllersForDevice(...)`
2. **`DefaultMacroPlayer` の KBM 直結**:
   `DefaultMacroPlayer.cs`（`IMacroPlayer` 実装）が、マクロのキー・マウス送出のために静的 `Global.outputKBMHandler` や `Mapping` を直接呼んでおり、テスト時に OS の実入力が汚染される。

### 0.2 全体4層モデルにおける位置づけ
本Stepは **第4層 4-c 設定・プロファイル・アクションサービス層** の締めくくりであり、ドメイン2（アクション系: Step 7〜9）を完全完結させる。

---

## 1. 設計方針とアーキテクチャ

事前検討に基づき、**論点1：案A（`DefaultActionManager` への `IActionFactory` 注入、トグル状態内包、静的クラスのシム化）** および **論点2：案1（`DefaultMacroPlayer` への `IVirtualKBM` 注入によるマクロテスト完全自動化）** を採用する。

### 1.1 `DefaultActionManager` の静的依存解消とインスタンス化
- **`IActionFactory` の注入**:
  コンストラクタで `IActionFactory` を受け取り、`ActionFactory.CreateFrom(...)` の静的呼び出しを `_actionFactory.CreateFrom(...)` に置き換える。
- **トグル状態管理の内包**:
  静的 `ActionManager` が抱えていたトグル状態辞書を、`DefaultActionManager` 自身のインメモリ状態（`ConcurrentDictionary<string, bool> _toggledOn`）として内包し、スレッドセーフな状態管理を確立する。

```csharp
// DefaultActionManager.cs 実装イメージ
public class DefaultActionManager : IManagedActionManager
{
    private readonly IActionFactory _actionFactory;
    private readonly ConcurrentDictionary<string, bool> _toggledOn = new ConcurrentDictionary<string, bool>();

    public DefaultActionManager(IActionFactory actionFactory)
    {
        _actionFactory = actionFactory ?? throw new ArgumentNullException(nameof(actionFactory));
    }

    public bool GetToggledOn(string actionName)
    {
        return _toggledOn.TryGetValue(actionName, out bool state) && state;
    }

    public void SetToggledOn(string actionName, bool state)
    {
        _toggledOn[actionName] = state;
        ToggledOnChanged?.Invoke(this, new ToggledOnChangedEventArgs(actionName, state));
    }
}
```

---

### 1.2 静的 `ActionManager` の薄いシム化（互換ラッパー原則 §2.1）
`Mapping.cs` や `ControlService.cs` に残存する `ActionManager.SetToggledOn` などの呼び出し元を破壊しないため、静的 `ActionManager` を `IManagedActionManager` へ委譲する薄いシムとする。

```csharp
// ActionManager.cs（静的シム化イメージ）
public static class ActionManager
{
    private static IManagedActionManager Service => AppHost.Services.GetService<IManagedActionManager>();

    public static void SetToggledOn(string actionName, bool state)
    {
        Service?.SetToggledOn(actionName, state);
    }
}
```

---

### 1.3 `DefaultMacroPlayer` への `IVirtualKBM` 注入とマクロテスト自動化
`DefaultMacroPlayer.cs` のコンストラクタに `IVirtualKBM`（Step 10 で DI 登録される契約）を注入し、静的 `Global.outputKBMHandler` への直接依存を排除する。

```csharp
// DefaultMacroPlayer.cs 実装イメージ
public class DefaultMacroPlayer : IMacroPlayer
{
    private readonly IVirtualKBM _virtualKBM;

    public DefaultMacroPlayer(IVirtualKBM virtualKBM = null)
    {
        // 既存互換フォールバック（§2.1）
        _virtualKBM = virtualKBM ?? AppHost.Services.GetService<IVirtualKBM>() ?? Global.outputKBMHandler;
    }

    public async Task PlayMacroAsync(MacroAction action, CancellationToken ct)
    {
        foreach (var step in action.Steps)
        {
            if (ct.IsCancellationRequested) break;
            _virtualKBM.SendMacroKey(step.KeyValue, step.IsKeyDown);
            if (step.DelayMs > 0) await Task.Delay(step.DelayMs, ct);
        }
    }
}
```

- **テスト容易性の確立**:
  単体テストにおいて `Mock<IVirtualKBM>` を渡すことで、**OS の実キーボード・マウスを一切汚染せずに、マクロのキー送出順序・タイミングが 100% 正しいかを自動テスト（`dotnet test`）で検証可能**になる。

---

## 2. 成果物一覧

| 種別 | ファイルパス | 変更内容 |
|---|---|---|
| マネージャー改修 | `DS4Windows/DS4Control/DefaultActionManager.cs` | `IActionFactory` 注入、トグル状態（`_toggledOn`）の内包 |
| シム化 | `DS4Windows/DS4Control/ActionManager.cs` | `IManagedActionManager` への薄い委譲シム化 |
| プレイヤー改修 | `DS4Windows/Actions/DefaultMacroPlayer.cs` | `IVirtualKBM` 注入による静的 `outputKBMHandler` の排除 |
| DI 登録確認 | `DS4Windows/DI/ServiceRegistration.cs` | `DefaultActionManager` および `DefaultMacroPlayer` の注入解決確認 |
| 単体テスト拡充 | `DS4WindowsTests/ActionManagerTests.cs` | トグル状態管理・イベント発火の単体テスト |
| 単体テスト新設 | `DS4WindowsTests/MacroPlayerTests.cs` | `Mock<IVirtualKBM>` によるマクロ再生順序・遅延の完全自動テスト |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step9-1: `DefaultActionManager` の呼び出し元と依存精査
1. `DefaultActionManager.cs` を精査し、`ActionFactory.CreateFrom`、`ActionManager`、`ActionRegistry` の全参照箇所を特定する。

### タスク Step9-2: `DefaultActionManager` のリファクタリング
1. コンストラクタに `IActionFactory` を追加注入。
2. `ConcurrentDictionary<string, bool>` によるトグル状態管理を実装。
3. `ActionFactory.CreateFrom` の静的呼び出しを `_actionFactory.CreateFrom` に置換。

### タスク Step9-3: 静的 `ActionManager` のシム化
1. `ActionManager.cs` をシム化し、`IManagedActionManager` への委譲にピンポイント置換する。

### タスク Step9-4: `DefaultMacroPlayer` への `IVirtualKBM` 注入
1. `DefaultMacroPlayer.cs` のコンストラクタに `IVirtualKBM` を追加（フォールバック付き）。
2. 静的 `Global.outputKBMHandler` 呼び出しを `_virtualKBM` 経由に置換。

### タスク Step9-5: DIコンテナ登録の整合
1. `DS4Windows/DI/ServiceRegistration.cs` におけるコンストラクタ解決を確認。

### タスク Step9-6: 単体テスト作成と自動テスト実行
1. `ActionManagerTests.cs` でトグル状態の独立テストを作成。
2. `MacroPlayerTests.cs` を新設し、`Mock<IVirtualKBM>` でマクロキー送信を検証。
3. `dotnet test` でリグレッションがないことを確認。

### タスク Step9-7: ビルド検証、進捗更新、完了報告書の作成
1. Debug / Release ビルド成功を確認。
2. `Phase5-Status.md` の Step9 を「計画書承認済」に更新。
3. `Phase5-Step9-Completion-Report.md` を作成。

---

## 4. リスクと回避策

| リスク | 影響度 | 回避策 |
|---|---|---|
| **既存静的参照の破壊** | 高 | `ActionManager` を直接解体せず、`IManagedActionManager` への薄いシムとして温存する（§1.2）。 |
| **トグル状態のマルチスレッド競合** | 中 | `ConcurrentDictionary<string, bool>` を使用し、スレッドセーフな読み書きを保証する（§1.1）。 |
| **テスト時の実キー汚染** | 中 | `DefaultMacroPlayer` に `IVirtualKBM` を注入し、テスト時はモックで完全にトラップする（§1.3）。 |

---

## 5. 完了判定基準

- [ ] `DefaultActionManager.cs` 内から静的 `ActionFactory.CreateFrom` および `ActionManager.SetToggledOn` への依存が解消されていること。
- [ ] アクションのトグル状態が `DefaultActionManager` 内部でスレッドセーフに管理されていること。
- [ ] 静的 `ActionManager.cs` がシム化され、既存の静的呼び出し元との互換性が維持されていること。
- [ ] `DefaultMacroPlayer.cs` が `IVirtualKBM` 経由でキー送出を行い、静的 `Global.outputKBMHandler` への直接依存が排除されていること。
- [ ] 単体テストがすべてパスし、ビルドエラー・警告増がないこと。
