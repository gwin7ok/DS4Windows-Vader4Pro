# Phase7-Step8 計画書: `Mapping` の instance 化と、呼び出し元の切り替え

作成日: 2026-10-17  
対象ブランチ: `For-DI-migration-work`  
上位計画: `Phase7-Plan.md` §2（Step7〜9: `Mapping` の instance 化の第 2 段）、`Phase7-Residual-Ledger.md` §8〜§10、`Phase7-Step7-Completion-Report.md` §5・§6  
状態: **Q1〜Q9 は推奨どおり承認（2026-10-17）。W1〜W3 完了（ビルド・テスト成功）。W4 実装済み（ビルド・テスト待ち）。次は W5（文書）。**

---

## 1. 目的と位置づけ

- Step7 で、`Mapping` の外から触られる静的な状態を 3 つの保持クラス（`SyntheticInputState`、`MacroRuntimeState`、`StickMotionState`）へ出し、特性化テストを整えた。
- Step8 は、`Mapping`（7,836 行。`public class Mapping` だが、メンバーのほぼすべてが static）を **DI の Singleton ＋ 抽象の窓口** にして、次を解消する。
  - `Mapping` の静的な束縛: `profileSettings`、`profileApplication`、`VirtualKBM`（`AppHost`／`ServiceProviderHolder`／`Global` のフォールバック）。
  - 保持クラス 3 つの `Shared`（暫定の静的な窓口）。
  - `Global.outputKBMHandler`／`outputKBMMapping`（Step7 の Q6＝A で Step8 に割り当て済み）。
  - 呼び出し元（`ControlService`、`Mouse`、`SyntheticDispatcher`、`Actions/*`、`ProfileApplicationService`、`Global`、画面）の静的呼び出し。
- Step9 は、互換 `AppHost` の削除、`Mapping.cs` の `Global` 定数参照（43 件）の再配置、K3（マクロの二重ガード）。Step8 では扱わない。
- 計算のロジック（`MapCustom`、`SetCurveAndDeadzone`、`Commit` などの本体）は変えない。変えるのは、`static` の有無と、状態・依存の受け取り方だけ。

---

## 2. 調査結果

### 2.1 `Mapping` の現状

- 静的なフィールド（Step7 後の残り）の分類。
  - サービスへの束縛: `profileSettings`、`profileApplication`、`VirtualKBM`（プロパティ）。
  - Step7 の転送プロパティ（保持クラスへの転送。状態なし）: `deviceState`、`globalState`、`syncStateLock`、`macrodone`、`macroControl`、`macroCount`、`macroTaskQueue`、`macroDispatchInFlight`、`macroDispatchLock`、`gyroStickX`、`gyroStickY`、`mapStickActionData`、`flickMappingData`、`l2/r2TwoStageMappingData`、`wheelFilters`、`deltaAccelProcessors`。
  - `Mapping` の内部だけで使う状態（instance フィールドにする）:
    - 入力の評価: `fieldMappings`／`outputFieldMappings`／`previousFieldMappings`（8）、`customMapQueue`、`lastButtonTriggerState`、`prevInputEstablished`、`held`、`suppressedTriggerButtons`、`deviceRuntime`、`nowAction`／`oldnowAction`／`oldnowKeyAct`。
    - スティック・ジャイロ: `stickValueHistory`、`outSqrStk`、`lastStickAxisValues`、`lastWheelGyroValues`、`previousPointer*`、`accel*`、`totalTravelRadial`、`tempOutXNorm`／`tempOutYNorm`、`usingDeltaAccel`、`horizontalRemainder`／`verticalRemainder`、`mcounter`／`mouseaccel`／`prevmouseaccel`。
    - ホイール・クリック: `oldnow`、`pressagain`、`wheel`、`stickWheel*`。
    - マクロ・ライトバー: `threadCts`、`lightbarMacroTask`、`altTabDone`／`altTabNow`ほか、`macroEndIndex`。
    - 出力・色・デバッグ: `extrasRumbleActive`、`fadetimer`／`prevFadetimer`、`lastColor`、`mappingEvalSinceLast`ほか計測用カウンター、`latestDebugMsgTime`／`latestDebugData`。
    - キャッシュ: `_syntheticActionCache`、`keyButtonControllers`／`keyButtonControllerLock`。
  - static のまま残す（状態を持たない・定数）: `ds4ControlMapping`、`shiftTriggerMapping`、`BoolDS4Controls`、`calibrationColor_*`、各 `const`、純粋な関数（`TValue`、`ClampInt`、`compare`、`remainderCutoff`、`getBool*Mapping` の 4 つ、`DS4ControltoInt`、`WarpEaseOut` ほか）。
- 静的コンストラクター: `customMapQueue` の初期化と、`ActionManager.ToggledOnChanged` へのログ出力だけの購読。前者は instance のコンストラクターへ移し、後者は静的コンストラクターに残す（購読を `Mapping` の instance ごとに増やさない。ログのみの購読で、挙動を変えない）。

### 2.2 外部の利用者（メソッドごと）

- `ControlService`（`Mapping.` の参照 約 40 件）:
  - `Commit`（2）、`HandleDeviceDisconnect`（1）、`MapCustom`（1）、`SetCurveAndDeadzone`（1）、`TempMouseJoystick`（1）→ instance 呼び出しへ。
  - `getBoolButtonMapping`（15）、`getBoolAxisDirMapping`（8）、`getBoolTouchMapping`（4）、`getBoolTriggerMapping`（2）→ 純粋な関数。static のまま（Q2＝A）。
  - 保持クラスの `StickMotionState.Shared`（9）→ コンストラクター注入へ。
- `Mouse`: `Mapping.MapClick`（7）→ `SyntheticInputState` へ移す（Q5＝A。中身は `deviceState[device].currentClicks.*Count++` だけで、`Mapping` の他の状態に触れない）。`StickMotionState.Shared`（5）→ コンストラクター注入。コンストラクターの呼び出しは `ControlService`（1）とテスト（4）。
- `SyntheticDispatcher`（静的クラス）: `SyntheticInputState.Shared`（13）、`Global.outputKBMHandler`（2）、`Global.outputKBMMapping`（1）。呼び出し元は `Mapping`（3）、`KeyButtonActionController`（21）、`KeyAction`（1）、`KeyOutputAction`（2）、`RepeatHelper`（2）。
- `Actions/*`:
  - `ActionManager`（静的ファサード）: `HandleDeviceDisconnect`（1。ホスト未構築時のフォールバック）、`GetOrCreateKeyButtonControllerForAction`（2）。
  - `DefaultActionManager`（DI 登録済み）: `HandleDeviceDisconnect`（1）。
  - `DefaultMacroPlayer`（DI 登録済み）: `PlayMacroDirect`（1）、`EndMacroDirect`（1）。
  - `Actions/*` の `ServiceProviderHolder` 17 件は Phase8 の対象で、触れない。
- `ProfileApplicationService`: `Mapping.ClearKeyButtonControllersForDevice`（1）。
- `Global`: `Mapping.ResetRuntimeStateForKey`（1。`Global.SaveAction` の末尾。`Global.SaveAction` の呼び出し元は `SpecialActionRepository` だけ）。
- 画面: `ControllerReadingsControl`（`SetCurveAndDeadzone`）、`RecordBoxViewModel`（`GetBoolMappingExternal` 2）。どちらも `ControlService` の参照を持つ。`ProfileSettingsViewModel`（`MOUSESPEEDFACTOR`）と `MouseCursor`（`remainderCutoff` 4）は定数・純粋な関数で、Step9 の再配置まで static のまま。
- 保持クラスの `Shared` の利用者（`ServiceRegistration` の 3 登録を除く）: `Mapping`（3）、`ControlService`（9）、`Mouse`（5）、`SyntheticDispatcher`（13）、`ActionsLoadedRuntimeReset`（3）。

### 2.3 Service Locator の内訳（`Mapping.cs`）

- `AppHost.GetService` 6 件:
  - `IProfileSettingsService`（`Global.ProfileSettingsServiceInstance` のフォールバックつき）、`IProfileApplicationService`、`IVirtualKBM`（`Global.outputKBMHandler` のフォールバックつき）→ コンストラクター注入。
  - `IProcessLauncher`（`?? new DefaultProcessLauncher()`）、`IProfileSwitcher`（`?? new DefaultProfileSwitcher()`）→ コンストラクター注入。
  - `IDeviceStateAccessor`（マクロの振動イベントでコントローラーを得る）→ `ControlService` が実装者のため、循環を避けて `Func<IDeviceStateAccessor>`（Q4＝A）。
- `ServiceProviderHolder` 5 件:
  - `IKeyButtonActionControllerFactory`（2）、`IControllerRegistry`（1）→ 新設する `KeyButtonControllerRegistry` がコンストラクターで受ける（Q3＝A）。
  - `MouseOutputAction`（2。`Commit` の左クリックの押下と、別の分岐）→ 未登録で `GetService` は常に null。出力は後続のフォールバック（`VirtualKBM.PerformMouseButtonEvent`）が行っている。この無効な検索を削除する（Q8＝A。出力は変わらない）。

### 2.4 `outputKBMHandler`／`outputKBMMapping`

- `Global.outputKBMHandler`（`VirtualKBMBase`、public static フィールド）:
  - 書き込み: `OutputKBMHandlerLifecycle`（`ReleaseHandler`、`SwitchToFallbackHandler`）、`Global.InitOutputKBMHandler`（`DetermineHandler` から）。
  - 読み取り: `OutputKBMHandlerAdapter`（19 件。呼ぶたびに `Global.outputKBMHandler` へ委譲）、`OutputKBMHandlerLifecycle`（8 件）、`SyntheticDispatcher`（2）、`Mapping`（1。フォールバック）。
  - `ControlService` は `IVirtualKBM`（アダプター）と `IVirtualKBMLifecycle` を注入で受けており、`Global` を直接見ない。
- `Global.outputKBMMapping`: `ProfileSettingsServiceInstance.OutputKBMMapping` への転送プロパティ。読み取りは `SyntheticDispatcher.ResolveNativeKey` と `Global.RefreshActionAlias`、書き込みは `Global.InitOutputKBMMapping`（Lifecycle の `InitializeMapping` から）。実体は `IProfileSettingsService.OutputKBMMapping`（`BackingStore`）に既にある。
- テストへの影響が大きい: `outputKBMHandler`／`outputKBMMapping` を触るテストは 11 ファイル（`VirtualKBMLifecycleTests` が 31 件）。

### 2.5 依存の循環（注入にするときの注意）

- `Mapping` → `IProfileApplicationService`、`ProfileApplicationService` → `Mapping.ClearKeyButtonControllersForDevice`: 直結すると循環する。`KeyButtonControllerRegistry`（Q3＝A）を挟んで解消する（`ProfileApplicationService` は `Func<ControlService>` で遅延、`Mapping` は登録簿だけを見る）。
- `Mapping` → `ControlService`（`IDeviceStateAccessor`）、`ControlService` → `Mapping`: `Func<IDeviceStateAccessor>` で遅延する。
- `DefaultActionManager` → `Mapping`: `AppHost.PreallocateActionEntries` が、ホスト構築の直後に `DefaultActionManager` を解決する。`IMapping` を直接注入すると、`Mapping` とその依存（`BackingStore`、`ProfileApplicationService` ほか）がホスト構築時に作られ、起動順序（Phase5-Plan §5.4）の保証が崩れるおそれがある。`Func<IMapping>`（遅延）にする（Q6＝A に含む）。
- `IProfileSwitcher`（`DefaultProfileSwitcher`）は `IProfileApplicationService` を受ける。`Mapping` が `IProfileSwitcher` を受けても、循環はしない。

### 2.6 ホットパス（毎レポート実行）、割り当てゼロ、スレッド

- `MapCustom`、`SetCurveAndDeadzone`、`Commit`、`ApplyStickCalibration` が毎レポート実行される。`MappingHotPathAllocationTests`（`SetCurveAndDeadzone` 2、`ApplyStickCalibration` 1）がゼロ割り当てを固定している。
- static から instance へ変えても、割り当ては増えない（`this` の参照のみ）。ただし、**ホットパスのメソッドの中のラムダが、新たに `this` を捕捉すると、呼び出しごとに割り当てが生じる**。全ラムダを走査した結果:
  - `CalcAntiSnapbackStick` の `queue.Any(...)`: 既にローカル変数（`axisXValue` など）を捕捉しており、割り当ては既存。`this` は使わない。変更なし。
  - ほかは、マクロの開始（`Task.Factory.StartNew`、`ContinueWith`）、ライトバーマクロ（`Task.Run`）、ログ文字列（`Select`）、プロファイル適用（`Task.Run`）で、レポートごとの経路ではない。
- 保持クラスの配列は参照を再代入しない（要素の差し替えのみ）。`GlobalState` だけは `ResetAll` で差し替わるため、`Mapping` は `GlobalState` を保持クラス経由で読み続ける。それ以外の配列（`DeviceState` ほか）は、`Mapping` が **readonly の instance フィールドへ 1 度だけ複製して持つ**（転送プロパティを削除し、間接参照も増やさない。Q5＝A）。
- マクロは別スレッドの `Task`（`PlayMacroTask`）で動き、`macroControl` を `MapCustom`（レポートのスレッド）と共有する。Singleton の instance に変えても、この共有の範囲は広がらない（保持クラスは従来から単一の instance）。`macroDispatchInFlight`／`macroDispatchLock`（K3）は Step9 まで触れない。
- `Phase5-Plan.md` §5 の 6 大ガードレール:
  - 5.2（適用時 Halt 停止）: `ProcessControlSettingAction` の `d.HaltReportingRunAction(...)` の中で `profileApplication.ApplyProfile` を呼ぶ構造は変えない。注入される `IProfileApplicationService` が Singleton で同一であること（既存と同じ）を W2 で確認する。
  - 5.4（On-Demand パス評価）: 上記のとおり、`DefaultActionManager` への注入は `Func` にして、ホスト構築時に `Mapping` を作らない。
  - 5.6（切断時クリーンアップ）: `HandleDeviceDisconnect` の呼び出し元（`ControlService`、`DefaultActionManager`）が切り替わるだけ。呼び出し順は変えない。
  - 5.1（同一 XML 排他）、5.3（AutoProfile のスレッド）、5.5（ドライバ破棄順序）: 関与しない。

### 2.7 テストへの影響（型や定数を動かす前の検索結果）

- `Mapping` の static メソッドを直接呼ぶテスト（instance への切り替えが必要）:
  - `MappingArgumentPassThroughTests`（`SetCurveAndDeadzone`、`ApplyStickCalibration`、`Commit`）、`MappingHotPathAllocationTests`、`MappingCharacterizationScope`、`Phase7Step7CommitCharacterizationTests`（`MapClick`／`Commit`／`HandleDeviceDisconnect`。`getBool*` は static のまま）、`Phase7Step7MapCustomCharacterizationTests`、`ControlTabAndSpecialActionKeyTests`／`ManagedActionManagerRegistrationTests`（`GetOrCreateSyntheticKeyAction`、`ClearSyntheticActionCache`）。
- リフレクションで `Mapping` の static メンバーを見るテスト（`BindingFlags.Static` の変更や、フィールドの移動で壊れる）:
  - `MappingGyroControlsHelperTests`（`IsUsingGyroForControls`）、`MappingLogActionDoneCountOnTriggerTests`（`LogActionDoneCountOnTrigger`）、`MappingPlayMacroDispatchGuardTests`（`PlayMacro`）、`MappingSpecialActionSuppressionTests`（`suppressedTriggerButtons` 7 箇所）、`MouseWheelShimTests`（`typeof(Mapping)` の確認が必要）。
  - `StandaloneTests` の `MappingControllerDisposeBehaviorTests`／`MappingControllerLifecycleTests`: `keyButtonControllers`（private static フィールド）と `GetOrCreateKeyButtonController` を見る。`KeyButtonControllerRegistry` へ移るため、書き換えが必要。
- 文字列でソース（`Mapping.cs`）を読むガードテスト（`static` を含むシグネチャを検索しているため、`static` を外すと**壊れる**）:
  - `MappingCommitAndCalibrationGlobalReferenceGuardTests`（`public static DS4State ApplyStickCalibration(`、`private static bool ShiftTrigger(`、`public static void Commit(`、`public static void MapClick(`）。
  - `MappingSetCurveAndDeadzoneGlobalReferenceGuardTests`（`public static DS4State SetCurveAndDeadzone(`、`public static DS4State ApplyStickCalibration(`）。
  - `MappingControlPathGlobalReferenceGuardTests`（`MapCustom`、`ProcessControlSettingAction`、`IfAxisIsNotModified`、`MapCustomAction`、`ReleaseActionKeys`、`getMouseMapping`、`Scale360degreeGyroAxis` の各シグネチャ 7＋2）。
  - `MappingHelperMethodsGlobalReferenceGuardTests`（`GetByteMapping`、`GetBoolMappingExternal`、`GetBoolMapping`、`getBoolSpecialActionMapping`、`GetBoolActionMapping`、`GetXYAxisMapping`、`GetMouseWheelMapping`、`IsUsingGyroForControls`）。
  - `MappingMacroPathGlobalReferenceGuardTests`（`PlayMacroCodeValue`、`AltTabSwapping`、`AltTabSwappingRelease`）。
  - 対処の方針: マーカーから `static ` を除く（読み取る範囲は同じ）。ガードの意図（`Global` 参照の不在）は変えない。
- Step7 のガード（現在の `Mapping.cs` の行を完全一致で期待する。W4 で書き換えが必要）: `Phase7Step7SyntheticInputStateGuardTests`、`Phase7Step7MacroRuntimeStateGuardTests`、`Phase7Step7StickMotionStateGuardTests`（`private static readonly ... = ....Shared;` と転送プロパティの行）。
- `MappingStep3FinalGlobalReferenceGuardTests`: `Mapping.cs` の TODO 文（`TODO(Phase6-Step3-7, 温存・決定KEEP): DI ホスト未構築時`）の存在を期待する（87 行目）。この TODO は、フォールバックの削除とともに消えるため、期待を更新する。
- ラチェット: `Step10FourGlobalRatchetGuardTests`（`Mapping.cs` の基準値 `(0, 6, 43)` → `(0, 0, 41 前後)`、`OutputKBMHandlerAdapter` 19 → 0、`OutputKBMHandlerLifecycle` 8 → 0、`SyntheticDispatcher` 4 → 0、`Global.cs` ほか）、`Phase7Step1ServiceLocatorRatchetGuardTests`（`Mapping.cs` 5 → 0）。
- コンストラクターを変えるクラスの `new` の件数（テスト含む）: `Mouse` 5（本体 1＋テスト 4）、`DefaultActionManager` 8、`DefaultMacroPlayer` 6、`SpecialActionRepository` 4、`OutputKBMHandlerLifecycle` 2、`ProfileApplicationService` 1。**`ControlService` は本体の `ServiceRegistration` の 1 箇所だけ**（テストはコンテナから解決する）。

### 2.8 Step7 の報告書との差

- Step7 完了報告書 §3 は、「`ControlService` のコンストラクターを変えると多数のテストの構築コードの改修になる」ため、保持クラスを注入にしなかったと記している。今回の調査では、`new ControlService(` は本体の `ServiceRegistration` の 1 箇所だけで、テストは `AppHost.GetService<ControlService>()` で解決していた。コンストラクターの変更は軽い。Step8 で `IMapping` と `StickMotionState` を注入する。

### 2.9 本体の呼び出しが 0 件のメンバー（`copilot-instructions.md` 2.4）

- `Mapping` の `internal static` のうち、本体にも `DI/` 経由にも呼び出しが無い 4 件:
  - `DispatchProfileActionEdge`（`DispatchOrSetBeingTriggered` を呼ぶだけ）。
  - `TakePendingRestoreProfileName`（`b8c914c2` で「専用プロファイルサービス」と同時に追加。復元の保留は、現在 `IProfileApplicationService`（`ClearPendingRestore`、`RestoreFromAction`）が持つ）。
  - `ApplyProfileDirect`、`RestoreProfileDirect`（`ca3d4b13` で `DefaultProfileSwitcher` 用に追加。現在の `DefaultProfileSwitcher` は `IProfileApplicationService` を直接呼び、`Mapping` を経由しない）。
- 判断（推奨）: 「使うべきでない（正規の経路と重複する並行実装）」と判断し、削除する（Q7＝A）。
- なお `GetOrCreateSyntheticKeyAction`／`ClearSyntheticActionCache` は、本体では `ProcessControlSettingAction` が使っている（テストからも使われる）。0 件ではない。

---

## 3. 窓口（抽象）の設計案

### 3.1 `IMapping`（`DS4Windows/DI/IMapping.cs`、実装 `Mapping`）

- 外部の利用者が使う 8 メンバーだけを持つ。
  - 入力の評価: `MapCustom`、`SetCurveAndDeadzone`、`TempMouseJoystick`、`Commit`。
  - 画面向け: `GetBoolMappingExternal`。
  - 切断・マクロ（`Actions/*` 向け）: `HandleDeviceDisconnect`、`PlayMacroDirect`、`EndMacroDirect`。
- `ApplyStickCalibration`（`SetCurveAndDeadzone` が内部で呼ぶ）、`GetOrCreateSyntheticKeyAction`、`ClearSyntheticActionCache` は、クラスの public のままにして、窓口には含めない（利用者はテストだけ）。
- 登録: `services.AddSingleton<IMapping, Mapping>()`（1 つの instance。`Mapping` の具象型は直接解決させない）。
- `ControlService` は `IMapping` をコンストラクターで受け、読み取り専用プロパティ `MappingService` として公開する（`ProfileSettingsService` と同じ流儀）。`ControllerReadingsControl` と `RecordBoxViewModel` は、持っている `controlService` からそれを使う。

### 3.2 `Mapping` のコンストラクター（依存 11 個）

- 保持クラス: `SyntheticInputState`、`MacroRuntimeState`、`StickMotionState`。
- サービス: `IProfileSettingsService`、`IProfileApplicationService`、`IVirtualKBM`、`IProcessLauncher`、`IProfileSwitcher`。
- 新設: `IKeyButtonControllerRegistry`（Q3）、`ISyntheticDispatcher`（Q6）。
- 遅延: `Func<IDeviceStateAccessor>`。
- 11 個は多いが、`Mapping` が実際に使っている部品の数である。数を減らす整理（部品のまとまりの導入）は Step10 で検討する。

### 3.3 `IKeyButtonControllerRegistry`（`DI/`）と `KeyButtonControllerRegistry`（`DS4Control/Services/`）

- 今 `Mapping` が持つ、デバイス × アクション名ごとの `KeyButtonActionController` の登録簿（辞書、ロック、生成、破棄）を切り出す。メンバー: `GetOrCreate(device, mode, actionName)`、`GetOrCreate(device, sa)`、`GetOrCreateForAction(device, sa)`（アダプターで包んで返す）、`ClearForDevice(device)`。
- 依存: `IKeyButtonActionControllerFactory`、`IControllerRegistry`（どちらも登録済み）。ログ、キー形式（`"{device}:{actionName}"`）、`inst ?? new KeyButtonActionController(...)` のフォールバックは、そのまま移す。
- 利用者: `Mapping`、`ProfileApplicationService`（`ClearKeyButtonControllersForDevice` の置き換え）、`ActionManager`（静的ファサード。Q8）。

### 3.4 `ISyntheticDispatcher`（Q6）

- 今の静的クラス `SyntheticDispatcher` の `ResolveNativeKey`／`SendPress`／`SendRelease`／`ResetKeyTiming` を持つ。依存: `SyntheticInputState`、`IVirtualKBM`（ハンドラの既定）、`IProfileSettingsService`（`OutputKBMMapping`）。

---

## 4. 決定が必要な事項

### Q1: `Mapping` の窓口の形

- A（推奨）: `IMapping`（8 メンバー）を 1 つ新設し、`Mapping` が実装する（§3.1）。
- B: 利用者ごとに複数の窓口へ分ける（入力の評価、切断・マクロ、画面向けなど）。窓口の数が増え、実装は同じクラスに集まるだけ。
- C: 窓口を作らず、具象クラス `Mapping` を Singleton として登録する（テストでの差し替えが難しい）。

### Q2: static のまま残すもの

- A（推奨）: 状態を持たない純粋な関数と定数は static のまま残す（`getBool*Mapping` 4 つ、`compare`、`remainderCutoff`、`DS4ControltoInt`、`MOUSESPEEDFACTOR` ほか）。`ControlService` の 29 件、`MouseCursor` の 4 件、`ProfileSettingsViewModel` の 1 件の呼び出しを変えずに済む。`Mapping.cs` の `Global` 定数参照（Step9）の再配置とあわせて、置き場所を最終的に決める。
- B: すべて instance にする（呼び出し元の変更が約 40 件増える。得られるものは少ない）。

### Q3: キー／ボタンのコントローラー登録簿の切り出し

- A（推奨）: `KeyButtonControllerRegistry` を新設する（§3.3）。`Mapping` の `ServiceProviderHolder` 3 件が消え、`Mapping` ⇔ `ProfileApplicationService` の循環が解消する。`ActionManager` の静的ファサードも、これを経由できる。
- B: `Mapping` の中に残し、`ProfileApplicationService` が `Func<IMapping>` で `ClearKeyButtonControllersForDevice` を呼ぶ（循環は避けられるが、`Mapping` の責務が増えたままになり、登録簿のテストも `Mapping` の構築が必要）。

### Q4: `Mapping` から `ControlService` への逆参照（マクロの振動イベント）

- A（推奨）: `Func<IDeviceStateAccessor>` を注入する（既存の `Func<ControlService>` と同じ流儀）。
- B: `PlayMacro` の経路に `ControlService` を引数で渡す（`PlayMacroDirect` など `ctrl` を持たない入口があり、シグネチャの変更が広がる）。

### Q5: 保持クラスの受け取り方、`Shared` の削除、`MapClick` の移動

- A（推奨）:
  - `Mapping`、`Mouse`、`ControlService`、`SyntheticDispatcher`、`ActionsLoadedRuntimeReset` は、保持クラスをコンストラクターで受ける。`Shared` を 3 クラスから削除する。
  - `Mapping` は、保持クラスの配列を readonly の instance フィールドへ 1 度だけ複製し、**Step7 の転送プロパティをすべて削除する**。`globalState` だけは、差し替えがあるため保持クラスの `GlobalState` を直接読む。
  - `MapClick`（クリックの回数を数えるだけ）を `SyntheticInputState` へ移す。`Mouse` は `Mapping` に依存しなくなる。`Global.ResetRuntimeStateForKey` の中身は、`SyntheticInputState` のメソッドにする（Q9）。
  - `ActionsLoadedRuntimeReset` は instance にし、`ServiceRegistration` の `BackingStore` の登録で `sp` から作って購読する（購読が 1 つであることは、現在と同じ工夫で保つ）。
- B: 転送プロパティは Step9 まで残す（`Shared` の削除だけ行う）。間接参照が残り、Step9 でもう一度書き換える。

### Q6: `outputKBMHandler`／`outputKBMMapping` の整理と `SyntheticDispatcher`

- A（推奨）:
  - ハンドラの実体を `OutputKBMHandlerAdapter` に持たせる（`VirtualKBMBase Handler { get; set; }` を持ち、`IVirtualKBM` の各メソッドはそれへ委譲する）。`OutputKBMHandlerLifecycle` は具象のアダプターを受けて書き込む。`Global.outputKBMHandler`、`Global.InitOutputKBMHandler`（`DetermineHandler` と `fakeKeyRepeat` のログは Lifecycle へ移す）、`Global.outputKBMMapping`、`Global.InitOutputKBMMapping` を削除する。`outputKBMMapping` は `IProfileSettingsService.OutputKBMMapping` を直接使い、`Global.RefreshActionAlias` は内部で `ProfileSettingsServiceInstance` を使う形にする（または同じ実体の `IProfileSettingsService.RefreshActionAlias` に統一）。
  - `SyntheticDispatcher` を instance（`ISyntheticDispatcher`）にして `Mapping` へ注入する。`Actions/*` の利用者（`KeyButtonActionController` 21、`KeyAction` 1、`KeyOutputAction` 2、`RepeatHelper` 2）は、Phase8 の範囲（`new` で作られる静的な経路）のため、**暫定の静的な橋渡し**（`SyntheticDispatcher` の静的メソッドが、Composition Root で 1 度だけ束縛された instance へ委譲する。TODO: Phase8 で削除）を残す。`Shared`／`Global` とは違い、束縛先は DI が作った唯一の instance で、フォールバックは持たない。
- B: `Actions/*` の利用者もすべて Step8 で注入に切り替える（`KeyButtonActionController` の生成経路とファクトリー、`RepeatHelper` の生成、`KeyAction`／`KeyOutputAction` の生成を変える。Phase8 の範囲と重なり、リスクが大きい）。
- C: `SyntheticDispatcher` は静的のまま、`Global` への依存だけをなくす（`Shared` を削除する以上、束縛が別途必要で、結局 A の橋渡しになる）。

### Q7: 本体の呼び出しが 0 件の 4 メンバー（§2.9）

- A（推奨）: `DispatchProfileActionEdge`、`TakePendingRestoreProfileName`、`ApplyProfileDirect`、`RestoreProfileDirect` を削除する。判断の根拠は、§2.9（正規の経路 `IProfileApplicationService` と重複する並行実装。`copilot-instructions.md` 2.4 の「使うべきでないもの」）。機能の廃止ではない（呼び出し元が無い）。
- B: 温存し、TODO を付ける（使われる見込みがないため、理由づけが難しい）。

### Q8: `ActionManager`（静的ファサード）と `DefaultActionManager` の `Mapping` 呼び出し、および `MouseOutputAction` の無効な検索

- A（推奨）:
  - `DefaultActionManager` は `Func<IMapping>` と `IKeyButtonControllerRegistry` を受ける（ホスト構築時に `Mapping` を作らないため、`Func`）。
  - `ActionManager`（静的）の 3 箇所は、すでに取得している `sp`（`ServiceProviderHolder.Provider`）から `IMapping`／`IKeyButtonControllerRegistry` を解決する（`ServiceProviderHolder` の語は増えない。ラチェットは増えない）。ホストが存在しない経路では、`Mapping` の instance も存在しないため、何もしない（本番では `DefaultActionManager` が必ず解決されるので、この経路は通らない）。
  - `Commit` の `MouseOutputAction` の検索 2 箇所（常に null）を削除し、後続のフォールバックの出力だけにする（出力は同じ）。`MouseOutputAction` を登録しない方針は変わらない。
- B: `ActionManager` の 3 箇所を削除する（Phase8 でファサードごと見直す。フォールバックの削除が、ホスト未構築時の挙動を変える）。

### Q9: `Global.ResetRuntimeStateForKey` の呼び出し元の移動

- A（推奨）: `Global.SaveAction` の末尾の `Mapping.ResetRuntimeStateForKey(savedKey)`（mode==4 のキー保存の後）を、`SpecialActionRepository.SaveAction` の `Global.SaveAction` 呼び出しの直後へ移す（`SpecialActionRepository` が `SyntheticInputState` を受け、`ResetRuntimeStateForKey` 相当を呼ぶ）。実行順とログ（`Runtime state for key {key} cleared on all devices.`）は変えない。`Global.SaveAction` を `Global` から直接呼ぶのは、テスト 1 件（`Phase6Step10GroupThreeContractTests`）だけで、この中身の検証には含まれていないことを W1 で確認する。
- B: `Global.SaveAction` に残し、`Global` から `IMapping` を得る口を作る（`Global` に新たな静的な窓口が必要になる）。

---

## 5. 実装の Wave（推奨案の場合）

各 Wave のビルド・テストはユーザーが実行して結果を報告する。各 Wave で `MappingHotPathAllocationTests` と W1（Step7）の特性化テストが成功することを確認する。

- **W1: 保持クラスの注入と、`Mapping` を介さない整理**（`Mapping` はまだ static）
  - `ControlService` と `Mouse` が、`StickMotionState`／`SyntheticInputState` をコンストラクターで受ける（`Shared` の参照を除く）。`Mouse` のコンストラクターの呼び出し 5 件（本体 1、テスト 4）。
  - `MapClick` を `SyntheticInputState` へ移す（`Mapping.MapClick` は W2 で削除するため、W1 では `Mapping.MapClick` を `SyntheticInputState` への薄い転送にする）。
  - `Global.ResetRuntimeStateForKey` の移動（Q9＝A）。`SpecialActionRepository` の呼び出し 4 件。
  - 0 件の 4 メンバーの削除（Q7＝A。承認が前提）。
- **W2: `Mapping` の instance 化と、依存の注入**（最大の Wave。機械的な変更が中心）
  - `IMapping`、`Mapping` のコンストラクター（§3.2）、`KeyButtonControllerRegistry`、`ServiceRegistration`（Singleton）。
  - メソッドとフィールドの `static` を外す（§2.1 の分類）。`Mapping` の内部だけの状態は instance フィールドになる。転送プロパティを削除し、readonly の複製に置き換える（Q5＝A）。
  - `AppHost` 6、`ServiceProviderHolder` 5 を解消（Q4・Q3・Q8）。`Commit` の `MouseOutputAction` の検索を削除。
  - 呼び出し元の切り替え: `ControlService`（`IMapping` を注入し、`MappingService` を公開）、`ControllerReadingsControl`、`RecordBoxViewModel`、`DefaultMacroPlayer`、`DefaultActionManager`、`ActionManager`、`ProfileApplicationService`。
  - この時点の `SyntheticDispatcher` と `ActionsLoadedRuntimeReset` は、`Shared` を使い続ける（W3 で切り替える）。`Mapping` は `IVirtualKBM`（アダプター。従来どおり `Global.outputKBMHandler` へ委譲）を受けるので、出力経路の挙動は変わらない。
  - テスト: §2.7 の「直接呼ぶテスト」「リフレクション」「文字列ガード（`static` マーカー）」の書き換え。テスト用の `Mapping` の作成の窓口（`TestMappingFactory`）を新設する。
- **W3: 出力 KBM の所有権と `SyntheticDispatcher`**
  - Q6＝A: アダプターがハンドラを持つ、Lifecycle、`Global.outputKBMHandler`／`outputKBMMapping`／`InitOutputKBM*` の削除、`SyntheticDispatcher` の instance 化と橋渡し、`Mapping` への注入。
  - `ActionsLoadedRuntimeReset` を instance にし、`SyntheticDispatcher` ほかから保持クラスの `Shared` の参照を除く。
  - テスト: 11 ファイル（`VirtualKBMLifecycleTests` 31 件ほか）の書き換え。
- **W4: `Shared` の削除と、ガードテスト**
  - 3 つの保持クラスの `Shared` を削除し、`ServiceRegistration` の登録を通常の `AddSingleton<T>()` にする。
  - Step7 のガード 3 本を、コンストラクター注入と readonly 複製の形に更新する。新規のガード: `Mapping` に static の可変状態が残っていないこと（定数・表・純粋な関数を除く）、`Mapping.cs` に `AppHost`／`ServiceProviderHolder`／`Global.outputKBM*` が無いこと、`Shared` が無いこと、`Global.outputKBM*` の不在、`DefaultActionManager` の `Func<IMapping>`。
  - ラチェットの更新（§8）。
- **W5: 文書**
  - 台帳 §11、`Phase7-Status.md`、モデル図（01〜04。`IMapping`、`KeyButtonControllerRegistry`、`ISyntheticDispatcher`、`OutputKBMHandlerAdapter` の所有、依存方向）、チェックリスト U-18、完了報告書。

---

## 6. リスクと確認

- **機械的な変更の量（W2）**: `static` の除去は約 100 メソッドと約 40 フィールドに及ぶ。スクリプトで行い（バックスラッシュを含む正規表現をインラインで書かない）、除去後の差分を、メソッド本体が変わっていないこと（`git diff` の `static` 以外の差分が、束縛の置き換えだけであること）で確認する。
- **ホットパスの割り当て**: §2.6 のとおり、`this` を新たに捕捉するラムダをホットパスに入れない。W2 の後に `MappingHotPathAllocationTests` を確認する。ホットパスのメソッドに、新たにラムダやローカル関数が追加されていないことを、W2 の差分で確認する。
- **起動順序（Phase5-Plan §5.4）**: `DefaultActionManager` は `Func<IMapping>` で遅延する。ホスト構築時（`PreallocateActionEntries`）に `Mapping` が作られないことを、ガードテストで固定する。
- **循環（スタックオーバーフロー）**: Step6 W1 で一度起きた。`Mapping` のコンストラクターで `ControlService` を解決しない（`Func`）。`BackingStore` の登録の中で `IMapping` を解決しない。ガードテストで固定する。
- **マクロの別スレッド**: `PlayMacro` の `Task.Factory.StartNew` と `ContinueWith`、`macroTaskQueue` の更新は、メソッド本体を変えない。`MappingPlayMacroDispatchGuardTests`（二重実行防止）を、instance の `PlayMacro` に対して維持する。
- **既存の不具合**: 調査中に見つけたものはない。見つけた場合は、移行前のコードと比較して回帰かどうかを確認し、修正前に承認を得る。
- **確認不能（U-2・U-5・U-6）**: スロット 5〜8、マクロの実時間は、単体テストが届かない。構造は変えないが、状態の置き場所（static → instance）が変わるため、U-18 に含める。

---

## 7. 実機確認（`Phase6-Step11-RealDevice-Verification-Checklist.md` §11 の U-18 に追加する）

- 通常入力（ボタン・スティック・トリガーの再割り当て）、キー・マウスへの割り当て（押下と解放。押したまま切断してもキーが残らない）、ジャイロ（マウス・スティック・ホイール）、タッチパッド（移動・クリック・タップ）、フリックスティック、2 段トリガー、マクロ（単発・押している間・同期。二重に走らない）、ライトバーマクロ、スペシャルアクション（プロファイル切替・一時プロファイルの復帰・プログラム起動・切断）、キー保存後のリセット、プロファイル切替、複数台（スロット 5〜8 は U-2 のまま確認不能）。
- 起動直後にログに例外が出ないこと（コンストラクター注入の解決）、終了時に固まらないこと。
- U-11〜U-17 は未確認のまま（U-13 は先送り）。

---

## 8. 数値目標（推奨案。確定は各 Wave の末）

- `AppHost.GetService`: 11 → **5**（`Mapping.cs` の 6 件）。
- `ServiceProviderHolder`: 22 → **17**（`Mapping.cs` の 5 件。残りは `Actions/` の 17 件で Phase8）。
- `Program.rootHub`: 0 のまま。
- `Global.*`: 432 → 約 **395**（`OutputKBMHandlerAdapter` −19、`OutputKBMHandlerLifecycle` −8、`SyntheticDispatcher` −4、`Mapping.cs` −2、`Global.cs` の `outputKBM*` 関連。新ファイルは 0）。`Mapping.cs` の定数参照（約 41）は Step9。

---

## 9. 範囲外（持ち越し）

- Step9: 互換 `AppHost`（`DI/Compat/AppHost.cs`）の削除、`Mapping.cs` の `Global` 定数・`Clamp` の再配置、K3（`macroDispatchInFlight` の再設計）、`getBool*` など static の純粋な関数の最終的な置き場所。
- Step10: `BackingStore` のコンストラクター注入、`Mapping` のコンストラクターの依存の整理（11 個）、孤立定数。
- Phase8: `Actions/` の `ServiceProviderHolder` 17、`ActionManager` の静的な呼び出し、`SyntheticDispatcher` の橋渡しの削除、`KeyButtonActionController` ほかの生成経路。

---

## 10. 承認のお願い

- Q1〜Q9 のうち、推奨以外にする項目があれば教えてください。特に確認したいのは次の 3 つです。
  - Q6（`SyntheticDispatcher` の暫定の静的な橋渡しを Phase8 まで残す点）。
  - Q7（本体の呼び出しが 0 件の 4 メンバーの削除）。
  - Q8（`ActionManager` の静的ファサードの扱い、`MouseOutputAction` の無効な検索の削除）。
- 承認後、W1 から実装します。W2 は変更量が最大のため、着手前に、変更するメソッドの一覧を再確認します。
