# MouseOutputAction 実装設計書

## 1. 現状と課題
- `MouseOutputAction.cs` は `IOutputAction` を実装しているものの、実処理がログ出力のみのスタブとなっている。
- 信号出力層（第3-b層）の `IVirtualKBM` への完全移行を完遂するため、スタブを解消し実体コードを実装する。

## 2. SpecialAction におけるマウス操作の仕様
`SpecialAction` で定義されるマウス関連の操作種別:
1. **MouseButton (ボタン押下/解放)**:
   - 対象ボタン: Left (1), Right (2), Middle (4), 4th (8), 5th (16)
   - `Execute`: `IVirtualKBM.PerformMouseButtonPress((uint)button)`
   - `Stop`: `IVirtualKBM.PerformMouseButtonRelease((uint)button)`
2. **MouseWheel (スクロール)**:
   - 垂直/水平スクロール量に応じた `IVirtualKBM.PerformMouseWheelEvent(v, h)`

## 3. 実装方針
- `MouseOutputAction` の `Execute` / `Stop` に上記ロジックを実装。
- DI から注入された `IVirtualKBM`（または `ctx.OutputHandler`）を経由して安全に実行。
- 単体テスト（`VirtualKBMTests.cs`）にモック呼び出しのアサーションを追加。
