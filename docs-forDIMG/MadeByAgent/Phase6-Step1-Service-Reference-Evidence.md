2026-09-18 再監査証拠資料。HEAD 59d46db47cc9d20f212b0f6fa881515b767c91ed。取得済み参照の分類記録。全件網羅・件数確定の証明ではない。aは契約候補ありを意味し無条件の置換承認ではない。

---

## この資料の採用制限（主報告による訂正）

以下は取得済み調査メモの保存であり、**監査完了報告ではない**。検索上限、概数、行範囲の未展開、分類の不整合があるため、本文の「証明済み」「完全DI化済み」「完全一致」「実測」「0件」等の断定は未検証として扱う。下記の集計は確定件数に採用しない。

- Global本体内にはstatic importがなくても無修飾の同一型メンバ参照がある。import検索だけでは無修飾参照0件を証明できない。
- `App.xaml.cs` はこの分担の範囲外。重複した概数ではなくUI別紙を参照する。
- パスAPIの意味差はUI別紙に記載。ExecutableDirectoryとexedirpathを無条件に同等としない。
- 同名異型が別ファイルに存在することだけでは、1ファイル1型原則の違反とはならない。
- 環境プローブは純粋関数ではなく、可変辞書・配列はconstではない。
- サービスからGlobalを呼ぶ事実だけで実際の呼出循環を確定しない。Phase5各タスクの完了／未完了も元証跡の確認を要する。
- 旧Step1レポートがControlServiceを調査対象外にした、という本文§5の指摘は旧本文と一致しないため撤回する。

**本書は未検証の候補索引に限定する。全件表・除外確定表・実装承認の根拠として使用しない。**

---

# Phase6-Step1 監査報告（スコープ: DS4Forms / App.xaml.cs / ControlService.cs / Mapping.cs / DS4LightBar.cs / Mouse.cs / MouseCursor.cs を除く全 .cs）

## 0. 監査方法と件数の証明可能性

- `Global\.`（修飾形式）を `DS4Windows/**/*.cs` 全体に grep → **255件（ScpUtil.cs 自体に201件超）**。上位数件が他ファイルへ切れていたため、非限定の `\bGlobal\b` を領域別に再実行し補完（下表はその合計）。
- 無修飾形式（`using static DS4Windows.Global;`）は **`ControlService.cs:36`、`DS4LightBar.cs:22`、`Mapping.cs:31` の3ファイルのみ** — すべてユーザー指定の監査除外ファイル。**→ 本スコープ内で無修飾参照は0件（証明済み）**。
- 一部検索は表示上限（200/255件）で切れているため、**ファイル別の正確な全件数は上位数ファイル（ScpUtil.cs 等）でカットの可能性あり**。証明できる範囲のみ記載する。

## 1. ファイル別・メンバ別一覧（分類 a/b/c/除外）

### 1-1. `DS4Control/Services/`（DIサービス実装自身 — 意図的シムか移行対象か）

| ファイル:行 | 参照メンバ | 分類 | 判定根拠 |
|---|---|---|---|
| `Services\AppearanceSettingsService.cs:12-22` | `UseCurrentTheme`, `UseIconChoice`, `iconChoiceResources` | **意図的シム（除外候補）** | PR-E で新設された「Global への薄いシム」自体。ただし Phase6 完了条件「サービス内部から Global 再委譲排除」（Phase5-Status §5）と矛盾する**将来は正本をサービス側に持つ要設計(c)** |
| `Services\AppNotificationService.cs:12-19` | `Notifications`, `FlashWhenLate` | 同上（シム、Phase5-Step14 フェーズA (a)-6 で Global 委譲化**済み**＝正規経路） | `Phase5-Step14-FormSettings-Unification-Plan.md` タスク表 |
| `Services\AppSettingsService.cs:57-390` | `StartMinimized`〜`SpecialActionDetailColWidth` 約60メンバ | **意図的シム（Step13-7/14前クリーンアップの正規経路）** | コメント「Global(m_Config)への正規シム」明記。実機検証セクション10で検証対象化済み |
| `Services\AutoProfileService.cs:39-160` | `autoProfileSwitchNotifyChoice`(2), `ProfilePath[j]`(101,160), `AutoProfileRevertDefaultProfile`(141,158) | **(b) 呼出元移行対象** | `ProfilePath` は `IProfileRepository.ProfilePath`（`DI\IProfileRepository.cs:35`）で置換可能。`AutoProfileRevertDefaultProfile` は `IAppSettingsService.cs:36` に既存。`autoProfileSwitchNotifyChoice` は `IAutoProfileService.cs:33` に同名委譲あり → 実質(a)〜(b) |
| `Services\DefaultElevatedProcessLauncher.cs:14` | `exelocation` | (a) | `IPathService.ExecutablePath`（`Services\PathService.cs:41` が委譲）へ置換可能 |
| `Services\Ds4DeviceRegistryAdapter.cs:60` | `IsHidHideInstalled()` | (b) | `IEnvironmentService` にメンバ追加（1メンバ） |
| `Services\EnvironmentService.cs:20-26` | `IsAdministrator`, `exeversion`, `RefreshHidHideInfo`, `RefreshFakerInputInfo` | **意図的シム（カテゴリBに近い）** | 環境プローブ専用に純化済み（`DI\IEnvironmentService.cs:1-25` のドキュメント参照） |
| `Services\OutputKBMHandlerAdapter.cs:11-41` | `outputKBMHandler` ×22 | **意図的シム（IVirtualKBM 実体）** | ファイル先頭コメント「Global.outputKBMHandler への遅延委譲アダプタ」= Phase2 成果物。恒久対応は Phase6 完了後の Phase7 圏 |
| `Services\OutputSlotService.cs:193,196` | `outDevTypeTemp`, `activeOutDevType` | (b) | Phase5-Step13-6/7 の追加。`IProfileSettingsService`/SSOT 統一は **Phase6-Step5（移管タスク4）の主題** |
| `Services\PathService.cs:28-41` | `appdatapath`(2), `exelocation` | **意図的シム（On-Demand ガードレール §5.4 準拠）** | コメント明記。除外推奨 |
| `Services\ProfileActionProvider.cs:14` | `store`（フォールバック） | (b) | DI優先順位が不明瞭。コンストラクタ必須化が望ましい |
| `Services\ProfileApplicationService.cs:53,60,72,87-89,116` | `ApplyProfile`(3), `OlderProfilePath`(3) | **(c) 要設計（最重要）** | `Global.ApplyProfile`（ScpUtil.cs:3192）を依然直呼び。**フェーズG（`ApplyProfileToSlot`→`IProfileApplicationService` 委譲）が未実施**のため循環構造が残存。`OlderProfilePath` は `IProfileRepository.OlderProfilePath` で(a)置換可 |
| `Services\ProfileRepository.cs:19,20,26,58,77,85,190-227` | `ProfileSettingsServiceInstance`, `ProfileXmlStoreInstance`, `PathServiceInstance`, `ProfilePath[...]`, `loggedInvalidActions`, `ProfilePath`/`OlderProfilePath`/`SelectedProfile`/`LinkedProfileUI`(190-193), `SelectedProfileChanged`(197-202), `changeLinkedProfile`/`removeLinkedProfile`/`SaveLinkedProfiles`/`ProfileActions`/`CacheExtraProfileInfo`/`CacheProfileCustomsFlags`/`LoadTempProfile`/`EmitMissingActionLogsForDevice` | **混在**: 190-227は「同一配列の単一実体を複製しない」という明示的設計判断（Phase4-Step10知見、コメント記載あり=**意図的シム・除外推奨**）。19,20,26,58はDI解決フォールバック((b))、77,85は LoadProfile 内部状態書込((c): 202行で Global.ProfilePath へ直書きしつつ IProfileRepository.ProfilePath が同じ配列を返すため、**真のSSOTは依然 Global/m_Config**) | |
| `Services\ProfileSettingsService.cs:25,29,547-558,599,604` | `store`(2), `ProfileActions`(get/set 547-558 **二重書込**), `defaultButtonMapping`(599), `reverseX360ButtonMapping`(604) | (b) | `ProfileActions` セッターは `SafeConfig.profileActions` と `Global.ProfileActions` に**二重代入**（同一配列なら無害だが監査注意点）。599/604は静的初期化辞書のClone((a)カテゴリB的だが真constではないため除外しない) |
| `Services\ProfileXmlStore.cs:16` | `store`（フォールバック） | (b) | 同上 |
| `Services\SpecialActionRepository.cs:19,23,31,63,88` | `store`, `PathServiceInstance`, `appdatapath`, `LoadActions`(Global fallback), `SaveActions`(Global fallback) | (b) | Globalフォールバック分岐はDI解決順序の保険。呼出元移行後の Step15 でフォールバック削除候補 |
| `Services\DefaultElevatedProcessLauncher.cs` 以外の `IElevatedProcessLauncher.cs:12` | （コメント参照のみ） | 除外（コメント） | |

### 1-2. `DS4Control/` 直下の補助クラス

| ファイル:行 | メンバ | 分類 | 移行先根拠 |
|---|---|---|---|
| `ActionManager.cs:72` | `MAX_DS4_CONTROLLER_COUNT` | 除外候補（**真const**: `ScpUtil.cs:639` `public const int`） | カテゴリB |
| `ActionManager.cs:594,601` | `getProfileActions`, `GetProfileAction` | (b) | `IProfileActionProvider.GetProfileActionNames/GetProfileAction`（`Services\ProfileActionProvider.cs:20,30`）に**完全一致の実装が既存** |
| `DefaultActionManager.cs:60` | `MAX_DS4_CONTROLLER_COUNT` | 除外（真const） | |
| `Log.cs:95` | `NotificationServiceInstance` | (b) | `AppHost.GetService<INotificationService>()` へ。`NotificationServiceInstance` は ScpUtil.cs:945-952 のテスト差替可能シム（Phase5-Step14通知統合で設置）。静的Logger経由の制約上(c)寄り |
| `MouseWheel.cs:55,82-88` | `ScrollSensitivity[dev]`, `outputKBMMapping.WHEEL_TICK_*`, `outputKBMHandler.PerformMouseWheelEvent` | **(c) ホットパス** | `IProfileSettingsService.ScrollSensitivity`（既存）+(a)。ただしタッチスクロール入力ループ内 = Phase6-Step4 圏。`IProfileSettingsService.OutputKBMMapping`（同インターフェース Step10-2-A-9）で mapping は置換可。handler は `IVirtualKBM`（登録済み `ServiceRegistration.cs` `AddSingleton<IVirtualKBM, OutputKBMHandlerAdapter>`） |
| `OutputSlotManager.cs:120,132` | `vigemBusVersionInfo` ×2 | (c) | ViGEm バックエンド圏 = **カテゴリE（DI化対象外）に近い**。バージョン情報のみなら `IEnvironmentService` 拡張(b)も可。除外を提案するが「勝手に除外はしない」ため(c)記録 |
| `OutputSlotPersist.cs:40,64,78` | `appdatapath` ×2, `exeversion` | (a) | `IPathService.AppDataPath` / `IEnvironmentService.ApplicationVersion` で置換可 |
| `PresetOption.cs:76-178` | `LoadBlankDevProfile`/`LoadBlankDS4Profile`/`LoadDefault*Profile` ×10 | (b) | 全て `IProfileRepository`（新設メンバ必要）または新規 `IPresetProfileLoader`。Plan §0.2 の「PresetOption 10件」に一致。`ProfileSettingsService.TEST_PROFILE_ITEM_COUNT` と同名constは除外対象外（本件はメソッド呼び出し） |
| `ProfileMigration.cs:86,287,424,505` | `CONFIG_VERSION`(真const `ScpUtil.cs:1097` → 除外), `exeversion` ×3 | (a) | `IEnvironmentService.ApplicationVersion` |
| `ProfileRepository.cs:58`（DS4Control直下・DTO用同名クラス `DS4Control\ProfileRepository.cs`） | `ProfileSettingsServiceInstance` | (b) | フォールバック解決。**注意**: `DS4Control\ProfileRepository.cs`（DTO用）と `DS4Control\Services\ProfileRepository.cs` は**同名異型が2つ存在**（原則1ファイル1型違反、構造的注意点） |
| `SyntheticDispatcher.cs:17-18,28,78` | `outputKBMMapping`, `outputKBMHandler` ×3 | (b)/(c) | `IVirtualKBM`/`IProfileSettingsService.OutputKBMMapping` 置換可。ディスパッチ経路のため実機回帰要 |
| `Util.cs:234,310,313` | `IsAdministrator()`, `exedirpath` ×2 | (a) | `IEnvironmentService` / `IPathService`（`ExecutableDirectory`） |
| `WindowPlacementHelper.cs:148-240` | `FormLocationX/Y`, `FormWidth/Height` ×8 | (a) | `IAppSettingsService.FormWidth` 等すべて既存（`DI\IAppSettingsService.cs:46-49`） |
| `Xbox360OutDevice.cs:112`, `DS4OutDevices\DS4OutDeviceBasic.cs:88`, `DS4OutDeviceExt.cs:108` | `GetSASteeringWheelEmulationAxis(device)` | (b) | `IProfileSettingsService.GetSASteeringWheelEmulationAxis`（既存）。ただし出力層 = 仮想コントローラー圏（カテゴリE隣接） |

### 1-3. `ScpUtil.cs`（Global実体ファイル自身 — シムと内部実装の区別）

- **[Legacy] ログ付きシム**（**意図的シム・除外**: §2.1 フォールバック維持原則）: 738-1029（9サービスの `*Instance` フォールバック）、966-1029（一時プロファイルsetter シム）、3008-3055（OutCurveMode シム）。
- **内部実装からの参照**（シム内部の実体、除外=Strangler Fig の本体部）: 1389-1392, 1401-1402, 1703-1704, 1883-1901, 3145-3166（ApplyProfileToSlot — **フェーズG 未実施の TODO コメント**:3189-3193付近「本メソッド自体を IProfileApplicationService.ApplyProfile への委譲に書き換え予定」）, 3603-3617, 4237-4238, 5068-5077, 6196-6233, 7144, 8307-8308, 8649-9294（BackingStore.Load/Save/PostProcessLoad 内部）, 10180, 10423-11132（BackingStore.Load*Profile 群内部）。
- **カテゴリB（真const・readonly不変・純粋関数）**: `TEST_PROFILE_ITEM_COUNT`(:640), `MAX_DS4_CONTROLLER_COUNT`(:639), `RESOURCES_PREFIX`(:1100), `XML_EXTENSION`(:1105), `CONFIG_VERSION`/`APP_CONFIG_VERSION`(:1097-1098), `MIN_TOUCHPAD_PASSTHRU_VIGEMBUS_VERSION`(:1070), `CUSTOM_EXE_CONFIG_FILENAME`(:1104)。
- **除外しない（真constでない）重要メンバ**: `configFileDecimalCulture`(:645, static field), `exeversion`(:672), `exelocation`/`exedirpath`(:653,669), `vigemBusVersionInfo`(:1076, static field), `defaultButtonMapping`(:1107, static array — 可変), `outputKBMMapping`(:1091), `Clamp`(:3891, 純粋関数=カテゴリBだが参照元は記録), `CompileVersionNumberFromString`(:2027), `PrepareAbsMonitorBounds`(:3985), `CheckIfVirtualDevice`/`GetInstanceIdFromDevicePath`(:1771/1822), `DeviceOptions`(:3111→`m_Config.deviceOptions` 委譲), `firstRun`/`multisavespots`/`useDInputOnly`/`activeOutDevType`/`loggedInvalidActions`(BackingStore 委譲プロパティ群)。

### 1-4. DTO 層（`DS4Control/DTOXml/`）

| ファイル:行 | メンバ | 分類 |
|---|---|---|
| `ActionsDTO.cs:187-307` | `configFileDecimalCulture` ×6 | (b): `IProfileSettingsService.ConfigDecimalCulture`（既存、`ProfileSettingsService.cs:41` 実装） |
| `AppSettingsDTO.cs:155-1108` | `MAX_DS4_CONTROLLER_COUNT` ×16（**真const → 除外**）, `exeversion`(:893), `APP_CONFIG_VERSION`(:894, 真const→除外) | (a) `ApplicationVersion` |
| `AutoProfilesDTO.cs:136-166` | `MAX_DS4_CONTROLLER_COUNT` ×4 | 除外（真const） |
| `OutputSlotPersistDTO.cs:34` | `exeversion` | (a) |
| `ProfileDTO.cs:64,75,3500-3829` | `exeversion`(:64), `CONFIG_VERSION`(:75 真const→除外), `getDS4ControlsByName`/`getX360ControlsByName`/`getX360ControlString` ×11 | (b): 辞書引き系は `IProfileSettingsService` 拡張か純粋関数移設（カテゴリB: 不変辞書 `ds4inputNames` 等）。**DTO層はカテゴリD（POCO）に近いが、これらは振る舞い呼び出しなため除外しない** |
| `OutputKBM\VirtualKBMFactory.cs:34` | `fakerInputInstalled` | (b): `IEnvironmentService` へ `FakerInputInstalled` 追加 |

### 1-5. `DS4Library/`（入力監視層・第1層）

| ファイル:行 | メンバ | 分類 |
|---|---|---|
| `DS4Device.cs:1545,1568-1570` | `DebouncingMs[...]`, `DebouncingMsChanged` | (b): `IProfileSettingsService.DebouncingMs` / `DebouncingMsChanged` イベント（**契約済み**: `IProfileSettingsService.cs:470-472`）。ただし第1層→横断サービス参照でホットパス隣接、イベント購読解除のライフサイクル管理が(c)要素 |
| `DS4Devices.cs:208-235,315,324` | `UseMoonlight`, `UseAdvancedMoonlight`, `CheckIfVirtualDevice`, `GetInstanceIdFromDevicePath` ×4 | (c): `IDs4DeviceRegistry` 契約拡張（Phase5-Step11 圏の残留）。デバイス列挙スレッド内のため実機検証必須 |
| `DS4Sixaxis.cs:477` | `UseDs3PitchRollSim` | (a): `IProfileSettingsService.UseDs3PitchRollSim`（既存） |
| `DS4State.cs:286-299` | `Clamp` ×4 | 除外（カテゴリB 純粋関数）だが、回転ホットパス内のため移設（`ColorUtil` 等）推奨 |

### 1-6. `Actions/`、`DI/`（インターフェース層）

- **`Actions/**` : 実参照 0件**（`\bGlobal\b` grep でヒットなし＝完全DI化済み。Plan §2.5 の「MAX_DS4_CONTROLLER_COUNT 1件残存」記述は**現状と不一致** — 解消済み）。
- `DI/**` : 実参照 0件（4件はすべてXMLドキュメントコメント内）。

### 1-7. ルート直下

| ファイル:行 | メンバ | 分類 |
|---|---|---|
| `App.xaml.cs:110-1025` | 約55件（`UseLang`, `SetCulture`, `FindConfigLocation`, `appdatapath`, `Load/Save`, `exeversion`, `LoadActions`, `CreateStdActions`, `UseCurrentTheme`, `IsAdministrator`, `SaveAsProfile`, `ProfilePath/OlderProfilePath`, `firstRun`, `multisavespots` ほか） | **Phase6-Step7（22件計上→実測約55件）**。Pre-Host 部分はカテゴリA除外。残り(a)/(b)中心 |
| `AutoProfileHolder.cs:54,134` | `appdatapath`, `MAX_DS4_CONTROLLER_COUNT`(const→除外) | (a) |
| `LoggerHolder.cs:62,132,144,195` | `appdatapath`, `LogMaxArchiveFiles`, `LogMinLevel` ×3 | (b) ログブートストラップ（カテゴリA隣接。DI構築前は参照必須） |
| `ProfileEntity.cs:50-78` | `appdatapath`, `SaveProfile`, `CacheExtraProfileInfo` | (a) `IProfileRepository` |
| `ProfileList.cs:50` | `appdatapath` | (a) |
| `StartupMethods.cs:31-202` | `exedirpath`, `exelocation`, `exeFileName` ×8 | (a) `IPathService`（ショートカット生成のUI補助） |
| `BezierCurve.cs:185` | `Clamp`（309はコメント） | 除外（カテゴリB） |
| `HidLibrary\HidDevices.cs:74,87` | `DeviceOptions.VerboseLogMessages` ×2 | (b)/(c): `DeviceOptions` は `m_Config.deviceOptions` 委譲。HidLibrary = カテゴリF 除外候補だが本件はこのリポジトリの修正版のため**除外せず記録** |

### 1-8. `OutputKBM/`、その他
- `OutputKBM\` 他4ファイル、`DS4OutDevices\DS4OutDeviceFactory.cs/Extras.cs`、`OutSlotDevice.cs`、`ControllerSlotManager.cs`、`Toggle*`、`Key*`、`Macro*`、`RepeatHelper.cs`、`DeviceRuntimeState.cs`、`DeltaAccelSettings.cs`、`Debouncer.cs`、`OneEuroFilter.cs`、`VJoyFeeder/`、`Updater*.cs`、`LogRotator.cs`、`LogWriter.cs`、`Notifications/`、`AutoProfileChecker.cs`、`KeyboardSettings.cs`、`StatusLogMsg.cs`、`ProfileSubSettingBase.cs`、`ProfilePropGroups.cs`、`SensitivitySettings.cs`、`StickOutCurve.cs`、`Trigger.cs`、`InputMethods.cs`、`DS4Touchpad.cs`、`DS4StateExposed.cs`、InputDevices/*: **`\bGlobal\b` 参照 0件（grep 実測）**。

## 2. サマリ集計（証明可能範囲）

| 領域 | 実参照件数（実測） |
|---|---|
| `ScpUtil.cs` 自体（シム+内部実装） | 138件（`Global\.` 非正規表現除去後）※コメント含む |
| `DS4Control/Services/` | 192件（うちコメント・シム定義含む。純粋な呼び出しは約120件） |
| App.xaml.cs | 約55件 |
| ScpUtil.cs 以外の DS4Control 直下 | 約48件 |
| DS4Library | 13件 |
| ルート .cs（App以外） | 約25件 |
| DTOXml | 約40件（うち MAX_DS4_CONTROLLER_COUNT 20件は真const除外） |
| **Actions/** | **0件** |
| **DI/**（インターフェース） | **0件** |
| 除外ファイル（Forms/ControlService/Mapping等） | 本監査スコープ外 |

**Phase6-Plan §0.2 の「193件」に対する乖離**: 除外ファイルを除いた新規実測は UI 系（App.xaml.cs 等）で計上比 **約2.5倍（22→55件）**。Step7以降の見積り見直しが必要。

## 3. DI契約・実装の整合性チェック（分類根拠）

| Globalメンバ群 | 既存DI契約 | 契約実装との一致 |
|---|---|---|
| FormWidth/Height/LocationX/Y, 各ColWidth, LogMinLevel, CheckUpdate*系, FirstRun, RunHotPlug, Notifications, SwipeProfiles, ProfileChangedNotification | `IAppSettingsService.cs:33-66` | ✅ `AppSettingsService.cs` が同一実体（Global委譲シム）として実装済み |
| ScrollSensitivity, DebouncingMs, OutputKBMMapping, UseDs3PitchRollSim, GetSASteeringWheelEmulationAxis, GetDefaultButtonMapping | `IProfileSettingsService.cs`（Step10-2-A 各節） | ✅ 実装済み（`ProfileSettingsService.cs`） |
| ProfilePath/OlderProfilePath/SelectedProfile/LinkedProfileUI, LoadTempProfile, CacheExtraProfileInfo | `IProfileRepository.cs:35-41` | ✅ ただし**実装が Global 配列をそのまま公開（同一実体参照）**でありSSOTは未移動 |
| GetAction/GetActionIndex | `ISpecialActionRepository.cs:18-19` | ✅ 実装済み。だが `ActionManager.cs:594,601`・`BackingStore.RebuildProfileActionDicts`（ScpUtil.cs:4237-4238）は未接続 |
| ApplyProfile | `IProfileApplicationService` | ⚠️ **循環**: `ProfileApplicationService.cs:53,116` が `Global.ApplyProfile` を逆呼び（フェーズG未完了） |
| exelocation/exedirpath/appdatapath | `IPathService` | ✅（On-Demand 評価 §5.4 準拠） |
| exeversion/IsAdministrator/RefreshHidHideInfo | `IEnvironmentService` | ✅ |

## 4. Phase5-Step14/15 および Phase6-Status の記述と証跡のズレ（重要）

1. **Phase6-Status.md は「Step1 未着手・Phase5-Step14 未実施」（§0, 最終更新 2026-09-11）と記載**。しかし `Phase6-Step1-Global-Usage-Classification-Report.md`（同フォルダ、2026-09-18作成）は**完了判定基準を全[x]とマーク済み** → **Status と成果物が矛盾**。本監査でも既存レポートに重大な不正確さ（§5参照）を確認したため、Step1 は**実質やり直し／本レポートで補完**と扱うべき。
2. **Phase5-Step14**: 自動テストは複数回成功（169件PASS: ProfileSync Status §3.1, D-3 完了 2026-09-15）だが、**実機CP4（`Phase5-Step14-RealDevice-Verification-Checklist.md` 実施記録 §14「未実施」）は未完了**。さらに Step14 圏から **ProfileSync-And-ApplyUnified**（フェーズA完了〜フェーズE/H未着手、F-7/F-8, C-3, (c)-3 が gwin7ok氏実施待ち）が進行中 → **Phase6 着手前提「Step14完了」は満たされていない**。
3. **Step15（Legacy shim削除判断）**: 計画書・Status とも**存在・着手の証跡なし**（`*Step15*` ファイル0件、Phase5-Status で「未着手」）。→ Phase6-Step12 の前提も未充足。
4. **フェーズG（`Global.ApplyProfileToSlot` → `IProfileApplicationService` 委譲）はユーザー承認済み・未実装**（ProfileSync Status §2.7 未着手）。これは Phase6-Step2/Step3 の ControlService・Mapping 作業と**直接競合**するため、着手順の確定が必須。

## 5. 既存 Phase6-Step1 レポートへの是正要求

既存 `Phase6-Step1-Global-Usage-Classification-Report.md` は: ① `ControlService.cs` を「調査対象外」にしながらホットパス判定を[x]完了扱い、② `Mouse.cs 27件/MouseCursor.cs 14件` を引用するも実測記載なし、③ 分類D（MainWindow.xaml.cs）を本計画書に存在しない名称で言及、④ 「193件＋28件の仕分け完了」を[x]とするが**一覧表（計画書 §3.1 の全件一覧表・除外一覧表）が存在しない**。**完了基準（§3）の「全件一覧表」観点では未充足** — 本レポートがその補完に相当する。

## 6. Step2以降への申し送り（優先度順）

1. **最優先の設計競合**: `ProfileApplicationService` ↔ `Global.ApplyProfile` の循環（§3表 ApplyProfile 行）。フェーズG完了→Phase6-Step2着手の順を推奨。
2. `ProfileActions` 二重書込（`ProfileSettingsService.cs:554-558`）と `ProfilePath` 直書き（`Services\ProfileRepository.cs:77`）： 同一配列参照前提の暗黙結合。Phase6-Step5（SSOT一元化）の根拠証跡。
3. 同名異型クラス: `DS4Control\ProfileRepository.cs`（DTO用 IProfileRepository）と `Services\ProfileRepository.cs`（DI IProfileRepository）— 原則1ファイル1型（copilot-instructions §3.3 原則1）への技術的負債コメント要否確認。
4. 真const以外で「除外しない」対象: `configFileDecimalCulture`、`defaultButtonMapping`/`reverseX360ButtonMapping`（可変static配列）、`vigemBusVersionInfo`、`exeversion`/`exelocation` 群、`DeviceOptions` — すべて(b)以上の分類で記録済み。

（ファイル変更なし・読み取り専用で実施。全行番号は本日時点の実測。）
