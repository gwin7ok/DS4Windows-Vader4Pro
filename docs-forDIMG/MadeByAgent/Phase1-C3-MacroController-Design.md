# MacroController 設計草案（C3 — Phase 1）

作成: Agent（DI移行作業用ブランチ）
参照元: docs/DI-Migration-Plan.md (C3), docs/Action-Subsystem-API.md, docs/SpecialAction-Instance-Design.md, docs/TriggerToSendFlow.md

## 目的
`PlayMacro` / `PlayMacroTask` の既存ロジックを `MacroController` に移し、`ActionManager` 経由で呼ぶようにする。マクロキュー管理は `MacroController` に移す。

## 既存ロジックの要点（調査結果）

- `PlayMacro` は `Mapping.cs` から直接呼ばれる（`docs/Direct-Callsites-Inventory.md` L5, L15 参照）。
- `PlayMacroTask` はマクロ実行の非同期タスク（`docs/Action-Subsystem-API.md` L40 参照）。
- マクロは `Macro, HoldMacro` の `keyType` を持つ（ログ `ds4windows_log_20260826_1.txt` L2102 参照）。
- `MacroAction` は `SpecialActionBase` を継承し、`PlayMacro` を呼ぶ（`docs/SpecialAction-Instance-Design.md` L55 参照）。
- マクロシーケンスは逐次 `SyntheticDispatcher` を直接呼んで送出（`docs/TriggerToSendFlow.md` L31 参照）。

## 設計案（§2.1修正版準拠）

### 原則
- 古い方式（`Mapping.cs` の直接 `PlayMacro` 呼び出し）は削除せず残す（新しい `MacroController` の動作確認が取れるまで）。
- 同時に複数の実装経路を持たない（`MacroController` 経由の単一路線を目指すが、フォールバックを残す）。
- ログ出力（`AppLogger.LogTrace` / `LogDebug`）は維持（削除・新設しない）。

### クラス構造（草案）

```csharp
// 新規作成（次の PR 対象）
public interface IMacroController
{
    bool IsMacroRunning { get; }
    Task PlayMacroAsync(int device, string macroStr, List<int> macroLst, int[] macroArr, string triggerKey, bool synchronized, SpecialAction action = null);
    void StopMacro(int device);
}

public class MacroController : IMacroController
{
    // マクロキュー管理（device ごと）
    // 既存の PlayMacroTask のロジックを内部で再利用
    // ActionManager 経由で呼ばれる際は、MacroAction がこのコントローラを利用
}
```

### 移行ステップ（段階的）

1. **C3-1**: `IMacroController` インターフェース作成（コンストラクタ・インジェクション用）。
2. **C3-2**: `MacroController` 実装（既存 `PlayMacro` / `PlayMacroTask` のロジックを移植、フォールバック残存）。
3. **C3-3**: `MacroAction` が `IMacroController` を利用するよう修正（`ActionManager` 経由で呼ばれる）。
4. **C3-4**: `Mapping.cs` の直接 `PlayMacro` 呼び出しを `MacroController` 経由に切り替え（フォールバック残存）。
5. **C3-5**: `handled == true` が安定した後、フォールバック削除（別 PR、D ステップ）。

### リスクと対策

- **リスク**: マクロや連打・リピート挙動の微妙な再現差 → **緩和**: 既存 `outputKBMHandler` をモックして比較テスト（`MockManagedActionManager` を拡張）。
- **リスク**: 並列・非同期性（`PlayMacroTask` の非同期実行）→ **緩和**: `MacroController` 内でキュー管理を厳密にし、二重実行を防止（`IsMacroRunning` ガードを維持）。
- **リスク**: `MacroAction` の `SpecialActionBase` 継承と `IMacroController` の依存関係 → **緩和**: `MacroAction` はコンストラクタで `IMacroController` を受け取り、`ActionManager` が `MacroController` を DI 登録する。

### テスト項目（草案）

- 単体: `MacroController.PlayMacroAsync` が既存 `PlayMacro` と同等のマクロシーケンスを生成すること。
- 統合: `ActionManager.DispatchTriggerEstablished` → `MacroAction` → `MacroController` の経路でマクロが実行されること。
- 回帰: 既存のマクロプロファイル（`Macro, HoldMacro`）で入力送出が変わらないことを手動／自動で確認。

### 次のアクション

- `IMacroController` のインターフェース定義を作成（C3-1）。
- `MacroController` の実装草案を作成（C3-2）。
- `MacroAction` の修正案を作成（C3-3）。

## 制約（§2.1修正版再確認）

- 古い方式（`Mapping.cs` の直接 `PlayMacro` 呼び出し）は削除せず残す（新しい `MacroController` の動作確認が取れるまで）。
- 新しい機能に複数の候補手段を同時に実装しない（`MacroController` 経由の単一路線を目指すが、フォールバックを残す）。
- ログ出力（`AppLogger.LogTrace` / `LogDebug` 等）は維持（削除・新設しない）。
- `Global.cs` の静的メンバは薄いデリゲート（シム）として残す（75ファイルの呼び出し元を一度に壊さない）。
