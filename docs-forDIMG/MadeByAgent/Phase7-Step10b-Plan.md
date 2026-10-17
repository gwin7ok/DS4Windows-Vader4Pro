# Phase7-Step10b 計画書: 持ち越し事項の消化と、`Global` の表・純粋関数の再配置

作成日: 2026-10-18  
対象ブランチ: `For-DI-migration-work`  
上位計画: `Phase7-Step10-Plan.md`（Q1〜Q9 は推奨どおり承認）§3「10b」、`Phase7-Step10a-Completion-Report.md` §5、`Phase7-Residual-Ledger.md` §3・§13  
状態: **Q1〜Q8 は推奨どおり承認（2026-10-18）。W1〜W4 完了（ビルド・テスト成功）。W5 は文書。10b 完了。完了報告書は `Phase7-Step10b-Completion-Report.md`。**

---

## 1. 目的と位置づけ

- 10a で `Global` の定数・`Clamp` は片づいた。10b は、`Global` に残る**表・文字列変換・色の純粋関数**を、用途ごとの型へ移し、あわせて Step2・Step6・Step9 から 10 へ持ち越した**契約の再判断**と**孤立定数**を消化する。
- 機能・ログは変えない。表の中身・計算は 1 文字も変えず、置き場所と呼び出し元だけを替える。
- 目標: `Global.*` 107 → 約 80（見積もり。実測は 93）。`AppHost` 4、`rootHub` 0、`ServiceProviderHolder` 17 は変わらない。
- 永続化に属するもの（`configFileDecimalCulture`、`CompileVersionNumber*`、設定の定数）は 10c、`Global.store` と環境・パスの窓口は 10d で扱う。10b では触らない。

---

## 2. 調査結果

### 2.1 表と文字列変換（`Global.cs`）

- 利用箇所（コメントを除く）:
  - `getX360ControlString`（2 つのオーバーロード）: `PressKeyViewModel` 7、`MappedControl` 3、`BackingStore` 2。
  - `xboxDefaultNames`／`ds4DefaultNames`／`ds4inputNames`: `BindingWindow.xaml.cs` 各 1（`Global.cs` 内部の `getX360ControlString` も使う）。
  - `macroDS4Values`: `RecordBoxViewModel` 1。
  - `defaultButtonMapping`: `PresetMenuHelper` 2、`ProfileSettingsService` 1。
  - `reverseX360ButtonMapping`: `ProfileSettingsService` 1、テスト 1。
  - `iconChoiceResources`: `AppearanceSettingsService` 1（`IAppearanceSettingsService.GetIconResourcePath` の実装）。10a で `ResourcePaths` の定数を一時的に参照するようにした（下位層から上位層の参照。TODO 付き）。
- テスト: `Step10GroupTwo／Three／FourTwoGlobalReferenceGuardTests`（ファイルごとの許可リストの名前）、`ProfileSettingsServiceReverseX360ButtonMappingTests`、`MappingControlPathGlobalReferenceGuardTests`。許可リストは「超えない」検査のため、参照が減っても失敗しないが、名前の文字列を含むガードは更新が要る。

### 2.2 色の純粋関数

- `getTransitionedColor`（`DS4LightBar` 8、`Mapping` 2）、`HuetoRGB`（`DS4LightBar` 4、`DS4Color` 2）、private の `applyRatio` 2 種（`Global.cs` 内）。`DS4LightBar.cs` と `Mapping.cs` は `using static DS4Windows.Global;` で修飾なしに呼ぶ。
- 状態を持たない純粋な計算で、ライトバーの更新（入力処理の経路の近く）から呼ばれる。割り当てを増やさないこと。

### 2.3 持ち越しの契約（`copilot-instructions` §2.4 の手順で再判断）

- **`ProfileSubSettingBase.OnSubPropertyChanged`**:
  - 経緯: Phase5-Step14（`36d124a6`、2026-09）で、サブ設定の変更を親のプロファイルコンテナへ中継するために追加。唯一の購読者は `IProfileSettingsService.WireSubSettingsEvents` 経由の「プロファイル編集画面の適用ボタンの活性化」で、Step2 W2（Q2＝案 A、承認済み）でその購読の仕組みを削除した。以後、本体に購読者はいない（テスト 2 件が直接購読）。
  - 判断: **使うべきでないもの**（中継先が廃止済みの孤立イベント）。基底クラスの `PropertyChanged`（画面のバインディングが使う）は残す。
- **`ProfileApplicationService` の依存 `IDeviceStateService`**:
  - 経緯: `234e20f9`（DI コンテナ化の途中）でコンストラクターに追加。フィールド `_deviceState` を保持するだけで、使われていない。
  - 判断: **使うべきでないもの**（未使用の依存）。コンストラクターから外す（`IDeviceStateService` 自体は他の利用者があり残す）。
- **`IEnvironmentService.UsingMaxControllers`**:
  - 経緯: Phase6-Step2-1b（決定 O2）で `ControlService.USING_MAX_CONTROLLERS`（静的シム）の置換先として追加。Step2 の調査で「接続」と判断し、TODO 付きで残した。
  - 判断: **使うべきもの**。呼び出し元 3 箇所を注入に切り替える: `AutoProfilesDTO.cs:52`（静的な変換メソッドの中）、`AutoProfilesViewModel.cs:85`（`UsingExpandedControllers`）、`ProgramItem.cs:227`（`ExpandedControllersVisible`）。切り替え後に `ControlService.USING_MAX_CONTROLLERS` を削除する。
  - 新たな発見: 同じ静的シムの仲間の `ControlService.CURRENT_DS4_CONTROLLER_LIMIT` は、**30 箇所以上（16 ファイル）**から使われている（`BackingStore` 7、`ProfileEditor` 5、`CheckBatteryViewModel` 3、`MainWindow` 2、`OutputSlotManager` 3 ほか。テスト 5）。10b の範囲を超えるため、10d（`BackingStore` のコンストラクター注入で `BackingStore` の分が片づく）と 10e で扱う（Q4）。
- **`IOutputSlotService.OutputSlotChanged`**:
  - 経緯: Phase5-Step12 で UI 通知の拡張点として追加（Phase6-Step5-2 でイベント発行を分離）。抜き差しの要求の実行後に発行し、トレースログ `[DI] OutputSlotService.OutputSlotChanged: Slot … = …` を出す。購読者は 0 件。
  - 判断: **温存**（Q5）。購読者を付けるのは機能拡張で、移行の範囲外。削除するとトレースログが消える。TODO を「Phase8 以降の UI 通知」に更新する。

### 2.4 孤立定数（`BackingStore.cs`、参照 0 件）

- 6 件（台帳の「8 件」は Step1 以降の変化で現在は 6 件）:
  - `DEFAULT_CHECK_EVERY_VALUE`（1）、`DEFAULT_CHECK_EVERY_UNIT`（1）: 実際の既定値はフィールド初期化子 `checkEveryValue = 1`／`checkEveryUnit = 1` に重複して書かれている。→ **使うべきもの**（既定値の単一の出所）。フィールド初期化子から定数を参照する形に接続する。
  - `DEFAULT_STICK_OUTPUT_CURVE`／`_ID`、`DEFAULT_SA_OUTPUT_CURVE`／`_ID`: 追加は `59d8e49a`（プロファイル用 `ProfileDTO` の既定値の強制）。`ProfileDTO` は Phase6-Step12 W6 で削除済み。→ **使うべきでないもの**（廃止済みの DTO の残骸）。削除する。

### 2.5 `MainWindow` のコンストラクター引数 18 個

- 18 個すべてが `MainWindow.xaml.cs` で実際に使われている（未使用の依存はない）。引数が多いのは、画面が多くの責務を持つため。
- 引数をまとめる（ファサードの導入）と、画面の責務の再設計になり、Phase7 の目的を超える。

---

## 3. 決定事項と質問（推奨つき）

### Q1. 表と文字列変換の再配置先（名前は仮）
- 推奨: **A. 用途ごとに 3 つの静的クラスへ移し、`Global` から削除する**（表の中身は変えない）。
  - `X360ControlTables`（`DS4Control/`）: `defaultButtonMapping`、`reverseX360ButtonMapping`、`xboxDefaultNames`、`ds4DefaultNames`、`getX360ControlString`（2 つ）。
  - `DS4ControlNames`（`DS4Control/`）: `ds4inputNames`、`macroDS4Values`。
  - `TrayIconResources`（`DS4Forms/`）: `iconChoiceResources`。`AppearanceSettingsService.GetIconResourcePath` が参照する（サービスから画面側の型への参照が残る。`IAppearanceSettingsService` が `TrayIconChoice` を受け取り、画面側が解決する形への整理は Phase8 以降）。
- B: 1 つの `ControlTables` にまとめる。 → 責務の異なる表が混ざる（1 ファイル 1 型の原則は満たすが、肥大化する）。

### Q2. 色の純粋関数の再配置
- 推奨: **A. `ColorUtil`（`DS4Control/`、静的クラス）へ移す**（`getTransitionedColor`、`HuetoRGB`、`applyRatio`×2）。本体は 1 文字も変えず、`AggressiveInlining` は付けない（`Global` では付いていない。ライトバーの更新は入力の経路の外で、割り当てを増やさない）。`DS4LightBar.cs`／`Mapping.cs` の `using static DS4Windows.Global;` による修飾なしの呼び出しは、`ColorUtil.` に修飾する。
- B: `DS4Color` のメンバーにする。 → `DS4Color` は構造体で、ライブラリ層（第 1 層）。依存方向は問題ないが、`Mapping` の `Color`（WPF）を使う関数も含むため分割が要る。

### Q3. `ProfileSubSettingBase.OnSubPropertyChanged` の扱い
- 推奨: **A. イベントを削除する**（`PropertyChanged` は残す）。テスト 2 件（`ProfileActionsSyncTests`、`SensitivitySettingsTests`）は `PropertyChanged` の検査に絞る。→ **機能廃止にあたらない内部イベントだが、念のため承認を求める**。
- B: 温存して TODO を付ける。 → 購読者のいない孤立イベントが基底クラスに残る。

### Q4. `ControlService` の静的シム
- 推奨: **A. 10b では `USING_MAX_CONTROLLERS`（3 箇所）だけ切り替えて削除する。`CURRENT_DS4_CONTROLLER_LIMIT`（30 箇所以上）は 10d／10e の新しい項目として台帳に登録する**。
- B: 10b で両方を切り替える。 → 16 ファイル、画面・`BackingStore`・テストに波及し、10b の範囲を大きく超える。
- 補足（`USING_MAX_CONTROLLERS` の注入方法）: `AutoProfilesViewModel` は `ViewModelFactory` から `IEnvironmentService` を受ける。`ProgramItem` は生成側（`AutoProfilesViewModel`）から値を渡す。`AutoProfilesDTO` の静的な変換メソッドは、呼び出し元（`AutoProfileHolder`）から真偽値を引数で受ける形にする。いずれも値は従来と同じ（`EnvironmentService.ProcessControllerSlotLimit == 8`）。

### Q5. `IOutputSlotService.OutputSlotChanged` の扱い
- 推奨: **A. 温存し、TODO を更新する**（トレースログを維持する）。
- B: 削除する。 → トレースログが消える。購読者が必要になったら再追加になる。

### Q6. 孤立定数 6 件
- 推奨: **A. `DEFAULT_CHECK_EVERY_VALUE`／`_UNIT` はフィールド初期化子に接続する。曲線の 4 件は削除する**。
- B: すべて温存する。 → Phase6-Step12 の判断（防壁 1・4 による温存）を引き継ぐが、廃止済みの DTO の残骸が残る。

### Q7. `MainWindow` の引数 18 個
- 推奨: **A. 変更しない。台帳に記録し、画面の責務の整理（Phase8 以降）で扱う**。
- B: ファサードで束ねる。 → 画面の再設計になる。

### Q8. Wave の構成
- 推奨: **A. 次の 5 Wave。各 Wave の後にユーザーがビルド・テストを実行する**。
  - W1: `X360ControlTables`／`DS4ControlNames`（表と変換の移動。`BackingStore`、`ProfileSettingsService`、画面、テストの切り替え）。
    - 調査で判明: 1 引数の `getX360ControlString`（XML 保存用の名前。`BackingStore` のインスタンスメソッドへ `Global` が委譲していた）も状態を持たない純粋な switch のため、`X360ControlTables` へ移した（`BackingStore` から削除。呼び出しは `BackingStore.cs:1539` と `PressKeyViewModel`）。
  - W2: `TrayIconResources`／`ColorUtil`（アイコンの表、色の関数）。
  - W3: 契約の整理（`OnSubPropertyChanged` の削除、`ProfileApplicationService` の未使用の依存、`OutputSlotChanged` の TODO 更新）。
  - W4: `UsingMaxControllers` の接続（3 箇所）と `USING_MAX_CONTROLLERS` の削除、孤立定数の整理。
  - W5: 文書（台帳、モデル図、Status、完了報告書）。
- B: 契約の整理を先に行う。 → 順序の入れ替えは可能（独立）。

---

## 4. リスクと対策

- 表の移動: 中身をコピーせず、宣言ごと移す（`git diff` で移動を確認できる形）。`X360Controls` ほかの列挙型の順序依存（`defaultButtonMapping` は `DS4Controls` の並びと対応）を変えない。ガード: 移動後の配列の長さ・要素が従来と一致すること（特性化テスト）。
- 色の関数: ホットパスではないが、ライトバーの更新で毎フレーム呼ばれる。ガード: 既存の結果と一致すること（境界値と代表値）。
- `UsingMaxControllers` の切り替え: 静的な値（プロセス起動時に決まる）を、注入された同じ値に替える。ガード: 切り替え前後で、`AutoProfilesDTO` が 4〜8 番目のコントローラーを書く条件が同じこと。
- `ProfileApplicationService` のコンストラクター変更: `ServiceRegistration`（コンテナ経由）と `TestRepositoryFactory` の 1 箇所だけ。
- 件数を固定するガード（Step10a の教訓）: 実装前に、`Assert.Equal(<件数>, …)` を含む参照ガード（`Step10Group*GlobalReferenceGuardTests` など）を洗い出す。
- ガードレール: 触れない。

## 5. 実機確認

- 先送り（10e の総合検証で一括）。10b で実機でしか確認できない項目はない。10e の確認項目に、トレイアイコンの切り替え（色の選択）、自動プロファイルの画面（拡張コントローラー 8 台の表示）、ライトバーの色の遷移を加える。

## 6. 完了条件

- `Global` に表・変換・色の関数が存在しない。`Global.*` 93（見積もりは約 80）。孤立定数 6 件が整理されている。`OnSubPropertyChanged`、`ProfileApplicationService` の未使用の依存、`USING_MAX_CONTROLLERS` が削除されている。
- ガードとラチェットが成功（ユーザーが実行）。文書の更新（台帳 §14、モデル図、Status、完了報告書）。

## 7. 持ち越し

- 10c: `configFileDecimalCulture`、`CompileVersionNumber*`、設定の定数、`LoadActions`／`SaveAction` などの薄いシム。
- 10d: `Global.store`、環境・パスの窓口（`exeversion` ほか）、`ProfileSettingsServiceInstance` 経由の設定の窓口（`useDInputOnly` ほか）、`using static DS4Windows.Global;`、`CURRENT_DS4_CONTROLLER_LIMIT`（`BackingStore` の分）。
- 10e: `CURRENT_DS4_CONTROLLER_LIMIT`（画面の分）、`AppHost` の暗黙構築、`Mapping` の依存の整理、`MainWindow` の引数の記録。
