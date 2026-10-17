/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using DS4Windows.DS4Control;
using DS4Windows.InputDevices;
using DS4Windows.StickModifiers;
using DS4WinWPF.DS4Control.DTOXml;
using DS4WinWPF.DS4Forms.ViewModels;
using Sensorit.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;
using DS4WinWPF;
using DS4WinWPF.ApiDTO;
using WpfScreenHelper;
using static DS4Windows.Mouse;
using static DS4Windows.Util;
using LightbarMacro = DS4WinWPF.DS4Forms.ViewModels.LightbarMacro;
namespace DS4Windows
{

    public class Global
    {
        static Global()
        {
            // Static initializer retained for future Global initializations.
        }
        public static int LogMaxArchiveFiles
        {
            set { m_Config.logMaxArchiveFiles = value; }
            get { return m_Config.logMaxArchiveFiles; }
        }

        public static string LogMinLevel
        {
            set { m_Config.logMinLevel = value; }
            get { return m_Config.logMinLevel; }
        }
        // Phase7-Step10a W4: コントローラー数などの定数（MAX_DS4_CONTROLLER_COUNT／TEST_PROFILE_ITEM_COUNT／TEST_PROFILE_INDEX／
        // OLD_XINPUT_CONTROLLER_COUNT／DS4_STICK_AXIS_MIDPOINT）は ControllerLimits へ移し、別名を削除した。

        public static CultureInfo configFileDecimalCulture = new CultureInfo("en-US"); // Loading and Saving decimal values in configuration files should always use en-US decimal format (ie. dot char as decimal separator char, not comma char)

        protected static BackingStore m_Config = new BackingStore();
        public static BackingStore store => m_Config;
        // Phase7-Step4 W3: 実行ファイルの情報と設定の保存場所の実体は PathService へ移した（FindConfigLocation／SaveWhere／
        // SaveDefault、firstRun／multisavespots／appDataPpath、fileVersion も同様）。下位層（BackingStore／LoggerHolder など）が
        // 静的に読む次の 4 つは、PathService の状態を指す読み書きの窓口として残す。
        // TODO(Phase7-Step10): BackingStore の永続化を切り出すときに、下位層へ IPathService を注入して、窓口を廃止する。
        public static string exelocation => PathService.ExecutableLocationState;
        public static string exedirpath
        {
            get => PathService.ExecutableDirectoryState;
            set => PathService.ExecutableDirectoryState = value;
        }
        public static string exeversion => PathService.ExecutableVersionState;
        public static string appdatapath
        {
            get => PathService.AppDataPathState;
            set => PathService.AppDataPathState = value;
        }
        // =========================================================================
        // Phase4-Step1: IProfileSettingsService DI シム (Strangler Fig 移行用)
        // =========================================================================
        // Phase4-Step1: IProfileSettingsService DI シム (Strangler Fig 移行用)
        // =========================================================================
        private static DS4Windows.DI.IProfileSettingsService profileSettingsService = null;
        private static readonly DS4Windows.DI.IProfileSettingsService fallbackProfileSettingsService = // Phase7-Step5 W2: コンテナが無い環境（テスト）用のフォールバック。ControlService は無い（旧: Program.rootHub が null のとき）
        new ProfileSettingsService(m_Config, new EnvironmentService(), () => null);

        public static DS4Windows.DI.IProfileSettingsService ProfileSettingsServiceInstance
        {
            get
            {
                if (profileSettingsService != null)
                    return profileSettingsService;

                try
                {
                    var service = DS4WinWPF.AppHost.GetService<DS4Windows.DI.IProfileSettingsService>();
                    if (service != null)
                    {
                        profileSettingsService = service;
                        return profileSettingsService;
                    }
                }
                catch
                {
                    // DIコンテナ未初期化時の安全なフォールバック
                }

                AppLogger.LogTrace("[Legacy] Global.ProfileSettingsServiceInstance: Fallback instance used");
                AppLogger.LogToGui("[Legacy] Global.ProfileSettingsServiceInstance: Fallback instance used", false, true);
                return fallbackProfileSettingsService;
            }
            set => profileSettingsService = value;
        }

        // Phase7-Step5 W3（Q7＝A）: ProfileXmlStoreInstance（本体の使用者が 0 件。テストだけが使っていた）を削除した。
        // NotificationServiceInstance は AppLogger.NotificationService（App が起動時に設定する静的プロパティ）へ移した。
        // ProfileSettingsServiceInstance は、Global の静的メンバー・BackingStore・Mapping が使うため、Step7〜9／Step10 まで残す。

        public static bool[] useTempProfile
        {
            get
            {
                return ProfileSettingsServiceInstance.UseTempProfileArray;
            }
            set
            {
                AppLogger.LogTrace("[Legacy] Global.useTempProfile setter accessed via static shim");
                AppLogger.LogToGui("[Legacy] Global.useTempProfile setter accessed via static shim", false, true);
                ProfileSettingsServiceInstance.UseTempProfileArray = value;
            }
        }
        public static bool[] tempprofileDistance
        {
            get
            {
                return ProfileSettingsServiceInstance.TempProfileDistanceArray;
            }
            set
            {
                AppLogger.LogTrace("[Legacy] Global.tempprofileDistance setter accessed via static shim");
                AppLogger.LogToGui("[Legacy] Global.tempprofileDistance setter accessed via static shim", false, true);
                ProfileSettingsServiceInstance.TempProfileDistanceArray = value;
            }
        }
        public static bool[] useDInputOnly
        {
            get
            {
                return ProfileSettingsServiceInstance.UseDInputOnlyArray;
            }
            set
            {
                AppLogger.LogTrace("[Legacy] Global.useDInputOnly setter accessed via static shim");
                AppLogger.LogToGui("[Legacy] Global.useDInputOnly setter accessed via static shim", false, true);
                ProfileSettingsServiceInstance.UseDInputOnlyArray = value;
            }
        }
        // (Removed) Previously used to hold pending removed invalid special actions
        // collected by UI-time calls. This mechanism was refactored so that
        // removal detection and logging happen at save/apply time; the array has
        // therefore been removed to avoid dead code.
        // Phase7-Step6 W2: 出力デバイス種別の配列（旧 outDevTypeTemp＝編集画面の未確定値、旧 activeOutDevType＝スロットごとの接続中の種別）は
        // OutputSlotService へ移した（IOutputSlotService.OutDevTypeTemp／ActiveOutDevType）。

        // Phase7-Step4 W1: ViGEmBus／HidHide／FakerInput の導入状態（vigemInstalled／vigemBusVersionInfo／hidHideInstalled／
        // fakerInputInstalled／fakerInputVersion と関連する定数）と、その調べ物は EnvironmentService へ移した。

        // Phase7-Step8 W3（Q6＝A）: 出力 KBM ハンドラの実体（旧 outputKBMHandler）は OutputKBMHandlerAdapter.Handler へ、
        // KBM マッピング（旧 outputKBMMapping）は IProfileSettingsService.OutputKBMMapping を直接使う形にした
        // （決定・破棄・フォールバックの操作は OutputKBMHandlerLifecycle）。

        // Phase7-Step10c W5: 実体は LegacyProfileXmlSerializer.ConfigVersion へ移した。以下は同じ値を指す別名（ProfileMigration が使う）。
        // TODO(Phase7-Step10d): 利用箇所をなくしたら、この別名を削除する。
        public const int CONFIG_VERSION = Services.LegacyProfileXmlSerializer.ConfigVersion;
        public const int APP_CONFIG_VERSION = 2;
        // Phase7-Step10a W3・W4: 資源パスの定数（ASSEMBLY_RESOURCE_PREFIX／RESOURCES_PREFIX）は画面側の ResourcePaths へ移し、削除した。
        // Need to add additional probing path in code starting with .NET 6.
        public const string PROBING_PATH = "Lang";
        public const string LANGUAGE_ASSEMBLY_NAME = "DS4Windows.resources.dll";
        // Phase7-Step10c W3: 実体は AppSettingsXmlSerializer.CustomExeConfigFileName へ移した。以下は同じ値を指す別名（テストが使う）。
        // TODO(Phase7-Step10d): 利用箇所をなくしたら、この別名を削除する。
        public const string CUSTOM_EXE_CONFIG_FILENAME = Services.AppSettingsXmlSerializer.CustomExeConfigFileName;
        // Phase7-Step10c W5: 実体は LegacyProfileXmlSerializer.XmlExtension へ移した（別名。TODO(Phase7-Step10d): 利用箇所がなくなったら削除）。
        public const string XML_EXTENSION = Services.LegacyProfileXmlSerializer.XmlExtension;

        // Phase7-Step10b W1: 既定のボタン割り当て・名前の表・getX360ControlString は X360ControlTables へ、
        // ds4inputNames／macroDS4Values は DS4ControlNames へ移し、Global から削除した（宣言は移動のみで、中身は同一）。

        // Phase7-Step10b W2: iconChoiceResources（トレイアイコンの表）は画面側の TrayIconResources へ移し、Global から削除した。

        /// <summary>
        /// Check if Admin Rights are needed to write in Appliplation Directory
        /// </summary>
        /// <returns></returns>
        public static bool AdminNeeded()
        {
            try
            {
                File.WriteAllText(exedirpath + "\\test.txt", "test");
                File.Delete(exedirpath + "\\test.txt");
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }

        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        internal static string GetStringDeviceProperty(string deviceInstanceId,
            NativeMethods.DEVPROPKEY prop)
        {
            string result = string.Empty;
            NativeMethods.SP_DEVINFO_DATA deviceInfoData = new NativeMethods.SP_DEVINFO_DATA();
            deviceInfoData.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(deviceInfoData);
            ulong propertyType = 0;
            var requiredSize = 0;

            Guid hidGuid = new Guid();
            NativeMethods.HidD_GetHidGuid(ref hidGuid);
            //IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(IntPtr.Zero, deviceInstanceId, 0, extraFlags | NativeMethods.DIGCF_DEVICEINTERFACE | NativeMethods.DIGCF_ALLCLASSES);
            IntPtr deviceInfoSet = NativeMethods.SetupDiCreateDeviceInfoList(IntPtr.Zero, 0);
            //NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, 0, ref deviceInfoData);
            NativeMethods.SetupDiOpenDeviceInfo(deviceInfoSet, deviceInstanceId, IntPtr.Zero, 0, ref deviceInfoData);
            NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData, ref prop, ref propertyType,
                    null, 0, ref requiredSize, 0);

            if (requiredSize > 0)
            {
                byte[] dataBuffer = new byte[requiredSize];
                NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData, ref prop, ref propertyType,
                    dataBuffer, dataBuffer.Length, ref requiredSize, 0);

                result = dataBuffer.ToUTF16String();
            }

            if (deviceInfoSet.ToInt64() != NativeMethods.INVALID_HANDLE_VALUE)
            {
                NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return result;
        }

        public static string GetInstanceIdFromDevicePath(string devicePath)
        {
            string result = string.Empty;
            uint requiredSize = 0;
            NativeMethods.CM_Get_Device_Interface_Property(devicePath, ref NativeMethods.DEVPKEY_Device_InstanceId, out _, null, ref requiredSize, 0);
            if (requiredSize > 0)
            {
                byte[] buffer = new byte[requiredSize];
                NativeMethods.CM_Get_Device_Interface_Property(devicePath, ref NativeMethods.DEVPKEY_Device_InstanceId, out _, buffer, ref requiredSize, 0);
                result = buffer.ToUTF16String();
            }

            return result;
        }

        internal static string[] GetStringArrayDeviceProperty(string deviceInstanceId,
            NativeMethods.DEVPROPKEY prop)
        {
            string[] result = null;
            NativeMethods.SP_DEVINFO_DATA deviceInfoData = new NativeMethods.SP_DEVINFO_DATA();
            deviceInfoData.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(deviceInfoData);
            ulong propertyType = 0;
            var requiredSize = 0;

            IntPtr zero = IntPtr.Zero;
            //IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(zero, deviceInstanceId, 0, extraFlags | NativeMethods.DIGCF_DEVICEINTERFACE | NativeMethods.DIGCF_ALLCLASSES);
            IntPtr deviceInfoSet = NativeMethods.SetupDiCreateDeviceInfoList(IntPtr.Zero, 0);
            //NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, 0, ref deviceInfoData);
            NativeMethods.SetupDiOpenDeviceInfo(deviceInfoSet, deviceInstanceId, IntPtr.Zero, 0, ref deviceInfoData);
            NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData, ref prop, ref propertyType,
                    null, 0, ref requiredSize, 0);

            if (requiredSize > 0)
            {
                byte[] dataBuffer = new byte[requiredSize];
                NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData, ref prop, ref propertyType,
                    dataBuffer, dataBuffer.Length, ref requiredSize, 0);

                string tempStr = Encoding.Unicode.GetString(dataBuffer);
                string[] hardwareIds = tempStr.TrimEnd(new char[] { '\0', '\0' }).Split('\0');
                result = hardwareIds;
            }

            if (deviceInfoSet.ToInt64() != NativeMethods.INVALID_HANDLE_VALUE)
            {
                NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return result;
        }

        public static bool CheckIfVirtualDevice(string devicePath)
        {
            bool result = false;
            bool excludeMatchFound = false;

            var instanceId = GetInstanceIdFromDevicePath(devicePath);
            var testInstanceId = instanceId;
            while (!string.IsNullOrEmpty(testInstanceId))
            {
                var hardwareIds = GetStringArrayDeviceProperty(testInstanceId, NativeMethods.DEVPKEY_Device_HardwareIds);
                if (hardwareIds != null)
                {
                    // hardware IDs of root hubs/controllers that emit supported virtual devices as sources
                    var excludedIds = new[]
                    {
                        @"ROOT\HIDGAMEMAP", // reWASD
                        @"ROOT\VHUSB3HC", // VirtualHere
                    };

                    excludeMatchFound = hardwareIds.Any(id => excludedIds.Contains(id.ToUpper()));
                    if (excludeMatchFound)
                    {
                        break;
                    }
                }

                // Check for potential non-present device as well
                string parentInstanceId = GetStringDeviceProperty(testInstanceId, NativeMethods.DEVPKEY_Device_Parent);

                // Found root enumerator. Use instanceId of device one layer lower in final check
                if (parentInstanceId.Equals(@"HTREE\ROOT\0", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                testInstanceId = parentInstanceId;
            }

            if (!excludeMatchFound &&
                !string.IsNullOrEmpty(testInstanceId) &&
                (testInstanceId.StartsWith(@"ROOT\SYSTEM", StringComparison.OrdinalIgnoreCase)
                || testInstanceId.StartsWith(@"ROOT\USB", StringComparison.OrdinalIgnoreCase)))
            {
                result = true;
            }

            return result;
        }

        public static void SetCulture(string culture)
        {
            try
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo(culture);
            }
            catch { /* Skip setting culture that we cannot set */ }
        }

        // Phase7-Step10c W2: CreateStdActions は、唯一の呼び出し元の SpecialActionRepository.CreateStandardActions へ移した（内容は同一）。

        public static bool CreateAutoProfiles(string m_Profile)
        {
            bool Saved = true;

            try
            {
                XmlNode Node;
                XmlDocument doc = new XmlDocument();

                Node = doc.CreateXmlDeclaration("1.0", "utf-8", String.Empty);
                doc.AppendChild(Node);

                Node = doc.CreateComment(string.Format(" Auto-Profile Configuration Data. {0} ", DateTime.Now));
                doc.AppendChild(Node);

                Node = doc.CreateWhitespace("\r\n");
                doc.AppendChild(Node);

                Node = doc.CreateNode(XmlNodeType.Element, "Programs", "");
                doc.AppendChild(Node);
                doc.Save(m_Profile);
            }
            catch { Saved = false; }

            return Saved;
        }

        public static event EventHandler<SerialChangeArgs> DeviceSerialChange;
        public static void OnDeviceSerialChange(object sender, int index, string serial)
        {
            if (DeviceSerialChange != null)
            {
                SerialChangeArgs args = new SerialChangeArgs(index, serial);
                DeviceSerialChange(sender, args);
            }
        }

        // Phase7-Step10c W3: CompileVersionNumberFromString／CompileVersionNumber は VersionUtil へ移し、Global から削除した（本体は同一）。

        // general values
        // -- Re-Enable Exclusive Mode Starts Here --
        // New explicit startup update-check settings
        // 0 = hours, 1 = days
        // Legacy `CheckWhen` removed. Use explicit CheckEveryValue/CheckEveryUnit instead.

        // Phase7-Step3 W4: 読み取り専用にした（代入と番号の算出は AppSettingsService.LastVersionChecked が担う）。
        // Changelog が読むため、Phase7 の後続 Step まで残す。
        public static string LastVersionChecked => m_Config.lastVersionChecked;

        public static int Notifications
        {
            set { m_Config.notifications = value; }
            get { return m_Config.notifications; }
        }

        public static bool UseMoonlight
        {
            get => m_Config.useMoonlight;
            set => m_Config.useMoonlight = value;
        }

        public static bool UseAdvancedMoonlight
        {
            get => m_Config.useAdvancedMoonlight;
            set => m_Config.useAdvancedMoonlight = value;
        }

        public static string UseLang
        {
            set { m_Config.useLang = value; }
            get { return m_Config.useLang; }
        }

        public static int getOSCServerPortNum()
        {
            return m_Config.oscServPort;
        }
        public static event EventHandler<byte> BatteryChanged;

        public static void InvokeBatteryChanged(byte percentage)
        {
            BatteryChanged?.Invoke(typeof(Global), percentage);
        }

        public static AppThemeChoice UseCurrentTheme
        {
            get => m_Config.useCurrentTheme;
            set => m_Config.useCurrentTheme = value;
        }

        public static int[] DebouncingMs => ProfileSettingsServiceInstance.DebouncingMs;

        public static bool UseDs3PitchRollSim
        {
            get => ProfileSettingsServiceInstance.UseDs3PitchRollSim;
            set => ProfileSettingsServiceInstance.UseDs3PitchRollSim = value;
        }

        // controller/profile specfic values
        public static int getIdleDisconnectTimeout(int index)
        {
            return ProfileSettingsServiceInstance.GetIdleDisconnectTimeout(index);
        }

        public static LightbarSettingInfo getLightbarSettingsInfo(int index)
        {
            return ProfileSettingsServiceInstance.GetLightbarSettingsInfo(index);
        }


        public static SASteeringWheelEmulationAxisType GetSASteeringWheelEmulationAxis(int index)
        {
            return ProfileSettingsServiceInstance.GetSASteeringWheelEmulationAxis(index);
        }

        //public static DS4Color[] MainColor => m_Config.m_Leds;
        public static ref DS4Color getMainColor(int index)
        {
            return ref ProfileSettingsServiceInstance.GetMainColor(index);
            //return ref m_Config.m_Leds[index];
        }

        public static ControlServiceDeviceOptions DeviceOptions => m_Config.deviceOptions;

        public static OutContType[] OutContType => m_Config.outputDevType;
        public static bool[] DistanceProfiles = m_Config.distanceProfiles;


        // Phase7-Step10c W2: SaveAction／SaveActions／RemoveAction／ReloadActionsAndVerify／LoadActions（Actions.xml の保存・読込の薄いシム）は削除した。
        // 保存・読込は SpecialActionsXmlSerializer、実行中のアクション状態のクリアと読み直しの検証は SpecialActionRepository へ移した。
        // （SaveActions／LoadActions の呼び出しは、移動前から SpecialActionRepository 経由かテストだけだった。）

        public static int GetActionIndexOf(string name)
        {
            return m_Config.GetActionIndexOf(name);
        }

        public static SpecialAction GetAction(string name)
        {
            return m_Config.GetAction(name);
        }

        // Phase7-Step10b W1: 1 引数の getX360ControlString（XML 保存用の名前）は X360ControlTables へ移した。

        // Phase7-Step5 W2（Q8＝A）: SaveControllerConfigs／LoadControllerConfigs を削除した。device を渡す経路は ProfileXmlStore が
        // BackingStore を直接呼ぶ。引数なし（全コントローラーの走査。Program.rootHub を使う）は呼び出しが 0 件だった。

        // Phase7-Step10b W2: getTransitionedColor／applyRatio／HuetoRGB（色の純粋関数）は ColorUtil へ移し、Global から削除した（本体は同一）。

        // Phase7-Step10a W4: Global.Clamp は NumericUtil.Clamp へ移し、削除した。

        private static int ClampInt(int min, int value, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        // Phase7-Step8 W3: 旧 InitOutputKBMHandler／InitOutputKBMMapping は OutputKBMHandlerLifecycle へ移した（挙動・ログは同一）。

        /// <summary>
        /// Take Windows virtual key value and refresh action alias for currently used output KB+M system
        /// </summary>
        /// <param name="setting">Instance of edited DS4ControlSettings object</param>
        /// <param name="shift">Flag to indicate if shift action is being modified</param>
        public static void RefreshActionAlias(DS4ControlSettings setting, bool shift)
        {
            if (!shift)
            {
                setting.action.actionAlias = 0;
                if (setting.actionType == DS4ControlSettings.ActionType.Key)
                {
                    setting.action.actionAlias = ProfileSettingsServiceInstance.OutputKBMMapping.GetRealEventKey(Convert.ToUInt32(setting.action.actionKey));
                }
            }
            else
            {
                setting.shiftAction.actionAlias = 0;
                if (setting.shiftActionType == DS4ControlSettings.ActionType.Key)
                {
                    setting.shiftAction.actionAlias = ProfileSettingsServiceInstance.OutputKBMMapping.GetRealEventKey(Convert.ToUInt32(setting.shiftAction.actionKey));
                }
            }
        }

    }
}

