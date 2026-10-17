# Step0 Globalメンバー・呼び出し元インベントリ

## 調査結果

- 対象: `DS4Windows/DS4Control/ScpUtil.cs` 内 `public class Global`
- 実測: 442件（Globalクラス範囲内のpublic static宣言行）
- 計画書の推定469件との差分: -27件
- 参考: `ScpUtil.cs`全体の`public static`宣言は469件であり、Globalクラス外の27件を含む。したがって、Globalの実測値は442件である。
- 分類は宣言名と周辺責務に基づく実務上の責務カテゴリ。分類境界が複数にまたがるものは主たる利用責務で分類し、「その他フラグ・状態」は後続Stepの確認対象とする。

## 分類別件数

| 分類 | 件数 |
|---|---:|
| SpecialAction管理 | 26 |
| その他フラグ・状態 | 125 |
| デバイス・コントローラ状態 | 29 |
| パス・環境・UI・ログ | 19 |
| プロファイル・入力設定 | 174 |
| モニタ・座標変換 | 4 |
| ユーティリティ計算 | 18 |
| 出力・KBM・OSC/UDP | 47 |

## 全メンバー一覧

| 行 | メンバー | 分類 | 宣言 |
|---:|---|---|---|
| 544 | `loggedInvalidActions` | SpecialAction管理 | public static HashSet<string> loggedInvalidActions = new HashSet<string>(); |
| 546 | `ProfileEditorLeftWidth` | プロファイル・入力設定 | public static int ProfileEditorLeftWidth |
| 551 | `ProfileEditorRightWidth` | プロファイル・入力設定 | public static int ProfileEditorRightWidth |
| 556 | `SpecialActionNameColWidth` | SpecialAction管理 | public static int SpecialActionNameColWidth |
| 561 | `SpecialActionTriggerColWidth` | SpecialAction管理 | public static int SpecialActionTriggerColWidth |
| 566 | `SpecialActionDetailColWidth` | SpecialAction管理 | public static int SpecialActionDetailColWidth |
| 574 | `ControllerIndexColWidth` | デバイス・コントローラ状態 | public static int ControllerIndexColWidth |
| 579 | `ControllerIdColWidth` | デバイス・コントローラ状態 | public static int ControllerIdColWidth |
| 584 | `ControllerStatusColWidth` | デバイス・コントローラ状態 | public static int ControllerStatusColWidth |
| 589 | `ControllerExclusiveColWidth` | デバイス・コントローラ状態 | public static int ControllerExclusiveColWidth |
| 594 | `ControllerBatteryColWidth` | デバイス・コントローラ状態 | public static int ControllerBatteryColWidth |
| 599 | `ControllerSelectProfileColWidth` | プロファイル・入力設定 | public static int ControllerSelectProfileColWidth |
| 604 | `ControllerEditColWidth` | デバイス・コントローラ状態 | public static int ControllerEditColWidth |
| 610 | `ControllerLinkedProfileColWidth` | プロファイル・入力設定 | public static int ControllerLinkedProfileColWidth |
| 616 | `ControllerLinkProfIdColWidth` | プロファイル・入力設定 | public static int ControllerLinkProfIdColWidth |
| 622 | `LogMaxArchiveFiles` | パス・環境・UI・ログ | public static int LogMaxArchiveFiles |
| 628 | `LogMinLevel` | パス・環境・UI・ログ | public static string LogMinLevel |
| 633 | `ControllerCustomColorColWidth` | デバイス・コントローラ状態 | public static int ControllerCustomColorColWidth |
| 645 | `configFileDecimalCulture` | プロファイル・入力設定 | public static CultureInfo configFileDecimalCulture = new CultureInfo("en-US"); // Loading and Saving decimal values in configuration files should always use en-US decimal format (ie. dot char as decimal separator char, not comma char) |
| 648 | `store` | ユーティリティ計算 | public static BackingStore store => m_Config; |
| 653 | `exelocation` | パス・環境・UI・ログ | public static string exelocation = new Func<string>(() => |
| 669 | `exedirpath` | パス・環境・UI・ログ | public static string exedirpath = Directory.GetParent(exelocation).FullName; |
| 670 | `exeFileName` | パス・環境・UI・ログ | public static string exeFileName = Path.GetFileName(exelocation); |
| 671 | `fileVersion` | パス・環境・UI・ログ | public static FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(exelocation); |
| 672 | `exeversion` | パス・環境・UI・ログ | public static string exeversion = fileVersion.FileVersion; |
| 673 | `exeversionLong` | パス・環境・UI・ログ | public static ulong exeversionLong = (ulong)fileVersion.ProductMajorPart << 48 \| |
| 675 | `fullExeVersionLong` | パス・環境・UI・ログ | public static ulong fullExeVersionLong = exeversionLong \| (ushort)fileVersion.ProductPrivatePart; |
| 676 | `IsWin8OrGreater` | その他フラグ・状態 | public static bool IsWin8OrGreater() |
| 692 | `IsWin10OrGreater` | その他フラグ・状態 | public static bool IsWin10OrGreater() |
| 703 | `appdatapath` | パス・環境・UI・ログ | public static string appdatapath; |
| 704 | `firstRun` | その他フラグ・状態 | public static bool firstRun = false; |
| 705 | `multisavespots` | その他フラグ・状態 | public static bool multisavespots = false; |
| 706 | `appDataPpath` | パス・環境・UI・ログ | public static string appDataPpath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\DS4Windows"; |
| 707 | `localAppDataPpath` | パス・環境・UI・ログ | public static string localAppDataPpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DS4Windows"); |
| 708 | `runHotPlug` | その他フラグ・状態 | public static bool runHotPlug = false; |
| 709 | `tempprofilename` | プロファイル・入力設定 | public static string[] tempprofilename = new string[TEST_PROFILE_ITEM_COUNT] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty }; |
| 710 | `useTempProfile` | プロファイル・入力設定 | public static bool[] useTempProfile = new bool[TEST_PROFILE_ITEM_COUNT] { false, false, false, false, false, false, false, false, false }; |
| 711 | `tempprofileDistance` | プロファイル・入力設定 | public static bool[] tempprofileDistance = new bool[TEST_PROFILE_ITEM_COUNT] { false, false, false, false, false, false, false, false, false }; |
| 712 | `useDInputOnly` | ユーティリティ計算 | public static bool[] useDInputOnly = new bool[TEST_PROFILE_ITEM_COUNT] { true, true, true, true, true, true, true, true, true }; |
| 713 | `linkedProfileCheck` | プロファイル・入力設定 | public static bool[] linkedProfileCheck = new bool[MAX_DS4_CONTROLLER_COUNT] { false, false, false, false, false, false, false, false }; |
| 714 | `touchpadActive` | プロファイル・入力設定 | public static bool[] touchpadActive = new bool[TEST_PROFILE_ITEM_COUNT] { true, true, true, true, true, true, true, true, true }; |
| 717 | `IsFirstConnection` | その他フラグ・状態 | public static bool IsFirstConnection(int index) |
| 722 | `MarkConnected` | その他フラグ・状態 | public static void MarkConnected(int index) |
| 727 | `ResetConnectionFlags` | その他フラグ・状態 | public static void ResetConnectionFlags() |
| 740 | `outDevTypeTemp` | デバイス・コントローラ状態 | public static OutContType[] outDevTypeTemp = new OutContType[TEST_PROFILE_ITEM_COUNT] { DS4Windows.OutContType.X360, DS4Windows.OutContType.X360, |
| 746 | `activeOutDevType` | デバイス・コントローラ状態 | public static OutContType[] activeOutDevType = new OutContType[TEST_PROFILE_ITEM_COUNT] { DS4Windows.OutContType.None, DS4Windows.OutContType.None, |
| 757 | `vigemInstalled` | 出力・KBM・OSC/UDP | public static bool vigemInstalled = false; |
| 759 | `vigembusVersion` | 出力・KBM・OSC/UDP | public static string vigembusVersion = BLANK_VIGEMBUS_VERSION; |
| 760 | `vigemBusVersionInfo` | 出力・KBM・OSC/UDP | public static Version vigemBusVersionInfo = |
| 763 | `minSupportedViGEmBusVersionInfo` | 出力・KBM・OSC/UDP | public static Version minSupportedViGEmBusVersionInfo = new Version(MIN_SUPPORTED_VIGEMBUS_VERSION); |
| 764 | `hidHideInstalled` | 出力・KBM・OSC/UDP | public static bool hidHideInstalled = IsHidHideInstalled(); |
| 765 | `fakerInputInstalled` | 出力・KBM・OSC/UDP | public static bool fakerInputInstalled = IsFakerInputInstalled(); |
| 767 | `fakerInputVersion` | 出力・KBM・OSC/UDP | public static string fakerInputVersion = FakerInputVersion(); |
| 768 | `absDisplayBounds` | その他フラグ・状態 | public static Rect absDisplayBounds = new Rect(0, 0, 2, 2); |
| 769 | `fullDesktopBounds` | ユーティリティ計算 | public static Rect fullDesktopBounds = new Rect(0, 0, 2, 2); |
| 772 | `absUseAllMonitors` | モニタ・座標変換 | public static bool absUseAllMonitors = true; |
| 774 | `outputKBMHandler` | 出力・KBM・OSC/UDP | public static VirtualKBMBase outputKBMHandler = null; |
| 775 | `outputKBMMapping` | プロファイル・入力設定 | public static VirtualKBMMapping outputKBMMapping = null; |
| 787 | `defaultButtonMapping` | プロファイル・入力設定 | public static X360Controls[] defaultButtonMapping = { |
| 847 | `reverseX360ButtonMapping` | プロファイル・入力設定 | public static DS4Controls[] reverseX360ButtonMapping = new Func<DS4Controls[]>(() => |
| 862 | `string` | その他フラグ・状態 | public static Dictionary<X360Controls, string> xboxDefaultNames = new Dictionary<X360Controls, string>() |
| 909 | `string` | その他フラグ・状態 | public static Dictionary<X360Controls, string> ds4DefaultNames = new Dictionary<X360Controls, string>() |
| 955 | `getX360ControlString` | その他フラグ・状態 | public static string getX360ControlString(X360Controls key, OutContType conType) |
| 970 | `string` | その他フラグ・状態 | public static Dictionary<DS4Controls, string> ds4inputNames = new Dictionary<DS4Controls, string>() |
| 1032 | `int` | その他フラグ・状態 | public static Dictionary<DS4Controls, int> macroDS4Values = new Dictionary<DS4Controls, int>() |
| 1067 | `string` | その他フラグ・状態 | public static Dictionary<TrayIconChoice, string> iconChoiceResources = new Dictionary<TrayIconChoice, string> |
| 1076 | `SaveWhere` | その他フラグ・状態 | public static void SaveWhere(string path) |
| 1085 | `SaveDefault` | その他フラグ・状態 | public static bool SaveDefault(string path) |
| 1119 | `AdminNeeded` | その他フラグ・状態 | public static bool AdminNeeded() |
| 1133 | `IsAdministrator` | ユーティリティ計算 | public static bool IsAdministrator() |
| 1140 | `CheckForDevice` | デバイス・コントローラ状態 | public static bool CheckForDevice(string guid) |
| 1349 | `CheckHidHideAffectedStatus` | 出力・KBM・OSC/UDP | public static bool CheckHidHideAffectedStatus(string deviceInstanceId, |
| 1358 | `IsHidHideInstalled` | 出力・KBM・OSC/UDP | public static bool IsHidHideInstalled() |
| 1363 | `IsFakerInputInstalled` | 出力・KBM・OSC/UDP | public static bool IsFakerInputInstalled() |
| 1369 | `IsViGEmBusInstalled` | 出力・KBM・OSC/UDP | public static bool IsViGEmBusInstalled() |
| 1374 | `IsRunningSupportedViGEmBus` | 出力・KBM・OSC/UDP | public static bool IsRunningSupportedViGEmBus() |
| 1381 | `IsUsingMinViGEm117333` | 出力・KBM・OSC/UDP | public static bool IsUsingMinViGEm117333() |
| 1388 | `RefreshViGEmBusInfo` | 出力・KBM・OSC/UDP | public static void RefreshViGEmBusInfo() |
| 1393 | `RefreshHidHideInfo` | 出力・KBM・OSC/UDP | public static void RefreshHidHideInfo() |
| 1451 | `GetInstanceIdFromDevicePath` | デバイス・コントローラ状態 | public static string GetInstanceIdFromDevicePath(string devicePath) |
| 1502 | `CheckIfVirtualDevice` | 出力・KBM・OSC/UDP | public static bool CheckIfVirtualDevice(string devicePath) |
| 1551 | `FindConfigLocation` | プロファイル・入力設定 | public static void FindConfigLocation() |
| 1585 | `SetCulture` | パス・環境・UI・ログ | public static void SetCulture(string culture) |
| 1595 | `CreateStdActions` | SpecialAction管理 | public static void CreateStdActions() |
| 1633 | `CreateAutoProfiles` | プロファイル・入力設定 | public static bool CreateAutoProfiles(string m_Profile) |
| 1660 | `EventHandler` | その他フラグ・状態 | public static event EventHandler<EventArgs> ControllerStatusChange; // called when a controller is added/removed/battery or touchpad mode changes/etc. |
| 1661 | `ControllerStatusChanged` | デバイス・コントローラ状態 | public static void ControllerStatusChanged(object sender) |
| 1667 | `EventHandler` | その他フラグ・状態 | public static event EventHandler<BatteryReportArgs> BatteryStatusChange; |
| 1668 | `OnBatteryStatusChange` | デバイス・コントローラ状態 | public static void OnBatteryStatusChange(object sender, int index, int level, bool charging) |
| 1677 | `EventHandler` | その他フラグ・状態 | public static event EventHandler<ControllerRemovedArgs> ControllerRemoved; |
| 1678 | `OnControllerRemoved` | デバイス・コントローラ状態 | public static void OnControllerRemoved(object sender, int index) |
| 1687 | `EventHandler` | その他フラグ・状態 | public static event EventHandler<DeviceStatusChangeEventArgs> DeviceStatusChange; |
| 1688 | `OnDeviceStatusChanged` | デバイス・コントローラ状態 | public static void OnDeviceStatusChanged(object sender, int index) |
| 1697 | `EventHandler` | その他フラグ・状態 | public static event EventHandler<SerialChangeArgs> DeviceSerialChange; |
| 1698 | `OnDeviceSerialChange` | デバイス・コントローラ状態 | public static void OnDeviceSerialChange(object sender, int index, string serial) |
| 1707 | `CompileVersionNumberFromString` | ユーティリティ計算 | public static ulong CompileVersionNumberFromString(string versionStr) |
| 1721 | `CompileVersionNumber` | その他フラグ・状態 | public static ulong CompileVersionNumber(int majorPart, int minorPart, |
| 1731 | `UseExclusiveMode` | デバイス・コントローラ状態 | public static bool UseExclusiveMode |
| 1737 | `getUseExclusiveMode` | デバイス・コントローラ状態 | public static bool getUseExclusiveMode() |
| 1741 | `LastChecked` | その他フラグ・状態 | public static DateTime LastChecked |
| 1748 | `CheckUpdateStartupEnabled` | その他フラグ・状態 | public static bool CheckUpdateStartupEnabled |
| 1754 | `CheckEveryValue` | その他フラグ・状態 | public static int CheckEveryValue |
| 1761 | `CheckEveryUnit` | その他フラグ・状態 | public static int CheckEveryUnit |
| 1769 | `LastVersionChecked` | その他フラグ・状態 | public static string LastVersionChecked |
| 1779 | `LastVersionCheckedNum` | その他フラグ・状態 | public static ulong LastVersionCheckedNum |
| 1784 | `Notifications` | その他フラグ・状態 | public static int Notifications |
| 1790 | `ProfileChangedNotification` | プロファイル・入力設定 | public static bool ProfileChangedNotification |
| 1796 | `DCBTatStop` | ユーティリティ計算 | public static bool DCBTatStop |
| 1802 | `SwipeProfiles` | プロファイル・入力設定 | public static bool SwipeProfiles |
| 1808 | `DS4Mapping` | プロファイル・入力設定 | public static bool DS4Mapping |
| 1814 | `QuickCharge` | その他フラグ・状態 | public static bool QuickCharge |
| 1820 | `UseMoonlight` | その他フラグ・状態 | public static bool UseMoonlight |
| 1826 | `UseAdvancedMoonlight` | その他フラグ・状態 | public static bool UseAdvancedMoonlight |
| 1832 | `getQuickCharge` | その他フラグ・状態 | public static bool getQuickCharge() |
| 1837 | `CloseMini` | その他フラグ・状態 | public static bool CloseMini |
| 1843 | `StartMinimized` | その他フラグ・状態 | public static bool StartMinimized |
| 1849 | `MinToTaskbar` | ユーティリティ計算 | public static bool MinToTaskbar |
| 1855 | `GetMinToTaskbar` | ユーティリティ計算 | public static bool GetMinToTaskbar() |
| 1860 | `FormWidth` | パス・環境・UI・ログ | public static int FormWidth |
| 1866 | `FormHeight` | パス・環境・UI・ログ | public static int FormHeight |
| 1872 | `FormLocationX` | パス・環境・UI・ログ | public static int FormLocationX |
| 1878 | `FormLocationY` | パス・環境・UI・ログ | public static int FormLocationY |
| 1884 | `UseLang` | プロファイル・入力設定 | public static string UseLang |
| 1890 | `DownloadLang` | その他フラグ・状態 | public static bool DownloadLang |
| 1896 | `FlashWhenLate` | その他フラグ・状態 | public static bool FlashWhenLate |
| 1902 | `getFlashWhenLate` | その他フラグ・状態 | public static bool getFlashWhenLate() |
| 1907 | `FlashWhenLateAt` | その他フラグ・状態 | public static int FlashWhenLateAt |
| 1913 | `getFlashWhenLateAt` | その他フラグ・状態 | public static int getFlashWhenLateAt() |
| 1918 | `isUsingOSCServer` | 出力・KBM・OSC/UDP | public static bool isUsingOSCServer() |
| 1922 | `setUsingOSCServer` | 出力・KBM・OSC/UDP | public static void setUsingOSCServer(bool state) |
| 1927 | `getOSCServerPortNum` | 出力・KBM・OSC/UDP | public static int getOSCServerPortNum() |
| 1931 | `setOSCServerPort` | 出力・KBM・OSC/UDP | public static void setOSCServerPort(int value) |
| 1936 | `isInterpretingOscMonitoring` | 出力・KBM・OSC/UDP | public static bool isInterpretingOscMonitoring() |
| 1940 | `setInterpretingOscMonitoring` | 出力・KBM・OSC/UDP | public static void setInterpretingOscMonitoring(bool state) |
| 1945 | `isUsingOSCSender` | 出力・KBM・OSC/UDP | public static bool isUsingOSCSender() |
| 1949 | `setUsingOSCSender` | 出力・KBM・OSC/UDP | public static void setUsingOSCSender(bool state) |
| 1954 | `getOSCSenderPortNum` | 出力・KBM・OSC/UDP | public static int getOSCSenderPortNum() |
| 1959 | `setOSCSenderPort` | 出力・KBM・OSC/UDP | public static void setOSCSenderPort(int value) |
| 1964 | `getOSCSenderAddress` | 出力・KBM・OSC/UDP | public static string getOSCSenderAddress() |
| 1968 | `setOSCSenderAddress` | 出力・KBM・OSC/UDP | public static void setOSCSenderAddress(string value) |
| 1975 | `isUsingUDPServer` | 出力・KBM・OSC/UDP | public static bool isUsingUDPServer() |
| 1979 | `setUsingUDPServer` | 出力・KBM・OSC/UDP | public static void setUsingUDPServer(bool state) |
| 1984 | `getUDPServerPortNum` | 出力・KBM・OSC/UDP | public static int getUDPServerPortNum() |
| 1988 | `setUDPServerPort` | 出力・KBM・OSC/UDP | public static void setUDPServerPort(int value) |
| 1993 | `getUDPServerListenAddress` | 出力・KBM・OSC/UDP | public static string getUDPServerListenAddress() |
| 1997 | `setUDPServerListenAddress` | 出力・KBM・OSC/UDP | public static void setUDPServerListenAddress(string value) |
| 2002 | `UseUDPSeverSmoothing` | 出力・KBM・OSC/UDP | public static bool UseUDPSeverSmoothing |
| 2008 | `IsUsingUDPServerSmoothing` | 出力・KBM・OSC/UDP | public static bool IsUsingUDPServerSmoothing() |
| 2013 | `UDPServerSmoothingMincutoff` | 出力・KBM・OSC/UDP | public static double UDPServerSmoothingMincutoff |
| 2024 | `EventHandler` | その他フラグ・状態 | public static event EventHandler UDPServerSmoothingMincutoffChanged; |
| 2026 | `UDPServerSmoothingBeta` | 出力・KBM・OSC/UDP | public static double UDPServerSmoothingBeta |
| 2037 | `EventHandler` | その他フラグ・状態 | public static event EventHandler UDPServerSmoothingBetaChanged; |
| 2039 | `UseIconChoice` | その他フラグ・状態 | public static TrayIconChoice UseIconChoice |
| 2045 | `EventHandler` | その他フラグ・状態 | public static event EventHandler<byte> BatteryChanged; |
| 2047 | `InvokeBatteryChanged` | デバイス・コントローラ状態 | public static void InvokeBatteryChanged(byte percentage) |
| 2052 | `UseCurrentTheme` | パス・環境・UI・ログ | public static AppThemeChoice UseCurrentTheme |
| 2058 | `UseCustomSteamFolder` | ユーティリティ計算 | public static bool UseCustomSteamFolder |
| 2064 | `CustomSteamFolder` | ユーティリティ計算 | public static string CustomSteamFolder |
| 2070 | `AutoProfileRevertDefaultProfile` | プロファイル・入力設定 | public static bool AutoProfileRevertDefaultProfile |
| 2076 | `autoProfileSwitchNotifyChoice` | プロファイル・入力設定 | public static AutoProfileDisplayProfileSwitchChoices autoProfileSwitchNotifyChoice |
| 2085 | `FakeExeName` | パス・環境・UI・ログ | public static string FakeExeName |
| 2098 | `AbsoluteDisplayEDID` | その他フラグ・状態 | public static string AbsoluteDisplayEDID |
| 2104 | `RightStickDriftXAxis` | プロファイル・入力設定 | public static sbyte[] RightStickDriftXAxis => m_Config.rightStickDriftXAxis; |
| 2105 | `RightStickDriftYAxis` | プロファイル・入力設定 | public static sbyte[] RightStickDriftYAxis => m_Config.rightStickDriftYAxis; |
| 2106 | `LeftStickDriftXAxis` | プロファイル・入力設定 | public static sbyte[] LeftStickDriftXAxis => m_Config.leftStickDriftXAxis; |
| 2107 | `LeftStickDriftYAxis` | プロファイル・入力設定 | public static sbyte[] LeftStickDriftYAxis => m_Config.leftStickDriftYAxis; |
| 2109 | `InverseRumbleMotors` | プロファイル・入力設定 | public static bool[] InverseRumbleMotors => m_Config.inverseRumbleMotors; |
| 2111 | `DebouncingMs` | その他フラグ・状態 | public static int[] DebouncingMs => m_Config.debouncingMs; |
| 2113 | `DebouncingMsHasChanged` | その他フラグ・状態 | public static void DebouncingMsHasChanged() |
| 2118 | `EventHandler` | その他フラグ・状態 | public static event EventHandler DebouncingMsChanged; |
| 2120 | `UseDs3PitchRollSim` | その他フラグ・状態 | public static bool UseDs3PitchRollSim |
| 2127 | `ButtonMouseInfos` | プロファイル・入力設定 | public static ButtonMouseInfo[] ButtonMouseInfos => m_Config.buttonMouseInfos; |
| 2128 | `ButtonAbsMouseInfos` | プロファイル・入力設定 | public static ButtonAbsMouseInfo[] ButtonAbsMouseInfos => m_Config.buttonAbsMouseInfos; |
| 2130 | `RumbleBoost` | プロファイル・入力設定 | public static byte[] RumbleBoost => m_Config.rumble; |
| 2131 | `getRumbleBoost` | プロファイル・入力設定 | public static byte getRumbleBoost(int index) |
| 2144 | `setRumbleAutostopTime` | プロファイル・入力設定 | public static void setRumbleAutostopTime(int index, int value) |
| 2153 | `getRumbleAutostopTime` | プロファイル・入力設定 | public static int getRumbleAutostopTime(int index) |
| 2158 | `EnableTouchToggle` | プロファイル・入力設定 | public static bool[] EnableTouchToggle => m_Config.enableTouchToggle; |
| 2159 | `getEnableTouchToggle` | プロファイル・入力設定 | public static bool getEnableTouchToggle(int index) |
| 2164 | `IdleDisconnectTimeout` | その他フラグ・状態 | public static int[] IdleDisconnectTimeout => m_Config.idleDisconnectTimeout; |
| 2165 | `getIdleDisconnectTimeout` | その他フラグ・状態 | public static int getIdleDisconnectTimeout(int index) |
| 2170 | `EnableOutputDataToDS4` | 出力・KBM・OSC/UDP | public static bool[] EnableOutputDataToDS4 => m_Config.enableOutputDataToDS4; |
| 2171 | `getEnableOutputDataToDS4` | 出力・KBM・OSC/UDP | public static bool getEnableOutputDataToDS4(int index) |
| 2176 | `TouchSensitivity` | プロファイル・入力設定 | public static byte[] TouchSensitivity => m_Config.touchSensitivity; |
| 2177 | `getTouchSensitivity` | プロファイル・入力設定 | public static byte[] getTouchSensitivity() |
| 2182 | `getTouchSensitivity` | プロファイル・入力設定 | public static byte getTouchSensitivity(int index) |
| 2187 | `TouchActive` | プロファイル・入力設定 | public static bool[] TouchActive => touchpadActive; |
| 2188 | `GetTouchActive` | プロファイル・入力設定 | public static bool GetTouchActive(int index) |
| 2193 | `LightbarSettingsInfo` | プロファイル・入力設定 | public static LightbarSettingInfo[] LightbarSettingsInfo => m_Config.lightbarSettingInfo; |
| 2194 | `getLightbarSettingsInfo` | プロファイル・入力設定 | public static LightbarSettingInfo getLightbarSettingsInfo(int index) |
| 2199 | `DinputOnly` | ユーティリティ計算 | public static bool[] DinputOnly => m_Config.dinputOnly; |
| 2200 | `getDInputOnly` | ユーティリティ計算 | public static bool getDInputOnly(int index) |
| 2205 | `ProcessPriority` | その他フラグ・状態 | public static int ProcessPriority |
| 2211 | `StartTouchpadOff` | プロファイル・入力設定 | public static bool[] StartTouchpadOff => m_Config.startTouchpadOff; |
| 2213 | `IsUsingTouchpadForControls` | プロファイル・入力設定 | public static bool IsUsingTouchpadForControls(int index) |
| 2218 | `TouchOutMode` | プロファイル・入力設定 | public static TouchpadOutMode[] TouchOutMode = m_Config.touchOutMode; |
| 2220 | `IsUsingSAForControls` | その他フラグ・状態 | public static bool IsUsingSAForControls(int index) |
| 2225 | `SATriggers` | プロファイル・入力設定 | public static string[] SATriggers => m_Config.sATriggers; |
| 2226 | `getSATriggers` | プロファイル・入力設定 | public static string getSATriggers(int index) |
| 2231 | `SATriggerCond` | プロファイル・入力設定 | public static bool[] SATriggerCond => m_Config.sATriggerCond; |
| 2232 | `getSATriggerCond` | プロファイル・入力設定 | public static bool getSATriggerCond(int index) |
| 2236 | `SetSaTriggerCond` | プロファイル・入力設定 | public static void SetSaTriggerCond(int index, string text) |
| 2242 | `GyroOutputMode` | プロファイル・入力設定 | public static GyroOutMode[] GyroOutputMode => m_Config.gyroOutMode; |
| 2243 | `GetGyroOutMode` | プロファイル・入力設定 | public static GyroOutMode GetGyroOutMode(int device) |
| 2248 | `SAMousestickTriggers` | プロファイル・入力設定 | public static string[] SAMousestickTriggers => m_Config.sAMouseStickTriggers; |
| 2249 | `GetSAMouseStickTriggers` | プロファイル・入力設定 | public static string GetSAMouseStickTriggers(int device) |
| 2254 | `SAMouseStickTriggerCond` | プロファイル・入力設定 | public static bool[] SAMouseStickTriggerCond => m_Config.sAMouseStickTriggerCond; |
| 2255 | `GetSAMouseStickTriggerCond` | プロファイル・入力設定 | public static bool GetSAMouseStickTriggerCond(int device) |
| 2259 | `SetSaMouseStickTriggerCond` | プロファイル・入力設定 | public static void SetSaMouseStickTriggerCond(int index, string text) |
| 2264 | `GyroMouseStickTriggerTurns` | プロファイル・入力設定 | public static bool[] GyroMouseStickTriggerTurns = m_Config.gyroMouseStickTriggerTurns; |
| 2265 | `GetGyroMouseStickTriggerTurns` | プロファイル・入力設定 | public static bool GetGyroMouseStickTriggerTurns(int device) |
| 2270 | `GyroMouseStickHorizontalAxis` | プロファイル・入力設定 | public static int[] GyroMouseStickHorizontalAxis => |
| 2272 | `getGyroMouseStickHorizontalAxis` | プロファイル・入力設定 | public static int getGyroMouseStickHorizontalAxis(int index) |
| 2277 | `GyroMouseStickInf` | プロファイル・入力設定 | public static GyroMouseStickInfo[] GyroMouseStickInf => m_Config.gyroMStickInfo; |
| 2278 | `GetGyroMouseStickInfo` | プロファイル・入力設定 | public static GyroMouseStickInfo GetGyroMouseStickInfo(int device) |
| 2283 | `GyroSwipeInf` | プロファイル・入力設定 | public static GyroDirectionalSwipeInfo[] GyroSwipeInf => m_Config.gyroSwipeInfo; |
| 2284 | `GetGyroSwipeInfo` | プロファイル・入力設定 | public static GyroDirectionalSwipeInfo GetGyroSwipeInfo(int device) |
| 2289 | `GyroMouseStickToggle` | プロファイル・入力設定 | public static bool[] GyroMouseStickToggle => m_Config.gyroMouseStickToggle; |
| 2290 | `SetGyroMouseStickToggle` | プロファイル・入力設定 | public static void SetGyroMouseStickToggle(int index, bool value, ControlService control) |
| 2293 | `SASteeringWheelEmulationAxis` | プロファイル・入力設定 | public static SASteeringWheelEmulationAxisType[] SASteeringWheelEmulationAxis => m_Config.sASteeringWheelEmulationAxis; |
| 2294 | `GetSASteeringWheelEmulationAxis` | プロファイル・入力設定 | public static SASteeringWheelEmulationAxisType GetSASteeringWheelEmulationAxis(int index) |
| 2299 | `SASteeringWheelEmulationRange` | 出力・KBM・OSC/UDP | public static int[] SASteeringWheelEmulationRange => m_Config.sASteeringWheelEmulationRange; |
| 2300 | `GetSASteeringWheelEmulationRange` | 出力・KBM・OSC/UDP | public static int GetSASteeringWheelEmulationRange(int index) |
| 2305 | `TouchDisInvertTriggers` | プロファイル・入力設定 | public static int[][] TouchDisInvertTriggers => m_Config.touchDisInvertTriggers; |
| 2306 | `getTouchDisInvertTriggers` | プロファイル・入力設定 | public static int[] getTouchDisInvertTriggers(int index) |
| 2311 | `GyroSensitivity` | プロファイル・入力設定 | public static int[] GyroSensitivity => m_Config.gyroSensitivity; |
| 2312 | `getGyroSensitivity` | プロファイル・入力設定 | public static int getGyroSensitivity(int index) |
| 2317 | `GyroSensVerticalScale` | プロファイル・入力設定 | public static int[] GyroSensVerticalScale => m_Config.gyroSensVerticalScale; |
| 2318 | `getGyroSensVerticalScale` | プロファイル・入力設定 | public static int getGyroSensVerticalScale(int index) |
| 2323 | `GyroInvert` | プロファイル・入力設定 | public static int[] GyroInvert => m_Config.gyroInvert; |
| 2324 | `getGyroInvert` | プロファイル・入力設定 | public static int getGyroInvert(int index) |
| 2329 | `GyroTriggerTurns` | プロファイル・入力設定 | public static bool[] GyroTriggerTurns => m_Config.gyroTriggerTurns; |
| 2330 | `getGyroTriggerTurns` | プロファイル・入力設定 | public static bool getGyroTriggerTurns(int index) |
| 2335 | `GyroMouseHorizontalAxis` | プロファイル・入力設定 | public static int[] GyroMouseHorizontalAxis => m_Config.gyroMouseHorizontalAxis; |
| 2336 | `getGyroMouseHorizontalAxis` | プロファイル・入力設定 | public static int getGyroMouseHorizontalAxis(int index) |
| 2341 | `GyroMouseDeadZone` | プロファイル・入力設定 | public static int[] GyroMouseDeadZone => m_Config.gyroMouseDZ; |
| 2342 | `GetGyroMouseDeadZone` | プロファイル・入力設定 | public static int GetGyroMouseDeadZone(int index) |
| 2347 | `SetGyroMouseDeadZone` | プロファイル・入力設定 | public static void SetGyroMouseDeadZone(int index, int value, ControlService control) |
| 2352 | `GyroMouseToggle` | プロファイル・入力設定 | public static bool[] GyroMouseToggle => m_Config.gyroMouseToggle; |
| 2353 | `SetGyroMouseToggle` | プロファイル・入力設定 | public static void SetGyroMouseToggle(int index, bool value, ControlService control) |
| 2356 | `SetGyroControlsToggle` | プロファイル・入力設定 | public static void SetGyroControlsToggle(int index, bool value, ControlService control) |
| 2359 | `GyroMouseInfo` | プロファイル・入力設定 | public static GyroMouseInfo[] GyroMouseInfo => m_Config.gyroMouseInfo; |
| 2361 | `GyroControlsInf` | プロファイル・入力設定 | public static GyroControlsInfo[] GyroControlsInf => m_Config.gyroControlsInf; |
| 2362 | `GetGyroControlsInfo` | プロファイル・入力設定 | public static GyroControlsInfo GetGyroControlsInfo(int index) |
| 2367 | `WheelSmoothInfo` | 出力・KBM・OSC/UDP | public static SteeringWheelSmoothingInfo[] WheelSmoothInfo => m_Config.wheelSmoothInfo; |
| 2368 | `SAWheelFuzzValues` | 出力・KBM・OSC/UDP | public static int[] SAWheelFuzzValues => m_Config.saWheelFuzzValues; |
| 2371 | `DS4Color` | デバイス・コントローラ状態 | public static ref DS4Color getMainColor(int index) |
| 2378 | `DS4Color` | デバイス・コントローラ状態 | public static ref DS4Color getLowColor(int index) |
| 2385 | `DS4Color` | デバイス・コントローラ状態 | public static ref DS4Color getChargingColor(int index) |
| 2392 | `DS4Color` | デバイス・コントローラ状態 | public static ref DS4Color getCustomColor(int index) |
| 2399 | `getUseCustomLed` | ユーティリティ計算 | public static bool getUseCustomLed(int index) |
| 2406 | `DS4Color` | デバイス・コントローラ状態 | public static ref DS4Color getFlashColor(int index) |
| 2412 | `TapSensitivity` | プロファイル・入力設定 | public static byte[] TapSensitivity => m_Config.tapSensitivity; |
| 2413 | `getTapSensitivity` | プロファイル・入力設定 | public static byte getTapSensitivity(int index) |
| 2418 | `DoubleTap` | その他フラグ・状態 | public static bool[] DoubleTap => m_Config.doubleTap; |
| 2419 | `getDoubleTap` | その他フラグ・状態 | public static bool getDoubleTap(int index) |
| 2424 | `ScrollSensitivity` | プロファイル・入力設定 | public static int[] ScrollSensitivity => m_Config.scrollSensitivity; |
| 2425 | `getScrollSensitivity` | プロファイル・入力設定 | public static int[] getScrollSensitivity() |
| 2429 | `getScrollSensitivity` | プロファイル・入力設定 | public static int getScrollSensitivity(int index) |
| 2434 | `LowerRCOn` | その他フラグ・状態 | public static bool[] LowerRCOn => m_Config.lowerRCOn; |
| 2435 | `TouchClickPassthru` | プロファイル・入力設定 | public static bool[] TouchClickPassthru => m_Config.touchClickPassthru; |
| 2436 | `TouchpadButtonMode` | プロファイル・入力設定 | public static TouchButtonActivationMode[] TouchpadButtonMode => m_Config.touchpadButtonMode; |
| 2437 | `TouchpadJitterCompensation` | プロファイル・入力設定 | public static bool[] TouchpadJitterCompensation => m_Config.touchpadJitterCompensation; |
| 2438 | `getTouchpadJitterCompensation` | プロファイル・入力設定 | public static bool getTouchpadJitterCompensation(int index) |
| 2443 | `TouchpadInvert` | プロファイル・入力設定 | public static int[] TouchpadInvert => m_Config.touchpadInvert; |
| 2444 | `getTouchpadInvert` | プロファイル・入力設定 | public static int getTouchpadInvert(int index) |
| 2449 | `L2ModInfo` | その他フラグ・状態 | public static TriggerDeadZoneZInfo[] L2ModInfo => m_Config.l2ModInfo; |
| 2450 | `GetL2ModInfo` | その他フラグ・状態 | public static TriggerDeadZoneZInfo GetL2ModInfo(int index) |
| 2456 | `getL2Deadzone` | プロファイル・入力設定 | public static byte getL2Deadzone(int index) |
| 2462 | `R2ModInfo` | その他フラグ・状態 | public static TriggerDeadZoneZInfo[] R2ModInfo => m_Config.r2ModInfo; |
| 2463 | `GetR2ModInfo` | その他フラグ・状態 | public static TriggerDeadZoneZInfo GetR2ModInfo(int index) |
| 2469 | `getR2Deadzone` | プロファイル・入力設定 | public static byte getR2Deadzone(int index) |
| 2475 | `SXDeadzone` | プロファイル・入力設定 | public static double[] SXDeadzone => m_Config.SXDeadzone; |
| 2476 | `getSXDeadzone` | プロファイル・入力設定 | public static double getSXDeadzone(int index) |
| 2481 | `SZDeadzone` | プロファイル・入力設定 | public static double[] SZDeadzone => m_Config.SZDeadzone; |
| 2482 | `getSZDeadzone` | プロファイル・入力設定 | public static double getSZDeadzone(int index) |
| 2488 | `getLSDeadzone` | プロファイル・入力設定 | public static int getLSDeadzone(int index) |
| 2495 | `getRSDeadzone` | プロファイル・入力設定 | public static int getRSDeadzone(int index) |
| 2502 | `getLSAntiDeadzone` | プロファイル・入力設定 | public static int getLSAntiDeadzone(int index) |
| 2509 | `getRSAntiDeadzone` | プロファイル・入力設定 | public static int getRSAntiDeadzone(int index) |
| 2515 | `LSModInfo` | その他フラグ・状態 | public static StickDeadZoneInfo[] LSModInfo => m_Config.lsModInfo; |
| 2516 | `GetLSDeadInfo` | プロファイル・入力設定 | public static StickDeadZoneInfo GetLSDeadInfo(int index) |
| 2521 | `RSModInfo` | その他フラグ・状態 | public static StickDeadZoneInfo[] RSModInfo => m_Config.rsModInfo; |
| 2522 | `GetRSDeadInfo` | プロファイル・入力設定 | public static StickDeadZoneInfo GetRSDeadInfo(int index) |
| 2527 | `SXAntiDeadzone` | プロファイル・入力設定 | public static double[] SXAntiDeadzone => m_Config.SXAntiDeadzone; |
| 2528 | `getSXAntiDeadzone` | プロファイル・入力設定 | public static double getSXAntiDeadzone(int index) |
| 2533 | `SZAntiDeadzone` | プロファイル・入力設定 | public static double[] SZAntiDeadzone => m_Config.SZAntiDeadzone; |
| 2534 | `getSZAntiDeadzone` | プロファイル・入力設定 | public static double getSZAntiDeadzone(int index) |
| 2540 | `getLSMaxzone` | その他フラグ・状態 | public static int getLSMaxzone(int index) |
| 2547 | `getRSMaxzone` | その他フラグ・状態 | public static int getRSMaxzone(int index) |
| 2553 | `SXMaxzone` | その他フラグ・状態 | public static double[] SXMaxzone => m_Config.SXMaxzone; |
| 2554 | `getSXMaxzone` | その他フラグ・状態 | public static double getSXMaxzone(int index) |
| 2559 | `SZMaxzone` | その他フラグ・状態 | public static double[] SZMaxzone => m_Config.SZMaxzone; |
| 2560 | `getSZMaxzone` | その他フラグ・状態 | public static double getSZMaxzone(int index) |
| 2566 | `getL2AntiDeadzone` | プロファイル・入力設定 | public static int getL2AntiDeadzone(int index) |
| 2573 | `getR2AntiDeadzone` | プロファイル・入力設定 | public static int getR2AntiDeadzone(int index) |
| 2580 | `getL2Maxzone` | その他フラグ・状態 | public static int getL2Maxzone(int index) |
| 2587 | `getR2Maxzone` | その他フラグ・状態 | public static int getR2Maxzone(int index) |
| 2593 | `LSRotation` | その他フラグ・状態 | public static double[] LSRotation => m_Config.LSRotation; |
| 2599 | `getLSRotation` | その他フラグ・状態 | public static double getLSRotation(int index) |
| 2604 | `RSRotation` | その他フラグ・状態 | public static double[] RSRotation => m_Config.RSRotation; |
| 2610 | `getRSRotation` | その他フラグ・状態 | public static double getRSRotation(int index) |
| 2615 | `L2Sens` | その他フラグ・状態 | public static double[] L2Sens => m_Config.l2Sens; |
| 2616 | `getL2Sens` | その他フラグ・状態 | public static double getL2Sens(int index) |
| 2621 | `R2Sens` | その他フラグ・状態 | public static double[] R2Sens => m_Config.r2Sens; |
| 2622 | `getR2Sens` | その他フラグ・状態 | public static double getR2Sens(int index) |
| 2627 | `SXSens` | その他フラグ・状態 | public static double[] SXSens => m_Config.SXSens; |
| 2628 | `getSXSens` | その他フラグ・状態 | public static double getSXSens(int index) |
| 2633 | `SZSens` | その他フラグ・状態 | public static double[] SZSens => m_Config.SZSens; |
| 2634 | `getSZSens` | その他フラグ・状態 | public static double getSZSens(int index) |
| 2639 | `LSSens` | その他フラグ・状態 | public static double[] LSSens => m_Config.LSSens; |
| 2640 | `getLSSens` | その他フラグ・状態 | public static double getLSSens(int index) |
| 2645 | `RSSens` | その他フラグ・状態 | public static double[] RSSens => m_Config.RSSens; |
| 2646 | `getRSSens` | その他フラグ・状態 | public static double getRSSens(int index) |
| 2651 | `BTPollRate` | その他フラグ・状態 | public static int[] BTPollRate => m_Config.btPollRate; |
| 2652 | `getBTPollRate` | その他フラグ・状態 | public static int getBTPollRate(int index) |
| 2659 | `DualSenseRumbleEmulationMode` | プロファイル・入力設定 | public static DualSenseDevice.RumbleEmulationMode[] DualSenseRumbleEmulationMode |
| 2665 | `UseGenericRumbleStrRescaleForDualSenses` | プロファイル・入力設定 | public static bool[] UseGenericRumbleStrRescaleForDualSenses |
| 2671 | `DualSenseHapticPowerLevel` | その他フラグ・状態 | public static byte[] DualSenseHapticPowerLevel |
| 2679 | `SquStickInfo` | プロファイル・入力設定 | public static SquareStickInfo[] SquStickInfo => m_Config.squStickInfo; |
| 2680 | `GetSquareStickInfo` | プロファイル・入力設定 | public static SquareStickInfo GetSquareStickInfo(int device) |
| 2685 | `LSAntiSnapbackInfo` | その他フラグ・状態 | public static StickAntiSnapbackInfo[] LSAntiSnapbackInfo => m_Config.lsAntiSnapbackInfo; |
| 2686 | `GetLSAntiSnapbackInfo` | その他フラグ・状態 | public static StickAntiSnapbackInfo GetLSAntiSnapbackInfo(int device) |
| 2691 | `RSAntiSnapbackInfo` | その他フラグ・状態 | public static StickAntiSnapbackInfo[] RSAntiSnapbackInfo => m_Config.rsAntiSnapbackInfo; |
| 2692 | `GetRSAntiSnapbackInfo` | その他フラグ・状態 | public static StickAntiSnapbackInfo GetRSAntiSnapbackInfo(int device) |
| 2697 | `LSOutputSettings` | プロファイル・入力設定 | public static StickOutputSetting[] LSOutputSettings => m_Config.lsOutputSettings; |
| 2698 | `RSOutputSettings` | プロファイル・入力設定 | public static StickOutputSetting[] RSOutputSettings => m_Config.rsOutputSettings; |
| 2700 | `L2OutputSettings` | プロファイル・入力設定 | public static TriggerOutputSettings[] L2OutputSettings => m_Config.l2OutputSettings; |
| 2701 | `R2OutputSettings` | プロファイル・入力設定 | public static TriggerOutputSettings[] R2OutputSettings => m_Config.r2OutputSettings; |
| 2703 | `setLsOutCurveMode` | その他フラグ・状態 | public static void setLsOutCurveMode(int index, int value) |
| 2707 | `getLsOutCurveMode` | その他フラグ・状態 | public static int getLsOutCurveMode(int index) |
| 2711 | `lsOutBezierCurveObj` | その他フラグ・状態 | public static BezierCurve[] lsOutBezierCurveObj => m_Config.lsOutBezierCurveObj; |
| 2713 | `setRsOutCurveMode` | その他フラグ・状態 | public static void setRsOutCurveMode(int index, int value) |
| 2717 | `getRsOutCurveMode` | その他フラグ・状態 | public static int getRsOutCurveMode(int index) |
| 2721 | `rsOutBezierCurveObj` | その他フラグ・状態 | public static BezierCurve[] rsOutBezierCurveObj => m_Config.rsOutBezierCurveObj; |
| 2723 | `setL2OutCurveMode` | その他フラグ・状態 | public static void setL2OutCurveMode(int index, int value) |
| 2727 | `getL2OutCurveMode` | その他フラグ・状態 | public static int getL2OutCurveMode(int index) |
| 2731 | `l2OutBezierCurveObj` | その他フラグ・状態 | public static BezierCurve[] l2OutBezierCurveObj => m_Config.l2OutBezierCurveObj; |
| 2733 | `setR2OutCurveMode` | その他フラグ・状態 | public static void setR2OutCurveMode(int index, int value) |
| 2737 | `getR2OutCurveMode` | その他フラグ・状態 | public static int getR2OutCurveMode(int index) |
| 2741 | `r2OutBezierCurveObj` | その他フラグ・状態 | public static BezierCurve[] r2OutBezierCurveObj => m_Config.r2OutBezierCurveObj; |
| 2743 | `setSXOutCurveMode` | その他フラグ・状態 | public static void setSXOutCurveMode(int index, int value) |
| 2747 | `getSXOutCurveMode` | その他フラグ・状態 | public static int getSXOutCurveMode(int index) |
| 2751 | `sxOutBezierCurveObj` | その他フラグ・状態 | public static BezierCurve[] sxOutBezierCurveObj => m_Config.sxOutBezierCurveObj; |
| 2753 | `setSZOutCurveMode` | その他フラグ・状態 | public static void setSZOutCurveMode(int index, int value) |
| 2757 | `getSZOutCurveMode` | その他フラグ・状態 | public static int getSZOutCurveMode(int index) |
| 2761 | `szOutBezierCurveObj` | その他フラグ・状態 | public static BezierCurve[] szOutBezierCurveObj => m_Config.szOutBezierCurveObj; |
| 2763 | `TrackballMode` | その他フラグ・状態 | public static bool[] TrackballMode => m_Config.trackballMode; |
| 2764 | `getTrackballMode` | その他フラグ・状態 | public static bool getTrackballMode(int index) |
| 2769 | `TrackballFriction` | その他フラグ・状態 | public static double[] TrackballFriction => m_Config.trackballFriction; |
| 2770 | `getTrackballFriction` | その他フラグ・状態 | public static double getTrackballFriction(int index) |
| 2787 | `TouchMouseStickInf` | プロファイル・入力設定 | public static TouchMouseStickInfo[] TouchMouseStickInf => m_Config.touchMStickInfo; |
| 2788 | `GetTouchMouseStickInfo` | プロファイル・入力設定 | public static TouchMouseStickInfo GetTouchMouseStickInfo(int device) |
| 2793 | `TouchAbsMouse` | プロファイル・入力設定 | public static TouchpadAbsMouseSettings[] TouchAbsMouse => m_Config.touchpadAbsMouse; |
| 2794 | `TouchRelMouse` | プロファイル・入力設定 | public static TouchpadRelMouseSettings[] TouchRelMouse => m_Config.touchpadRelMouse; |
| 2796 | `DeviceOptions` | デバイス・コントローラ状態 | public static ControlServiceDeviceOptions DeviceOptions => m_Config.deviceOptions; |
| 2798 | `OutContType` | その他フラグ・状態 | public static OutContType[] OutContType => m_Config.outputDevType; |
| 2799 | `OutputVirtualTriggerButton` | プロファイル・入力設定 | public static bool[] OutputVirtualTriggerButton => m_Config.outputVirtualTriggerButtons; |
| 2800 | `OutputDS4TriggerMode` | プロファイル・入力設定 | public static DS4TriggerOutputMode[] OutputDS4TriggerMode => m_Config.outputDS4TriggerMode; |
| 2801 | `GetOutputDS4TriggerMode` | プロファイル・入力設定 | public static DS4TriggerOutputMode GetOutputDS4TriggerMode(int index) |
| 2806 | `LaunchProgram` | SpecialAction管理 | public static string[] LaunchProgram => m_Config.launchProgram; |
| 2807 | `ProfilePath` | プロファイル・入力設定 | public static string[] ProfilePath => m_Config.profilePath; |
| 2808 | `OlderProfilePath` | プロファイル・入力設定 | public static string[] OlderProfilePath => m_Config.olderProfilePath; |
| 2809 | `SelectedProfile` | プロファイル・入力設定 | public static string[] SelectedProfile => m_Config.selectedProfile; |
| 2810 | `LinkedProfileUI` | プロファイル・入力設定 | public static string[] LinkedProfileUI => m_Config.linkedProfileUI; |
| 2811 | `DistanceProfiles` | プロファイル・入力設定 | public static bool[] DistanceProfiles = m_Config.distanceProfiles; |
| 2814 | `EventHandler` | その他フラグ・状態 | public static event EventHandler<SelectedProfileChangedEventArgs> SelectedProfileChanged; |
| 2816 | `RaiseSelectedProfileChanged` | プロファイル・入力設定 | public static void RaiseSelectedProfileChanged(int deviceIndex, string profileName) |
| 2833 | `ApplyProfile` | プロファイル・入力設定 | public static bool ApplyProfile(int device, string profileName, bool isTemp, bool launchProgram, |
| 2982 | `ProfileActions` | SpecialAction管理 | public static List<string>[] ProfileActions => m_Config.profileActions; |
| 2983 | `getProfileActionCount` | SpecialAction管理 | public static int getProfileActionCount(int index) |
| 2988 | `CalculateProfileActionCount` | SpecialAction管理 | public static void CalculateProfileActionCount(int index) |
| 2993 | `getProfileActions` | SpecialAction管理 | public static List<string> getProfileActions(int index) |
| 2998 | `UpdateDS4CSetting` | プロファイル・入力設定 | public static void UpdateDS4CSetting(int deviceNum, string buttonName, bool shift, object action, string exts, DS4KeyType kt, int trigger = 0) |
| 3005 | `UpdateDS4Extra` | デバイス・コントローラ状態 | public static void UpdateDS4Extra(int deviceNum, string buttonName, bool shift, string exts) |
| 3012 | `GetDS4Action` | SpecialAction管理 | public static ControlActionData GetDS4Action(int deviceNum, string buttonName, bool shift) => m_Config.GetDS4Action(deviceNum, buttonName, shift); |
| 3013 | `GetDS4Action` | SpecialAction管理 | public static ControlActionData GetDS4Action(int deviceNum, DS4Controls control, bool shift) => m_Config.GetDS4Action(deviceNum, control, shift); |
| 3014 | `GetDS4KeyType` | デバイス・コントローラ状態 | public static DS4KeyType GetDS4KeyType(int deviceNum, string buttonName, bool shift) => m_Config.GetDS4KeyType(deviceNum, buttonName, shift); |
| 3015 | `GetDS4Extra` | デバイス・コントローラ状態 | public static string GetDS4Extra(int deviceNum, string buttonName, bool shift) => m_Config.GetDS4Extra(deviceNum, buttonName, shift); |
| 3016 | `GetDS4STrigger` | プロファイル・入力設定 | public static int GetDS4STrigger(int deviceNum, string buttonName) => m_Config.GetDS4STrigger(deviceNum, buttonName); |
| 3017 | `GetDS4STrigger` | プロファイル・入力設定 | public static int GetDS4STrigger(int deviceNum, DS4Controls control) => m_Config.GetDS4STrigger(deviceNum, control); |
| 3018 | `getDS4CSettings` | プロファイル・入力設定 | public static List<DS4ControlSettings> getDS4CSettings(int device) => m_Config.ds4settings[device]; |
| 3019 | `GetDS4CSetting` | プロファイル・入力設定 | public static DS4ControlSettings GetDS4CSetting(int deviceNum, string control) => m_Config.GetDS4CSetting(deviceNum, control); |
| 3020 | `GetDS4CSetting` | プロファイル・入力設定 | public static DS4ControlSettings GetDS4CSetting(int deviceNum, DS4Controls control) => m_Config.GetDS4CSetting(deviceNum, control); |
| 3021 | `GetControlSettingsGroup` | プロファイル・入力設定 | public static ControlSettingsGroup GetControlSettingsGroup(int deviceNum) => m_Config.ds4controlSettings[deviceNum]; |
| 3022 | `HasCustomActions` | SpecialAction管理 | public static bool HasCustomActions(int deviceNum) => m_Config.HasCustomActions(deviceNum); |
| 3023 | `HasCustomExtras` | ユーティリティ計算 | public static bool HasCustomExtras(int deviceNum) => m_Config.HasCustomExtras(deviceNum); |
| 3025 | `containsCustomAction` | SpecialAction管理 | public static bool containsCustomAction(int deviceNum) |
| 3030 | `containsCustomExtras` | ユーティリティ計算 | public static bool containsCustomExtras(int deviceNum) |
| 3035 | `SaveAction` | SpecialAction管理 | public static void SaveAction(string name, string controls, int mode, |
| 3067 | `SaveActions` | SpecialAction管理 | public static void SaveActions() |
| 3081 | `RemoveAction` | SpecialAction管理 | public static void RemoveAction(string name) |
| 3140 | `LoadActions` | SpecialAction管理 | public static bool LoadActions() => m_Config.LoadActions(); |
| 3142 | `GetActions` | SpecialAction管理 | public static List<SpecialAction> GetActions() => m_Config.actions; |
| 3144 | `GetActionIndexOf` | SpecialAction管理 | public static int GetActionIndexOf(string name) |
| 3149 | `GetProfileActionIndexOf` | SpecialAction管理 | public static int GetProfileActionIndexOf(int device, string name) |
| 3156 | `GetAction` | SpecialAction管理 | public static SpecialAction GetAction(string name) |
| 3161 | `GetProfileAction` | SpecialAction管理 | public static SpecialAction GetProfileAction(int device, string name) |
| 3169 | `NormalizeActionName` | SpecialAction管理 | public static string NormalizeActionName(string name) |
| 3175 | `CalculateProfileActionDicts` | SpecialAction管理 | public static void CalculateProfileActionDicts(int device) |
| 3180 | `CacheProfileCustomsFlags` | プロファイル・入力設定 | public static void CacheProfileCustomsFlags(int device) |
| 3185 | `CacheExtraProfileInfo` | プロファイル・入力設定 | public static void CacheExtraProfileInfo(int device) |
| 3190 | `getX360ControlsByName` | その他フラグ・状態 | public static X360Controls getX360ControlsByName(string key) |
| 3195 | `getX360ControlString` | その他フラグ・状態 | public static string getX360ControlString(X360Controls key) |
| 3200 | `getDS4ControlsByName` | デバイス・コントローラ状態 | public static DS4Controls getDS4ControlsByName(string key) |
| 3205 | `getDefaultX360ControlBinding` | その他フラグ・状態 | public static X360Controls getDefaultX360ControlBinding(DS4Controls dc) |
| 3210 | `containsLinkedProfile` | プロファイル・入力設定 | public static bool containsLinkedProfile(string serial) |
| 3216 | `getLinkedProfile` | プロファイル・入力設定 | public static string getLinkedProfile(string serial) |
| 3228 | `changeLinkedProfile` | プロファイル・入力設定 | public static void changeLinkedProfile(string serial, string profile) |
| 3234 | `removeLinkedProfile` | プロファイル・入力設定 | public static void removeLinkedProfile(string serial) |
| 3244 | `Load` | その他フラグ・状態 | public static bool Load() => m_Config.Load(); |
| 3246 | `LoadProfile` | プロファイル・入力設定 | public static bool LoadProfile(int device, bool launchprogram, ControlService control, |
| 3259 | `LoadTempProfile` | プロファイル・入力設定 | public static bool LoadTempProfile(int device, string name, bool launchprogram, |
| 3275 | `LoadBlankDevProfile` | プロファイル・入力設定 | public static void LoadBlankDevProfile(int device, bool launchprogram, ControlService control, |
| 3287 | `LoadBlankDS4Profile` | プロファイル・入力設定 | public static void LoadBlankDS4Profile(int device, bool launchprogram, ControlService control, |
| 3299 | `LoadDefaultGamepadGyroProfile` | プロファイル・入力設定 | public static void LoadDefaultGamepadGyroProfile(int device, bool launchprogram, ControlService control, |
| 3311 | `LoadDefaultDS4GamepadGyroProfile` | プロファイル・入力設定 | public static void LoadDefaultDS4GamepadGyroProfile(int device, bool launchprogram, ControlService control, |
| 3323 | `LoadDefaultMixedControlsProfile` | プロファイル・入力設定 | public static void LoadDefaultMixedControlsProfile(int device, bool launchprogram, ControlService control, |
| 3335 | `LoadDefaultDS4MixedControlsProfile` | プロファイル・入力設定 | public static void LoadDefaultDS4MixedControlsProfile(int device, bool launchprogram, ControlService control, |
| 3347 | `LoadDefaultMixedGyroMouseProfile` | プロファイル・入力設定 | public static void LoadDefaultMixedGyroMouseProfile(int device, bool launchprogram, ControlService control, |
| 3359 | `LoadDefaultDS4MixedGyroMouseProfile` | プロファイル・入力設定 | public static void LoadDefaultDS4MixedGyroMouseProfile(int device, bool launchprogram, ControlService control, |
| 3371 | `LoadDefaultKBMProfile` | プロファイル・入力設定 | public static void LoadDefaultKBMProfile(int device, bool launchprogram, ControlService control, |
| 3383 | `LoadDefaultKBMGyroMouseProfile` | プロファイル・入力設定 | public static void LoadDefaultKBMGyroMouseProfile(int device, bool launchprogram, ControlService control, |
| 3395 | `Save` | その他フラグ・状態 | public static bool Save() |
| 3400 | `SaveProfile` | プロファイル・入力設定 | public static void SaveProfile(int device, string proName) |
| 3405 | `SaveAsProfile` | プロファイル・入力設定 | public static void SaveAsProfile(int device, string propath) |
| 3410 | `SaveLinkedProfiles` | プロファイル・入力設定 | public static bool SaveLinkedProfiles() |
| 3415 | `LoadLinkedProfiles` | プロファイル・入力設定 | public static bool LoadLinkedProfiles() |
| 3420 | `SaveControllerConfigs` | プロファイル・入力設定 | public static bool SaveControllerConfigs(DS4Device device = null) |
| 3432 | `LoadControllerConfigs` | プロファイル・入力設定 | public static bool LoadControllerConfigs(DS4Device device = null) |
| 3455 | `getTransitionedColor` | その他フラグ・状態 | public static DS4Color getTransitionedColor(ref DS4Color c1, ref DS4Color c2, double ratio) |
| 3488 | `HuetoRGB` | ユーティリティ計算 | public static Color HuetoRGB(float hue, float sat, float bri) |
| 3528 | `Clamp` | ユーティリティ計算 | public static double Clamp(double min, double value, double max) |
| 3538 | `InitOutputKBMHandler` | 出力・KBM・OSC/UDP | public static void InitOutputKBMHandler(string identifier) |
| 3556 | `InitOutputKBMMapping` | プロファイル・入力設定 | public static void InitOutputKBMMapping(string identifier) |
| 3561 | `RefreshFakerInputInfo` | 出力・KBM・OSC/UDP | public static void RefreshFakerInputInfo() |
| 3572 | `RefreshActionAlias` | SpecialAction管理 | public static void RefreshActionAlias(DS4ControlSettings setting, bool shift) |
| 3592 | `RefreshExtrasButtons` | プロファイル・入力設定 | public static void RefreshExtrasButtons(int deviceNum, List<DS4Controls> devButtons) |
| 3601 | `TranslateCoorToAbsDisplay` | ユーティリティ計算 | public static void TranslateCoorToAbsDisplay(double inX, double inY, |
| 3622 | `PrepareAbsMonitorBounds` | モニタ・座標変換 | public static void PrepareAbsMonitorBounds(string edid) |
| 3657 | `FindMonitorByEDID` | モニタ・座標変換 | public static bool FindMonitorByEDID(string edid, out DISPLAY_DEVICE display) |
| 3689 | `GrabCurrentMonitors` | モニタ・座標変換 | public static IEnumerable<DISPLAY_DEVICE> GrabCurrentMonitors() |

## 呼び出し元ファイル一覧

- 実測: 80ファイル（計画書の暫定値75ファイルとの差分: +5）
- `ScpUtil.cs`自身の内部参照は除外した。件数は各ファイル内の`Global.`出現数である。

| ファイル | `Global.`出現数 |
|---|---:|
| `DS4Windows/Actions/DefaultProfileSwitcher.cs` | 4 |
| `DS4Windows/App.xaml.cs` | 55 |
| `DS4Windows/AutoProfileChecker.cs` | 19 |
| `DS4Windows/AutoProfileHolder.cs` | 2 |
| `DS4Windows/BezierCurveEditor/BezierCurve.cs` | 2 |
| `DS4Windows/DS4Control/ActionManager.cs` | 3 |
| `DS4Windows/DS4Control/ControlService.cs` | 136 |
| `DS4Windows/DS4Control/DefaultActionManager.cs` | 1 |
| `DS4Windows/DS4Control/DS4LightBar.cs` | 7 |
| `DS4Windows/DS4Control/DS4OutDevices/DS4OutDeviceBasic.cs` | 1 |
| `DS4Windows/DS4Control/DS4OutDevices/DS4OutDeviceExt.cs` | 1 |
| `DS4Windows/DS4Control/DTOXml/ActionsDTO.cs` | 6 |
| `DS4Windows/DS4Control/DTOXml/AppSettingsDTO.cs` | 16 |
| `DS4Windows/DS4Control/DTOXml/AutoProfilesDTO.cs` | 4 |
| `DS4Windows/DS4Control/DTOXml/OutputSlotPersistDTO.cs` | 1 |
| `DS4Windows/DS4Control/DTOXml/ProfileDTO.cs` | 10 |
| `DS4Windows/DS4Control/Mapping.cs` | 91 |
| `DS4Windows/DS4Control/Mouse.cs` | 66 |
| `DS4Windows/DS4Control/MouseCursor.cs` | 26 |
| `DS4Windows/DS4Control/MouseWheel.cs` | 6 |
| `DS4Windows/DS4Control/OutputKBM/VirtualKBMFactory.cs` | 1 |
| `DS4Windows/DS4Control/OutputSlotManager.cs` | 3 |
| `DS4Windows/DS4Control/OutputSlotPersist.cs` | 3 |
| `DS4Windows/DS4Control/PresetOption.cs` | 10 |
| `DS4Windows/DS4Control/ProfileMigration.cs` | 4 |
| `DS4Windows/DS4Control/Services/DefaultElevatedProcessLauncher.cs` | 1 |
| `DS4Windows/DS4Control/Services/DefaultProcessInspector.cs` | 1 |
| `DS4Windows/DS4Control/Services/IElevatedProcessLauncher.cs` | 1 |
| `DS4Windows/DS4Control/Services/IProcessInspector.cs` | 1 |
| `DS4Windows/DS4Control/Services/OutputKBMHandlerAdapter.cs` | 24 |
| `DS4Windows/DS4Control/SyntheticDispatcher.cs` | 4 |
| `DS4Windows/DS4Control/Util.cs` | 3 |
| `DS4Windows/DS4Control/WindowPlacementHelper.cs` | 16 |
| `DS4Windows/DS4Control/Xbox360OutDevice.cs` | 1 |
| `DS4Windows/DS4Forms/About.xaml.cs` | 1 |
| `DS4Windows/DS4Forms/AutoProfiles.xaml.cs` | 9 |
| `DS4Windows/DS4Forms/BindingWindow.xaml.cs` | 11 |
| `DS4Windows/DS4Forms/DupBox.xaml.cs` | 2 |
| `DS4Windows/DS4Forms/LanguagePackControl.xaml.cs` | 1 |
| `DS4Windows/DS4Forms/LanguageSelectDialog.xaml.cs` | 4 |
| `DS4Windows/DS4Forms/MainWindow.xaml.cs` | 112 |
| `DS4Windows/DS4Forms/ProfileEditor.xaml.cs` | 118 |
| `DS4Windows/DS4Forms/RecordBox.xaml.cs` | 3 |
| `DS4Windows/DS4Forms/SaveWhere.xaml.cs` | 14 |
| `DS4Windows/DS4Forms/SpecialActionEditor.xaml.cs` | 1 |
| `DS4Windows/DS4Forms/UpdaterWindow.xaml.cs` | 2 |
| `DS4Windows/DS4Forms/ViewModels/AutoProfilesViewModel.cs` | 4 |
| `DS4Windows/DS4Forms/ViewModels/BindingWindowViewModel.cs` | 3 |
| `DS4Windows/DS4Forms/ViewModels/ControllerListViewModel.cs` | 50 |
| `DS4Windows/DS4Forms/ViewModels/ControllerRegDeviceOptsViewModel.cs` | 7 |
| `DS4Windows/DS4Forms/ViewModels/LanguagePackViewModel.cs` | 8 |
| `DS4Windows/DS4Forms/ViewModels/LogViewModel.cs` | 1 |
| `DS4Windows/DS4Forms/ViewModels/MainWindowsViewModel.cs` | 9 |
| `DS4Windows/DS4Forms/ViewModels/MappingListViewModel.cs` | 6 |
| `DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs` | 581 |
| `DS4Windows/DS4Forms/ViewModels/RecordBoxViewModel.cs` | 10 |
| `DS4Windows/DS4Forms/ViewModels/RenameProfileViewModel.cs` | 1 |
| `DS4Windows/DS4Forms/ViewModels/SettingsViewModel.cs` | 106 |
| `DS4Windows/DS4Forms/ViewModels/SpecialActEditorViewModel.cs` | 1 |
| `DS4Windows/DS4Forms/ViewModels/SpecialActions/CheckBatteryViewModel.cs` | 2 |
| `DS4Windows/DS4Forms/ViewModels/SpecialActions/LaunchProgramViewModel.cs` | 2 |
| `DS4Windows/DS4Forms/ViewModels/SpecialActions/LoadProfileViewModel.cs` | 1 |
| `DS4Windows/DS4Forms/ViewModels/SpecialActions/MacroViewModel.cs` | 1 |
| `DS4Windows/DS4Forms/ViewModels/SpecialActions/MultiActButtonViewModel.cs` | 1 |
| `DS4Windows/DS4Forms/ViewModels/SpecialActions/PressKeyViewModel.cs` | 14 |
| `DS4Windows/DS4Forms/ViewModels/SpecialActions/SpecialActionViewModel.cs` | 2 |
| `DS4Windows/DS4Forms/ViewModels/SpecialActionsListViewModel.cs` | 14 |
| `DS4Windows/DS4Forms/ViewModels/TouchButtonUserControlViewModel.cs` | 2 |
| `DS4Windows/DS4Forms/ViewModels/TrayIconViewModel.cs` | 18 |
| `DS4Windows/DS4Forms/ViewModels/UpdaterWindowViewModel.cs` | 2 |
| `DS4Windows/DS4Forms/WelcomeDialog.xaml.cs` | 13 |
| `DS4Windows/DS4Library/DS4Device.cs` | 3 |
| `DS4Windows/DS4Library/DS4Devices.cs` | 6 |
| `DS4Windows/DS4Library/DS4Sixaxis.cs` | 1 |
| `DS4Windows/DS4Library/DS4State.cs` | 4 |
| `DS4Windows/HidLibrary/HidDevices.cs` | 2 |
| `DS4Windows/LoggerHolder.cs` | 5 |
| `DS4Windows/ProfileEntity.cs` | 5 |
| `DS4Windows/ProfileList.cs` | 1 |
| `DS4Windows/StartupMethods.cs` | 8 |


