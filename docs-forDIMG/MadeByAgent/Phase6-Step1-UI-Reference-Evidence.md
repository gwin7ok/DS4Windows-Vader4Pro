> **2026-09-18 再監査証拠資料。HEAD 59d46db47cc9d20f212b0f6fa881515b767c91ed。取得済み参照の分類記録。全件網羅・件数確定の証明ではない。aは契約候補ありを意味し無条件の置換承認ではない。**

## 監査結果

**コード変更・ファイル書き込み・ビルド・テストは行っていません。** 今回は不具合修正ではなく参照分類の依頼なので、最後のセルフチェックにある「修正してテストする」は適用していません。

対象は、現在のワークスペースにある次の範囲です。

- `DS4Windows/DS4Forms/**/*.cs`：ファイル検索で列挙した **69ファイル**
- `DS4Windows/App.xaml.cs`
- 照合先：`Global`／`BackingStore` の該当実装、`DS4Windows/DI` の契約、対応サービス実装・登録

既存の分類レポートは判定資料として使用していません。計画書中の過去の件数も流用していません。

> 以下は指定されたUI・App範囲の監査です。リポジトリ全体の呼出元、反射による利用、全ビルド構成を含む完全性は主張しません。コンパイラによる参照解析ではなく、ファイル列挙・全文検索・該当実コードの照合です。

### 分類の読み方

| 表記 | 意味 |
|---|---|
| **a** | 使用目的に対応する既存契約・実装がある |
| **b：B番号** | 既存サービスの軽微な拡張・意味差の是正が必要。提案APIは後掲 |
| **c：C番号** | 新しい責務、または責務境界・実行手順の設計が必要 |
| **除外const** | 実宣言が `const` |
| **除外純粋** | 実装が入力だけで戻り値を決める |
| **除外pre-host** | 確認した経路がHost構築前 |
| **F** | フォールバック分岐。**参照から除外せず**分類に残した |

行番号は参照がある物理行です。同一メンバが同じ行に複数回ある場合は行番号を一度だけ記載しています。

---

## 1. 同名APIでも「a」にできなかった重要事項

| 項目 | 実装照合結果 |
|---|---|
| `ApplyProfileToSlot` | `Global` は `CURRENT_DS4_CONTROLLER_LIMIT` を使用する一方、`ProfileApplicationService.ApplyProfile` は **4未満に固定**。さらに内部の `Global.ApplyProfile` の戻り値を捨てて `success = true` にする。既存サービスへの無条件置換は不可 |
| `GetAction` | `BackingStore.GetAction` は名前を正規化し、未発見時に `"null"` の `SpecialAction` を返す。`SpecialActionRepository.GetAction` は正規化せず、未発見時は `null`。同等ではない |
| `RemoveAction` | `Global` はランタイムエントリ解放、削除・保存、再読込・件数検証を実行。既存DI実装は主にメモリリスト削除と `ActionsChanged`。永続化・ランタイム整合が不足 |
| `SaveAction` | `AddAction`／`ReplaceAction`／`SaveActions` への単純な分解では、再読込検証、キー状態リセットなどを保持できない |
| `LoadActions` | `Global` 側はファイル不存在時にも `BackingStore.LoadActions` に進み、既定アクションを作成する。DI実装は **`File.Exists` が偽なら即 `false`** |
| `LoadBlankDevProfile` | `LoadDefaultProfile` は `ResetToDefaults` を呼ぶだけ。Blank読込、標準SpecialAction設定、キャッシュ更新等の代替ではない |
| `SaveAsProfile` | 実体は **`ResetProfile` 後に保存**。通常の `SaveProfile` への置換は不可 |
| `appdatapath` | `PathService.AppDataPath` は未設定時に `AppContext.BaseDirectory` を返す。またsetterは `_customAppDataPath` を変えるだけで、`Global.appdatapath` を更新しない |
| `exedirpath` | `Global` はジャンクション解決後の `exelocation` の親。`PathService.ExecutableDirectory` は `AppContext.BaseDirectory`。表では既存 `ExecutablePath` から親を求める経路を採用 |
| `configFileDecimalCulture` | 可変staticフィールド。既存 `ConfigDecimalCulture` は別の `new CultureInfo("en-US")`。同じ初期値でも同一状態ではない |
| `getX360ControlString` | **1引数版は純粋なswitch**。**2引数版は可変static辞書を読む**。同じ除外区分にはできない |
| `ProfileSettingsServiceInstance.OutContType` | `IProfileSettingsService.OutContType` は共有BackingStoreを参照する。一方、`IOutputSlotService.Get/SetOutputDeviceType` の孤立配列は代替に使えない |

主な根拠：

- [`ProfileApplicationService.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Control/Services/ProfileApplicationService.cs)
- [`SpecialActionRepository.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Control/Services/SpecialActionRepository.cs)
- [`ProfileRepository.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Control/Services/ProfileRepository.cs)
- [`PathService.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Control/Services/PathService.cs)
- [`ScpUtil.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Control/ScpUtil.cs)：1275、1396、1915、3149、3380、3426、3630、3768、5085、5335、6120付近

---

## 2. `DS4Forms` 直下の全参照

表中の既存サービス略号：

- `App`＝`IAppSettingsService`
- `PS`＝`IProfileSettingsService`
- `PR`＝`IProfileRepository`
- `SA`＝`ISpecialActionRepository`
- `Path`＝`IPathService`
- `Env`＝`IEnvironmentService`
- `Appearance`＝`IAppearanceSettingsService`
- `Output`＝`IOutputSlotService`

### [`About.xaml.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/About.xaml.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `exeversion` | 33 | a：`Env.ApplicationVersion` |

### [`AutoProfiles.xaml.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/AutoProfiles.xaml.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `appdatapath` | 46, 68, 349, 368, 400 | b：B1。現行値そのものと、未設定時の意味を保持 |
| `CreateAutoProfiles` | 69 | b：B7 |
| `UseCustomSteamFolder` | 73 | b：B3 |
| `CustomSteamFolder` | 74, 75 | b：B3 |

`m_Profile` はフィールド初期化でパスを固定しています。移行時はサービス取得時に一度だけ固定せず、操作時評価を維持・整理する必要があります。

### [`BindingWindow.xaml.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/BindingWindow.xaml.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `ds4inputNames` | 73 | c：C1 |
| `xboxDefaultNames` | 166 | c：C1 |
| `ds4DefaultNames` | 171 | c：C1 |
| `defaultButtonMapping` | 200, 244, 927 | a：`PS.GetDefaultButtonMapping()` の要素参照 |
| `InverseRumbleMotors` | 884 | a：`PS.InverseRumbleMotors` |
| `ASSEMBLY_RESOURCE_PREFIX` | 798 | 除外const |
| `RESOURCES_PREFIX` | 999, 1000, 1001 | 除外const |

`GetDefaultButtonMapping()` は配列のcloneを返しますが、ここでは要素を読むだけなので対応可能です。共有配列を書き換えるAPIとしては同等ではありません。

### [`DupBox.xaml.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/DupBox.xaml.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `appdatapath` | 59, 60 | b：B1 |

### [`LanguagePackControl.xaml.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/LanguagePackControl.xaml.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `Save` | 77 | a：`App.Save()` |

### [`LanguageSelectDialog.xaml.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/LanguageSelectDialog.xaml.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `UseLang` | 139, 141 | b：B3 |
| `SetCulture` | 140 | b：B4 |

**初回起動画面であることだけではpre-host除外にできません。** 確認した `App` の生成箇所は `CreateHost` 後です。

### [`MainWindow.xaml.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/MainWindow.xaml.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `ProfileSettingsServiceInstance` | 110 | a・F：`PS` 注入 |
| `PathServiceInstance` | 112 | a・F：`Path` 注入 |
| `EnvironmentServiceInstance` | 113 | a・F：`Env` 注入 |
| `OutputSlotServiceInstance` | 116 | a・F：`Output` 注入 |
| `ProfileRepositoryInstance` | 117 | a・F：`PR` 注入 |
| `NotificationServiceInstance` | 118 | a・F：`INotificationService` 注入 |
| `ApplyProfileToSlot` | 1082, 2006 | b：B9 |
| `ProfilePath` | 1962, 1974, 1990 | a：`PR.ProfilePath` |
| `OlderProfilePath` | 1975 | a：`PR.OlderProfilePath` |
| `TEST_PROFILE_INDEX` | 1845, 1889, 2079 | 除外const |
| `RESOURCES_PREFIX` | 2182, 2185, 2188, 2191, 2194, 2197 | 除外const |

### [`ProfileEditor.xaml.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ProfileEditor.xaml.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `UseLang` | 284 | b：B3 |
| `ProfileEditorLeftWidth` | 340, 379, 387, 403 | a：`App.ProfileEditorLeftWidth` |
| `ProfileEditorRightWidth` | 341, 380, 387, 404 | a：`App.ProfileEditorRightWidth` |
| `SpecialActionNameColWidth` | 342, 382, 387, 417 | a：`App.SpecialActionNameColWidth` |
| `SpecialActionTriggerColWidth` | 343, 383, 387, 418 | a：`App.SpecialActionTriggerColWidth` |
| `SpecialActionDetailColWidth` | 344, 384, 387, 419 | a：`App.SpecialActionDetailColWidth` |
| `FormWidth` | 348 | a：`App.FormWidth` |
| `FormHeight` | 349 | a：`App.FormHeight` |
| `ProfilePath` | 1113, 1280, 1284 | a：`PR.ProfilePath` |
| `LoadBlankDevProfile` | 1128 | b：B10 |
| `ProfileActions` | 1133, 1134, 1395, 1872, 1940 | a：`PR.ProfileActions` または `PS.ProfileActions`。要素／リストの更新であり、共有実体を維持 |
| `LSModInfo` | 1155, 1170 | a：`PS.LSModInfo` |
| `RSModInfo` | 1156, 1182 | a：`PS.RSModInfo` |
| `outDevTypeTemp` | 1271 | a：`Output.OutDevTypeTemp` |
| `CacheProfileCustomsFlags` | 1299, 2080, 2144, 2271, 2301, 2340, 2381, 2414 | a：`PR.CacheProfileCustomsFlags(...)` |
| `SaveProfile` | 1407 | a・F：`PR.SaveProfile(...)` |
| `ApplyProfileToSlot` | 1419 | b：B9 |
| `InverseRumbleMotors` | 1528, 1572, 1579, 1591, 1600 | a：`PS.InverseRumbleMotors` |
| `OutContType` | 1797 | a：`PS.OutContType` |
| `ProfileSettingsServiceInstance` | 1798, 1800 | a：注入済み `PS`。これはフォールバックでなく、直接取得・null判定・追加書込み |
| `GetAction` | 1829, 1895 | b：B8 |
| `CacheExtraProfileInfo` | 1884, 2054 | a：`PR.CacheExtraProfileInfo(...)` |
| `appdatapath` | 2453 | b：B1。`Actions.xml` エクスポート元 |
| `TEST_PROFILE_INDEX` | 1111, 1113, 1141, 1150 | 除外const |
| `ASSEMBLY_RESOURCE_PREFIX` | 940, 944, 948, 952, 956, 960, 964, 968, 972, 976, 980, 984, 988, 992, 996, 1001, 1005, 1009, 1013, 1017, 1022, 1026, 1030, 1034, 1038, 1043, 1047, 1051, 1055 | 除外const |
| `RESOURCES_PREFIX` | 2484, 2485, 2486, 2487, 2488 | 除外const |

387行のログは、文字列内だから除外せず、**5個の補間式それぞれを設定読取りとして計上**しています。

### [`RecordBox.xaml.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/RecordBox.xaml.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `appdatapath` | 332, 509 | b：B1 |
| `RESOURCES_PREFIX` | 643 | 除外const |

### [`SaveWhere.xaml.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/SaveWhere.xaml.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `AdminNeeded` | 43 | b：B5 |
| `SaveWhere` | 51, 92 | c：C3 |
| `exedirpath` | 51, 65, 78, 79, 80 | a：`Directory.GetParent(Path.ExecutablePath).FullName` |
| `appDataPpath` | 56, 58, 89, 92 | b：B2 |
| `SaveDefault` | 65, 89 | b：B11 |

ここも確認した通常起動経路では **Host構築後**。画面全体をpre-host除外にはしません。

### [`SpecialActionEditor.xaml.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/SpecialActionEditor.xaml.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `RemoveAction` | 306 | c：C2 |

### [`UpdaterWindow.xaml.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/UpdaterWindow.xaml.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `IsAdministrator` | 94, 237 | a：`Env.IsAdministrator()` |

これはDS4Windows本体内の画面です。「Updaterという名前」を理由に別プロセス除外にはしていません。

### [`WelcomeDialog.xaml.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/WelcomeDialog.xaml.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `FindConfigLocation` | 66 | 除外pre-host：確認した `App.CheckOptions` → `WelcomeDialog(true)` 経路 |
| `Load` | 67 | 除外pre-host：同上 |
| `IsWin10OrGreater` | 77, 122 | b：B5。driverinstallのpre-host経路ではbootstrap利用を維持 |
| `IsWin8OrGreater` | 89, 130 | b：B5。同上 |
| `RefreshViGEmBusInfo` | 236 | b：B5。同上 |
| `IsViGEmBusInstalled` | 237 | b：B5。同上 |
| `IsHidHideInstalled` | 352 | b：B5。同上 |
| `IsFakerInputInstalled` | 458 | b：B5。同上 |
| `RESOURCES_PREFIX` | 514 | 除外const |

OS・ドライバ照会は純粋関数ではありません。またViGEm出力バックエンドの実装ではなく、環境照会です。共有可能な環境APIはbに残し、pre-host実行のためだけにDI取得を強制しない整理です。

---

## 3. `ViewModels` 直下の全参照

### [`AboutViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/AboutViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `exeversion` | 15 | a：`Env.ApplicationVersion` |

### [`AutoProfilesViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/AutoProfilesViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `AutoProfileRevertDefaultProfile` | 79, 83 | a・F：`App.AutoProfileRevertDefaultProfile` |
| `autoProfileSwitchNotifyChoice` | 105, 109 | a・F：`IAutoProfileService.AutoProfileSwitchNotifyChoice` |

### [`BindingWindowViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/BindingWindowViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `outDevTypeTemp` | 57 | a：`Output.OutDevTypeTemp` |
| `RefreshActionAlias` | 691, 755 | b：B6 |

`RefreshActionAlias` は設定オブジェクトを変更し、現在のKBMマッピングを読みます。純粋関数除外にはできません。

### [`ControllerListViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/ControllerListViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `ProfileSettingsServiceInstance` | 73, 611 | a・F：`PS` 注入 |
| `ProfileRepositoryInstance` | 74, 612 | a・F：`PR` 注入 |
| `RESOURCES_PREFIX` | 397, 407, 412, 417 | 除外const |

### [`ControllerRegDeviceOptsViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/ControllerRegDeviceOptsViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `UseMoonlight` | 63, 66, 67 | b：B3 |
| `UseAdvancedMoonlight` | 73, 76, 77 | b：B3 |
| `SaveControllerConfigs` | 232 | c：C4 |

### [`LanguagePackViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/LanguagePackViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `UseLang` | 60, 88, 90 | b：B3 |
| `PROBING_PATH` | 103 | 除外const |
| `LANGUAGE_ASSEMBLY_NAME` | 120 | 除外const |

60行はログ本文の文字列 `"Global.UseLang"` とは別に、`{Global.UseLang}` が実行されます。

### [`LogViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/LogViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `exeversion` | 23 | a：`Env.ApplicationVersion` |

### [`MainWindowsViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/MainWindowsViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `exedirpath` | 71, 146 | a：`Directory.GetParent(Path.ExecutablePath).FullName` |
| `appdatapath` | 93 | b：B1 |
| `IsViGEmBusInstalled` | 123 | b：B5 |
| `IsRunningSupportedViGEmBus` | 124 | b：B5 |
| `exelocation` | 127 | a：`Path.ExecutablePath` |
| `IsAdministrator` | 147 | a：`Env.IsAdministrator()` |
| `exeFileName` | 157 | a：`System.IO.Path.GetFileName(Path.ExecutablePath)` |
| `AdminNeeded` | 160 | b：B5 |

### [`MappingListViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/MappingListViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `GetDS4CSetting` | 236 | a：`PS.GetDS4CSetting(...)` |
| `getX360ControlString`〔2引数〕 | 292, 297, 305 | c：C1 |
| `defaultButtonMapping` | 297, 302 | a：`PS.GetDefaultButtonMapping()` |

### [`ProfileSettingsViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `IsUsingMinViGEm117333` | 2976 | b：B5 |
| `ProfileSettingsServiceInstance` | 2992, 3780 | a・F：`PS` 注入 |
| `OutputSlotServiceInstance` | 2994 | a・F：`Output` 注入 |
| `ProfileRepositoryInstance` | 2995 | a・F：`PR` 注入 |
| `exedirpath` | 3698, 3701, 3710 | a：`Directory.GetParent(Path.ExecutablePath).FullName` |
| `defaultButtonMapping` | 4192 | a：`PS.GetDefaultButtonMapping()` |
| `RefreshActionAlias` | 4196 | b：B6 |
| `ASSEMBLY_RESOURCE_PREFIX` | 3008, 3011 | 除外const |

### [`RecordBoxViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/RecordBoxViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `ProfileSettingsServiceInstance` | 118 | a・F：`PS` 注入 |
| `macroDS4Values` | 405 | c：C1 |
| `RESOURCES_PREFIX` | 448, 449, 450, 457, 458, 459 | 除外const |

### [`RenameProfileViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/RenameProfileViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `appdatapath` | 45 | b：B1 |

### [`SettingsViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/SettingsViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `UseExclusiveMode` | 46, 47 | a：`App.UseExclusiveMode` |
| `SwipeProfiles` | 52, 53 | a：`App.SwipeProfiles` |
| `ProfileChangedNotification` | 117, 118 | a：`App.ProfileChangedNotification` |
| `DCBTatStop` | 127 | b：B3 |
| `FlashWhenLate` | 128 | b：B3 |
| `FlashWhenLateAt` | 129 | b：B3 |
| `QuickCharge` | 133 | b：B3 |
| `UseIconChoice` | 137, 140, 142 | a：`Appearance.UseIconChoice` |
| `UseCurrentTheme` | 150, 153, 155 | a：`Appearance.UseCurrentTheme` |
| `CheckUpdateStartupEnabled` | 165, 168, 190, 638 | a：`App.CheckUpdateStartupEnabled` |
| `CheckEveryValue` | 182, 195, 197 | a：`App.CheckEveryValue` |
| `CheckEveryUnit` | 216, 419, 641 | a：`App.CheckEveryUnit` |
| `isUsingOSCServer` | 226, 229 | b：B3 `UseOscServer` getter |
| `setUsingOSCServer` | 230 | b：B3 `UseOscServer` setter |
| `getOSCServerPortNum` | 235 | b：B3 `OscServerPort` getter |
| `setOSCServerPort` | 235 | b：B3 `OscServerPort` setter |
| `isInterpretingOscMonitoring` | 237 | b：B3 `InterpretingOscMonitoring` getter |
| `setInterpretingOscMonitoring` | 237 | b：B3 `InterpretingOscMonitoring` setter |
| `isUsingOSCSender` | 241, 244 | b：B3 `UseOscSender` getter |
| `setUsingOSCSender` | 245 | b：B3 `UseOscSender` setter |
| `getOSCSenderPortNum` | 250 | b：B3 `OscSenderPort` getter |
| `setOSCSenderPort` | 250 | b：B3 `OscSenderPort` setter |
| `getOSCSenderAddress` | 254 | b：B3 `OscSenderAddress` getter |
| `setOSCSenderAddress` | 255 | b：B3 `OscSenderAddress` setter |
| `isUsingUDPServer` | 261, 264, 293 | a：`App.UseUdpServer` getter |
| `setUsingUDPServer` | 265 | a：`App.UseUdpServer` setter |
| `getUDPServerListenAddress` | 273 | a：`App.UdpServerListenAddress` getter |
| `setUDPServerListenAddress` | 274 | a：`App.UdpServerListenAddress` setter |
| `getUDPServerPortNum` | 276 | a：`App.UdpServerPort` getter |
| `setUDPServerPort` | 276 | a：`App.UdpServerPort` setter |
| `UseUDPSeverSmoothing` | 280, 283, 285, 293 | b：B3 |
| `UDPServerSmoothingMincutoff` | 299, 302, 304 | b：B3。既存変更イベントを保持 |
| `UDPServerSmoothingBeta` | 312, 315, 317 | b：B3。既存変更イベントを保持 |
| `UseCustomSteamFolder` | 325, 328 | b：B3 |
| `CustomSteamFolder` | 336, 339, 343 | b：B3 |
| `FakeExeName` | 362, 365, 367 | b：B3。既存ファイル名検証を保持 |
| `hidHideInstalled` | 382 | b：B5。再プローブでなくキャッシュ値の参照 |
| `AbsoluteDisplayEDID` | 397, 398 | b：B3 |
| `ProcessPriority` | 403, 406 | b：B3 |
| `IsAdministrator` | 454 | a：`Env.IsAdministrator()` |
| `exedirpath` | 542, 543, 544, 651, 655, 656 | a：`Directory.GetParent(Path.ExecutablePath).FullName` |
| `exelocation` | 652, 653, 658 | a：`Path.ExecutablePath` |
| `GrabCurrentMonitors` | 681 | b：B5 |
| `LogMaxArchiveFiles` | 698, 701, 703 | b：B3 |
| `LogMinLevel` | 715, 718, 720 | a：`App.LogMinLevel` |

OSC各メンバの実体は設定のget/setです。ここを `IUdpServerService` の起動・停止操作に置換してはいけません。UDP設定についても同様です。

### [`SpecialActEditorViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/SpecialActEditorViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `GetActions` | 153 | a：`SA.ActionList` |

### [`SpecialActionsListViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/SpecialActionsListViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `SpecialActionRepositoryInstance` | 89 | a・F：`SA` 注入 |
| `ProfileRepositoryInstance` | 90 | a・F：`PR` 注入 |
| `OutputSlotServiceInstance` | 92 | a・F：`Output` 注入 |
| `ProfileSettingsServiceInstance` | 93 | a・F：`PS` 注入 |
| `GetActions` | 224 | a・F：`SA.ActionList` |
| `RemoveAction` | 315 | c・F：C2 |

315行のフォールバックに対して、通常分岐の `specialActionRepo.RemoveAction(...)` は同等ではありません。**フォールバックを削除すれば解決する問題ではありません。**

### [`TouchButtonUserControlViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/TouchButtonUserControlViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `TouchpadButtonMode` | 49, 50 | a：`PS.TouchpadButtonMode` |

### [`TrayIconViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/TrayIconViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `exeversion` | 35 | a：`Env.ApplicationVersion`。static初期化からの依存受渡し整理は必要 |
| `iconChoiceResources` | 98 | a：`Appearance.GetIconResourcePath(...)` |
| `UseIconChoice` | 98 | a：`Appearance.UseIconChoice` |
| `BatteryChanged` | 99 | b：B12 |
| `ProfilePath` | 199 | a：`PR.ProfilePath` |
| `exedirpath` | 255 | a：`Directory.GetParent(Path.ExecutablePath).FullName` |
| `RESOURCES_PREFIX` | 447, 448, 449, 450, 451, 452, 453, 454, 455, 456, 457, 458 | 除外const |

`IDeviceStateService.DeviceStateChanged` は接続状態イベントであり、`BatteryChanged` の代替ではありません。

### [`UpdaterWindowViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/UpdaterWindowViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `LastVersionChecked` | 56, 62 | b：B3。文字列設定と数値版の再計算を保持 |

---

## 4. `ViewModels/SpecialActions` の全参照

### [`CheckBatteryViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/SpecialActions/CheckBatteryViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `configFileDecimalCulture` | 110 | b：B13 |
| `SaveAction` | 113 | c：C2 |

### [`LaunchProgramViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/SpecialActions/LaunchProgramViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `SaveAction` | 111 | c：C2 |
| `configFileDecimalCulture` | 111 | b：B13 |

### [`LoadProfileViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/SpecialActions/LoadProfileViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `SaveAction` | 89 | c：C2 |

### [`MacroViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/SpecialActions/MacroViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `SaveAction` | 107 | c：C2 |

### [`MultiActButtonViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/SpecialActions/MultiActButtonViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `SaveAction` | 151 | c：C2 |

### [`PressKeyViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/SpecialActions/PressKeyViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `OutContType` | 115, 117, 153, 155 | a：`PS.OutContType` |
| `getX360ControlString`〔2引数〕 | 117, 155, 221 | c：C1 |
| `getX360ControlString`〔1引数〕 | 121, 159, 225, 230 | 除外純粋：BackingStoreの入力値switch。静的ユーティリティへの移設候補 |
| `outDevTypeTemp` | 221 | a：`Output.OutDevTypeTemp` |
| `SaveAction` | 258, 265 | c：C2 |

### [`SpecialActionViewModel.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/SpecialActions/SpecialActionViewModel.cs)

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `SaveAction` | 25 | c：C2 |
| `configFileDecimalCulture` | 25 | b：B13 |

---

## 5. [`App.xaml.cs`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/App.xaml.cs) の全参照

`AppHost.CreateHost(...)` は **298行付近**、その後に `CreateControlService`、初回言語選択、保存場所選択、設定読込が続きます。したがって、`Application_Startup` 全体をpre-host除外にはできません。

| Globalメンバ | 全行番号 | 分類・移行先 |
|---|---|---|
| `UseLang` | 110 | b：B3。`ApplyLanguageSetting` はHost前後から呼ばれる共有処理 |
| `SetCulture` | 111 | b：B4。同上。pre-host呼出し用の直接経路は維持 |
| `FindConfigLocation` | 154 | 除外pre-host：起動ログ準備より前の保存場所判定 |
| `appdatapath` | 157 | 除外pre-host：初期ログローテーション |
| `LogMaxArchiveFiles` | 157 | 除外pre-host：同上 |
| `LogMinLevel` | 162 | 除外pre-host：bootstrapログレベル |
| `RefreshViGEmBusInfo` | 289 | 除外pre-host：Host構築前の環境情報初期化 |
| `firstRun` | 310 | a：`App.FirstRun` |
| `multisavespots` | 336 | c：C3 |
| `appdatapath` | 349, 418, 541, 542, 543 | b：B1 |
| `Load` | 356 | a：`App.Load()` |
| `Save` | 362, 430, 557, 999 | a：`App.Save()`。999はHost破棄前の保存順序を維持 |
| `exeversion` | 368 | a：`Env.ApplicationVersion` |
| `exeFileName` | 374 | a：`System.IO.Path.GetFileName(Path.ExecutablePath)` |
| `UseLang` | 426, 458, 459, 460 | b：B3 |
| `DeviceOptions` | 428 | c：C4 |
| `SaveAsProfile` | 438 | b：B10 |
| `ProfilePath` | 441 | a：`PR.ProfilePath` |
| `OlderProfilePath` | 441 | a：`PR.OlderProfilePath` |
| `ResetConnectionFlags` | 448 | b：B12 |
| `LoadActions` | 452 | b：B14 |
| `CreateStdActions` | 454 | c：C5 |
| `UseCurrentTheme` | 462, 463 | a：`Appearance.UseCurrentTheme` |
| `LoadLinkedProfiles` | 467 | b：B10 |
| `IsAdministrator` | 482 | a：`Env.IsAdministrator()` |
| `hidHideInstalled` | 485 | b：B5 |
| `appDataPpath` | 564, 566, 568, 569, 572 | b：B2 |
| `exedirpath` | 565, 567, 570 | a：`Directory.GetParent(Path.ExecutablePath).FullName` |
| `appdatapath`〔代入〕 | 585 | b：B1。既存 `Path.AppDataPath = null` では同等にならない |
| `RefreshViGEmBusInfo` | 604 | 除外pre-host：`CheckOptions` のdriverinstall分岐 |
| `FindConfigLocation` | 607 | 除外pre-host：同上 |
| `Load` | 608 | 除外pre-host：同上 |
| `UseLang` | 612, 613 | 除外pre-host：同上 |
| `UseCurrentTheme` | 614, 615 | 除外pre-host：同上 |
| `MAX_DS4_CONTROLLER_COUNT` | 1025 | 除外const |

**110・111行を除外しなかった理由：** 同じメソッドが通常起動後・設定画面からも使用されます。「pre-hostからも呼ばれる」ことは、そのメソッドの全参照を恒久除外する理由にはなりません。

---

## 6. 分類b：提案APIと保持条件

以下は**未実装の提案**です。既存契約があるものと混同しないよう、追加／是正を明記しています。

| ID | 既存サービスへの提案 | 保持する意味・理由 |
|---|---|---|
| **B1** | `IPathService` に `string ConfiguredAppDataPath { get; set; }` を追加 | getter/setterを現在の `Global.appdatapath` に連動。未設定値をBaseDirectoryへ置換しない。既存 `AppDataPath` のfallback／override仕様を黙って変えない |
| **B2** | `IPathService` に `string RoamingAppDataPath { get; }` を追加 | 現在選択中の保存場所と、`appDataPpath` の候補保存場所を区別 |
| **B3** | `IAppSettingsService` に下表の設定を追加 | 独立フィールドを作らず既存設定実体を共有。検証、Trim、数値再計算、既存イベントを保持 |
| **B4** | `IAppearanceSettingsService` に `void SetCulture(string cultureCode)` を追加 | 現行 `Global.SetCulture` のCurrentUICulture／DefaultThreadCurrentUICulture更新と例外処理を保持。WPF辞書・CurrentCulture更新まで勝手に統合しない |
| **B5** | `IEnvironmentService` に下表の照会・操作を追加 | 権限、OS、ドライバ、モニタ環境という既存責務内。キャッシュ値と再プローブを区別 |
| **B6** | `IProfileSettingsService` に `void RefreshActionAlias(DS4ControlSettings setting, bool shift)` を追加 | 現在の `OutputKBMMapping` を使用。通常／shift両方のゼロクリアとキー変換を保持 |
| **B7** | `IAutoProfileService` に `bool CreateAutoProfiles(string path)` を追加 | 現行の空 `Programs` XML生成を保持。既存Holderの保存を呼んで未意図の内容を出力しない |
| **B8** | `ISpecialActionRepository` に `SpecialAction GetActionOrPlaceholder(string name)` を追加 | 現行の名前正規化・大小文字無視・未発見時placeholderを保持。既存 `GetAction` のnull契約を黙って変更しない |
| **B9** | `IProfileApplicationService` に `bool ApplyProfileToSlot(int slotIndex, string profileName, ProfileChangeSource source)` を追加し、既存 `ApplyProfile` 実装を是正 | `CURRENT_DS4_CONTROLLER_LIMIT`、実際のbool結果、Halt、通知・ログ経路を保持。単に既存4固定メソッドを呼ぶラッパーでは不十分 |
| **B10** | `IProfileRepository` に下記3メソッドを追加 | Blank、reset後保存、リンク読込を既存通常読込／保存と区別 |
| **B11** | `IProfileXmlStore` に `bool CreateDefaultAppSettingsXml(string path)` を追加 | 現行 `SaveDefault` の最小XML生成・任意指定パスを保持。通常の全設定Saveと区別 |
| **B12** | `IDeviceStateService` に `event EventHandler<byte> BatteryChanged` と `void ResetConnectionFlags()` を追加 | 電池イベントは既存イベントへadd/remove委譲。接続フラグは `firstConnectionAfterStartup` を更新し、別配列を作らない |
| **B13** | 既存 `IProfileSettingsService.ConfigDecimalCulture` の実装を共有実体へ是正 | 現行static CultureInfoと別インスタンスである差をなくす。定数扱いしない |
| **B14** | `ISpecialActionRepository` に `bool LoadActionsOrCreateDefaults()` を追加 | ファイル不存在時もBackingStoreの既定生成・ランタイム初期化に進む。現在の `LoadActions()` の早期returnとの差を明示 |

### B3：追加する設定API

すべてget/setです。

| 型 | 提案メンバ |
|---|---|
| `string` | `UseLang`, `CustomSteamFolder`, `FakeExeName`, `AbsoluteDisplayEDID`, `LastVersionChecked`, `OscSenderAddress` |
| `bool` | `DCBTatStop`, `FlashWhenLate`, `QuickCharge`, `UseMoonlight`, `UseAdvancedMoonlight`, `UseCustomSteamFolder`, `UseUdpServerSmoothing`, `UseOscServer`, `UseOscSender`, `InterpretingOscMonitoring` |
| `int` | `FlashWhenLateAt`, `ProcessPriority`, `LogMaxArchiveFiles`, `OscServerPort`, `OscSenderPort` |
| `double` | `UdpServerSmoothingMinCutoff`, `UdpServerSmoothingBeta` |

重要なsetter条件：

- `LastVersionChecked`：`LastVersionCheckedNum` 再計算を保持。
- `FakeExeName`：無効ファイル名を拒否する既存挙動を保持。
- `OscSenderAddress`：既存 `Trim()` を保持。
- smoothingの2係数：既存 `UDPServerSmoothingMincutoffChanged`／`UDPServerSmoothingBetaChanged` 発火を保持。
- ViewModel側のイベント・ログ設定更新・保存処理は、そのまま別途維持。

### B5：環境API

```csharp
bool AdminNeeded();
bool IsWin8OrGreater();
bool IsWin10OrGreater();

bool HidHideInstalled { get; } // キャッシュ値
bool IsHidHideInstalled();     // 実環境プローブ
bool IsFakerInputInstalled();

void RefreshViGEmBusInfo();
bool IsViGEmBusInstalled();
bool IsRunningSupportedViGEmBus();
bool IsUsingMinViGEm117333();

IEnumerable<DS4Windows.Util.DISPLAY_DEVICE> GrabCurrentMonitors();
```

`AdminNeeded()` は管理者判定ではなく、ファイル書込みを試す操作です。`IsAdministrator()` への置換は不可です。

### B10：プロファイル補完API

```csharp
void LoadBlankDevProfile(
    int deviceIndex,
    bool launchProgram,
    ControlService control,
    bool xinputChange = true,
    bool postLoad = true);

void SaveAsProfile(int deviceIndex, string profileName);

bool LoadLinkedProfiles();
```

- `LoadBlankDevProfile`：Blank読込だけでなく標準SpecialAction、キャッシュ、一時プロファイル状態を保持。
- `SaveAsProfile`：通常保存前の `ResetProfile` を保持。
- 書込み処理は既存XML排他との整合が必要です。

---

## 7. 分類c：新責務の設計案

| ID | 提案インターフェース | 対象と責務 |
|---|---|---|
| **C1** | `IControlMetadataProvider` | 入力表示名、出力機種別表示名、マクロ符号の提供。可変static辞書を「定数」とみなさず、所有権・更新可否・未定義キー時の挙動を定義 |
| **C2** | `ISpecialActionEditingService` | SpecialActionの編集確定・削除。永続化、再読込、件数検証、ActionManager解放、キー状態リセットを含む操作境界 |
| **C3** | `IConfigurationLocationService` | 選択中の保存場所、複数保存場所検出結果、BackingStoreが保持する各XMLパスの一括更新。bootstrapとHost後の責務境界を整理 |
| **C4** | `IControllerConfigurationService` | `ControlServiceDeviceOptions` の共有実体と、デバイス固有設定の保存。接続状態サービスと永続化責務を混在させない |
| **C5** | `IStandardActionInitializationService` | 標準アクション導入時の複数プロファイルXML更新とアクション再読込。単一Actions.xmlリポジトリのCRUDではない |

想定APIの最小草案：

```csharp
// C1
string GetInputName(DS4Controls control);
string GetOutputName(X360Controls control, OutContType outputType);
int GetMacroValue(DS4Controls control);

// C2
void SaveAction(
    string name, string controls, int mode,
    string details, bool edit,
    double delayTime = 0.0, string extras = "");

void RemoveAction(string name);

// C3
bool MultipleSaveLocations { get; }
void SelectLocation(string path);

// C4
ControlServiceDeviceOptions DeviceOptions { get; }
bool SaveControllerConfigs(DS4Device device = null);

// C5
void CreateStandardActions();
```

特にC2は `SA.RemoveAction` に `SaveActions` を一つ足すだけでは、`Global.RemoveAction` 全体と同等になりません。ランタイム状態・再読込検証まで含めた操作設計が必要です。

---

## 8. フォールバックの扱い

**今回、フォールバックを「対象外」として消していません。** 表にFを付けて残しています。

| 種類 | 移行期に維持する理由 | 解消条件 |
|---|---|---|
| `?? Global.*ServiceInstance` | 既存の直接生成・任意引数・DI未解決時にもサービス参照を確保する経路 | 対象View／VMの生成経路で必須依存が渡ることを確認してから整理 |
| `AutoProfilesViewModel` の設定get/set fallback | サービス未取得でも従来設定を読書きできる互換経路 | 注入必須化と既存設定の同一実体確認 |
| `ProfileEditor.SaveProfile` fallback | リポジトリ未取得時の保存機能を維持 | DI生成経路と保存成否・ログの同等性確認 |
| `SpecialActionsListViewModel.GetActions` fallback | サービス未取得でも共有アクション一覧を参照 | リポジトリ注入の確実化 |
| `SpecialActionsListViewModel.RemoveAction` fallback | **通常DI分岐より多くの永続化・整合処理を持つ** | C2による操作同等性を先に確保。単純削除不可 |

Fは「移行完了」の印ではありません。また、ViewModelにService Locatorを増設する提案でもありません。

---

## 9. `using static`・alias・型・コメントの確認

### `using static`／alias

対象範囲で確認したstatic importは次の2件です。

| ファイル | 行 | 宣言 | Global無修飾参照への影響 |
|---|---:|---|---|
| `SettingsViewModel.cs` | 34 | `using static DS4Windows.Util;` | Globalの展開ではない |
| `TouchButtonUserControlViewModel.cs` | 26 | `using static DS4Windows.Mouse;` | Globalの展開ではない |

対象範囲で、`using static DS4Windows.Global`、Global型へのalias、Global継承は検索上確認されませんでした。確認された `Timer`、WPF型、Markdown、コントローラーオプションのaliasはGlobalへの別名ではありません。

**Global型だけを参照する `typeof(Global)` 等の除外対象は、今回の指定範囲では検出していません。** `ScpUtil.cs` 内の `typeof(Global)` は照合先実装にあり、今回の呼出元表の対象外です。

### コメント・文字列のみで、実参照として数えなかった箇所

| ファイル | 行番号 | 内容 |
|---|---|---|
| `LanguagePackControl.xaml.cs` | 73 | 説明コメント |
| `MainWindow.xaml.cs` | 304, 371, 1363 | コメントアウトコード／説明 |
| `ProfileEditor.xaml.cs` | 1391, 2334, 2408 | 説明／コメントアウトコード |
| `ProfileSettingsViewModel.cs` | 131, 147, 216, 293, 299, 501, 941 | 行末コメント／説明 |
| `LanguagePackViewModel.cs` | 61, 91 | コメントアウトコード |
| `WelcomeDialog.xaml.cs` | 68 | コメントアウトコード |
| `App.xaml.cs` | 92, 544 | XMLコメント／コメントアウトコード |

`LanguagePackViewModel.cs:60`、`LanguageSelectDialog.xaml.cs:141` は文字列本文にも `Global` が出ますが、**補間式内の実参照は除外していません。**

---

## 10. Global参照を検出しなかった31ファイル

上記の参照表に登場しないファイルも検索対象に含めています。

| 配下 | ファイル |
|---|---|
| [`DS4Forms/`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms) | `AxialStickUserControl.xaml.cs`, `ChangelogWindow.xaml.cs`, `ColorPickerWindow.xaml.cs`, `ControllerReadingsControl.xaml.cs`, `ControllerRegisterOptionsWindow.xaml.cs`, `FirstLaunchUtilWindow.xaml.cs`, `LightbarMacroCreator.xaml.cs`, `LogMessageDisplay.xaml.cs`, `OutputSlotManagerControl.xaml.cs`, `PluginOutDevWindow.xaml.cs`, `PresetOptionWindow.xaml.cs`, `ProfileNotificationWindow.xaml.cs`, `RecordBoxWindow.xaml.cs`, `RenameProfileWindow.xaml.cs`, `StickCalibrationWindow.xaml.cs`, `TouchButtonUserControl.xaml.cs`, `UtilMethods.cs` |
| [`DS4Forms/Converters/`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/Converters) | `EscapeAccessKeysConverter.cs`, `InvertBoolToVisibilityConverter.cs`, `MissingToGrayConverter.cs` |
| [`DS4Forms/Themes/`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/Themes) | `_.cs` |
| [`DS4Forms/ViewModels/`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels) | `AxialStickControlViewModel.cs`, `ChangelogViewModel.cs`, `ControllersViewModel.cs`, `CurrentOutDeviceViewModel.cs`, `FirstLauchUtilViewModel.cs`, `LightbarMacroViewModel.cs`, `PresetOptionViewModel.cs` |
| [`DS4Forms/ViewModels/Util/`](g:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Forms/ViewModels/Util) | `EnumChoiceSelection.cs`, `EventHandlers.cs`, `NotifyDataErrorBase.cs` |

---

## 結論

この範囲は、既存サービスへの機械的置換だけでは完了しません。優先して扱うべき境界は次のとおりです。

1. **プロファイル適用**：4スロット固定・成功値の不一致を是正してから移行。
2. **SpecialAction編集／削除**：保存・再読込・ランタイム整合をまとめた操作責務を設計。
3. **パス**：実行ファイルの解決済みパス、選択中設定パス、Roaming候補を区別。
4. **初期化**：Host前のbootstrapと、Host後の初回設定処理を分離して分類。
5. **定数判定**：可変辞書・CultureInfo・バージョンフィールドをconstとして除外しない。

**今回の表は指定範囲の再抽出・実装照合結果であり、Phase6全体や呼出元全体の監査完了を意味しません。**
