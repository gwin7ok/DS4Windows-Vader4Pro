# [作業計画書] Controlタブ キーリピート & トグルキー不具合 修正計画

## 1. 課題と現状のメカニズム
- **通常キー（Toggle OFF）**: 長押し時にリピート信号が送信されない。
- **トグルキー（Toggle ON）**: ボタンを押してもトグル（長押し状態）にならない。
- **原因**: Controlタブの設定が `SpecialAction` 化されておらず、`ActionManager` / `KeyButtonActionController`（`RepeatHelper` / `ToggleController`）の共通実行機構を通過していなかった。

## 2. 目標設計
- Controlタブのキー設定を `(device, kvpKey)` 単位の「合成SpecialAction」としてキャッシュ生成し、既存の `ActionManager` に合流させる。
- 出力層ロジックは変更せず、2層（変換層）での橋渡しのみで完結させる。

## 3. 作業スコープ
- `DS4Windows/DS4Control/Mapping.cs`
- `docs-forDIMG/MadeByAgent/Bugfix-ControlTab-KeyRepeat-And-Toggle-Plan.md`
- `docs-forDIMG/MadeByAgent/Bugfix-ControlTab-KeyRepeat-And-Toggle-Report.md`

## 4. 完了条件
- [ ] 通常キー長押し時にキーリピートが機能すること。
- [ ] トグルキー設定時に1回押しで長押し保持、再押しで解放されること。
- [ ] `dotnet publish` および `dotnet test` が全件成功すること。
