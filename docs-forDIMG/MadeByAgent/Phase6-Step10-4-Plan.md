# Phase6-Step10-4 計画書: サービス実装のコンストラクタのフォールバック（F08）と、全域の再走査・分類

作成日: 2026-10-15  
状態: 計画案・承認待ち（実装未着手）  
上位計画書: `Phase6-Step10-Plan.md`（§0A.5、§0A.6 が本 Step の台帳）、前の Step: `Phase6-Step10-3-Plan.md`（完了）  
準拠: `.github/copilot-instructions.md` §2.1〜2.4、§3.1、§3.3、§3.4

---

## 1. 目的
- **F08**: サービス実装・`Actions/*` のコンストラクタに残っている、`AppHost.GetService`／`Program.rootHub`／`Global.*Instance`／`new <具象>` のフォールバックを整理する（Pure DI の完成。copilot-instructions §3.1）。
- **全域の再走査**: Step10-1〜3 の完了後のコードで、`Global.` の直接参照をソリューション全体で再集計し、各項目を「温存（理由付き）」「Step10-4 で置換」「後続（Step12／Phase7）へ送る」に確定する（決定6）。
- 動作は変えない。置換先は既存の契約を優先し、新しい契約は `Global` への薄い委譲にする。

## 2. 全域の再走査の結果（2026-10-15、コメントを除く。`ScpUtil.cs` 自体を除く。71 ファイル、644 件）

### 2.1 温存（定数・不変データ・純粋関数。決定1）
- **定数**: `MAX_DS4_CONTROLLER_COUNT`（`Mapping.cs` 24、`AppSettingsDTO.cs` 14、`DS4LightBar.cs` 7、`AutoProfilesDTO.cs` 4、ほか）、`TEST_PROFILE_INDEX`／`TEST_PROFILE_ITEM_COUNT`、`RESOURCES_PREFIX`／`ASSEMBLY_RESOURCE_PREFIX`（`ProfileEditor.xaml.cs` 34、`TrayIconViewModel.cs` 12、ほか）、`CONFIG_VERSION`／`APP_CONFIG_VERSION`、`LANGUAGE_ASSEMBLY_NAME`／`PROBING_PATH`。
- **不変の値**: `exeversion`（`ProfileDTO`、`AppSettingsDTO`、`ProfileMigration`、`OutputSlotPersist`、`OutputSlotPersistDTO`）、`configFileDecimalCulture`（`ActionsDTO`、種別ごとの ViewModel）。
- **純粋関数・名前表**: `Clamp`（`Mapping.cs` 23、`DS4State` 4、`Mouse`／`MouseCursor` 各 2、`BezierCurve` 1）、`getDS4ControlsByName`／`getX360ControlsByName`／`getX360ControlString`（`ProfileDTO`、`PressKeyViewModel`、`MappingListViewModel`）、`ds4inputNames`／`xboxDefaultNames`／`ds4DefaultNames`（`BindingWindow`）、`macroDS4Values`（`RecordBoxViewModel`）。

### 2.2 温存（Pre-Host。ホスト生成前に動く処理）
- `App.xaml.cs` 16 件（`FindConfigLocation`、`Load`、`RefreshViGEmBusInfo`、`SetCulture`、`UseLang`、`appdatapath`、`LogMaxArchiveFiles`、`LogMinLevel`、`UseCurrentTheme`）。
- `LoggerHolder.cs` 5 件（`appdatapath`、`LogMaxArchiveFiles`、`LogMinLevel`）。
- `WelcomeDialog.xaml.cs` 3 件（`FindConfigLocation`、`Load`。TODO 付き。Step10-1-2 の決定）。
- 文字列リテラルの `Global.UseLang`（`LanguageSelectDialog`、`LanguagePackViewModel` のログ文）は、参照ではない。

### 2.3 温存（§2.1 の薄いシム。サービス実装が `Global` の実体へ委譲する）
- `DS4Control/Services/` の実装クラス: `AppSettingsService` 185、`ProfileRepository` 27、`OutputKBMHandlerAdapter` 21、`EnvironmentService` 20、`ProfileSettingsService` 11、`ProfilePresetLoader` 10、`SpecialActionRepository` 9、`OutputKBMHandlerLifecycle` 8、`PathService` 8、`AppearanceSettingsService` 8、`AppNotificationService` 6、`DeviceStateService` 4、`ProfileXmlStore` 3、`OutputSlotService` 2、`ProfileSlotApplier` 1、`ProfileActionProvider` 1、`Ds4DeviceRegistryAdapter` 1、`DefaultElevatedProcessLauncher` 1（`exelocation`）。
- Step12（旧シムの物理削除）まで残す。

### 2.4 要個別判断（置換候補。決定が必要）
- **`WindowPlacementHelper.cs` 16 件**（`FormLocationX`／`FormLocationY`／`FormWidth`／`FormHeight`）: 0A.6 の台帳に無かった新規の発見。`IAppSettingsService` に同名の契約が存在する。`MainWindow` だけが使う `internal static` クラス。
- **`MainWindow.xaml.cs` 19 件**: `*Instance` 5（`EnvironmentServiceInstance`、`NotificationServiceInstance`、`OutputSlotServiceInstance`、`PathServiceInstance`、`ProfileRepositoryInstance`、`ProfileSettingsServiceInstance` を含む）、`ApplyProfileToSlot`×2、`ProfilePath`×3、`OlderProfilePath`×1。
- **`AutoProfileService.cs` 7 件**: `AutoProfileRevertDefaultProfile`×2、`autoProfileSwitchNotifyChoice`×2、`ProfilePath`×2、`CreateAutoProfiles`×1。
- **`ProfileApplicationService.cs` 7 件**: `ApplyProfile`×3、`OlderProfilePath`×4。
- **`ControlService.cs` 9 件**（定数除く 7）: `IsRunningSupportedViGEmBus`×2、`vigemInstalled`、`vigembusVersion`×2、`RefreshViGEmBusInfo`、`ProfileSettingsServiceInstance`。
- **`ProfileEntity.cs` 5 件**: `CacheExtraProfileInfo`、`SaveProfile`、`appdatapath`×3。
- **`ProfileList.cs` 1**（`PathServiceInstance`）、**`AutoProfileHolder.cs` 2**（`appdatapath`、定数）、**`OutputSlotPersist.cs` 3**（`appdatapath`×2、`exeversion`）、**`Log.cs` 1**（`NotificationServiceInstance`）、**`OutputSlotManager.cs` 2**（`vigemBusVersionInfo`）。
- **`Actions/ActionManager.cs` 3 件**: `GetProfileAction`、`getProfileActions`、定数。
- **`Mapping.cs` の非定数 3 件**: `ApplyProfile`、`ProfileSettingsServiceInstance`、`outputKBMHandler`。**`SyntheticDispatcher.cs` 4 件**: `outputKBMHandler`×2、`outputKBMMapping`×2。
- **第1層・第3層（`Global` を直接読む下位層）**: `DS4Devices.cs` 6（`CheckIfVirtualDevice`×2、`GetInstanceIdFromDevicePath`×2、`UseMoonlight`、`UseAdvancedMoonlight`）、`DS4Device.cs`（`DebouncingMs`×2、`DebouncingMsChanged`）、`DS4Sixaxis.cs`（`UseDs3PitchRollSim`）、`HidDevices.cs` 2（`DeviceOptions`）、`DS4OutDeviceBasic`／`DS4OutDeviceExt`／`Xbox360OutDevice`（`GetSASteeringWheelEmulationAxis`）、`VirtualKBMFactory.cs`（`fakerInputInstalled`）、`Util.cs`（`IsAdministrator`。Step10-1 で Step12 送りに決定済み）。

### 2.5 F08: コンストラクタのフォールバック（現行）
- **`AppHost.GetService` のフォールバック**: `AutoProfileService`（3）、`ProfileApplicationService`（5）、`OutputSlotService`（`IOutputSlotStore`）、`OutputSlotStore`（`IPathService`）、`ProfileActionChainService`、`ProfileRepository`（`ControlService` の遅延取得）、`DefaultActionManager`、`DefaultMacroPlayer`、`LaunchProcessAction`、`MacroAction`（3）、`MouseOutputAction`、`ProfileSwitchAction`（3）、`AutoProfileChecker`。
- **`new <具象>` のフォールバック**: `OutputSlotService`（`new OutputSlotStore()`）、`OutputSlotStore`（`new PathService()`）、`ProfileActionChainService`（`new MappingActionDispatcher()`）、`DefaultActionManager`（`new DefaultActionFactory()`）、`LaunchProcessAction`／`MacroAction`／`ProfileSwitchAction`（`new Default…()`）。
- **`Global.*Instance` のフォールバック**: `ProfileRepository`（`ProfileSettingsServiceInstance`、`ProfileXmlStoreInstance`、`PathServiceInstance`）、`SpecialActionRepository`（`PathServiceInstance`）。
- **`Program.rootHub` の参照**: `AutoProfileService` 7（`running` の確認と待機）、`ProfileSettingsService` 4（DualSense の判定など）、`OutputSlotService` 1、`DS4Sixaxis` 1。
- **静的 `ServiceProviderHolder.Provider` の利用**: `Actions/ActionManager.cs` 12、`Mapping.cs` 5、`DefaultKeyButtonActionControllerFactory` 2、`KeyActionAdapter` 1、`ActionFactory` 1、`App.xaml.cs` 1。
- **テストでの生成**: `ProfileSettingsService` 76、`SpecialActionRepository` 32、`ProfileRepository` 27、`OutputSlotService` 23、`DefaultActionManager` 7、`ProfileActionChainService` 6、`AutoProfileService` 4、`DefaultMacroPlayer` 3、`LaunchProcessAction` 2、`MouseOutputAction` 1。引数なしの生成が多いため、必須化はテストの更新を伴う。

## 3. 決定が必要な事項

### D4-1: 置換候補（§2.4）の範囲
- 案A（推奨）: **Step10-4 で置換する**: `WindowPlacementHelper`、`AutoProfileService`、`ProfileApplicationService`、`ControlService` の ViGEm 関連、`MainWindow` の `*Instance`、`ProfileEntity`、`ProfileList`、`AutoProfileHolder`、`Log.cs`、`ActionManager` の `GetProfileAction`／`getProfileActions`。**後続へ送る**: `Mapping.cs`／`SyntheticDispatcher.cs`（Phase7 の instance 化。`outputKBMHandler` は TODO 付き）、第1層・第3層（`DS4Devices`、`DS4Device`、`DS4Sixaxis`、`HidDevices`、出力デバイス、`VirtualKBMFactory`。層をまたぐ契約の設計が必要なため、Step12 または Phase7）、`Util.IsAdministrator`（Step12）、`OutputSlotPersist`／`OutputSlotManager`（Step10-5 の出力スロットの接続と一緒に確認）。
- 案B: 置換候補をすべて Step12 へ送り、Step10-4 は F08 だけにする。範囲は小さくなるが、`Global` の残存が増えたまま Step11 へ進む。
- 案C: 第1層・第3層も含めて、すべて Step10-4 で置換する。層をまたぐ契約の新設が必要で、リスクが高い。

### D4-2: F08（コンストラクタのフォールバック）の進め方
- 案A（推奨）: **クラスタ単位で、依存を必須引数にする**（`Program.rootHub`／`AppHost` のフォールバックと `new <具象>` を削除）。3 つのクラスタに分ける: ①プロファイル系（`ProfileRepository`、`SpecialActionRepository`、`ProfileApplicationService`、`AutoProfileService`、`ProfileActionChainService`）、②出力スロット系（`OutputSlotService`、`OutputSlotStore`）、③`Actions/*`（`DefaultActionManager`、`DefaultMacroPlayer`、`LaunchProcessAction`、`MacroAction`、`MouseOutputAction`、`ProfileSwitchAction`、`AutoProfileChecker`）。各クラスタで、ビルド・テスト・実機確認を行う。
- 案B: 引数なしの生成を、テスト専用のファクトリ（テストヘルパー）へ集約してから、一括で必須化する。1 回の変更が大きくなる。
- 案C: テストでの引数なしの生成が多いクラスは、引数なしの生成を温存し、理由を TODO に記載する（`ProfileSettingsService` 76、`SpecialActionRepository` 32 など）。変更は小さいが、フォールバックが残る。

### D4-3: `Program.rootHub` の置換先
- 案A（推奨）: `ControlService` を `Func<ControlService>`（遅延解決）で受け取る。`AutoProfileService`（実行状態の確認）、`ProfileSettingsService`（DualSense の判定）、`OutputSlotService`（`ControlService` の取得）が対象。循環依存を避けるため、遅延解決を使う（Step9-4、Step10-2-3 と同じ形）。
- 案B: 「実行中か」「デバイスの種別」だけを返す小さな契約（`IControllerHub` など）を新設する。契約は増えるが、依存が最小になる。
- 案C: 温存して TODO を付け、Step12 へ送る。

### D4-4: 静的な `ServiceProviderHolder.Provider`（`Actions/ActionManager.cs` ほか 22 件）
- 案A（推奨）: **温存し、TODO を付けて Phase7／Phase8 へ送る**。`ActionManager` は、`Mapping.cs`（static）と一体の静的ファサードで、`Mapping` の instance 化（Phase7）と、Controls／SpecialActions 統一ディスパッチ（Phase8）の際に、構造ごと見直すのが妥当。
- 案B: 静的ファサードを維持したまま、`IManagedActionManager` の取得を 1 か所（初期化時）に集約する。変更は中規模で、効果は限定的。

### D4-5: 実施順（Step10-4 の細分化）
- 案A（推奨）: ①再走査の確定（分類表の確定。コードは変えない）→ ②置換候補のうち小さなもの（`WindowPlacementHelper`、`ProfileList`、`ProfileEntity`、`Log.cs`、`AutoProfileHolder`）→ ③F08 のクラスタ 1（プロファイル系）→ ④クラスタ 2（出力スロット系）→ ⑤クラスタ 3（`Actions/*`）→ ⑥`MainWindow`・`ControlService` の残り → ⑦ガードテストと文書。

## 4. マイクロステップ（案。決定後に確定）
- **Step10-4-0**: 分類表の確定とガードテストの設計（コードの変更なし。D4-1〜D4-5 の決定を反映）。
- **Step10-4-1**: 小さな置換（`WindowPlacementHelper` ほか）。
- **Step10-4-2**: F08 クラスタ 1（プロファイル系）。
- **Step10-4-3**: F08 クラスタ 2（出力スロット系）。
- **Step10-4-4**: F08 クラスタ 3（`Actions/*`）。
- **Step10-4-5**: `MainWindow`・`ControlService` の残りの `Global` 参照の置換。
- **Step10-4-6**: 全域のガードテスト（温存項目の許可リスト方式。ソリューション全体で、許可リスト以外の `Global.`／`AppHost`／`Program.rootHub` が増えないことを検査）と文書（モデル図 03／04、`Phase6-Status.md`、`Phase6-Step10-Plan.md`）。

## 5. 実機確認（案。確認できない項目は `Phase6-Step11-Plan.md` §3.3 へ登録する）
- **ウィンドウの位置・サイズ**（`WindowPlacementHelper`）: 終了時に保存され、起動時に復元されること。最小化起動。
- **自動プロファイル**（`AutoProfileService`）: 対象のプログラムの起動・終了で、プロファイルが切り替わること。サービスの開始・停止との連動。
- **プロファイルの適用**（`ProfileApplicationService`）: プロファイルの切替、一時プロファイルの復帰。
- **出力スロット**: スロットの手動接続・切断、永続化。
- **スペシャルアクション**: マクロ、プログラム起動、プロファイル切替（`Actions/*`）。

## 6. リスク
- **テストの更新量**: 引数なしの生成が、テストに約 180 件ある。クラスタごとに、同じ Step でテストを更新する。
- **循環依存**: `OutputSlotService` ↔ `ControlService` など。遅延解決（`Func<>`）で回避する（既存の方針）。
- **起動順序**: ホスト生成前に解決してはならないサービスがある（K7-1）。`Func<>` の使用と、実機での起動確認で担保する。
- **K1（`ProfileApplicationService` の `deviceIndex >= 4`）**: スロット数 8 への拡張に関わる既知の項目。クラスタ 1 の置換時に、現行コードで再確認する。

## 7. 完了条件
- 許可リスト（§2.1〜2.3、および D4-1 で「温存」「後続へ送る」と決めたもの）以外の `Global.`、`AppHost.GetService`、`Program.rootHub`、`new <具象>` のフォールバックが、`Services/`・`Actions/`・View・ViewModel に残っていない（残るものは、理由付きの TODO がある）。
- 追加した契約の契約テストと、全域のガードテストが通り、`dotnet test` が全件合格する。
- 実機確認の結果が記録され、確認できない項目が先送り台帳へ登録されている。

---

## 8. 決定の記録（2026-10-15、すべて推奨案で承認）
- **D4-1＝A**: 置換候補（§2.4）のうち、`WindowPlacementHelper`、`AutoProfileService`、`ProfileApplicationService`、`ControlService` の ViGEm 関連、`MainWindow` の `*Instance`、`ProfileEntity`、`ProfileList`、`AutoProfileHolder`、`ActionManager` の `GetProfileAction`／`getProfileActions` は Step10-4 で置換する。`Mapping.cs`／`SyntheticDispatcher.cs`（Phase7）、第1層・第3層（Step12 または Phase7）、`Util.IsAdministrator`（Step12）は後続へ送る。
- **D4-2＝A**: F08 は、①プロファイル系、②出力スロット系、③`Actions/*` の 3 クラスタに分け、クラスタごとに必須化とテスト更新、実機確認を行う。
- **D4-3＝A**: `Program.rootHub` は、`Func<ControlService>`（遅延解決）で置き換える。
- **D4-4＝A**: 静的な `ServiceProviderHolder.Provider`（`ActionManager` ほか 22 件）は温存し、TODO を付けて Phase7／Phase8 へ送る。
- **D4-5＝A**: 実施順は §4 のとおり（分類の確定 → 小さな置換 → クラスタ 1 → 2 → 3 → `MainWindow`・`ControlService` → ガードテストと文書）。

## 9. 分類表（Step10-4-0 の成果。コードの変更なし）

### 9.1 Step10-4 で置換する（置換先と、実施するマイクロステップ）
- **Step10-4-1（小さな置換）**
  - `WindowPlacementHelper.cs`（16 件）: `internal static` のため、各メソッドが `IAppSettingsService`（`FormLocationX`／`FormLocationY`／`FormWidth`／`FormHeight`）を引数で受け取る形にする。呼び出し元は `MainWindow`（5 か所）。
  - `ProfileList.cs`（1 件）: `IPathService` を必須引数にし、`Global.PathServiceInstance` のフォールバックを削除する。生成箇所は `MainWindow`（1）とテスト（3）。
  - `ProfileEntity.cs`（5 件。`appdatapath`×3、`SaveProfile`、`CacheExtraProfileInfo`）: `IPathService` と `IProfileRepository`（`SaveProfile`、`CacheExtraProfileInfo`）を、`ProfileList` から渡す（`ProfileEntity` は `ProfileList` だけが生成する）。
  - `AutoProfileHolder.cs`（`appdatapath` 1 件）: `IPathService` を必須引数にする。生成箇所は DI の登録、`AutoProfileService` のフォールバック、テスト（7）。
- **Step10-4-2（クラスタ 1: プロファイル系）**
  - `ProfileRepository`（`Global.*Instance`×3、`AppHost`×1 のフォールバック）、`SpecialActionRepository`（`PathServiceInstance`）、`ProfileApplicationService`（`AppHost`×5、`ApplyProfile`×3、`OlderProfilePath`×4。K1 の `deviceIndex >= 4` を現行コードで再確認）、`AutoProfileService`（`AppHost`×3、`Program.rootHub`×7、`new AutoProfileHolder()`、`new EnvironmentService()`、`AutoProfileRevertDefaultProfile`×2、`autoProfileSwitchNotifyChoice`×2、`ProfilePath`×2、`CreateAutoProfiles`）、`ProfileActionChainService`（`AppHost`、`new MappingActionDispatcher()`）、`ProfileSettingsService`（`Program.rootHub`×4、`new EnvironmentService`）。
- **Step10-4-3（クラスタ 2: 出力スロット系）**: `OutputSlotService`（`Program.rootHub`、`AppHost`、`new OutputSlotStore()`、`new OutputSlotManager`）、`OutputSlotStore`（`AppHost`、`new PathService()`）、`OutputSlotPersist`／`OutputSlotManager`（`appdatapath`、`vigemBusVersionInfo`。Step10-5 の接続の前提として確認）。
- **Step10-4-4（クラスタ 3: `Actions/*`）**: `DefaultActionManager`、`DefaultMacroPlayer`、`LaunchProcessAction`、`MacroAction`（3）、`MouseOutputAction`、`ProfileSwitchAction`（3）、`AutoProfileChecker`。`Actions/*Action` は、利用側が `new` する箇所と、DI の外で生成される経路（`DefaultActionFactory` など）を突き合わせてから、必須化するか、理由を書いて温存するかを判断する。
- **Step10-4-5（`MainWindow`・`ControlService`）**: `MainWindow`（`*Instance`×6、`ApplyProfileToSlot`×2、`ProfilePath`×3、`OlderProfilePath`×1。Step9 のガードテストの許容範囲を確認）、`ControlService`（`IsRunningSupportedViGEmBus`×2、`vigemInstalled`、`vigembusVersion`×2、`RefreshViGEmBusInfo`、`ProfileSettingsServiceInstance`。`IEnvironmentService` に既存の契約があるものを使う）、`ActionManager` の `GetProfileAction`／`getProfileActions`（`IProfileActionProvider`）。

### 9.2 温存する（理由付き。許可リストへ登録）
- §2.1（定数・不変データ・純粋関数）、§2.2（Pre-Host）、§2.3（薄いシム）。
- **`Log.cs`（`NotificationServiceInstance`）**: 静的ロガー（`AppLogger`）で、コンストラクター注入ができない。D4-4 と同様に、静的ファサードとして温存し、TODO を付けて Step12 へ送る（当初の「置換」から、本分類で変更）。
- **`Mapping.cs`（`ApplyProfile`、`ProfileSettingsServiceInstance`、`outputKBMHandler`）、`SyntheticDispatcher.cs`（`outputKBMHandler`、`outputKBMMapping`）**: Phase7（`Mapping` の instance 化）へ送る（TODO 付き）。
- **静的な `ServiceProviderHolder.Provider`**（`Actions/ActionManager.cs` 12 ほか 22 件）: D4-4 により、Phase7／Phase8 へ送る。

### 9.3 後続へ送る（Step12 または Phase7。理由付きで台帳へ）
- 第1層（`DS4Devices.cs`: `CheckIfVirtualDevice`×2、`GetInstanceIdFromDevicePath`×2、`UseMoonlight`、`UseAdvancedMoonlight`。`DS4Device.cs`: `DebouncingMs`×2、`DebouncingMsChanged`。`DS4Sixaxis.cs`: `UseDs3PitchRollSim`、`Program.rootHub`。`HidDevices.cs`: `DeviceOptions`×2）。
- 第3層（`DS4OutDeviceBasic`／`DS4OutDeviceExt`／`Xbox360OutDevice`: `GetSASteeringWheelEmulationAxis`。`VirtualKBMFactory.cs`: `fakerInputInstalled`）。
- `Util.cs`（`IsAdministrator`）。
- 下位層から上位層の設定を読んでいるため、層をまたぐ契約の設計（例: 下位層が購読するイベントや、生成時に値を渡す形）が必要。`Phase6-Step12-Plan.md`（旧シムの物理削除）または Phase7 で、層の依存方向とあわせて扱う。

## 10. 進捗（2026-10-15）
- **Step10-4-0（分類表の確定）**: 完了（本書 §8〜9。コードの変更なし）。次: Step10-4-1（小さな置換）の計画と実装。

---

## 11. Step10-4-1 の計画（小さな置換）。2026-10-15

### 11.1 対象の再確認（現行コード）
- **`WindowPlacementHelper.cs`**: `Global.FormLocationX`／`FormLocationY`／`FormWidth`／`FormHeight` を使っているのは、`ApplyPlacement(MainWindow, bool)` だけ（16 件）。`GetPlacement`／`GetLogicalPlacement` は `Global` を使わない。呼び出し元は `MainWindow` の `ApplyPlacement` 1 か所（1197 行）。
- **`ProfileList.cs`**: `IPathService` を任意引数で受け取り、省略時は `Global.PathServiceInstance`。生成箇所は `MainWindow`（フィールド初期化子）、テスト 2 件（`AutoProfilesViewModelTests`、`PatternCViewModelTests`）。`ProfileEntity` は `ProfileList.AddProfileSort` の 2 か所だけが生成する。
- **`ProfileEntity.cs`**: `DeleteFile`（`appdatapath`）、`RenameProfile`（`appdatapath`×2）、`SaveProfile`（`Global.SaveProfile`／`Global.CacheExtraProfileInfo`）。呼び出し元: `DeleteFile` は `MainWindow` 1 件、`RenameProfile` は `MainWindow` 1 件、**`SaveProfile` と `FireSaved` は 0 件**（`ProfileSaved` イベントの購読も 0 件。`ProfileDeleted` は `ControllerListViewModel` が購読）。
- **`AutoProfileHolder.cs`**: `appdatapath`×1。コンストラクターで `Load()` を実行する。生成箇所は、DI の登録、`AutoProfileService` の `new AutoProfileHolder()` のフォールバック、テスト 7 件。

### 11.2 計画の調整（§9.1 からの変更）
- **`AutoProfileHolder` は、10-4-1 から 10-4-2（クラスタ 1）へ移す**。理由: `AutoProfileService` の `new AutoProfileHolder()` のフォールバック（F08）と同時に直さないと、一時的に別の `Global`／`AppHost` を持ち込むことになるため。10-4-2 で、`IPathService` を必須引数にし、フォールバックと同時に整理する。

### 11.3 変更の内容
1. **`WindowPlacementHelper.ApplyPlacement`**: `IAppSettingsService` を引数に追加する（`ApplyPlacement(MainWindow, IAppSettingsService, bool)`）。`Global.Form*` を、引数の `FormLocationX` など（`IAppSettingsService` の既存の契約）へ置換する。`MainWindow` が、保持している `appSettingsService` を渡す。
2. **`ProfileList`**: `IPathService` を必須引数にし、`Global.PathServiceInstance` のフォールバックを削除する（null は拒否）。`MainWindow` のフィールド初期化子（`new ProfileList()`）は、`pathService` を解決した後（コンストラクター内）で生成するように移す。テスト 2 件は `new PathService()` を渡す。
3. **`ProfileEntity`**: `IPathService` を必須引数にする（`ProfileList` が、自身の `pathService` を渡す）。`DeleteFile`／`RenameProfile` の `Global.appdatapath` を、`pathService.AppDataPath` へ置換する。
4. **`ProfileEntity.SaveProfile`／`FireSaved`／`ProfileSaved`（呼び出し元 0 件）**: §2.4 に従って判断する（11.4 の決定 E1）。

### 11.4 決定が必要な事項
- **E1（呼び出し元 0 件の `ProfileEntity.SaveProfile`／`FireSaved`／`ProfileSaved`）**
  - 経緯: 上流の `DS4Windows` から引き継いだメンバー。現在のプロファイルの保存は、`ProfileEditor` → `ProfileSettingsViewModel.SaveProfile` → `IProfileRepository`（Step8・Step9）に一本化されており、このメンバーは、それと重複する並行実装（`Global.SaveProfile`／`Global.CacheExtraProfileInfo` を直接呼ぶ）で、どこからも使われていない。「本来使うべき API」には当たらない（正規の経路は別にある）。
  - 案A（推奨）: **削除する**（§2.4 の「使うべきでないもの」）。`Global` への直接参照が 2 件減り、`ProfileEntity` が、パスの操作だけに単純化される。機能廃止（§2.2）には当たらない（呼び出し元が無く、画面の機能ではない）。
  - 案B: 温存し、TODO（経緯と、Step12 で削除する予定）を付ける。`Global` への参照が残る。
- **E2（`AutoProfileHolder` を 10-4-2 へ移す）**: 11.2 のとおり。案A（推奨）＝移す。案B＝10-4-1 で、`IPathService` を任意引数（`AppHost` へのフォールバック付き）として足す（フォールバックが増えるため非推奨）。

### 11.5 テスト
- `Step10FourOneGlobalReferenceGuardTests`（新規）: `WindowPlacementHelper.cs`、`ProfileList.cs`、`ProfileEntity.cs` に、温存項目以外の `Global.`／`AppHost`／`Program.rootHub` が無いこと（`ApplyPlacement` が `IAppSettingsService` を受け取ること）、`ProfileList`／`ProfileEntity` の必須引数の null 拒否。
- 動作の検査: 一時ディレクトリで、`ProfileList.Refresh`（プロファイルの追加・削除の反映）、`ProfileEntity.DeleteFile`／`RenameProfile` が、注入した `IPathService` の保存先を使うこと。
- 既存のテスト（`ProfileList`／`AutoProfilesViewModel`／`PatternC`）を、新しいコンストラクターに合わせる。

### 11.6 実機確認
1. **ウィンドウの位置・サイズ**: ウィンドウを移動・リサイズして終了し、起動し直したときに復元されること。最小化起動（`-m`）でも崩れないこと。
2. **プロファイル一覧**: メイン画面の「プロファイル」タブの一覧が表示され、新規作成・複製・名前変更・削除が反映されること（名前変更・削除は `ProfileEntity` を通る）。
3. **コントローラー一覧のプロファイル選択**: 削除したプロファイルが、各コントローラーの選択肢から消えること。

### 11.7 リスク
- **`MainWindow` の初期化順序**: `profileListHolder` を使う箇所（153 行付近）は、`pathService` の解決より後。生成位置の移動に伴い、他のフィールドの初期化子から参照していないことを確認する。
- **`WindowPlacementHelper` の起動時の呼び出し**: ウィンドウの表示前（`SourceInitialized` など）に呼ばれる。`appSettingsService` は、コンストラクターで解決済みのため、順序の問題は無い見込み。

## 12. Step10-4-1 の実装記録。2026-10-15
- **決定**: E1＝A（`ProfileEntity.SaveProfile`／`FireSaved`／`ProfileSaved` を削除。呼出元 0 件、正規の保存経路と重複する並行実装）、E2＝A（`AutoProfileHolder` は 10-4-2 へ移す）。
- **変更**
  - `WindowPlacementHelper.ApplyPlacement` に `IAppSettingsService` を引数追加し、`Global.Form*` 16 件を置換。`MainWindow` は `appSettingsService` を渡す。
  - `ProfileList` は `IPathService` を必須引数にし、`Global.PathServiceInstance` のフォールバックを削除。`MainWindow` の生成位置を、`pathService` 解決後（コンストラクター内）へ移動。
  - `ProfileEntity` は `IPathService` を必須引数にし、`Global.appdatapath` を置換。未使用の `SaveProfile`／`FireSaved`／`ProfileSaved` を削除。
- **テスト**: 新規 `Step10FourOneGlobalReferenceGuardTests`（Global 参照の不在、未使用メンバー削除、必須引数、注入パスでの Refresh／Rename／Delete）。`AutoProfilesViewModelTests`／`PatternCViewModelTests` の `new ProfileList()` を更新。
- **状態**: ビルド・テスト・実機確認（§11.6）の結果待ち。

## 13. Step10-4-1 の完了と Step10-4-2 の計画（クラスタ 1: プロファイル系）。2026-10-15
- **Step10-4-1**: ビルド・テストビルド・テスト実行成功、実機確認（ウィンドウ位置・サイズ、プロファイル一覧、コントローラー選択肢）問題なし。

### 13.1 現行コードの再確認（クラスタ 1 の 6 クラス）
- **ProfileRepository**: 引数 3 つ（`IProfileSettingsService`、`IProfileXmlStore`、`IPathService`）が全て省略可。省略時は `Global.*Instance` へフォールバック。さらに `LoadProfile` 内に `AppHost.GetService<ControlService>()` が 1 件。`Global.ProfilePath` などの薄い委譲（約 25 件）は、契約（`IProfileRepository`）の実体なので温存対象。
- **SpecialActionRepository**: `BackingStore config = null`（省略時 `Global.store`）、`IPathService`（省略時 `Global.PathServiceInstance`）。
- **ProfileApplicationService**: 引数 5 つ全て省略可。省略時は `AppHost` の 5 件。`ControlService` を直接保持。`Global.ApplyProfile`×3、`Global.OlderProfilePath`×4（`IProfileRepository` に同名の契約がある）。`deviceIndex >= 4` の上限が 4 か所（K1）。
- **AutoProfileService**: `new AutoProfileHolder()`、`new EnvironmentService()`、`AppHost`×3、`Program.rootHub`×7、`Global.AutoProfileRevertDefaultProfile`×2（`IAppSettingsService` に契約あり）、`Global.ProfilePath`×2（`IProfileRepository`）、`autoProfileSwitchNotifyChoice`／`CreateAutoProfiles`（`IAutoProfileService` の薄い委譲で温存）。
- **ProfileActionChainService**: `AppHost` と `new MappingActionDispatcher()` のフォールバック。
- **ProfileSettingsService**: `Program.rootHub`×4（`DualSenseDevice` 判定・デバイス取得）、`new EnvironmentService()`、`Global.store` のフォールバック。薄い委譲（`RefreshActionAlias` ほか）は温存。
- **生成箇所**: DI 登録、`ScpUtil.cs` の Global 用の静的フォールバック 3 件（714・779・808 行）、`AutoProfileChecker`（フォールバック）、テスト多数（`ProfileSettingsService` 約 80、`ProfileRepository` 約 25、`SpecialActionRepository` 約 30）。
- **発見（§2.4 の対象）**: `AutoProfileChecker` は `new` する呼出元が 0 件。経緯の調査後、削除可否を 10-4-4 で判断する。

### 13.2 方針（案）
- **P1**: 省略可の引数を、必須引数（null は `ArgumentNullException`）にする。DI の登録は、すでに全依存を解決できるため、変更不要。
- **P2**: `Program.rootHub`／`AppHost` の `ControlService` は、`Func<ControlService>`（D4-3）で受け取る。`ProfileSettingsService` は `ControlService` を持たず、`DualSenseDevice` 判定のためだけに使うため、`Func<ControlService>` を使う（`ControlService` → `ProfileSettingsService` の依存循環を避けるため遅延解決）。
- **P3**: `Global` 用の静的フォールバック（`ScpUtil.cs` 714・779・808 行）は、シム側の実装であり、必須引数に合わせて明示的に引数を渡す形に変える。静的初期化順序のハザード（`Global.store` の初期化前の参照）を、契約テストで確認する。
- **P4**: `new EnvironmentService()`／`new MappingActionDispatcher()`／`new AutoProfileHolder()` は、必須引数にして削除する。
- **P5**: `AutoProfileHolder` は `IPathService` を必須引数にする（10-4-1 から移した分）。

### 13.3 テストへの影響
- テストの生成箇所を、必須引数に合わせて更新する（約 135 件。テスト用の共通ヘルパーを `DS4WindowsTests` に 1 つ用意して、置換を機械化する）。
- 新規ガード `Step10FourTwoGlobalReferenceGuardTests`: 6 ファイルに温存項目以外の `Global`／`AppHost`／`Program.rootHub` が無いこと、必須引数の null 拒否。

### 13.4 分割の提案
- 影響が大きいため、10-4-2 を次の 3 つに分けて、それぞれでビルド・テスト・実機確認を行う。
  - **10-4-2a**: `ProfileActionChainService`、`ProfileApplicationService`、`SpecialActionRepository`（依存が少なく、テストの更新も少ない）。
  - **10-4-2b**: `ProfileRepository`、`AutoProfileService`＋`AutoProfileHolder`。
  - **10-4-2c**: `ProfileSettingsService`（テストの更新が約 80 件。`Func<ControlService>` の導入）。

## 14. 決定と Step10-4-2a の実装記録。2026-10-15
- **決定**: G1＝A（10-4-2 を 2a／2b／2c の 3 分割）、G2＝A（テスト用の共通ヘルパー）、G3＝A（`AutoProfileChecker` は 10-4-4 で判断）。
- **10-4-2a の変更**
  - `ProfileActionChainService`: `IProfileActionProvider`／`IMappingActionDispatcher` を必須化。`AppHost` と `new MappingActionDispatcher()` のフォールバックを削除。
  - `ProfileApplicationService`: 5 引数を全て必須化。`ControlService` は `Func<ControlService>`（遅延解決。D4-3）。`AppHost` の 5 件を削除。`Global.OlderProfilePath`×4 は `IProfileRepository.OlderProfilePath`（同じ実体）へ置換。`Global.ApplyProfile`×3 は実装本体が `Global` にあるため温存し、TODO（Phase7）を付与。
  - `SpecialActionRepository`: `BackingStore`／`IPathService` を必須化。`Global.store`／`PathServiceInstance`／`appdatapath` のフォールバックを削除。`BackingStore` は DI 未登録のため、`ServiceRegistration` で `Global.store` を渡すファクトリー登録に変更。`Global` 側の静的フォールバックも、明示的に引数を渡す形へ。薄い委譲（`SaveAction` など 4 件）は温存。
- **テスト**: 新規 `Step10FourTwoGlobalReferenceGuardTests`、共通ヘルパー `TestRepositoryFactory`（約 30 件の生成箇所を置換）。
- **新たな発見（10-4-2b へ追加）**: `ProfileActionProvider` が `BackingStore config = null`（省略時 `Global.store`）。クラスタ 1 の仲間として、10-4-2b で必須化する。
- **実機確認項目**: スペシャルアクションの一覧表示・追加・削除・保存、プロファイル切替アクション（ボタン割り当てからの切替と、元のプロファイルへの復帰）、アクション連鎖。

## 15. Step10-4-2a の完了と、Step10-4-2b の計画。2026-10-15
- **Step10-4-2a**: ビルド・テストビルド・テスト実行成功。実機確認（スペシャルアクションの一覧・追加・削除・保存、プロファイル切替と元への復帰）問題なし。アクション連鎖の確認は、下記の決定により省略。
- **決定（アクション連鎖の廃止＝案A）**: 「プロファイル切替アクションの後に、新プロファイルの同じトリガーのアクションを続けて発火させる」連鎖（`ProfileActionChainService`。移行前の `Mapping.cs` の処理を `b8c914c2` で切り出したもの）は、トリガーを再成立させない限り追加のアクションを実行しない仕様へ変更する。実施は 10-4-2 の完了後に、別の計画（Step10-4-2d として提案）で行う。2a は挙動を維持した。
- **K1 の再確認（現行コード）**: `ProfileApplicationService`（4 か所）と `ProfileActionChainService`（1 か所）が `deviceIndex >= 4` で弾いている。一方、スロットの上限は `IEnvironmentService.ControllerSlotLimit`（最大 8）。移行前の `Mapping.cs` も `device >= 4` であり、**移行前から存在する上限**（回帰ではない）。5〜8 番目のコントローラーでは、プロファイル切替アクションと復帰が動かない可能性がある。是正の可否はユーザー判断（10-4-2b の決定 H3）。

### 15.1 Step10-4-2b の対象と方針（案）
- **ProfileRepository**: 引数 3 つを必須化し、`Func<ControlService>` を追加（`LoadProfile` 内の `AppHost` を置換）。`Global.ProfilePath` は自身の `ProfilePath`（同じ実体）へ置換。`Global.loggedInvalidActions.Clear()` は温存（TODO）。他の薄い委譲は温存。
- **ProfileActionProvider**: `BackingStore` を必須化（`ServiceRegistration` で `Global.store` を渡すファクトリー登録）。
- **AutoProfileHolder**: `IPathService` を必須化（`Global.appdatapath` の置換）。
- **AutoProfileService**: 全引数を必須化し、`new AutoProfileHolder()`／`new EnvironmentService()`／`AppHost` を削除。`Program.rootHub`×7 は `Func<ControlService>`、`Global.AutoProfileRevertDefaultProfile`×2 は `IAppSettingsService`、`Global.ProfilePath`×2 は `IProfileRepository` へ。`autoProfileSwitchNotifyChoice`／`CreateAutoProfiles` は薄い委譲として温存。
- **`Global` 側の静的フォールバック**（`fallbackProfileRepository`）: 必須引数に合わせ、遅延生成（初回アクセス時）に変更する。静的初期化順序のハザードを避けるため。
- **AutoProfileChecker（決定 H1）**: `AutoProfileService` の必須化に合わせ、`AutoProfileService` を `new` する分岐（呼出元 0 件）が、そのままではコンパイルできない。案A（推奨）: 型は温存し、`IAutoProfileService` が解決できない場合は例外にする（削除の判断は 10-4-4）。案B: 本ステップで削除する。
- **テスト**: `TestRepositoryFactory` に `CreateProfileRepository` ほかを追加（約 25＋10 件）。`Step10FourTwoGlobalReferenceGuardTests` に対象を追加。
- **実機確認項目**: プロファイルの選択・保存・読み込み、自動プロファイル（Auto Profiles 画面での編集と、対象プロセス起動時の切替、終了時の復帰、「サービスを一時停止」の動作）。

## 16. Step10-4-2b の実装記録。2026-10-15
- **決定**: H1＝A（`AutoProfileChecker` は型を温存し、`IAutoProfileService` が未解決なら例外。削除の判断は 10-4-4）、H3＝A（K1 は 10-4-2 では触れず、別の小ステップで提案）。
- **変更**
  - `ProfileRepository`: 4 引数（`IProfileSettingsService`、`IProfileXmlStore`、`IPathService`、`Func<ControlService>`）を必須化。`AppHost`／`Global.*Instance` のフォールバックを削除。`Global.ProfilePath`／`ProfileActions` の内部参照は自身のプロパティへ。薄い委譲は温存。
  - `ProfileActionProvider`: `BackingStore` を必須化（`ServiceRegistration` で `Global.store` を渡す）。
  - `AutoProfileHolder`: `IPathService` を必須化（`Global.appdatapath` を置換）。
  - `AutoProfileService`: 8 引数を全て必須化。`new`／`AppHost` を削除。`Program.rootHub`×7 → `Func<ControlService>`、`Global.AutoProfileRevertDefaultProfile`×2 → `IAppSettingsService`、`Global.ProfilePath`×2 → `IProfileRepository`。
  - `AutoProfileChecker`: `new AutoProfileService` の分岐を例外に変更（TODO 付き）。
  - `ProfileEditor`: `?? new ProfileRepository()` のフォールバックを例外に変更（DI 登録済みのため挙動は同じ）。
  - `Global` の `fallbackProfileRepository`: 初回アクセス時の生成に変更。
- **注意（挙動の差）**: `AutoProfileService` が `ControlService` を `Func`（DI の解決）経由で取得するため、`Program.rootHub` が未設定の時点で呼ばれると、DI が `ControlService` を生成する可能性がある。実機では、起動後のタイマーから呼ばれるため `rootHub` は設定済みの見込み。実機確認で、自動プロファイルの動作を確認する。
- **テスト**: `TestRepositoryFactory` に `CreateProfileRepository` ほか 3 つを追加し、生成箇所を置換。`Step10FourTwoGlobalReferenceGuardTests` に 4 ファイルの `Global`／`AppHost` 不在、必須引数、注入パスでの `AutoProfileHolder` の読み込みを追加。

## 17. Step10-4-2b の完了と、Step10-4-2c の計画（ProfileSettingsService）。2026-10-15
- **Step10-4-2b**: ビルド・テストビルド・テスト実行成功、実機確認（プロファイル、自動プロファイル、サービスの一時停止）問題なし。

### 17.1 現行コードの再確認
- **ProfileSettingsService**: 引数 `BackingStore backingStore = null`（省略時 `Global.store`）、`IEnvironmentService environmentService = null`（省略時 `new EnvironmentService()`）。`SafeConfig => _config ?? Global.store`。
- **`Program.rootHub`×4**（2 メソッド）: `GetRumbleBoost`（DualSense の判定。レポートごとの経路）と `SetRumbleAutostopTime`（接続中のデバイスへの反映）。いずれも、スロット番号のデバイスを取るだけ。`ControlService.GetController(int)` が同じ値を、範囲外安全に返す。
- **`Global.ProfileActions`**: `ProfileActions` プロパティの取得のフォールバックと、設定時の同期ループ。`SafeConfig` が `Global.store` と同一なら、同期ループは同じ配列への代入で、何も起きない。
- **生成箇所**: DI 登録（`BackingStore` が未登録）、`ScpUtil.cs` 714 行の静的フォールバック、テスト約 80 件（引数なし約 60、`BackingStore` 指定約 15、環境サービス指定数件）。
- 薄い委譲（`RefreshActionAlias`、`RefreshExtrasButtons`、`defaultButtonMapping`、`reverseX360ButtonMapping`）は温存。

### 17.2 方針（案）
- **I1**: `BackingStore`／`IEnvironmentService` を必須化。DI は `ServiceRegistration` で `Global.store` を渡すファクトリー登録。`Global` の静的フォールバックは、明示的に渡す。
- **I2**: `Program.rootHub` の 4 件は、`Func<ControlService>`（D4-3）で受け取り、`GetController(index)` で取得する。`ControlService` の生成が `IProfileSettingsService` に依存するため、遅延解決が必須。レポートごとの経路（`GetRumbleBoost`）では、1 回の `Func` 呼び出し（DI の Singleton の取得）が増える。割り当てテスト（`MappingHotPathAllocationTests` など）で確認する。
- **I3**: `Global.ProfileActions` のフォールバックと同期ループは、削除する（`SafeConfig` が非 null になるため。本番では同じ実体で、挙動は変わらない）。
- **テスト**: `TestRepositoryFactory.CreateProfileSettingsService(store = null, env = null, control = null)`（既定: `Global.store`、`new EnvironmentService()`、`() => null`）で、約 80 件の生成箇所を置換。ガードテストに対象を追加。
- **実機確認項目**: プロファイルの読み込み・保存、振動（DualSense を含む。DualSense の振動強度の補正）、振動の自動停止時間の設定変更が、接続中のコントローラーに反映されること。

## 18. Step10-4-2c の実装記録。2026-10-15
- **決定**: I1（必須化）、I2＝A（`Func<ControlService>`）、I3＝A（`Global.ProfileActions` の同期ループとフォールバックを削除）。
- **変更**: `ProfileSettingsService` は、`BackingStore`／`IEnvironmentService`／`Func<ControlService>` を必須化。`Program.rootHub`×4 を `GetController(index)` へ、`Global.store`／`new EnvironmentService()` のフォールバックを削除。`ServiceRegistration` はファクトリー登録（`Global.store` を渡す）。`Global` の静的フォールバックは、明示的に引数を渡す形へ。
- **テスト**: `TestRepositoryFactory.CreateProfileSettingsService`（79 件の生成箇所を置換）、`Step10FourTwoGlobalReferenceGuardTests` に対象を追加。
- **実機確認項目**: プロファイルの読み込み・保存、振動（DualSense の補正を含む）、振動の自動停止時間の変更の反映。
- **後続**: クラスタ 1 は完了。続けて、アクション連鎖の廃止（10-4-2d）と K1（`>= 4` の上限）の小ステップを提案し、その後 10-4-3（出力スロット系）へ進む。

## 19. Step10-4-2c の完了と、Step10-4-2d（アクション連鎖の廃止）の計画。2026-10-15
- **Step10-4-2c**: ビルド・テストビルド・テスト実行成功。実機確認は、プロファイルの読み込み・保存は問題なし。振動（DualSense の補正）と振動の自動停止時間の反映は、デバイスが無いため先送り（`Phase6-Step11-Plan.md` §3.3 に登録）。クラスタ 1 は完了。

### 19.1 対象の再確認（連鎖の廃止。§15 の決定）
- **使用箇所**: `ProfileApplicationService.ApplyFromAction` の 2 か所（`DispatchNextActions`）だけ。`IProfileActionChainService`／`ProfileActionChainService` の呼び出し元は、他に無い。
- **`IMappingActionDispatcher.DispatchProfileActionEdge`**／`MappingActionDispatcher`／`Mapping.DispatchProfileActionEdge`: 連鎖の呼び出し元は `ProfileActionChainService` のみ（`Mapping` 内部の呼び出しは別）。連鎖の廃止後も、`Mapping` 内の利用は残る。
- **`IProfileActionProvider`**: `ControlService` も使うため、残す。

### 19.2 方針（案）
- **J1**: `ProfileApplicationService` から、`IProfileActionChainService` の依存と `DispatchNextActions` の 2 か所の呼び出しを削除する。プロファイルの適用だけを行う。
- **J2**: `IProfileActionChainService`／`ProfileActionChainService`／DI 登録／テスト（`ProfileActionChainServiceTests`）を削除する。
- **J3**: `IMappingActionDispatcher`／`MappingActionDispatcher` は、他に利用者が無いかを再確認し、無ければ §2.4 の手順（経緯の調査と判断記録）で扱う。本ステップでは削除せず、判断を記録する。
- **J4**: カスケードループ防止（Fresh-press ガード、`IsExecuting`）は、連鎖とは別の機構で、影響しない。ガードの契約テストが通ることを確認する。
- **実機確認項目**: プロファイル切替アクションの発火後、同じトリガーの別アクションが、再度トリガーを押すまで実行されないこと。切替と元への復帰が正常であること。

## 20. Step10-4-2d の実装記録。2026-10-15
- **決定**: J2＝A（連鎖の型・DI 登録・テストを削除）、J3＝A（`IMappingActionDispatcher`／`MappingActionDispatcher` は温存し、10-4-4 で整理）。
- **変更**
  - `ProfileApplicationService`: `IProfileActionChainService` の依存と、`DispatchNextActions` の 2 か所の呼び出しを削除（引数は 4 つ）。
  - 削除: `IProfileActionChainService.cs`、`ProfileActionChainService.cs`、`ProfileActionChainServiceTests.cs`、DI 登録。
  - テスト: `ProfileApplicationServiceTests` の `FakeActionChainService`、`Step10FourTwoGlobalReferenceGuardTests` の連鎖関連を整理。
- **挙動の変更（承認済み）**: プロファイル切替アクションの後、新プロファイルの同じトリガーのアクションは、トリガーを再成立させない限り実行されない。
- **§2.4 の判断記録（J3）**: `IMappingActionDispatcher`／`MappingActionDispatcher` は、連鎖の廃止により、`DispatchProfileActionEdge` の呼び出し元が 0 件になった。`Mapping.DispatchProfileActionEdge`（内部の利用あり）への中継であり、10-4-4（Actions／Mapping 境界の整理）で、利用者の有無を再確認して扱いを決める。
- **実機確認項目**: プロファイル切替アクションの発火後、同じトリガーの別アクションが実行されないこと（再度押すと実行される）。切替と元への復帰が正常。
- **テストの記録（2026-10-15）**: 全件実行で `ControlServicePr4ShimEquivalenceTests.GetterShims_UsedOnReportPath_DoNotAllocate` が 1 回失敗（10,000 回で 1,128 バイト）。同テストの単独再実行は 3 件とも成功。測定対象の経路は変更しておらず、割り当てが 1 回だけだったため、層別 JIT などによるフレーキーと判断。再発した場合は、原因を調査する。

## 21. Step10-4-2d の実機確認結果と、Step10-4-2e（Fresh-press ガードの全型への適用）。2026-10-15
- **Step10-4-2d**: ビルド・テストビルド・テスト実行成功。切替と元への復帰は問題なし。連鎖の廃止の確認では、切替の直後に同じトリガーの `zzz_DC`（`DisconnectBT`）が実行された。
- **原因（ログ解析）**: 連鎖サービスは動作しておらず（廃止は有効）、**通常のマッピング評価**が、押しっぱなしのトリガーを新プロファイルの `DisconnectBT` の新規成立と判断して実行した。Fresh-press ガード（`RequiresFreshPressAfterReset`）は `Mapping.cs` の `Program`／`Profile`／`GyroCalibrate` にだけあり、`DisconnectBT`／`Macro`／`Button`／`Key`／`BatteryCheck`／`SASteeringWheelEmulationCalibrate` には無かった。**移行前から存在する動作**（回帰ではない）。
- **決定**: 案A（ガードを全型へ拡張）。ユーザー承認済み（2026-10-15）。
- **変更**: `Mapping.cs` の型ごとの分岐の直前に、上記 6 型のガード（`freshPressBlockedOtherTypes`）を追加。ガード中は動作だけを止め、ボタンの抑制は維持される。解除条件は既存と同じ（トリガーが `FreshPressReleaseHoldMs`＝80ms 連続して未成立）。
- **テスト**: `Step10FourTwoGlobalReferenceGuardTests.Mapping_FreshPressGuard_CoversAllSpecialActionTypes`（ソース走査）。実機確認で動作を確認する。
- **副作用（仕様）**: プロファイル適用・コントローラー接続の直後に押しっぱなしのボタンは、いったん離すまで、これらの型のアクションが効かない。
- **実機確認項目**: 切替の直後に、同じトリガーの `DisconnectBT`／キー入力／マクロが実行されないこと。押し直すと実行されること。通常の操作（切替をしない状態でのキー入力・マクロ・バッテリー確認）が従来どおり動くこと。

## 22. Step10-4-2e の完了と、Step10-4-2f（K1 の是正）の計画。2026-10-15
- **Step10-4-2e**: ビルド・テストビルド・テスト実行成功。実機確認問題なし。ログ上、切替から `zzz_DC`（`DisconnectBT`）の成立までが約 1.5〜2.3 秒（修正前は約 30ms）。切替の直後の即時成立は発生していない。連鎖サービスは動作していない。クラスタ 1（10-4-2a〜2e）は完了。

### 22.1 K1 の現行コード（`Phase6-Status.md` §6.5、`Phase6-Step3-Plan.md` §2.4.3）
- **K1-a（スロット 5〜8 の拒否）**: `ProfileApplicationService` の 4 か所（`ApplyFromAction`、`RestoreFromAction`、`ClearPendingRestore`、`ApplyProfile`）が `deviceIndex >= 4`。スロット上限は `IEnvironmentService.ControllerSlotLimit`（Windows 8 以上で 8）。配列（`olderProfilePath` など）は 9 要素で、8 スロット分は確保済み。
- **K1-b（戻り値の破棄）**: `ApplyProfile` は `Global.ApplyProfile` の `bool` を捨て、直後に `success = true` とする（失敗でも成功を返す）。
- **K1-c（Halt）**: `source == MappingAction` のときは Halt せずに適用する（入れ子の Halt を避けるため。接続処理中の Halt との関係は未整理）。
- **影響する呼び出し元**: `AutoProfileService`（スロット 5〜8 の自動プロファイルが拒否される）、`DefaultProfileSwitcher`、`Mapping.cs` のプロファイル切替アクション（`ApplyFromAction`。5〜8 番目のコントローラーでは動作しない可能性）。`ProfileSlotApplier`／`Mapping.cs` の `Global.ApplyProfile` フォールバックは、K1 の是正後に置換する予定（Step12 の「フェーズG」）。

### 22.2 方針（案）
- **M1**: 4 か所の `>= 4` を、`IEnvironmentService.ControllerSlotLimit`（必須の依存として注入）に置き換える。
- **M2**: `ApplyProfile` は `Global.ApplyProfile` の戻り値を使い、失敗なら `false` を返す（失敗時のログは既存の `[DI]` 接頭辞で維持）。
- **M3（K1-c）**: Halt の扱いは変更しない。接続処理中の入れ子の Halt は、現行の `Global.ApplyProfileToSlot`／`ProfileSlotApplier` の経路を維持する。
- **M4**: `Global.ApplyProfile` フォールバックの置換（Mapping／ProfileSlotApplier）は、本ステップでは行わない（Step12 または Phase7）。
- **テスト**: スロット 4〜7 を受け入れること、範囲外（8 以上、負）を拒否すること、失敗の戻り値が呼び出し元へ伝わること。
- **実機確認**: 5 台以上のコントローラーが必要なため、確認できない見込み。先送り台帳（`Phase6-Step11-Plan.md` §3.3）へ登録する。1〜4 番目のコントローラーでの、プロファイル切替と復帰（回帰確認）は実施する。

## 23. Step10-4-2f の実装記録（K1 の是正）。2026-10-15
- **決定**: N1＝A（K1 を是正）、N2＝A（戻り値を使用）。
- **変更**: `ProfileApplicationService` に `IEnvironmentService` を必須引数として追加。4 か所の `deviceIndex >= 4` を `ControllerSlotLimit` に置換（K1-a）。`ApplyProfile` は `Global.ApplyProfile` の戻り値を `success` に使う（K1-b）。Halt の扱いは変更なし（K1-c は温存）。`ProfileSlotApplier` の TODO コメントを更新。
- **挙動の変更**: 失敗した適用が、成功として返されなくなる。5〜8 番目のスロットも対象になる。
- **テスト**: `ProfileApplicationService_SlotLimit_FollowsEnvironment_NotFixedFour`（上限の内側は受け入れ、上限と負の値は拒否）。`ProfileApplicationService_RequiresAllDependencies` に環境サービスを追加。
- **実機確認項目**: 1〜4 番目のコントローラーでの、プロファイル切替アクションと復帰、自動プロファイル（回帰確認）。5〜8 番目は先送り（`Phase6-Step11-Plan.md` §3.3 に登録）。

## 24. Step10-4-2f の完了と、Step10-4-3（クラスタ 2: 出力スロット系）の計画。2026-10-15
- **Step10-4-2f**: ビルド・テストビルド・テスト実行成功。実機確認（プロファイル切替と復帰、自動プロファイルの切替と復帰）問題なし。クラスタ 1 は全て完了。

### 24.1 現行コードの再確認
- **OutputSlotPersist**（静的クラス）: `Global.appdatapath`×2（読み書きの保存先）、`Global.exeversion`×1（ヘッダーのコメント。`OutputSlotPersistDTO` にも 1 件）。呼び出し元は `OutputSlotStore`（2）と `ControlService`（`WriteConfig` と `ReadConfig` の各 1 か所）。
- **OutputSlotStore**: `IPathService` は省略可（`AppHost`、`new PathService()` のフォールバック）。受け取った `_pathService` は読み書きに使われておらず、保存先は `Persist` 側の `Global.appdatapath` のまま（注入が実質無効）。
- **OutputSlotManager**: `Global.vigemBusVersionInfo`×2（`AllocateController`）。`ViGEmBus` のバージョンは、実行時に `RefreshViGEmBusInfo` で更新されるため、取得は都度の読み取りが必要。生成箇所は `ControlService`（1）、`OutputSlotService` のフォールバック（1）、テスト（3）。
- **OutputSlotService**: 全引数が省略可。`Program.rootHub`（`ControlService`）、`control.OutputslotMan`、`AppHost` の `IOutputSlotStore`、`new OutputSlotManager()`、`new OutputSlotStore()` のフォールバック。`Global.outDevTypeTemp`／`activeOutDevType` は Phase7 へ温存済み（Step5 決定）。生成箇所は DI、`ScpUtil.cs` の静的フォールバック、テスト約 20 件。
- **`IEnvironmentService`** には、ViGEmBus のバージョンを返す契約が無い（`IsViGEmBusInstalled`／`IsRunningSupportedViGEmBus` などのみ）。

### 24.2 方針（案）と分割
- **10-4-3a（永続化と割り当て）**
  - **Q1**: `OutputSlotPersist.ReadConfig／WriteConfig` に保存先のディレクトリ（`IPathService.AppDataPath`）を引数として渡す形にし、`Global.appdatapath` を除去する。`OutputSlotStore` は `IPathService` を必須にし、渡す。`ControlService` の 2 か所の呼び出しは、保持している `_pathService` を渡す形に更新する（`ControlService` の他の置換は 10-4-5）。`Global.exeversion`（不変の値）は、温存する。
  - **Q2**: `IEnvironmentService` に `Version ViGEmBusVersion`（`Global.vigemBusVersionInfo` への薄い委譲）を追加。`OutputSlotManager` は `Func<Version>` を必須引数にし、都度読み取る。`ControlService` が `() => _environmentService.ViGEmBusVersion` を渡す。
- **10-4-3b（出力スロットサービス）**
  - **Q3**: `OutputSlotService` の引数を、全て必須にする: `Func<OutputSlotManager>`（スロット管理の取得。`ControlService` が所有する実体を、遅延で取得）、`IOutputSlotStore`、`Func<ControlService>`。`DI` は `sp.GetRequiredService<ControlService>().OutputslotMan` を渡す。`new OutputSlotManager()`／`new OutputSlotStore()`／`Program.rootHub`／`AppHost` を削除。`Global` の静的フォールバックは、遅延生成にする。
- **テスト**: `TestRepositoryFactory` に出力スロット系の生成ヘルパーを追加し、約 25 件を置換。ガードテストを追加。
- **実機確認項目**: 出力スロットの管理画面（Output Slots）での、仮想コントローラーの追加・取り外し・状態表示。コントローラー接続時の仮想デバイス（X360／DS4）の生成。出力スロットの設定の保存と、再起動後の復元（`OutputSlots.xml`）。

### 24.3 決定事項（要承認）
- **Q1**: `OutputSlotPersist` の保存先を引数にする（A）／静的クラスのまま `Global` を温存する（B）。
- **Q2**: `IEnvironmentService` に `ViGEmBusVersion` を追加し `Func<Version>` で渡す（A）／`OutputSlotManager` を温存する（B）。
- **Q3**: `OutputSlotService` を `Func<OutputSlotManager>`＋`Func<ControlService>` で必須化する（A）／温存する（B）。

## 25. Step10-4-3（クラスタ 2）の実装記録。2026-10-15
- **決定**: Q1＝A、Q2＝A、Q3＝A（すべて推奨案）。10-4-3a と 10-4-3b は、コンパイルの依存（`OutputSlotService` が `new OutputSlotManager()` を使うため）から、一括で実装した。
- **変更**
  - `OutputSlotPersist`: `ReadConfig`／`WriteConfig` が保存先のディレクトリを引数で受け取る（`Global.appdatapath` を除去）。`Global.exeversion` は温存。
  - `OutputSlotStore`: `IPathService` を必須化し、保存先として渡す（これまで注入が実質無効だった状態を是正）。
  - `IEnvironmentService`／`EnvironmentService`: `ViGEmBusVersion` を追加（`Global.vigemBusVersionInfo` への薄い委譲）。
  - `OutputSlotManager`: `Func<Version>` を必須化し、割り当て時に都度読み取る。`ControlService` は `() => _environmentService.ViGEmBusVersion` を渡し、`OutputSlotPersist` の 2 か所の呼び出しに `_pathService.AppDataPath` を渡す。
  - `OutputSlotService`: `Func<OutputSlotManager>`／`IOutputSlotStore`／`Func<ControlService>` を必須化。`Program.rootHub`／`AppHost`／`new` を削除（`Phase6-Step10 §8` の TODO を解消）。`ServiceRegistration` はファクトリー登録。`Global` の静的フォールバックは、初回アクセス時の生成に変更。
- **テスト**: `TestRepositoryFactory` に `CreateOutputSlotManager`／`CreateOutputSlotService` を追加し、生成箇所を置換。`Step10FourTwoGlobalReferenceGuardTests` に対象 4 ファイルと、必須引数・保存先の注入・バージョンの都度読み取りを追加。
- **実機確認項目**: 出力スロット管理画面（Output Slots）での仮想コントローラーの追加・取り外し・状態表示、コントローラー接続時の仮想デバイス（X360／DS4）の生成、出力スロットの保存と再起動後の復元（`OutputSlots.xml`）。

## 26. Step10-4-3 のテスト失敗と既存の不具合の発見。2026-10-15
- **事象**: `OutputSlotStore_Save_UsesInjectedPath` が、`store.Save(manager)` の戻り値 `false` で失敗した。
- **原因**: `OutputSlotPersist.WriteConfig` は、書き込みに成功しても `result` を `true` にせず、常に `false` を返す。**上流（2020-05-21、`8071fcfc`）から存在する不具合**で、今回の移行の回帰ではない。影響: `ControlService` は戻り値を使わず、`IOutputSlotService.SaveOutputSlots()` の呼び出し元は 0 件のため、実害はない。
- **対応**: テストは、戻り値ではなく、注入された保存先へのファイル出力のみを検証する形に変更した。戻り値の是正は、ユーザーの判断（案A: 書き込み成功時に `result = true`／案B: TODO を付けて温存）を待つ。
- **決定（2026-10-15）**: 案A。`OutputSlotPersist.WriteConfig` は、書き込み成功時に `result = true` を返す（1 行の是正）。テストは、戻り値の `true` を検証する形に戻した。失敗時（`UnauthorizedAccessException`）は従来どおり `false`。

## 27. StandaloneTests の失敗（ActionDoneFlagRoundtrip）。2026-10-15
- **事象**: `StandaloneTests.ActionManagerSmokeTests.ActionDoneFlagRoundtrip` が、10-4-3 の後に単独実行でも失敗（`IsToggledOn should be true after set`）。`ActionManager` 自体は未変更。
- **原因（コード読解による推定。実行による確認は未了）**: 旧 `Global` の静的初期化で、`new OutputSlotService()`（フォールバック）が `AppHost` を触り、DI ホストがテストの最初の `GetStateFor` より前に作られていた（K7-1 の副作用）。10-4-3 でフォールバックを遅延生成にしたため、ホストが `GetStateFor` の途中で初めて作られ、1 回目（静的辞書）と 2 回目（`DefaultActionManager`）で別の状態を返した。テストが、ホストの生成タイミングに暗黙に依存していた。
- **対応**: テストの冒頭で `AppHost.CreateHost()` を明示的に呼ぶ。本番は `App` が明示的にホストを構築するため影響しない。
- **関連**: ホストの生成を `Global` の静的初期化に頼らない整理は、K7-1（`Phase6-Status.md` §6.5）の持ち越しに含まれる。
- **確認（2026-10-15）**: 修正後、`ActionDoneFlagRoundtrip` が成功。原因の推定（ホストの生成タイミングへの暗黙の依存）が裏づけられた。

## 28. Step10-4-3 の完了と、Step10-4-4（クラスタ 3: Actions/*）の計画。2026-10-15
- **Step10-4-3**: ビルド・テストビルド・テスト実行成功、実機確認（Plug／Unplug、Permanent の保存と復元）問題なし。副産物として、`OutputSlotPersist.WriteConfig` の戻り値の是正（§26）と、`StandaloneTests` のホスト生成タイミング依存の是正（§27）を実施。

### 28.1 現行コードの再確認
- **葉のクラス（フォールバックで `AppHost`／`new` を使う）**: `LaunchProcessAction`（`IProcessLauncher`）、`MacroAction`（`IMacroPlayer`。3 か所）、`ProfileSwitchAction`（`IProfileSwitcher`。3 か所）、`MouseOutputAction`（`IVirtualKBM`）、`DefaultMacroPlayer`（`IVirtualKBM`）、`DefaultActionManager`（`IActionFactory`）。
- **生成の流れ**: `DefaultActionFactory`（DI の Singleton）が、`new KeyActionAdapter／LaunchProcessActionAdapter／MacroActionAdapter／ProfileSwitchActionAdapter(sa, index)` を生成し、アダプタの内部で、葉のクラスが `AppHost` を引いて依存を解決している（Service Locator）。
- **静的な互換の入口**: `ActionFactory`（静的。`ServiceProviderHolder.Provider` が無い場合の最小フォールバックで、アダプタを `new`）、`ActionManager`（静的ファサード。`ServiceProviderHolder.Provider`×12、`Global.getProfileActions`／`GetProfileAction`×2）。`ServiceProviderHolder` は D4-4 により Phase7／Phase8 へ温存済み。
- **`MouseOutputAction`**: 本番の呼び出し元は `Mapping.cs` の `GetService`（常に null。Step9b-3 の F03 で「未登録・未完成。登録しない」と判断済み）とテストのみ。TODO 付きで温存済みのため、引数だけ必須化する。
- **`AutoProfileChecker`**（§2.4 の調査結果）: 2026-09-08（`22ba46d4`）に「`IAutoProfileService` を直接使うため、AutoProfileChecker を廃止」としたが、型だけが残った。呼出元は 0 件。`MainWindow.xaml.cs:564` の、未使用のハンドラーのオーバーロード（`AutoProfileChecker sender` を引数に持つ）が型を参照しているだけ。→「廃止済み機能の残骸」（使うべきでないもの）に当たる。
- **`IMappingActionDispatcher`／`MappingActionDispatcher`**（J3）: 2026-09-04（`874fb025`）に、`ProfileActionChainService` のために作られた。連鎖の廃止（10-4-2d）で、呼出元が 0 件。→「正規の経路と重複するだけの並行実装」（使うべきでないもの）に当たる。`Mapping.DispatchProfileActionEdge`（静的）は、`Mapping` 内部で使われており、そのまま残す。

### 28.2 方針（案）
- **S1（葉のクラスの必須化）**: 上記 6 クラスの依存を必須引数にし、`AppHost`／`new Default*` のフォールバックと、依存を省略する旧コンストラクターを削除する。
- **S2（`DefaultActionFactory`）**: `IMacroPlayer`、`IProcessLauncher`、`IProfileSwitcher`（いずれも DI 登録済み）を、コンストラクターで受け取り、アダプタ経由で葉のクラスへ渡す。アダプタには、依存を受け取るコンストラクターのみを残す。
- **S3（静的 `ActionFactory` のフォールバック）**: DI の依存を作れないため、ホストが無い場合は `Key` 型（`KeyAction`。DI を要しない）のみを返し、それ以外は `null` を返す（ホストが無い状態は、テスト等に限られる。TODO を付ける）。
- **S4（`ActionManager.PreallocateForProfileApply`）**: `Global.getProfileActions`／`GetProfileAction` を、`ServiceProviderHolder` 経由の `IProfileActionProvider` に置換する。
- **S5（`AutoProfileChecker`／`IMappingActionDispatcher`）**: 規約 §2.4 に従い、上記の調査結果を根拠として削除する（型、DI 登録、`MainWindow` の未使用オーバーロード）。
- **テスト**: 既存テストの生成箇所（`DefaultActionManagerTests`、`MacroActionTests`、`LaunchProcessActionTests`、`VirtualKBMTests` など）を更新。ガードテストを追加。
- **実機確認項目**: マクロの実行、プログラム起動アクション、プロファイル切替アクション、キー押下アクション（Press／Toggle）、ボタン割り当てが、従来どおり動くこと。

### 28.3 決定事項（要承認）
- **S1**: 葉のクラスの依存を必須化する（A）／温存する（B）。
- **S2・S3**: `DefaultActionFactory` で依存を受け取り、静的フォールバックは `Key` 型のみにする（A）／静的フォールバックでも `new Default*` を維持する（B）。
- **S5**: `AutoProfileChecker`、`IMappingActionDispatcher`／`MappingActionDispatcher` を削除する（A）／温存して TODO を付ける（B）。

## 29. Step10-4-4 の実装記録。2026-10-15
- **決定**: S1＝A、S2・S3＝A、S4（置換）、S5＝A（すべて推奨案）。
- **変更**
  - S1: `MacroAction`、`ProfileSwitchAction`、`LaunchProcessAction`、`MouseOutputAction`、`DefaultMacroPlayer`、`DefaultActionManager` の依存を必須化。`AppHost`／`new Default*` のフォールバックと、依存を省略する旧コンストラクターを削除。
  - S2: `DefaultActionFactory` が `IMacroPlayer`／`IProcessLauncher`／`IProfileSwitcher` を受け取り、アダプタ経由で各アクションへ渡す。アダプタの、依存を省略するコンストラクターを削除。
  - S3: 静的 `ActionFactory` のホストが無い場合のフォールバックは、`Key` 型のみ返す（TODO: Phase7／Phase8 で `DefaultActionFactory` へ統合）。
  - S4: `ActionManager.PreallocateForProfileApply` が、`Global.getProfileActions`／`GetProfileAction` の代わりに、`ServiceProviderHolder` 経由の `IProfileActionProvider` を使う。
  - S5（§2.4 の判断記録）: 削除 — `AutoProfileChecker`（廃止済み機能の残骸。2026-09-08 `22ba46d4`）、`IMappingActionDispatcher`／`MappingActionDispatcher`（連鎖の廃止で呼出元 0 件。2026-09-04 `874fb025`）、DI 登録、`MainWindow` の未使用オーバーロード（`AutoprofileChecker_RequestServiceChange(AutoProfileChecker, bool)`。ハンドラーを 1 つに統合）。`Mapping.DispatchProfileActionEdge`（静的）は残す。
- **テスト**: `TestRepositoryFactory.CreateDefaultActionFactory`、`Step10FourTwoGlobalReferenceGuardTests` に Actions の 9 ファイルと必須引数、工場の受け渡しを追加。`DefaultActionManagerTests`／`VirtualKBMTests` の生成箇所を更新。
- **実機確認項目**: キー押下（Press／Toggle）、マクロ、プログラム起動、プロファイル切替、ボタン割り当てのスペシャルアクション、自動プロファイル（`MainWindow` のハンドラーの統合の回帰確認）。

## 30. Step10-4-4 の完了と、Step10-4-5（MainWindow・ControlService）の計画。2026-10-15
- **Step10-4-4**: ビルド・テストビルド・テスト実行成功。実機確認（キー押下の Press／Toggle、マクロ、プログラム起動、プロファイル切替、自動プロファイル）問題なし。経緯: `Mapping.cs` の旧コンストラクター呼び出し 2 か所（`LaunchProcessAction`、`ProfileSwitchAction`）と、ミニの DI コンテナを組むテスト 2 件、`new DefaultActionManager()` のテスト 7 件を、必須引数に合わせて更新した。

### 30.1 現行コードの再確認（コメントを除く）
- **MainWindow.xaml.cs**
  - コンストラクターのフォールバック 7 件: `Global.ProfileSettingsServiceInstance`／`PathServiceInstance`／`EnvironmentServiceInstance`／`OutputSlotServiceInstance`／`ProfileRepositoryInstance`／`NotificationServiceInstance`、`Program.rootHub`。
  - `Global.ProfilePath`×3、`Global.OlderProfilePath`×1（`SyncProfileListAndControllers`）。`IProfileRepository` に同名の契約がある。
  - `Global.ApplyProfileToSlot`×2（`SelectProfCombo_SelectionChanged`、`SyncProfileListAndControllers`）。`IProfileSlotApplier.ApplyToSlot`（Global への薄い委譲）がある。
  - 定数（`RESOURCES_PREFIX`×6、`TEST_PROFILE_INDEX`×1）は温存。
- **ControlService.cs**
  - `Global.ProfileSettingsServiceInstance` のフォールバック 1 件（引数 `profileSettings`）。
  - ViGEm 関連: `Global.RefreshViGEmBusInfo`×1、`IsRunningSupportedViGEmBus`×2、`vigemInstalled`×1、`vigembusVersion`×2。`IEnvironmentService` に `RefreshViGEmBusInfo`／`IsRunningSupportedViGEmBus`／`IsViGEmBusInstalled`／`ViGEmBusVersion` が既にある。
  - 定数（`MAX_DS4_CONTROLLER_COUNT`、`TEST_PROFILE_INDEX`）は温存。`ControlService` の生成は DI のみ（テストでの直接生成なし）。

### 30.2 方針（案）
- **T1（MainWindow）**: フォールバック 7 件を削除し、未登録なら例外にする。`Global.ProfilePath`／`OlderProfilePath` は `profileRepo` の同名のプロパティへ、`Global.ApplyProfileToSlot` は `IProfileSlotApplier.ApplyToSlot` へ置換する（`IProfileSlotApplier` は `AppHost` から解決。MainWindow は Composition Root に隣接する窓のため、`AppHost` の使用は従来どおり）。
- **T2（ControlService）**: ViGEm 関連 6 件を `_environmentService` へ置換。`profileSettings` のフォールバックを削除し、必須にする。ログの文言は維持（バージョンは `Version.ToString()`。実デバイス情報の 4 要素の書式で、従来の文字列と同じ）。
- **テスト**: `ProfileEditorTargetDeviceGuardTests`（182 行）と `Step10GroupThreeGlobalReferenceGuardTests`（148 行）の、`Global.ApplyProfileToSlot(` を数える検査を、`profileSlotApplier.ApplyToSlot(` に更新。新規のガードを `Step10FourTwoGlobalReferenceGuardTests` に追加（`ControlService` の許可リストは、定数のみ）。
- **実機確認項目**: 起動時のログ（ViGEmBus のバージョン表示）、プロファイル一覧の選択による適用、プロファイル編集画面での保存・名前変更の反映（`SyncProfileListAndControllers`）、サービスの開始・停止。

### 30.3 決定事項（要承認）
- **T1**: MainWindow のフォールバック削除と置換を行う（A）／温存する（B）。
- **T2**: ControlService の ViGEm 関連と `profileSettings` を置換する（A）／温存する（B）。

## 31. Step10-4-5 の実装記録。2026-10-15
- **決定**: T1＝A、T2＝A（推奨案）。
- **変更**
  - `MainWindow`: コンストラクターのフォールバック 7 件（`Global.*Instance`×6、`Program.rootHub`）を削除。`Global.ProfilePath`×3／`OlderProfilePath`×1 を `profileRepo` へ、`Global.ApplyProfileToSlot`×2 を `IProfileSlotApplier.ApplyToSlot` へ置換（`IProfileSlotApplier` は `AppHost` から解決）。
  - `ControlService`: `profileSettings` のフォールバックを削除（必須）。ViGEm 関連 6 件（`RefreshViGEmBusInfo`／`IsRunningSupportedViGEmBus`×2／`vigemInstalled`／`vigembusVersion`×2）を `_environmentService` へ置換。
- **テスト**: `ProfileEditorTargetDeviceGuardTests`（182 行）と `Step10GroupThreeGlobalReferenceGuardTests`（148 行）の検査を、`profileSlotApplier.ApplyToSlot(` に更新。`Step10FourTwoGlobalReferenceGuardTests` に、`MainWindow`／`ControlService` が定数以外の `Global` と `Program.rootHub` を持たない検査を追加。
- **実機確認項目**: 起動時のログ（ViGEmBus のバージョン表示）、プロファイル一覧の選択による適用、編集画面の保存・名前変更の反映、サービスの開始・停止。
- **テスト更新（2026-10-15）**: `ControlServiceGlobalReferenceGuardTests` の許容リストから、置換済みの 6 項目（`ProfileSettingsServiceInstance`、`RefreshViGEmBusInfo`、`IsRunningSupportedViGEmBus`、`vigembusVersion`、`vigemInstalled`）を除き、定数 2 件（`MAX_DS4_CONTROLLER_COUNT`、`TEST_PROFILE_INDEX`）のみとした。

## 32. Step10-4-5 の実機確認結果と、Step10-4-5b（名前変更後の Selected Profile 空欄の是正）。2026-10-15
- **Step10-4-5**: ビルド・テストビルド・テスト実行成功。実機確認（起動ログ、プロファイル適用、編集画面の反映、サービスの開始・停止）問題なし。
- **発見**: メイン画面のプロファイル一覧から、接続中のコントローラーに適用中のプロファイルの名前を変更し、サービスを停止→開始すると、コントローラータブの Selected Profile が空欄になる。
- **原因**: `MainWindow` の名前変更（`entity.RenameProfile`）が、ファイルと一覧の項目名だけを変え、各スロットの現在のプロファイル名（`ProfilePath`／`OlderProfilePath`）と、リンクプロファイル（`LinkedProfiles.xml`）の旧名を更新しない。再接続時に旧名を探して、ファイルが無く空欄になる。上流の 2021-06-18（`d8d1c170`、アプリ内での名前変更の許可）から変わっておらず、**今回の移行の回帰ではない**。プロファイル編集画面の名前変更は、Step10-3b で共通経路（`SyncProfileListAndControllers`）を通るようにしてあり、この不具合は無かった。
- **決定**: 案A（共通経路へ統合し、リンクプロファイルも更新する）。ユーザー承認済み。
- **変更**
  - `MainWindow` の名前変更: 成功時に `SyncProfileListAndControllers(oldProfile, newProfile)` を呼ぶ。
  - `SyncProfileListAndControllers` の名前変更の分岐: `profileRepo.ReplaceLinkedProfileName(oldProfile, newProfile)` を呼ぶ（編集画面の名前変更でも、リンクプロファイルが更新される）。
  - `IProfileRepository`／`ProfileRepository`: `ReplaceLinkedProfileName` を追加（`LinkedProfiles.xml` の値と `LinkedProfileUI` の旧名を新名へ置換。接続中でないコントローラーの紐づけも対象。置換があれば保存）。
- **残る範囲（未対応）**: 自動プロファイル（`Auto Profiles.xml`）の旧名の参照置換は、本ステップでは行わない（別の小ステップとして提案）。
- **実機確認項目**: 接続中のコントローラーに適用中のプロファイルの名前を、メイン画面の一覧から変更→サービスの停止→開始→Selected Profile に新名が表示されること。リンクプロファイルを有効にしたコントローラーで、アプリを再起動しても新名が使われること。

## 33. Step10-4-5c（名前変更時の自動プロファイルの旧名の置換）。2026-10-15
- **Step10-4-5b**: ビルド・テストビルド・テスト実行成功、実機確認問題なし。
- **追加の要望（ユーザー）**: 自動プロファイル（`Auto Profiles.xml`）が旧名を参照している場合の置換も、今回行う。
- **変更**: `AutoProfileHolder.ReplaceProfileName(oldName, newName)` を追加（各ルールのコントローラーごとのプロファイル名の旧名を新名へ置換。1 件以上置換したら、注入された `IPathService` の `Auto Profiles.xml` へ保存）。`MainWindow.SyncProfileListAndControllers` の名前変更の分岐から、`_autoProfileService.Holder.ReplaceProfileName` を呼ぶ（監視に使う唯一の `AutoProfileHolder` を共有しているため、バックグラウンド監視にも即時に反映される）。
- **テスト**: `AutoProfileHolder_ReplaceProfileName_ReplacesAndPersists`（置換、保存、読み直しの往復）、`MainWindow` の呼び出しのソース走査。
- **実機確認項目**: Auto Profiles 画面でルールを作り、あるプロファイルを割り当てる→そのプロファイルの名前をメイン画面から変更→Auto Profiles 画面（再表示）とアプリの再起動後も、新名が割り当てられていること。対象プロセスの起動時に、新名のプロファイルへ切り替わること。

## 34. Step10-4-5b／5c の完了と、Step10-4-6（全域のガードと文書更新）の計画。2026-10-15
- **Step10-4-5b／5c**: ビルド・テストビルド・テスト実行成功、実機確認問題なし（名前変更後の Selected Profile、リンクプロファイル、自動プロファイルの旧名の置換）。

### 34.1 全域の再走査の結果（`DS4Windows` 配下の全 `.cs`、コメントを除く。2026-10-15 時点）
- **`Program.rootHub`**: 6 ファイル・計 25 件。`App.xaml.cs`（12。起動処理）、`ScpUtil.cs`（9。Global の薄いシム）、`ServiceRegistration.cs`（1。`ControlService` の登録）、`ProfileEditor.xaml.cs`（1）、`Mapping.cs`（1。Phase7）、`DS4Sixaxis.cs`（1。第1層。Step12／Phase7）。
- **`AppHost.GetService`／`AppHost.Services`**: 9 ファイル・計 51 件。`MainWindow.xaml.cs`（17）、`ScpUtil.cs`（12。シム）、`Mapping.cs`（6）、`App.xaml.cs`（5）、`ProfileEditor.xaml.cs`（4）、`AppHost.cs`（4。自身）、`ControlService.cs`（2）、ほか。
- **`Global.*`**: 63 ファイル・計 778 件。うち、`ScpUtil.cs`（233）と `AppSettingsService.cs`（185）、`EnvironmentService.cs`（21）などの薄いシム（実装本体が `Global` にあるもの）が過半を占める。置換対象のサービス（`ProfileRepository` 24、`ProfileApplicationService` 3、`AutoProfileService` 3 など）は、薄い委譲・実装本体が `Global` にあるもの・定数のみ。

### 34.2 方針（案）
- **U1（全域のガードテスト）**: 新規 `Step10FourGlobalRatchetGuardTests` を作る。`DS4Windows` 配下の全 `.cs` を走査し、ファイルごとの `Program.rootHub`／`AppHost.GetService|Services`／`Global.*` の件数（コメントを除く）が、上記の基準値**以下**であることを検査する（減るのは許可。増える・新しいファイルに現れるのは失敗）。以後の Step で、`Global` などへの退行が混入しないことを固定する。
- **U2（文書の更新）**: `Phase6-Status.md`（Step10-4 の完了と数値）、`Phase6-Step10-Plan.md`（進捗）、`Phase6-Step10-4-Plan.md`（§10 進捗と完了報告）、モデル図 `03-Class-Interface-Diagram.md`（契約・コンストラクターの変更）と `04-Service-Lifecycle-Spec.md`（依存の解決方式）、`DI-App-Wide-Migration-Plan.md`（概要の数値）、`Phase6-Step11-Plan.md`（先送り台帳の追加分は登録済み）。
- **U3（Step10-4 の完了報告）**: Step10 全体の完了報告書は Step10-5 の後に作成する（既定の計画どおり）。Step10-4 の完了は、`Phase6-Step10-4-Plan.md` の末尾に要約する。
- **残る課題（Step10-5 以降へ）**: `AppHost` を `MainWindow` が 17 件使っている点（Composition Root の隣接。Phase7 または Step12 で、引数による注入へ）、`Program.rootHub` の `App.xaml.cs` の 12 件（起動順序。K7-1）、`Mapping`／`SyntheticDispatcher`（Phase7）。

### 34.3 決定事項（要承認）
- **U1**: 全域のガードテストを作る（A）／作らない（B）。
- **U2**: 上記の文書をすべて更新する（A）／計画書と進捗文書のみ（B）。

## 35. Step10-4 の完了の要約。2026-10-15
- **完了したマイクロステップ**: 10-4-0（分類表）、10-4-1（小さな置換）、10-4-2a〜2f（クラスタ 1、アクション連鎖の廃止、Fresh-press ガードの全型適用、K1 の是正）、10-4-3（クラスタ 2）、10-4-4（クラスタ 3）、10-4-5（MainWindow・ControlService）、10-4-5b／5c（名前変更時の旧名の置換）、10-4-6（全域の退行ガードと文書）。すべて、ビルド・テストビルド・テスト実行成功、実機確認済み（先送りは下記）。
- **本ステップで是正した既存の不具合（回帰ではない）**: スロット 5〜8 の拒否と戻り値の破棄（K1）、`OutputSlotPersist.WriteConfig` の戻り値、`DisconnectBT` などの切替直後の即時成立（Fresh-press ガード）、名前変更後に Selected Profile が空欄になる不具合（リンクプロファイル・自動プロファイルの旧名）。
- **削除した型（規約 §2.4 の判断記録は §28）**: `IProfileActionChainService`／`ProfileActionChainService`、`IMappingActionDispatcher`／`MappingActionDispatcher`、`AutoProfileChecker`。
- **全域の基準値（`Step10FourGlobalRatchetGuardTests`）**: `Program.rootHub` 6 ファイル 25 件、`AppHost.GetService|Services` 9 ファイル 51 件、`Global.*` 63 ファイル 778 件（薄いシムが過半）。
- **先送り**: 振動（DualSense の補正）・自動停止時間の反映（10-4-2c）、スロット 5〜8 の切替・自動プロファイル（10-4-2f）→ `Phase6-Step11-Plan.md` §3.3。
- **残る課題**: `MainWindow` の `AppHost` 17 件（Phase7 または Step12）、`App.xaml.cs` の `Program.rootHub` 12 件（K7-1）、`Mapping`／`SyntheticDispatcher`（Phase7）、静的 `ActionFactory`／`ActionManager` の `ServiceProviderHolder`（D4-4。Phase7／Phase8）、`Global.ApplyProfile` の実装本体（Phase7）。

## 36. Step10-4-5d（名前変更時のプロファイル切替アクションの切替先の置換）。2026-10-15
- **発見（Step10-3d の実機確認）**: プロファイルの名前を変更すると、プロファイル切替のスペシャルアクションの切替先が、旧名のまま（なし扱い）になる。
- **原因**: 名前変更の共通経路（`SyncProfileListAndControllers`）は、リンクプロファイル（5b）と自動プロファイル（5c）の旧名を置換していたが、`Actions.xml` のプロファイル切替アクション（`details` に切替先のプロファイル名を保存）は更新していなかった。上流から存在する不具合（回帰ではない）。
- **変更**: `IProfileRepository.ReplaceProfileSwitchTarget(oldName, newName)` を追加（`Profile` 種別のアクションの `details` の旧名を新名へ置換。大文字小文字は区別しない。1 件以上置換したら `Actions.xml` へ保存）。名前変更の共通経路から呼ぶ。実行時は `action.details` を参照するため、読み込み済みの定義へ即時に反映される。
- **テスト**: `ProfileRenamePropagationTests.ReplaceProfileSwitchTarget_ReplacesOnlyMatchingProfileActions_AndSaves`。
- **実機確認項目**: プロファイル切替アクションを作る（切替先に、あるプロファイルを指定）→そのプロファイルの名前を変更→スペシャルアクションの一覧で、切替先に新名が表示され、実行すると新名のプロファイルへ切り替わること。
- **基準値の更新（2026-10-15）**: `ProfileRepository.cs` の `Global.*` を 24 → 26（`ReplaceProfileSwitchTarget` が `Global.store` へ委譲するため。リポジトリのシムの性質。`Step10FourGlobalRatchetGuardTests`）。
