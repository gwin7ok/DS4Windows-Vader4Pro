# [作業計画書] Controlタブ キーリピート & トグルキー不具合 修正計画

## 1. 課題と現状のメカニズム
- **通常キー（Toggle OFF）**: 長押し時にリピート信号が送信されない。
- **トグルキー（Toggle ON）**: ボタンを押してもトグル（長押し状態）にならない。
- **原因**: Controlタブの設定が `SpecialAction` 化されておらず、`ActionManager` / `KeyButtonActionController`（`RepeatHelper` / `ToggleController`）の共通実行機構を通過していなかった。

## 2. 目標設計
- Controlタブのキー設定を `(device, kvpKey)` 単位の「合成SpecialAction」としてキャッシュ生成し、既存の `ActionManager` に合流させる。
- 出力層ロジックは変更せず、2層（変換層）での橋渡しのみで完結させる。

## 3. テスト計画 (dotnet test)
- `DS4WindowsTests/ControlTabAndSpecialActionKeyTests.cs`:
  1. SpecialActionsタブの Press モードでのキー押下・解放動作検証
  2. SpecialActionsタブの Toggle モードでの 1回押し保持・2回押し解除動作検証
  3. Controlタブ用の合成 SpecialAction 生成（Press/Toggle、ScanCode/VirtualKey）およびキャッシュ再利用の検証

## 4. 完了条件
- [x] 合成 SpecialAction 生成ヘルパー（`GetOrCreateSyntheticKeyAction`）の実装
- [x] 単体テストコード（`ControlTabAndSpecialActionKeyTests.cs`）の作成
- [ ] `dotnet build` および `dotnet test` の全件成功
