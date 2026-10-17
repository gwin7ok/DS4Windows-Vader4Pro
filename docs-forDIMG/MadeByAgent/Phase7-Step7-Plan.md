# Phase7-Step7 計画書: `Mapping` の特性化テストと、静的な状態の切り出し

作成日: 2026-10-17  
対象ブランチ: `For-DI-migration-work`  
上位計画: `Phase7-Plan.md` §2（Step7〜9: `Mapping` の instance 化の第 1 段）、`Phase7-Residual-Ledger.md`  
状態: 実装完了（Q1〜Q6 はすべて推奨どおり承認。W1〜W4 のビルド・テスト成功、W5 は文書。実機確認 U-17 は未）。結果は `Phase7-Step7-Completion-Report.md`。

---

## 1. 目的と位置づけ

- Step7〜9 は、`Mapping`（8,192 行）の instance 化である。ホットパス（毎レポート実行）、割り当てゼロ、マクロの別スレッド、確認不能の U-2・U-5・U-6 が同じ経路にかかるため、Phase7 で最大のリスクを持つ。
- Step7 は、その**安全網づくり**と**下ごしらえ**である。
  - 入力 → 出力の特性化テストを整備する（現状の挙動を固定してから、構造を変える）。
  - 外部から触られている静的な状態を、`Mapping` の外の保持クラスへ切り出す。
  - 下位層からの逆参照（`BackingStore` → `Mapping`）をなくす。
- `Mapping` 自体の instance 化と呼び出し元の切り替えは Step8、互換 `AppHost` の削除と定数の再配置は Step9。Step7 では `Mapping` のメソッドの本体（計算のロジック）に触れない。

## 2. 調査結果

### 2.1 `Mapping` の規模と構造

- `Mapping.cs`: 8,192 行。`public class Mapping`（static クラスではない）だが、メンバーのほぼすべてが static。
- 静的なフィールド: 約 60 個。大きく 5 群に分かれる。
  - 合成入力の状態: `globalState`、`deviceState`（8）、`syncStateLock`、`prevInputEstablished`、`keyButtonControllers`、`_syntheticActionCache`、`customMapQueue`、`lastButtonTriggerState`、`held` ほか。
  - マクロの状態: `macrodone`、`macroDispatchInFlight`／`macroDispatchLock`（暫定対策。K3 に関係）、`macroControl`、`macroCount`、`macroTaskQueue`、`threadCts`、`lightbarMacroTask`、`altTab*` ほか。
  - スティック・ジャイロ・トリガーの処理状態: `stickValueHistory`、`outSqrStk`、`gyroStickX`／`gyroStickY`、`mapStickActionData`、`lastStickAxisValues`、`lastWheelGyroValues`、`wheelFilters`、`flickMappingData`、`l2TwoStageMappingData`／`r2TwoStageMappingData`、`deltaAccelProcessors`、`previousPointer*`、`accel*`、`horizontalRemainder` ほか。
  - デバイスごとの実行時状態: `fieldMappings`／`outputFieldMappings`／`previousFieldMappings`（8）、`extrasRumbleActive`、`fadetimer`／`prevFadetimer`、`lastColor`、`deviceRuntime`、`suppressedTriggerButtons`、`nowAction` ほか。
  - サービスへの束縛: `profileSettings`、`profileApplication`、`VirtualKBM`（`AppHost`／`Global` のフォールバックつき。Step8 の対象）。
- 入れ子の型: `SyntheticState`（と `MouseClick`／`KeyPress`／`KeyPresses`）、`DS4SquareStick`、`DS4TimedStickAxisValue`、`PostMapStickData`、`LastWheelGyroCoord`、`FlickStickMappingData`、`TwoStageTriggerMappingData`、`DeltaSettingsProcessor`（約 200 行）、`DeltaSettingsProcessorGroup`、`Click`。
- Service Locator: `AppHost.GetService` 6、`ServiceProviderHolder` 5（Step8 の対象）。`Global.*` 52（ほぼ `MAX_DS4_CONTROLLER_COUNT` などの定数。Step9 の対象）。

### 2.2 外部の利用者（`Mapping.<名前>` の静的参照）

- `ControlService`: `Commit`、`MapCustom`、`SetCurveAndDeadzone`、`TempMouseJoystick`、`HandleDeviceDisconnect`、`getBool*Mapping`（約 30 件）、`flickMappingData`、`deltaAccelProcessors`、`wheelFilters`、`l2/r2TwoStageMappingData`。
- `Mouse`: `Click`、`MapClick`、`gyroStickX`／`gyroStickY`、`mapStickActionData`／`PostMapStickData`。
- `SyntheticDispatcher`: `deviceState`、`globalState`、`SyntheticState`。
- `MouseCursor`、`MouseWheel`: `remainderCutoff`。
- `Actions/*`: `HandleDeviceDisconnect`、`GetOrCreateKeyButtonControllerForAction`、`PlayMacroDirect`／`EndMacroDirect`（Phase8 の対象。Step7 は触れない）。
- そのほか: `ProfileApplicationService`（`ClearKeyButtonControllersForDevice`）、`Global`（`ResetRuntimeStateForKey`）、`ControllerReadingsControl`（`SetCurveAndDeadzone`）、`RecordBoxViewModel`（`GetBoolMappingExternal`）、`ProfileSettingsViewModel`（`MOUSESPEEDFACTOR`）。

### 2.3 下位層から上位層への逆参照

- `BackingStore.LoadActions`（`BackingStore.cs:5063-5139`）が、`Actions.xml` の読み込み後に `Mapping.globalState`／`deviceState`／`macrodone` を新品へ戻している（同じブロックが 2 箇所）。ログ `Runtime synthetic state reset after Actions load` と `ActionManager.ClearAllEntries`／`ClearAllToggledOn` も同じブロックにある。
- `LoadActions` は、`SpecialActionRepository`、`App`、`Global`（3 箇所）、`BackingStore.LoadProfiles` ほかから呼ばれる。つまり、リセットを呼び出し元へ移すと、経路ごとに挙動が変わる。**`BackingStore` が通知（イベント）を出し、購読者がリセットする**形にすれば、全経路で挙動を保てる（Q4）。
- このリセットは、DI 移行中に加えられたものである（上流にはない）。挙動は維持する（機能削減はしない）。

### 2.4 既存のテスト（安全網の現状）

- 固定されているもの: `SetCurveAndDeadzone`（引数の通し 13 本、ゼロアロケーション 2 本）、`ApplyStickCalibration`（2 本＋割り当て 1 本）、`CheckForSpecialActionSuppression`、`IsUsingGyroForControls`、`PlayMacro` の二重実行防止、ボタンの SpecialAction の出力、`LogActionDoneCountOnTrigger`、`Global` 参照の不在を見る文字列ガード（多数）。
- **固定されていないもの（最大の穴）**: `MapCustom`（通常入力の評価の本体）、`Commit`（合成入力の出力）、`getBool*Mapping` 群、`MapClick`、`HandleDeviceDisconnect`。いずれも、入力の状態を与えて出力の状態を確かめるテストがない。
- `MapCustom` は、静的な `profileSettings`（`AppHost` ／ `Global.ProfileSettingsServiceInstance` のフォールバック）を使い、`ControlService`（`ProfileActionProvider` ほか）を必要とする。テストは、既存のテスト基盤（`TestRepositoryFactory`、`MappingLogActionDoneCountOnTriggerTests` の実 `ControlService`）で組める見込み。

### 2.5 テストが届かないもの（実機でしか確認できない）

- ジャイロのマウス・スティック・ハンドル、フリックスティック、タッチパッド、タッチのスティック。複数台（スロット 5〜8＝U-2）。マクロの実行の実時間（U-5、U-6）。これらは、構造を変えない範囲（状態の置き場所の移動）に限定して扱い、実機確認（U-17）で補う。

---

## 3. 決定が必要な事項

### Q1: 特性化テストの範囲

- A（推奨）: 通常入力の評価を固定する。
  - `MapCustom`: ボタン→ボタンの再割り当て、スティックの反転・軸の割り当て、トリガー、ボタン→キー（合成状態のカウント）、ボタン→マウスボタン。
  - `Commit`（偽の `IVirtualKBM` を `Global.outputKBMHandler` へ差して出力を記録。終了時に戻す）、`getBool*Mapping`、`MapClick`、`HandleDeviceDisconnect`（保持中のキーの解放）。
  - マクロ、ジャイロ、フリックスティック、タッチは範囲外（既存の二重実行防止のガードと、実機確認に任せる）。
- B: A に加えて、ジャイロ・フリックスティックの計算を、合成データで固定する（計算が複雑で、期待値の設計が難しい。得られる安全は大きいが、Wave が大きくなる）。
- C: 特性化テストを作らない（推奨しない。Step8 で呼び出し元とホットパスを触るため、安全網がなくなる）。

### Q2: 切り出す状態の保持クラスの構成

- A（推奨）: 責務ごとに 3 つの保持クラスを、1 型 1 ファイルで新設する。外部から触られる状態だけを移し、`Mapping` の内部だけで使う状態（`fieldMappings` など）は、Step8 で instance フィールドになるまで残す。
  - `SyntheticInputState`: `globalState`、`deviceState`、`syncStateLock`（`SyntheticDispatcher`、`BackingStore` が使う）。
  - `MacroRuntimeState`: `macrodone` と、マクロの実行に関する状態（`macroDispatchInFlight`／`macroDispatchLock`、`macroTaskQueue` ほか。K3 の再設計は Step9）。
  - `StickMotionState`: `gyroStickX`／`gyroStickY`、`mapStickActionData`、`flickMappingData`、`l2/r2TwoStageMappingData`、`wheelFilters`、`deltaAccelProcessors`（`Mouse`、`ControlService` が使う）。
- B: 1 つの大きな `MappingRuntimeState` に全部入れる（型は少ないが、責務が混ざる。Step8 の分割が難しくなる）。
- C: 切り出さず、逆参照の解消だけを行う。

### Q3: 保持クラスの渡し方（Step8 までの暫定）

- A（推奨）: 保持クラスは instance のクラスとする。`Mapping`、`Mouse`、`SyntheticDispatcher` のような静的／コンテナ外のコードは、暫定の静的な窓口（各クラスの `Shared`）から同じ instance を得る。DI にも同じ instance を Singleton として登録し、`ControlService` などコンテナ内のクラスは注入で受ける。Step8 で `Mapping` が instance 化されたら、`Shared` を削除する（TODO を明記）。ホットパスでは、`Mapping` が型の初期化時に参照を 1 度だけ保持し、毎回の間接参照のコストを増やさない。
- B: 静的クラス（`static class`）にする（Step8 でもう一度書き直しになる）。

### Q4: `BackingStore` → `Mapping` の逆参照の解消

- A（推奨）: `BackingStore` に `ActionsLoaded` イベントを追加し、`LoadActions` の末尾で発行する（現在のリセットのブロックを置き換える。`ActionManager.ClearAllEntries` は Phase8 まで `BackingStore` に残す）。購読者は `ServiceRegistration` の `BackingStore` の登録で付ける。購読者は、ログ（`Runtime synthetic state reset after Actions load`）を含め、現在と同じリセットを、保持クラスに対して行う。コンテナ構築前の読み込みでは購読者がいないが、その時点では状態は新品であり、結果は同じ。
- B: `LoadActions` の各呼び出し元でリセットする（経路が 5 つあり、漏れると挙動が変わる）。
- C: 逆参照を残す（Step8 で `Mapping` が instance 化されると、`BackingStore` が参照できなくなる）。

### Q5: 入れ子の型のトップレベル化

- A（推奨）: 外部から使われる入れ子の型（`SyntheticState`、`Click`、`PostMapStickData`、`FlickStickMappingData`、`TwoStageTriggerMappingData`、`DeltaSettingsProcessor`、`DeltaSettingsProcessorGroup`）を、1 ファイル 1 型としてトップレベルへ出す（名前空間は `DS4Windows` のまま）。参照 `Mapping.X` は `X` に変わる。型の中身は変えない。`Mapping` の内部だけで使う入れ子の型（`DS4SquareStick`、`LastWheelGyroCoord` ほか）は残す。
- B: 入れ子のまま残す（保持クラスが `Mapping.X` を参照することになり、Step8 で `Mapping` を instance 化するときに型の参照が循環する）。

### Q6: `outputKBMHandler`／`outputKBMMapping` の担当 Step（Step6 Q4 の持ち越し）

- A（推奨）: Step8 で扱う。`Mapping` の `VirtualKBM` の束縛と、`SyntheticDispatcher` の静的な参照は、`Mapping` を instance 化して注入を受けるときにまとめて解消する。Step7 では触れない（`Mapping` と `SyntheticDispatcher` の両方に関わり、状態の切り出しと混ぜるとリスクが上がる）。
- B: Step7 で、`OutputKBMHandlerLifecycle` へ実体を移す。

---

## 4. 実装の Wave（推奨案の場合）

- W1: 特性化テスト（Q1＝A）。本体のコードは変えない。ここまでのテストが、現在の `Mapping` で成功することを確認してから先へ進む（ビルド・テストはユーザーが実行）。
- W2: `SyntheticInputState` とその入れ子の型（`SyntheticState`）の切り出し。`BackingStore` の逆参照を `ActionsLoaded` イベントに置き換え（Q4）。`SyntheticDispatcher` を更新。
- W3: `MacroRuntimeState` の切り出し（`macrodone` ほか）。
- W4: `StickMotionState` と入れ子の型（`PostMapStickData`、`FlickStickMappingData`、`TwoStageTriggerMappingData`、`DeltaSettingsProcessor`／`Group`、`Click`）の切り出し。`Mouse`、`ControlService` を更新。
- W5: ガードテスト（`Mapping` が移した状態を持たない、`BackingStore` が `Mapping` を参照しない、`Shared` が残る範囲を固定）、ラチェットの更新、台帳、チェックリスト U-17、モデル図、完了報告書。

各 Wave で、ゼロアロケーションのテスト（`MappingHotPathAllocationTests`）と、W1 の特性化テストが成功することを確認する。

## 5. リスクと確認

- ホットパス: 保持クラスの導入で、毎レポートの割り当てを増やさないこと。`Mapping` が型の初期化時に保持クラスの参照を静的な readonly に保持し、配列の参照を再代入しない。
- スレッド: 配列の要素の読み書きにロックはない（従来どおり）。デバイスごとに別スレッドで入力が処理されるが、配列は「デバイスの添字」ごとに分かれており、共有されるのは `globalState` と一部のマクロの状態だけ。構造の変更で、共有の範囲を広げない。
- `BackingStore.LoadActions` のリセット（Q4）: 全経路で同じ結果になること。ガードテストで、購読の登録とログ文を固定する。
- 型の初期化の順序: `Mapping` の静的コンストラクターは `ActionManager` のイベントを購読している。保持クラスの `Shared` の初期化が、`Global`／`AppHost` を参照しない（サイクルを作らない）ことを保つ。
- 実機確認 U-17: 通常入力（ボタン・スティック・トリガーの再割り当て）、キーボード・マウスへの割り当て、ジャイロ・タッチ・フリックスティック、マクロ、プロファイル切り替え、Actions.xml の再読み込み、切断時のキーの解放。スロット 5〜8（U-2）は確認不能のまま。

## 6. 範囲外（持ち越し）

- Step8: `Mapping` の instance 化、`profileSettings`／`profileApplication`／`VirtualKBM` の束縛、`AppHost`（6）と `ServiceProviderHolder`（5）の解消、`outputKBMHandler`／`outputKBMMapping`（Q6）、呼び出し元の切り替え、`Shared` の削除。
- Step9: 互換 `AppHost` の削除、`Mapping.cs` の `Global` 定数参照（52）の再配置、K3（マクロの二重ガード）。
- Phase8: `Actions/` の `ServiceProviderHolder` 17、`ActionManager` の静的な呼び出し。
