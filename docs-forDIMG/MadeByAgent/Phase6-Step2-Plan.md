# Phase6-Step2 計画書: `ControlService.cs` のGlobal直参照解消

作成日: 2026-09-09  
改訂日: 2026-09-18（Phase6-Step1 コア参照エビデンスに基づく全74箇所網羅・Pure DI設計・ホットパス性能保護の全面拡充改訂）  
状態: 計画書改訂・承認待ち（実装未着手）  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`, `docs-forDIMG/DI-App-Wide-Migration-Plan.md`  
参照エビデンス:  
  - `Phase6-Step1-Core-Reference-Evidence.md`（コア層実地調査エビデンス）  
  - `Phase6-Step1-ABC-Classification.md`（a/b/c分類・契約差検証記録）  
  - `Phase6-Step1-Unique-ID-Manifest.md`（一意ID台帳）  
  - `Phase5-Step14-Issue7-RootCause-and-CrossSetting-Audit-Report.md`（横断設定・孤立プロパティ監査）  

---

## 0. 背景と改訂の経緯

### 0.1 暫定39件から全74実参照式への昇華
Phase6上位計画（`Phase6-Plan.md` §0.2）では、`ControlService.cs` の対象件数を「暫定39件」としていた。これは主に `Global.` 修飾子による単純 grep 抽出に由来する値であった。  
しかし、2026-09-18に実施された Step1 再監査（`Phase6-Step1-Core-Reference-Evidence.md`）において、同ファイル冒頭の `using static DS4Windows.Global;`（36行目）による**無修飾参照（U: Unqualified）**が多数実利用されていることが判明した。  
実コードの精密走査の結果、修飾参照（Q: Qualified）84行・93出現に加え、無修飾参照を含めた実質的な Global 依存式は**計74箇所（除外8箇所を除く実移行対象66箇所）**に上ることが確定した。

本計画書は、先行して記録されていた候補（C2-01〜C2-03）の枠組みを全74箇所へ全面的に拡張し、すべての参照箇所に対する移行先サービス、分類（a: 既存契約 / b: 軽微拡張 / c: 要設計 / 除外）、ホットパス区分、依存性注入（Pure DI）構造、PR分割計画、および検証基準を完全網羅した実装計画書として改訂したものである。

### 0.2 `ControlService.cs` の重要性と課題
`ControlService.cs`（約3,400行）は、4層アーキテクチャにおいて**第1層（入力読み取り層）と第2層（信号変換層）の境界**に位置し、コントローラーのライフサイクル管理、ViGEm仮想バスへの出力割り当て、UDP/OSCモーションサーバー連携、および毎秒250〜1000回実行される入力ポーリング（`On_Report`）を統括する最重要コンポーネントである。  
本改修における最大のリスクは以下の3点である：
1. **ホットパス性能の維持**: 入力ポーリングループ内で仮想メソッド呼び出しや余分なヒープ割り当て・クローン処理が発生し、遅延やGCスパイクを引き起こすリスク。
2. **循環依存の防止**: `ProfileApplicationService` が既に `ControlService` に依存しているため、逆向きにコンストラクタ注入を行うことによる循環参照リスク。
3. **二重管理・孤立設定の再発防止**: DIサービス側が独自フィールドを持たず、正本（`BackingStore` / `Global`）と完全に同期する薄いシム（SSOT）を維持する原則。

---

## 1. 目的

`ControlService.cs` 内のすべての `Global.*` 直接参照および `using static DS4Windows.Global;` 経由の無修飾静的参照を解消し、コンストラクタ経由で注入された DI サービス経由の呼び出しへ完全移行する。これにより、信号変換層・入力監視層の境界における静的依存を根絶し、テスト容易性と保守性を恒久的に向上させる。

---

## 2. 全件参照台帳（全74箇所・完全カタログ）

`Phase6-Step1-Core-Reference-Evidence.md` の実コード検証結果に基づく全参照の一覧である。

### 2.1 区分表記の定義
- **形式**: `Q` = `Global.` 明示修飾 / `U` = `using static DS4Windows.Global;` による無修飾
- **分類**:
  - `(a)` 既存DIサービスに同一または同等の契約・実装が既に存在（単純・確実な置換）
  - `(b)` 既存DIサービスへの軽微なプロパティ／メソッド追加が必要（薄いシムの追加）
  - `(c)` ライフサイクル管理や複数責務の集約など、軽微な設計整理が必要
  - `除外` 真の定数（`const`）、純粋計算、または Phase6 明示的対象外（ViGEm クライアント直接依存）
- **経路（ホットパス区分）**:
  - `H`: 毎レポート実行される超高頻度ホットパス（毎秒250〜1000回）
  - `H条件`: 入力ポーリング経路上だが、初回・状態変化時・特定モード時のみ実行
  - `N`: 初期化・設定変更・デバイス接続・切断等の非ホットパス
  - `N†`: 通常は設定変更経路だが、プロファイル動的切替等の入力起点からも到達可能
  - `A`: 入力から起動される非同期ワーカー・マクロ処理

---

### 表1: 初期化・KBM・環境・サーバー系（ID: C2-01 〜 C2-27、計31参照）

| ID | 行番号・形式 | メソッド／経路 | 参照メンバ | 分類 | ホットパス | 移行先サービス・メンバ／方針 |
|---|---|---|---|---|---|---|
| C2-01 | 51 Q | 静的初期化 | `IsWin8OrGreater()` | (b) | N | `IEnvironmentService.IsWin8OrGreater()`（静的フィールド評価順序の整理を伴う） |
| C2-02 | 204 Q | コンストラクタ | `ProfileSettingsServiceInstance` | (a) | N | コンストラクタ注入された `_profileSettings` を直接利用（既存配線済み） |
| C2-03 | 242 Q | コンストラクタ | `DeviceOptions` | (b) | N | `IAppSettingsService.DeviceOptions`（`BackingStore.deviceOptions` 委譲） |
| C2-04 | 250 Q | コンストラクタ | `UDPServerSmoothingMincutoffChanged` | (b) | N | `IAppSettingsService.UDPServerSmoothingMincutoffChanged` イベント購読 |
| C2-05 | 251 Q | コンストラクタ | `UDPServerSmoothingBetaChanged` | (b) | N | `IAppSettingsService.UDPServerSmoothingBetaChanged` イベント購読 |
| C2-06 | 262 Q | `SystemEvents_DisplaySettingsChanged` | `PrepareAbsMonitorBounds` | (c) | N | 画面座標系管理（過渡期はシム経由ヘルパー化、将来 `IDisplayCoordinateService`） |
| C2-07 | 332 Q | `CreateOSCCallback` | `isInterpretingOscMonitoring()` | (b) | N/受信時 | `IAppSettingsService.InterpretingOscMonitoring` |
| C2-08 | 356 U | `CreateOSCCallback` | `isUsingOSCSender()` | (b) | N/受信時 | `IAppSettingsService.UseOscSender` |
| C2-09 | 478, 481 Q | `RefreshOutputKBMHandler` | `outputKBMHandler` (null判定・代入) | (c) | N | KBMハンドラ管理（`_virtualKBM` の内部状態制御またはファクトリ化） |
| C2-10 | 480 Q | `RefreshOutputKBMHandler` | `outputKBMHandler.Disconnect()` | (a) | N | `_virtualKBM.Disconnect()` |
| C2-11 | 484, 486 Q | `RefreshOutputKBMHandler` | `outputKBMMapping` (null判定・代入) | (a) | N | `_profileSettings.OutputKBMMapping = null` |
| C2-12 | 495 Q | `InitOutputKBMHandler` | `InitOutputKBMHandler(...)` | (c) | N | KBMハンドラ初期化（過渡期プライベートヘルパーまたはファクトリ） |
| C2-13 | 500 Q | `InitOutputKBMHandler` | `outputKBMHandler.Connect()` | (a) | N | `_virtualKBM.Connect()` |
| C2-14 | 507 Q | `InitOutputKBMHandler` | `outputKBMHandler` (代入) | (c) | N | KBMハンドラ置換制御 |
| C2-15 | 512 U, 518 Q | `InitOutputKBMHandler` | `outputKBMHandler.GetIdentifier()` | (a) | N | `_virtualKBM.GetIdentifier()` |
| C2-16 | 514 Q | `InitOutputKBMHandler` | `outputKBMHandler.Version = ...` | (c) | N | ハンドラ初期化時設定（※`IVirtualKBM.Version` は現在get専用のため配慮要） |
| C2-17 | 514 Q | `InitOutputKBMHandler` | `fakerInputVersion` | (b) | N | `IEnvironmentService.FakerInputVersion` |
| C2-18 | 518 Q | `InitOutputKBMHandler` | `InitOutputKBMMapping(...)` | (c) | N | マッピング初期化集約 |
| C2-19 | 519 Q | `InitOutputKBMHandler` | `outputKBMMapping.PopulateConstants()` | (a) | N | `_profileSettings.OutputKBMMapping.PopulateConstants()` |
| C2-20 | 520 Q | `InitOutputKBMHandler` | `outputKBMMapping.PopulateMappings()` | (a) | N | `_profileSettings.OutputKBMMapping.PopulateMappings()` |
| C2-21 | 663, 698 Q | `RequestElevation`, `CheckHidHide` | `exelocation` | (a) | N | `_pathService.ExecutablePath` |
| C2-22 | 687, 764, 792, 804, 858 Q | HidHide 判定・属性更新 | `hidHideInstalled` | (b) | N | `IEnvironmentService.HidHideInstalled` |
| C2-23 | 803 Q | `CheckAffected` | `GetInstanceIdFromDevicePath(...)` | (b) | N | `IEnvironmentService.GetInstanceIdFromDevicePath(...)` |
| C2-24 | 806 Q | `CheckAffected` | `CheckHidHideAffectedStatus(...)` | (b) | N | `IEnvironmentService.CheckHidHideAffectedStatus(...)` |
| C2-25 | 886, 1012, 1736 Q | UDP ポート取得 | `getUDPServerPortNum()` | (a) | N | `_appSettings.UdpServerPort` |
| C2-26 | 887, 1013, 1737 Q | UDP アドレス取得 | `getUDPServerListenAddress()` | (a) | N | `_appSettings.UdpServerListenAddress` |
| C2-27 | 929, 931, 945, 946 Q | OSC ポート・アドレス取得 | `getOSCServerPortNum()`, `getOSCSender*` | (b) | N | `_appSettings.OscServerPort`, `_appSettings.OscSenderAddress`, `_appSettings.OscSenderPort` |

---

### 表2: サービス起動・停止・プロファイル・出力状態系（ID: C2-28 〜 C2-49、計30参照）

| ID | 行番号・形式 | メソッド／経路 | 参照メンバ | 分類 | ホットパス | 移行先サービス・メンバ／方針 |
|---|---|---|---|---|---|---|
| C2-28 | 1416 Q | `PluginOutDev` | `OutContType[index]` | (a) | N† | `_profileSettings.OutContType[index]`（旧C2-01） |
| C2-29 | 1419, 2176, 2569 U | `PluginOutDev`, `PrepConn`, `SyncChange` | `getDInputOnly(index)` | (a) | N† | `_profileSettings.GetDInputOnly(index)`（永続設定） |
| C2-30 | 1424, 1567, 1574, 1604, 1904, 2197, 2561, 2608, 2670 U | プラグイン／アンプラグ／停止／同期 | `useDInputOnly[index]` | (a) | N† | `_profileSettings.UseDInputOnlyArray[index]`（実行時状態） |
| C2-31 | 1429, 1495, 1584, 1586, 2188, 2191, 2198, 2563, 2671 Q/U | スロット割当・解放・同期 | `activeOutDevType[index]` | (a) | N† | `_outputSlotService.ActiveOutDevType[index]` |
| C2-32 | 1623 Q | `Start` | `IsAdministrator()` | (a) | N | `_environmentService.IsAdministrator()` |
| C2-33 | 1624, 1630 Q | `Start` | `outputKBMHandler.GetIdentifier()`, `.GetFullDisplayName()` | (a) | N | `_virtualKBM.GetIdentifier()`, `_virtualKBM.GetFullDisplayName()` |
| C2-34 | 1633 U | `Start` | `getUseExclusiveMode()` | (a) | N | `_appSettings.UseExclusiveMode` |
| C2-35 | 1643, 1648, 1653 U | `Start` | `isUsingOSCServer()`, `isUsingOSCSender()`, `isUsingUDPServer()` | (a)/(b) | N | `_appSettings.UseOscServer`, `_appSettings.UseOscSender`, `_appSettings.UseUdpServer` |
| C2-36 | 1774, 1848, 1957 U | `Start`, `Stop` | `runHotPlug` (書込み) | (a) | N | `_appSettings.RunHotPlug = ...` |
| C2-37 | 1778 Q | `Start` | `ProcessPriority` | (b) | N | `_appSettings.ProcessPriority` |
| C2-38 | 1822 U | `CheckQuickCharge` | `getQuickCharge()` | (b) | N | `_appSettings.QuickCharge` |
| C2-39 | 1863 U | `Stop` | `DCBTatStop` | (b) | N | `_appSettings.DCBTatStop` |
| C2-40 | 1951, 1952 U | `Stop` | `outputKBMHandler.GetDisplayName()`, `.Disconnect()` | (a) | N | `_virtualKBM.GetDisplayName()`, `_virtualKBM.Disconnect()` |
| C2-41 | 2095 Q | `PrepareConnectedInput...` | `RefreshExtrasButtons(index, buttons)` | (b) | N | `_profileSettings.RefreshExtrasButtons(index, buttons)` |
| C2-42 | 2096 Q | `PrepareConnectedInput...` | `LoadControllerConfigs(device)` | (b) | N | `_profileXmlStore.LoadControllerConfigsForDevice(device)` |
| C2-43 | 2101, 2661 U | `PrepareConnectedInput...`, `On_DS4Removal` | `isUsingOSCSender()` | (b) | N | `_appSettings.UseOscSender` |
| C2-44 | 2116, 2123, 2141 Q | `PrepareConnectedInput...` | `IsFirstConnection(i)`, `MarkConnected(i)` | (b) | N | `_deviceStateService.IsFirstConnection(i)`, `_deviceStateService.MarkConnected(i)` |
| C2-45 | 2128, 2131, 2154, 2156 U | `PrepareConnectedInput...` | `containsLinkedProfile(s)`, `getLinkedProfile(s)` | (b) | N | `_profileRepository.ContainsLinkedProfile(s)`, `_profileRepository.GetLinkedProfile(s)` |
| C2-46 | 2137, 2146, 2151, 2157, 2162 Q/U | `PrepareConnectedInput...` | `OlderProfilePath[i]`, `SelectedProfile[i]`, `LinkedProfileUI[i]` | (a) | N | `_profileRepository.OlderProfilePath[i]`, `SelectedProfile[i]`, `LinkedProfileUI[i]` |
| C2-47 | 2168 Q | `PrepareConnectedInput...` | `ApplyProfileToSlot(i, ...)` | (b) | N | §3.2参照（循環依存回避ラッパーまたは `_profileApplicationService` 呼出し） |
| C2-48 | 2174, 3128 U | `PrepareConnectedInput...`, `LagFlashWarning` | `getMainColor(index)` | (a) | N/H条件 | `_profileSettings.GetMainColor(index)` |
| C2-49 | 2257, 2260, 2269, 2275 Q | UDP 平滑化設定 | `UDPServerSmoothingMincutoff`, `UDPServerSmoothingBeta` | (b) | N | `_appSettings.UDPServerSmoothingMincutoff`, `_appSettings.UDPServerSmoothingBeta` |

---

### 表3: 入力ポーリング・ホットパス・出力フィードバック系（ID: C2-50 〜 C2-66、計25参照）

| ID | 行番号・形式 | メソッド／経路 | 参照メンバ | 分類 | ホットパス | 移行先サービス・メンバ／方針 |
|---|---|---|---|---|---|---|
| C2-50 | 1792 Q | `PrepareDevUDPMotion` コールバック | `IsUsingUDPServerSmoothing` | (b) | **H** | `_appSettings.UseUdpServerSmoothing`（Report毎に評価されるためキャッシュ考慮） |
| C2-51 | 2281, 2282, 2285, 2286, 2288, 2302, 2309, 2325 U | `CheckProfileOptions` | `getEnableOutputDataToDS4`, `getIdleDisconnectTimeout`, `getBTPollRate`, `getTrackballFriction`, `getRumbleAutostopTime`, `DualSense*` | (a) | N† | `_profileSettings.GetEnableOutputDataToDS4(i)`, `GetIdleDisconnectTimeout(i)`, `GetBTPollRate(i)`, `GetTrackballFriction(i)`, `GetRumbleAutostopTime(i)`, `DualSense*` |
| C2-52 | 2336 U | `CheckLauchProfileOption` | `LaunchProgram[index]` | (a) | N† | `_profileSettings.LaunchProgram[index]` |
| C2-53 | 2385, 2408, 2417, 2433, 2442 U | フック・設定変更イベント | `WheelSmoothInfo`, `L2OutputSettings`, `R2OutputSettings` | (a) | N | `_profileSettings.WheelSmoothInfo[i]`, `L2OutputSettings[i]`, `R2OutputSettings[i]` |
| C2-54 | 2540 U | `On_SerialChange` | `OnDeviceSerialChange` | (b) | N | `_deviceStateService.OnDeviceSerialChange(sender, i, serial)` |
| C2-55 | 2701 U | `On_Report` | `getFlashWhenLateAt()` | (b) | **H** | `_appSettings.FlashWhenLateAt` |
| C2-56 | 2749, 2753 Q/U | `On_Report`（初回Report時） | `appdatapath`, `ProfilePath[i]`, `store.EmitMissingActionLogsForDevice` | (a) | H条件 | `_pathService.AppDataPath`, `_profileRepository.ProfilePath[i]`, `_profileRepository.EmitMissingActionLogsForDevice(i, false)` |
| C2-57 | 2762, 2764 Q/U | `On_Report` | `UseIconChoice`, `InvokeBatteryChanged` | (a)/(b) | **H** / H条件 | `_appearanceSettings.UseIconChoice`, `_appearanceSettings.InvokeBatteryChanged(byte)` |
| C2-58 | 2775 Q | `On_Report`（非Primary結合時） | `GetGyroOutMode(JointDeviceSlotNumber)` | (a) | H条件 | `_profileSettings.GetGyroOutMode(...)`（旧C2-02） |
| C2-59 | 2780 U | `On_Report` | `outputKBMHandler.Sync()` | (a) | **H** | `_virtualKBM.Sync()` |
| C2-60 | 2788, 2846 U | `On_Report` | `useDInputOnly[index]` | (a) | **H** | `_profileSettings.UseDInputOnlyArray[index]` |
| C2-61 | 2805, 2851 Q/U | `On_Report` | `getEnableTouchToggle(i)`, `GetOutputDS4TriggerMode(i)` | (a) | **H** | `_profileSettings.GetEnableTouchToggle(i)`, `_profileSettings.OutputDS4TriggerMode[i]` |
| C2-62 | 2815, 2816 U | `On_Report` | `containsCustomAction(i)`, `containsCustomExtras(i)`, `getProfileActionCount(i)` | (b) | **H** | `_profileSettings.ContainsCustomAction(i)`, `_profileSettings.ContainsCustomExtras(i)`, `_profileActionProvider.GetProfileActionCount(i)` |
| C2-63 | 2821, 2837, 2849 U | `On_Report` | `isUsingOSCSender()`, `isUsingOSCServer()`, `activeOutDevType[i]` | (a)/(b) | **H** | `_appSettings.UseOscSender`, `_appSettings.UseOscServer`, `_outputSlotService.ActiveOutDevType[i]` |
| C2-64 | 2890 Q | `On_Report`（useDInputOnly時） | `GetSASteeringWheelEmulationAxis(index)` | (a) | H条件 | `_profileSettings.GetSASteeringWheelEmulationAxis(index)`（旧C2-03） |
| C2-65 | 3114 U | `LagFlashWarning` | `getFlashWhenLate()` | (b) | H条件 | `_appSettings.FlashWhenLate` |
| C2-66 | 3212, 3214, 3216, 3223, 3237, 3301 U | タッチトグル・ランブルブースト | `IsUsingTouchpadForControls`, `GetTouchActive`, `TouchActive[i]`, `getRumbleBoost` | (a) | **H** / N† | `_profileSettings.TouchOutMode[i] == TouchpadOutMode.Controls`, `_profileSettings.TouchpadActiveArray[i]`, `_profileSettings.GetRumbleBoost(i)` |

---

### 表4: 明示的除外項目（ID: C2-EX01 〜 C2-EX07、計8参照）

| ID | 行番号 | 参照メンバ | 除外理由（根拠） |
|---|---|---|---|
| C2-EX01 | 47 Q | `Global.MAX_DS4_CONTROLLER_COUNT` | 真の定数（`const int`、4層モデル共通値） |
| C2-EX02 | 49, 51 Q | `Global.OLD_XINPUT_CONTROLLER_COUNT` | 真の定数（`const int`） |
| C2-EX03 | 2462 Q | `Global.TEST_PROFILE_INDEX` | 真の定数（`const int`） |
| C2-EX04 | 1055 Q | `Global.RefreshViGEmBusInfo()` | Phase6-Plan.md §0.3.1 明示的除外（ViGEm クライアント直接依存） |
| C2-EX05 | 1056, 1760 Q | `Global.IsRunningSupportedViGEmBus()` | Phase6-Plan.md §0.3.1 明示的除外（ViGEm クライアント直接依存） |
| C2-EX06 | 1631, 1762 Q | `Global.vigembusVersion` | Phase6-Plan.md §0.3.1 明示的除外（ViGEm クライアント直接依存） |
| C2-EX07 | 1756 U | `vigemInstalled` | Phase6-Plan.md §0.3.1 明示的除外（ViGEm クライアント直接依存） |

---

## 3. アーキテクチャ設計・依存性注入（Pure DI）

### 3.1 コンストラクタ注入の拡張設計
`ControlService` は現在、`ArgumentParser`, `IDs4DeviceRegistry`, `IProfileSettingsService` の3つのみをコンストラクタで受け取っている。  
Pure DI の原則（`copilot-instructions.md` §3.1）に従い、サービスロケータ（`AppHost.Services`）を内部に持ち込まず、必要な依存をすべてコンストラクタで明示的に受け取る構造へ拡張する。

```csharp
// 拡張後の ControlService コンストラクタ設計（互換フォールバック付き）
public ControlService(
    DS4WinWPF.ArgumentParser cmdParser,
    IDs4DeviceRegistry deviceRegistry,
    DI.IProfileSettingsService profileSettings = null,
    DI.IAppSettingsService appSettings = null,
    DI.IEnvironmentService environmentService = null,
    DI.IPathService pathService = null,
    DI.IOutputSlotService outputSlotService = null,
    DI.IVirtualKBM virtualKBM = null,
    DI.IProfileRepository profileRepository = null,
    DI.IDeviceStateService deviceStateService = null,
    DI.IAppearanceSettingsService appearanceSettings = null,
    DI.IProfileXmlStore profileXmlStore = null,
    DI.IProfileActionProvider profileActionProvider = null)
{
    this.cmdParser = cmdParser;
    this._deviceRegistry = deviceRegistry;
    this._profileSettings = profileSettings ?? Global.ProfileSettingsServiceInstance;
    this._appSettings = appSettings ?? AppHost.GetService<DI.IAppSettingsService>();
    this._environmentService = environmentService ?? AppHost.GetService<DI.IEnvironmentService>();
    this._pathService = pathService ?? AppHost.GetService<DI.IPathService>();
    this._outputSlotService = outputSlotService ?? AppHost.GetService<DI.IOutputSlotService>();
    this._virtualKBM = virtualKBM ?? AppHost.GetService<DI.IVirtualKBM>();
    this._profileRepository = profileRepository ?? AppHost.GetService<DI.IProfileRepository>();
    this._deviceStateService = deviceStateService ?? AppHost.GetService<DI.IDeviceStateService>();
    this._appearanceSettings = appearanceSettings ?? AppHost.GetService<DI.IAppearanceSettingsService>();
    this._profileXmlStore = profileXmlStore ?? AppHost.GetService<DI.IProfileXmlStore>();
    this._profileActionProvider = profileActionProvider ?? AppHost.GetService<DI.IProfileActionProvider>();
    ...
}
```
※ 過渡期ルールとして、単体テストや一部の非DI起動経路との下位互換性を担保するため、省略可能引数（`= null`）および `AppHost.GetService` / `Global` へのフォールバックを維持する。

### 3.2 循環依存の回避設計（`ProfileApplicationService` ⇄ `ControlService`）
- **現状の課題**: `ProfileApplicationService` のコンストラクタは `ControlService` を受け取る構成になっている。もし `ControlService` がコンストラクタで `IProfileApplicationService` を受け取ると、DI コンテナ解決時および実行時において**双方向の循環依存**が発生する。
- **解決方針**:
  - `ControlService.cs:2168` の `Global.ApplyProfileToSlot`（ID: C2-47）の呼び出しにおいて、`ControlService` に `IProfileApplicationService` を注入しない。
  - 代わりに、プロファイル切り替え要求イベント（`public event EventHandler<ProfileSlotApplyEventArgs> ProfileApplyRequested;`）を発行して上位（`MainWindow` やコーディネーター）に委譲するか、または既存の `_profileRepository.LoadProfileToSlot` などの低レベル適用APIを呼び出す構造とする。
  - これにより、`ControlService`（第1/2層）から `ProfileApplicationService`（第4層サービス）への上向き依存を排除し、4層アーキテクチャのレイヤー原則（上位 → 下位）を厳格に順守する。

---

## 4. 分類(b)・分類(c) のサービス拡張・設計方針

### 4.1 分類(b) 既存サービスへの軽微拡張一覧
各DIサービスに、以下の薄いシム（`BackingStore` / `Global` 委譲）を追加する。独自フィールドは一切保持せず、SSOT（信頼できる唯一の情報源）を徹底する。

1. **`IAppSettingsService` / `AppSettingsService.cs`**:
   - `bool FlashWhenLate { get; set; }` → `Global.getFlashWhenLate()` / `Global.setFlashWhenLate(value)`
   - `int FlashWhenLateAt { get; set; }` → `Global.getFlashWhenLateAt()` / `Global.setFlashWhenLateAt(value)`
   - `bool QuickCharge { get; set; }` → `Global.getQuickCharge()` / `Global.setQuickCharge(value)`
   - `bool DCBTatStop { get; set; }` → `Global.DCBTatStop`
   - `ProcessPriorityClass ProcessPriority { get; set; }` → `Global.ProcessPriority`
   - `int OscServerPort { get; set; }` → `Global.getOSCServerPortNum()`
   - `string OscSenderAddress { get; set; }` → `Global.getOSCSenderAddress()`
   - `int OscSenderPort { get; set; }` → `Global.getOSCSenderPortNum()`
   - `bool UseOscServer { get; set; }` → `Global.isUsingOSCServer()`
   - `bool UseOscSender { get; set; }` → `Global.isUsingOSCSender()`
   - `bool InterpretingOscMonitoring { get; set; }` → `Global.isInterpretingOscMonitoring()`
   - `bool UseUdpServerSmoothing { get; set; }` → `Global.IsUsingUDPServerSmoothing`
   - `float UDPServerSmoothingMincutoff { get; set; }` → `Global.UDPServerSmoothingMincutoff`
   - `float UDPServerSmoothingBeta { get; set; }` → `Global.UDPServerSmoothingBeta`
   - `event EventHandler UDPServerSmoothingMincutoffChanged;`
   - `event EventHandler UDPServerSmoothingBetaChanged;`
   - `ControlServiceDeviceOptions DeviceOptions { get; }` → `Global.DeviceOptions`

2. **`IEnvironmentService` / `EnvironmentService.cs`**:
   - `bool IsWin8OrGreater()` → `Global.IsWin8OrGreater()`
   - `bool HidHideInstalled { get; }` → `Global.hidHideInstalled`
   - `string GetInstanceIdFromDevicePath(string devicePath)` → `Global.GetInstanceIdFromDevicePath(devicePath)`
   - `bool CheckHidHideAffectedStatus(string instanceId, HashSet<string> affected, HashSet<string> exempted, bool forced)` → `Global.CheckHidHideAffectedStatus(...)`
   - `string FakerInputVersion { get; }` → `Global.fakerInputVersion`

3. **`IDeviceStateService` / `DeviceStateService.cs`**:
   - `bool IsFirstConnection(int deviceIndex)` → `Global.IsFirstConnection(deviceIndex)`
   - `void MarkConnected(int deviceIndex)` → `Global.MarkConnected(deviceIndex)`
   - `void OnDeviceSerialChange(DS4Device sender, int index, string serial)` → `Global.OnDeviceSerialChange(sender, index, serial)`

4. **`IProfileRepository` / `ProfileRepository.cs`**:
   - `bool ContainsLinkedProfile(string serial)` → `Global.containsLinkedProfile(serial)`
   - `string GetLinkedProfile(string serial)` → `Global.getLinkedProfile(serial)`

5. **`IProfileSettingsService` / `ProfileSettingsService.cs`**:
   - `void RefreshExtrasButtons(int deviceIndex, List<DS4Controls> buttons)` → `Global.RefreshExtrasButtons(deviceIndex, buttons)`
   - `bool ContainsCustomAction(int deviceIndex)` → `Global.containsCustomAction(deviceIndex)`
   - `bool ContainsCustomExtras(int deviceIndex)` → `Global.containsCustomExtras(deviceIndex)`

6. **`IProfileXmlStore` / `ProfileXmlStore.cs`**:
   - `void LoadControllerConfigsForDevice(DS4Device device)` → `Global.LoadControllerConfigs(device)`

7. **`IAppearanceSettingsService` / `AppearanceSettingsService.cs`**:
   - `void InvokeBatteryChanged(byte batteryLevel)` → `Global.InvokeBatteryChanged(batteryLevel)`

8. **`IProfileActionProvider` / `ProfileActionProvider.cs`**:
   - `int GetProfileActionCount(int deviceIndex)` → `Global.getProfileActionCount(deviceIndex)`

### 4.2 分類(c) ライフサイクル・座標変換の整理
- **KBM ハンドラ初期化（ID: C2-09, C2-12, C2-14, C2-16, C2-18）**:
  `RefreshOutputKBMHandler` および `InitOutputKBMHandler` は、ハンドラの生成・破棄・フォールバック交換・バージョン設定・マッピング初期化が密結合している。
  本Step2では、既存の挙動（Haltとの整合、フォールバックの動作順序）を100%維持するため、`ControlService` 内部のプライベートメソッドとしてハンドラ交換手順をカプセル化しつつ、`Global.outputKBMHandler` への直接代入を `_virtualKBM` アダプタ経由の更新に置き換える。
- **画面解像度変更時の座標系更新（ID: C2-06）**:
  `SystemEvents_DisplaySettingsChanged` から呼ばれる `Global.PrepareAbsMonitorBounds(string.Empty)` は、マウス絶対座標計算のためのWin32モニター列挙処理である。過渡期は薄いシムメソッドを経由させ、Phase6-Step4（Mouse系移行）と連動して最終的なサービス抽象化を行う。

---

## 5. ホットパス性能維持とアーキテクチャ・ガードレール

### 5.1 ホットパス（毎秒250〜1000回）の性能保護原則
1. **ゼロアロケーション（Zero Allocation）**:
   `On_Report`、`LagFlashWarning`、`CheckForTouchToggle` 等の高頻度実行経路上では、GC プレッシャーを避けるため、配列の `.Clone()`、`new` によるオブジェクト生成、および LINQ の使用を厳禁とする。
2. **BackingStore 直列配列参照の維持**:
   `_profileSettings.UseDInputOnlyArray[index]` や `_profileSettings.LSModInfo[index]` などの配列アクセスは、内部キャッシュを経由せず、BackingStore の固定配列を直接参照する getter を介して行われる。余分なディクショナリ検索や動的解決を挟まない。
3. **ホットパス内のログ出力厳禁**:
   高頻度経路上に常時出力される `[DI]` 追跡ログ、`Debug.WriteLine`、例外スローを持ち込まない。
4. **設定値の都度サービス解決の排除**:
   コンストラクタで受け取った readonly フィールド（`_profileSettings`, `_virtualKBM` 等）を直接参照し、`AppHost.GetService<T>()` をホットパス内で絶対に呼ばない。

### 5.2 アーキテクチャ・ガードレールの順守（Phase5 §5 継承）
1. **プロファイル適用時の Halt 停止保証**:
   プロファイル適用や出力デバイス変更を伴う操作では、入力ポーリングスレッドの停止（Halt）と排他制御を確実に維持する。
2. **スレッド直列化**:
   `eventDispatchThread` および `eventDispatcher` によるイベント直列化キューの順序を乱さない。
3. **切断時クリーンアップ**:
   コントローラー切断時（`On_DS4Removal`）およびサービス停止時（`Stop`, `ShutDown`）における、仮想デバイスアンプラグおよび状態リセットを完全に維持する。

---

## 6. PR分割計画とマイクロステップ

変更規模（実移行対象66箇所）と安全性を考慮し、以下の**6つのサブPR（マイクロステップ）**に分割して段階的に実装を進める。各PRごとに `dotnet test`（Actions / Standalone）を実行し、全件グリーンを確認してから次へ進む。

```text
【Phase6-Step2 マイクロステップ構成】
├─ Step2-0: 着手前提検証・ベースライン記録（実装なし）
├─ Step2-1 (PR-1): 非ホットパスの環境・パス・基本設定（ID: C2-01〜05, 21〜27, 32〜40）[22参照]
├─ Step2-2 (PR-2): 出力スロット・プロファイル状態・接続イベント（ID: C2-28〜31, 41〜49）[20参照]
├─ Step2-3 (PR-3): KBMハンドラ初期化・ライフサイクル整理（ID: C2-09〜20）[12参照]
├─ Step2-4 (PR-4): 入力処理ホットパス（条件付き・低頻度分岐）（ID: C2-51〜54, 56, 58, 64〜65）[14参照]
├─ Step2-5 (PR-5): 入力処理ホットパス（毎レポート最頻度経路）（ID: C2-50, 55, 57, 59〜63, 66）[18参照]
└─ Step2-6 (PR-6): using static DS4Windows.Global; 削除・未修飾参照ゼロ検証・最終クリーンアップ
```

---

### Step2-0: 着手前提検証・ベースライン記録（実装なし）
1. 作業ツリーの状態、HEADコミットハッシュ（`59d46db47...` / `62e6938...`）、未コミット差分ゼロを確認する。
2. ソリューション全体のビルド（`dotnet build -c Release`）および既存テスト（`dotnet test`）を実行し、ベースライン結果（全テストパス、警告ゼロ）を記録する。

---

### Step2-1 (PR-1): 非ホットパスの環境・パス・基本設定
- **対象**: ID: C2-01〜C2-05, C2-21〜C2-27, C2-32〜C2-40（初期化、管理者権限、HidHide、UDP/OSCポート設定、Start/Stop）
- **作業内容**:
  1. `IAppSettingsService` および `IEnvironmentService` に必要なプロパティ・メソッドを追加（§4.1）。
  2. `ControlService` コンストラクタに `IAppSettingsService`, `IEnvironmentService`, `IPathService` を追加注入。
  3. 対象箇所の `Global.*` を注入インスタンス参照へ置換。
- **検証**:
  - 新規単体テスト: 追加したシムプロパティが `Global` / `BackingStore` と同期していることを検証。
  - 回帰テスト: `dotnet test` 全件合格を確認。

---

### Step2-2 (PR-2): 出力スロット・プロファイル状態・接続イベント
- **対象**: ID: C2-28〜C2-31, C2-41〜C2-49（`PluginOutDev`, `useDInputOnly`, `activeOutDevType`, `PrepareConnectedInputControllerSettingEvents`）
- **作業内容**:
  1. `IDeviceStateService`, `IProfileRepository`, `IProfileXmlStore`, `IProfileSettingsService` に必要なシムを追加（§4.1）。
  2. `ControlService` コンストラクタに `IOutputSlotService`, `IProfileRepository`, `IDeviceStateService`, `IProfileXmlStore` を追加注入。
  3. C2-28（旧C2-01: `Global.OutContType` → `_profileSettings.OutContType`）を含む対象箇所を置換。
  4. C2-47（`ApplyProfileToSlot`）の循環依存を回避する設計を適用。
- **検証**:
  - 単体テスト: スロット別の `OutContType`, `useDInputOnly`, `activeOutDevType` の読み書き分離を検証。
  - 回帰テスト: `dotnet test` 全件合格を確認。

---

### Step2-3 (PR-3): KBMハンドラ初期化・ライフサイクル整理
- **対象**: ID: C2-09〜C2-20（`RefreshOutputKBMHandler`, `InitOutputKBMHandler`）
- **作業内容**:
  1. `ControlService` コンストラクタに `IVirtualKBM` を追加注入。
  2. `Global.outputKBMHandler` の接続・切断・識別子取得を `_virtualKBM` 経由へ置換。
  3. `Global.outputKBMMapping` を `_profileSettings.OutputKBMMapping` へ置換。
  4. ハンドラ交換時の例外保護・フォールバック切替順序を維持。
- **検証**:
  - 回帰テスト: `VirtualKBMTests.cs` および `dotnet test` 全件合格を確認。

---

### Step2-4 (PR-4): 入力処理ホットパス（条件付き・低頻度分岐）
- **対象**: ID: C2-51〜C2-54, C2-56, C2-58, C2-64〜C2-65（初回Report時ログ、結合ジャイロ、ステアリング軸、オプションチェック）
- **作業内容**:
  1. C2-58（旧C2-02: `Global.GetGyroOutMode` → `_profileSettings.GetGyroOutMode`）を置換。
  2. C2-64（旧C2-03: `Global.GetSASteeringWheelEmulationAxis` → `_profileSettings.GetSASteeringWheelEmulationAxis`）を置換。
  3. 初回Report時のプロファイル欠落ログ出力（`EmitMissingActionLogsForDevice`）を置換。
- **検証**:
  - 単体テスト: 結合スロットでのジャイロモード取得、ステアリング軸取得の独立性を検証。
  - 回帰テスト: `dotnet test` 全件合格を確認。

---

### Step2-5 (PR-5): 入力処理ホットパス（毎レポート最頻度経路）
- **対象**: ID: C2-50, C2-55, C2-57, C2-59〜C2-63, C2-66（`On_Report` 最頻度経路、タッチトグル、KBM Sync、遅延警告）
- **作業内容**:
  1. `IAppearanceSettingsService`, `IProfileActionProvider` をコンストラクタ追加注入。
  2. `On_Report` 内の残存 `Global` 参照（`useDInputOnly`, `Sync()`, `TouchActive`, `containsCustomAction` 等）を一挙に置換。
  3. 新規オブジェクト割り当て・クローンが一切発生していないことをコードレビューで厳格確認。
- **検証**:
  - ベンチマーク: `On_Report` 処理時間のプロファイル計測（置換前後の処理時間比較）。
  - 回帰テスト: `dotnet test` 全件合格を確認。

---

### Step2-6 (PR-6): `using static DS4Windows.Global;` 削除・最終クリーンアップ
- **対象**: `ControlService.cs` 冒頭の `using static DS4Windows.Global;` ディレクティブ
- **作業内容**:
  1. `using static DS4Windows.Global;` を削除する。
  2. コンパイルを通し、意図しない無修飾 Global 参照が残存していないことを Roslyn コンパイラで完全に検出・解消する。
  3. 表4の明示的除外（`MAX_DS4_CONTROLLER_COUNT` 等の const、ViGEmバックエンド）のみが `Global.` 修飾で残っていることを確認する。
  4. `ServiceRegistration.cs` の `ControlService` 解決ファクトリを更新し、新規注入サービスを登録。
- **検証**:
  - 静的解析: `ControlService.cs` 内の `Global.` 出現箇所が除外リスト（表4の8件）と完全一致することを確認。
  - 回帰テスト: 全テストプロジェクトの完全ビルド・実行でグリーンを確認。

---

## 7. テスト・性能・実機検証計画

### 7.1 自動単体テスト計画
`DS4WindowsTests` に以下のテストケースを追加する（1ファイル1型を厳守）：
1. **`ControlServiceDiWiringTests.cs`**:
   - `ControlService` が全依存サービスを受け取って正常にインスタンス化されること。
   - `ServiceRegistration` 経由の DI 解決で `ControlService` が Singleton として正しく解決されること。
2. **`ControlServiceShimEquivalenceTests.cs`**:
   - 各サービスに追加された新規シム（`FlashWhenLate`, `QuickCharge`, `IsFirstConnection` 等）が、`Global` / `BackingStore` と完全に値を共有していること。
   - スロットインデックス（0〜3）の分離が正しく機能していること。
3. **`ControlServiceHotPathAllocationTests.cs`**:
   - `On_Report` で呼び出される `_profileSettings` の各 getter がヒープ割り当て（GC Alloc）を行わないことの検証。

### 7.2 ホットパス性能比較（マイクロベンチマーク）
- **測定対象**: `On_Report` の1回あたりの平均処理時間（μs）および p95/p99 レイテンシ。
- **測定条件**:
  - Release x64 ビルド。
  - 同一PC・同一コントローラー（有線USB接続、1000Hz ポーリング）。
  - ウォームアップ後、同一操作シナリオ（左右スティック全開旋回＋ボタン連打）で 100,000 レポート分の処理時間を計測。
- **合否判定基準**:
  - 置換前後の平均処理時間差が ±5% 以内（統計的有意差なし）であること。
  - レポート処理中の Gen0 GC 発生回数が 0 回（完全ゼロアロケーション）であること。

### 7.3 実機動作検証項目（Phase6-Step11連携）
1. **コントローラー接続・切断・再接続**:
   - DS4 / DualSense / Vader 4 Pro コントローラーを有線および Bluetooth で接続し、正常に認識・仮想デバイスプラグインされること。
2. **プロファイル動的切替**:
   - プロファイル切替時に出力デバイス種別（Xbox 360 ⇄ DS4）が正しく反映され、入力遅延・フリーズが発生しないこと。
3. **タッチパッド・ジャイロ・ステアリング操作**:
   - タッチパッドによるマウス操作・トグル切り替えが正常に動作すること。
   - ジャイロによるマウス操作およびステアリングホイールエミュレーションが滑らかに動作すること。

---

## 8. ロールバック方針

万が一、特定のサブPRで入力遅延の悪化、デッドロック、または既存機能の破損が確認された場合は、以下の手順で直ちにロールバックを行う：
1. **サブPR単位の即時差し戻し**: 影響を受けたPRのみを `git revert` し、直前の健全な状態に復旧する。
2. **シムの温存**: 既存の静的メンバ（`Global.*`）はすべて温存されているため、読み取り式を旧参照に戻すだけで完全に元の動作に戻すことが可能。
3. **原因の単離と再設計**: 性能劣化の原因（仮想呼び出しオーバーヘッドか、意図せぬアロケーションか）をベンチマークログから特定し、計画書を補正した上で再適用する。

---

## 9. 完了判定チェックリスト

- [ ] 表1〜表3に定義された全66箇所の実移行対象が、DIサービス経由の呼び出しへ置換されていること。
- [ ] 表4に定義された明示的除外項目（const定数3件、ViGEmバックエンド5件、計8参照）以外の `Global.` 直接参照が `ControlService.cs` から完全に根絶されていること。
- [ ] `ControlService.cs` 冒頭の `using static DS4Windows.Global;` ディレクティブが安全に削除されていること。
- [ ] コンストラクタ注入において、`ProfileApplicationService` との循環依存が発生していないこと。
- [ ] 追加されたすべての新規DIシムメンバが、正本（`BackingStore` / `Global`）と連動する薄い委譲として実装されていること。
- [ ] ホットパスにおいて、ゼロアロケーション（GC Alloc = 0）および遅延劣化なしがベンチマークで確認されていること。
- [ ] `dotnet build -c Release` で警告・エラーが0件であること。
- [ ] `DS4WindowsTests` および `StandaloneTests` の全自動テストが100%成功を維持していること。
- [ ] 実機コントローラーによる接続・切断・入力追従・プロファイル切替が正常に動作すること。