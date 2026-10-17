# Phase6-Step10-3d 計画書: スペシャルアクションの「ボタン」種別の Toggle の実行時の実装

作成日: 2026-10-15
親計画: `Phase6-Step10-3c-Plan.md` §7〜8（案 A。Step10-4・10-5 の後に実施。ユーザー指示: KBM の Toggle と共通化し、構造を複雑にしない）
前提: Step10-4・10-5 完了。ボタン種別の SwitchMode（Press／Toggle）の保存・読込・表示は、Step10-3c で是正済み。

---

## 1. 目的
- ボタン種別（`Button`）のスペシャルアクションを、Switch Mode＝Toggle で保存した場合に、実行時も Toggle として動作させる（1 回押すと押しっぱなし、もう 1 回押すと解除）。
- 現状は、Toggle で保存しても Press としてしか動作しない（`Phase6-Step10-3c-Plan.md` §7）。

## 2. 現行コードの確認（2026-10-15）
- **出力の決定（Stage3）**: `Mapping.cs` の「Stage3: Synchronous Button SpecialAction handling」（3212 行付近）が、プロファイルの各 `Button` 種別のアクションについて、トリガーが成立している間だけ、出力（ボタン／マウスのカウンター／タッチパッドクリック）を設定する。直前のトリガー状態は、既存の `lastButtonTriggerState`（`device:actionname` → bool）に保持している。
- **二重の経路（MapCustomAction）**: `Mapping.cs` の `MapCustomAction` の `Button` 種別の分岐（5295 行付近）も、トリガー成立中に出力を設定する。通常のゲームパッドのボタンは、`TryDispatchSATriggerEstablished` → `DispatchInputEdge`（`Action` 実装は無く false）→ `GetOrCreateKeyButtonControllerForAction` の `KeyButtonActionController`（Toggle モードの場合は `ToggleImpl` が `ActionManager.SetToggledOn` で `IsToggledOn` を反転する）を呼ぶ。
- **KBM の Toggle との関係**: キー種別の Toggle は、`ActionManager`（`GetStateFor`／`SetToggledOn`／`IsToggledOn`）と `KeyAction`／`ToggleActionController` が担う。ボタン種別には `Action` の実装が無いため、Toggle の状態を出力へ反映する場所が無い。
- **SwitchMode の判定**: `SpecialAction.KeyButtonSwitchMode`（`Toggle`／`Press`）。ボタン種別でも、`SpecialAction` のコンストラクターが設定する（Step10-3c で保存・読込を是正済み）。
- **Fresh-press ガード**（Step10-4-2e）: `ActionInstanceState.RequiresFreshPressAfterReset` は、プロファイル適用・接続の直後の押しっぱなしを新規成立と見なさない。解除の判定（トリガーが 80ms 連続して未成立）は、`MapCustomAction` のループの冒頭で、全アクションに対して行われる。

## 3. 方針（案）
- **W1（出力の決定を Stage3 の 1 か所にする）**
  - Stage3 で、Toggle モードのボタン種別について、「トリガーの立ち上がり（`triggered && !prevState`）で `IsToggledOn` を反転し、出力は `IsToggledOn` の間だけ設定する」。Press モードは従来どおり（トリガーが成立している間）。
  - 状態の保持は、キー種別の Toggle と同じ `ActionManager` の `IsToggledOn`（`SetToggledOn`）を使う。新しい状態の辞書は作らない（`lastButtonTriggerState` は既存）。
  - `MapCustomAction` の `Button` の分岐（5295 行付近）は、Toggle モードのときは出力を設定せず、`actionFound = true` のみとする（Stage3 に出力を一本化する。`KeyButtonActionController` の `ToggleImpl` による二重の反転を防ぐ）。Press モードは従来どおり。
- **W2（判定の部品化とテスト）**: 立ち上がりでの反転と、出力の決定を、新規ファイル（`DS4Windows/DS4Control/ButtonSpecialActionOutput.cs`。1 ファイル 1 型）の静的メソッドにまとめ、ホストと `ActionManager` を使った単体テストで、反転・保持・ガードを検証する（`Mapping.cs` のホットパスには、このメソッドの呼び出しを足すだけにする）。
- **W3（Fresh-press ガード）**: 立ち上がりでの反転は、`RequiresFreshPressAfterReset` が true の間は行わない（プロファイル適用・接続の直後の押しっぱなしで、勝手に ON にならない。10-4-2e と同じ考え方）。
- **解除（状態のクリア）**: プロファイルの適用・コントローラーの切断で `ActionManager` の状態が作り直される既存の流れにより、`IsToggledOn` は false に戻る。
- **ホットパスの負荷**: Toggle モードのボタン種別のみ、毎ティック `ActionManager.GetStateFor`（辞書の参照。割り当てなし）を呼ぶ。Press モードの経路は変えない。既存の割り当てテストで確認する。

## 4. テスト・実機確認
- **単体テスト**: 立ち上がりで反転する、押しっぱなしでは反転しない、離して再度押すと解除される、ガード中は反転しない、Press モードは従来どおり。
- **ソース走査**: `MapCustomAction` の `Button` の分岐が、Toggle モードで出力を設定しないこと。
- **実機確認**: Toggle で保存したボタンのアクションで、1 回押すと押しっぱなし、もう 1 回押すと解除されること（GamePadTester など）。Press のアクションと、キー種別の Toggle のアクションに影響がないこと。プロファイルを切り替えると、解除されること。

## 5. 決定事項（要承認）
- **W1**: 出力の決定を Stage3 に一本化し、`MapCustomAction` の `Button` の分岐を Toggle のときは出力しない（A）／両方の経路で Toggle を実装する（B）。
- **W2**: 判定を新規ファイル（1 ファイル 1 型）の静的メソッドにする（A）／`Mapping.cs` に直接書く（B）。
- **W3**: Fresh-press ガードを、Toggle の立ち上がりにも適用する（A）／適用しない（B）。

## 6. 進捗
- 計画書の作成（2026-10-15）。承認待ち。

## 7. 方針の見直し（2026-10-15、ユーザーの提案：Press／Toggle を Stage3 へ全て統合できないか）
- **確認した現行コード（`Mapping.cs`）**
  - `MapCustomAction` の `Button` の分岐（5295 行付近）は、通常のゲームパッドのボタンについては、`TryDispatchSATriggerEstablished` を呼ぶだけで、**出力を設定しない**（フォールバックは「コントローラーが無い場合も実行しない」実装。例外時のみ直接設定）。出力の実体は Stage3 にある。タッチパッドクリック・マウス系の出力は、Stage3 と分岐の両方で設定している（カウンターが二重に加算される）。
  - 分岐が呼ぶ `KeyButtonActionController`（Press／Toggle の実装）は、**キーボードのキー操作（`SyntheticDispatcher.SendPress`／`SendRelease`）を前提にした実装**で、ボタン種別では、Xbox 360 のボタン番号をキーコードとして扱う形になる。ボタン種別の出力には寄与しておらず、意図しないキー入力を送る可能性がある（実機でのログ確認が必要。未確認）。
- **結論**: ボタン種別（`Button`）は、Press・Toggle とも、**Stage3 に全て統合できる**。`MapCustomAction` の `Button` の分岐は、`actionFound = true` のみ（出力・ディスパッチをしない）にする。
- **キー種別（`Key`。キーボードのキーを押すアクション）は、統合の対象外とする**: 出力先が、ゲームパッドの出力（Stage3 が組み立てる `outputfieldMapping`）ではなく、仮想キーボード（`IVirtualKBM` のキーの押下・解放、リピート）であり、状態管理も `KeyAction`／`KeyButtonActionController` が担う。Stage3 へ移すと、出力経路の再設計（Phase8 の「統合トリガーディスパッチ」の範囲）になる。
- **W1（改訂）**
  - A（推奨）: ボタン種別の Press・Toggle を Stage3 に一本化する。Toggle は、立ち上がりで `ActionManager` の `IsToggledOn` を反転し、ON の間だけ出力する。`MapCustomAction` の `Button` の分岐は、出力もディスパッチもしない。キー種別は変更しない。
  - B: 当初案（Toggle のときだけ、分岐の出力を止める）。
  - C: キー種別も含めて Stage3 へ統合する（Phase8 の範囲。本ステップでは扱わない）。
- **副作用（A）**: ボタン種別の Press で、`KeyButtonActionController` が送っていた可能性のある、意図しないキー入力が無くなる（要実機確認）。タッチパッド・マウス系の二重の加算が無くなる。

## 8. 決定と実装の記録（2026-10-15）
- **決定**: W1＝A（改訂。ボタン種別の Press・Toggle を Stage3 に一本化。キー種別は対象外）、W2＝A（新規ファイル）、W3＝A（Fresh-press ガードを適用）。キー種別の統合（案 C）は、Phase8 の計画として別に扱う（`Phase8-Unified-Trigger-Dispatch-Plan.md` に追記）。
- **変更**
  - 新規 `DS4Windows/DS4Control/ButtonSpecialActionOutput.cs`（`ResolveOutputActive`）: Press＝トリガー成立中のみ ON。Toggle＝立ち上がりで `ActionManager` の `IsToggledOn` を反転し（ガード中は反転しない）、ON の間は出力。
  - `Mapping.cs` の Stage3: 出力の条件を `triggered` から `outputActive`（上記の結果）へ変更。
  - `Mapping.cs` の `MapCustomAction` の `Button` の分岐: 出力もディスパッチもしない（`actionFound = true` のみ）。
- **テスト**: `ButtonSpecialActionOutputTests`（Press、Toggle の反転・保持・解除、ガード、スロットごとの状態、`Mapping.cs` のソース走査）。
- **実機確認項目**
  - Toggle で保存したボタンのアクションで、1 回押すと押しっぱなし、離しても保持、もう 1 回押すと解除（GamePadTester など）。
  - Press のボタンのアクションが、従来どおり動くこと（押している間だけ）。
  - 意図しないキー入力が発生しないこと（メモ帳を開いて、トリガーを押しても文字が入力されない）。
  - タッチパッドクリック・マウス系（左クリックなど）を割り当てたボタンのアクションが、従来どおり動くこと。
  - キー種別のアクション（Press／Toggle）と、プロファイル切替アクションに影響がないこと。
  - プロファイルを切り替えると、Toggle が解除されること。
