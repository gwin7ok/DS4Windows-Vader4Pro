# OutputAction Feature Inventory

概要: `IOutputAction` の具体実装（`KeyOutputAction`, `MacroOutputAction`, `MouseOutputAction` 等）が満たすべき要素を、共通項目と各実装ごとに列挙し、該当する既存ソースファイルへの参照（ファイル＋代表行番号）を付記します。

## 共通（すべての OutputAction）
- `Id` プロパティ（一意識別子）
- `Execute(IOutputContext ctx)`：開始処理
- `Stop(IOutputContext ctx)`：停止／中断／後片付け
- `IOutputContext` 必須フィールド: `Device`, `OutputHandler` (VirtualKBMBase / IOutputHandler), `Meta`（拡張パラメータ）
- スレッド安全性（タイマー／IRepeater からの呼び出しに備える）
- 冪等性（連続 Execute / 重複 Stop に耐える）
- ロギング／エラーハンドリング
- テストフック（`IOutputHandler` の差替え）
- リピート（IRepeater と協調）
- Toggle/Press 状態の直接変更を行わない（single-writer は Controller/ActionManager）

参照: `DS4Windows/Actions/OutputContextImpl.cs` (class at line ~6)

## KeyOutputAction（キー送出）
- VK / スキャンコード両対応（`vk`, `scan`）、`extended` フラグ
- 修飾キー処理（Ctrl/Shift/Alt 等の Down/Up 管理、同時押し順序）
- 押下／解放のペア管理（`Execute` → Down, `Stop` → Up）
- Press / PressedOnce / Toggle モードでの振る舞い（状態管理は Controller 側）
- FakeKeyRepeat 整合（`SendInputHandler` 偽リピート設定に依存）
- 長押し／リピート対応（`IRepeater` による tick を受ける設計）
- スキャン ↔ VK 変換ロジック
- 実送出: `ctx.OutputHandler.PerformKeyPress` / `PerformKeyPressAlt` / `PerformKeyRelease` 等を呼ぶ
- テスト用メタ（期待値・タイミング検証フック）

主な参照実装・呼び出し箇所:
- `DS4Windows/Actions/KeyOutputAction.cs` (class at line 6)
- `DS4Windows/Actions/KeyActionBinding.cs` (binding at line 6)
- `DS4Windows/DS4Control/OutputKBM/SendInputHandler.cs` (PerformKeyPress implementations, class at line 26, methods ~L172/L195)
- `DS4Windows/DS4Control/VirtualKBMBase.cs` (abstract PerformKeyPress definitions ~L58-L59)
- Mapping 直接呼び出し箇所（旧経路）: `DS4Windows/DS4Control/Mapping.cs` (例: PerformKeyPress at lines ~1725, 1743, 5775, 5780)

## MacroOutputAction（マクロシーケンス）
- マクロステップ列の保持（press/wait/release/move など）
- 非同期実行（Execute が非同期で複数ステップを実行）
- 中断トークン／キャンセル（`Stop` で確実に中断）
- ステップ遅延（ms 単位の Wait/Delay）
- 再入制御（同時二重実行の禁止やキューリングポリシー）
- 進捗管理（現在ステップ、残り時間など）
- エラー発生時の安全解放／ロールバック
- マクロ固有 Meta（`macroId`, `steps`, `repeat` など）

参照（部分実装あり）:
- マクロ関連実装はプロジェクト内の Macro 系ファイル（`Extras` や `DS4Windows/Extras` 下）に存在する可能性があります。実ファイルを特定するにはさらに検索します（現状 `MacroOutputAction` クラス定義は未検出）。

## MouseOutputAction（マウス送出）
- ボタン Down/Up（Left/Right/Middle）
- 移動: 相対 / 絶対切替、座標変換、DPI 補正
- ホイール（垂直／水平）とステップ量
- クリック列（ダブルクリック等）やホールド処理
- カーソル制限／キャプチャ（必要時）
- マウス Meta（`x`,`y`,`delta`,`absolute` 等）
- 実送出は `ctx.OutputHandler` 経由（SendInput など）

参照: マウス関連の OutputAction クラス定義は未検出。SendInput ハンドラはマウス関連メソッドも実装している。

## `IOutputContext.Meta` 推奨スキーマ（標準化候補）
- `vk`: uint（仮想キー）
- `scan`: uint（スキャンコード）
- `modifiers`: string[] または bitmask
- `extended`: bool
- `duration`: ms / TimeSpan（長押し目安）
- `macroId`, `steps`
- `mouseAction`: enum（move/click/wheel）および `{x,y,delta,absolute}`
- `repeat`: bool または `{ initialDelay, interval }`
- `expectedBehavior`: テスト用期待値

## 既存ソース上の重要シンボル位置（代表行）
- `DS4Windows/Actions/KeyOutputAction.cs` — class KeyOutputAction (line 6)
- `DS4Windows/Actions/OutputContextImpl.cs` — class OutputContextImpl (line 6)
- `DS4Windows/Actions/KeyActionBinding.cs` — class KeyActionBinding (line 6)
- `DS4Windows/Actions/KeyButtonActionController.cs` — class KeyButtonActionController (line 12)
- `DS4Windows/Actions/KeyButtonActionControllerAdapter.cs` — class KeyButtonActionControllerAdapter (line 8)
- `DS4Windows/DS4Control/OutputKBM/SendInputHandler.cs` — class SendInputHandler (line 26), `PerformKeyPress` (~L172), `PerformKeyPressAlt` (~L195)
- `DS4Windows/DS4Control/VirtualKBMBase.cs` — abstract `PerformKeyPress` / `PerformKeyPressAlt` (~L58-L59)
- `DS4Windows/DS4Control/Mapping.cs` — TryDispatch helpers (lines ~4184, ~4270) および PerformKeyPress 呼び出し点（例: ~L1725, ~L1743, ~L5775, ~L5780）

---

必要な次対応:
-（希望であれば）`MacroOutputAction` / `MouseOutputAction` の実ファイルを完全に特定して行番号まで追記します（追加検索を行います）。
- `IOutputContext.Meta` の厳密スキーマを `docs/Actions-Interface-Spec.md` に追記する作業を行います。

作成日時: 2025-12-19
