# トリガー検出 → 信号送信 フロー要約

## 概要
DS4Windows における「物理入力の検出」から「OS へキー／マウスイベントを送信」するまでの主要経路を整理した要約。

## 主要コンポーネント
- Mapping（`Mapping.cs`）: 入力評価／マッピングの中核。SpecialAction 判定や通常マッピングを行う。
- ActionManager / Actions フレームワーク: SpecialAction を Action 実装へディスパッチする層。
- KeyButtonActionController（`KeyButtonActionController.cs`）: Press / Toggle モードを扱うコントローラ（`PressImpl` / `ToggleImpl`）。
- KeyButtonActionControllerAdapter（`Actions/KeyButtonActionControllerAdapter.cs`）: Actions API と既存コントローラの橋渡し。
- SyntheticDispatcher（`SyntheticDispatcher.cs`）: 論理イベントを実際の送出呼び出しへ変換し、送出時刻・カウント等の状態を更新する仲介層。
- VirtualKBMBase 実装（例: `SendInputHandler.cs`）: OS へ実際に送るハンドラ（`SendInput` 等）。
- RepeatHelper: 繰り返し送出を行う補助コンポーネント。

## 高レベルフロー
1. Input（物理入力）
2. `Mapping.ProcessControlSettingAction` が入力を評価
   - SpecialAction 判定（`CheckForSpecialActionSuppression`）
   - 通常マッピング / SpecialAction / Macro に振り分け

### A: SpecialAction 経路
- `TryDispatchSATriggerEstablished` / `TryDispatchSATriggerReleased` が起点
- まず `ActionManager.DispatchInputEdge` を試みる（Action フレームワーク）
  - Action 実装がコントローラへ委譲 -> `IActionController`（例: `KeyButtonActionControllerAdapter`）
  - Adapter → `KeyButtonActionController.OnSATriggerEstablished/Released` を呼ぶ
- あるいは直接 `GetOrCreateKeyButtonControllerForAction` で `KeyButtonActionController` を取得
- `KeyButtonActionController`（`PressImpl`/`ToggleImpl`）は必要に応じて `RepeatHelper` を生成
- いずれのケースも最終的に `SyntheticDispatcher.SendPress/SendRelease(...)` に合流して送出される

### B: Macro 経路
- `PlayMacro` 等がマクロシーケンスを解釈し、逐次 `SyntheticDispatcher` を直接呼んで送出するケースがある

### C: 通常マッピング経路（見落としになりやすい重要経路）
- `Mapping` は多くの通常マッピングを直接 `outputKBMHandler`（`VirtualKBMBase` 実装）へ呼び出す
  - 例: `outputKBMHandler.PerformKeyPress(nativeKey)` / `PerformKeyPressAlt(nativeKey)`、マウス移動、ボタンイベント等
- この経路は `SyntheticDispatcher` を経由しないため、SpecialAction 経路と実装差が出やすい

## ScanCode（`useScanCode`）の扱いに関する要点
- `SyntheticDispatcher` は `useScanCode` により次を選ぶ:
  - true → `PerformKeyPressAlt` / `PerformKeyReleaseAlt`（スキャンコード経路／`KEYEVENTF_SCANCODE`）
  - false → `PerformKeyPress` / `PerformKeyRelease`（VK 経路）
- 重要: `KeyButtonActionControllerAdapter` の現在実装は `inner.OnSATriggerEstablished(..., false, ...)` のように `useScanCode` を `false` 固定で渡している箇所があり、Actions 経路では ScanCode が無視される場合がある。
- 一方、通常マッピングは `Mapping` 内で直接 `PerformKeyPressAlt` を呼ぶ場合があり、`useScanCode` を明示的に使っている箇所も存在する（経路によって挙動が分岐する）。

## 合流と状態管理
- `SyntheticDispatcher` は送出時に `Mapping.deviceState` / `Mapping.global_state` のタイミング・カウント（`lastSyntheticSendUtcTicks`, `vkCount`, `scanCodeCount`, `repeatCount` 等）を更新する。
- 通常マッピングと SpecialAction/Macro の双方が状態に影響を与えうるため、重複送出の抑止や優先判定は `Mapping` ロジックに依存する。

## 代表的な該当ファイル（参照）
- `DS4Windows/DS4Control/Mapping.cs`
- `DS4Windows/DS4Control/SyntheticDispatcher.cs`
- `DS4Windows/DS4Control/KeyButtonActionController.cs`
- `DS4Windows/Actions/KeyButtonActionControllerAdapter.cs`
- `DS4Windows/DS4Control/OutputKBM/SendInputHandler.cs`
- `DS4Windows/DS4Control/RepeatHelper.cs`

## 次ステップ候補
- `KeyButtonActionControllerAdapter` の `useScanCode` を `trigger` の値で渡す修正（Actions 経路で ScanCode を尊重）
- `Mapping` 側の通常マッピング経路を抜粋して、どの条件で `PerformKeyPressAlt` を呼ぶかを明示化
- ランタイムでの挙動確認（ログを使って ScanCode/VK 経路の実際の送出を比較）

---
ファイルを作成しました: `docs/TriggerToSendFlow.md` 。次にどれを行いますか？（例: Adapter 修正パッチ作成 / 詳細シーケンス図出力 / 実際のログ比較）
