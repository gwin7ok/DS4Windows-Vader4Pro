# [実装報告書] フェーズ2 Step 2-7: Controlタブ キー処理の ActionManager 完全一本化と安定化

## 1. 実施概要
Controlタブのキー設定（通常キー・トグル・リピート）において、SpecialActionsタブ（ルートA）とは別系統のレガシー処理（`SyntheticState` の遅延マージ・古いトグル計算）を通っていたことに起因する動作不安定（トグルのチャタリング・リピート不発）を解消するため、**ActionManager / KeyAction への完全一本化**を実施した。

1. **Controlタブ キー判定の ActionManager 直結 (`Mapping.ProcessControlSettingAction`)**:
   - `actionType == DS4ControlSettings.ActionType.Key` 判定時、レガシーな `deviceState.keyPresses` への格納を撤廃。
   - SpecialActionsタブと全く同一の `DispatchOrSetBeingTriggered`（`DispatchInputEdge` 経由）を直接呼び出す構成に変更。
   - 物理ボタンの立ち上がりエッジ（押下）および立ち下がりエッジ（解放）の検知が `DispatchInputEdge` で自動的かつ正確に行われるよう統一。

2. **`Mapping.Commit()` 内のレガシーキー合成処理の全廃**:
   - 以前の二重トグル管理や手動リピート（`fakeKeyRepeat`）、チャタリングの温床となっていた `keyPresses` ループ（約250行）を完全に削除。
   - `Commit()` はマウス入力の同期およびフレーム終了処理（`SaveToPrevious`, `VirtualKBM.Sync()`）のみを担当するクリーンな構造にスリム化。
   - 未使用となった警告フィールド（`keyshelddown`）を削除し、Warning 0件を達成。

3. **合成 SpecialAction 生成 (`GetOrCreateSyntheticKeyAction`) の最適化**:
   - キャッシュキーを `(device, kvpKey, toggle, useScan)` で分離し、Press用とToggle用のアクションが混ざらないよう分離。
   - `sa.keyType` に `DS4KeyType.Toggle` および `DS4KeyType.ScanCode` を正しく設定し、後続の `KeyAction` / `KeyButtonActionController` に正確なモードが伝達されるよう改修。

4. **単体テストコードの不整合修正 (`ControlTabAndSpecialActionKeyTests.cs`)**:
   - 111行目において、トグル用合成Actionのアサーションが `DS4KeyType.ScanCode`（トグルなし）となっていた不整合を `DS4KeyType.ScanCode | DS4KeyType.Toggle` に修正。

---

## 2. 成果物
- `DS4Windows/DS4Control/Mapping.cs`
- `DS4WindowsTests/ControlTabAndSpecialActionKeyTests.cs`
- `docs-forDIMG/MadeByAgent/Phase2-Status.md`
- `docs-forDIMG/MadeByAgent/Phase2-Step2-7-Report.md`

---

## 3. 検証結果
1. **ビルド検証**:
   - `dotnet publish` (Release / x64): **成功 (警告 0件, エラー 0件)**
2. **単体テスト検証**:
   - `dotnet test`: **全24件 PASS (成功 24, 失敗 0, スキップ 0)**
3. **実機動作検証**:
   - **通常キー**: 単押しでの1文字入力、長押しでの `RepeatHelper` による高精度リピート連打が正常動作。
   - **トグルキー**: 1回目の押下で確実なトグルON（入力ロック/連打維持）、2回目の押下で確実なトグルOFF（即時解放）が機能し、チャタリングが完全に解消されたことを確認。