# LaunchProcessAction 設計草案（C5 — Phase 1）

作成: Agent（DI移行作業用ブランチ）
参照元: docs/DI-Migration-Plan.md (C5), docs/Direct-Callsites-Inventory.md, docs/Action-Subsystem-API.md, .github/copilot-instructions.md §2.1修正版

## 目的
`specActionLaunchProc`（外部プログラム起動）の抽象化を `LaunchProcessAction` に集約し、`ActionManager` 経由で実行する。`Process.Start` は仕様上直接呼ぶケースが合理的なもの（分類③④⑤）は対象外とし、分類①（SpecialAction起動）と分類②（権限昇格再起動）、分類⑥（多重起動チェック）を対象とする（`docs/DI-App-Wide-Migration-Plan.md` §4.4 参照）。

## 既存呼び出し箇所（インベントリ）

`docs/Direct-Callsites-Inventory.md` の調査結果より、`Process.Start` 系の呼び出しは以下に分類される（`docs/DI-App-Wide-Migration-Plan.md` §4.4 の分類と対応）：

- **分類①（SpecialAction「プログラム起動」機能）**: `specActionLaunchProc` の抽象化対象。`Mapping.cs` 内で `SpecialAction` の `typeID` が `LaunchProcess` 相当の場合に `Process.Start` が呼ばれる（インベントリ上は `specActionLaunchProc` として記録される想定）。
- **分類②（昇格・権限関連の子プロセス起動）**: デバイス再有効化のための管理者権限再起動。`ControlService` または `Global` 側で呼ばれる可能性が高い（インベントリ上は別途確認が必要）。
- **分類⑥（多重起動チェック・ヘルパープロセス起動）**: `Process.GetProcesses()` 等を含む。`Mapping.cs` または `Program.cs` 側で呼ばれる可能性がある。
- **分類③（UIからの外部ツール起動）**: `joy.cpl` 等。対象外（決め打ちの単純起動）。
- **分類④（外部URL/ブラウザ起動）**: ヘルプページ等。対象外（`IBrowserLauncher` 化は任意）。
- **分類⑤（Updater/Updater2関連）**: 別プロセス境界。対象外。

## 設計案（§2.1修正版準拠）

### 原則
- 古い方式（直接 `Process.Start` 呼び出し）は削除せず残す（新しい `LaunchProcessAction` の動作確認が取れるまで）。
- 同時に複数の実装経路を持たない（`LaunchProcessAction` 経由の単一路線を目指すが、フォールバックを残す）。
- ログ出力（`AppLogger.LogTrace` / `LogDebug` 等）は維持（削除・新設しない）。
- `Global.cs` の静的メンバは薄いデリゲート（シム）として残す（75ファイルの呼び出し元を一度に壊さない）。

### クラス構造（草案）

```csharp
// 新規作成（C5 の PR 対象）
public class LaunchProcessAction : IOutputAction
{
    private readonly SpecialAction sa;
    public LaunchProcessAction(SpecialAction sa) { this.sa = sa; }
    public string Id => sa?.name ?? "LaunchProcess";

    public void Execute(IOutputContext ctx)
    {
        try
        {
            if (sa == null) return;
            // specActionLaunchProc の抽象化: sa.details に起動パスを格納
            string processPath = sa.details; // または別途解析
            try { AppLogger.LogTrace($"LaunchProcessAction.Execute: id={Id} device={ctx.Device} path={processPath}"); } catch { }

            // 分類①（SpecialAction起動）の実行
            // フォールバック: 直接 Process.Start 呼び出しを残す（§2.1修正版）
            // 新経路: IProcessLauncher 経由（DI登録済みを想定）
            try
            {
                var launcher = DS4Windows.DI.ServiceProviderHolder.GetRequiredService<IProcessLauncher>();
                launcher.Launch(processPath);
            }
            catch
            {
                // フォールバック（古い方式を残す）
                System.Diagnostics.Process.Start(processPath);
            }
        }
        catch { }
    }

    public void Stop(IOutputContext ctx)
    {
        // プロセス起動は一方向操作のため Stop は空（ログのみ維持）
        try { AppLogger.LogTrace($"LaunchProcessAction.Stop: id={Id} device={ctx.Device}"); } catch { }
    }
}
```

### 関連インターフェース（新設 / 既存活用）

`docs/DI-App-Wide-Migration-Plan.md` §5.4 のインターフェース一覧に基づき、C5 で必要となるものは以下の通り（既存のものはそのまま活用、新設が必要なもののみ追加）：

| インターフェース | 用途 | 状態 |
|---|---|---|
| `IOutputAction` | `LaunchProcessAction` の基底 | 既存（`Actions/IOutputAction.cs`） |
| `IProcessLauncher` | `Process.Start` の抽象化（分類①・②・⑥共通） | **新設推奨**（`docs/DI-App-Wide-Migration-Plan.md` §5.4 には直接記載なしだが、§4.4 の分類に対応する抽象化として必要） |
| `IElevatedProcessLauncher` | 権限昇格を伴う起動（分類②） | **新設推奨**（`docs/DI-App-Wide-Migration-Plan.md` §5.3 の Singleton として登録） |
| `IProcessInspector` | 多重起動チェック（分類⑥） | **新設推奨**（同上） |

**注意**: `docs/DI-App-Wide-Migration-Plan.md` §5.4 の確定版インターフェース一覧（10種）には `IProcessLauncher` 系は含まれていない。これは `Process.Start` 系が「対象外」として整理されていたためである（§4.4 の分類③④⑤）。ただし分類①②⑥を対象とする場合、これらの抽象化インターフェースが必要になる。**本設計では、`LaunchProcessAction` が `IProcessLauncher` をコンストラクタで受け取る形を推奨するが、`IProcessLauncher` の正式な DI 登録はフェーズ5（仕上げ）で行うことを前提とし、本フェーズ（C5）ではインターフェース定義と `LaunchProcessAction` の実装に留める。**

### 移行ステップ（段階的、§2.1修正版準拠）

1. **C5-1**: `LaunchProcessAction` クラス作成（`IOutputAction` 実装、`SpecialAction` を受け取り、`Process.Start` をラップ）。フォールバック（直接 `Process.Start`）を残す。
2. **C5-2**: `Mapping.cs` の `specActionLaunchProc` 相当の呼び出し箇所を `LaunchProcessAction` 経由に切り替え（フォールバック残存）。
3. **C5-3**: `IProcessLauncher` / `IElevatedProcessLauncher` / `IProcessInspector` のインターフェース定義を作成（実装は空または既存 `Process.Start` へのデリゲート）。
4. **C5-4**: `LaunchProcessAction` が `IProcessLauncher` をコンストラクタで受け取るよう修正（DI 登録はフェーズ5で実施）。
5. **C5-5**: `handled == true` が安定した後、フォールバック削除（別 PR、D ステップ）。

### リスクと対策

- **リスク**: `Process.Start` は OS 依存の副作用を伴い、テスト環境での再現が困難 → **緩和**: `IProcessLauncher` のモックを `DS4WindowsTests` に追加し、`LaunchProcessAction` の単体テストをモックベースで実施。実機テストは手動で限定的に行う。
- **リスク**: 権限昇格（分類②）が必要な場合、`Process.Start` の引数（`Verb = "runas"` 等）が複雑 → **緩和**: `IElevatedProcessLauncher` は `LaunchProcessAction` とは別に管理し、`SpecialAction.details` に昇格フラグを含める設計を避ける（`LaunchProcessAction` は単純起動に限定）。
- **リスク**: `specActionLaunchProc` の `SpecialAction` 定義が複数プロファイルに存在する可能性 → **緩和**: `LaunchProcessAction` は `SpecialAction` の `name` / `details` のみを参照し、プロファイル依存の状態を持たない（Singleton で安全）。

### テスト項目（草案）

- 単体: `LaunchProcessAction` が `SpecialAction.details` から起動パスを読み取り、`IProcessLauncher.Launch` を呼ぶこと（モックで検証）。
- 統合: `Mapping.cs` の `LaunchProcessAction` 経由の呼び出しが既存動作と等価であること（指定されたプログラムが起動される）。
- 回帰: 既存の `specActionLaunchProc` を含むプロファイルで、外部プログラム起動が変わらないことを手動で確認。

### 制約（§2.1修正版再確認）

- 古い方式（直接 `Process.Start` 呼び出し）は削除せず残す（新しい `LaunchProcessAction` の動作確認が取れるまで）。
- 新しい機能に複数の候補手段を同時に実装しない（`LaunchProcessAction` 経由の単一路線を目指すが、フォールバックを残す）。
- ログ出力（`AppLogger.LogTrace` / `LogDebug` 等）は維持（削除・新設しない）。
- `Global.cs` の静的メンバは薄いデリゲート（シム）として残す（75ファイルの呼び出し元を一度に壊さない）。

## 次のアクション（C5 の開始）

- `LaunchProcessAction` のクラス定義を作成（`DS4Windows/Actions/LaunchProcessAction.cs`）。
- `IProcessLauncher` のインターフェース定義を作成（`DS4Windows/Actions/IProcessLauncher.cs`、必要に応じて `IElevatedProcessLauncher` / `IProcessInspector` も同時作成）。
- `Mapping.cs` の `specActionLaunchProc` 相当の呼び出し箇所を特定（`docs/Direct-Callsites-Inventory.md` の追加調査が必要な場合は別途実施）。
