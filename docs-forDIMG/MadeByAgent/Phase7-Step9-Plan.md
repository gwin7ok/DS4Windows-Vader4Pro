# Phase7-Step9 計画書: 互換 `AppHost` の削除、`Mapping.cs` の `Global` 参照の再配置、K3 の判断

作成日: 2026-10-17  
対象ブランチ: `For-DI-migration-work`  
上位計画: `Phase7-Plan.md` §2（Step7〜9: `Mapping` の instance 化の第 3 段）、`Phase7-Residual-Ledger.md` §8〜§11、`Phase7-Step8-Completion-Report.md` §5・§6  
状態: **Q1〜Q5 は推奨どおり承認（2026-10-17）。W1 完了（ビルド・テスト成功）。W2 完了（ビルド・テスト成功）。W3 完了（ビルド・テスト成功）。W4 は文書。Step9 完了。完了報告書は `Phase7-Step9-Completion-Report.md`。**

---

## 1. 目的と位置づけ

- Step8 で `Mapping` は DI の Singleton になり、`AppHost`／`ServiceProviderHolder` の利用は 0 件になった。Step9 は `Mapping` の instance 化の仕上げとして、次の 3 つを扱う。
  - 互換 `AppHost`（`DI/Compat/AppHost.cs`）の削除。
  - `Mapping.cs` に残る `Global` 参照 41 件（定数 18、`Clamp` 23）の再配置。
  - K3（マクロの二重実行防止 `macroDispatchInFlight`）の判断。
- ロジックは変えない。ホットパス（入力 1 回ごとの処理）は割り当てゼロのまま。
- Step9 の「静的な純粋関数の最終的な置き場所」は、Step10（`Global` の残りの再配置）と同時に判断する方が手戻りが少ないため、Q4 で扱いを決める。

---

## 2. 調査結果

### 2.1 互換 `AppHost`（`DI/Compat/AppHost.cs`）

- 中身は `GetService<T>()` を `DS4WinWPF.AppHost` へ転送するだけの 1 メソッド。
- 修飾なしの `AppHost.GetService`／`AppHost.Services` を `DS4Windows` 名前空間のコードから呼んでいる箇所は **0 件**。確認した結果:
  - `App.xaml.cs:597` は `DS4WinWPF` 名前空間のため、`DS4WinWPF.AppHost` に束縛される（互換版ではない）。
  - 残りの `AppHost.GetService` の文字列は、コメントと `DI/AppHost.cs` 自身のログ文だけ。
  - テストは `DS4WinWPF.AppHost` と完全修飾で呼ぶ。`DS4Windows.AppHost` を参照するテストは無い。
- 参照しているのは次の 2 つだけ。
  - `Step10FourGlobalRatchetGuardTests` の基準値 `["DS4Windows/DI/Compat/AppHost.cs"] = (0, 1, 0)`。
  - `Step12DeadShimRemovalGuardTests.Wave5_RemovedApiSurface_DoesNotReappear`（このファイルを探し、見つからないと検査を省略して合格する）。ファイルを消すと、この検査が黙って無効になるため、「ファイルが存在しない」ことを検査する形に替える。
- 削除すると `AppHost.GetService|Services` の基準値は 5 → **4**（`App.xaml.cs` 1、`DI/AppHost.cs` 2、`Global.cs` 1）。
- 付随する整理: `DI/Compat/` フォルダーが空になる。

### 2.2 `Mapping.cs` の `Global` 参照（ラチェット 41）

- 内訳（コメントを除く）:
  - 定数 18 件: `Global.MAX_DS4_CONTROLLER_COUNT`（配列の長さ。フィールド初期化子ほか）、`Global.TEST_PROFILE_ITEM_COUNT`（`stickValueHistory`、`outSqrStk`、`lastStickAxisValues`）。
  - `Global.Clamp(double, double, double)` 23 件（スティック・トリガーの評価。ホットパス）。
- 定義: `Global.cs` の `public const int MAX_DS4_CONTROLLER_COUNT = 8;`、`TEST_PROFILE_ITEM_COUNT = MAX + 1`、`TEST_PROFILE_INDEX`、`OLD_XINPUT_CONTROLLER_COUNT = 4`、`DS4_STICK_AXIS_MIDPOINT = 128`。`Global.Clamp` は 1 行の純粋関数（`Global.cs:1034`）。
- 全体での利用状況（Step10 の見通し）:
  - 定数 5 つ: 約 253 件、35 ファイル（`BackingStore` 135 が最大。`Mapping` 20、DTO、画面、テストが続く）。
  - `Global.Clamp`: 33 件、5 ファイル（`Mapping` 23、`DS4State` 4、`BezierCurve` 2、`Mouse` 2、`MouseCursor` 2）。
- 状態保持クラス 3 つ（`SyntheticInputState`、`MacroRuntimeState`、`StickMotionState`）にも、配列の長さの定数が各 1 件ある（Step7 で `Mapping` から移した分）。
- 判断の材料:
  - 定数は `const` のため、参照先を替えてもコンパイル後の値は同じ（実行時の性能に影響しない）。
  - `Clamp` は静的な 1 行関数。呼び出し先を替えても、JIT でインライン化され、割り当ては増えない（`MappingHotPathAllocationTests` で確認できる）。

### 2.3 K3: `macroDispatchInFlight`

- 経緯: Controls タブの直接マクロ割り当て（`action == null`）は、押している間、入力ポーリングのたびに `PlayMacro` が呼ばれ続け、二重実行防止が素通りだったため、control 単位の暫定フラグを追加した（既存コメントでは「恒久対応が必要」と申し送られている）。
- 現状（Step7〜8 で `MacroRuntimeState` へ移した後）:
  - 実体: `MacroRuntimeState.DispatchInFlight`（`bool[52]`）と `DispatchLock`。
  - 使用箇所: `Mapping.PlayMacro` の開始部（ロックして確認→ true に設定）と、`PlayMacroTask` の `finally`（ロックして false に戻す）の 2 箇所だけ。
  - テスト: `MappingPlayMacroDispatchGuardTests`（4 件。配列を直接読む）、`Phase7Step7MacroRuntimeStateGuardTests`。
- 恒久対応の本筋は、「トリガー判定層」と「マクロ実行層」の分離、および Controls／SpecialActions に共通の単一の実行層への一本化（`02`／`03` のモデル図の 2-d マクロの分解）。これは `Actions/` のマクロ再生（`DefaultMacroPlayer`）を含む Phase8 の範囲で、Step9 で行うと、`Mapping` の外へ影響が広がる。
- Step9 でできる範囲の改善: ロックと配列の直接操作を `MacroRuntimeState` の 2 メソッド（開始を試みる／終了する）に包み、`Mapping` が `DispatchLock` と配列を直接触らないようにする。挙動は同一。

### 2.4 `AppHost` の暗黙構築と `CreationNote`（台帳の持ち越し）

- `DI/AppHost.GetService<T>()`／`GetService(Type)` は、ホスト未構築のとき暗黙に `CreateHost()` する（`Dispose` の後は作らない）。`CreationNote` はその経路の記録（K7-1 の原因特定用）で、`App.xaml.cs:354` が起動時に Info で出力する。
- 本体の起動は `CreateHost(config, parser)` を明示的に呼ぶため、本体では暗黙構築に来ない。テストには、`CreateHost` を呼ばずに `GetService` で暗黙構築に頼るものがあり得る（`Phase7Step4AppHostGuardTests` が `Dispose` 後の挙動を固定している）。
- 互換 `AppHost` の削除とは独立している。暗黙構築を無くすと、テスト基盤（共有ホスト）の改修が広がる。

---

## 3. 決定事項と質問（推奨つき）

### Q1. 互換 `AppHost` を削除するか
- 推奨: **削除する（A）**。呼び出し元 0 件。ファイルとフォルダーを削除し、基準値とガードを更新する。
- B: 残す。 → 得るものが無い（`DS4Windows.AppHost` と `DS4WinWPF.AppHost` の二重の名前が残る）。

### Q2. `Mapping.cs` の `Global` 参照の再配置先
- 推奨: **A. 新しい静的クラスを 2 つ作る**（1 ファイル 1 型）。
  - `ControllerLimits`（`DS4Windows/DS4Control/ControllerLimits.cs`。定数 5 つ。名前空間は `DS4Windows`）。
  - `NumericUtil`（`DS4Windows/DS4Control/NumericUtil.cs`。`Clamp(double, double, double)`）。
  - `Global` 側は薄いシムとして残す（`Global.MAX_DS4_CONTROLLER_COUNT = ControllerLimits.MaxControllerCount` の const 別名、`Global.Clamp` は `NumericUtil.Clamp` へ委譲）。全体約 253 件の利用箇所は Step10 で順次移す（今回は触れない）。
  - `Mapping.cs` と状態保持クラス 3 つの参照だけを新しい型へ切り替える。
- B: `Mapping` の内部に private の定数・関数を持つ（`ClampInt` と同じ形）。 → 一時しのぎで、Step10 で別の型へ移すことになり二度手間。
- C: 何もしない（Step10 で一括）。 → `Mapping.cs` の 41 件が残り、Step9 の目的を満たさない。
- 名前は仮。承認時に変更可。

### Q3. K3 の扱い
- 推奨: **A. `MacroRuntimeState` に包む（挙動は同一）。恒久の再設計は Phase8 に正式に持ち越す**。
  - `TryBeginDispatch(int controlIndex)`（ロックして確認→未実行なら true にして成功を返す）と `EndDispatch(int controlIndex)`（ロックして false に戻す）を追加。
  - `Mapping.PlayMacro`／`PlayMacroTask` は、この 2 つを呼ぶ。`Mapping` のキャッシュフィールド `macroDispatchInFlight` と `DispatchLock` の直接使用を無くす。
  - `DispatchInFlight`（読み取り）はテストのために公開のまま、`DispatchLock` は非公開にする。
  - ログ（`PlayMacro START GUARD`）、条件、解除のタイミングは変えない。
  - TODO を「Phase8: 実行層の一本化で再設計」に更新する。
- B: 完全な再設計を Step9 で行う。 → `Actions/` と実機確認が必要で、リスクが大きい。Phase8 の範囲。
- C: 触らない（コメントだけ更新）。 → 動作は同じだが、`Mapping` の外から `DispatchLock` が見えたままで、Phase8 の入り口が曖昧。

### Q4. 静的な純粋関数・定数（`getBool*Mapping`、`DS4ControltoInt`、`remainderCutoff`、`MOUSESPEEDFACTOR` ほか）の最終的な置き場所
- 推奨: **A. Step9 では `Mapping` に残し、Step10 の `Global` 再配置と同時に判断する**。外部の利用者は `ControlService`（`getBool*` 29 件）、`MouseCursor`、`ProfileSettingsViewModel` と多く、文字列ガードのテストも依存している。移すだけで、DI 化の目的（状態・依存の解消）には寄与しない。
- B: Step9 で `MappingFunctions` のような静的クラスへ移す。 → 差分が大きく、目的に対する効果が小さい。

### Q5. `AppHost` の暗黙構築と `CreationNote`
- 推奨: **A. Step9 では触らない。Step10 で、テスト基盤の整理とあわせて判断する**。理由: 互換 `AppHost` の削除とは独立しており、テストが暗黙構築に頼っている箇所を洗い出す作業が別に要る。本体の起動は、すでに明示的な `CreateHost` を使う。
- B: 暗黙構築を無くす（`GetService` がホスト未構築なら例外または null）。 → テストの改修が広がり、Step9 の範囲を超える。

---

## 4. Wave 計画

- **W1: 互換 `AppHost` の削除**
  - `DI/Compat/AppHost.cs` を削除（フォルダーも消える）。
  - `Step10FourGlobalRatchetGuardTests`: Compat の行を削除、`AppHost` の合計 5 → 4。
  - `Step12DeadShimRemovalGuardTests`: Wave5 の検査を「互換ファイルが存在しない」に替える（`RemovedFiles` への追加）。
  - 新しいガード `Phase7Step9W1CompatAppHostRemovedGuardTests`: ファイルが無いこと、`DS4Windows` 名前空間に `AppHost` 型が無いこと。
  - 確認: 外部の参照が 0 件（再検索）。
- **W2: `ControllerLimits`／`NumericUtil` の新設と切り替え**
  - 新設 2 ファイル。`Global` の const を別名に、`Global.Clamp` をシムに。
  - `Mapping.cs`（41 件）、`SyntheticInputState`／`MacroRuntimeState`／`StickMotionState`（各 1 件）の参照を切り替える。
  - ラチェット: `Mapping.cs` 41 → 0、保持クラス 3 つ 1 → 0。`Global.*` の合計 399 → **355**。
  - ガード `Phase7Step9W2NoGlobalInMappingGuardTests`: `Mapping.cs` と保持クラス 3 つが `Global.` を参照しないこと。`ControllerLimits` の値が従来と同じこと（8／9／8／4／128）。`NumericUtil.Clamp` の境界値（min 未満、max 超過、範囲内、NaN の扱いが旧と同じ）。
  - 性能: `MappingHotPathAllocationTests` が成功すること。
- **W3: K3 のカプセル化**
  - `MacroRuntimeState` に `TryBeginDispatch`／`EndDispatch` を追加。`Mapping` の 2 箇所を切り替え、キャッシュフィールドを削除。
  - `MappingPlayMacroDispatchGuardTests` は、配列の読み取りで動くため原則そのまま。`Phase7Step7MacroRuntimeStateGuardTests` の `DispatchLock` の検査を更新。
  - 新しいガード: `TryBeginDispatch` の二重呼び出しが 2 回目に失敗すること、`EndDispatch` で解除されること、インデックスが範囲外のときの扱い（旧: 例外を `catch {}` で握りつぶしていたため、同じ挙動を維持）。
- **W4: 文書**
  - 台帳 §12、モデル図（`03` の注釈、`04` の注釈）、`Phase7-Status.md`、完了報告書 `Phase7-Step9-Completion-Report.md`、チェックリスト U-19（下記）。

各 Wave の後に、ユーザーがビルド・テストビルド・テスト実行を行う（エージェントは行わない）。

---

## 5. リスクと対策

- ホットパス（`Clamp` 23 件、配列の長さの定数）
  - 対策: `Clamp` の本体を 1 文字も変えずに移す（`(value < min) ? min : (value > max) ? max : value`）。`MappingHotPathAllocationTests`（割り当てゼロ）と、`MapCustom`／`Commit` の特性化テストで確認する。
- 文字列ガードのテスト（`Mapping.cs` の行やシグネチャを読むもの）
  - 対策: 実装前に `Global.Clamp`／`MAX_DS4_CONTROLLER_COUNT` を含む文字列を検索し、該当テストを洗い出す（Step8 の教訓）。
- `Global` の const を別名にすると、`switch`／属性などの定数式で使っている箇所が影響を受けないか
  - 対策: const から const への代入は定数式のまま。コンパイルで確認される。
- 状態保持クラスで、名前空間やアクセスが変わらないこと。
- ガードレール: 5.2（適用時 Halt）・5.4（On-Demand）・5.6（切断時クリーンアップ）は、触れない。マクロの別スレッド（K3）は、ロックの範囲と順序を変えない。

## 6. 実機確認

- U-19（新設予定、先送り）: マクロ（Controls タブの直接割り当て: 押している間の連打、単発、ホールド）が二重に走らず、離すと止まること。スティック・トリガーの感度とデッドゾーンが従来どおりであること。U-11〜U-18 と同様、ユーザーの判断で先送りしてよい（先送りは記録する）。

## 7. 完了条件

- 互換 `AppHost` が存在しない。`Mapping.cs` と保持クラス 3 つに `Global.` の参照が無い。
- ラチェット: `Global.*` 399 → 355、`AppHost.GetService|Services` 5 → 4、`Program.rootHub` 0、`ServiceProviderHolder` 17（変更なし）。
- K3 のロジックが `MacroRuntimeState` に包まれ、`Mapping` が `DispatchLock` を直接使わない。
- 単体テストが成功（ユーザーが実行）。文書の更新（台帳、モデル図、Status、完了報告書）。

## 8. 持ち越し

- Step10: `Global` の残り（定数 約 250 件の利用箇所、`Clamp` の残り 10 件）の再配置、静的な純粋関数の置き場所（Q4）、暗黙構築と `CreationNote`（Q5）、`BackingStore` のコンストラクター注入、`Mapping` の依存 11 個の整理。
- Phase8: K3 の恒久対応（実行層の一本化）、静的な `SyntheticDispatcher`、`Actions/` の `ServiceProviderHolder` 17。
