# Action Subsystem API — 仕様

目的: `Mapping` に分散していたランタイム・フラグと同期ロジックを責務ごとに分離し、可読性・保守性・テスト性を改善する。

設計カテゴリ:
- 各アクションインスタンス（`ActionInstanceState`）: アクション単位・デバイス単位の一時状態を保持。
- 各デバイスランタイム（`DeviceRuntimeState`）: デバイス固有のランタイム管理（アンロード制御、ライトバー、マクロ制御キューなど）。
- アクション一覧 / レジストリ（`ActionRegistry`）: SpecialAction 定義一覧、初期化とサイズチェック、診断ログ。
- マクロ管理（`MacroManager`）: マクロの実行と同期（同期マクロはトリガ単位でFIFOで実行）。
- トグル制御（`ToggleController`）: Toggle デバウンスと PressedOnce ロジック。
- リリースポリシー（`ReleasePolicy`）: uTrigger/untrigger の評価ロジック。

ファイルと主要 API

1) `ActionManager.cs`
- 既存: `ActionInstanceState GetStateFor(SpecialAction action, int device)` を提供。
- `ActionInstanceState` に以下を保持:
  - `bool PressedOnce`
  - `long LastToggleTimeUtcTicks`
  - `bool FirstTouch`
  - `bool ActionDone` — 旧 `Mapping.actionDone[index].dev[device]` 相当
  - `int UntriggerIndex` — 期待されるアンターゲットインデックス（-1 で無効）
  - `uint OneShotFlags` — GyroCalibrate などのワンショット状態用ビットマスク

2) `DeviceRuntimeState.cs`
- `SpecialAction UntriggerAction`
- `int UntriggerIndex`
- `bool[] MacroControl`, `uint MacroCount`
- Lightbar / fade 状態 (`FadeTimer`, `LastColor`, `ForceLight`)

3) `ActionRegistry.cs`
- `Initialize(IEnumerable<SpecialAction> source)`
- `SpecialAction GetByIndex(int index)`
- `int Count { get; }`
- `string SnapshotSummary()` — デバッグ用の簡易要約

4) `MacroManager.cs`
- `Task PlayMacro(int device, string macroStr, List<int> macroLst, int[] macroArr, string triggerKey, bool synchronized, SpecialAction action = null)`
  - 同期フラグがある場合は `triggerKey` 毎に FIFO 実行を保証。
  - 非同期の場合は fire-and-forget。

5) `ToggleController.cs`
- `bool ShouldFlipToggle(long lastToggleTimeUtcTicks, long nowUtcTicks, int debounceMs)`
- `void ApplyToggle(ref bool toggleState, out long newLastToggleTimeUtcTicks)`

6) `ReleasePolicy.cs`
- `bool EvaluateUntrigger(bool[] triggerStates, bool automaticUntrigger)`
  - 自動アンターゲットかどうかで評価方式を切替。

移行の方針（段階的）
1. 読み取りラッパを追加し、現行ロジックがすぐ壊れないようにする（既存の `GetActionDone` / `SetActionDone` のような補助）。
2. `ActionInstanceState` と `DeviceRuntimeState` に該当するデータを移す。まずは読み書きを移行してテスト。
3. `MacroManager` に `Mapping.PlayMacro` の同期ロジックを移植。
4. `ToggleController`/`ReleasePolicy` による一元化。
5. 全参照の置換・不要なグローバルの削除。

テスト観点
- Key Toggle: 物理押下→物理解除、連打、デバウンス時間内の反応確認。
- Macro 同期: 同一 trigger のマクロが FIFO で実行されること。
- Untrigger（プロファイルアンロード）: `untriggeraction` フローの一度きり実行と復帰確認。

備考
- 既存の `Mapping` 内ロジックは段階的に移す。まずは API とスケルトン実装を追加し、小さな置換を行ってビルド・動作確認を繰り返す。

---

次: この仕様に基づき、`Mapping` 内の残り `actionDone[...]` 参照を `ActionInstanceState.ActionDone` に置換するパッチを小分けに進めますか？（推奨: 1ファイル単位で置換→ビルド）
