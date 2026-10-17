# Phase7-Step10 計画書: 仕上げ（`Global` の解体の完了、`BackingStore` の永続化の切り出し、Phase7 の総合検証）

作成日: 2026-10-18  
対象ブランチ: `For-DI-migration-work`  
上位計画: `Phase7-Plan.md` §2（Step10）、`Phase7-0-Survey.md` §3.2・§5（D2・D4）、`Phase7-Residual-Ledger.md` §1〜§12、`Phase7-Step9-Completion-Report.md` §5  
状態: **Q1〜Q9 は推奨どおり承認（2026-10-18）。10a・10b・10c は完了（2026-10-18。`Phase7-Step10a-Completion-Report.md`、`Phase7-Step10b-Completion-Report.md`、`Phase7-Step10c-Completion-Report.md`）。次は 10d の計画書の作成。**

---

## 1. 目的と位置づけ

- Phase7 の最後の Step。Step3〜9 で、サービス・起動・`Program.rootHub`・`Mapping` の解体を終えた。残りは次の 3 つ。
  - `Global` に残る参照 355 件（約 7 割が定数・資源パス）と、`Global.store`（`BackingStore` の静的な生成）の解消。
  - `BackingStore`（6,572 行）の永続化の切り出し（`LegacyProfileXmlSerializer` ほか）と、コンストラクター注入。
  - 持ち越し事項の消化と、Phase7 の総合検証・完了報告。
- Step10 は規模が大きい（ファイル数約 40、`BackingStore` の約 3,300 行の移動を含む）。Step3 の 3a〜3c の前例にならい、**サブ Step 10a〜10e に分け**、各サブ Step を個別の計画書（`Phase7-Step10a-Plan.md` ほか）と Wave で進める。本書は全体の構成と、先に決めておくべき決定事項（Q1〜Q9）を扱う。
- 機能・ログ・安全弁は変えない。保存形式は現状維持（D4＝F1。承認済み）。

---

## 2. 調査結果

### 2.1 `Global` の残り（355 件、`Global.cs` は 1,076 行）

- 定数・資源パスが約 7 割:
  - `TEST_PROFILE_ITEM_COUNT` 116（`BackingStore` 114）、`MAX_DS4_CONTROLLER_COUNT` 49（`BackingStore` 18、`AppSettingsDTO` 14、`DS4LightBar` 7、`AutoProfileEntrySerializer` 4）、`TEST_PROFILE_INDEX` 5、`OLD_XINPUT_CONTROLLER_COUNT` 2。→ Step9 で新設した `ControllerLimits` へ機械的に置き換えられる（計 172）。
  - `RESOURCES_PREFIX` 37（`TrayIconViewModel` 12、`ImageLocationPaths` 6、`MacroStepItem` 6、`ResourcePaths` 5、`CompositeDeviceModel` 4 ほか）、`ASSEMBLY_RESOURCE_PREFIX` 30（`ProfileEditor` 29）。→ 画面側の資源パスの型（`ResourcePaths`）へ（計 67）。
  - 純粋計算 `Clamp` の残り 9（`DS4State` 4、`Mouse` 2、`MouseCursor` 2、`BezierCurve` 1）→ `NumericUtil`。
- 残りの約 107 件（定数・資源・`Clamp` を除く）:
  - 設定の定数: `CONFIG_VERSION`、`APP_CONFIG_VERSION`、`XML_EXTENSION`、`CUSTOM_EXE_CONFIG_FILENAME`、`PROBING_PATH`、`LANGUAGE_ASSEMBLY_NAME`、`configFileDecimalCulture`（9）。
  - 表と文字列変換: `getX360ControlString`（10）、`macroDS4Values`、`iconChoiceResources`、`defaultButtonMapping`、`reverseX360ButtonMapping`、`xboxDefaultNames`／`ds4DefaultNames`／`ds4inputNames`（`Global.cs` 内部）。
  - 環境・パス（`PathService`／`EnvironmentService` に実体があり、`Global` は窓口）: `exeversion` 11、`appdatapath` 6、`exedirpath` 2、`exelocation` 1、`IsAdministrator`、`AdminNeeded`、`GetInstanceIdFromDevicePath`、`CheckIfVirtualDevice` ほか。
  - 設定の窓口（`ProfileSettingsServiceInstance` 経由）: `useDInputOnly`、`useTempProfile`、`tempprofileDistance`、`DebouncingMs`、`UseDs3PitchRollSim`、`UseMoonlight`、`UseAdvancedMoonlight`（主に第 1 層の `DS4Devices`／`DS4Device`／`DS4SixAxis`／`HidDevices` が読む）。
  - `Global.store` と `m_Config`（`BackingStore` の静的な生成。参照 75 件、うち `Global.cs` 内 36）。
  - 薄いシム: `SaveAction`／`RemoveAction`／`LoadActions`／`GetAction`／`CreateStdActions`／`CreateAutoProfiles`／`SetCulture`／`RefreshActionAlias` ほか（呼び出し元は各サービス 1 件ずつ）。
  - イベント: `DeviceSerialChange`、`BatteryChanged` の窓口。
- 第 1 層（`DS4Devices`／`DS4Device`／`DS4SixAxis`／`HidDevices`）の `Global` 参照は、下位層から上位層への直接参照にあたる（依存方向の違反）。サービスの注入に直接置き換えられない（第 1 層が DI を知らない）ため、**設定値を関数・オプションとして上位から渡す形**（Step5 の `DS4SixAxis` のジャイロトリガー判定の関数注入と同じ）が必要。

### 2.2 `BackingStore`（6,572 行）

- 構成（行数の目安）: 状態のフィールドとプロパティ 約 2,200 行、`SaveProfile` 約 800、`LoadProfile` 約 2,300、`Load`（アプリ設定の読込）約 430、`Save` 約 160、`LoadActions`／`SaveActions` 系 約 450、リンクプロファイル 約 190、コントローラー設定 約 200、レガシー拡張設定 約 230、既定プロファイル 約 450。
- 永続化の切り出し（`LegacyProfileXmlSerializer` ほか）の条件が整っている:
  - 呼び出し元は、`ProfileRepository`（5）、`ProfileXmlStore`（4）、`App` 1、`ControlService` 1 など、少数。薄い層（`IProfileXmlStore`／`IProfileRepository`）を介している。
  - Step6 の調査で、`SaveProfile` は `Global.` 7 件・`control.` 0 件、`LoadProfile` は `Global.` 7 件・`control.` 5 件と、結合が薄い。
- 既存のテスト: `ProfileLegacyXmlCompletenessTests`（旧形式の要素の欠落）、`ProfileRepositoryTests`、`Phase7Step3cApplyProfileCharacterizationTests`。**往復のゴールデンテスト（保存→読込→保存で一致）は未整備**。切り出しの前に整えることを必須とする。
- `BackingStore` の依存の注入: `IProcessInspector`（プロパティ）、`ActiveOutDevType`（プロパティ）、`PathService`（省略時の `Global.store`）、`Global.exedirpath` などの窓口。これらは永続化を切り出すときにコンストラクター注入へ（`BackingStore.cs` の TODO、`PathService.cs` の TODO）。

### 2.3 持ち越し事項（台帳より）

- 契約の再判断: `ProfileSubSettingBase.OnSubPropertyChanged`（購読者がいなくなった）、`ProfileApplicationService` の未使用の依存 `IDeviceStateService`、`IEnvironmentService.UsingMaxControllers`（接続予定 Step10）、`IOutputSlotService.OutputSlotChanged`（購読者 0 件）。
- 孤立定数 8 件（`BackingStore`）。
- `MainWindow` のコンストラクター引数 18 個。
- `AppHost` の暗黙構築と `CreationNote`（Step9 Q5＝A で Step10 へ）。
- `Mapping` のコンストラクターの依存 11 個。静的な純粋関数（`getBool*Mapping`、`DS4ControltoInt`、`remainderCutoff`、`MOUSESPEEDFACTOR` ほか）の置き場所（Step9 Q4＝A で Step10 へ）。
- `Global.ProfileSettingsServiceInstance` とそのフォールバック。
- 名前空間＝フォルダ（最終目標）の扱いの判断。`BackingStore` の分解の範囲の再評価（D2）。モデル図の更新。Phase7 の総合検証と完了報告。

### 2.4 名前空間の現状

- 475 ファイル中、`DS4Windows` 183、`DS4WinWPF.DS4Forms` 98、`DS4WinWPF.DS4Forms.ViewModels` 47、`DS4Windows.Actions` 44、`DS4Windows.DI` 28、`DS4Windows.Services` 23 ほか。フォルダーと一致しない型が多数ある。
- 名前空間の変更は、`using` の追加、XAML の `xmlns`、リフレクション・文字列ガードのテストに波及する。copilot-instructions §3.3 原則 3 は、全移行完了後のクリーンアップで行うと定めている。

---

## 3. サブ Step の構成（案）

- **10a: `Global` の定数・資源パス・純粋計算の再配置**（機械的で低リスク）
  - `ControllerLimits` へ 172 件、画面の資源パスの型へ 67 件、`NumericUtil` へ 9 件。`Global` の別名・シムを削除。
  - 目標: `Global.*` 355 → 約 107。→ 10a 完了後の実測は 107。
- **10b: 持ち越し事項の消化**（§2.3 の契約の再判断、孤立定数、`MainWindow` の引数、静的な純粋関数の置き場所、表と文字列変換の再配置）
- **10c: `BackingStore` の永続化の切り出し**（最大。ゴールデンテストが先）
  - 10c-1: ゴールデンテスト（プロファイル、アプリ設定、アクション、リンクプロファイル、コントローラー設定）。
  - 10c-2: `LegacyProfileXmlSerializer`（`LoadProfile`／`SaveProfile`／レガシー拡張設定）。
  - 10c-3: アプリ設定・アクション・リンクプロファイル・コントローラー設定の読み書き。
  - 10c-4: 既定プロファイル群のプリセット化。
- **10d: `BackingStore` のコンストラクター注入と `Global.store` の解消**
  - `IProcessInspector`／`ActiveOutDevType`／`IPathService` をコンストラクター注入に。`Global.store`／`m_Config`／`ProfileSettingsServiceInstance` の削除。第 1 層（`DS4Devices` ほか）の設定の読み取りを、上位から渡す形に。環境・パスの窓口（`exeversion` ほか）の廃止。
- **10e: 仕上げと総合検証**
  - `AppHost` の暗黙構築と `CreationNote`、`Mapping` の依存の整理、`Global` の最終状態の確定、モデル図（01〜04）の最終更新、`copilot-instructions` の更新、Phase7 の総合検証（実機確認の一括と GO／NO-GO）と完了報告書。

依存関係: 10a → 10b（独立に近い）→ 10c → 10d → 10e。10a と 10b は入れ替え可能。10c は 10a の後（`BackingStore` の定数参照 114 件が先に片づくと、差分が小さくなる）。

---

## 4. 決定事項と質問（推奨つき）

### Q1. Step10 をサブ Step（10a〜10e）に分けるか
- 推奨: **分ける（A）**。各サブ Step で個別の計画書を作り、承認後に Wave で進める（Step3 の 3a〜3c、Step10b の前例）。
- B: 1 つの Step として Wave を並べる。 → 計画書が巨大になり、承認の粒度が粗い。

### Q2. 定数・資源パスの再配置先（10a）
- 推奨: **A. 定数は `ControllerLimits`（Step9 で新設済み）、資源パスは画面側の `ResourcePaths`（既存）へ。参照を全部置き換えた後、`Global` の別名を削除する**。設定ファイルの定数（`CONFIG_VERSION`、`XML_EXTENSION` ほか）は 10c の永続化の型へ。
- B: `Global` の別名を残して、新規コードだけ新しい型を使う。 → `Global` が残り続け、Phase7 の目的に反する。

### Q3. `BackingStore` の永続化の切り出しの範囲と順序（10c）
- 推奨: **A. ゴールデンテストを先に整備し、その後で、プロファイル（`LegacyProfileXmlSerializer`）→ アプリ設定・アクション・リンクプロファイル・コントローラー設定の順に切り出す**。保存形式は現状維持（D4＝F1）。切り出しは「移動」であり、ロジックは変えない（`git diff` で移動を確認できる形）。
- B: プロファイルだけ切り出して、残りは `BackingStore` に残す。 → `BackingStore` の約半分の行数が残る。
- C: 切り出さない。 → Phase7 の目的（`BackingStore` の解体）を満たさない。

### Q4. `BackingStore` の分解（D2 の再評価）
- 推奨: **A. 状態のフィールド群のドメインエンティティへの分解は、Phase7 に入れない。永続化の切り出しで約 3,300 行が外へ出る（約 3,200 行が残る）ことを成果とし、さらなる分解は Phase9 以降の候補として台帳に記録する**。
- B: Phase7 で分解する。 → `Mapping` の instance 化と同規模で、検証が破綻する（D2 の根拠）。

### Q5. 名前空間＝フォルダ
- 推奨: **A. Phase7 では行わない。Phase8 の後のクリーンアップ（別フェーズ）で行う**。475 ファイルに波及し、XAML・リフレクション・文字列ガードのテストの改修が広いため。copilot-instructions §3.3 原則 3 の方針どおり。
- B: Phase7 の最後に実施。 → 差分が爆発し、総合検証と同時期に重なる。

### Q6. `Global` の最終状態（10d）
- 推奨: **A. 目標は `Global` の削除（ゼロ）。第 1 層の設定の読み取りは、上位から関数・オプションで渡す形にする。10d の調査で届かない部分があれば、その範囲だけを明記して残す（TODO と持ち越しの記録つき）**。
- B: 最初から薄いシムとして `Global` を残す。 → 第 1 層から上位層への参照（依存方向の違反）が残る。

### Q7. 持ち越しの契約の判断（10b）
- 推奨: **A. 経緯を調べ、copilot-instructions §2.4 の手順（使うべきものは接続、使うべきでないものは削除）で個別に判断する**。判断は 10b の計画書に記録し、承認後に実装する。
  - 見込み: `ProfileSubSettingBase.OnSubPropertyChanged` と `ProfileApplicationService` の未使用の依存 `IDeviceStateService` は削除、`IEnvironmentService.UsingMaxControllers` は接続（`MainWindow` の引数整理とあわせて）、`IOutputSlotService.OutputSlotChanged` は購読者の有無で判断。

### Q8. `AppHost` の暗黙構築・`CreationNote`、`Mapping` の依存 11 個、静的な純粋関数の置き場所（10e）
- 推奨: **A. 10e の調査で、テストへの影響の件数を数えてから決める。暫定の見込み: 暗黙構築は、テストの共有ホストの改修が小さければ廃止し、大きければ維持して理由を記録。`Mapping` の依存 11 個は、Phase8（Actions の統合）で整理する方が自然なため、Phase7 では整理せず記録。静的な純粋関数は `Mapping` に残す（Step9 Q4 のとおり）**。
- B: すべて Phase7 で解消する。 → 効果に対して差分が大きい。

### Q9. 実機確認 U-11〜U-19 の扱い
- 推奨: **A. 各サブ Step では先送りを続け、10e で Phase7 の総合検証として一括して行う（Phase6-Step11 の前例。GO／NO-GO を判断）**。10c（永続化）は、保存した設定が実アプリで読めるかの確認（U-20 を新設）を、10c の後に推奨する。
- B: サブ Step ごとに実機確認する。 → 手間が大きい。
- C: 実機確認を行わずに完了とする。 → 実機でしか確認できない項目（ジャイロ、タッチ、仮想デバイス）が未確認のまま Phase7 を終えることになる。

---

## 5. リスクと対策

- 10c（永続化）: 設定ファイルの読み書きは、欠落すると利用者の設定が壊れる。対策: ゴールデンテストを先に整備（保存した XML の要素一覧と値の往復）。移動のみで、ロジックと要素の順序は変えない。`git diff` で移動を確認する。既存の `ProfileLegacyXmlCompletenessTests` を拡張する。
- 10d（`Global.store` の解消）: 起動順序（ブートストラップ、ログ設定、言語の初期化）の崩れ。対策: Step4 のブートストラップを土台とし、`-driverinstall` 分岐を含めて確認。第 1 層の設定の読み取りは、値の渡し忘れで既定値を読むことがないよう、特性化テストで固定する。
- 10a（機械的置き換え）: `const` を別名経由から直接に替えるだけで、定数式の値は変わらない。対策: 置き換え前後で `ControllerLimits` の値のテスト（Step9 で整備済み）が成功すること。XAML の `x:Static` でのバインディングがないか事前に検索する。
- ラチェット: 減る方向のみ許可。サブ Step ごとに基準値を更新し、新しい型へ引き継ぐ。
- ガードレール: 5.1（同一 XML の排他ロック）は 10c が直接かかわる。`ProfileXmlStore` の排他ロックの範囲を変えない。5.5（ドライバ破棄順序）と終了処理は 10d で `Dispose` に触れる場合に確認する。

## 6. 見込みの数値

- `Global.*`: 355 → 10a の後に約 107 → 10b・10c の後に約 70 → 10d の後に 0（Q6＝A の場合）。
- `AppHost.GetService|Services`: 4 のまま（10e で `App.xaml.cs` の窓口と `DI/AppHost.cs` のログ以外が 0 になる見込み）。`ServiceProviderHolder` 17（Phase8）。
- `BackingStore.cs`: 6,572 行 → 約 3,200 行（10c の後）。`Global.cs`: 1,076 行 → 削除または数十行。

## 7. 完了条件（Step10 全体）

- 10a〜10e のすべてが完了し、各サブ Step の完了条件を満たす。
- `Global` が削除されている（または Q6 の例外として明記された範囲だけ残る）。`BackingStore` の永続化が `LegacyProfileXmlSerializer` ほかに分かれ、ゴールデンテストで固定されている。
- モデル図 01〜04、`copilot-instructions` が最終状態を反映している。
- 実機確認（総合検証）が実施され、GO の判断がある。`Phase7-Completion-Report.md` が作成されている。

## 8. 持ち越し（Phase7 の外）

- Phase8: `Actions/` の `ServiceProviderHolder` 17、静的な `SyntheticDispatcher`、K3 の恒久対応、`Mapping` の依存の整理。
- Phase9 以降の候補: `BackingStore` の状態のドメインエンティティへの分解、名前空間＝フォルダの再編。
