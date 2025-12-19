# DI 経由への置換 — マイグレーション計画

作成日: 2025-12-20

目的: 現在 `Mapping.cs` 等に分散している直接副作用（キーボード/マウス送出、マクロ再生、プロファイル切替、外部プロセス起動等）を、`IManagedActionManager` / `ActionManager` と SpecialAction コントローラ経由の単一ルートに移行して、テスト性・保守性・一貫性を向上させる。

前提:
- `ActionManager`（および `DefaultActionManager`）は既に DI 登録されており、`DispatchTriggerEdge/Established/Released` API が存在する。
- まずは「検出(Trigger) → Dispatch(Manager) → Controller.Handle(送出)」というフローを確立し、既存の直接 `outputKBMHandler` 呼び出しなどは段階的に置換する。

優先順位（高 → 低）
- 1) `Mapping.cs` 内の SpecialAction に紐づく外部副作用（例: キー/MOUSE 送出、`specActionLaunchProc`、`PlayMacro`、`Global.ApplyProfile`）
- 2) Macro 系（`PlayMacro` / `PlayMacroTask`） — マクロは並列・非同期性があるため慎重にテスト
- 3) Updater / Updater2（`externals/DS4Updater`）・UI（`DS4Forms`）での `Process.Start` 呼び出し — アプリ起動/外部ツール起動なので仕様に応じて例外扱い
- 4) その他ユーティリティ (`ScpUtil`, `Util`, `ControlService` 等) のプロセス操作

段階的作業計画

ステップ A — インベントリ & テスト基盤（完了目標: 1–2 日）
- A1: 既存インベントリを拡充（完了: `docs/Direct-Callsites-Inventory.md`）
- A2: `IManagedActionManager` のモック実装を作成し、キー送出/トグルの振る舞いを再現する単体テストを用意する（`DS4WindowsTests` に追加）
- A3: CI で動く小さな統合テスト（モック OutputHandler を用いて Dispatch から Controller 経由での送出が行われることを検証）

ステップ B — 低リスクで小さな置換（完了目標: 数日）
- B1: `Mapping.cs` の「TryDispatchSATriggerEstablished` / `TryDispatchSATriggerReleased` を常に `ActionManager.DispatchTrigger...` を経由するように厳密化し、返り値 `handled` に応じてのみ既存の直接 send を呼ぶフォールバックを残す（まずはフォールバックを残す）
- B2: テストを回し、`handled == true` の場合に直接 send が呼ばれないことを確認

ステップ C — 機能別の移行（1機能ずつ安全に）
- C1: Key send 系（`PerformKeyPress*`, `PerformKeyRelease*`, `PerformKeyPressAlt`）を `KeyOutputAction` に集約。`KeyActionBinding` を介して `OutputContext` を渡し、送出の実装はコントローラに委任する。
- C2: Mouse / Move 系を同様に `MouseOutputAction` にまとめる。
- C3: Macro 系は `MacroAction` を実装し、既存の `PlayMacro` ロジックを内部で再利用しつつ `ActionManager` 経由で呼ぶようにする。マクロキュー管理は `MacroController` に移す。
- C4: `Global.ApplyProfile` は `ProfileSwitchAction` を作り、ActionManager 経由で実行。ApplyProfile が複数場所から呼ばれる場合は、呼び出し元をラップして `ActionManager.Dispatch...` を呼ぶ。
- C5: SpecialAction による外部プロセス起動（`specActionLaunchProc`）は `LaunchProcessAction` として抽象し、プロセス起動はインターフェース経由（モック可能）にする。

ステップ D — フォールバックの削除と整流化
- D1: 上記機能ごとに `handled` 判定が確実に true を返すようコントローラを完成させ、既存の Mapping の直接 send フォールバックを順次削除。
- D2: 各機能の削除は小パッチ（1機能 = 1 PR）で行い、回帰テストを必須にする。

ステップ E — ドキュメントとロールアウト
- E1: `docs/Direct-Callsites-Inventory.md` と `docs/DI-Migration-Plan.md` を更新
- E2: リグレッション対策としてリリース候補ブランチで十分な実機テスト（特にマクロ・複合トグル）を実施

変更の粒度 & PR 方針
- 1 PR = 1 機能（例: Key send の移行）
- 各 PR にユニットテスト + 小さな統合テストを含める
- 既存動作との互換性を保つため、最初はフォールバックを残し、フォールバック削除は別 PR

スケジュール見積もり（概算）
- インベントリ・テスト基盤: 1–2 日
- Key / Mouse 移行: 2–4 日
- Macro 移行: 3–5 日（テストケース多め）
- Profile / Launch 移行: 1–3 日
- 全体完了（安定化含む）: 2–4 週間（レビュー・テスト含む）

リスクと緩和策
- リスク: マクロや連打・リピート挙動の微妙な再現差→ 緩和: 既存 `outputKBMHandler` をモックして比較テスト
- リスク: 外部プロセス起動の権限/コンテキスト問題→ 緩和: `LaunchProcessAction` に呼び出し元情報/UseShellExecute フラグ等を明示
- リスク: UI/Updater 系の起動処理は仕様的に置換不可な場合がある→ 緩和: UI/Updater は対象外とし、別途 Integration note を残す

最初に着手すべき具体タスク（次アクション）
- 1) `Mapping.cs` にある直接呼び出しのうち、最も単純な `PerformKeyPress/PerformKeyRelease` 帯域を `KeyOutputAction` でラップする小さな PR 案を作成する
- 2) `IManagedActionManager` の軽量モックを `DS4WindowsTests` に追加し、Dispatch → Controller 経路のユニットテストを作る
- 3) マクロ周りの既存 `PlayMacro` をリファクタ案として切り出し、`MacroController` 設計草案を作る

連絡事項
- UI / Updater 系の `Process.Start` 呼び出しは仕様的な起動処理であり、DI 置換の優先度は低め。まずは `Mapping.cs` と SpecialAction に集中する。

---

保存場所: `g:/Cursor_Folder/DS4Windows-Vader4Pro/docs/DI-Migration-Plan.md`

必要なら、このファイルを基に最初の小さな PR（`Key send` のラップ）用のパッチを作成します。どれを最初に作りますか？

## Key send（`PerformKeyPress*` / `PerformKeyRelease*`）のラップ案

目的: `Mapping.cs` に散在する `PerformKeyPress*` / `PerformKeyRelease*` 呼び出しを `ActionManager` 経由の `KeyOutputAction` に集約し、コントローラ（`KeyButtonActionController` 等）が実際の送出を担うようにする。

設計サマリ:
- 新規クラス: `KeyOutputAction` (implements `IOutputAction`) — `Execute(OutputContext ctx)` / `Stop(OutputContext ctx)` を提供し、内部で `VirtualKBMBase` を介してキー送出を行う（現状の `outputKBMHandler` 呼び出しはここに集約）。
- 既存バインディング: `KeyActionBinding` を使って `KeyOutputAction` を構築し、`ActionManager` が `TriggerContext` を渡して `OnTriggered` / `OnReleased` を呼ぶ流れに統一する。
- Mapping の変更: 直接 `outputKBMHandler.PerformKeyPress...` を呼ぶ箇所を段階的に次のように置換する。
	1. 先に `ActionManager.DispatchTriggerEstablished/Released(action, device, logical, native, useScan, outputKBMHandler)` を呼ぶ（既に呼んでいる箇所あり）。
	2. `handled == true` の場合は直接呼び出しを行わない。`handled == false` の場合のみ暫定フォールバックとして従来の `PerformKeyPress*` を呼ぶ（フォールバックは段階的に削除）。

実装手順（小さな PR 単位）:
1. `DS4Windows/Actions/KeyOutputAction.cs` を追加。`IOutputAction` 実装で `OutputContext` と `VirtualKBMBase` を受け取り、`PerformKeyPress/Release` 相当を呼ぶ。
2. `KeyActionBinding` が `KeyOutputAction` を作成する箇所を確認・必要であれば修正して `OutputAction` を登録。
3. `DS4WindowsTests` にモック `VirtualKBMBase` を用いたユニットテストを追加:
	 - `DispatchTriggerEstablished` → `KeyOutputAction.Execute` が呼ばれる
	 - `DispatchTriggerReleased` → `KeyOutputAction.Stop` が呼ばれる
	 - `handled == true` 時に `Mapping.cs` の直接 send が発生しないことの検証（最初は Mapping にてフォールバックを有効にしておく）
4. `Mapping.cs` の代表的呼び出し箇所（例: `L1725/L1743/L1776/L1796/L1817/L1831`）を順次置換し、1つの Pull Request として提出。

テスト項目:
- 単体: `KeyOutputAction` が `VirtualKBMBase` の該当メソッドを呼ぶこと
- 統合: `ActionManager`（実装またはモック）を経由した Dispatch が Mapping の既存動作と等価であること（トグル・リピート・フェイクリピート含む）
- 回帰: 既存のプロファイルとマクロで入力送出が変わらないことを手動／自動で確認

リスクと対策:
- キーリピートや `fakeKeyRepeat` の微妙な条件差→ 既存 `outputKBMHandler` のロジックを `KeyOutputAction` 内に移植し、同一条件で動作させる。
- 置換中の一時不整合→ フォールバックを残し、段階的に削除する。

PR チェックリスト（1 PR = 1 範囲）:
- 変更ファイル一覧 & 影響範囲の説明
- ユニットテスト追加（モック）
- 動作手順 / 回帰テスト案
- 既知の差分とフォールバックの説明

推奨進め方: 最初の PR は `KeyOutputAction` クラス追加 + `KeyActionBinding` の軽微な修正 + 単体テスト。次の PR で `Mapping.cs` の 1–2 箇所を `Dispatch` 経由に切り替える。

---

（この節を基に小さなパッチを自動生成しますか？）