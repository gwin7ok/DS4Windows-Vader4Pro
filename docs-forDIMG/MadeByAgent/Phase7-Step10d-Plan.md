# Phase7-Step10d 計画書: `BackingStore` のコンストラクター注入と `Global` の廃止

作成日: 2026-10-18  
対象ブランチ: `For-DI-migration-work`  
上位計画: `Phase7-Step10-Plan.md`（Q1〜Q9 は推奨どおり承認。Q6＝「`Global` の削除（ゼロ）が目標」）§3「10d」、`Phase7-Step10c-Completion-Report.md` §5、`Phase7-Residual-Ledger.md` §15  
状態: **計画書の作成まで（調査完了）。Q1〜Q9 の承認待ち。実装は未着手。**

---

## 1. 目的と位置づけ

- 10c で `BackingStore` は設定の状態だけを持つ型（1,660 行）になった。10d では、**`Global`（562 行、53 メンバー）を削除する**。そのために次を行う。
  - `BackingStore` を DI のコンストラクター注入で生成する（`Global.store`／`m_Config` の静的な生成をやめる）。
  - `Global` に残る静的な窓口（環境・パス、設定の読み取り、イベント、定数）を、持ち主の型へ移す。
  - 第 1 層（`DS4Devices`／`DS4Device`／`DS4SixAxis`／`HidDevices`／出力デバイス）が `Global` を読む依存を、上位から渡す形に替える（依存方向の違反の解消）。
  - `using static DS4Windows.Global;` と、`Global` を前提とするガード・テストを整理する。
- 機能・ログ・起動順序は変えない。特に、起動直後（コンテナ構築より前）に設定を読む箇所（ログのローテーションと最小ログレベル）の順序を守る。
- 見込み: `Global.*` 69 → 0、`Global.cs` 削除。

---

## 2. 調査結果

### 2.1 `Global` の残り（53 メンバー）と使用状況

- **本体からの参照は約 70 件、テストからの参照は約 400 件**（テスト: `Global.store` 215、`Global.appdatapath` 55、`ProfileSettingsServiceInstance` 20、`Notifications` 16、`UseLang` 11、`exedirpath` 10、`tempprofileDistance` 8 ほか）。したがって、テストの移行が最大の作業量になる。
- 本体の参照（ファイル別・種類別）:
  - **環境・パスの静的な窓口**: `exeversion` 9（`Changelog`、`AppSettingsDTO`、`OutputSlotPersistDTO`、`OutputSlotPersist`、`ProfileMigration`、`EnvironmentService`）、`exelocation` 1、`IsAdministrator` 2、`AdminNeeded` 1、`GetInstanceIdFromDevicePath` 3、`CheckIfVirtualDevice` 2。実体は、`PathService` の静的な状態（`ExecutableVersionState` など）と、`Global.cs` 内の Windows 判定のコード。
  - **設定の読み取り（`m_Config` 経由）**: `UseLang` 5、`LogMaxArchiveFiles` 3、`LogMinLevel` 3、`UseCurrentTheme` 2、`SetCulture` 2、`LastVersionChecked` 2、`DeviceOptions` 2、`UseMoonlight`／`UseAdvancedMoonlight` 各 1、`DebouncingMs` 1、`UseDs3PitchRollSim` 1、`GetSASteeringWheelEmulationAxis` 3。
  - **イベント・通知**: `BatteryChanged`／`InvokeBatteryChanged`（`AppearanceSettingsService`）、`OnDeviceSerialChange`（`DeviceStateService`）、`CreateAutoProfiles`（`AutoProfileService`）。
  - **定数・文化**: `configFileDecimalCulture` 9（`ActionsDTO`、`SpecialActionSerializer`、`SpecialActions` の ViewModel 3）、`CONFIG_VERSION` 1、`APP_CONFIG_VERSION` 1、`PROBING_PATH`／`LANGUAGE_ASSEMBLY_NAME`（`LanguagePackViewModel`）、別名（`CUSTOM_EXE_CONFIG_FILENAME`、`XML_EXTENSION`）。
  - **`BackingStore` 自身**: `appdatapath` 3（`m_Actions`／`m_linkedProfiles`／`m_controllerConfigs` の初期化子）、`ProfileSettingsServiceInstance` 2、`useDInputOnly` 1、`RefreshActionAlias` 1。
  - **生成**: `Global.store` 3（`ServiceRegistration`、`AppLogger.NotificationService` の既定、`PathService` の省略時）。
  - `RefreshActionAlias` 2（`BackingStore.UpdateDS4CSetting`、`ProfileSettingsService`）。
- `using static DS4Windows.Global;`: `Mapping.cs`、`DS4LightBar.cs`（修飾なしで使っているメンバーは 0）。ガード: `MappingStep3FinalGlobalReferenceGuardTests`、`MouseGlobalReferenceGuardTests`。

### 2.2 `BackingStore` の生成と依存

- 生成: `Global.m_Config = new BackingStore()`（静的）。`ServiceRegistration` が `Global.store` を `BackingStore` として登録し、`IProcessInspector` と `ActiveOutDevType` をプロパティで設定する（`BackingStore` が `Global` の静的初期化の中で作られるため）。コンストラクターは引数なし。
- `new BackingStore()` の呼び出し: テスト 45 件、StandaloneTests 3 件（本体は `Global` のみ）。
- 起動順序: `App` は、コンテナ構築の前に `PathService`／`EnvironmentService` を作り（ブートストラップ）、`CreateHost(config, parser, bootstrapPathService, bootstrapEnvironmentService)` で同じインスタンスを登録する（Step4 の D3＝S1）。コンテナ構築の前に `Global.LogMaxArchiveFiles`／`LogMinLevel`（起動直後のログローテーションと最小ログレベル）を読むのは、`Global.store` が静的に存在するから。`BackingStore` をコンテナだけが作る形にすると、この読み取りが壊れる。→ `BackingStore` もブートストラップで作り、同じインスタンスを登録する必要がある（Q3）。

### 2.3 第 1 層・低レベルの `Global` 依存（依存方向の違反）

- `DS4Devices`: `UseMoonlight`／`UseAdvancedMoonlight`（`isValid…` の判定）、`GetInstanceIdFromDevicePath`／`CheckIfVirtualDevice`（デバイスの再有効化・仮想デバイスの判定）。
- `DS4Device`: `DebouncingMs[slot]`（入力のデバウンス）。`DS4SixAxis`: `UseDs3PitchRollSim`（Step5 で `DS4SixAxis` のジャイロトリガー判定を関数注入にした前例がある）。`HidDevices`: `DeviceOptions.VerboseLogMessages`。
- 出力デバイス（`DS4OutDeviceBasic`／`DS4OutDeviceExt`／`Xbox360OutDevice`）: `GetSASteeringWheelEmulationAxis(device)`。
- ロギング: `LoggerHolder`（`LogMaxArchiveFiles`／`LogMinLevel`。`ILogSettingsApplier` の実装が呼ぶ）、`AppLogger.NotificationService` の既定（`new AppNotificationService(Global.store)`）。`Util`: `IsAdministrator`。

### 2.4 テスト

- `Global.store`（215）を使うテストは、共有のコンテナの `BackingStore` を前提にしている。`appdatapath`／`exedirpath`（65）は `PathService` の静的な状態の読み書き。これらは、テスト用の静的なヘルパー（例: `TestGlobals`）に集約すれば、機械的に置き換えられる。

---

## 3. 決定事項と質問（推奨つき）

### Q1. サブ Wave の構成
- 推奨: **A. 次の 7 Wave。各 Wave の後にユーザーがビルド・テストを実行する**。
  - W1: テストの移行（`Global.store`／`appdatapath`／`exedirpath`／`ProfileSettingsServiceInstance` ほか → テスト用ヘルパー `TestGlobals`）。本体は変えない。
  - W2: 環境・パスの窓口と定数・文化の移行（`exeversion` ほか → 持ち主の型へ。`IsAdministrator`／`AdminNeeded`／デバイスパスの判定 → 静的ヘルパー）。
  - W3: 第 1 層・出力デバイスの依存（設定値を上位から渡す）。
  - W4: アプリ層の設定の読み取り・イベント（`UseLang`／`UseCurrentTheme`／`SetCulture`／`LastVersionChecked`／ログ設定／`BatteryChanged`／`OnDeviceSerialChange`／`CreateAutoProfiles` ほか）。
  - W5: `BackingStore` のコンストラクター注入とブートストラップ（`Global.store`／`m_Config`／`ProfileSettingsServiceInstance`／`RefreshActionAlias`、`CURRENT_DS4_CONTROLLER_LIMIT` の `BackingStore` の分）。
  - W6: `Global` の削除（`using static` の削除、ガード・ラチェットの整理）。
  - W7: 文書。
- B: 先に `BackingStore` の注入を行う。 → 静的な窓口を使う利用者が多数残ったまま、生成方法が変わる。

### Q2. テストの移行
- 推奨: **A. 先に（W1）、`Global` を使うテストを `TestGlobals`（テスト用の静的ヘルパー）へ機械的に置き換える**。`TestGlobals` は、当面 `Global` を呼び、W5・W6 で実体を差し替える（テストの本体は変えない）。
  - 例: `TestGlobals.Store`（共有のコンテナの `BackingStore`）、`TestGlobals.AppDataPath { get; set; }`（`PathService.AppDataPathState`）、`ExeDirPath`、`ProfileSettings`。
- B: 各 Wave でテストを直す。 → 同じテストを何度も触る。

### Q3. `BackingStore` の生成と起動順序
- 推奨: **A. `BackingStore` をブートストラップで作り（`IPathService` などを受ける）、`CreateHost(…, bootstrapBackingStore)` で同じインスタンスをコンテナへ登録する**。コンテナを使わない起動直後の読み取り（ログローテーション、最小ログレベル）は、`App` が持つこのインスタンスから行う。ブートストラップを経ない起動（テスト）では、`ServiceRegistration` の既定の登録（`new BackingStore(pathService, environment)`）が使われる。
  - `IProcessInspector`（プロパティ注入）と `ActiveOutDevType`（プロパティ注入）は、コンストラクター注入へ。`ActiveOutDevType` は `OutputSlotService` が持つ配列を受ける（`BackingStore` の登録で `IOutputSlotService` を解決しない制約［循環でスタックオーバーフロー。`ServiceRegistration` の注記］があるため、遅延 `Func` またはプロパティの継続を、W5 の調査で決める）。
- B: コンテナだけが作る。 → 起動直後の読み取りが壊れる。

### Q4. 第 1 層の設定値の渡し方
- 推奨: **A. 第 1 層に小さな読み取り専用のインターフェース（またはデリゲート）を定義し、`ControlService` のコンストラクターが設定する**（Step5 の `DS4SixAxis` のジャイロトリガー判定の関数注入と同じ形）。値は、実行時に変わるため（設定画面での切り替え）、その都度読む。
  - 対象: `UseMoonlight`／`UseAdvancedMoonlight`、`DebouncingMs`、`UseDs3PitchRollSim`、`DeviceOptions.VerboseLogMessages`、`GetSASteeringWheelEmulationAxis`（出力デバイスは `ControlService` が生成するため、生成時に渡せる可能性が高い。W3 の調査で確認）。
- B: 第 1 層から `Global` の参照を残す。 → Q6（Step10）の目標（ゼロ）に反し、依存方向の違反が残る。

### Q5. 環境・パスの静的な窓口と判定のコード
- 推奨: **A. 値の窓口は持ち主の型を直接使う**（`Global.exeversion` → `PathService.ExecutableVersionState` ほか。静的な状態は `PathService` に既にある）。**Windows 判定のコード**（`IsAdministrator`、`AdminNeeded`、`GetInstanceIdFromDevicePath`、`CheckIfVirtualDevice`、`GetStringDeviceProperty` など）は、静的ヘルパークラス（`AdminCheck`、`DevicePathProbe`。名前は仮。状態を持たない）へ移し、`EnvironmentService` と `DS4Devices`／`Util` がこれを使う。
- B: 実体を `EnvironmentService` に移し、`DS4Devices` へ注入する。 → 第 1 層の静的なクラス（`DS4Devices`）への注入経路を作ることになり、Q4 と同じ設計が増える。状態を持たない判定なので、静的ヘルパーで足りる。

### Q6. 定数と文化
- 推奨: **A. 持ち主の型へ移し、`Global` の別名を削除する**。`configFileDecimalCulture` → 新しい静的クラス `ConfigFileCulture`（9 件の利用箇所）、`CONFIG_VERSION`／`XML_EXTENSION` → `LegacyProfileXmlSerializer` の定数、`CUSTOM_EXE_CONFIG_FILENAME` → `AppSettingsXmlSerializer` の定数、`APP_CONFIG_VERSION` → `AppSettingsDTO`、`PROBING_PATH`／`LANGUAGE_ASSEMBLY_NAME` → `LanguagePackViewModel`（画面側）。
- B: 別名を残す。 → `Global` が消えない。

### Q7. イベントと通知
- 推奨: **A. `BatteryChanged`／`InvokeBatteryChanged` は `AppearanceSettingsService` 自身のイベントにする**（購読の送信元・引数は従来と同じ `typeof(Global)` を維持できないため、**購読者の確認**を W4 の冒頭で行う。購読者が送信元を使っていなければ、`this` へ変更）。`OnDeviceSerialChange`／`DeviceSerialChange` は `DeviceStateService` のイベントにする。`CreateAutoProfiles` は `AutoProfileService` へ移す。
- B: `Global` にイベントを残す。 → 同上。

### Q8. `RefreshActionAlias` と `ProfileSettingsServiceInstance`
- 推奨: **A. `RefreshActionAlias` は `ProfileSettingsService`（`OutputKBMMapping` の持ち主）のメソッドにし、`BackingStore.UpdateDS4CSetting` からの呼び出しは、呼び出し元［`LegacyProfileXmlSerializer`］が `IProfileSettingsService` 経由で行う形にする。`Global.ProfileSettingsServiceInstance` は削除する**（`BackingStore` の 2 件と `Global.cs` 内のアクセサ 15 件が使う。アクセサ自体を削除）。
- B: 残す。 → Q6（Step10）に反する。

### Q9. `ControlService.CURRENT_DS4_CONTROLLER_LIMIT` の `BackingStore` の分（7 件）
- 推奨: **A. `BackingStore` が `IEnvironmentService.ControllerSlotLimit` をコンストラクターで受けて使う**（静的シムを使わない）。画面・サービスの分（約 25 件）は 10e。
- B: 10e でまとめる。 → `BackingStore` の注入（W5）と同時にやる方が、コンストラクターの変更が 1 回で済む。

---

## 4. リスクと対策

- 起動順序（最重要）: ブートストラップで `BackingStore` を作る位置、`-driverinstall` 分岐、`Application_Startup` の Pre-Host の読み取りの順序を変えない。ガード: `AppPostHostGlobalReferenceGuardTests`（`App.xaml.cs` の `Global` 参照を領域ごとに固定）を、新しい形に合わせて更新し、順序を固定する。実機確認（U-14 の範囲＋ U-20）を 10e で。
- 静的初期化の循環: `PathService` の静的初期化が `Global` を参照しない（ガード: `Phase7Step4PathServiceGuardTests`）形を維持する。`BackingStore` のコンストラクターが `IPathService` を使うが、`PathService` は `BackingStore` を知らない（Step4 の注記: `PathService` の省略時の `Global.store` は W5 で削除）。
- テストの大量の機械置換（約 400 件）: スクリプトで置換し、コメント・文字列は除く。`Global.store` を使うテストが、共有のコンテナの `BackingStore` を前提にしている箇所（実行順への依存、Step10c の教訓）を、W1 で `TestGlobals` に集約して、ホストの作成・破棄を 1 か所で扱う。
- 第 1 層の変更（W3）: 入力の取り込みの経路（`DS4Device` のデバウンスなど）は、ホットパスに近い。デリゲート／インターフェースの呼び出しが割り当てを増やさないこと（静的フィールドに保持した読み取り専用のオブジェクトを呼ぶ）。`MappingHotPathAllocationTests` は `Mapping` のみのため、第 1 層の割り当ては、変更前後の確認として特性化テスト（呼び出しの値が従来と同じ）を W3 で追加する。
- ログの順序: ログのローテーションと最小ログレベルの適用の順序（`App.xaml.cs:203–208`）を変えない。
- ガードレール 5.1〜5.6: 5.5（ドライバ破棄順序）は 10e の `Dispose` の確認に含める。
- 件数を固定するガードの更新漏れ（10a・10c の教訓）: 各 Wave の終わりに、ラチェットの基準値と実数の突き合わせ（`ratchet_slack` 相当）を行う。

## 5. 実機確認

- 先送り（10e の総合検証）。10d で実機でしか確認できない項目は、起動直後のログ（最小ログレベルの反映）、言語・テーマの反映、バッテリー表示、デバイスの接続（Moonlight 設定の反映）。10e の確認項目に加える。

## 6. 完了条件

- `Global` クラスが存在しない（`Global.cs` 削除）。本体の `Global.*` 参照 0、ラチェットの `Global.cs` の行を削除し合計 0。`using static DS4Windows.Global;` が存在しない。
- `BackingStore` がコンストラクター注入で生成される（`Global.store`／`m_Config` なし）。ブートストラップで作った同じインスタンスがコンテナに登録される。
- 第 1 層に `Global` の参照がない。テストが `TestGlobals` などを使い、`Global` を参照しない。
- ガードとラチェットが成功（ユーザーが実行）。文書の更新（台帳 §16、モデル図、Status、完了報告書）。

## 7. 持ち越し

- 10e: `CURRENT_DS4_CONTROLLER_LIMIT`（画面・サービスの分）、`AppHost` の暗黙構築と `CreationNote`、`Mapping` の依存の整理、`ColorUtil` の孤立コードの判断、Phase7 の総合検証（実機確認 U-11〜U-20）と完了報告、モデル図・`copilot-instructions` の最終更新。
