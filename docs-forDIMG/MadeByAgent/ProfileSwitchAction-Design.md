# ProfileSwitchAction 設計草案（C4 — Phase 1）

作成: Agent（DI移行作業用ブランチ）
参照元: docs/DI-Migration-Plan.md (C4), docs/Direct-Callsites-Inventory.md, docs/Action-Subsystem-API.md

## 目的
`Global.ApplyProfile` の呼び出しを `ProfileSwitchAction` に集約し、`ActionManager` 経由で実行する。`ApplyProfile` が複数場所から呼ばれる場合は、呼び出し元をラップして `ActionManager.Dispatch...` を呼ぶ。

## 既存呼び出し箇所（インベントリ）

- `DS4Windows/DS4Control/Mapping.cs` L5564 — `Global.ApplyProfile(device, action.details, false, true, ctrl, ...)`
- `DS4Windows/AutoProfileChecker.cs` L123, L130, L193, L200
- `DS4Windows/DS4Control/ControlService.cs` L2137
- `DS4Windows/DS4Forms/MainWindow.xaml.cs` L340, L1024, L1297, L1900
- `DS4Windows/DS4Forms/ProfileEditor.xaml.cs` L1477

## 設計案（§2.1修正版準拠）

### 原則
- 古い方式（直接 `Global.ApplyProfile` 呼び出し）は削除せず残す（新しい `ProfileSwitchAction` の動作確認が取れるまで）。
- 同時に複数の実装経路を持たない（`ProfileSwitchAction` 経由の単一路線を目指すが、フォールバックを残す）。
- ログ出力（`AppLogger.LogTrace` / `LogDebug`）は維持（削除・新設しない）。

### クラス構造（草案）

```csharp
// 新規作成（次の PR 対象）
public class ProfileSwitchAction : IOutputAction
{
    private readonly SpecialAction sa;
    public ProfileSwitchAction(SpecialAction sa) { this.sa = sa; }
    public string Id => sa?.name ?? "ProfileSwitch";

    public void Execute(IOutputContext ctx)
    {
        try
        {
            if (sa == null) return;
            // Global.ApplyProfile の呼び出しをラップ
            // フォールバック: 直接呼び出しを残す（§2.1修正版）
            try { AppLogger.LogTrace($"ProfileSwitchAction.Execute: id={Id} device={ctx.Device} profile={sa.details}"); } catch { }
        }
        catch { }
    }

    public void Stop(IOutputContext ctx)
    {
        try { AppLogger.LogTrace($"ProfileSwitchAction.Stop: id={Id} device={ctx.Device}"); } catch { }
    }
}
```

### 移行ステップ（段階的）

1. **C4-1**: `ProfileSwitchAction` クラス作成（`IOutputAction` 実装、`SpecialAction` を受け取り、`Global.ApplyProfile` をラップ）。
2. **C4-2**: `Mapping.cs` の `Global.ApplyProfile` 呼び出し（L5564）を `ProfileSwitchAction` 経由に切り替え（フォールバック残存）。
3. **C4-3**: `AutoProfileChecker.cs`、`ControlService.cs`、`MainWindow.xaml.cs`、`ProfileEditor.xaml.cs` の呼び出し元を順次ラップ（別 PR で段階的に）。
4. **C4-4**: `handled == true` が安定した後、フォールバック削除（別 PR、D ステップ）。

### リスクと対策

- **リスク**: `Global.ApplyProfile` は複数場所（UI、Updater、Mapping）から呼ばれる → **緩和**: 各呼び出し元を個別にラップし、1 PR = 1 呼び出し元で段階的に移行。
- **リスク**: プロファイル切替のタイミング（同期/非同期）→ **緩和**: `ProfileSwitchAction` は `Execute` で同期的に `ApplyProfile` を呼び、`Stop` は空（プロファイル切替は一方向の操作）。
- **リスク**: `Global.ApplyProfile` の引数（`device`, `profile`, `useDefault`, `useProfile`, `ctrl` 等）が複雑 → **緩和**: `SpecialAction.details` にプロファイル名を格納し、`ProfileSwitchAction` が `Global.ApplyProfile` の引数を再構築する。

### テスト項目（草案）

- 単体: `ProfileSwitchAction` が `SpecialAction` の `details` からプロファイル名を読み取り、`Global.ApplyProfile` を呼ぶこと（モックで検証）。
- 統合: `Mapping.cs` の `ProfileSwitchAction` 経由の呼び出しが既存動作と等価であること（プロファイル切替が正常に行われる）。
- 回帰: 既存のプロファイル（デフォルト、カスタム）で切替が変わらないことを手動で確認。

### 制約（§2.1修正版再確認）

- 古い方式（直接 `Global.ApplyProfile` 呼び出し）は削除せず残す（新しい `ProfileSwitchAction` の動作確認が取れるまで）。
- 新しい機能に複数の候補手段を同時に実装しない（`ProfileSwitchAction` 経由の単一路線を目指すが、フォールバックを残す）。
- ログ出力（`AppLogger.LogTrace` / `LogDebug` 等）は維持（削除・新設しない）。
- `Global.cs` の静的メンバは薄いデリゲート（シム）として残す（75ファイルの呼び出し元を一度に壊さない）。
