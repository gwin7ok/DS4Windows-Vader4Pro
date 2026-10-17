using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows.DI;

namespace DS4Windows
{
    /// <summary>
    /// <see cref="IEnvironmentService"/> の実装。
    ///
    /// 【Phase5-Step14 FormSettings統一】
    /// 以前は本クラスが RunAtStartup / StartMinimized / CloseMinimizes / UseLang /
    /// FormWidth / FormHeight / FormLocationX / FormLocationY を、Global/m_Config(BackingStore)
    /// と一切連動しない独自の private field として孤立保持していた。これは
    /// <see cref="Services.AppSettingsService"/> が正規に保持する同名設定と意味的に重複しており、
    /// 実害はなかったものの将来の誤接続を誘発する地雷であったため撤去した。
    /// 永続設定は必ず <see cref="IAppSettingsService"/> 経由でアクセスすること。
    ///
    /// 【Phase7-Step4 W1】
    /// ViGEmBus／HidHide／FakerInput の導入状態の調べ物（SetupAPI の列挙）と、その結果の保持を <c>Global</c> から本クラスへ移した。
    /// 調べた結果はプロセス全体で 1 つの状態（起動前の App、ControlService、WelcomeDialog などが同じ値を見る）のため、
    /// 本クラスの static フィールドとして持つ。インスタンスはどれを使っても同じ状態を読み書きする。
    /// </summary>
    public class EnvironmentService : IEnvironmentService
    {
        // 読み取り専用のシステム状態プローブ／実行時操作であり、永続化設定ではない。
        // 管理者権限・書き込み権限・実行ファイルのバージョン・デバイスパスの変換は、他の層（BackingStore／Util／DS4Devices）も
        // 静的に呼ぶため Global に残している（TODO(Phase7-Step4 W3／Step10): パスの所有の移管と、下位層への注入で解消する）。
        public bool IsAdministrator() => Global.IsAdministrator();

        public string ApplicationVersion => Global.exeversion;

        // ---- Phase7-Step4 W1: 導入状態の保持（旧 Global の static フィールド）----
        // ViGEmBus の既定値は「未導入」。RefreshViGEmBusInfo で更新される。
        private const string BlankViGEmBusVersion = "0.0.0.0";
        private const string MinSupportedViGEmBusVersion = "1.16.112.0";
        private const string MinTouchpadPassthruViGEmBusVersion = "1.17.333.0";
        private const string ViGEmBusGuid = "{96E42B22-F5E9-42F8-B043-ED0F932F014F}";
        private const string BlankFakerInputVersion = "0.0.0.0";

        private static bool s_vigemInstalled = false;
        private static Version s_vigemBusVersionInfo = new Version(BlankViGEmBusVersion);
        private static readonly Version s_minSupportedViGEmBusVersionInfo = new Version(MinSupportedViGEmBusVersion);

        // HidHide／FakerInput は、旧 Global では静的初期化の時点で調べていた。ここでは静的初期化の循環（下記）を避けるため、
        // 最初に読まれたときに調べる（値は同じ。以降は Refresh…Info で更新される）。
        private static bool? s_hidHideInstalled;
        private static bool? s_fakerInputInstalled;
        private static string s_fakerInputVersion;

        /// <summary>HidHide の導入状態（Refresh で更新されるキャッシュ値）。</summary>
        public static bool HidHideInstalledState
        {
            get => s_hidHideInstalled ?? (s_hidHideInstalled = CheckForSysDevice(@"root\HidHide")).Value;
            set => s_hidHideInstalled = value;
        }

        /// <summary>FakerInput の導入状態（Refresh で更新されるキャッシュ値）。VirtualKBMFactory が読む。</summary>
        public static bool FakerInputInstalledState
        {
            get => s_fakerInputInstalled ?? (s_fakerInputInstalled = CheckForSysDevice(@"root\FakerInput")).Value;
            set => s_fakerInputInstalled = value;
        }

        /// <summary>FakerInput の導入バージョン（Refresh で更新されるキャッシュ値）。</summary>
        public static string FakerInputVersionState
        {
            get => s_fakerInputVersion ?? (s_fakerInputVersion = QueryFakerInputVersion());
            set => s_fakerInputVersion = value;
        }

        /// <summary>導入済みの ViGEmBus のバージョン（未導入のときは 0.0.0.0）。</summary>
        public static Version ViGEmBusVersionState => s_vigemBusVersionInfo;

        public void RefreshHidHideInfo() => HidHideInstalledState = IsHidHideInstalled();

        public void RefreshFakerInputInfo()
        {
            FakerInputInstalledState = IsFakerInputInstalled();
            FakerInputVersionState = QueryFakerInputVersion();
        }

        // ---- Phase6-Step2-1 (PR-1): ControlService の Global 直接参照解消 ----
        public bool HidHideInstalled => HidHideInstalledState;

        public string GetInstanceIdFromDevicePath(string devicePath) =>
            Global.GetInstanceIdFromDevicePath(devicePath);

        public bool CheckHidHideAffectedStatus(string deviceInstanceId,
            HashSet<string> affectedDevs,
            HashSet<string> exemptedDevices, bool force = false)
        {
            string tempDeviceInstanceId = deviceInstanceId.ToUpper();
            return affectedDevs.Contains(tempDeviceInstanceId);
        }

        // ---- Phase6-Step2-1b (決定O2=C): コントローラースロット上限 ----
        // 旧 ControlService.CURRENT_DS4_CONTROLLER_LIMIT の計算（純粋な OS 判定＋ビルド定義）をここへ移設した。
        // TODO(技術的負債): ControlService の static 互換シム（CURRENT_DS4_CONTROLLER_LIMIT / USING_MAX_CONTROLLERS）が
        // 本値を共有するため static のまま保持している。互換シム撤去後（Phase6-Step12）はインスタンスフィールドへ戻す。
        internal static readonly int ProcessControllerSlotLimit = CalculateControllerSlotLimit();

        public int ControllerSlotLimit => ProcessControllerSlotLimit;

        public bool UsingMaxControllers => ProcessControllerSlotLimit == ControlService.EXPANDED_CONTROLLER_COUNT;

        // ---- Phase6-Step2-3 (PR-3): FakerInput 導入バージョン ----
        public string FakerInputVersion => FakerInputVersionState;

        // ---- Phase6-Step9-1／Phase7-Step4 W1: ViGEmBus の導入状態 ----
        public bool IsViGEmBusInstalled() => s_vigemInstalled;

        public bool IsRunningSupportedViGEmBus() =>
            s_vigemInstalled && s_minSupportedViGEmBusVersionInfo.CompareTo(s_vigemBusVersionInfo) <= 0;

        public bool IsUsingMinViGEm117333() =>
            s_vigemBusVersionInfo.CompareTo(new Version(MinTouchpadPassthruViGEmBusVersion)) >= 0;

        public bool AdminNeeded() => Global.AdminNeeded();

        // ---- Phase6-Step10-1-1／Phase7-Step4 W1: OS・ドライバーの判定 ----
        // 静的初期化の循環を起こさないこと（ガード: EnvironmentServiceStaticInitGuardTests）。本クラスは Global の静的メンバーを
        // 静的初期化の中で参照しない（メソッド本体の中でのみ、Global の移管前のメンバーを参照する）。
        public bool IsWin10OrGreater() => System.Environment.OSVersion.Version.Major >= 10;

        public bool IsWin8OrGreater() => IsWin8OrGreaterCore();

        public bool IsFakerInputInstalled() => CheckForSysDevice(@"root\FakerInput");

        public bool IsHidHideInstalled() => CheckForSysDevice(@"root\HidHide");

        // Phase6-Step10-4-3a: OutputSlotManager が都度読み取る（RefreshViGEmBusInfo で更新されるため、キャッシュしない）
        public Version ViGEmBusVersion => s_vigemBusVersionInfo;

        public void RefreshViGEmBusInfo() => FindViGEmDeviceInfo();

        // 静的初期化の循環を避けるため、この計算は Global のメソッド（Global.IsWin8OrGreater 等）を呼ばず自己完結させる。
        // Global は明示的な静的コンストラクタを持ち、その初期化中に OutputSlotManager（→ ControlService.CURRENT_DS4_CONTROLLER_LIMIT
        // → 本クラスの ProcessControllerSlotLimit）が生成される。本クラスの静的初期化から Global の静的初期化を起こすと、
        // 初期化の順序（EnvironmentService が最初に触れられた場合）によって、循環の途中の値 0 が ControlService.CURRENT_DS4_CONTROLLER_LIMIT
        // に確定してしまう。const（Global.MAX_DS4_CONTROLLER_COUNT 等）は定数として埋め込まれるため、Global の静的初期化を起こさない。
        // 回帰ガード: EnvironmentServiceStaticInitGuardTests
        private static int CalculateControllerSlotLimit()
        {
#if FORCE_4_INPUT
            return ControllerLimits.OldXInputControllerCount;
#else
            return IsWin8OrGreaterCore() ? ControllerLimits.MaxControllerCount : ControllerLimits.OldXInputControllerCount;
#endif
        }

        // Phase7-Step4 W1: 旧 Global.IsWin8OrGreater（削除済み）の判定。静的初期化の中からも呼ぶため、Global を経由しない。
        private static bool IsWin8OrGreaterCore()
        {
            bool result = false;
            if (System.Environment.OSVersion.Version.Major > 6)
            {
                result = true;
            }
            else if (System.Environment.OSVersion.Version.Major == 6 &&
                System.Environment.OSVersion.Version.Minor >= 2)
            {
                result = true;
            }

            return result;
        }

        // ---- Phase7-Step4 W1: 旧 Global の ViGEmBus／システムデバイスの調べ物（処理は移しただけで、変更していない）----

        private class ViGEmBusInfo
        {
            public string instanceId;
            public string deviceName;
            public string deviceVersionStr;
            public Version deviceVersion;
            public string manufacturer;
            public string driverProviderName;
        }

        private static void FindViGEmDeviceInfo()
        {
            bool result = false;
            Guid deviceGuid = Guid.Parse(ViGEmBusGuid);
            NativeMethods.SP_DEVINFO_DATA deviceInfoData =
                new NativeMethods.SP_DEVINFO_DATA();
            deviceInfoData.cbSize =
                System.Runtime.InteropServices.Marshal.SizeOf(deviceInfoData);

            var dataBuffer = new byte[4096];
            ulong propertyType = 0;
            var requiredSize = 0;

            // Properties to retrieve
            NativeMethods.DEVPROPKEY[] lookupProperties = new NativeMethods.DEVPROPKEY[]
            {
                NativeMethods.DEVPKEY_Device_DriverVersion, NativeMethods.DEVPKEY_Device_InstanceId,
                NativeMethods.DEVPKEY_Device_Manufacturer, NativeMethods.DEVPKEY_Device_Provider,
                NativeMethods.DEVPKEY_Device_DeviceDesc,
            };

            List<ViGEmBusInfo> tempViGEmBusInfoList = new List<ViGEmBusInfo>();

            IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(ref deviceGuid, null, 0,
                NativeMethods.DIGCF_DEVICEINTERFACE);
            for (int i = 0; !result && NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, i, ref deviceInfoData); i++)
            {
                ViGEmBusInfo tempBusInfo = new ViGEmBusInfo();

                foreach (NativeMethods.DEVPROPKEY currentDevKey in lookupProperties)
                {
                    NativeMethods.DEVPROPKEY tempKey = currentDevKey;
                    if (NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData,
                        ref tempKey, ref propertyType,
                        dataBuffer, dataBuffer.Length, ref requiredSize, 0))
                    {
                        string temp = dataBuffer.ToUTF16String();
                        if (currentDevKey.fmtid == NativeMethods.DEVPKEY_Device_DriverVersion.fmtid &&
                            currentDevKey.pid == NativeMethods.DEVPKEY_Device_DriverVersion.pid)
                        {
                            try
                            {
                                tempBusInfo.deviceVersion = new Version(temp);
                                tempBusInfo.deviceVersionStr = temp;
                            }
                            catch (ArgumentException)
                            {
                                // Default to unknown version
                                tempBusInfo.deviceVersionStr = BlankViGEmBusVersion;
                                tempBusInfo.deviceVersion = new Version(tempBusInfo.deviceVersionStr);
                            }
                        }
                        else if (currentDevKey.fmtid == NativeMethods.DEVPKEY_Device_InstanceId.fmtid &&
                            currentDevKey.pid == NativeMethods.DEVPKEY_Device_InstanceId.pid)
                        {
                            tempBusInfo.instanceId = temp;
                        }
                        else if (currentDevKey.fmtid == NativeMethods.DEVPKEY_Device_Manufacturer.fmtid &&
                            currentDevKey.pid == NativeMethods.DEVPKEY_Device_Manufacturer.pid)
                        {
                            tempBusInfo.manufacturer = temp;
                        }
                        else if (currentDevKey.fmtid == NativeMethods.DEVPKEY_Device_Provider.fmtid &&
                            currentDevKey.pid == NativeMethods.DEVPKEY_Device_Provider.pid)
                        {
                            tempBusInfo.driverProviderName = temp;
                        }
                        else if (currentDevKey.fmtid == NativeMethods.DEVPKEY_Device_DeviceDesc.fmtid &&
                            currentDevKey.pid == NativeMethods.DEVPKEY_Device_DeviceDesc.pid)
                        {
                            tempBusInfo.deviceName = temp;
                        }
                    }
                }

                tempViGEmBusInfoList.Add(tempBusInfo);
            }

            if (deviceInfoSet.ToInt64() != NativeMethods.INVALID_HANDLE_VALUE)
            {
                NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            // Iterate over list and find most recent version number
            Version latestKnown = new Version(BlankViGEmBusVersion);
            string deviceInstanceId = string.Empty;
            foreach (ViGEmBusInfo item in tempViGEmBusInfoList)
            {
                if (latestKnown.CompareTo(item.deviceVersion) <= 0)
                {
                    latestKnown = item.deviceVersion;
                    deviceInstanceId = item.instanceId;
                }
            }

            // Get bus info for most recent version found and save info
            ViGEmBusInfo latestBusInfo =
                tempViGEmBusInfoList.SingleOrDefault(item => item.instanceId == deviceInstanceId);
            PopulateFromViGEmBusInfo(latestBusInfo);
        }

        private static void PopulateFromViGEmBusInfo(ViGEmBusInfo busInfo)
        {
            if (busInfo != null)
            {
                s_vigemInstalled = true;
                s_vigemBusVersionInfo = busInfo.deviceVersion;
            }
            else
            {
                s_vigemInstalled = false;
                s_vigemBusVersionInfo = new Version(BlankViGEmBusVersion);
            }
        }

        private static bool CheckForSysDevice(string searchHardwareId)
        {
            bool result = false;
            Guid sysGuid = Guid.Parse("{4d36e97d-e325-11ce-bfc1-08002be10318}");
            NativeMethods.SP_DEVINFO_DATA deviceInfoData =
                new NativeMethods.SP_DEVINFO_DATA();
            deviceInfoData.cbSize =
                System.Runtime.InteropServices.Marshal.SizeOf(deviceInfoData);
            var dataBuffer = new byte[4096];
            ulong propertyType = 0;
            var requiredSize = 0;
            IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(ref sysGuid, null, 0, 0);
            for (int i = 0; !result && NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, i, ref deviceInfoData); i++)
            {
                NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData,
                    ref NativeMethods.DEVPKEY_Device_HardwareIds, ref propertyType,
                    null, 0, ref requiredSize, 0);

                if (requiredSize > 0)
                {
                    NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData,
                        ref NativeMethods.DEVPKEY_Device_HardwareIds, ref propertyType,
                        dataBuffer, dataBuffer.Length, ref requiredSize, 0);

                    string hardwareId = dataBuffer.ToUTF16String();
                    if (hardwareId.Equals(searchHardwareId))
                        result = true;
                }
            }

            if (deviceInfoSet.ToInt64() != NativeMethods.INVALID_HANDLE_VALUE)
            {
                NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return result;
        }

        private static string QueryFakerInputVersion()
        {
            // Start with the blank version for result
            string result = BlankFakerInputVersion;
            IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(ref Util.fakerInputGuid, null, 0, NativeMethods.DIGCF_DEVICEINTERFACE);
            NativeMethods.SP_DEVINFO_DATA deviceInfoData = new NativeMethods.SP_DEVINFO_DATA();
            deviceInfoData.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(deviceInfoData);
            bool foundDev = false;
            for (int i = 0; !foundDev && NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, i, ref deviceInfoData); i++)
            {
                ulong devPropertyType = 0;
                int requiredSizeProp = 0;
                NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData,
                    ref NativeMethods.DEVPKEY_Device_DriverVersion, ref devPropertyType, null, 0, ref requiredSizeProp, 0);

                if (requiredSizeProp > 0)
                {
                    var versionTextBuffer = new byte[requiredSizeProp];
                    NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData,
                        ref NativeMethods.DEVPKEY_Device_DriverVersion, ref devPropertyType, versionTextBuffer, requiredSizeProp, ref requiredSizeProp, 0);

                    string tmpitnow = System.Text.Encoding.Unicode.GetString(versionTextBuffer);
                    string tempStrip = tmpitnow.TrimEnd('\0');
                    foundDev = true;
                    result = tempStrip;
                }
            }

            if (deviceInfoSet.ToInt64() != NativeMethods.INVALID_HANDLE_VALUE)
            {
                NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return result;
        }
    }
}
