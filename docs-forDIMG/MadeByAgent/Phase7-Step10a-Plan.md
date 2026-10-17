# Phase7-Step10a 計画書: `Global` の定数・資源パス・`Clamp` の再配置

作成日: 2026-10-18  
対象ブランチ: `For-DI-migration-work`  
上位計画: `Phase7-Step10-Plan.md`（Q1〜Q9 は推奨どおり承認）§3「10a」、`Phase7-Step9-Completion-Report.md` §5  
状態: **Q1〜Q5 は推奨どおり承認（2026-10-18）。W1〜W4 完了（ビルド・テスト成功）。W5 は文書。10a 完了。完了報告書は `Phase7-Step10a-Completion-Report.md`。**

---

## 1. 目的と位置づけ

- Step9 で新設した `ControllerLimits`（コントローラー数などの定数）と `NumericUtil`（`Clamp`）へ、`Global` の利用箇所をすべて切り替え、`Global` の定数・シムを削除する。あわせて、画面の資源パスの定数（`RESOURCES_PREFIX`、`ASSEMBLY_RESOURCE_PREFIX`）を画面側の `ResourcePaths` へ移す。
- 機械的な置き換えで、定数の値と `Clamp` の計算は変えない。機能・ログへの影響はない。
- 目標: `Global.*` **355 → 107**（定数・資源パス・`Clamp` を削除。計画時は 100 と見積もったが、数え違いで、実測は 107）。`AppHost` 4、`rootHub` 0、`ServiceProviderHolder` 17 は変わらない。

---

## 2. 調査結果

### 2.1 対象の参照（コメントを除く、リポジトリ全体）

- 本体（`DS4Windows`）: 定数 246 件、`Clamp` 9 件。
  - `BackingStore.cs` 132（`TEST_PROFILE_ITEM_COUNT` 114、`MAX_DS4_CONTROLLER_COUNT` 18）。
  - `ProfileEditor.xaml.cs` 32（`ASSEMBLY_RESOURCE_PREFIX` 29、`TEST_PROFILE_INDEX` 3）。
  - `AppSettingsDTO.cs` 14、`TrayIconViewModel.cs` 12、`DS4LightBar.cs` 7、`ImageLocationPaths.cs` 6、`MacroStepItem.cs` 6、`ResourcePaths.cs` 5、`Global.cs` 4（内部の参照）、`AutoProfileEntrySerializer.cs` 4、`CompositeDeviceModel.cs` 4。
  - 少数（各 1〜3）: `EnvironmentService`（3）、`BindingWinResourcePaths`（3）、`ControlService`（2）、`App.xaml.cs`、`ActionEntry`、`DefaultActionManager`、`DefaultMacroPlayer`、`AutoProfileEntity`、`DeviceStateService`、`OutputSlotService`、`BindingWindow.xaml.cs`、`MainWindow.xaml.cs`、`RecordBoxResourcePaths`、`WelcomeDialogResourcePaths`、`ProfileSettingsViewModel`。
  - `Clamp` の残り: `DS4State` 4、`Mouse` 2、`MouseCursor` 2、`BezierCurve` 1。
- テスト（`DS4WindowsTests`）: 修飾つきの参照 46 件（15 ファイル。`PatternCViewModelTests` 10、`ProfileSettingsViewModelTargetDeviceTests` 7、`ControllerSlotLimitTests` 5 ほか）。Step9 で新設した `Phase7Step9W2NoGlobalInMappingGuardTests` の 5 件（`Global` の別名が `ControllerLimits` と一致することの検査）は、別名の削除とともに検査の対象ごと更新する。
- XAML には `Global` の定数への参照はない（`x:Static` はすべてシステム側）。
- 修飾なしで使われている箇所（`using static DS4Windows.Global;` 経由）: `Mapping.cs` と `DS4LightBar.cs` に宣言があるが、定数の修飾なしの使用は 0 件。`Global.cs` 内の自己参照のみ。

### 2.2 別名を持つ型（定数の再宣言）

- `ControlService.MAX_DS4_CONTROLLER_COUNT`（`public const int ... = Global.MAX_DS4_CONTROLLER_COUNT`。クラス内で 22 件、外部からの参照あり）→ 名前を維持し、値を `ControllerLimits.MaxControllerCount` から取る。
- `ProfileSettingsService` の `TEST_PROFILE_ITEM_COUNT = 9`／`MAX_DS4_CONTROLLER_COUNT = 8`（リテラルの再宣言。使用は計 12 件）→ 値の食い違いを防ぐため、`ControllerLimits` から取る。

### 2.3 削除禁止の防壁（ガード）

- `Step12DeadShimRemovalGuardTests.ProtectedInGlobal` が、`MAX_DS4_CONTROLLER_COUNT`、`TEST_PROFILE_ITEM_COUNT`、`TEST_PROFILE_INDEX`、`ASSEMBLY_RESOURCE_PREFIX`、`RESOURCES_PREFIX`、`DS4_STICK_AXIS_MIDPOINT`、`Clamp` を「`Global` に残っていること」として固定している（Phase6 の防壁 4）。10a の最後にこれらを `RemovedFromGlobal`（再導入の禁止）へ移す。
- `Step10GroupOne／GroupTwo／FourTwoGlobalReferenceGuardTests` は、ファイルごとの許可リスト（定数名を含む）。許可リストは「超えない」検査のため、参照を減らしても失敗しない。名前を整理する必要があるものは更新する。
- `MappingStep3FinalGlobalReferenceGuardTests` は `Mapping.cs` に `using static DS4Windows.Global;` があることを検査している。`Mapping.cs` は `Global` を使わなくなったが、この宣言は 10d（`Global` の削除）まで触らない。

### 2.4 資源パスの定数

- `ResourcePaths`（`DS4WinWPF.DS4Forms`、`ResourcePaths.cs`）、`ImageLocationPaths`、`BindingWinResourcePaths`、`RecordBoxResourcePaths`、`WelcomeDialogResourcePaths` は、いずれも `Global.RESOURCES_PREFIX` で文字列を組み立てる画面側の型。
- `Global.cs` 内部の `iconChoiceResources`（トレイアイコンの資源の表。`AppearanceSettingsService` が使う）も `RESOURCES_PREFIX` を使う。この表は 10b で画面側へ移す対象のため、10a では `ResourcePaths` の定数を一時的に参照する（TODO を付ける）。

---

## 3. 決定事項と質問（推奨つき）

### Q1. 資源パスの定数の置き場所と名前
- 推奨: **A. `ResourcePaths` に `public const string ResourcesPrefix`／`AssemblyResourcePrefix` として置く**（値は現在と同じ文字列）。画面側の他の型（`ImageLocationPaths` ほか）は `ResourcePaths.ResourcesPrefix` を参照する。`Global.iconChoiceResources` は 10b で移すまで、一時的に同じ定数を参照する（`TODO(Phase7-Step10b)` を付ける）。
- B: 新しい静的クラス `ResourcePrefixes` を作る。 → 型が 1 つ増えるだけで、利点が小さい。

### Q2. 再宣言された定数（`ControlService`、`ProfileSettingsService`）
- 推奨: **A. `ControlService.MAX_DS4_CONTROLLER_COUNT`（公開の名前）は維持して値を `ControllerLimits` から取る。`ProfileSettingsService` のリテラルは `ControllerLimits` から取る形に替える**。
- B: 触らない。 → `ProfileSettingsService` のリテラルは、`ControllerLimits` が変わっても追従しない。

### Q3. `Global` の別名とシムの削除の時期
- 推奨: **A. 10a の最後の Wave で削除する**（利用箇所がすべて置き換わってから。`Step12DeadShimRemovalGuardTests` の防壁を `RemovedFromGlobal` へ移す）。
- B: 10d（`Global` の削除）まで残す。 → 新規コードが誤って旧名を使えてしまう。

### Q4. Wave の構成
- 推奨: **A. 次の 5 Wave。各 Wave の後にユーザーがビルド・テストを実行する**。
  - W1: `BackingStore.cs`（132 件。1 ファイル）。
  - W2: 制御層・DTO・サービス・Actions（`AppSettingsDTO`、`DS4LightBar`、`AutoProfileEntrySerializer`、`EnvironmentService`、`ControlService`、`DeviceStateService`、`OutputSlotService`、`App.xaml.cs`、`MainWindow.xaml.cs`、`ProfileSettingsService` のリテラルほか）。
  - W3: 資源パス（`ResourcePaths` の定数新設、画面側 12 ファイル。74 件）。
  - W4: `Clamp` の残り 9 件、テスト 46 件の置き換え、`Global` の別名とシムの削除、ガードの更新、ラチェットの更新（355 → 107）。
  - W5: 文書（台帳 §13、モデル図 03 の注釈、Status、完了報告書）。
- B: `BackingStore` を最後にする。 → 最大の差分が最後に残り、リスクが後ろに寄る。

### Q5. `using static DS4Windows.Global;`（`Mapping.cs`、`DS4LightBar.cs`）
- 推奨: **A. 10a では残す**。定数の修飾なしの使用は 0 件で、実害はないため。10d で `Global` を削除するときに、ガード（`MappingStep3FinalGlobalReferenceGuardTests`）と一緒に外す。
- B: 10a で外す。 → ガードの書き換えが必要になり、10a の範囲が広がる。

---

## 4. Wave 計画（Q1〜Q5 が推奨どおりの場合）

- **W1: `BackingStore.cs`**
  - `Global.TEST_PROFILE_ITEM_COUNT`／`MAX_DS4_CONTROLLER_COUNT` などを `ControllerLimits.*` に置き換え（スクリプト。コメント行は除外）。
  - ラチェット: `BackingStore.cs` 159 → 約 27（定数以外の `Global.` 参照が残る）。実数は実装時にスクリプトで確認する。
- **W2: 制御層ほか**
  - 同様の置き換え。`ControlService.MAX_DS4_CONTROLLER_COUNT` と `ProfileSettingsService` のリテラルを `ControllerLimits` から取る形に。
  - 新しいガード `Phase7Step10aW2ControllerLimitsGuardTests`: 対象ファイルが `Global.<定数>` を参照しないこと。`ControlService.MAX_DS4_CONTROLLER_COUNT` と `ProfileSettingsService` の定数が `ControllerLimits` と一致すること。
- **W3: 資源パス**
  - `ResourcePaths` に定数を追加。画面側の 12 ファイルを置き換え。`Global.iconChoiceResources` は一時的に参照（TODO）。
  - ガード: 画面側のファイルが `Global.RESOURCES_PREFIX`／`ASSEMBLY_RESOURCE_PREFIX` を参照しないこと。定数の値が従来と同じ文字列であること。
- **W4: `Clamp`、テスト、別名の削除**
  - `DS4State`／`Mouse`／`MouseCursor`／`BezierCurve` を `NumericUtil.Clamp` に。テスト 46 件を `ControllerLimits` に。
  - `Global` の定数 5 つと `Global.Clamp` を削除。`Step12DeadShimRemovalGuardTests` の `ProtectedInGlobal` から該当の名前を外し、`RemovedFromGlobal` へ追加。
  - `Phase7Step9W2NoGlobalInMappingGuardTests` から、別名の一致の検査を除く（`ControllerLimits` の値の検査と `Clamp` の検査は残す）。
  - ラチェットの基準値を更新（合計 355 → 100）。
- **W5: 文書**

各 Wave の後に、ユーザーがビルド・テストビルド・テスト実行を行う（エージェントは行わない）。

## 5. リスクと対策

- 置き換えの取りこぼし: スクリプトで置換後、`Global.<定数>` の残りを再検索して 0 件を確認する。コメント行の例外処理は Step9 と同じ。
- `const` を経由しない参照（属性、`switch` の `case`、既定値）: `const` から `const` への代入は定数式のまま。ビルドで確認される。
- テストの文字列ガード（`Global.MAX_DS4_CONTROLLER_COUNT` を文字列で探すもの）: 実装前に検索し、W4 で更新する。
- 画面側の型（`ImageLocationPaths` ほか）は XAML から `{Binding}` で参照される。プロパティの名前と値は変えない（式の中の定数の参照先が替わるだけ）。
- ガードレール: 触れない（定数と純粋計算のみ）。ホットパスの `Clamp` は Step9 で `NumericUtil` 化済みで、ここで残りの呼び出し元を替えても、割り当ては増えない。

## 6. 実機確認

- 先送り（U-11〜U-19 とあわせて、10e の総合検証で一括）。10a は定数の参照先の変更のみで、実機でしか確認できない項目はない。画面の資源（トレイアイコン、プロファイル編集の画像）が表示されることは、起動して確認できる（U-20 候補とは別に、10e の確認項目へ追記する）。

## 7. 完了条件

- `Global` に定数 5 つと `Clamp` が存在しない。`Global.*` 107。画面の資源パスの定数が `ResourcePaths` にある。
- ガード（`Phase7Step10aW2ControllerLimitsGuardTests` ほか）とラチェットが成功（ユーザーが実行）。文書の更新。

## 8. 持ち越し

- 10b: 表と文字列変換（`getX360ControlString`、`macroDS4Values`、`iconChoiceResources`、`defaultButtonMapping`、`reverseX360ButtonMapping`、名前表）の再配置、契約の再判断、孤立定数。
- 10d: `using static DS4Windows.Global;` の削除と、`MappingStep3FinalGlobalReferenceGuardTests` の更新。
