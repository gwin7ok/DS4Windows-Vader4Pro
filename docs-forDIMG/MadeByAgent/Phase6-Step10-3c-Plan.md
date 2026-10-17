# Phase6-Step10-3c 計画書: スペシャルアクションの「キー押下」でコントローラーのボタンを割り当てられない件の調査

作成日: 2026-10-14  
状態: 調査待ち（未着手。計画案）  
位置づけ: Step10-3 の実機確認で判明した、DI 化の範囲外の機能の問題（`Phase6-Step10-3-Plan.md` §12）。DI 化の変更とは別のコミットで扱う  
準拠: `.github/copilot-instructions.md` §2.2、§2.4、§4

## 1. 現象
- スペシャルアクションの編集画面の「Press Key」で開くボタン割り当て画面（`SpecialActionEditor.PressKeySelectBtn_Click` → `BindingWindow.ExposeMode.ForPressToggle`）に、コントローラーの図は表示されるが、キーボードとマウスしか割り当てられない。コントローラーのボタン（Xbox 360／DS4 の表記）を割り当てられない。

## 2. 調査項目（案）
1. `git log -S"ForPressToggle"` で、このモードが追加された経緯と、当時の意図（コントローラーのボタンを割り当てられること）を確認する。
2. `BindingWindow` の `ForPressToggle` の分岐（`InitButtonBindings` と、116〜135 行付近のモード別の表示制御）が、実際に実行されているかを、ログと実機で確認する。
3. 置換前（Phase5 以前）から同じ現象だったか、リファクタで失われたかを判定する（§2.2 の機能維持に関わるため）。
4. `PressKeyViewModel` のボタン種別（`ActionType.Button`、`SaveAction` の `mode == 10`）の保存・読み込みが、一連の経路で動くかを確認する。

## 3. 判定後の進め方
- 置換前からの不具合、または機能の欠落であれば、是正の可否と方法を、選択肢と推奨を添えて確認する（承認後に実装）。
- 確認項目（割り当てたボタン名が出力種別に合った表記で表示されること）は、`Phase6-Step11-Plan.md` §3.3 の台帳のとおり。

## 4. 調査結果（2026-10-15、コードの読解のみ。実機での再現は未実施）
- **経緯（意図は「コントローラーのボタンを割り当てられること」）**:
  - 当初、`SpecialActionEditor` の「Press Key」は `BindingWindow.ExposeMode.Keyboard`（キーボードのみ）で開いていた。
  - `9ddba691`（2025-12-14、v3.11.7）で「SpecialAction: enable controller binding」として `ExposeMode.Full` に変更した。
  - `283f6add`（2025-12-15）で `ExposeMode.ForPressToggle` を追加し、コントローラー・キーボード・マウスの領域を表示しつつ、モード欄・マクロ記録・エクストラ欄を隠す形にした。
  - Phase6 の DI 化（Step10-2-4a）では、この呼び出し（`BindingWindow.ExposeMode.ForPressToggle`）を窓口経由に変えただけで、モードは変えていない。
- **コード上の経路（問題は見つからなかった）**:
  1. `PressKeyViewModel.PrepareSettings()` は、`DS4Controls.None` の設定（キー種別）を作る。
  2. `BindingWindow` は、`ForPressToggle` で `InitButtonBindings()` を実行し、Xbox 360／DS4 のボタン（`aBtn` など）に `OutputButtonBtn_Click` を結び付ける。
  3. ボタンのクリックで、`binding.outputType = Button`、`binding.control = <ボタン>` を設定し、ウィンドウを閉じる。
  4. `Window_Closing` の `WriteBinds()` が、`settings.actionType = Button`、`settings.action.actionBtn` を書き込む。
  5. `PressKeyViewModel.ReadSettings()` が `ActionType.Button` を読み取り、`lastActionType = Button`、`lastActionBtn` を保持する。
  6. 保存時は、`SaveAction(mode == 10)` で `Actions.xml` に `Button` 種別として保存される。`ActionsDTO` の保存・読み込み、`SpecialAction` の復元も `Button` を扱える。
- **所見**: 保存・読み込みと、クリック後の流れは、コード上は成立している。「割り当てられない」現象は、実行時の条件（出力種別、クリックの当たり判定、画面の状態など）に依存する可能性が高い。置換前（Phase5 以前）と同じ現象かを含め、実機での再現と、ログによる確認が必要。
- **調査に必要な情報（ユーザーへの確認事項）**:
  1. プロファイルの出力種別（Xbox 360／DS4）。
  2. コントローラーの図のボタンをクリックしたときの見た目（ウィンドウが閉じるか、何も起きないか、ボタンが反応しないか）。
  3. 閉じた後の「Press Key」ボタンの表示（キー名／ボタン名／変化なし）。
  4. Trace ログ（`OutputButtonBtn_Click`、`ReadSettings` の呼び出しの有無）。必要なら、診断用のログ出力を追加する（動作は変えない）。

## 5. 実機の観察と原因（2026-10-15）

### 5.1 観察（ユーザー）
- 「Select action」画面で、コントローラーの Cross ボタンをクリックすると、画面が閉じて SA 編集画面へ戻る。戻った直後の「Press Key」ボタンの表示は、変更前の `C (Toggle)` のまま。
- 保存して、もう一度 SA 編集画面を開くと、`Cross`（Switch Mode は Press）と表示される。
- コントローラーのボタンを割り当て、Switch Mode を Toggle にして保存しても、開き直すと Press に戻っている。
- Press モードのボタンの割り当ては、GamePadTester で正常に動作することを確認した。

### 5.2 結論
- **コントローラーのボタンの割り当て自体は、動作している**（当初の「割り当てられない」という認識は、表示の更新遅れによるもの）。見つかった問題は次の 2 件。

### 5.3 問題 1: 戻った直後に、Press Key ボタンの表示が更新されない
- 原因: `PressKeyViewModel.ReadSettings` の `ActionType.Button` の分岐が、`describeText` を書き換えて `DescribeTextChanged` イベントだけを発行し、画面が購読している `PropertyChanged`（`DescribeText`）を発行していない。キー種別の分岐は、`UpdateDescribeText()` を経由して両方を発行するため、更新される。
- 影響: 表示のみ。保存される内容は正しい。
- 是正案: Button の分岐でも、`UpdateDescribeText()` と `UpdateToggleControls()` を呼ぶ（保存・再読込後の表示と同じ「ボタン名のみ」の表記になる）。

### 5.4 問題 2: ボタンの割り当てで Switch Mode（Toggle）が保存されない
- 原因（3 か所で SwitchMode が捨てられている）:
  1. `PressKeyViewModel.SaveAction` の Button の分岐が、`extras`（SwitchMode）を渡さずに `SaveAction(mode 10, …)` を呼ぶ（キー種別の分岐は渡している）。
  2. `ActionsDTO.MapFrom` の `Button` の分岐が、`SwitchMode` を書き出さない（`Key` の分岐は書き出す）。
  3. `ActionsDTO.MapTo` の `"Button"` の分岐が、`SwitchMode` を読み込まない（`new SpecialAction(…)` に `extras` を渡さない）。
- 補足: `SpecialAction` のコンストラクタ（`Button`）は、`extras` の 1 行目を `SwitchMode` として解釈する実装が既にある。実行時の `KeyButtonActionController` も `KeyButtonSwitchMode` を参照している。つまり、保存・読込の経路だけが未完成。
- 影響: Toggle を選んでも、保存すると Press になる。実行時の動作は、常に Press。
- 是正案: 上記 3 か所で SwitchMode を渡す・書き出す・読み込む（`Key` と同じ形）。既存の `Actions.xml`（`SwitchMode` が無いもの）は、従来どおり Press として読み込む。

### 5.5 置換前（Phase5 以前）との比較
- 本件は DI 化の変更とは無関係（`ReadSettings`、`SaveAction`、`ActionsDTO` の該当箇所は、DI 化では引数の渡し方を変えただけ）。`git log` 上、Button の SwitchMode は `SpecialAction` のコンストラクタにだけ実装され、保存・読込側への展開が行われていない状態で残っている（実装途中の機能）。

## 6. 是正の実施（2026-10-15、ユーザー承認 A）
- **状態**: 実装済み・ビルド、テスト、実機確認の結果待ち。
- **問題 1（表示）**: `PressKeyViewModel.ReadSettings` の `ActionType.Button` の分岐で、`UpdateDescribeText()` と `UpdateToggleControls()` を呼ぶようにした（`PropertyChanged` が発行され、戻った直後に表示が更新される）。
- **問題 2（SwitchMode の保存）**:
  - `PressKeyViewModel.SaveAction`: Button の分岐でも `extras`（`"{SwitchMode}\n{ucontrols}"`）を渡す。
  - `ActionsDTO.MapFrom`（`Button`）: `action.extra` の 1 行目を `SwitchMode` として書き出す。
  - `ActionsDTO.MapTo`（`"Button"`）: `SwitchMode` があれば `extras`（`"{SwitchMode}\n"`）として `SpecialAction` に渡す。無ければ従来どおり（Press）。
- **テスト**（`Phase6Step10GroupThreeContractTests`）: ボタンの Toggle／Press の保存・読込の往復（`SaveAction` → `Actions.xml` → 読み直し → 編集画面の開き直し）、`SwitchMode` の無い従来の `Actions.xml` が Press として読み込まれること、戻った直後に `DescribeText` の `PropertyChanged` が発行されること。
- **実機確認（案）**:
  1. SA の「Press Key」で、コントローラーのボタンを割り当てて戻った直後に、ボタン名（`Cross` など）が表示されること。
  2. Switch Mode を Toggle にして保存し、開き直しても Toggle のままであること。
  3. Toggle で保存したアクションが、実機（GamePadTester など）で Toggle として動作すること（1 回押すと押しっぱなしの状態になり、もう 1 回押すと解除される）。
  4. Press で保存したアクションは、従来どおり動作すること。

## 7. 実機確認の結果と、新たに判明した問題（2026-10-15）
- **確認できたこと（§6 の項目 1・2）**: 戻った直後のボタン名の表示、Toggle の保存・再読込（ログの `isToggle=True`）は、期待どおり。
- **問題 3（実行時に、ボタンの Toggle が機能しない）**: Toggle で保存したボタンのアクションを実行しても、Press としてしか動作しない。
  - **原因（コードの読解とユーザーのログによる）**: ボタン種別（`Button`）のスペシャルアクションの出力は、`Mapping.cs` の Stage3（「Synchronous Button SpecialAction handling」。3226 行付近）が、トリガーが成立している間だけ `outputfieldMapping.buttons[...] = true` を設定する実装のみ。Toggle（押すたびに ON／OFF を切り替える）の状態を持つ処理が存在しない。
  - **`KeyButtonActionController` の経路は出力に寄与していない**: `DefaultActionFactory`／`ActionFactory` は、`Key`／`Program`／`Macro`／`Profile` にしか `Action` の実装を作らない（`Button` は `null`）。そのため、ログの `BUTTON press delegated to KeyButtonActionController`（`Mapping.cs` 5324 行付近の `TryDispatchSATriggerEstablished`）は、コントローラーを生成してログを出すだけで、実際のボタン出力も、キーボード出力も行わない（SwitchMode が `isToggle=True` と表示されるのは、コントローラーの生成時の判定のみ）。
  - **つまり**: Toggle は、保存・読込の経路（本 Step で是正）と、実行時の動作（未実装）の両方が欠けていた機能。`SpecialAction` コンストラクターの Button の SwitchMode 解釈と、`KeyButtonSwitchMode` の参照は、実装途中のまま残っている。
- **性質**: DI 化の範囲外の機能追加（`Mapping.cs` のホットパスの変更）。
- **選択肢（要判断）**:
  - A: Stage3 に Toggle の状態を追加する（`device:actionname` ごとの ON／OFF を、トリガーの立ち上がりで反転し、ON の間は出力する。プロファイルの適用・切断時に状態をクリアする）。実機確認が必須。
  - B: 当面、ボタンの割り当てでは Switch Mode を Press に固定し、Toggle を選べないようにする（画面の変更のみ。誤解を防ぐ）。Toggle の実装は、別途計画する。
  - C: 何もせず、先送り台帳に記録する。

## 8. 決定と実施時期（2026-10-15、ユーザー承認）
- **決定**: 問題 3（ボタンの Toggle が実行時に機能しない）は、**案 A（Toggle の実装）**で進める。
- **実施時期**: **Step10 の残り（Step10-4、Step10-5）の後**に実施する（本書の §6 の是正［表示の更新、SwitchMode の保存・読込］は、実施・確認済み）。
- **設計上の条件（ユーザー指示）**: 構造が複雑にならないよう、**キーボード・マウス（KBM）の Toggle モードと共通化した形**で実装する。
  - 方針案（実施時に、現行コードで検証してから確定する）:
    - 現行の KBM の Toggle は、`ActionManager` の「アクションごとの `IsToggledOn`（`SetToggledOn`／`ToggledOnChanged`）」と、`KeyAction`／`ToggleActionController`／`KeyButtonActionController` の `ToggleImpl` が担っている（`Mapping` は `IsToggledOn` を直接書き換えない設計）。
    - ボタン種別には `Action` の実装が無い（`DefaultActionFactory`／`ActionFactory` が `null` を返す）ため、Toggle の状態を持つ場所が無い。ここに、`Button` 用の `Action` 実装（立ち上がりで `IsToggledOn` を反転する）を足し、Stage3 が、トリガー成立中（Press）または `IsToggledOn`（Toggle）の間だけ出力ボタンを ON にする形を第一候補とする。状態の保持と、プロファイル適用・切断時のクリア（`ClearAllEntries`、`ToggledOnChanged` による解除）は、既存の KBM と同じ仕組みを使う。
    - Stage3 と、`Mapping.cs` 5324 行付近の `TryDispatchSATriggerEstablished`（出力に寄与していないログのみの呼び出し）の二重の経路も、この際に整理する。
  - 新しい状態の辞書（`lastButtonTriggerState` のような静的辞書の追加）は、極力作らない。
- **実機確認（案）**: Toggle で保存したボタンのアクションで、1 回押すと押しっぱなし、もう 1 回押すと解除されること（GamePadTester）。Press のアクションと、KBM の Toggle のアクションに影響がないこと。プロファイルの切替・コントローラーの切断で、Toggle の状態がクリアされること。
- **計画書**: 実施時に、`Phase6-Step10-3d-Plan.md`（仮称）を新規作成する。それまでは、本書 §7・§8 を正とする。
