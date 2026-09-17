# Phase6-Step1 コア参照エビデンス（再監査）

**2026-09-18 再監査証拠資料。HEAD 59d46db47cc9d20f212b0f6fa881515b767c91ed。取得済み参照の分類記録。全件網羅・件数確定の証明ではない。aは契約候補ありを意味し無条件の置換承認ではない。**

- 出典: 取得済み調査結果の転記（新規調査は実施していない。本ファイルは文書保存のみを目的とする）。
- 本資料の本文は再監査セッションで取得・確認された内容の全文であり、全表・全行番号・精度限界を省略しない。

---

## 再監査結果

**読み取り専用で実施。ファイル作成・アプリ編集・ビルド・実行は行っていません。** 今回は不具合修正ではないため、提示された自己点検仮説の「再現テスト・ログ追加」は適用していません。

- `.git/HEAD` と参照先を直接読み取り、監査途中・終了前とも **`59d46db47cc9d20f212b0f6fa881515b767c91ed`** を確認しました。
- 以下は**作業ツリーの実コード**から取得した行番号です。未コミット差分の有無は未検証です。
- 旧193件・既存分類レポートの件数は流用していません。
- **無修飾参照が多数残存**しています。修飾形式だけの検索では監査不十分です。
- **重要な阻害要因**：既存 `IProfileApplicationService.ApplyProfile` は存在しますが、現在の実装はスロット4以上を拒否し、内側の適用失敗を戻り値に反映していません。したがって、該当参照を無条件に **a** として置換するのは不適切です。

### 表記

| 表記 | 意味 |
|---|---|
| a | 既存インターフェース・実装に対応する経路あり |
| b | 既存サービスの追加／修正が必要。表の `＋` は**未実装の提案メンバ** |
| c | 責務・ライフサイクルを含む設計が必要。移行先名は提案 |
| 除外 | `const`、状態非依存の計算、明示されたViGEmバックエンド除外 |
| H | 入力レポート／タッチ／ジャイロ処理のホットパス |
| H条件 | 入力経路上だが初回・状態変化・アクション成立時だけ |
| A | 入力から起動される非同期処理／マクロワーカー。同期ポーリング本体とは区別 |
| N | 初期化・設定変更・接続・停止等。通常ポーリング外 |
| N† | 通常は設定経路だが、入力起点のプロファイル適用からも到達する |
| Q / U | `Global.` 修飾 / `using static` による無修飾 |

移行先の略記：

- **P** = `IProfileSettingsService`
- **R** = `IProfileRepository`
- **AP** = `IProfileApplicationService`
- **PA** = `IProfileActionProvider`
- **SA** = `ISpecialActionRepository`
- **AS** = `IAppSettingsService`
- **ES** = `IEnvironmentService`
- **PS** = `IPathService`
- **OS** = `IOutputSlotService`
- **KB** = `IVirtualKBM`

`[i]` は元コードと同じスロット引数です。配列要素への書込みは、通知副作用のあるSetterへの置換と同一とは扱っていません。

---

## 1. `ControlService.cs`

対象：[絶対パス](G:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Control/ControlService.cs)  
`using static DS4Windows.Global;`：**36行**

### 初期化・KBM・環境・サーバー

| メソッド／経路 | 行番号・形式 | Globalメンバ | 分類 | 具体的な移行先／理由 |
|---|---|---|---|---|
| 静的初期化 N | 51 Q | `IsWin8OrGreater` | b | ES＋`IsWin8OrGreater()`。静的フィールド初期化へどう値を渡すか別途必要 |
| コンストラクタ N | 204 Q | `ProfileSettingsServiceInstance` | a | 注入済みPを使用。省略可能引数・フォールバック互換性は維持判断が必要 |
| コンストラクタ N | 242 Q | `DeviceOptions` | b | AS＋`DeviceOptions` → 同じ `BackingStore.deviceOptions` |
| コンストラクタ N | 250 Q | `UDPServerSmoothingMincutoffChanged` | b | AS＋同名イベント。既存イベントへadd/remove委譲 |
| コンストラクタ N | 251 Q | `UDPServerSmoothingBetaChanged` | b | AS＋同名イベント |
| `SystemEvents_DisplaySettingsChanged` N | 262 Q | `PrepareAbsMonitorBounds` | c | 提案 `IDisplayCoordinateService.PrepareAbsMonitorBounds(string)` |
| `CreateOSCCallback` 内コールバック N・受信時 | 332 Q | `isInterpretingOscMonitoring` | b | AS＋`InterpretingOscMonitoring` |
| 同上 | 356 U | `isUsingOSCSender` | b | AS＋`UseOscSender` |
| `RefreshOutputKBMHandler` N | 478,481 Q | `outputKBMHandler` null判定・代入 | c | 提案 `IKbmBackendLifecycle` の現在バックエンド管理／破棄 |
| 同上 | 480 Q | `outputKBMHandler.Disconnect` | a | KB.`Disconnect()`。前後の交換手順はc |
| 同上 | 484,486 Q | `outputKBMMapping` | a | P.`OutputKBMMapping` 判定・null代入 |
| `InitOutputKBMHandler` N | 495 Q | `InitOutputKBMHandler` | c | `IKbmBackendLifecycle.Initialize(identifier)` 案 |
| 同上 | 500 Q | `outputKBMHandler.Connect` | a | KB.`Connect()` |
| 同上 | 507 Q | `outputKBMHandler` 代入 | c | `IKbmBackendLifecycle` にフォールバック交換を集約 |
| 同上 | 512 U; 518 Q | `outputKBMHandler.GetIdentifier` | a | KB.`GetIdentifier()` |
| 同上 | 514 Q | `outputKBMHandler.Version` 書込み | c | ライフサイクル側で設定。**KB.`Version` はgetのみ** |
| 同上 | 514 Q | `fakerInputVersion` | b | ES＋`FakerInputVersion` |
| 同上 | 518 Q | `InitOutputKBMMapping` | c | バックエンドと対応マッピングを同時更新する初期化責務 |
| 同上 | 519 Q | `outputKBMMapping.PopulateConstants` | a | P.`OutputKBMMapping.PopulateConstants()` |
| 同上 | 520 Q | `outputKBMMapping.PopulateMappings` | a | P.`OutputKBMMapping.PopulateMappings()` |
| `DS4Devices_RequestElevation` N | 663 Q | `exelocation` | a | PS.`ExecutablePath` |
| `CheckHidHidePresence` N | 687 Q | `hidHideInstalled` | b | ES＋`HidHideInstalled`。キャッシュ値を維持 |
| 同上 | 698 Q | `exelocation` | a | PS.`ExecutablePath` |
| `UpdateHidHideAttributes` N | 764 Q | `hidHideInstalled` | b | ES＋`HidHideInstalled` |
| `UpdateHidHiddenAttributes` N | 792 Q | 同上 | b | 同上 |
| `CheckAffected` N | 803 Q | `GetInstanceIdFromDevicePath` | b | ES＋同名メソッド。実体はWin32照会で、単純文字列変換ではない |
| 同上 | 804 Q | `hidHideInstalled` | b | ES＋`HidHideInstalled` |
| 同上 | 806 Q | `CheckHidHideAffectedStatus` | b | ES＋同名メソッド。HidHide判定として維持 |
| `ChangeExclusiveStatus` N | 858 Q | `hidHideInstalled` | b | ES＋`HidHideInstalled` |
| `ChangeUDPStatus` N | 886 Q | `getUDPServerPortNum` | a | AS.`UdpServerPort` |
| 同上 | 887 Q | `getUDPServerListenAddress` | a | AS.`UdpServerListenAddress` |
| `ChangeOSCListenerStatus` N | 929,931 Q | `getOSCServerPortNum` | b | AS＋`OscServerPort` |
| `ChangeOSCSenderStatus` N | 945,946 Q | `getOSCSenderAddress` | b | AS＋`OscSenderAddress` |
| 同上 | 945,946 Q | `getOSCSenderPortNum` | b | AS＋`OscSenderPort` |
| `UseUDPPort` N | 1012 Q | `getUDPServerPortNum` | a | AS.`UdpServerPort` |
| 同上 | 1013 Q | `getUDPServerListenAddress` | a | AS.`UdpServerListenAddress` |
| `Start` N | 1623 Q | `IsAdministrator` | a | ES.`IsAdministrator()` |
| 同上 | 1624 Q | `outputKBMHandler.GetIdentifier` | a | KB.`GetIdentifier()` |
| 同上 | 1630 Q | `outputKBMHandler.GetFullDisplayName` | a | KB.`GetFullDisplayName()`。補間式内なので文字列として除外しない |
| 同上 | 1633 U | `getUseExclusiveMode` | a | AS.`UseExclusiveMode` |
| 同上 | 1643 U | `isUsingOSCServer` | b | AS＋`UseOscServer` |
| 同上 | 1648 U | `isUsingOSCSender` | b | AS＋`UseOscSender` |
| 同上 | 1653 U | `isUsingUDPServer` | a | AS.`UseUdpServer` |
| 同上 | 1736 Q | `getUDPServerPortNum` | a | AS.`UdpServerPort` |
| 同上 | 1737 Q | `getUDPServerListenAddress` | a | AS.`UdpServerListenAddress` |
| 同上 | 1774 U | `runHotPlug` 書込み | a | AS.`RunHotPlug` |
| 同上 | 1778 Q | `ProcessPriority` | b | AS＋`ProcessPriority` |
| `PrepareDevUDPMotion` のReportコールバック **H** | 1792 Q | `IsUsingUDPServerSmoothing` | b | AS＋`UseUdpServerSmoothing`。登録メソッドが低頻度でも参照自体は毎Report |
| `CheckQuickCharge` N・充電変化時 | 1822 U | `getQuickCharge` | b | AS＋`QuickCharge` |
| `Stop` N | 1848,1957 U | `runHotPlug` 書込み | a | AS.`RunHotPlug` |
| 同上 | 1863 U | `DCBTatStop` | b | AS＋`DCBTatStop` |
| 同上 | 1951 U | `outputKBMHandler.GetDisplayName` | a | KB.`GetDisplayName()` |
| 同上 | 1952 U | `outputKBMHandler.Disconnect` | a | KB.`Disconnect()` |

### プロファイル・出力状態・設定変更

| メソッド／経路 | 行番号・形式 | Globalメンバ | 分類 | 具体的な移行先／理由 |
|---|---|---|---|---|
| `PluginOutDev` N† | 1416 Q | `OutContType` | a | P.`OutContType[i]`。OS.`GetOutputDeviceType` は使用不可 |
| 同上 | 1419 U | `getDInputOnly` | a | P.`GetDInputOnly(i)`：永続設定 |
| 同上 | 1424,1567 U | `useDInputOnly` | a | P.`UseDInputOnlyArray[i]`：実行時状態 |
| 同上 | 1429,1495 U | `activeOutDevType` | a | OS.`ActiveOutDevType[i]` |
| `UnplugOutDev` N† | 1574,1604 U | `useDInputOnly` | a | P.`UseDInputOnlyArray[i]` |
| 同上 | 1584,1586 U | `activeOutDevType` | a | OS.`ActiveOutDevType[i]` |
| `Stop` N | 1904 U | `useDInputOnly` | a | P.`UseDInputOnlyArray[i]` |
| `PrepareConnectedInputControllerSettingEvents` N | 2095 Q | `RefreshExtrasButtons` | b | P＋`RefreshExtrasButtons(i, buttons)`。既存ControlSettingsGroupを更新 |
| 同上 | 2096 Q | `LoadControllerConfigs` | b | `IProfileXmlStore`＋`LoadControllerConfigsForDevice(device)` 案 |
| 同上 | 2101 U | `isUsingOSCSender` | b | AS＋`UseOscSender` |
| 同上 | 2116,2123 Q | `IsFirstConnection` | b | `IDeviceStateService`＋`IsFirstConnection(i)`。接続済み判定とは別状態 |
| 同上 | 2128,2154 U | `containsLinkedProfile` | b | R＋`ContainsLinkedProfile(serial)` |
| 同上 | 2131,2156 U | `getLinkedProfile` | b | R＋`GetLinkedProfile(serial)`。MACの `:` 除去を保持 |
| 同上 | 2137 U | `OlderProfilePath` | a | R.`OlderProfilePath[i]` |
| 同上 | 2141 Q | `MarkConnected` | b | `IDeviceStateService`＋`MarkConnected(i)` |
| 同上 | 2146,2151 Q | `SelectedProfile` | a | R.`SelectedProfile[i]` |
| 同上 | 2157,2162 Q | `LinkedProfileUI` | a | R.`LinkedProfileUI[i]` |
| 同上 | 2168 Q | `ApplyProfileToSlot` | **b** | AP.`ApplyProfile(i,name,false,false,ControlService,null)`。既存実装のスロット数・成功値を先に修正 |
| 同上 | 2174 U | `getMainColor` | a | P.`GetMainColor(i)` |
| 同上 | 2176 U | `getDInputOnly` | a | P.`GetDInputOnly(i)` |
| 同上 | 2188 U; 2191,2198 Q | `activeOutDevType` | a | OS.`ActiveOutDevType[i]` |
| 同上 | 2197 U | `useDInputOnly` | a | P.`UseDInputOnlyArray[i]` |
| `ResetUdpSmoothingFilters` N | 2257,2260 Q | `UDPServerSmoothingMincutoff` | b | AS＋同名プロパティ |
| 同上 | 2257,2260 Q | `UDPServerSmoothingBeta` | b | AS＋同名プロパティ |
| `ChangeUdpSmoothingAttrs` N | 2269,2275 Q | `UDPServerSmoothingMincutoff` | b | AS＋同名プロパティ |
| 同上 | 2269,2275 Q | `UDPServerSmoothingBeta` | b | AS＋同名プロパティ |
| `CheckProfileOptions` N† | 2281,2282 U | `getEnableOutputDataToDS4` | a | P.`GetEnableOutputDataToDS4(i)` |
| 同上 | 2285 U | `getIdleDisconnectTimeout` | a | P.`GetIdleDisconnectTimeout(i)` |
| 同上 | 2286 U | `getBTPollRate` | a | P.`GetBTPollRate(i)` |
| 同上 | 2288 U | `getTrackballFriction` | a | P.`GetTrackballFriction(i)` |
| 同上 | 2302 U | `getRumbleAutostopTime` | a | P.`GetRumbleAutostopTime(i)` |
| 同上 | 2309 U | `DualSenseRumbleEmulationMode` | a | P.`DualSenseRumbleEmulationMode[i]` |
| 同上 | 2325 U | `DualSenseHapticPowerLevel` | a | P.`DualSenseHapticPowerLevel[i]` |
| `CheckLauchProfileOption` N† | 2336 U | `LaunchProgram` | a | P.`LaunchProgram[i]` |
| `SetupInitialHookEvents` N | 2385 U | `WheelSmoothInfo` | a | P.`WheelSmoothInfo[i]` |
| 同メソッドで登録する設定変更コールバック N | 2408,2417 U | `L2OutputSettings` | a | P.`L2OutputSettings[i]` |
| 同上 | 2433,2442 U | `R2OutputSettings` | a | P.`R2OutputSettings[i]` |
| `On_SerialChange` N | 2540 U | `OnDeviceSerialChange` | b | `IDeviceStateService`＋`OnDeviceSerialChange(sender,i,serial)` と旧イベントへの橋渡し |
| `On_SyncChange` N | 2561 U | `useDInputOnly` | a | P.`UseDInputOnlyArray[i]` |
| 同上 | 2563 Q | `activeOutDevType` | a | OS.`ActiveOutDevType[i]` |
| 同上 | 2569 U | `getDInputOnly` | a | P.`GetDInputOnly(i)` |
| `On_DS4Removal` N | 2608,2670 U | `useDInputOnly` | a | P.`UseDInputOnlyArray[i]` |
| 同上 | 2661 U | `isUsingOSCSender` | b | AS＋`UseOscSender` |
| 同上 | 2671 Q | `activeOutDevType` | a | OS.`ActiveOutDevType[i]` |

### 入力処理

| メソッド／経路 | 行番号・形式 | Globalメンバ | 分類 | 具体的な移行先／理由 |
|---|---|---|---|---|
| `On_Report` H | 2701 U | `getFlashWhenLateAt` | b | AS＋`FlashWhenLateAt` |
| 同上・初回Report H条件 | 2749 U | `appdatapath` | a | PS.`AppDataPath`。On-Demand評価を維持 |
| 同上 | 2749 U | `ProfilePath` | a | R.`ProfilePath[i]`。補間式内も実参照 |
| 同上 | 2753 Q | `store.EmitMissingActionLogsForDevice` | a | R.`EmitMissingActionLogsForDevice(i,false)` |
| `On_Report` H | 2762 Q | `UseIconChoice` | a | `IAppearanceSettingsService.UseIconChoice` |
| 同上 | 2764 U | `InvokeBatteryChanged` | b | `IAppearanceSettingsService`＋`InvokeBatteryChanged(byte)`／旧 `BatteryChanged` の橋渡し |
| 同上 | 2775 Q | `GetGyroOutMode` | a | P.`GetGyroOutMode(i)` |
| 同上 | 2780 U | `outputKBMHandler.Sync` | a | KB.`Sync()` |
| 同上 | 2788,2846 U | `useDInputOnly` | a | P.`UseDInputOnlyArray[i]` |
| 同上 | 2805 U | `getEnableTouchToggle` | a | P.`GetEnableTouchToggle(i)` |
| 同上 | 2815 U | `containsCustomAction` | b | P＋`ContainsCustomAction(i)`：キャッシュ読取りを保持 |
| 同上 | 2815 U | `containsCustomExtras` | b | P＋`ContainsCustomExtras(i)`：キャッシュ読取りを保持 |
| 同上 | 2816 U | `getProfileActionCount` | b | PA＋`GetProfileActionCount(i)`：既存キャッシュを読む |
| 同上 | 2821 U | `isUsingOSCSender` | b | AS＋`UseOscSender` |
| 同上 | 2837 U | `isUsingOSCServer` | b | AS＋`UseOscServer` |
| 同上 | 2849 U | `activeOutDevType` | a | OS.`ActiveOutDevType[i]` |
| 同上 | 2851 Q | `GetOutputDS4TriggerMode` | a | P.`OutputDS4TriggerMode[i]` |
| 同上 | 2890 Q | `GetSASteeringWheelEmulationAxis` | a | P.`GetSASteeringWheelEmulationAxis(i)` |
| `LagFlashWarning` H条件 | 3114 U | `getFlashWhenLate` | b | AS＋`FlashWhenLate` |
| 同上 | 3128 U | `getMainColor` | a | P.`GetMainColor(i)` |
| `CheckForTouchToggle` H | 3212 U | `IsUsingTouchpadForControls` | a | P.`TouchOutMode[i] == TouchpadOutMode.Controls` |
| 同上 | 3214 U | `GetTouchActive` | a | P.`TouchpadActiveArray[i]` |
| 同上 | 3216,3223 U | `TouchActive` 書込み | a | P.`TouchpadActiveArray[i]`。通知付きSetterへの変更とは区別 |
| `StartTPOff` N† | 3237 U | `TouchActive` 書込み | a | P.`TouchpadActiveArray[i]` |
| `SetDevRumble` 出力フィードバック／アクション経路 | 3301 U | `getRumbleBoost` | a | P.`GetRumbleBoost(i)`。単なる `RumbleBoost[i]` 置換は不可 |

### 除外

| 行番号 | メンバ | 理由 |
|---|---|---|
| 47 Q | `MAX_DS4_CONTROLLER_COUNT` | `Global` の `const` |
| 49,51 Q | `OLD_XINPUT_CONTROLLER_COUNT` | `const`。条件コンパイル両枝を記載 |
| 2462 Q | `TEST_PROFILE_INDEX` | `const` |
| 1055 Q | `RefreshViGEmBusInfo` | `StartViGEm`、ViGEmバックエンド除外 |
| 1056,1760 Q | `IsRunningSupportedViGEmBus` | 同上 |
| 1631,1762 Q | `vigembusVersion` | 同上。**不変値だからではない** |
| 1756 U | `vigemInstalled` | 同上。**可変状態** |

`OutContType`／`activeOutDevType` は設定・共有状態であり、ViGEm関連というだけでは除外していません。

---

## 2. `Mapping.cs`

対象：[絶対パス](G:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Control/Mapping.cs)  
`using static DS4Windows.Global;`：**31行**

### 初期化・座標変換・プロファイル適用

| メソッド／経路 | 行番号・形式 | Globalメンバ | 分類 | 具体的な移行先／理由 |
|---|---|---|---|---|
| 静的フィールド初期化 N | 75 Q | `ProfileSettingsServiceInstance` | a | Pの依存受渡し。現在のstatic初期化・フォールバックを含む配線は別途必要 |
| `VirtualKBM` getter H/A | 78 Q | `outputKBMHandler` | a | KBの保持参照。毎回の `AppHost.GetService` 解決も残さない方針が必要 |
| `MapCustom` H | 3640 U; 3674 Q | `absUseAllMonitors` | c | 提案 `IDisplayCoordinateService.UseAllMonitors` |
| 同上 | 3658,3676 Q | `TranslateCoorToAbsDisplay` | c | 同サービス.`TranslateCoorToAbsDisplay(...)` |
| 同上 | 3668 U | `ButtonAbsMouseInfos` | a | P.`ButtonAbsMouseInfos[i]` |
| `MapCustomAction` H条件 | 5257 U | `ProfilePath` | a | R.`ProfilePath[i]` |
| 同上・`Task.Run`→`HaltReportingRunAction` A | 5315 Q | `ApplyProfile` | **b** | AP.`ApplyProfile(i,details,isTemp,true,MappingAction,prolog)`。既存実装の不備とHalt所有権を確認してから |
| `SAWheelEmulationCalibration` H条件 | 8249 Q | `SaveControllerConfigs` | b | `IProfileXmlStore`＋`SaveControllerConfigsForDevice(controller)` |
| `Scale360degreeGyroAxis` H条件 | 8423 Q | `LoadControllerConfigs` | b | 同＋`LoadControllerConfigsForDevice(controller)` |
| 同メソッド H | 8546 Q | `OutContType` | a | P.`OutContType[i]` |

### `SetCurveAndDeadzone` — すべてH・無修飾

| 行番号 | Globalメンバ | 分類 | Pの具体メンバ |
|---|---|---|---|
| 1595 | `getLSRotation` | a | `LSRotation[i]` |
| 1599 | `getRSRotation` | a | `RSRotation[i]` |
| 1603 | `GetLSAntiSnapbackInfo` | a | `LSAntiSnapbackInfo[i]` |
| 1604 | `GetRSAntiSnapbackInfo` | a | `RSAntiSnapbackInfo[i]` |
| 1616 | `GetLSDeadInfo` | a | `LSModInfo[i]` |
| 1617 | `GetRSDeadInfo` | a | `RSModInfo[i]` |
| 2107 | `GetL2ModInfo` | a | `L2ModInfo[i]` |
| 2162 | `GetR2ModInfo` | a | `R2ModInfo[i]` |
| 2216 | `getLSSens` | a | `LSSens[i]` |
| 2227 | `getRSSens` | a | `RSSens[i]` |
| 2235 | `getL2Sens` | a | `L2Sens[i]` |
| 2239 | `getR2Sens` | a | `R2Sens[i]` |
| 2243 | `GetSquareStickInfo` | a | `SquStickInfo[i]` |
| 2262 | `getLsOutCurveMode` | a | `GetLsOutCurveMode(i)` |
| 2371,2372,2385,2386 | `lsOutBezierCurveObj` | a | `LsOutBezierCurveObj[i]` |
| 2409 | `getRsOutCurveMode` | a | `GetRsOutCurveMode(i)` |
| 2518,2519,2531,2532 | `rsOutBezierCurveObj` | a | `RsOutBezierCurveObj[i]` |
| 2537 | `getL2OutCurveMode` | a | `GetL2OutCurveMode(i)` |
| 2576 | `l2OutBezierCurveObj` | a | `L2OutBezierCurveObj[i]` |
| 2580 | `getR2OutCurveMode` | a | `GetR2OutCurveMode(i)` |
| 2619 | `r2OutBezierCurveObj` | a | `R2OutBezierCurveObj[i]` |
| 2624 | `IsUsingSAForControls` | a | `GyroOutputMode[i] == GyroOutMode.Controls` |
| 2627 | `getSXDeadzone` | a | `SXDeadzone[i]` |
| 2628 | `getSZDeadzone` | a | `SZDeadzone[i]` |
| 2629 | `getSXMaxzone` | a | `SXMaxzone[i]` |
| 2630 | `getSZMaxzone` | a | `SZMaxzone[i]` |
| 2631 | `getSXAntiDeadzone` | a | `SXAntiDeadzone[i]` |
| 2632 | `getSZAntiDeadzone` | a | `SZAntiDeadzone[i]` |
| 2633 | `getSXSens` | a | `SXSens[i]` |
| 2634 | `getSZSens` | a | `SZSens[i]` |
| 2680 | `getSXOutCurveMode` | a | `GetSxOutCurveMode(i)` |
| 2727 | `sxOutBezierCurveObj` | a | `SxOutBezierCurveObj[i]` |
| 2731 | `getSZOutCurveMode` | a | `GetSzOutCurveMode(i)` |
| 2778 | `szOutBezierCurveObj` | a | `SzOutBezierCurveObj[i]` |

### その他の設定・アクション参照

| メソッド／経路 | 行番号・形式 | Globalメンバ | 分類 | 具体的な移行先 |
|---|---|---|---|---|
| `ApplyStickCalibration` H | 2788,2790 U | `RightStickDriftXAxis` | a | P.`RightStickDriftXAxis[i]` |
| 同上 | 2793,2795 U | `RightStickDriftYAxis` | a | P.`RightStickDriftYAxis[i]` |
| 同上 | 2799,2801 U | `LeftStickDriftXAxis` | a | P.`LeftStickDriftXAxis[i]` |
| 同上 | 2805,2807 U | `LeftStickDriftYAxis` | a | P.`LeftStickDriftYAxis[i]` |
| `MapCustom` H | 2896 U | `getProfileActionCount` | b | PA＋`GetProfileActionCount(i)` |
| 同上 | 2910 U | `GetControlSettingsGroup` | b | P＋`GetControlSettingsGroup(i)`。`GetDS4CSettings` のListとは別型 |
| 同上 | 3274 U | `getProfileActions` | a | R.`ProfileActions[i]` |
| 同上 | 3285 U | `GetProfileAction` | a | PA.`GetProfileAction(i,name)` |
| 同上 | 3459 U | `GetSASteeringWheelEmulationAxis` | a | P.`GetSASteeringWheelEmulationAxis(i)` |
| `ProcessControlSettingAction` H | 4461,4475 U | `ButtonMouseInfos` | a | P.`ButtonMouseInfos[i]` |
| 同上 | 4601 U | `reverseX360ButtonMapping` | a※ | P.`GetReverseX360ButtonMapping()[(int)xboxControl]`。Cloneコスト注意 |
| `IfAxisIsNotModified` 到達未確認 | 4877 U | `GetDS4CSetting` | a | P.`GetDS4CSetting(i,dc)`。検索上は定義のみ。未使用でもGlobal参照として掲載 |
| `LogActionDoneCountOnTrigger` H条件 | 4887 U | `GetActions` | a | SA.`ActionList`。`Actions` はコピーを生成する別経路 |
| `MapCustomAction` H | 4938 U | `GetActions` | a | SA.`ActionList` |
| 同上 | 4946 U | `getProfileActions` | a | R.`ProfileActions[i]`。既存の `.ToArray()` スナップショット維持 |
| 同上 | 4978 U | `GetProfileAction` | a | PA.`GetProfileAction(i,name)` |
| 同上 | 4979 U | `GetProfileActionIndexOf` | b | PA＋`GetProfileActionIndexOf(i,name)` |
| 同上 | 4998,5263,6089 U | `GetDS4CSetting` | a | P.`GetDS4CSetting(i,control)` |
| 同上・適用後処理 A | 5320 U | `getProfileActions` | a | R.`ProfileActions[i]` |
| 同上 | 5324 U | `GetProfileAction` | a | PA.`GetProfileAction(i,name)` |
| 同上 | 5325 U | `GetProfileActionIndexOf` | b | PA＋`GetProfileActionIndexOf(i,name)` |
| `ReleaseActionKeys` H条件 | 6122 U | `GetDS4CSetting` | a | P.`GetDS4CSetting(i,dc)` |
| `GetAbsMouseMapping` H | 6752 U | `ButtonAbsMouseInfos` | a | P.`ButtonAbsMouseInfos[i]` |
| `getMouseMapping` H | 7092 U | `getLSDeadzone` | a | P.`LSModInfo[i].deadZone` |
| 同上 | 7094 U | `getRSDeadzone` | a | P.`RSModInfo[i].deadZone` |
| 同上 | 7098 U | `ButtonMouseInfos` | a | P.`ButtonMouseInfos[i]` |
| `GetByteMapping` H | 7506 U | `IsUsingSAForControls` | a | P.`GyroOutputMode[i] == GyroOutMode.Controls` |
| `GetBoolMappingExternal` H／外部呼出し | 7631 U | 同上 | a | 同上 |
| 同上 | 7635,7636 U | `SXSens` | a | P.`SXSens[i]` |
| 同上 | 7637,7638 U | `SZSens` | a | P.`SZSens[i]` |
| `GetBoolMapping` H | 7694 U | `IsUsingSAForControls` | a | P.`GyroOutputMode[i] == GyroOutMode.Controls` |
| `getBoolSpecialActionMapping` H | 7838 U | 同上 | a | 同上 |
| `GetBoolActionMapping` H | 7936 U | 同上 | a | 同上 |
| `GetXYAxisMapping` H | 8043 U | 同上 | a | 同上 |
| `Scale360degreeGyroAxis` H | 8456 U | `SAWheelFuzzValues` | a | P.`SAWheelFuzzValues[i]` |
| 同上 | 8471 U | `getSXDeadzone` | a | P.`SXDeadzone[i]` |
| 同上 | 8530 U | `WheelSmoothInfo` | a | P.`WheelSmoothInfo[i]` |
| 同上 | 8543 U | `getSXAntiDeadzone` | a | P.`SXAntiDeadzone[i]` |

### `outputKBMMapping` 全参照

**全行a。移行先は P.`OutputKBMMapping` の同じ下位メンバ。** 大文字名でも `Global` のconstではありません。

| メソッド／経路 | 行番号 | 下位メンバ |
|---|---|---|
| `Commit` H・U | 1356,1433 | `MOUSEEVENTF_LEFTDOWN` |
| 同上 | 1363,1460 | `MOUSEEVENTF_RIGHTDOWN` |
| 同上 | 1369,1447 | `MOUSEEVENTF_MIDDLEDOWN` |
| 同上 | 1375,1381,1473,1486 | `MOUSEEVENTF_XBUTTONDOWN` |
| 同上 | 1387,1440 | `MOUSEEVENTF_LEFTUP` |
| 同上 | 1389,1466 | `MOUSEEVENTF_RIGHTUP` |
| 同上 | 1391,1453 | `MOUSEEVENTF_MIDDLEUP` |
| 同上 | 1393,1395,1479,1492 | `MOUSEEVENTF_XBUTTONUP` |
| 同上 | 1499,1501 | `WHEEL_TICK_UP` |
| 同上 | 1510,1512 | `WHEEL_TICK_DOWN` |
| `ProcessControlSettingAction` H・U | 4565 | `GetRealEventKey` |
| `MapCustomAction` H条件・U | 5268,5276 | `GetRealEventKey` |
| 同上・Q | 5539 | `GetRealEventKey` |
| `ReleaseActionKeys` H条件・U | 6127,6135 | `GetRealEventKey` |
| `PlayMacroCodeValue` A・U | 6515 | `MOUSEEVENTF_LEFTDOWN` |
| 同上 | 6519 | `MOUSEEVENTF_RIGHTDOWN` |
| 同上 | 6523 | `MOUSEEVENTF_MIDDLEDOWN` |
| 同上 | 6527,6531 | `MOUSEEVENTF_XBUTTONDOWN` |
| 同上 | 6535,6579 | `macroKeyTranslate` |
| 同上 | 6536,6580 | `GetRealEventKey` |
| 同上 | 6559 | `MOUSEEVENTF_LEFTUP` |
| 同上 | 6563 | `MOUSEEVENTF_RIGHTUP` |
| 同上 | 6567 | `MOUSEEVENTF_MIDDLEUP` |
| 同上 | 6571,6575 | `MOUSEEVENTF_XBUTTONUP` |
| `AltTabSwapping` A・U | 6683,6692,6694 | `KEY_TAB` |
| `AltTabSwappingRelease` H条件/A・U | 6705 | `KEY_TAB` |
| 同上 | 6707 | `KEY_LALT` |
| `GetMouseWheelMapping` H・Q | 6721 | `WHEEL_TICK_DOWN` |
| 同上 | 6722 | `WHEEL_TICK_UP` |

### 除外：計算・const

| 経路 | 行番号 | メンバ・理由 |
|---|---|---|
| `SetCurveAndDeadzone` H | 1678,1679,1686,1687,1793,1839,1911,1912,1920,1921,2028,2074,2123,2133,2178,2188,2219,2220,2230,2231,2237,2241 Q | `Clamp`：引数だけを使用する計算 |
| `CalculateControllerAngle` H | 8197 Q | `Clamp`：同上 |
| `MapCustomAction` H条件 | 5628,5630 U | `getTransitionedColor`：引数の色・比率だけを使用する計算 |
| 静的初期化 N | 173,610,611,614,637,677,733,740,956,963,973,977,982,988,1015,1018,1019,1020,1021,1024,1035,1209,2838 Q | `MAX_DS4_CONTROLLER_COUNT`：const |
| 静的初期化 N | 590,603,622 Q | `TEST_PROFILE_ITEM_COUNT`：const |

**配列自体が不変という判断ではありません。** 上表は配列サイズに用いた `Global` のconst参照のみの除外です。

---

## 3. `DS4LightBar.cs`

対象：[絶対パス](G:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Control/DS4LightBar.cs)  
`using static DS4Windows.Global;`：**22行**

| メソッド／経路 | 行番号・形式 | Globalメンバ | 分類 | 具体的な移行先／理由 |
|---|---|---|---|---|
| `updateLightBar` H | 70 U | `getLightbarSettingsInfo` | a | P.`GetLightbarSettingsInfo(i)` |
| 同上 | 125 U | `getMainColor` | a | P.`GetMainColor(i)` |
| 同上 | 185 U | `getIdleDisconnectTimeout` | a | P.`GetIdleDisconnectTimeout(i)` |
| 同上 | 298 U | `DistanceProfiles` | b | R＋`DistanceProfiles[i]`。`BackingStore.distanceProfiles` と同じ実体 |
| 同上 | 298 U | `tempprofileDistance` | a | P.`TempProfileDistanceArray[i]` |
| 同上 | 121,181,197,203,260,309,315,317 U | `getTransitionedColor` | 除外 | 状態非依存の色補間計算 |
| 静的初期化 N | 44,45,51,52,58,59,60 Q | `MAX_DS4_CONTROLLER_COUNT` | 除外 | const |

注意：

- `defaultLight`、`shuttingdown` は**このクラス自身のフィールド**。
- `HuetoRGB` は**このクラス自身のメソッド**。
- `Max` は `System.Math` のstatic import。
- `BatteryIndicatorDurations` はこのクラスの **readonly配列＝要素可変**。Global参照ではないため対象外であり、不変扱いではありません。

---

## 4. `Mouse.cs`

対象：[絶対パス](G:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Control/Mouse.cs)

**Globalのstatic importなし。以下はすべてQ。**

| メソッド／経路 | 行番号 | Globalメンバ | 分類 | 具体的な移行先 |
|---|---|---|---|---|
| `sixaxisMoved` H | 228 | `GetGyroOutMode` | a | P.`GetGyroOutMode(i)` |
| 同上 | 248 | `getGyroSensitivity` | a | P.`GetGyroSensitivity(i)` |
| 同上 | 279 | `GetGyroSwipeInfo` | a | P.`GetGyroSwipeInfo(i)` |
| `IsGyroTriggerActive` H | 330 | `getGyroTriggerTurns` | a | P.`GetGyroTriggerTurns(i)` |
| 同上 | 333 | `GetGyroControlsInfo` | a | P.`GetGyroControlsInfo(i)` |
| 同上 | 339 | `getSATriggers` | a | P.`GetSATriggers(i)` |
| 同上 | 340 | `getSATriggerCond` | a | P.`GetSATriggerCond(i)` |
| 同上 | 344 | `GetGyroMouseStickTriggerTurns` | a | P.`GetGyroMouseStickTriggerTurns(i)` |
| 同上 | 345 | `GetSAMouseStickTriggers` | a | P.`GetSAMouseStickTriggers(i)` |
| 同上 | 346 | `GetSAMouseStickTriggerCond` | a | P.`GetSAMouseStickTriggerCond(i)` |
| `ReplaceOneEuroFilterPair` N | 390 | `GyroMouseStickInf` | a | P.`GyroMouseStickInf[i].RemoveRefreshEvents()` |
| `SetupLateOneEuroFilters` N・呼出経路要確認 | 399,400,401,402 | `GyroMouseStickInf` | a | P.`GyroMouseStickInf[i]` の `MinCutoff/Beta/SetRefreshEvents` |
| `PostSetup` N† | 407 | `TouchOutMode` | a | P.`TouchOutMode[i]` |
| 同上 | 410 | `GetTouchMouseStickInfo` | a | P.`TouchMouseStickInf[i]` |
| 同上 | 420 | `GetGyroOutMode` | a | P.`GetGyroOutMode(i)` |
| 同上 | 423 | `GyroMouseStickInf` | a | P.`GyroMouseStickInf[i]` |
| `Reset` N† | 432 | `TouchOutMode` | a | P.`TouchOutMode[i]` |
| 同上 | 444 | `GetGyroOutMode` | a | P.`GetGyroOutMode(i)` |
| 同上 | 447 | `GyroMouseStickInf` | a | P.`GyroMouseStickInf[i].RemoveRefreshEvents()` |
| `SixMouseReset` H | 463 | `GetGyroMouseStickInfo` | a | P.`GetGyroMouseStickInfo(i)` |
| `SixMouseStick` H | 475 | `getGyroMouseStickHorizontalAxis` | a | P.`GetGyroMouseStickHorizontalAxis(0)`。**元コードの引数0を勝手に変更しない** |
| 同上 | 482 | `GetGyroMouseStickInfo` | a | P.`GetGyroMouseStickInfo(i)` |
| `TouchpadMouseStick` H | 778 | `GetTouchMouseStickInfo` | a | P.`TouchMouseStickInf[i]` |
| 同上 | 785,786 | `Clamp` | 除外 | 状態非依存の計算 |
| `touchesMoved` H | 1105 | `TouchOutMode` | a | P.`TouchOutMode[i]` |
| 同上 | 1108,1162,1172 | `GetTouchActive` | a | P.`TouchpadActiveArray[i]` |
| 同上 | 1110 | `getTouchDisInvertTriggers` | a | P.`TouchDisInvertTriggers[i]` |
| 同上 | 1118,1134 | `getTrackballMode` | a | P.`GetTrackballMode(i)` |
| 同上 | 1171 | `GetTouchMouseStickInfo` | a | P.`TouchMouseStickInf[i]` |
| `touchesBegan` H | 1219 | `TouchOutMode` | a | P.`TouchOutMode[i]` |
| 同上 | 1248 | `getDoubleTap` | a | P.`GetDoubleTap(i)` |
| 同上 | 1251 | `TapSensitivity` | a | P.`TapSensitivity[i]` |
| `touchesEnded` H | 1277 | `getTapSensitivity` | a | P.`TapSensitivity[i]` |
| 同上 | 1278,1304 | `TouchOutMode` | a | P.`TouchOutMode[i]` |
| 同上 | 1291 | `getDoubleTap` | a | P.`GetDoubleTap(i)` |
| 同上 | 1307 | `getTouchDisInvertTriggers` | a | P.`TouchDisInvertTriggers[i]` |
| 同上 | 1315 | `getTrackballMode` | a | P.`GetTrackballMode(i)` |
| 同上 | 1391 | `GetTouchMouseStickInfo` | a | P.`TouchMouseStickInf[i]` |
| 同上 | 1483 | `TouchAbsMouse` | a | P.`TouchAbsMouse[i]` |
| 同上 | 1484 | `GetTouchActive` | a | P.`TouchpadActiveArray[i]` |
| `touchUnchanged` H | 1553 | `TouchOutMode` | a | P.`TouchOutMode[i]` |
| 同上 | 1562 | `getTouchDisInvertTriggers` | a | P.`TouchDisInvertTriggers[i]` |
| `TouchButtonCheckProcess` H | 1671 | `TouchpadButtonMode` | a | P.`TouchpadButtonMode[i]` |
| `synthesizeMouseButtons` H | 1804,1849 | `TouchOutMode` | a | P.`TouchOutMode[i]` |
| 同上 | 1807 | `TouchClickPassthru` | a | P.`TouchClickPassthru[i]` |
| 同上 | 1820,1831,1837,1843 | `GetDS4CSetting` | a | P.`GetDS4CSetting(i,control)` |
| 同上 | 1854 | `TapSensitivity` | a | P.`TapSensitivity[i]` |
| `touchButtonUp` H | 1880 | `TouchpadButtonMode` | a | P.`TouchpadButtonMode[i]` |
| `touchButtonDown` H | 1893 | `LowerRCOn` | a | P.`LowerRCOn[i]` |

`GetTouchMouseStickInfo` という名前のDIメソッドはありませんが、**同じBackingStoreを返す `TouchMouseStickInf[i]` があるためa**です。

---

## 5. `MouseCursor.cs`

対象：[絶対パス](G:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Control/MouseCursor.cs)

**Globalのstatic importなし。以下はすべてQ。**

| メソッド／経路 | 行番号 | Globalメンバ | 分類 | 具体的な移行先 |
|---|---|---|---|---|
| コンストラクタ N | 34,35 | `GyroMouseInfo` | a | P.`GyroMouseInfo[i].SetRefreshEvents(...)` |
| `ReplaceOneEuroFilterPair` N | 40 | `GyroMouseInfo` | a | P.`GyroMouseInfo[i].RemoveRefreshEvents()` |
| `SetupLateOneEuroFilters` N | 46,47,48,49 | `GyroMouseInfo` | a | P.`GyroMouseInfo[i]` の `MinCutoff/Beta/SetRefreshEvents` |
| `sixaxisMoved` H | 86 | `getGyroMouseHorizontalAxis` | a | P.`GetGyroMouseHorizontalAxis(i)` |
| 同上 | 92 | `GyroMouseInfo` | a | P.`GyroMouseInfo[i]` |
| 同上 | 96 | `getGyroSensitivity` | a | P.`GetGyroSensitivity(i)` |
| 同上 | 143 | `getGyroSensVerticalScale` | a | P.`GetGyroSensVerticalScale(i)` |
| 同上 | 252 | `getGyroInvert` | a | P.`GetGyroInvert(i)` |
| 同上 | 260 | `outputKBMHandler.MoveRelativeMouse` | a | KB.`MoveRelativeMouse(x,y)` |
| `mouseRemainderReset` H | 274 | `GyroMouseInfo` | a | P.`GyroMouseInfo[i]` |
| `TouchesMovedAbsolute` H | 343 | `TouchAbsMouse` | a | P.`TouchAbsMouse[i]` |
| 同上 | 362 | `absUseAllMonitors` | c | 提案 `IDisplayCoordinateService.UseAllMonitors` |
| 同上 | 364 | `TranslateCoorToAbsDisplay` | c | 同サービス.`TranslateCoorToAbsDisplay(...)` |
| 同上 | 369 | `outputKBMHandler.MoveAbsoluteMouse` | a | KB.`MoveAbsoluteMouse(x,y)` |
| `TouchCenterAbsolute` H条件 | 375 | `outputKBMHandler.MoveAbsoluteMouse` | a | KB.`MoveAbsoluteMouse(0.5,0.5)` |
| `TouchMoveCursor` H | 380 | `TouchRelMouse` | a | P.`TouchRelMouse[i]` |
| 同上 | 387,388 | `Clamp` | 除外 | 状態非依存の計算 |
| 同上 | 396 | `getTouchSensitivity` | a | P.`TouchSensitivity[i]` |
| 同上 | 397 | `getTouchpadJitterCompensation` | a | P.`TouchpadJitterCompensation[i]` |
| 同上 | 472 | `getTouchpadInvert` | a | P.`TouchpadInvert[i]` |
| 同上 | 481 | `outputKBMHandler.MoveRelativeMouse` | a | KB.`MoveRelativeMouse(x,y)` |

---

## 6. 誤検出・除外したコード

### `Global.` を含むコメント

| ファイル | 行番号 |
|---|---|
| `ControlService.cs` | 256,1285,1576,1903,2166,2291,2673,2674,2675 |
| `Mapping.cs` | 612,613,3497,3558,3561,3646,3656,4943 |
| `Mouse.cs` | 129,130,777,1194,1392 |

### 無修飾候補の主な誤検出

| 対象 | 除外理由 |
|---|---|
| `ControlService.MAX_DS4_CONTROLLER_COUNT` の無修飾利用 | 同クラス47行のconstが優先される。Globalからの無修飾参照として重複計上しない |
| `ControlService.deviceOptions`／`DeviceOptions` | 同クラス107–108行のフィールド／プロパティ。Global参照は242行の右辺のみ |
| `Mapping` の `profileActions` 等 | ローカル変数。Global.`ProfileActions` と混同しない |
| `OutContType.DS4`／`.X360`／`.None` | enum型の利用。Globalの配列プロパティ利用ではない |
| `GyroMouseInfo.SmoothingMethod` 等 | 型／ネスト型の利用 |
| `Mapping` の `GetBeingTriggered`、`SetBeingTriggered`、`GetBoolMapping` 等 | 同クラス定義のメソッド |
| `Mapping` 2102–2104、2158–2160 | ブロックコメント内の旧デッドゾーン参照 |
| `Mapping` 2905–2906、4974–4976、5862–5867、7342–7343 | コメント内の旧Global呼出し候補 |
| `DS4LightBar` 119、299等 | コメント中の旧色／プロファイル参照 |
| ログ内の文字列ラベル | 実参照ではない。ただし `{Global.xxx}`／`{ProfilePath[i]}` は実参照として掲載 |

`ScpUtil.cs` は複数型を含みます。**Globalは537行から4080行付近まで**であり、後続 `Changelog`／`BackingStore` のstatic定義をGlobalメンバとして取り込んではいけません。

---

## 7. 実装照合の根拠と重要な差異

| 確認事項 | 根拠 |
|---|---|
| Pの既存契約 | [`IProfileSettingsService.cs`](G:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DI/IProfileSettingsService.cs)：30–55、59–139、142–203、206–245行 |
| 設定配列が同じBackingStoreへ委譲される | [`ProfileSettingsService.cs`](G:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Control/Services/ProfileSettingsService.cs)：23–28、267–400、508–605行付近 |
| `useDInputOnly` と `getDInputOnly` は別状態 | [`ScpUtil.cs`](G:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Control/ScpUtil.cs)：997–1008、2508–2513行 |
| `activeOutDevType` の正しい既存DI | [`IOutputSlotService.cs`](G:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DI/IOutputSlotService.cs)：79行、[`OutputSlotService.cs`](G:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Control/Services/OutputSlotService.cs)：196行 |
| `GetOutputDeviceType` は代用不可 | `IOutputSlotService.cs`：34–48行。孤立 `_deviceTypes` の警告が明記済み |
| プロファイル配列・欠落アクションログの正規経路 | [`ProfileRepository.cs`](G:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Control/Services/ProfileRepository.cs)：190–193、215、226–227行 |
| KBアダプタは登録済み | [`ServiceRegistration.cs`](G:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DI/ServiceRegistration.cs)：68行 |
| KBアダプタはバックエンドを保持せず毎回遅延委譲 | [`OutputKBMHandlerAdapter.cs`](G:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Control/Services/OutputKBMHandlerAdapter.cs)：11–41行 |
| KBのVersionは読取り専用 | [`IVirtualKBM.cs`](G:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Control/Services/IVirtualKBM.cs)：10行 |
| APにスロット上限・成功値の問題 | [`ProfileApplicationService.cs`](G:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Control/Services/ProfileApplicationService.cs)：103–118行。`deviceIndex >= 4`、Global戻り値を捨て `success = true` |
| MappingActionではAPがHaltしない | 同：122–130行。現在のMapping側5313行付近の外側Haltを不用意に消せない |
| アクション個数はキャッシュ | `ScpUtil.cs`：3328–3331行。`ProfileActions.Count` への置換を同義と断定しない |
| プロファイル別アクションindexは独自辞書 | `ScpUtil.cs`：3493–3498行。SA.`GetActionIndex` と別物 |
| PA取得は同じ辞書だがTrace追加あり | [`ProfileActionProvider.cs`](G:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Control/Services/ProfileActionProvider.cs)：28–43行 |
| 逆引き配列のDI getterはClone | `ProfileSettingsService.cs`：`GetReverseX360ButtonMapping()`、約619–622行。入力処理ごとの割当増加に注意 |
| SA.`Actions` はコピー、`ActionList` は実体 | [`SpecialActionRepository.cs`](G:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Control/Services/SpecialActionRepository.cs)：36–52行 |
| 表示座標変換は純粋関数ではない | `ScpUtil.cs`：3963–4017行。`absDisplayBounds`／`fullDesktopBounds`／モニター列挙状態に依存 |
| `GetInstanceIdFromDevicePath` は環境依存 | `ScpUtil.cs`：1771–1784行。Win32 `CM_Get_Device_Interface_Property` を使用 |

### ホットパス判定の根拠

- `ControlService.cs` **2225–2228行**：`device.Report` → `On_Report`。
- 同 **1785–1816行**：UDP平滑化判定は登録時ではなくReportコールバック内。
- 同 **2812、2826、2914–2917行付近**：曲線処理、`MapCustom`、`Commit`、ライトバー更新。
- 同 **2484–2495行**：タッチイベントとSixAxisイベントを `Mouse` へ登録。
- `Mouse.cs` **254–258、1164、1385、1486、1614行**：`MouseCursor` への入力経路。
- `Mapping.cs` **3461行**：`MapCustom` → `Scale360degreeGyroAxis` → 校正・設定ロード。
- `Mapping.cs` **5597、5783行**：`ReleaseActionKeys` の実呼出しあり。
- AltTab解除は **6653、6662、6671行**の `EndMacro` → `AltTabSwappingRelease`。**`ReleaseActionKeys` と同一経路と決めつけていません。**

---

## 8. 主エージェントへの申し送り・精度の限界

1. **cの主要設計対象**
   - `IDisplayCoordinateService`：全モニター使用状態・座標変換・表示構成変更時の更新。
   - `IKbmBackendLifecycle`：ハンドラ生成、フォールバック交換、Version設定、マッピング生成、破棄順序。
   - 両方とも名前は**提案**であり、既存インターフェースではありません。

2. **aでも機械置換だけでは安全でない箇所**
   - `GetReverseX360ButtonMapping()` のClone。
   - PAのTraceログ増加。
   - 配列直接書込みから通知付きSetterへの変更。
   - KBアダプタのnull時no-opと、旧直接呼出しの例外挙動の差。
   - `GetRumbleBoost` のDualSense分岐。
   - staticクラスへの依存受渡しとフォールバック維持。

3. **readonly配列は不変扱いしていません。**
   `reverseX360ButtonMapping`、`DistanceProfiles`、設定配列、KBMマッピングを「固定値」として除外するのは誤りです。

4. **網羅性の保証範囲**
   - 表は修飾検索、Global定義の確認、無修飾候補の単語検索、メソッド・実装の直接読取りで構成しました。
   - **Roslynによる全識別子のシンボル解決・自動集計は未実施**です。Global全定義の独立した完全抽出リストも生成していません。
   - 検索結果には件数上限・大文字小文字を区別しない一致・複数行正規表現による表示行のずれがあり、主要箇所は直接読取りで補完しましたが、**全行をASTで証明した監査完了とは断言しません**。
   - したがって、旧件数に代わる**確定総件数・分類別総数は提示しません**。同じ行の複数参照を「1件」にする集計も避けています。

5. **対象外の隣接ファイル**
   [`MouseWheel.cs`](G:/Cursor_Folder/DS4Windows-Vader4Pro/DS4Windows/DS4Control/MouseWheel.cs) に55、82、83、85、86、88行のGlobal参照を確認しました。今回指定の5ファイルには混入していません。Mouse系全体の移行完了判定では別途対象化が必要です。
