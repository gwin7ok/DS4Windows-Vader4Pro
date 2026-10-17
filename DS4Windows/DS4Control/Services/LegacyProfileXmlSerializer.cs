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

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase7-Step10c W4・W5（Q1＝A）: プロファイル（旧形式の XML）の保存・読込。
    /// 旧: <c>BackingStore</c> の <c>LoadProfile</c>／<c>LoadLegacyExtraSettings</c>／<c>ReadLegacyElement</c>／<c>axisOutputCurveId</c>／<c>OutContDeviceId</c>／
    /// <c>GetLightbarModeType</c>／<c>PortOldGyroSettings</c>（W4。読込側）と、保存側（W5）。移動のみで、ロジック・ログ・要素の順序は変えない。
    /// <c>BackingStore</c> の公開状態を <c>store.</c> で読み書きする。プロファイルの置き場所は <see cref="DS4Windows.DI.IPathService"/>、
    /// 実行時の「DInput のみ」の配列は <see cref="DS4Windows.DI.IProfileSettingsService"/> から得る。
    /// 排他は呼び出し側（<c>ProfileXmlStore.XmlIoLock</c>）が行う。
    /// </summary>
    public class LegacyProfileXmlSerializer
    {
        // プロファイル XML の設定バージョンと拡張子（旧: ConfigVersion／XML_EXTENSION。Global の同名の定数は、この値を指す別名）
        public const int ConfigVersion = 5;
        public const string XmlExtension = ".xml";

        private readonly BackingStore store;
        private readonly DS4Windows.DI.IPathService _pathService;
        private readonly DS4Windows.DI.IProfileSettingsService _profileSettings;
        private readonly DS4Windows.DI.IEnvironmentService _environment;
        private readonly XmlDocument m_Xdoc = new XmlDocument(); // 旧 BackingStore.m_Xdoc と同じ使い方（排他は呼び出し側のロック）

        public LegacyProfileXmlSerializer(BackingStore store, DS4Windows.DI.IPathService pathService, DS4Windows.DI.IProfileSettingsService profileSettings,
            DS4Windows.DI.IEnvironmentService environment)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
        }

        private int axisOutputCurveId(string name)
        {
            return store.stickOutputCurveId(name);
        }

        private OutContType OutContDeviceId(string name)
        {
            OutContType id = OutContType.X360;
            switch (name)
            {
                case "None":
                case "X360": id = OutContType.X360; break;
                case "DS4": id = OutContType.DS4; break;
                default: break;
            }

            return id;
        }

        private void PortOldGyroSettings(int device)
        {
            if (store.gyroOutMode[device] == GyroOutMode.None)
            {
                store.gyroOutMode[device] = GyroOutMode.Controls;
            }
        }

        private LightbarMode GetLightbarModeType(string modeString)
        {
            LightbarMode result = LightbarMode.DS4Win;
            switch (modeString)
            {
                case "DS4Win":
                    result = LightbarMode.DS4Win;
                    break;
                case "Passthru":
                    result = LightbarMode.Passthru;
                    break;
                default:
                    break;
            }

            return result;
        }

        public bool LoadProfile(int device, bool launchprogram, ControlService control,
                string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            bool Loaded = true;
            Dictionary<DS4Controls, DS4KeyType> customMapKeyTypes = new Dictionary<DS4Controls, DS4KeyType>();
            Dictionary<DS4Controls, UInt16> customMapKeys = new Dictionary<DS4Controls, UInt16>();
            Dictionary<DS4Controls, X360Controls> customMapButtons = new Dictionary<DS4Controls, X360Controls>();
            Dictionary<DS4Controls, String> customMapMacros = new Dictionary<DS4Controls, String>();
            Dictionary<DS4Controls, String> customMapExtras = new Dictionary<DS4Controls, String>();
            Dictionary<DS4Controls, DS4KeyType> shiftCustomMapKeyTypes = new Dictionary<DS4Controls, DS4KeyType>();
            Dictionary<DS4Controls, UInt16> shiftCustomMapKeys = new Dictionary<DS4Controls, UInt16>();
            Dictionary<DS4Controls, X360Controls> shiftCustomMapButtons = new Dictionary<DS4Controls, X360Controls>();
            Dictionary<DS4Controls, String> shiftCustomMapMacros = new Dictionary<DS4Controls, String>();
            Dictionary<DS4Controls, String> shiftCustomMapExtras = new Dictionary<DS4Controls, String>();
            string rootname = "DS4Windows";
            bool missingSetting = false;
            string profilepath;
            if (propath == "")
                profilepath = _pathService.AppDataPath + @"\Profiles\" + store.profilePath[device] + ".xml";
            else
                profilepath = propath;

            bool xinputPlug = false;
            bool xinputStatus = false;

            if (File.Exists(profilepath))
            {
                XmlNode Item;

                using FileStream fileStream = new FileStream(profilepath, FileMode.Open, FileAccess.Read);
                ProfileMigration tmpMigration = new ProfileMigration(fileStream);
                // Do not perform automatic migrations at startup. Load profile text
                // as-is via the migration helper's reader if available.
                if (tmpMigration.ProfileReader != null)
                {
                    m_Xdoc.Load(tmpMigration.ProfileReader);
                }
                else
                {
                    Loaded = false;
                }
                tmpMigration.Close();

                if (m_Xdoc.SelectSingleNode(rootname) == null)
                {
                    rootname = "DS4Windows";
                    missingSetting = true;
                }

                if (device < ControllerLimits.MaxControllerCount)
                {
                    DS4LightBar.forcelight[device] = false;
                    DS4LightBar.forcedFlash[device] = 0;
                }

                OutContType oldContType = store.ActiveOutDevType[device];
                LightbarSettingInfo lightbarSettings = store.lightbarSettingInfo[device];
                LightbarDS4WinInfo lightInfo = lightbarSettings.ds4winSettings;

                // Make sure to reset currently set profile values before parsing
                store.ResetProfile(device);
                store.ResetMouseProperties(device, control);
                // Reset some Mapping properties before attempting to load different
                // profile
                control.PreLoadReset(device);

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/touchToggle"); Boolean.TryParse(Item.InnerText, out store.enableTouchToggle[device]); }
                catch { missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/idleDisconnectTimeout");
                    if (int.TryParse(Item?.InnerText ?? "", out int temp))
                    {
                        store.idleDisconnectTimeout[device] = temp;
                    }
                }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/outputDataToDS4"); Boolean.TryParse(Item.InnerText, out store.enableOutputDataToDS4[device]); }
                catch { missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LightbarMode");
                    string tempMode = Item.InnerText;
                    lightbarSettings.mode = GetLightbarModeType(tempMode);
                }
                catch { lightbarSettings.mode = LightbarMode.DS4Win; missingSetting = true; }

                //New method for saving color
                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/Color");
                    string[] colors;
                    if (!string.IsNullOrEmpty(Item.InnerText))
                        colors = Item.InnerText.Split(',');
                    else
                        colors = new string[0];

                    lightInfo.m_Led.red = byte.Parse(colors[0]);
                    lightInfo.m_Led.green = byte.Parse(colors[1]);
                    lightInfo.m_Led.blue = byte.Parse(colors[2]);
                }
                catch { missingSetting = true; }

                if (m_Xdoc.SelectSingleNode("/" + rootname + "/Color") == null)
                {
                    //Old method of color saving
                    try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/Red"); Byte.TryParse(Item.InnerText, out lightInfo.m_Led.red); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/Green"); Byte.TryParse(Item.InnerText, out lightInfo.m_Led.green); }
                    catch { missingSetting = true; }

                    try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/Blue"); Byte.TryParse(Item.InnerText, out lightInfo.m_Led.blue); }
                    catch { missingSetting = true; }
                }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RumbleBoost"); Byte.TryParse(Item.InnerText, out store.rumble[device]); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RumbleAutostopTime"); Int32.TryParse(Item.InnerText, out store.rumbleAutostopTime[device]); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ledAsBatteryIndicator"); Boolean.TryParse(Item.InnerText, out lightInfo.ledAsBattery); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/FlashType"); Byte.TryParse(Item.InnerText, out lightInfo.flashType); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/flashBatteryAt"); Int32.TryParse(Item.InnerText, out lightInfo.flashAt); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/touchSensitivity"); Byte.TryParse(Item.InnerText, out store.touchSensitivity[device]); }
                catch { missingSetting = true; }

                //New method for saving color
                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LowColor");
                    string[] colors;
                    if (!string.IsNullOrEmpty(Item.InnerText))
                        colors = Item.InnerText.Split(',');
                    else
                        colors = new string[0];

                    lightInfo.m_LowLed.red = byte.Parse(colors[0]);
                    lightInfo.m_LowLed.green = byte.Parse(colors[1]);
                    lightInfo.m_LowLed.blue = byte.Parse(colors[2]);
                }
                catch { missingSetting = true; }

                if (m_Xdoc.SelectSingleNode("/" + rootname + "/LowColor") == null)
                {
                    //Old method of color saving
                    try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LowRed"); byte.TryParse(Item.InnerText, out lightInfo.m_LowLed.red); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LowGreen"); byte.TryParse(Item.InnerText, out lightInfo.m_LowLed.green); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LowBlue"); byte.TryParse(Item.InnerText, out lightInfo.m_LowLed.blue); }
                    catch { missingSetting = true; }
                }

                //New method for saving color
                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ChargingColor");
                    string[] colors;
                    if (!string.IsNullOrEmpty(Item.InnerText))
                        colors = Item.InnerText.Split(',');
                    else
                        colors = new string[0];

                    lightInfo.m_ChargingLed.red = byte.Parse(colors[0]);
                    lightInfo.m_ChargingLed.green = byte.Parse(colors[1]);
                    lightInfo.m_ChargingLed.blue = byte.Parse(colors[2]);
                }
                catch { missingSetting = true; }

                if (m_Xdoc.SelectSingleNode("/" + rootname + "/ChargingColor") == null)
                {
                    try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ChargingRed"); Byte.TryParse(Item.InnerText, out lightInfo.m_ChargingLed.red); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ChargingGreen"); Byte.TryParse(Item.InnerText, out lightInfo.m_ChargingLed.green); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ChargingBlue"); Byte.TryParse(Item.InnerText, out lightInfo.m_ChargingLed.blue); }
                    catch { missingSetting = true; }
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/FlashColor");
                    string[] colors;
                    if (!string.IsNullOrEmpty(Item.InnerText))
                        colors = Item.InnerText.Split(',');
                    else
                        colors = new string[0];
                    lightInfo.m_FlashLed.red = byte.Parse(colors[0]);
                    lightInfo.m_FlashLed.green = byte.Parse(colors[1]);
                    lightInfo.m_FlashLed.blue = byte.Parse(colors[2]);
                }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/touchpadJitterCompensation"); bool.TryParse(Item.InnerText, out store.touchpadJitterCompensation[device]); }
                catch { missingSetting = true; }
                // Phase6-Step12: 旧形式の読込で欠けていた 10 要素。要素が無い（古いプロファイル）、または値が不正なときは、
                // ResetProfile で設定済みの既定値のままにする（missingSetting は立てない）。
                LoadLegacyExtraSettings(device, rootname);

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/lowerRCOn"); bool.TryParse(Item.InnerText, out store.lowerRCOn[device]); }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/tapSensitivity"); byte.TryParse(Item.InnerText, out store.tapSensitivity[device]); }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/doubleTap"); bool.TryParse(Item.InnerText, out store.doubleTap[device]); }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/scrollSensitivity"); int.TryParse(Item.InnerText, out store.scrollSensitivity[device]); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchpadInvert"); int temp = 0; int.TryParse(Item.InnerText, out temp); store.touchpadInvert[device] = Math.Min(Math.Max(temp, 0), 3); }
                catch { store.touchpadInvert[device] = 0; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchpadClickPassthru"); bool.TryParse(Item.InnerText, out store.touchClickPassthru[device]); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LeftTriggerMiddle"); byte.TryParse(Item.InnerText, out store.l2ModInfo[device].deadZone); }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RightTriggerMiddle"); byte.TryParse(Item.InnerText, out store.r2ModInfo[device].deadZone); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2AntiDeadZone"); int.TryParse(Item.InnerText, out store.l2ModInfo[device].antiDeadZone); }
                catch { store.l2ModInfo[device].antiDeadZone = 0; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2AntiDeadZone"); int.TryParse(Item.InnerText, out store.r2ModInfo[device].antiDeadZone); }
                catch { store.r2ModInfo[device].antiDeadZone = 0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2MaxZone"); int temp = 100;
                    int.TryParse(Item.InnerText, out temp);
                    store.l2ModInfo[device].maxZone = Math.Min(Math.Max(temp, 0), 100);
                }
                catch { store.l2ModInfo[device].maxZone = 100; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2MaxZone"); int temp = 100;
                    int.TryParse(Item.InnerText, out temp);
                    store.r2ModInfo[device].maxZone = Math.Min(Math.Max(temp, 0), 100);
                }
                catch { store.r2ModInfo[device].maxZone = 100; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2MaxOutput"); double temp = 100.0;
                    temp = double.Parse(Item.InnerText);
                    store.l2ModInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                }
                catch { store.l2ModInfo[device].maxOutput = 100.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2MaxOutput"); double temp = 100.0;
                    temp = double.Parse(Item.InnerText);
                    store.r2ModInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                }
                catch { store.r2ModInfo[device].maxOutput = 100.0; missingSetting = true; }

                try
                {
                    // Take a rotation angle (degrees) and convert to radians
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSRotation"); int tempDegrees = 0;
                    int.TryParse(Item.InnerText, out tempDegrees);
                    tempDegrees = Math.Min(Math.Max(tempDegrees, -180), 180);
                    store.LSRotation[device] = tempDegrees * Math.PI / 180.0;
                }
                catch { store.LSRotation[device] = 0.0; missingSetting = true; }

                try
                {
                    // Take a rotation angle (degrees) and convert to radians
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSRotation"); int tempDegrees = 0;
                    int.TryParse(Item.InnerText, out tempDegrees);
                    tempDegrees = Math.Min(Math.Max(tempDegrees, -180), 180);
                    store.RSRotation[device] = tempDegrees * Math.PI / 180.0;
                }
                catch { store.RSRotation[device] = 0.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSFuzz"); int temp = 0;
                    int.TryParse(Item.InnerText, out temp);
                    temp = Math.Min(Math.Max(temp, 0), 100);
                    store.lsModInfo[device].fuzz = temp;
                }
                catch { store.lsModInfo[device].fuzz = StickDeadZoneInfo.DEFAULT_FUZZ; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSFuzz"); int temp = 0;
                    int.TryParse(Item.InnerText, out temp);
                    temp = Math.Min(Math.Max(temp, 0), 100);
                    store.rsModInfo[device].fuzz = temp;
                }
                catch { store.rsModInfo[device].fuzz = StickDeadZoneInfo.DEFAULT_FUZZ; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ButtonMouseSensitivity"); int.TryParse(Item.InnerText, out store.buttonMouseInfos[device].buttonSensitivity); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ButtonMouseOffset"); double.TryParse(Item.InnerText, out store.buttonMouseInfos[device].mouseVelocityOffset); }
                catch { missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ButtonMouseVerticalScale");
                    int.TryParse(Item.InnerText, out int temp);
                    store.buttonMouseInfos[device].buttonVerticalScale = Math.Min(Math.Max(temp, 0), 500) * 0.01;
                }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/Rainbow"); double.TryParse(Item.InnerText, out lightInfo.rainbow); }
                catch { lightInfo.rainbow = 0; missingSetting = true; }
                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/MaxSatRainbow");
                    int.TryParse(Item.InnerText, out int temp);
                    lightInfo.maxRainbowSat = Math.Max(0, Math.Min(100, temp)) / 100.0;
                }
                catch { lightInfo.maxRainbowSat = 1.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSDeadZone");
                    int.TryParse(Item.InnerText, out int temp);
                    temp = Math.Min(Math.Max(temp, 0), 127);
                    store.lsModInfo[device].deadZone = temp;
                }
                catch
                {
                    store.lsModInfo[device].deadZone = 10; missingSetting = true;
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSDeadZone");
                    int.TryParse(Item.InnerText, out int temp);
                    temp = Math.Min(Math.Max(temp, 0), 127);
                    store.rsModInfo[device].deadZone = temp;
                }
                catch
                {
                    store.rsModInfo[device].deadZone = 10; missingSetting = true;
                }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiDeadZone"); int.TryParse(Item.InnerText, out store.lsModInfo[device].antiDeadZone); }
                catch { store.lsModInfo[device].antiDeadZone = 20; missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiDeadZone"); int.TryParse(Item.InnerText, out store.rsModInfo[device].antiDeadZone); }
                catch { store.rsModInfo[device].antiDeadZone = 20; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSMaxZone"); int temp = 100;
                    int.TryParse(Item.InnerText, out temp);
                    store.lsModInfo[device].maxZone = Math.Min(Math.Max(temp, 0), 100);
                }
                catch { store.lsModInfo[device].maxZone = 100; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSMaxZone"); int temp = 100;
                    int.TryParse(Item.InnerText, out temp);
                    store.rsModInfo[device].maxZone = Math.Min(Math.Max(temp, 0), 100);
                }
                catch { store.rsModInfo[device].maxZone = 100; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSVerticalScale");
                    if (double.TryParse(Item?.InnerText ?? "", out double temp))
                    {
                        store.lsModInfo[device].verticalScale = Math.Min(Math.Max(temp, 0.0), 200.0);
                    }
                }
                catch { store.lsModInfo[device].verticalScale = StickDeadZoneInfo.DEFAULT_VERTICAL_SCALE; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSMaxOutput"); double temp = 100.0;
                    temp = double.Parse(Item.InnerText);
                    store.lsModInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                }
                catch { store.lsModInfo[device].maxOutput = 100.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSMaxOutputForce");
                    if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                    {
                        store.lsModInfo[device].maxOutputForce = temp;
                    }
                }
                catch { store.lsModInfo[device].maxOutputForce = StickDeadZoneInfo.DEFAULT_MAXOUTPUT_FORCE; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSMaxOutput"); double temp = 100.0;
                    temp = double.Parse(Item.InnerText);
                    store.rsModInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                }
                catch { store.rsModInfo[device].maxOutput = 100; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSMaxOutputForce");
                    if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                    {
                        store.rsModInfo[device].maxOutputForce = temp;
                    }
                }
                catch { store.rsModInfo[device].maxOutputForce = StickDeadZoneInfo.DEFAULT_MAXOUTPUT_FORCE; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOuterBindDead");
                    if (int.TryParse(Item?.InnerText ?? "", out int temp))
                    {
                        temp = Math.Min(Math.Max(temp, 0), 100);
                        store.lsModInfo[device].outerBindDeadZone = temp;
                    }
                }
                catch { store.lsModInfo[device].outerBindDeadZone = StickDeadZoneInfo.DEFAULT_OUTER_BIND_DEAD; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOuterBindDead");
                    if (int.TryParse(Item?.InnerText ?? "", out int temp))
                    {
                        temp = Math.Min(Math.Max(temp, 0), 100);
                        store.rsModInfo[device].outerBindDeadZone = temp;
                    }
                }
                catch { store.rsModInfo[device].outerBindDeadZone = StickDeadZoneInfo.DEFAULT_OUTER_BIND_DEAD; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOuterBindInvert");
                    if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                    {
                        store.lsModInfo[device].outerBindInvert = temp;
                    }
                }
                catch { store.lsModInfo[device].outerBindInvert = StickDeadZoneInfo.DEFAULT_OUTER_BIND_INVERT; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOuterBindInvert");
                    if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                    {
                        store.rsModInfo[device].outerBindInvert = temp;
                    }
                }
                catch { store.rsModInfo[device].outerBindInvert = StickDeadZoneInfo.DEFAULT_OUTER_BIND_INVERT; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSVerticalScale");
                    if (double.TryParse(Item?.InnerText ?? "", out double temp))
                    {
                        store.rsModInfo[device].verticalScale = Math.Min(Math.Max(temp, 0.0), 200.0);
                    }
                }
                catch { store.rsModInfo[device].verticalScale = StickDeadZoneInfo.DEFAULT_VERTICAL_SCALE; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSDeadZoneType");
                    if (Enum.TryParse(Item?.InnerText ?? "", out StickDeadZoneInfo.DeadZoneType temp))
                    {
                        store.lsModInfo[device].deadzoneType = temp;
                    }
                }
                catch { }

                bool lsAxialDeadGroup = false;
                XmlNode lsAxialDeadElement =
                    m_Xdoc.SelectSingleNode("/" + rootname + "/LSAxialDeadOptions");
                lsAxialDeadGroup = lsAxialDeadElement != null;

                if (lsAxialDeadGroup)
                {
                    try
                    {
                        Item = lsAxialDeadElement.SelectSingleNode("DeadZoneX");
                        int.TryParse(Item.InnerText, out int temp);
                        temp = Math.Min(Math.Max(temp, 0), 127);
                        store.lsModInfo[device].xAxisDeadInfo.deadZone = temp;
                    }
                    catch { }

                    try
                    {
                        Item = lsAxialDeadElement.SelectSingleNode("DeadZoneY");
                        int.TryParse(Item.InnerText, out int temp);
                        temp = Math.Min(Math.Max(temp, 0), 127);
                        store.lsModInfo[device].yAxisDeadInfo.deadZone = temp;
                    }
                    catch { }

                    try
                    {
                        Item = lsAxialDeadElement.SelectSingleNode("MaxZoneX");
                        int.TryParse(Item.InnerText, out int temp);
                        store.lsModInfo[device].xAxisDeadInfo.maxZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = lsAxialDeadElement.SelectSingleNode("MaxZoneY");
                        int.TryParse(Item.InnerText, out int temp);
                        store.lsModInfo[device].yAxisDeadInfo.maxZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = lsAxialDeadElement.SelectSingleNode("AntiDeadZoneX");
                        int.TryParse(Item.InnerText, out int temp);
                        store.lsModInfo[device].xAxisDeadInfo.antiDeadZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = lsAxialDeadElement.SelectSingleNode("AntiDeadZoneY");
                        int.TryParse(Item.InnerText, out int temp);
                        store.lsModInfo[device].yAxisDeadInfo.antiDeadZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = lsAxialDeadElement.SelectSingleNode("MaxOutputX");
                        double.TryParse(Item.InnerText, out double temp);
                        store.lsModInfo[device].xAxisDeadInfo.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                    }
                    catch { }

                    try
                    {
                        Item = lsAxialDeadElement.SelectSingleNode("MaxOutputY");
                        double.TryParse(Item.InnerText, out double temp);
                        store.lsModInfo[device].yAxisDeadInfo.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                    }
                    catch { }
                }

                XmlNode lsDeltaAccelElement =
                    m_Xdoc.SelectSingleNode($"/{rootname}/LSDeltaAccelSettings");
                if (lsDeltaAccelElement != null)
                {
                    try
                    {
                        Item = lsDeltaAccelElement.SelectSingleNode("Enabled");
                        if (bool.TryParse(Item.InnerText ?? "", out bool temp))
                        {
                            store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.enabled
                                = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = lsDeltaAccelElement.SelectSingleNode("Multiplier");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.multiplier =
                                Math.Clamp(temp, 0.0, 10.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = lsDeltaAccelElement.SelectSingleNode("MaxTravel");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.maxTravel =
                                Math.Clamp(temp, 0.0, 1.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = lsDeltaAccelElement.SelectSingleNode("MinTravel");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minTravel =
                                Math.Clamp(temp, 0.0, 1.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = lsDeltaAccelElement.SelectSingleNode("EasingDuration");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.easingDuration =
                                Math.Clamp(temp, 0.0, 600.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = lsDeltaAccelElement.SelectSingleNode("MinFactor");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minfactor =
                                Math.Clamp(temp, 1.0, 10.0);
                        }
                    }
                    catch { }
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSDeadZoneType");
                    if (Enum.TryParse(Item?.InnerText ?? "", out StickDeadZoneInfo.DeadZoneType temp))
                    {
                        store.rsModInfo[device].deadzoneType = temp;
                    }
                }
                catch { }

                bool rsAxialDeadGroup = false;
                XmlNode rsAxialDeadElement =
                    m_Xdoc.SelectSingleNode("/" + rootname + "/RSAxialDeadOptions");
                rsAxialDeadGroup = rsAxialDeadElement != null;

                if (rsAxialDeadGroup)
                {
                    try
                    {
                        Item = rsAxialDeadElement.SelectSingleNode("DeadZoneX");
                        int.TryParse(Item.InnerText, out int temp);
                        temp = Math.Min(Math.Max(temp, 0), 127);
                        store.rsModInfo[device].xAxisDeadInfo.deadZone = temp;
                    }
                    catch { }

                    try
                    {
                        Item = rsAxialDeadElement.SelectSingleNode("DeadZoneY");
                        int.TryParse(Item.InnerText, out int temp);
                        temp = Math.Min(Math.Max(temp, 0), 127);
                        store.rsModInfo[device].yAxisDeadInfo.deadZone = temp;
                    }
                    catch { }

                    try
                    {
                        Item = rsAxialDeadElement.SelectSingleNode("MaxZoneX");
                        int.TryParse(Item.InnerText, out int temp);
                        store.rsModInfo[device].xAxisDeadInfo.maxZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = rsAxialDeadElement.SelectSingleNode("MaxZoneY");
                        int.TryParse(Item.InnerText, out int temp);
                        store.rsModInfo[device].yAxisDeadInfo.maxZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = rsAxialDeadElement.SelectSingleNode("AntiDeadZoneX");
                        int.TryParse(Item.InnerText, out int temp);
                        store.rsModInfo[device].xAxisDeadInfo.antiDeadZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = rsAxialDeadElement.SelectSingleNode("AntiDeadZoneY");
                        int.TryParse(Item.InnerText, out int temp);
                        store.rsModInfo[device].yAxisDeadInfo.antiDeadZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = rsAxialDeadElement.SelectSingleNode("MaxOutputX");
                        double.TryParse(Item.InnerText, out double temp);
                        store.rsModInfo[device].xAxisDeadInfo.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                    }
                    catch { }

                    try
                    {
                        Item = rsAxialDeadElement.SelectSingleNode("MaxOutputY");
                        double.TryParse(Item.InnerText, out double temp);
                        store.rsModInfo[device].yAxisDeadInfo.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                    }
                    catch { }
                }

                XmlNode rsDeltaAccelElement =
                    m_Xdoc.SelectSingleNode($"/{rootname}/RSDeltaAccelSettings");
                if (lsDeltaAccelElement != null)
                {
                    try
                    {
                        Item = rsDeltaAccelElement.SelectSingleNode("Enabled");
                        if (bool.TryParse(Item.InnerText ?? "", out bool temp))
                        {
                            store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.enabled
                                = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = rsDeltaAccelElement.SelectSingleNode("Multiplier");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.multiplier =
                                Math.Clamp(temp, 0.0, 10.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = rsDeltaAccelElement.SelectSingleNode("MaxTravel");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.maxTravel =
                                Math.Clamp(temp, 0.0, 1.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = rsDeltaAccelElement.SelectSingleNode("MinTravel");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minTravel =
                                Math.Clamp(temp, 0.0, 1.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = rsDeltaAccelElement.SelectSingleNode("EasingDuration");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.easingDuration =
                                Math.Clamp(temp, 0.0, 600.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = rsDeltaAccelElement.SelectSingleNode("MinFactor");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minfactor =
                                Math.Clamp(temp, 1.0, 10.0);
                        }
                    }
                    catch { }
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXDeadZone");
                    double.TryParse(Item.InnerText, out store.SXDeadzone[device]);
                }
                catch { store.SXDeadzone[device] = BackingStore.DEFAULT_SX_TILT_DEADZONE; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZDeadZone");
                    double.TryParse(Item.InnerText, out store.SZDeadzone[device]);
                }
                catch { store.SZDeadzone[device] = BackingStore.DEFAULT_SX_TILT_DEADZONE; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXMaxZone");
                    int temp = 0;
                    int.TryParse(Item.InnerText, out temp);
                    store.SXMaxzone[device] = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
                }
                catch { store.SXMaxzone[device] = 1.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZMaxZone");
                    int temp = 0;
                    int.TryParse(Item.InnerText, out temp);
                    store.SZMaxzone[device] = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
                }
                catch { store.SZMaxzone[device] = 1.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXAntiDeadZone");
                    int temp = 0;
                    int.TryParse(Item.InnerText, out temp);
                    store.SXAntiDeadzone[device] = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
                }
                catch { store.SXAntiDeadzone[device] = 0.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZAntiDeadZone");
                    int temp = 0;
                    int.TryParse(Item.InnerText, out temp);
                    store.SZAntiDeadzone[device] = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
                }
                catch { store.SZAntiDeadzone[device] = 0.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/Sensitivity");
                    string[] s = Item.InnerText.Split('|');
                    if (s.Length == 1)
                        s = Item.InnerText.Split(',');

                    if (double.TryParse(s[0], out store.LSSens[device]))
                    {
                        store.LSSens[device] = Math.Clamp(store.LSSens[device], 0.1, 5.0);
                    }
                    else
                    {
                        store.LSSens[device] = 1;
                    }

                    if (double.TryParse(s[1], out store.RSSens[device]))
                    {
                        store.RSSens[device] = Math.Clamp(store.RSSens[device], 0.1, 5.0);
                    }
                    else
                    {
                        store.RSSens[device] = 1;
                    }

                    if (double.TryParse(s[2], out store.l2Sens[device]))
                    {
                        store.l2Sens[device] = Math.Clamp(store.l2Sens[device], 0.1, 10.0);
                    }
                    else
                    {
                        store.l2Sens[device] = 1;
                    }

                    if (double.TryParse(s[3], out store.r2Sens[device]))
                    {
                        store.r2Sens[device] = Math.Clamp(store.r2Sens[device], 0.1, 10.0);
                    }
                    else
                    {
                        store.r2Sens[device] = 1;
                    }

                    if (double.TryParse(s[4], out store.SXSens[device]))
                    {
                        store.SXSens[device] = Math.Clamp(store.SXSens[device], 0.0, 5.0);
                    }
                    else
                    {
                        store.SXSens[device] = 1;
                    }

                    if (double.TryParse(s[5], out store.SZSens[device]))
                    {
                        store.SZSens[device] = Math.Clamp(store.SZSens[device], 0.0, 5.0);
                    }
                    else
                    {
                        store.SZSens[device] = 1;
                    }
                }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ChargingType"); int.TryParse(Item.InnerText, out lightInfo.chargingType); }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/MouseAcceleration"); bool.TryParse(Item.InnerText, out store.buttonMouseInfos[device].mouseAccel); }
                catch { missingSetting = true; }

                int shiftM = 0;
                if (m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftModifier") != null)
                    int.TryParse(m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftModifier").InnerText, out shiftM);

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LaunchProgram");
                    store.launchProgram[device] = Item.InnerText;
                }
                catch { store.launchProgram[device] = string.Empty; missingSetting = true; }

                if (launchprogram == true && store.launchProgram[device] != string.Empty)
                {
                    string programPath = store.launchProgram[device];
                    bool procFound = false;
                    bool handled = false;
                    try
                    {
                        var inspector = store.ProcessInspector;
                        if (inspector != null)
                        {
                            procFound = inspector.IsProcessRunning(programPath);
                            handled = true;
                        }
                    }
                    catch { }

                    if (!handled)
                    {
                        System.Diagnostics.Process[] localAll = System.Diagnostics.Process.GetProcesses();
                        for (int procInd = 0, procsLen = localAll.Length; !procFound && procInd < procsLen; procInd++)
                        {
                            try
                            {
                                string temp = localAll[procInd].MainModule.FileName;
                                if (temp == programPath)
                                {
                                    procFound = true;
                                }
                            }
                            // Ignore any process for which this information
                            // is not exposed
                            catch { }
                        }
                    }

                    if (!procFound)
                    {
                        Task processTask = new Task(() =>
                        {
                            Thread.Sleep(5000);
                            System.Diagnostics.Process tempProcess = new System.Diagnostics.Process();
                            tempProcess.StartInfo.FileName = programPath;
                            tempProcess.StartInfo.WorkingDirectory = new FileInfo(programPath).Directory.ToString();
                            //tempProcess.StartInfo.UseShellExecute = false;
                            try { tempProcess.Start(); }
                            catch { }
                        });

                        processTask.Start();
                    }
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/DinputOnly");
                    bool.TryParse(Item.InnerText, out store.dinputOnly[device]);
                }
                catch { store.dinputOnly[device] = false; missingSetting = true; }

                bool oldUseDInputOnly = _profileSettings.UseDInputOnlyArray[device];

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/StartTouchpadOff");
                    bool.TryParse(Item.InnerText, out store.startTouchpadOff[device]);
                    if (store.startTouchpadOff[device] == true) control.StartTPOff(device);
                }
                catch { store.startTouchpadOff[device] = false; missingSetting = true; }

                // Fallback lookup if TouchpadOutMode is not set
                bool tpForControlsPresent = false;
                XmlNode xmlUseTPForControlsElement =
                    m_Xdoc.SelectSingleNode("/" + rootname + "/UseTPforControls");
                tpForControlsPresent = xmlUseTPForControlsElement != null;
                if (tpForControlsPresent)
                {
                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/UseTPforControls");
                        if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                        {
                            if (temp) store.touchOutMode[device] = TouchpadOutMode.Controls;
                        }
                    }
                    catch { store.touchOutMode[device] = TouchpadOutMode.Mouse; }
                }

                // Fallback lookup if GyroOutMode is not set
                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/UseSAforMouse");
                    if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                    {
                        if (temp) store.gyroOutMode[device] = GyroOutMode.Mouse;
                    }
                }
                catch { store.gyroOutMode[device] = GyroOutMode.Controls; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SATriggers"); store.sATriggers[device] = Item.InnerText; }
                catch { store.sATriggers[device] = "-1"; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SATriggerCond"); store.sATriggerCond[device] = BackingStore.SaTriggerCondValue(Item.InnerText); }
                catch { store.sATriggerCond[device] = true; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelEmulationAxis"); SASteeringWheelEmulationAxisType.TryParse(Item.InnerText, out store.sASteeringWheelEmulationAxis[device]); }
                catch { store.sASteeringWheelEmulationAxis[device] = SASteeringWheelEmulationAxisType.None; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelEmulationRange"); int.TryParse(Item.InnerText, out store.sASteeringWheelEmulationRange[device]); }
                catch { store.sASteeringWheelEmulationRange[device] = 360; missingSetting = true; }

                bool sASteeringWheelSmoothingGroup = false;
                XmlNode xmlSASteeringWheelSmoothElement =
                    m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelSmoothingOptions");
                sASteeringWheelSmoothingGroup = xmlSASteeringWheelSmoothElement != null;

                if (sASteeringWheelSmoothingGroup)
                {
                    try
                    {
                        Item = xmlSASteeringWheelSmoothElement.SelectSingleNode("SASteeringWheelUseSmoothing");
                        //Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelUseSmoothing");
                        bool.TryParse(Item.InnerText, out bool temp);
                        store.wheelSmoothInfo[device].Enabled = temp;
                    }
                    catch { store.wheelSmoothInfo[device].Reset(); missingSetting = true; }

                    try
                    {
                        Item = xmlSASteeringWheelSmoothElement.SelectSingleNode("SASteeringWheelSmoothMinCutoff");
                        //Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelSmoothMinCutoff");
                        double.TryParse(Item.InnerText, out double temp);
                        store.wheelSmoothInfo[device].MinCutoff = temp;
                    }
                    catch { store.wheelSmoothInfo[device].MinCutoff = OneEuroFilterPair.DEFAULT_WHEEL_CUTOFF; missingSetting = true; }

                    try
                    {
                        Item = xmlSASteeringWheelSmoothElement.SelectSingleNode("SASteeringWheelSmoothBeta");
                        //Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelSmoothBeta");
                        double.TryParse(Item.InnerText, out double temp);
                        store.wheelSmoothInfo[device].Beta = temp;
                    }
                    catch { store.wheelSmoothInfo[device].Beta = OneEuroFilterPair.DEFAULT_WHEEL_BETA; missingSetting = true; }
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelFuzz");
                    int.TryParse(Item.InnerText, out int temp);
                    store.saWheelFuzzValues[device] = temp >= 0 && temp <= 100 ? temp : 0;
                }
                catch { store.saWheelFuzzValues[device] = 0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroOutputMode");
                    string tempMode = Item.InnerText;
                    //gyroOutMode[device] = GetGyroOutModeType(tempMode);
                    Enum.TryParse(tempMode, out store.gyroOutMode[device]);
                }
                catch { PortOldGyroSettings(device); missingSetting = true; }

                XmlNode xmlGyroControlsElement =
                    m_Xdoc.SelectSingleNode("/" + rootname + "/GyroControlsSettings");
                if (xmlGyroControlsElement != null)
                {
                    try
                    {
                        Item = xmlGyroControlsElement.SelectSingleNode("Triggers");
                        if (Item != null)
                        {
                            store.gyroControlsInf[device].triggers = Item.InnerText;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroControlsElement.SelectSingleNode("TriggerCond");
                        if (Item != null)
                        {
                            store.gyroControlsInf[device].triggerCond = BackingStore.SaTriggerCondValue(Item.InnerText);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroControlsElement.SelectSingleNode("TriggerTurns");
                        if (bool.TryParse(Item?.InnerText ?? "", out bool tempTurns))
                        {
                            store.gyroControlsInf[device].triggerTurns = tempTurns;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroControlsElement.SelectSingleNode("Toggle");
                        if (bool.TryParse(Item?.InnerText ?? "", out bool tempToggle))
                        {
                            store.SetGyroControlsToggle(device, tempToggle, control);
                        }
                    }
                    catch { }
                }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickTriggers"); store.sAMouseStickTriggers[device] = Item.InnerText; }
                catch { store.sAMouseStickTriggers[device] = "-1"; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickTriggerCond"); store.sAMouseStickTriggerCond[device] = BackingStore.SaTriggerCondValue(Item.InnerText); }
                catch { store.sAMouseStickTriggerCond[device] = true; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickTriggerTurns"); bool.TryParse(Item.InnerText, out store.gyroMouseStickTriggerTurns[device]); }
                catch { store.gyroMouseStickTriggerTurns[device] = true; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickHAxis"); int temp = 0; int.TryParse(Item.InnerText, out temp); store.gyroMouseStickHorizontalAxis[device] = Math.Min(Math.Max(0, temp), 1); }
                catch { store.gyroMouseStickHorizontalAxis[device] = 0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickDeadZone"); int.TryParse(Item.InnerText, out int temp);
                    store.gyroMStickInfo[device].deadZone = temp;
                }
                catch { store.gyroMStickInfo[device].deadZone = 30; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickMaxZone"); int.TryParse(Item.InnerText, out int temp);
                    store.gyroMStickInfo[device].maxZone = Math.Max(temp, 1);
                }
                catch { store.gyroMStickInfo[device].maxZone = 830; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickOutputStick");
                    if (Enum.TryParse(Item?.InnerText ?? "", out GyroMouseStickInfo.OutputStick temp))
                    {
                        store.gyroMStickInfo[device].outputStick = temp;
                    }
                }
                catch { }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickOutputStickAxes");
                    if (Enum.TryParse(Item?.InnerText ?? "", out GyroMouseStickInfo.OutputStickAxes temp))
                    {
                        store.gyroMStickInfo[device].outputStickDir = temp;
                    }
                }
                catch { }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickAntiDeadX"); double.TryParse(Item.InnerText, out double temp);
                    store.gyroMStickInfo[device].antiDeadX = temp;
                }
                catch { store.gyroMStickInfo[device].antiDeadX = 0.4; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickAntiDeadY"); double.TryParse(Item.InnerText, out double temp);
                    store.gyroMStickInfo[device].antiDeadY = temp;
                }
                catch { store.gyroMStickInfo[device].antiDeadY = 0.4; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickInvert"); uint.TryParse(Item.InnerText, out store.gyroMStickInfo[device].inverted); }
                catch { store.gyroMStickInfo[device].inverted = 0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickToggle");
                    bool.TryParse(Item.InnerText, out bool temp);
                    store.SetGyroMouseStickToggle(device, temp, control);
                }
                catch
                {
                    store.SetGyroMouseStickToggle(device, false, control);
                    missingSetting = true;
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickMaxOutput"); double temp = 100.0;
                    temp = double.Parse(Item.InnerText);
                    store.gyroMStickInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                }
                catch { store.gyroMStickInfo[device].maxOutput = 100.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickMaxOutputEnabled");
                    bool.TryParse(Item.InnerText, out bool temp);
                    store.gyroMStickInfo[device].maxOutputEnabled = temp;
                }
                catch { store.gyroMStickInfo[device].maxOutputEnabled = false; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickVerticalScale"); int.TryParse(Item.InnerText, out store.gyroMStickInfo[device].vertScale); }
                catch { store.gyroMStickInfo[device].vertScale = 100; missingSetting = true; }

                bool gyroMStickSmoothingGroup = false;
                XmlNode xmlGyroMStickSmoothingElement =
                    m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickSmoothingSettings");
                gyroMStickSmoothingGroup = xmlGyroMStickSmoothingElement != null;

                if (gyroMStickSmoothingGroup)
                {
                    try
                    {
                        Item = xmlGyroMStickSmoothingElement.SelectSingleNode("UseSmoothing");
                        bool.TryParse(Item.InnerText, out bool tempSmoothing);
                        store.gyroMStickInfo[device].useSmoothing = tempSmoothing;
                    }
                    catch { store.gyroMStickInfo[device].useSmoothing = false; missingSetting = true; }

                    try
                    {
                        Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingMethod");
                        string temp = Item?.InnerText ?? GyroMouseStickInfo.DEFAULT_SMOOTH_TECHNIQUE;
                        store.gyroMStickInfo[device].DetermineSmoothMethod(temp);
                    }
                    catch { store.gyroMStickInfo[device].ResetSmoothing(); missingSetting = true; }

                    try
                    {
                        Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingWeight");
                        int temp = 0; int.TryParse(Item.InnerText, out temp);
                        store.gyroMStickInfo[device].smoothWeight = Math.Min(Math.Max(0.0, Convert.ToDouble(temp * 0.01)), 1.0);
                    }
                    catch { store.gyroMStickInfo[device].smoothWeight = 0.5; missingSetting = true; }

                    try
                    {
                        Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingMinCutoff");
                        double.TryParse(Item.InnerText, out double temp);
                        store.gyroMStickInfo[device].minCutoff = Math.Min(Math.Max(0.0, temp), 100.0);
                    }
                    catch { store.gyroMStickInfo[device].minCutoff = GyroMouseStickInfo.DEFAULT_MINCUTOFF; missingSetting = true; }

                    try
                    {
                        Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingBeta");
                        double.TryParse(Item.InnerText, out double temp);
                        store.gyroMStickInfo[device].beta = Math.Min(Math.Max(0.0, temp), 1.0);
                    }
                    catch { store.gyroMStickInfo[device].beta = GyroMouseStickInfo.DEFAULT_BETA; missingSetting = true; }
                }
                else
                {
                    missingSetting = true;
                }

                XmlNode xmlGyroSwipeElement =
                    m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSwipeSettings");
                if (xmlGyroSwipeElement != null)
                {
                    try
                    {
                        Item = xmlGyroSwipeElement.SelectSingleNode("DeadZoneX");
                        if (int.TryParse(Item?.InnerText ?? "", out int tempDead))
                        {
                            store.gyroSwipeInfo[device].deadzoneX = tempDead;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroSwipeElement.SelectSingleNode("DeadZoneY");
                        if (int.TryParse(Item?.InnerText ?? "", out int tempDead))
                        {
                            store.gyroSwipeInfo[device].deadzoneY = tempDead;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroSwipeElement.SelectSingleNode("Triggers");
                        if (Item != null)
                        {
                            store.gyroSwipeInfo[device].triggers = Item.InnerText;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroSwipeElement.SelectSingleNode("TriggerCond");
                        if (Item != null)
                        {
                            store.gyroSwipeInfo[device].triggerCond = BackingStore.SaTriggerCondValue(Item.InnerText);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroSwipeElement.SelectSingleNode("TriggerTurns");
                        if (bool.TryParse(Item?.InnerText ?? "", out bool tempTurns))
                        {
                            store.gyroSwipeInfo[device].triggerTurns = tempTurns;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroSwipeElement.SelectSingleNode("XAxis");
                        if (Enum.TryParse(Item?.InnerText ?? "", out GyroDirectionalSwipeInfo.XAxisSwipe tempX))
                        {
                            store.gyroSwipeInfo[device].xAxis = tempX;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroSwipeElement.SelectSingleNode("DelayTime");
                        if (int.TryParse(Item?.InnerText ?? "", out int tempDelay))
                        {
                            store.gyroSwipeInfo[device].delayTime = tempDelay;
                        }
                    }
                    catch { }
                }

                // Check for TouchpadOutputMode if UseTPforControls is not present in profile
                if (!tpForControlsPresent)
                {
                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchpadOutputMode");
                        string tempMode = Item.InnerText;
                        Enum.TryParse(tempMode, out store.touchOutMode[device]);
                    }
                    catch { store.touchOutMode[device] = TouchpadOutMode.Mouse; missingSetting = true; }
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchDisInvTriggers");
                    string[] triggers = Item.InnerText.Split(',');
                    int temp = -1;
                    List<int> tempIntList = new List<int>();
                    for (int i = 0, arlen = triggers.Length; i < arlen; i++)
                    {
                        if (int.TryParse(triggers[i], out temp))
                            tempIntList.Add(temp);
                    }

                    store.touchDisInvertTriggers[device] = tempIntList.ToArray();
                }
                catch { store.touchDisInvertTriggers[device] = new int[1] { -1 }; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSensitivity"); int.TryParse(Item.InnerText, out store.gyroSensitivity[device]); }
                catch { store.gyroSensitivity[device] = 100; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSensVerticalScale"); int.TryParse(Item.InnerText, out store.gyroSensVerticalScale[device]); }
                catch { store.gyroSensVerticalScale[device] = 100; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroInvert"); int.TryParse(Item.InnerText, out store.gyroInvert[device]); }
                catch { store.gyroInvert[device] = 0; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroTriggerTurns"); bool.TryParse(Item.InnerText, out store.gyroTriggerTurns[device]); }
                catch { store.gyroTriggerTurns[device] = true; missingSetting = true; }

                /*try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSmoothing"); bool.TryParse(Item.InnerText, out gyroSmoothing[device]); }
                catch { gyroSmoothing[device] = false; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSmoothingWeight"); int temp = 0; int.TryParse(Item.InnerText, out temp); gyroSmoothWeight[device] = Math.Min(Math.Max(0.0, Convert.ToDouble(temp * 0.01)), 1.0); }
                catch { gyroSmoothWeight[device] = 0.5; missingSetting = true; }
                */

                bool gyroSmoothingGroup = false;
                XmlNode xmlGyroSmoothingElement =
                    m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseSmoothingSettings");
                gyroSmoothingGroup = xmlGyroSmoothingElement != null;

                if (gyroSmoothingGroup)
                {
                    try
                    {
                        Item = xmlGyroSmoothingElement.SelectSingleNode("UseSmoothing");
                        bool.TryParse(Item.InnerText, out bool temp);
                        store.gyroMouseInfo[device].enableSmoothing = temp;
                    }
                    catch { store.gyroMouseInfo[device].enableSmoothing = false; missingSetting = true; }

                    try
                    {
                        Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingMethod");
                        string temp = Item?.InnerText ?? GyroMouseInfo.DEFAULT_SMOOTH_TECHNIQUE;
                        store.gyroMouseInfo[device].DetermineSmoothMethod(temp);
                    }
                    catch { store.gyroMouseInfo[device].ResetSmoothing(); missingSetting = true; }

                    try
                    {
                        Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingWeight");
                        int.TryParse(Item.InnerText, out int temp);
                        store.gyroMouseInfo[device].smoothingWeight = Math.Min(Math.Max(0.0, Convert.ToDouble(temp * 0.01)), 1.0);
                    }
                    catch { store.gyroMouseInfo[device].smoothingWeight = 0.5; missingSetting = true; }

                    try
                    {
                        Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingMinCutoff");
                        double.TryParse(Item.InnerText, out double temp);
                        store.gyroMouseInfo[device].minCutoff = Math.Min(Math.Max(0.0, temp), 100.0);
                    }
                    catch { store.gyroMouseInfo[device].minCutoff = GyroMouseInfo.DEFAULT_MINCUTOFF; missingSetting = true; }

                    try
                    {
                        Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingBeta");
                        double.TryParse(Item.InnerText, out double temp);
                        store.gyroMouseInfo[device].beta = Math.Min(Math.Max(0.0, temp), 1.0);
                    }
                    catch { store.gyroMouseInfo[device].beta = GyroMouseInfo.DEFAULT_BETA; missingSetting = true; }
                }
                else
                {
                    missingSetting = true;
                }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseHAxis"); int temp = 0; int.TryParse(Item.InnerText, out temp); store.gyroMouseHorizontalAxis[device] = Math.Min(Math.Max(0, temp), 1); }
                catch { store.gyroMouseHorizontalAxis[device] = 0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseDeadZone"); int.TryParse(Item.InnerText, out int temp);
                    store.SetGyroMouseDZ(device, temp, control);
                }
                catch { store.SetGyroMouseDZ(device, MouseCursor.GYRO_MOUSE_DEADZONE, control); missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseMinThreshold");
                    double.TryParse(Item.InnerText, out double temp);
                    temp = Math.Min(Math.Max(temp, 1.0), 40.0);
                    store.gyroMouseInfo[device].minThreshold = temp;
                }
                catch { store.gyroMouseInfo[device].minThreshold = GyroMouseInfo.DEFAULT_MIN_THRESHOLD; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseToggle");
                    bool.TryParse(Item.InnerText, out bool temp);
                    store.SetGyroMouseToggle(device, temp, control);
                }
                catch
                {
                    store.SetGyroMouseToggle(device, false, control);
                    missingSetting = true;
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/BTPollRate");
                    int temp = 0;
                    int.TryParse(Item.InnerText, out temp);
                    store.btPollRate[device] = (temp >= 0 && temp <= 16) ? temp : 4;
                }
                catch { store.btPollRate[device] = 4; missingSetting = true; }

                // Note! xxOutputCurveCustom property needs to be read before xxOutputCurveMode property in case the curveMode is value 6
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOutputCurveCustom"); store.lsOutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOutputCurveMode"); store.setLsOutCurveMode(device, store.stickOutputCurveId(Item.InnerText)); }
                catch { store.setLsOutCurveMode(device, 0); missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOutputCurveCustom"); store.rsOutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOutputCurveMode"); store.setRsOutCurveMode(device, store.stickOutputCurveId(Item.InnerText)); }
                catch { store.setRsOutCurveMode(device, 0); missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSSquareStick"); bool.TryParse(Item.InnerText, out store.squStickInfo[device].lsMode); }
                catch { store.squStickInfo[device].lsMode = false; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SquareStickRoundness"); double.TryParse(Item.InnerText, out store.squStickInfo[device].lsRoundness); }
                catch { store.squStickInfo[device].lsRoundness = 5.0; missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SquareRStickRoundness"); double.TryParse(Item.InnerText, out store.squStickInfo[device].rsRoundness); }
                catch { store.squStickInfo[device].rsRoundness = 5.0; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSSquareStick"); bool.TryParse(Item.InnerText, out store.squStickInfo[device].rsMode); }
                catch { store.squStickInfo[device].rsMode = false; missingSetting = true; }


                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiSnapback"); bool.TryParse(Item.InnerText, out store.lsAntiSnapbackInfo[device].enabled); }
                catch { store.lsAntiSnapbackInfo[device].enabled = StickAntiSnapbackInfo.DEFAULT_ENABLED; missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiSnapback"); bool.TryParse(Item.InnerText, out store.rsAntiSnapbackInfo[device].enabled); }
                catch { store.rsAntiSnapbackInfo[device].enabled = StickAntiSnapbackInfo.DEFAULT_ENABLED; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiSnapbackDelta"); double.TryParse(Item.InnerText, out store.lsAntiSnapbackInfo[device].delta); }
                catch { store.lsAntiSnapbackInfo[device].delta = StickAntiSnapbackInfo.DEFAULT_DELTA; missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiSnapbackDelta"); double.TryParse(Item.InnerText, out store.rsAntiSnapbackInfo[device].delta); }
                catch { store.rsAntiSnapbackInfo[device].delta = StickAntiSnapbackInfo.DEFAULT_DELTA; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiSnapbackTimeout"); int.TryParse(Item.InnerText, out store.lsAntiSnapbackInfo[device].timeout); }
                catch { store.lsAntiSnapbackInfo[device].timeout = StickAntiSnapbackInfo.DEFAULT_TIMEOUT; missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiSnapbackTimeout"); int.TryParse(Item.InnerText, out store.rsAntiSnapbackInfo[device].timeout); }
                catch { store.rsAntiSnapbackInfo[device].timeout = StickAntiSnapbackInfo.DEFAULT_TIMEOUT; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOutputMode"); Enum.TryParse(Item.InnerText, out store.lsOutputSettings[device].mode); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOutputMode"); Enum.TryParse(Item.InnerText, out store.rsOutputSettings[device].mode); }
                catch { missingSetting = true; }

                XmlNode xmlLSOutputSettingsElement =
                    m_Xdoc.SelectSingleNode("/" + rootname + "/LSOutputSettings");
                bool lsOutputGroup = xmlLSOutputSettingsElement != null;
                if (lsOutputGroup)
                {
                    bool flickStickLSGroup = false;
                    XmlNode xmlFlickStickLSElement =
                        xmlLSOutputSettingsElement.SelectSingleNode("FlickStickSettings");
                    flickStickLSGroup = xmlFlickStickLSElement != null;

                    if (flickStickLSGroup)
                    {
                        try
                        {
                            Item = xmlFlickStickLSElement.SelectSingleNode("RealWorldCalibration");
                            double.TryParse(Item.InnerText, out double temp);
                            store.lsOutputSettings[device].outputSettings.flickSettings.realWorldCalibration = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlFlickStickLSElement.SelectSingleNode("FlickThreshold");
                            double.TryParse(Item.InnerText, out double temp);
                            store.lsOutputSettings[device].outputSettings.flickSettings.flickThreshold = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlFlickStickLSElement.SelectSingleNode("FlickTime");
                            double.TryParse(Item.InnerText, out double temp);
                            store.lsOutputSettings[device].outputSettings.flickSettings.flickTime = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlFlickStickLSElement.SelectSingleNode("MinAngleThreshold");
                            double.TryParse(Item.InnerText, out double temp);
                            store.lsOutputSettings[device].outputSettings.flickSettings.minAngleThreshold = temp;
                        }
                        catch { missingSetting = true; }

                    }
                    else
                    {
                        missingSetting = true;
                    }
                }
                else
                {
                    missingSetting = true;
                }

                XmlNode xmlRSOutputSettingsElement =
                    m_Xdoc.SelectSingleNode("/" + rootname + "/RSOutputSettings");
                bool rsOutputGroup = xmlRSOutputSettingsElement != null;
                if (rsOutputGroup)
                {
                    bool flickStickRSGroup = false;
                    XmlNode xmlFlickStickRSElement =
                        xmlRSOutputSettingsElement.SelectSingleNode("FlickStickSettings");
                    flickStickRSGroup = xmlFlickStickRSElement != null;

                    if (flickStickRSGroup)
                    {
                        try
                        {
                            Item = xmlFlickStickRSElement.SelectSingleNode("RealWorldCalibration");
                            double.TryParse(Item.InnerText, out double temp);
                            store.rsOutputSettings[device].outputSettings.flickSettings.realWorldCalibration = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlFlickStickRSElement.SelectSingleNode("FlickThreshold");
                            double.TryParse(Item.InnerText, out double temp);
                            store.rsOutputSettings[device].outputSettings.flickSettings.flickThreshold = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlFlickStickRSElement.SelectSingleNode("FlickTime");
                            double.TryParse(Item.InnerText, out double temp);
                            store.rsOutputSettings[device].outputSettings.flickSettings.flickTime = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlFlickStickRSElement.SelectSingleNode("MinAngleThreshold");
                            double.TryParse(Item.InnerText, out double temp);
                            store.rsOutputSettings[device].outputSettings.flickSettings.minAngleThreshold = temp;
                        }
                        catch { missingSetting = true; }
                    }
                    else
                    {
                        missingSetting = true;
                    }
                }
                else
                {
                    missingSetting = true;
                }


                // Start of DualSense specific profile load
                //
                XmlNode xmlDualSenseControllerSettingsElement =
                    m_Xdoc.SelectSingleNode("/" + rootname + "/DualSenseControllerSettings");
                bool dSControllerSettingsGroup = xmlDualSenseControllerSettingsElement != null;
                if (dSControllerSettingsGroup)
                {
                    XmlNode xmlDSRumbleGroupElement =
                        xmlDualSenseControllerSettingsElement.SelectSingleNode("RumbleSettings");
                    bool dSRumbleGroup = xmlDSRumbleGroupElement != null;

                    if (dSRumbleGroup)
                    {
                        try
                        {
                            Item = xmlDSRumbleGroupElement.SelectSingleNode("EmulationMode");
                            DualSenseDevice.RumbleEmulationMode.TryParse(Item.InnerText, out DualSenseDevice.RumbleEmulationMode temp);
                            store.dualSenseRumbleEmulationMode[device] = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlDSRumbleGroupElement.SelectSingleNode("EnableGenericRumbleRescale");
                            bool.TryParse(Item.InnerText, out bool temp);
                            store.useGenericRumbleRescaleForDualSenses[device] = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlDSRumbleGroupElement.SelectSingleNode("HapticPowerLevel");
                            byte.TryParse(Item.InnerText, out byte temp);
                            store.dualSenseHapticPowerLevel[device] = temp;
                        }
                        catch { missingSetting = true; }

                    }
                    else
                    {
                        missingSetting = true;
                    }
                }
                else
                {
                    missingSetting = true;
                }
                //
                // End of DualSense specific profile load

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2OutputCurveCustom"); store.l2OutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2OutputCurveMode"); store.setL2OutCurveMode(device, axisOutputCurveId(Item.InnerText)); }
                catch { store.setL2OutCurveMode(device, 0); missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2TwoStageMode");
                    if (Enum.TryParse(Item?.InnerText, out TwoStageTriggerMode tempMode))
                    {
                        store.l2OutputSettings[device].TwoStageMode = tempMode;
                    }
                }
                catch { }


                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2HipFireTime");
                    if (int.TryParse(Item?.InnerText, out int temp))
                    {
                        store.l2OutputSettings[device].hipFireMS = Math.Clamp(temp, 0, 5000);
                    }
                }
                catch { }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2TriggerEffect");
                    if (Enum.TryParse(Item?.InnerText, out InputDevices.TriggerEffects tempMode))
                    {
                        store.l2OutputSettings[device].TriggerEffect = tempMode;
                    }
                }
                catch { }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2OutputCurveCustom"); store.r2OutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2OutputCurveMode"); store.setR2OutCurveMode(device, axisOutputCurveId(Item.InnerText)); }
                catch { store.setR2OutCurveMode(device, 0); missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2TwoStageMode");
                    if (Enum.TryParse(Item?.InnerText, out TwoStageTriggerMode tempMode))
                    {
                        store.r2OutputSettings[device].TwoStageMode = tempMode;
                    }
                }
                catch { }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2HipFireTime");
                    if (int.TryParse(Item?.InnerText, out int temp))
                    {
                        store.r2OutputSettings[device].hipFireMS = Math.Clamp(temp, 0, 5000);
                    }
                }
                catch { }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2TriggerEffect");
                    if (Enum.TryParse(Item?.InnerText, out InputDevices.TriggerEffects tempMode))
                    {
                        store.r2OutputSettings[device].TriggerEffect = tempMode;
                    }
                }
                catch { }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXOutputCurveCustom"); store.sxOutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXOutputCurveMode"); store.setSXOutCurveMode(device, axisOutputCurveId(Item.InnerText)); }
                catch { store.setSXOutCurveMode(device, 0); missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZOutputCurveCustom"); store.szOutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZOutputCurveMode"); store.setSZOutCurveMode(device, axisOutputCurveId(Item.InnerText)); }
                catch { store.setSZOutCurveMode(device, 0); missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TrackballMode"); bool.TryParse(Item.InnerText, out store.trackballMode[device]); }
                catch { store.trackballMode[device] = false; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TrackballFriction"); double.TryParse(Item.InnerText, out store.trackballFriction[device]); }
                catch { store.trackballFriction[device] = 10.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchRelMouseRotation");
                    int.TryParse(Item.InnerText, out int temp);
                    temp = Math.Min(Math.Max(temp, -180), 180);
                    store.touchpadRelMouse[device].rotation = temp * Math.PI / 180.0;
                }
                catch { store.touchpadRelMouse[device].rotation = 0.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchRelMouseMinThreshold");
                    double.TryParse(Item.InnerText, out double temp);
                    temp = Math.Min(Math.Max(temp, 1.0), 40.0);
                    store.touchpadRelMouse[device].minThreshold = temp;
                }
                catch { store.touchpadRelMouse[device].minThreshold = TouchpadRelMouseSettings.DEFAULT_MIN_THRESHOLD; missingSetting = true; }


                bool touchMStickGroup = false;
                XmlNode xmlTouchMStickSmoothingElement =
                    m_Xdoc.SelectSingleNode("/" + rootname + "/TouchpadMouseStick");
                touchMStickGroup = xmlTouchMStickSmoothingElement != null;

                if (touchMStickGroup && xmlTouchMStickSmoothingElement.HasChildNodes)
                {
                    //try
                    //{
                    //    Item = xmlGyroMStickSmoothingElement.SelectSingleNode("UseSmoothing");
                    //    if (bool.TryParse(Item?.InnerText ?? "", out bool tempSmoothing))
                    //    {
                    //        gyroMStickInfo[device].useSmoothing = tempSmoothing;
                    //    }
                    //}
                    //catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("DeadZone");
                        if (int.TryParse(Item?.InnerText ?? "", out int temp))
                        {
                            store.touchMStickInfo[device].deadZone = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("MaxZone");
                        if (int.TryParse(Item?.InnerText ?? "", out int temp))
                        {
                            store.touchMStickInfo[device].maxZone = Math.Max(temp, 1);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("OutputStick");
                        if (Enum.TryParse(Item?.InnerText ?? "",
                            out TouchMouseStickInfo.OutputStick temp))
                        {
                            store.touchMStickInfo[device].outputStick = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("OutputStickAxes");
                        if (Item == null)
                        {
                            // Phase6-Step12: 互換。以前の保存（上流の 2022-10-20 以来の不具合）は、軸を 2 つ目の <OutputStick> として書いていた
                            XmlNodeList legacyOutputStickNodes = xmlTouchMStickSmoothingElement.SelectNodes("OutputStick");
                            if (legacyOutputStickNodes != null && legacyOutputStickNodes.Count > 1)
                                Item = legacyOutputStickNodes[1];
                        }
                        if (Enum.TryParse(Item?.InnerText ?? "",
                            out TouchMouseStickInfo.OutputStickAxes temp))
                        {
                            store.touchMStickInfo[device].outputStickDir = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("AntiDeadX");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.touchMStickInfo[device].antiDeadX = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("AntiDeadY");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.touchMStickInfo[device].antiDeadY = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("Invert");
                        if (uint.TryParse(Item?.InnerText ?? "",
                            out uint temp))
                        {
                            store.touchMStickInfo[device].inverted = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("MaxOutput");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.touchMStickInfo[device].maxOutput = Math.Clamp(temp, 0.0, 100.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("MaxOutputEnabled");
                        if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                        {
                            store.touchMStickInfo[device].maxOutputEnabled = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("VerticalScale");
                        if (int.TryParse(Item?.InnerText ?? "", out int temp))
                        {
                            store.touchMStickInfo[device].vertScale = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("OutputCurve");
                        if (Enum.TryParse(Item?.InnerText ?? "", out StickOutCurve.Curve temp))
                        {
                            store.touchMStickInfo[device].outputCurve = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("Rotation");
                        if (int.TryParse(Item?.InnerText ?? "", out int temp))
                        {
                            temp = Math.Clamp(temp, -180, 180);
                            store.touchMStickInfo[device].rotationRad = temp * Math.PI / 180.0;
                        }
                    }
                    catch { }


                    bool stickSmoothingGroup = false;
                    XmlNode xmlStickSmoothingElement =
                        xmlTouchMStickSmoothingElement.SelectSingleNode("SmoothingSettings");
                    stickSmoothingGroup = xmlStickSmoothingElement != null;

                    if (stickSmoothingGroup && xmlStickSmoothingElement.HasChildNodes)
                    {
                        //try
                        //{
                        //    Item = xmlGyroMStickSmoothingElement.SelectSingleNode("UseSmoothing");
                        //    bool.TryParse(Item.InnerText, out bool tempSmoothing);
                        //    gyroMStickInfo[device].useSmoothing = tempSmoothing;
                        //}
                        //catch { gyroMStickInfo[device].useSmoothing = false; missingSetting = true; }

                        try
                        {
                            Item = xmlTouchMStickSmoothingElement.SelectSingleNode("SmoothingMethod");
                            if (Enum.TryParse(Item?.InnerText ?? "",
                                out TouchMouseStickInfo.SmoothingMethod temp))
                            {
                                store.touchMStickInfo[device].smoothingMethod = temp;
                            }
                        }
                        catch { }

                        try
                        {
                            Item = xmlTouchMStickSmoothingElement.SelectSingleNode("SmoothingMinCutoff");
                            if (double.TryParse(Item?.InnerText ?? "", out double temp))
                            {
                                store.touchMStickInfo[device].minCutoff = Math.Clamp(temp, 0.0, 100.0);
                            }
                        }
                        catch { }

                        try
                        {
                            Item = xmlTouchMStickSmoothingElement.SelectSingleNode("SmoothingBeta");
                            if (double.TryParse(Item?.InnerText ?? "", out double temp))
                            {
                                store.touchMStickInfo[device].beta = Math.Clamp(temp, 0.0, 1.0);
                            }
                        }
                        catch { }
                    }
                }


                try
                {
                    Item = m_Xdoc.SelectSingleNode($"/{rootname}/TouchpadButtonMode");
                    if (Enum.TryParse(Item?.InnerText ?? "", out TouchButtonActivationMode tempMode))
                    {
                        store.touchpadButtonMode[device] = tempMode;
                    }
                }
                catch { store.touchpadButtonMode[device] = TouchButtonActivationMode.Click; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/OutputContDevice"); store.outputDevType[device] = OutContDeviceId(Item.InnerText); }
                catch { store.outputDevType[device] = OutContType.X360; missingSetting = true; }

                // Only change xinput devices under certain conditions. Avoid
                // performing this upon program startup before loading devices.
                if (xinputChange && device < ControlService.CURRENT_DS4_CONTROLLER_LIMIT)
                {
                    store.CheckOldDevicestatus(device, control, oldContType,
                        out xinputPlug, out xinputStatus);
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ProfileActions");
                    store.profileActions[device].Clear();
                    if (!string.IsNullOrEmpty(Item.InnerText))
                    {
                        string[] actionNames = Item.InnerText.Split('/');
                        for (int actIndex = 0, actLen = actionNames.Length; actIndex < actLen; actIndex++)
                        {
                            string tempActionName = actionNames[actIndex];
                            if (!store.profileActions[device].Contains(tempActionName))
                            {
                                store.profileActions[device].Add(tempActionName);
                            }
                        }
                    }
                }
                catch { store.profileActions[device].Clear(); missingSetting = true; }

                foreach (DS4ControlSettings dcs in store.ds4settings[device])
                    dcs.Reset();

                store.containsCustomAction[device] = false;
                store.containsCustomExtras[device] = false;
                store.profileActionCount[device] = store.profileActions[device].Count;
                store.profileActionDict[device].Clear();
                store.profileActionIndexDict[device].Clear();
                foreach (string actionname in store.profileActions[device])
                {
                    store.profileActionDict[device][actionname] = store.GetAction(actionname);
                    store.profileActionIndexDict[device][actionname] = store.GetActionIndexOf(actionname);
                }

                DS4KeyType keyType;
                ushort wvk;

                {
                    XmlNode ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/Control/Button");
                    if (ParentItem != null)
                    {
                        foreach (XmlNode item in ParentItem.ChildNodes)
                        {
                            if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                            {
                                store.UpdateDS4CSetting(device, item.Name, false, store.getX360ControlsByName(item.InnerText), "", DS4KeyType.None, 0);
                                customMapButtons.Add(store.getDS4ControlsByName(item.Name), store.getX360ControlsByName(item.InnerText));
                            }
                        }
                    }

                    ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/Control/Macro");
                    if (ParentItem != null)
                    {
                        foreach (XmlNode item in ParentItem.ChildNodes)
                        {
                            customMapMacros.Add(store.getDS4ControlsByName(item.Name), item.InnerText);
                            string[] skeys;
                            int[] keys;
                            if (!string.IsNullOrEmpty(item.InnerText))
                            {
                                skeys = item.InnerText.Split('/');
                                keys = new int[skeys.Length];
                            }
                            else
                            {
                                skeys = new string[0];
                                keys = new int[0];
                            }

                            for (int i = 0, keylen = keys.Length; i < keylen; i++)
                                keys[i] = int.Parse(skeys[i]);

                            if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                            {
                                store.UpdateDS4CSetting(device, item.Name, false, keys, "", DS4KeyType.None, 0);
                            }
                        }
                    }

                    ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/Control/Key");
                    if (ParentItem != null)
                    {
                        foreach (XmlNode item in ParentItem.ChildNodes)
                        {
                            if (ushort.TryParse(item.InnerText, out wvk) && Enum.TryParse(item.Name, out DS4Controls currentControl))
                            {
                                store.UpdateDS4CSetting(device, item.Name, false, wvk, "", DS4KeyType.None, 0);
                                customMapKeys.Add(store.getDS4ControlsByName(item.Name), wvk);
                            }
                        }
                    }

                    ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/Control/Extras");
                    if (ParentItem != null)
                    {
                        foreach (XmlNode item in ParentItem.ChildNodes)
                        {
                            if (item.InnerText != string.Empty && Enum.TryParse(item.Name, out DS4Controls currentControl))
                            {
                                store.UpdateDS4CExtra(device, item.Name, false, item.InnerText);
                                customMapExtras.Add(store.getDS4ControlsByName(item.Name), item.InnerText);
                            }
                            else
                                ParentItem.RemoveChild(item);
                        }
                    }

                    ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/Control/KeyType");
                    if (ParentItem != null)
                    {
                        foreach (XmlNode item in ParentItem.ChildNodes)
                        {
                            if (item != null)
                            {
                                keyType = DS4KeyType.None;
                                if (item.InnerText.Contains(DS4KeyType.ScanCode.ToString()))
                                    keyType |= DS4KeyType.ScanCode;
                                if (item.InnerText.Contains(DS4KeyType.Toggle.ToString()))
                                    keyType |= DS4KeyType.Toggle;
                                if (item.InnerText.Contains(DS4KeyType.Macro.ToString()))
                                    keyType |= DS4KeyType.Macro;
                                if (item.InnerText.Contains(DS4KeyType.HoldMacro.ToString()))
                                    keyType |= DS4KeyType.HoldMacro;
                                if (item.InnerText.Contains(DS4KeyType.Unbound.ToString()))
                                    keyType |= DS4KeyType.Unbound;

                                if (keyType != DS4KeyType.None && Enum.TryParse(item.Name, out DS4Controls currentControl))
                                {
                                    store.UpdateDS4CKeyType(device, item.Name, false, keyType);
                                    customMapKeyTypes.Add(store.getDS4ControlsByName(item.Name), keyType);
                                }
                            }
                        }
                    }

                    ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftControl/Button");
                    if (ParentItem != null)
                    {
                        foreach (XmlElement item in ParentItem.ChildNodes)
                        {
                            int shiftT = shiftM;
                            if (item.HasAttribute("Trigger"))
                                int.TryParse(item.Attributes["Trigger"].Value, out shiftT);

                            if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                            {
                                store.UpdateDS4CSetting(device, item.Name, true, store.getX360ControlsByName(item.InnerText), "", DS4KeyType.None, shiftT);
                                shiftCustomMapButtons.Add(store.getDS4ControlsByName(item.Name), store.getX360ControlsByName(item.InnerText));
                            }
                        }
                    }

                    ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftControl/Macro");
                    if (ParentItem != null)
                    {
                        foreach (XmlElement item in ParentItem.ChildNodes)
                        {
                            shiftCustomMapMacros.Add(store.getDS4ControlsByName(item.Name), item.InnerText);
                            string[] skeys;
                            int[] keys;
                            if (!string.IsNullOrEmpty(item.InnerText))
                            {
                                skeys = item.InnerText.Split('/');
                                keys = new int[skeys.Length];
                            }
                            else
                            {
                                skeys = new string[0];
                                keys = new int[0];
                            }

                            for (int i = 0, keylen = keys.Length; i < keylen; i++)
                                keys[i] = int.Parse(skeys[i]);

                            int shiftT = shiftM;
                            if (item.HasAttribute("Trigger"))
                                int.TryParse(item.Attributes["Trigger"].Value, out shiftT);

                            if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                            {
                                store.UpdateDS4CSetting(device, item.Name, true, keys, "", DS4KeyType.None, shiftT);
                            }
                        }
                    }

                    ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftControl/Key");
                    if (ParentItem != null)
                    {
                        foreach (XmlElement item in ParentItem.ChildNodes)
                        {
                            if (ushort.TryParse(item.InnerText, out wvk))
                            {
                                int shiftT = shiftM;
                                if (item.HasAttribute("Trigger"))
                                    int.TryParse(item.Attributes["Trigger"].Value, out shiftT);

                                if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                                {
                                    store.UpdateDS4CSetting(device, item.Name, true, wvk, "", DS4KeyType.None, shiftT);
                                    shiftCustomMapKeys.Add(store.getDS4ControlsByName(item.Name), wvk);
                                }
                            }
                        }
                    }

                    ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftControl/Extras");
                    if (ParentItem != null)
                    {
                        foreach (XmlElement item in ParentItem.ChildNodes)
                        {
                            if (item.InnerText != string.Empty)
                            {
                                if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                                {
                                    store.UpdateDS4CExtra(device, item.Name, true, item.InnerText);
                                    shiftCustomMapExtras.Add(store.getDS4ControlsByName(item.Name), item.InnerText);
                                }
                            }
                            else
                                ParentItem.RemoveChild(item);
                        }
                    }

                    ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftControl/KeyType");
                    if (ParentItem != null)
                    {
                        foreach (XmlElement item in ParentItem.ChildNodes)
                        {
                            if (item != null)
                            {
                                keyType = DS4KeyType.None;
                                if (item.InnerText.Contains(DS4KeyType.ScanCode.ToString()))
                                    keyType |= DS4KeyType.ScanCode;
                                if (item.InnerText.Contains(DS4KeyType.Toggle.ToString()))
                                    keyType |= DS4KeyType.Toggle;
                                if (item.InnerText.Contains(DS4KeyType.Macro.ToString()))
                                    keyType |= DS4KeyType.Macro;
                                if (item.InnerText.Contains(DS4KeyType.HoldMacro.ToString()))
                                    keyType |= DS4KeyType.HoldMacro;
                                if (item.InnerText.Contains(DS4KeyType.Unbound.ToString()))
                                    keyType |= DS4KeyType.Unbound;

                                if (keyType != DS4KeyType.None && Enum.TryParse(item.Name, out DS4Controls currentControl))
                                {
                                    store.UpdateDS4CKeyType(device, item.Name, true, keyType);
                                    shiftCustomMapKeyTypes.Add(store.getDS4ControlsByName(item.Name), keyType);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Loaded = false;
                store.ResetProfile(device);
                store.ResetMouseProperties(device, control);

                // Unplug existing output device if requested profile does not exist
                OutputDevice tempOutDev = device < ControlService.CURRENT_DS4_CONTROLLER_LIMIT ?
                    control.outputDevices[device] : null;
                if (tempOutDev != null)
                {
                    tempOutDev = null;
                    //Global.activeOutDevType[device] = OutContType.None;
                    DS4Device tempDev = control.DS4Controllers[device];
                    if (tempDev != null)
                    {
                        tempDev.queueEvent(() =>
                        {
                            control.UnplugOutDev(device, tempDev);
                        });
                    }
                }
            }

            // Only add missing settings if the actual load was graceful
            if (missingSetting && Loaded)// && buttons != null)
            {
                string proName = Path.GetFileName(profilepath);
                SaveProfile(device, proName);
            }

            if (Loaded)
            {
                store.CacheProfileCustomsFlags(device);
                store.buttonMouseInfos[device].activeButtonSensitivity =
                    store.buttonMouseInfos[device].buttonSensitivity;

                //if (device < Global.MAX_DS4_CONTROLLER_COUNT && control.touchPad[device] != null)
                //{
                //    control.touchPad[device]?.ResetToggleGyroModes();
                //    GyroOutMode currentGyro = gyroOutMode[device];
                //    if (currentGyro == GyroOutMode.Mouse)
                //    {
                //        control.touchPad[device].ToggleGyroMouse =
                //            gyroMouseToggle[device];
                //    }
                //    else if (currentGyro == GyroOutMode.MouseJoystick)
                //    {
                //        control.touchPad[device].ToggleGyroMouse =
                //            gyroMouseStickToggle[device];
                //    }
                //}

                // If a device exists, make sure to transfer relevant profile device
                // options to device instance
                if (postLoad && device < ControllerLimits.MaxControllerCount)
                {
                    store.PostLoadSnippet(device, control, xinputStatus, xinputPlug);
                }
            }

            return Loaded;
        }

        private string ReadLegacyElement(string rootname, string name)
        {
            return m_Xdoc.SelectSingleNode("/" + rootname + "/" + name)?.InnerText;
        }

        public void LoadLegacyExtraSettings(int device, string rootname)
        {
            if (int.TryParse(ReadLegacyElement(rootname, "DebouncingMs"), out int debounceMs))
                store.debouncingMs[device] = Math.Min(Math.Max(debounceMs, 0), 1000);

            if (bool.TryParse(ReadLegacyElement(rootname, "InverseRumbleMotors"), out bool inverseRumble))
                store.inverseRumbleMotors[device] = inverseRumble;

            // useDs3PitchRollSim は全体で 1 つの値（ProfileDTO.MapTo と同じ扱い）。要素があるときだけ代入する
            if (bool.TryParse(ReadLegacyElement(rootname, "UseDs3PitchRollSim"), out bool ds3Sim))
                store.useDs3PitchRollSim = ds3Sim;

            if (sbyte.TryParse(ReadLegacyElement(rootname, "LeftStickDriftXAxis"), out sbyte value))
                store.leftStickDriftXAxis[device] = value;
            if (sbyte.TryParse(ReadLegacyElement(rootname, "LeftStickDriftYAxis"), out value))
                store.leftStickDriftYAxis[device] = value;
            if (sbyte.TryParse(ReadLegacyElement(rootname, "RightStickDriftXAxis"), out value))
                store.rightStickDriftXAxis[device] = value;
            if (sbyte.TryParse(ReadLegacyElement(rootname, "RightStickDriftYAxis"), out value))
                store.rightStickDriftYAxis[device] = value;

            string triggerMode = ReadLegacyElement(rootname, "DS4OutputTriggerMode");
            if (triggerMode != null && Enum.TryParse(triggerMode, out DS4TriggerOutputMode mode) &&
                Enum.IsDefined(typeof(DS4TriggerOutputMode), mode))
            {
                store.outputDS4TriggerMode[device] = mode;
            }

            if (bool.TryParse(ReadLegacyElement(rootname, "GyroMouseJitterCompensation"), out bool gyroMouseJitter))
                store.gyroMouseInfo[device].jitterCompensation = gyroMouseJitter;
            if (bool.TryParse(ReadLegacyElement(rootname, "GyroMouseStickJitterCompensation"), out bool gyroStickJitter))
                store.gyroMStickInfo[device].jitterCompensation = gyroStickJitter;
        }

        private string axisOutputCurveString(int id)
        {
            return store.stickOutputCurveString(id);
        }

        private string OutContDeviceString(OutContType id)
        {
            string result = "X360";
            switch (id)
            {
                case OutContType.None:
                case OutContType.X360: result = "X360"; break;
                case OutContType.DS4: result = "DS4"; break;
                default: break;
            }

            return result;
        }

        private string GetLightbarModeString(LightbarMode mode)
        {
            string result = "DS4Win";
            switch (mode)
            {
                case LightbarMode.DS4Win:
                    result = "DS4Win";
                    break;
                case LightbarMode.Passthru:
                    result = "Passthru";
                    break;
                default:
                    break;
            }
            return result;
        }

        public bool SaveAsProfile(int device, string proName)
        {
            bool Saved = true;
            store.ResetProfile(device);
            Saved = SaveProfile(device, proName);
            return Saved;
        }

        public bool SaveProfile(int device, string proName)
        {
            bool Saved = true;
            //string path = _pathService.AppDataPath + @"\Profiles\" + Path.GetFileNameWithoutExtension(proName) + ".xml";
            if (proName.EndsWith(XmlExtension))
            {
                proName = proName.Remove(proName.LastIndexOf(XmlExtension));
            }

            string path = $@"{_pathService.AppDataPath}\Profiles\{proName}{XmlExtension}";
            try
            {
                XmlNode tmpNode;
                XmlNode xmlControls = m_Xdoc.SelectSingleNode("/DS4Windows/Control");
                XmlNode xmlShiftControls = m_Xdoc.SelectSingleNode("/DS4Windows/ShiftControl");
                m_Xdoc.RemoveAll();

                tmpNode = m_Xdoc.CreateXmlDeclaration("1.0", "utf-8", string.Empty);
                m_Xdoc.AppendChild(tmpNode);

                tmpNode = m_Xdoc.CreateComment(string.Format(" DS4Windows Configuration Data. {0} ", DateTime.Now));
                m_Xdoc.AppendChild(tmpNode);

                tmpNode = m_Xdoc.CreateComment(string.Format(" Made with DS4Windows version {0} ", _environment.ApplicationVersion));
                m_Xdoc.AppendChild(tmpNode);

                tmpNode = m_Xdoc.CreateWhitespace("\r\n");
                m_Xdoc.AppendChild(tmpNode);

                XmlElement rootElement = m_Xdoc.CreateElement("DS4Windows", null);
                rootElement.SetAttribute("app_version", _environment.ApplicationVersion);
                rootElement.SetAttribute("config_version", ConfigVersion.ToString());

                LightbarSettingInfo lightbarSettings = store.lightbarSettingInfo[device];
                LightbarDS4WinInfo lightInfo = lightbarSettings.ds4winSettings;

                XmlNode xmlTouchToggle = m_Xdoc.CreateNode(XmlNodeType.Element, "touchToggle", null); xmlTouchToggle.InnerText = store.enableTouchToggle[device].ToString(); rootElement.AppendChild(xmlTouchToggle);
                XmlNode xmlIdleDisconnectTimeout = m_Xdoc.CreateNode(XmlNodeType.Element, "idleDisconnectTimeout", null); xmlIdleDisconnectTimeout.InnerText = store.idleDisconnectTimeout[device].ToString(); rootElement.AppendChild(xmlIdleDisconnectTimeout);
                XmlNode xmlOutputDataToDS4 = m_Xdoc.CreateNode(XmlNodeType.Element, "outputDataToDS4", null); xmlOutputDataToDS4.InnerText = store.enableOutputDataToDS4[device].ToString(); rootElement.AppendChild(xmlOutputDataToDS4);
                XmlNode xmlColor = m_Xdoc.CreateNode(XmlNodeType.Element, "Color", null);
                xmlColor.InnerText = lightInfo.m_Led.red.ToString() + "," + lightInfo.m_Led.green.ToString() + "," + lightInfo.m_Led.blue.ToString();
                rootElement.AppendChild(xmlColor);
                XmlNode xmlRumbleBoost = m_Xdoc.CreateNode(XmlNodeType.Element, "RumbleBoost", null); xmlRumbleBoost.InnerText = store.rumble[device].ToString(); rootElement.AppendChild(xmlRumbleBoost);
                XmlNode xmlRumbleAutostopTime = m_Xdoc.CreateNode(XmlNodeType.Element, "RumbleAutostopTime", null); xmlRumbleAutostopTime.InnerText = store.rumbleAutostopTime[device].ToString(); rootElement.AppendChild(xmlRumbleAutostopTime);
                XmlNode xmlLightbarMode = m_Xdoc.CreateNode(XmlNodeType.Element, "LightbarMode", null); xmlLightbarMode.InnerText = GetLightbarModeString(lightbarSettings.mode); rootElement.AppendChild(xmlLightbarMode);
                XmlNode xmlLedAsBatteryIndicator = m_Xdoc.CreateNode(XmlNodeType.Element, "ledAsBatteryIndicator", null); xmlLedAsBatteryIndicator.InnerText = lightInfo.ledAsBattery.ToString(); rootElement.AppendChild(xmlLedAsBatteryIndicator);
                XmlNode xmlLowBatteryFlash = m_Xdoc.CreateNode(XmlNodeType.Element, "FlashType", null); xmlLowBatteryFlash.InnerText = lightInfo.flashType.ToString(); rootElement.AppendChild(xmlLowBatteryFlash);
                XmlNode xmlFlashBatterAt = m_Xdoc.CreateNode(XmlNodeType.Element, "flashBatteryAt", null); xmlFlashBatterAt.InnerText = lightInfo.flashAt.ToString(); rootElement.AppendChild(xmlFlashBatterAt);
                XmlNode xmlTouchSensitivity = m_Xdoc.CreateNode(XmlNodeType.Element, "touchSensitivity", null); xmlTouchSensitivity.InnerText = store.touchSensitivity[device].ToString(); rootElement.AppendChild(xmlTouchSensitivity);
                XmlNode xmlLowColor = m_Xdoc.CreateNode(XmlNodeType.Element, "LowColor", null);
                xmlLowColor.InnerText = lightInfo.m_LowLed.red.ToString() + "," + lightInfo.m_LowLed.green.ToString() + "," + lightInfo.m_LowLed.blue.ToString();
                rootElement.AppendChild(xmlLowColor);
                XmlNode xmlChargingColor = m_Xdoc.CreateNode(XmlNodeType.Element, "ChargingColor", null);
                xmlChargingColor.InnerText = lightInfo.m_ChargingLed.red.ToString() + "," + lightInfo.m_ChargingLed.green.ToString() + "," + lightInfo.m_ChargingLed.blue.ToString();
                rootElement.AppendChild(xmlChargingColor);
                XmlNode xmlFlashColor = m_Xdoc.CreateNode(XmlNodeType.Element, "FlashColor", null);
                xmlFlashColor.InnerText = lightInfo.m_FlashLed.red.ToString() + "," + lightInfo.m_FlashLed.green.ToString() + "," + lightInfo.m_FlashLed.blue.ToString();
                rootElement.AppendChild(xmlFlashColor);
                XmlNode xmlTouchpadJitterCompensation = m_Xdoc.CreateNode(XmlNodeType.Element, "touchpadJitterCompensation", null); xmlTouchpadJitterCompensation.InnerText = store.touchpadJitterCompensation[device].ToString(); rootElement.AppendChild(xmlTouchpadJitterCompensation);
                // Phase6-Step12: 旧形式（3.9.9 互換）の保存で欠けていた 10 要素（要素名は ProfileDTO と同じ。旧版は未知の要素を無視する）
                SaveLegacyExtraSettings(rootElement, device);
                XmlNode xmlLowerRCOn = m_Xdoc.CreateNode(XmlNodeType.Element, "lowerRCOn", null); xmlLowerRCOn.InnerText = store.lowerRCOn[device].ToString(); rootElement.AppendChild(xmlLowerRCOn);
                XmlNode xmlTapSensitivity = m_Xdoc.CreateNode(XmlNodeType.Element, "tapSensitivity", null); xmlTapSensitivity.InnerText = store.tapSensitivity[device].ToString(); rootElement.AppendChild(xmlTapSensitivity);
                XmlNode xmlDouble = m_Xdoc.CreateNode(XmlNodeType.Element, "doubleTap", null); xmlDouble.InnerText = store.doubleTap[device].ToString(); rootElement.AppendChild(xmlDouble);
                XmlNode xmlScrollSensitivity = m_Xdoc.CreateNode(XmlNodeType.Element, "scrollSensitivity", null); xmlScrollSensitivity.InnerText = store.scrollSensitivity[device].ToString(); rootElement.AppendChild(xmlScrollSensitivity);
                XmlNode xmlLeftTriggerMiddle = m_Xdoc.CreateNode(XmlNodeType.Element, "LeftTriggerMiddle", null); xmlLeftTriggerMiddle.InnerText = store.l2ModInfo[device].deadZone.ToString(); rootElement.AppendChild(xmlLeftTriggerMiddle);
                XmlNode xmlRightTriggerMiddle = m_Xdoc.CreateNode(XmlNodeType.Element, "RightTriggerMiddle", null); xmlRightTriggerMiddle.InnerText = store.r2ModInfo[device].deadZone.ToString(); rootElement.AppendChild(xmlRightTriggerMiddle);
                XmlNode xmlTouchpadInvert = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchpadInvert", null); xmlTouchpadInvert.InnerText = store.touchpadInvert[device].ToString(); rootElement.AppendChild(xmlTouchpadInvert);
                XmlNode xmlTouchClickPasthru = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchpadClickPassthru", null); xmlTouchClickPasthru.InnerText = store.touchClickPassthru[device].ToString(); rootElement.AppendChild(xmlTouchClickPasthru);

                XmlNode xmlL2AD = m_Xdoc.CreateNode(XmlNodeType.Element, "L2AntiDeadZone", null); xmlL2AD.InnerText = store.l2ModInfo[device].antiDeadZone.ToString(); rootElement.AppendChild(xmlL2AD);
                XmlNode xmlR2AD = m_Xdoc.CreateNode(XmlNodeType.Element, "R2AntiDeadZone", null); xmlR2AD.InnerText = store.r2ModInfo[device].antiDeadZone.ToString(); rootElement.AppendChild(xmlR2AD);
                XmlNode xmlL2Maxzone = m_Xdoc.CreateNode(XmlNodeType.Element, "L2MaxZone", null); xmlL2Maxzone.InnerText = store.l2ModInfo[device].maxZone.ToString(); rootElement.AppendChild(xmlL2Maxzone);
                XmlNode xmlR2Maxzone = m_Xdoc.CreateNode(XmlNodeType.Element, "R2MaxZone", null); xmlR2Maxzone.InnerText = store.r2ModInfo[device].maxZone.ToString(); rootElement.AppendChild(xmlR2Maxzone);
                XmlNode xmlL2MaxOutput = m_Xdoc.CreateNode(XmlNodeType.Element, "L2MaxOutput", null); xmlL2MaxOutput.InnerText = store.l2ModInfo[device].maxOutput.ToString(); rootElement.AppendChild(xmlL2MaxOutput);
                XmlNode xmlR2MaxOutput = m_Xdoc.CreateNode(XmlNodeType.Element, "R2MaxOutput", null); xmlR2MaxOutput.InnerText = store.r2ModInfo[device].maxOutput.ToString(); rootElement.AppendChild(xmlR2MaxOutput);
                XmlNode xmlButtonMouseSensitivity = m_Xdoc.CreateNode(XmlNodeType.Element, "ButtonMouseSensitivity", null); xmlButtonMouseSensitivity.InnerText = store.buttonMouseInfos[device].buttonSensitivity.ToString(); rootElement.AppendChild(xmlButtonMouseSensitivity);
                XmlNode xmlButtonMouseOffset = m_Xdoc.CreateNode(XmlNodeType.Element, "ButtonMouseOffset", null); xmlButtonMouseOffset.InnerText = store.buttonMouseInfos[device].mouseVelocityOffset.ToString(); rootElement.AppendChild(xmlButtonMouseOffset);
                XmlNode xmlRainbow = m_Xdoc.CreateNode(XmlNodeType.Element, "Rainbow", null); xmlRainbow.InnerText = lightInfo.rainbow.ToString(); rootElement.AppendChild(xmlRainbow);
                XmlNode xmlMaxSatRainbow = m_Xdoc.CreateNode(XmlNodeType.Element, "MaxSatRainbow", null); xmlMaxSatRainbow.InnerText = Convert.ToInt32(lightInfo.maxRainbowSat * 100.0).ToString(); rootElement.AppendChild(xmlMaxSatRainbow);
                XmlNode xmlLSD = m_Xdoc.CreateNode(XmlNodeType.Element, "LSDeadZone", null); xmlLSD.InnerText = store.lsModInfo[device].deadZone.ToString(); rootElement.AppendChild(xmlLSD);
                XmlNode xmlRSD = m_Xdoc.CreateNode(XmlNodeType.Element, "RSDeadZone", null); xmlRSD.InnerText = store.rsModInfo[device].deadZone.ToString(); rootElement.AppendChild(xmlRSD);
                XmlNode xmlLSAD = m_Xdoc.CreateNode(XmlNodeType.Element, "LSAntiDeadZone", null); xmlLSAD.InnerText = store.lsModInfo[device].antiDeadZone.ToString(); rootElement.AppendChild(xmlLSAD);
                XmlNode xmlRSAD = m_Xdoc.CreateNode(XmlNodeType.Element, "RSAntiDeadZone", null); xmlRSAD.InnerText = store.rsModInfo[device].antiDeadZone.ToString(); rootElement.AppendChild(xmlRSAD);
                XmlNode xmlLSMaxZone = m_Xdoc.CreateNode(XmlNodeType.Element, "LSMaxZone", null); xmlLSMaxZone.InnerText = store.lsModInfo[device].maxZone.ToString(); rootElement.AppendChild(xmlLSMaxZone);
                XmlNode xmlRSMaxZone = m_Xdoc.CreateNode(XmlNodeType.Element, "RSMaxZone", null); xmlRSMaxZone.InnerText = store.rsModInfo[device].maxZone.ToString(); rootElement.AppendChild(xmlRSMaxZone);
                XmlNode xmlLSVerticalScale = m_Xdoc.CreateNode(XmlNodeType.Element, "LSVerticalScale", null); xmlLSVerticalScale.InnerText = store.lsModInfo[device].verticalScale.ToString(); rootElement.AppendChild(xmlLSVerticalScale);
                XmlNode xmlRSVerticalScale = m_Xdoc.CreateNode(XmlNodeType.Element, "RSVerticalScale", null); xmlRSVerticalScale.InnerText = store.rsModInfo[device].verticalScale.ToString(); rootElement.AppendChild(xmlRSVerticalScale);
                XmlNode xmlLSMaxOutput = m_Xdoc.CreateNode(XmlNodeType.Element, "LSMaxOutput", null); xmlLSMaxOutput.InnerText = store.lsModInfo[device].maxOutput.ToString(); rootElement.AppendChild(xmlLSMaxOutput);
                XmlNode xmlRSMaxOutput = m_Xdoc.CreateNode(XmlNodeType.Element, "RSMaxOutput", null); xmlRSMaxOutput.InnerText = store.rsModInfo[device].maxOutput.ToString(); rootElement.AppendChild(xmlRSMaxOutput);
                XmlNode xmlLSMaxOutputForce = m_Xdoc.CreateNode(XmlNodeType.Element, "LSMaxOutputForce", null); xmlLSMaxOutputForce.InnerText = store.lsModInfo[device].maxOutputForce.ToString(); rootElement.AppendChild(xmlLSMaxOutputForce);
                XmlNode xmlRSMaxOutputForce = m_Xdoc.CreateNode(XmlNodeType.Element, "RSMaxOutputForce", null); xmlRSMaxOutputForce.InnerText = store.rsModInfo[device].maxOutputForce.ToString(); rootElement.AppendChild(xmlRSMaxOutputForce);
                XmlNode xmlLSDeadZoneType = m_Xdoc.CreateNode(XmlNodeType.Element, "LSDeadZoneType", null); xmlLSDeadZoneType.InnerText = store.lsModInfo[device].deadzoneType.ToString(); rootElement.AppendChild(xmlLSDeadZoneType);
                XmlNode xmlRSDeadZoneType = m_Xdoc.CreateNode(XmlNodeType.Element, "RSDeadZoneType", null); xmlRSDeadZoneType.InnerText = store.rsModInfo[device].deadzoneType.ToString(); rootElement.AppendChild(xmlRSDeadZoneType);

                XmlElement xmlLSAxialDeadGroupEl = m_Xdoc.CreateElement("LSAxialDeadOptions");
                XmlElement xmlLSAxialDeadX = m_Xdoc.CreateElement("DeadZoneX"); xmlLSAxialDeadX.InnerText = store.lsModInfo[device].xAxisDeadInfo.deadZone.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialDeadX);
                XmlElement xmlLSAxialDeadY = m_Xdoc.CreateElement("DeadZoneY"); xmlLSAxialDeadY.InnerText = store.lsModInfo[device].yAxisDeadInfo.deadZone.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialDeadY);
                XmlElement xmlLSAxialMaxX = m_Xdoc.CreateElement("MaxZoneX"); xmlLSAxialMaxX.InnerText = store.lsModInfo[device].xAxisDeadInfo.maxZone.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialMaxX);
                XmlElement xmlLSAxialMaxY = m_Xdoc.CreateElement("MaxZoneY"); xmlLSAxialMaxY.InnerText = store.lsModInfo[device].yAxisDeadInfo.maxZone.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialMaxY);
                XmlElement xmlLSAxialAntiDeadX = m_Xdoc.CreateElement("AntiDeadZoneX"); xmlLSAxialAntiDeadX.InnerText = store.lsModInfo[device].xAxisDeadInfo.antiDeadZone.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialAntiDeadX);
                XmlElement xmlLSAxialAntiDeadY = m_Xdoc.CreateElement("AntiDeadZoneY"); xmlLSAxialAntiDeadY.InnerText = store.lsModInfo[device].yAxisDeadInfo.antiDeadZone.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialAntiDeadY);
                XmlElement xmlLSAxialMaxOutputX = m_Xdoc.CreateElement("MaxOutputX"); xmlLSAxialMaxOutputX.InnerText = store.lsModInfo[device].xAxisDeadInfo.maxOutput.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialMaxOutputX);
                XmlElement xmlLSAxialMaxOutputY = m_Xdoc.CreateElement("MaxOutputY"); xmlLSAxialMaxOutputY.InnerText = store.lsModInfo[device].yAxisDeadInfo.maxOutput.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialMaxOutputY);
                rootElement.AppendChild(xmlLSAxialDeadGroupEl);

                XmlElement xmlRSAxialDeadGroupEl = m_Xdoc.CreateElement("RSAxialDeadOptions");
                XmlElement xmlRSAxialDeadX = m_Xdoc.CreateElement("DeadZoneX"); xmlRSAxialDeadX.InnerText = store.rsModInfo[device].xAxisDeadInfo.deadZone.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialDeadX);
                XmlElement xmlRSAxialDeadY = m_Xdoc.CreateElement("DeadZoneY"); xmlRSAxialDeadY.InnerText = store.rsModInfo[device].yAxisDeadInfo.deadZone.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialDeadY);
                XmlElement xmlRSAxialMaxX = m_Xdoc.CreateElement("MaxZoneX"); xmlRSAxialMaxX.InnerText = store.rsModInfo[device].xAxisDeadInfo.maxZone.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialMaxX);
                XmlElement xmlRSAxialMaxY = m_Xdoc.CreateElement("MaxZoneY"); xmlRSAxialMaxY.InnerText = store.rsModInfo[device].yAxisDeadInfo.maxZone.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialMaxY);
                XmlElement xmlRSAxialAntiDeadX = m_Xdoc.CreateElement("AntiDeadZoneX"); xmlRSAxialAntiDeadX.InnerText = store.rsModInfo[device].xAxisDeadInfo.antiDeadZone.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialAntiDeadX);
                XmlElement xmlRSAxialAntiDeadY = m_Xdoc.CreateElement("AntiDeadZoneY"); xmlRSAxialAntiDeadY.InnerText = store.rsModInfo[device].yAxisDeadInfo.antiDeadZone.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialAntiDeadY);
                XmlElement xmlRSAxialMaxOutputX = m_Xdoc.CreateElement("MaxOutputX"); xmlRSAxialMaxOutputX.InnerText = store.rsModInfo[device].xAxisDeadInfo.maxOutput.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialMaxOutputX);
                XmlElement xmlRSAxialMaxOutputY = m_Xdoc.CreateElement("MaxOutputY"); xmlRSAxialMaxOutputY.InnerText = store.rsModInfo[device].yAxisDeadInfo.maxOutput.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialMaxOutputY);
                rootElement.AppendChild(xmlRSAxialDeadGroupEl);

                // Output rotation values to profile in degrees
                XmlNode xmlLSRotation = m_Xdoc.CreateNode(XmlNodeType.Element, "LSRotation", null); xmlLSRotation.InnerText = Convert.ToInt32(store.LSRotation[device] * 180.0 / Math.PI).ToString(); rootElement.AppendChild(xmlLSRotation);
                XmlNode xmlRSRotation = m_Xdoc.CreateNode(XmlNodeType.Element, "RSRotation", null); xmlRSRotation.InnerText = Convert.ToInt32(store.RSRotation[device] * 180.0 / Math.PI).ToString(); rootElement.AppendChild(xmlRSRotation);

                XmlNode xmlLSFuzz = m_Xdoc.CreateNode(XmlNodeType.Element, "LSFuzz", null); xmlLSFuzz.InnerText = store.lsModInfo[device].fuzz.ToString(); rootElement.AppendChild(xmlLSFuzz);
                XmlNode xmlRSFuzz = m_Xdoc.CreateNode(XmlNodeType.Element, "RSFuzz", null); xmlRSFuzz.InnerText = store.rsModInfo[device].fuzz.ToString(); rootElement.AppendChild(xmlRSFuzz);
                XmlNode xmlLSOuterBindDead = m_Xdoc.CreateNode(XmlNodeType.Element, "LSOuterBindDead", null); xmlLSOuterBindDead.InnerText = Convert.ToInt32(store.lsModInfo[device].outerBindDeadZone).ToString(); rootElement.AppendChild(xmlLSOuterBindDead);
                XmlNode xmlRSOuterBindDead = m_Xdoc.CreateNode(XmlNodeType.Element, "RSOuterBindDead", null); xmlRSOuterBindDead.InnerText = Convert.ToInt32(store.rsModInfo[device].outerBindDeadZone).ToString(); rootElement.AppendChild(xmlRSOuterBindDead);
                XmlNode xmlLSOuterBindInvert = m_Xdoc.CreateNode(XmlNodeType.Element, "LSOuterBindInvert", null); xmlLSOuterBindInvert.InnerText = store.lsModInfo[device].outerBindInvert.ToString(); rootElement.AppendChild(xmlLSOuterBindInvert);
                XmlNode xmlRSOuterBindInvert = m_Xdoc.CreateNode(XmlNodeType.Element, "RSOuterBindInvert", null); xmlRSOuterBindInvert.InnerText = store.rsModInfo[device].outerBindInvert.ToString(); rootElement.AppendChild(xmlRSOuterBindInvert);

                XmlElement xmlLSDeltaAccelGroupEl = m_Xdoc.CreateElement("LSDeltaAccelSettings");
                XmlElement xmlLSDeltaEnabled = m_Xdoc.CreateElement("Enabled"); xmlLSDeltaEnabled.InnerText = store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.enabled.ToString(); xmlLSDeltaAccelGroupEl.AppendChild(xmlLSDeltaEnabled);
                XmlElement xmlLSDeltaMultiplier = m_Xdoc.CreateElement("Multiplier"); xmlLSDeltaMultiplier.InnerText = store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.multiplier.ToString(); xmlLSDeltaAccelGroupEl.AppendChild(xmlLSDeltaMultiplier);
                XmlElement xmlLSDeltaMaxTravel = m_Xdoc.CreateElement("MaxTravel"); xmlLSDeltaMaxTravel.InnerText = store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.maxTravel.ToString(); xmlLSDeltaAccelGroupEl.AppendChild(xmlLSDeltaMaxTravel);
                XmlElement xmlLSDeltaMinTravel = m_Xdoc.CreateElement("MinTravel"); xmlLSDeltaMinTravel.InnerText = store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minTravel.ToString(); xmlLSDeltaAccelGroupEl.AppendChild(xmlLSDeltaMinTravel);
                XmlElement xmlLSDeltaEasingDuration = m_Xdoc.CreateElement("EasingDuration"); xmlLSDeltaEasingDuration.InnerText = store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.easingDuration.ToString(); xmlLSDeltaAccelGroupEl.AppendChild(xmlLSDeltaEasingDuration);
                XmlElement xmlLSDeltaMinFactor = m_Xdoc.CreateElement("MinFactor"); xmlLSDeltaMinFactor.InnerText = store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minfactor.ToString(); xmlLSDeltaAccelGroupEl.AppendChild(xmlLSDeltaMinFactor);
                rootElement.AppendChild(xmlLSDeltaAccelGroupEl);

                XmlElement xmlRSDeltaAccelGroupEl = m_Xdoc.CreateElement("RSDeltaAccelSettings");
                XmlElement xmlRSDeltaEnabled = m_Xdoc.CreateElement("Enabled"); xmlRSDeltaEnabled.InnerText = store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.enabled.ToString(); xmlRSDeltaAccelGroupEl.AppendChild(xmlRSDeltaEnabled);
                XmlElement xmlRSDeltaMultiplier = m_Xdoc.CreateElement("Multiplier"); xmlRSDeltaMultiplier.InnerText = store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.multiplier.ToString(); xmlRSDeltaAccelGroupEl.AppendChild(xmlRSDeltaMultiplier);
                XmlElement xmlRSDeltaMaxTravel = m_Xdoc.CreateElement("MaxTravel"); xmlRSDeltaMaxTravel.InnerText = store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.maxTravel.ToString(); xmlRSDeltaAccelGroupEl.AppendChild(xmlRSDeltaMaxTravel);
                XmlElement xmlRSDeltaMinTravel = m_Xdoc.CreateElement("MinTravel"); xmlRSDeltaMinTravel.InnerText = store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minTravel.ToString(); xmlRSDeltaAccelGroupEl.AppendChild(xmlRSDeltaMinTravel);
                XmlElement xmlRSDeltaEasingDuration = m_Xdoc.CreateElement("EasingDuration"); xmlRSDeltaEasingDuration.InnerText = store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.easingDuration.ToString(); xmlRSDeltaAccelGroupEl.AppendChild(xmlRSDeltaEasingDuration);
                XmlElement xmlRSDeltaMinFactor = m_Xdoc.CreateElement("MinFactor"); xmlRSDeltaMinFactor.InnerText = store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minfactor.ToString(); xmlRSDeltaAccelGroupEl.AppendChild(xmlRSDeltaMinFactor);
                rootElement.AppendChild(xmlRSDeltaAccelGroupEl);

                XmlNode xmlSXD = m_Xdoc.CreateNode(XmlNodeType.Element, "SXDeadZone", null); xmlSXD.InnerText = store.SXDeadzone[device].ToString(); rootElement.AppendChild(xmlSXD);
                XmlNode xmlSZD = m_Xdoc.CreateNode(XmlNodeType.Element, "SZDeadZone", null); xmlSZD.InnerText = store.SZDeadzone[device].ToString(); rootElement.AppendChild(xmlSZD);

                XmlNode xmlSXMaxzone = m_Xdoc.CreateNode(XmlNodeType.Element, "SXMaxZone", null); xmlSXMaxzone.InnerText = Convert.ToInt32(store.SXMaxzone[device] * 100.0).ToString(); rootElement.AppendChild(xmlSXMaxzone);
                XmlNode xmlSZMaxzone = m_Xdoc.CreateNode(XmlNodeType.Element, "SZMaxZone", null); xmlSZMaxzone.InnerText = Convert.ToInt32(store.SZMaxzone[device] * 100.0).ToString(); rootElement.AppendChild(xmlSZMaxzone);

                XmlNode xmlSXAntiDeadzone = m_Xdoc.CreateNode(XmlNodeType.Element, "SXAntiDeadZone", null); xmlSXAntiDeadzone.InnerText = Convert.ToInt32(store.SXAntiDeadzone[device] * 100.0).ToString(); rootElement.AppendChild(xmlSXAntiDeadzone);
                XmlNode xmlSZAntiDeadzone = m_Xdoc.CreateNode(XmlNodeType.Element, "SZAntiDeadZone", null); xmlSZAntiDeadzone.InnerText = Convert.ToInt32(store.SZAntiDeadzone[device] * 100.0).ToString(); rootElement.AppendChild(xmlSZAntiDeadzone);

                XmlNode xmlSens = m_Xdoc.CreateNode(XmlNodeType.Element, "Sensitivity", null);
                xmlSens.InnerText = $"{store.LSSens[device]}|{store.RSSens[device]}|{store.l2Sens[device]}|{store.r2Sens[device]}|{store.SXSens[device]}|{store.SZSens[device]}";
                rootElement.AppendChild(xmlSens);

                XmlNode xmlChargingType = m_Xdoc.CreateNode(XmlNodeType.Element, "ChargingType", null); xmlChargingType.InnerText = lightInfo.chargingType.ToString(); rootElement.AppendChild(xmlChargingType);
                XmlNode xmlMouseAccel = m_Xdoc.CreateNode(XmlNodeType.Element, "MouseAcceleration", null); xmlMouseAccel.InnerText = store.buttonMouseInfos[device].mouseAccel.ToString(); rootElement.AppendChild(xmlMouseAccel);
                XmlNode xmlMouseVerticalScale = m_Xdoc.CreateNode(XmlNodeType.Element, "ButtonMouseVerticalScale", null); xmlMouseVerticalScale.InnerText = Convert.ToInt32(store.buttonMouseInfos[device].buttonVerticalScale * 100).ToString(); rootElement.AppendChild(xmlMouseVerticalScale);
                //XmlNode xmlShiftMod = m_Xdoc.CreateNode(XmlNodeType.Element, "ShiftModifier", null); xmlShiftMod.InnerText = shiftModifier[device].ToString(); rootElement.AppendChild(xmlShiftMod);
                XmlNode xmlLaunchProgram = m_Xdoc.CreateNode(XmlNodeType.Element, "LaunchProgram", null); xmlLaunchProgram.InnerText = store.launchProgram[device].ToString(); rootElement.AppendChild(xmlLaunchProgram);
                XmlNode xmlDinput = m_Xdoc.CreateNode(XmlNodeType.Element, "DinputOnly", null); xmlDinput.InnerText = store.dinputOnly[device].ToString(); rootElement.AppendChild(xmlDinput);
                XmlNode xmlStartTouchpadOff = m_Xdoc.CreateNode(XmlNodeType.Element, "StartTouchpadOff", null); xmlStartTouchpadOff.InnerText = store.startTouchpadOff[device].ToString(); rootElement.AppendChild(xmlStartTouchpadOff);
                XmlNode xmlTouchOutMode = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchpadOutputMode", null); xmlTouchOutMode.InnerText = store.touchOutMode[device].ToString(); rootElement.AppendChild(xmlTouchOutMode);
                XmlNode xmlSATriggers = m_Xdoc.CreateNode(XmlNodeType.Element, "SATriggers", null); xmlSATriggers.InnerText = store.sATriggers[device].ToString(); rootElement.AppendChild(xmlSATriggers);
                XmlNode xmlSATriggerCond = m_Xdoc.CreateNode(XmlNodeType.Element, "SATriggerCond", null); xmlSATriggerCond.InnerText = BackingStore.SaTriggerCondString(store.sATriggerCond[device]); rootElement.AppendChild(xmlSATriggerCond);
                XmlNode xmlSASteeringWheelEmulationAxis = m_Xdoc.CreateNode(XmlNodeType.Element, "SASteeringWheelEmulationAxis", null); xmlSASteeringWheelEmulationAxis.InnerText = store.sASteeringWheelEmulationAxis[device].ToString("G"); rootElement.AppendChild(xmlSASteeringWheelEmulationAxis);
                XmlNode xmlSASteeringWheelEmulationRange = m_Xdoc.CreateNode(XmlNodeType.Element, "SASteeringWheelEmulationRange", null); xmlSASteeringWheelEmulationRange.InnerText = store.sASteeringWheelEmulationRange[device].ToString(); rootElement.AppendChild(xmlSASteeringWheelEmulationRange);
                XmlNode xmlSASteeringWheelFuzz = m_Xdoc.CreateNode(XmlNodeType.Element, "SASteeringWheelFuzz", null); xmlSASteeringWheelFuzz.InnerText = store.saWheelFuzzValues[device].ToString(); rootElement.AppendChild(xmlSASteeringWheelFuzz);

                XmlElement xmlSASteeringWheelSmoothingGroupEl = m_Xdoc.CreateElement("SASteeringWheelSmoothingOptions");
                XmlElement xmlSASteeringWheelUseSmoothing = m_Xdoc.CreateElement("SASteeringWheelUseSmoothing"); xmlSASteeringWheelUseSmoothing.InnerText = store.wheelSmoothInfo[device].Enabled.ToString(); xmlSASteeringWheelSmoothingGroupEl.AppendChild(xmlSASteeringWheelUseSmoothing);
                XmlElement xmlSASteeringWheelSmoothMinCutoff = m_Xdoc.CreateElement("SASteeringWheelSmoothMinCutoff"); xmlSASteeringWheelSmoothMinCutoff.InnerText = store.wheelSmoothInfo[device].MinCutoff.ToString(); xmlSASteeringWheelSmoothingGroupEl.AppendChild(xmlSASteeringWheelSmoothMinCutoff);
                XmlElement xmlSASteeringWheelSmoothBeta = m_Xdoc.CreateElement("SASteeringWheelSmoothBeta"); xmlSASteeringWheelSmoothBeta.InnerText = store.wheelSmoothInfo[device].Beta.ToString(); xmlSASteeringWheelSmoothingGroupEl.AppendChild(xmlSASteeringWheelSmoothBeta);
                rootElement.AppendChild(xmlSASteeringWheelSmoothingGroupEl);

                //XmlNode xmlSASteeringWheelUseSmoothing = m_Xdoc.CreateNode(XmlNodeType.Element, "SASteeringWheelUseSmoothing", null); xmlSASteeringWheelUseSmoothing.InnerText = wheelSmoothInfo[device].Enabled.ToString(); rootElement.AppendChild(xmlSASteeringWheelUseSmoothing);
                //XmlNode xmlSASteeringWheelSmoothMinCutoff = m_Xdoc.CreateNode(XmlNodeType.Element, "SASteeringWheelSmoothMinCutoff", null); xmlSASteeringWheelSmoothMinCutoff.InnerText = wheelSmoothInfo[device].MinCutoff.ToString(); rootElement.AppendChild(xmlSASteeringWheelSmoothMinCutoff);
                //XmlNode xmlSASteeringWheelSmoothBeta = m_Xdoc.CreateNode(XmlNodeType.Element, "SASteeringWheelSmoothBeta", null); xmlSASteeringWheelSmoothBeta.InnerText = wheelSmoothInfo[device].Beta.ToString(); rootElement.AppendChild(xmlSASteeringWheelSmoothBeta);

                XmlNode xmlTouchDisInvTriggers = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchDisInvTriggers", null);
                string tempTouchDisInv = string.Join(",", store.touchDisInvertTriggers[device]);
                xmlTouchDisInvTriggers.InnerText = tempTouchDisInv;
                rootElement.AppendChild(xmlTouchDisInvTriggers);

                XmlNode xmlGyroSensitivity = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroSensitivity", null); xmlGyroSensitivity.InnerText = store.gyroSensitivity[device].ToString(); rootElement.AppendChild(xmlGyroSensitivity);
                XmlNode xmlGyroSensVerticalScale = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroSensVerticalScale", null); xmlGyroSensVerticalScale.InnerText = store.gyroSensVerticalScale[device].ToString(); rootElement.AppendChild(xmlGyroSensVerticalScale);
                XmlNode xmlGyroInvert = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroInvert", null); xmlGyroInvert.InnerText = store.gyroInvert[device].ToString(); rootElement.AppendChild(xmlGyroInvert);
                XmlNode xmlGyroTriggerTurns = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroTriggerTurns", null); xmlGyroTriggerTurns.InnerText = store.gyroTriggerTurns[device].ToString(); rootElement.AppendChild(xmlGyroTriggerTurns);
                /*XmlNode xmlGyroSmoothWeight = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroSmoothingWeight", null); xmlGyroSmoothWeight.InnerText = Convert.ToInt32(gyroSmoothWeight[device] * 100).ToString(); rootElement.AppendChild(xmlGyroSmoothWeight);
                XmlNode xmlGyroSmoothing = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroSmoothing", null); xmlGyroSmoothing.InnerText = gyroSmoothing[device].ToString(); rootElement.AppendChild(xmlGyroSmoothing);
                */

                XmlElement xmlGyroControlsSettingsElement = m_Xdoc.CreateElement("GyroControlsSettings");
                XmlNode xmlGyroControlsTriggers = m_Xdoc.CreateNode(XmlNodeType.Element, "Triggers", null); xmlGyroControlsTriggers.InnerText = store.gyroControlsInf[device].triggers; xmlGyroControlsSettingsElement.AppendChild(xmlGyroControlsTriggers);
                XmlNode xmlGyroControlsTriggerCond = m_Xdoc.CreateNode(XmlNodeType.Element, "TriggerCond", null); xmlGyroControlsTriggerCond.InnerText = BackingStore.SaTriggerCondString(store.gyroControlsInf[device].triggerCond); xmlGyroControlsSettingsElement.AppendChild(xmlGyroControlsTriggerCond);
                XmlNode xmlGyroControlsTriggerTurns = m_Xdoc.CreateNode(XmlNodeType.Element, "TriggerTurns", null); xmlGyroControlsTriggerTurns.InnerText = store.gyroControlsInf[device].triggerTurns.ToString(); xmlGyroControlsSettingsElement.AppendChild(xmlGyroControlsTriggerTurns);
                XmlNode xmlGyroControlsToggle = m_Xdoc.CreateNode(XmlNodeType.Element, "Toggle", null); xmlGyroControlsToggle.InnerText = store.gyroControlsInf[device].triggerToggle.ToString(); xmlGyroControlsSettingsElement.AppendChild(xmlGyroControlsToggle);
                rootElement.AppendChild(xmlGyroControlsSettingsElement);

                XmlElement xmlGyroSmoothingElement = m_Xdoc.CreateElement("GyroMouseSmoothingSettings");
                XmlNode xmlGyroSmoothing = m_Xdoc.CreateNode(XmlNodeType.Element, "UseSmoothing", null); xmlGyroSmoothing.InnerText = store.gyroMouseInfo[device].enableSmoothing.ToString(); xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothing);
                XmlNode xmlGyroSmoothingMethod = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingMethod", null); xmlGyroSmoothingMethod.InnerText = store.gyroMouseInfo[device].SmoothMethodIdentifier(); xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothingMethod);
                XmlNode xmlGyroSmoothWeight = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingWeight", null); xmlGyroSmoothWeight.InnerText = Convert.ToInt32(store.gyroMouseInfo[device].smoothingWeight * 100).ToString(); xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothWeight);
                XmlNode xmlGyroSmoothMincutoff = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingMinCutoff", null); xmlGyroSmoothMincutoff.InnerText = store.gyroMouseInfo[device].minCutoff.ToString(); xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothMincutoff);
                XmlNode xmlGyroSmoothBeta = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingBeta", null); xmlGyroSmoothBeta.InnerText = store.gyroMouseInfo[device].beta.ToString(); xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothBeta);
                rootElement.AppendChild(xmlGyroSmoothingElement);

                XmlNode xmlGyroMouseHAxis = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseHAxis", null); xmlGyroMouseHAxis.InnerText = store.gyroMouseHorizontalAxis[device].ToString(); rootElement.AppendChild(xmlGyroMouseHAxis);
                XmlNode xmlGyroMouseDZ = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseDeadZone", null); xmlGyroMouseDZ.InnerText = store.gyroMouseDZ[device].ToString(); rootElement.AppendChild(xmlGyroMouseDZ);
                XmlNode xmlGyroMinThreshold = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseMinThreshold", null); xmlGyroMinThreshold.InnerText = store.gyroMouseInfo[device].minThreshold.ToString(); rootElement.AppendChild(xmlGyroMinThreshold);
                XmlNode xmlGyroMouseToggle = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseToggle", null); xmlGyroMouseToggle.InnerText = store.gyroMouseToggle[device].ToString(); rootElement.AppendChild(xmlGyroMouseToggle);

                XmlNode xmlGyroOutMode = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroOutputMode", null); xmlGyroOutMode.InnerText = store.gyroOutMode[device].ToString(); rootElement.AppendChild(xmlGyroOutMode);
                XmlNode xmlGyroMStickTriggers = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickTriggers", null); xmlGyroMStickTriggers.InnerText = store.sAMouseStickTriggers[device].ToString(); rootElement.AppendChild(xmlGyroMStickTriggers);
                XmlNode xmlGyroMStickTriggerCond = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickTriggerCond", null); xmlGyroMStickTriggerCond.InnerText = BackingStore.SaTriggerCondString(store.sAMouseStickTriggerCond[device]); rootElement.AppendChild(xmlGyroMStickTriggerCond);
                XmlNode xmlGyroMStickTriggerTurns = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickTriggerTurns", null); xmlGyroMStickTriggerTurns.InnerText = store.gyroMouseStickTriggerTurns[device].ToString(); rootElement.AppendChild(xmlGyroMStickTriggerTurns);
                XmlNode xmlGyroMStickHAxis = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickHAxis", null); xmlGyroMStickHAxis.InnerText = store.gyroMouseStickHorizontalAxis[device].ToString(); rootElement.AppendChild(xmlGyroMStickHAxis);
                XmlNode xmlGyroMStickDZ = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickDeadZone", null); xmlGyroMStickDZ.InnerText = store.gyroMStickInfo[device].deadZone.ToString(); rootElement.AppendChild(xmlGyroMStickDZ);
                XmlNode xmlGyroMStickMaxZ = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickMaxZone", null); xmlGyroMStickMaxZ.InnerText = store.gyroMStickInfo[device].maxZone.ToString(); rootElement.AppendChild(xmlGyroMStickMaxZ);
                XmlNode xmlGyroMStickOutputStick = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickOutputStick", null); xmlGyroMStickOutputStick.InnerText = store.gyroMStickInfo[device].outputStick.ToString(); rootElement.AppendChild(xmlGyroMStickOutputStick);
                XmlNode xmlGyroMStickOutputStickAxes = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickOutputStickAxes", null); xmlGyroMStickOutputStickAxes.InnerText = store.gyroMStickInfo[device].outputStickDir.ToString(); rootElement.AppendChild(xmlGyroMStickOutputStickAxes);
                XmlNode xmlGyroMStickAntiDX = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickAntiDeadX", null); xmlGyroMStickAntiDX.InnerText = store.gyroMStickInfo[device].antiDeadX.ToString(); rootElement.AppendChild(xmlGyroMStickAntiDX);
                XmlNode xmlGyroMStickAntiDY = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickAntiDeadY", null); xmlGyroMStickAntiDY.InnerText = store.gyroMStickInfo[device].antiDeadY.ToString(); rootElement.AppendChild(xmlGyroMStickAntiDY);
                XmlNode xmlGyroMStickInvert = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickInvert", null); xmlGyroMStickInvert.InnerText = store.gyroMStickInfo[device].inverted.ToString(); rootElement.AppendChild(xmlGyroMStickInvert);
                XmlNode xmlGyroMStickToggle = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickToggle", null); xmlGyroMStickToggle.InnerText = store.gyroMouseStickToggle[device].ToString(); rootElement.AppendChild(xmlGyroMStickToggle);
                XmlNode xmlGyroMStickMaxOutput = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickMaxOutput", null); xmlGyroMStickMaxOutput.InnerText = store.gyroMStickInfo[device].maxOutput.ToString(); rootElement.AppendChild(xmlGyroMStickMaxOutput);
                XmlNode xmlGyroMStickMaxOutputEnabled = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickMaxOutputEnabled", null); xmlGyroMStickMaxOutputEnabled.InnerText = store.gyroMStickInfo[device].maxOutputEnabled.ToString(); rootElement.AppendChild(xmlGyroMStickMaxOutputEnabled);
                XmlNode xmlGyroMStickVerticalScale = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickVerticalScale", null); xmlGyroMStickVerticalScale.InnerText = store.gyroMStickInfo[device].vertScale.ToString(); rootElement.AppendChild(xmlGyroMStickVerticalScale);


                /*XmlNode xmlGyroMStickSmoothing = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickSmoothing", null); xmlGyroMStickSmoothing.InnerText = gyroMStickInfo[device].useSmoothing.ToString(); rootElement.AppendChild(xmlGyroMStickSmoothing);
                XmlNode xmlGyroMStickSmoothWeight = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickSmoothingWeight", null); xmlGyroMStickSmoothWeight.InnerText = Convert.ToInt32(gyroMStickInfo[device].smoothWeight * 100).ToString(); rootElement.AppendChild(xmlGyroMStickSmoothWeight);
                */
                XmlElement xmlGyroMStickSmoothingElement = m_Xdoc.CreateElement("GyroMouseStickSmoothingSettings");
                XmlNode xmlGyroMStickSmoothing = m_Xdoc.CreateNode(XmlNodeType.Element, "UseSmoothing", null); xmlGyroMStickSmoothing.InnerText = store.gyroMStickInfo[device].useSmoothing.ToString(); xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothing);
                XmlNode xmlGyroMStickSmoothingMethod = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingMethod", null); xmlGyroMStickSmoothingMethod.InnerText = store.gyroMStickInfo[device].SmoothMethodIdentifier(); xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothingMethod);
                XmlNode xmlGyroMStickSmoothWeight = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingWeight", null); xmlGyroMStickSmoothWeight.InnerText = Convert.ToInt32(store.gyroMStickInfo[device].smoothWeight * 100).ToString(); xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothWeight);
                XmlNode xmlGyroMStickSmoothMincutoff = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingMinCutoff", null); xmlGyroMStickSmoothMincutoff.InnerText = store.gyroMStickInfo[device].minCutoff.ToString(); xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothMincutoff);
                XmlNode xmlGyroMStickSmoothBeta = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingBeta", null); xmlGyroMStickSmoothBeta.InnerText = store.gyroMStickInfo[device].beta.ToString(); xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothBeta);
                rootElement.AppendChild(xmlGyroMStickSmoothingElement);

                XmlElement xmlGyroSwipeSettingsElement = m_Xdoc.CreateElement("GyroSwipeSettings");
                XmlNode xmlGyroSwipeDeadzoneX = m_Xdoc.CreateNode(XmlNodeType.Element, "DeadZoneX", null); xmlGyroSwipeDeadzoneX.InnerText = store.gyroSwipeInfo[device].deadzoneX.ToString(); xmlGyroSwipeSettingsElement.AppendChild(xmlGyroSwipeDeadzoneX);
                XmlNode xmlGyroSwipeDeadzoneY = m_Xdoc.CreateNode(XmlNodeType.Element, "DeadZoneY", null); xmlGyroSwipeDeadzoneY.InnerText = store.gyroSwipeInfo[device].deadzoneY.ToString(); xmlGyroSwipeSettingsElement.AppendChild(xmlGyroSwipeDeadzoneY);
                XmlNode xmlGyroSwipeTriggers = m_Xdoc.CreateNode(XmlNodeType.Element, "Triggers", null); xmlGyroSwipeTriggers.InnerText = store.gyroSwipeInfo[device].triggers; xmlGyroSwipeSettingsElement.AppendChild(xmlGyroSwipeTriggers);
                XmlNode xmlGyroSwipeTriggerCond = m_Xdoc.CreateNode(XmlNodeType.Element, "TriggerCond", null); xmlGyroSwipeTriggerCond.InnerText = BackingStore.SaTriggerCondString(store.gyroSwipeInfo[device].triggerCond); xmlGyroSwipeSettingsElement.AppendChild(xmlGyroSwipeTriggerCond);
                XmlNode xmlGyroSwipeTriggerTurns = m_Xdoc.CreateNode(XmlNodeType.Element, "TriggerTurns", null); xmlGyroSwipeTriggerTurns.InnerText = store.gyroSwipeInfo[device].triggerTurns.ToString(); xmlGyroSwipeSettingsElement.AppendChild(xmlGyroSwipeTriggerTurns);
                XmlNode xmlGyroSwipeXAxis = m_Xdoc.CreateNode(XmlNodeType.Element, "XAxis", null); xmlGyroSwipeXAxis.InnerText = store.gyroSwipeInfo[device].xAxis.ToString(); xmlGyroSwipeSettingsElement.AppendChild(xmlGyroSwipeXAxis);
                XmlNode xmlGyroSwipeDelayTime = m_Xdoc.CreateNode(XmlNodeType.Element, "DelayTime", null); xmlGyroSwipeDelayTime.InnerText = store.gyroSwipeInfo[device].delayTime.ToString(); xmlGyroSwipeSettingsElement.AppendChild(xmlGyroSwipeDelayTime);
                rootElement.AppendChild(xmlGyroSwipeSettingsElement);

                // Ensure profileActions list exists. Do NOT auto-add "Disconnect Controller"
                // here — respect the stored list so users can intentionally disable
                // the Bluetooth-disconnect special action for an existing profile.
                if (store.profileActions[device] == null)
                {
                    store.profileActions[device] = new List<string>();
                }
                XmlNode xmlProfileActions = m_Xdoc.CreateNode(XmlNodeType.Element, "ProfileActions", null);
                xmlProfileActions.InnerText = string.Join("/", store.profileActions[device]); rootElement.AppendChild(xmlProfileActions);
                XmlNode xmlBTPollRate = m_Xdoc.CreateNode(XmlNodeType.Element, "BTPollRate", null); xmlBTPollRate.InnerText = store.btPollRate[device].ToString(); rootElement.AppendChild(xmlBTPollRate);

                XmlNode xmlLsOutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "LSOutputCurveMode", null); xmlLsOutputCurveMode.InnerText = store.stickOutputCurveString(store.getLsOutCurveMode(device)); rootElement.AppendChild(xmlLsOutputCurveMode);
                XmlNode xmlLsOutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "LSOutputCurveCustom", null); xmlLsOutputCurveCustom.InnerText = store.lsOutBezierCurveObj[device].ToString(); rootElement.AppendChild(xmlLsOutputCurveCustom);

                XmlNode xmlRsOutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "RSOutputCurveMode", null); xmlRsOutputCurveMode.InnerText = store.stickOutputCurveString(store.getRsOutCurveMode(device)); rootElement.AppendChild(xmlRsOutputCurveMode);
                XmlNode xmlRsOutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "RSOutputCurveCustom", null); xmlRsOutputCurveCustom.InnerText = store.rsOutBezierCurveObj[device].ToString(); rootElement.AppendChild(xmlRsOutputCurveCustom);

                XmlNode xmlLsSquareStickMode = m_Xdoc.CreateNode(XmlNodeType.Element, "LSSquareStick", null); xmlLsSquareStickMode.InnerText = store.squStickInfo[device].lsMode.ToString(); rootElement.AppendChild(xmlLsSquareStickMode);
                XmlNode xmlRsSquareStickMode = m_Xdoc.CreateNode(XmlNodeType.Element, "RSSquareStick", null); xmlRsSquareStickMode.InnerText = store.squStickInfo[device].rsMode.ToString(); rootElement.AppendChild(xmlRsSquareStickMode);

                XmlNode xmlSquareStickRoundness = m_Xdoc.CreateNode(XmlNodeType.Element, "SquareStickRoundness", null); xmlSquareStickRoundness.InnerText = store.squStickInfo[device].lsRoundness.ToString(); rootElement.AppendChild(xmlSquareStickRoundness);
                XmlNode xmlSquareRStickRoundness = m_Xdoc.CreateNode(XmlNodeType.Element, "SquareRStickRoundness", null); xmlSquareRStickRoundness.InnerText = store.squStickInfo[device].rsRoundness.ToString(); rootElement.AppendChild(xmlSquareRStickRoundness);

                XmlNode xmlLsAntiSnapbackEnabled = m_Xdoc.CreateNode(XmlNodeType.Element, "LSAntiSnapback", null); xmlLsAntiSnapbackEnabled.InnerText = store.lsAntiSnapbackInfo[device].enabled.ToString(); rootElement.AppendChild(xmlLsAntiSnapbackEnabled);
                XmlNode xmlRsAntiSnapbackEnabled = m_Xdoc.CreateNode(XmlNodeType.Element, "RSAntiSnapback", null); xmlRsAntiSnapbackEnabled.InnerText = store.rsAntiSnapbackInfo[device].enabled.ToString(); rootElement.AppendChild(xmlRsAntiSnapbackEnabled);

                XmlNode xmlLsAntiSnapbackDelta = m_Xdoc.CreateNode(XmlNodeType.Element, "LSAntiSnapbackDelta", null); xmlLsAntiSnapbackDelta.InnerText = store.lsAntiSnapbackInfo[device].delta.ToString(); rootElement.AppendChild(xmlLsAntiSnapbackDelta);
                XmlNode xmlRsAntiSnapbackDelta = m_Xdoc.CreateNode(XmlNodeType.Element, "RSAntiSnapbackDelta", null); xmlRsAntiSnapbackDelta.InnerText = store.rsAntiSnapbackInfo[device].delta.ToString(); rootElement.AppendChild(xmlRsAntiSnapbackDelta);

                XmlNode xmlLsAntiSnapbackTimeout = m_Xdoc.CreateNode(XmlNodeType.Element, "LSAntiSnapbackTimeout", null); xmlLsAntiSnapbackTimeout.InnerText = store.lsAntiSnapbackInfo[device].timeout.ToString(); rootElement.AppendChild(xmlLsAntiSnapbackTimeout);
                XmlNode xmlRsAntiSnapbackTimeout = m_Xdoc.CreateNode(XmlNodeType.Element, "RSAntiSnapbackTimeout", null); xmlRsAntiSnapbackTimeout.InnerText = store.rsAntiSnapbackInfo[device].timeout.ToString(); rootElement.AppendChild(xmlRsAntiSnapbackTimeout);

                XmlNode xmlLsOutputMode = m_Xdoc.CreateNode(XmlNodeType.Element, "LSOutputMode", null); xmlLsOutputMode.InnerText = store.lsOutputSettings[device].mode.ToString(); rootElement.AppendChild(xmlLsOutputMode);
                XmlNode xmlRsOutputMode = m_Xdoc.CreateNode(XmlNodeType.Element, "RSOutputMode", null); xmlRsOutputMode.InnerText = store.rsOutputSettings[device].mode.ToString(); rootElement.AppendChild(xmlRsOutputMode);

                XmlElement xmlLsOutputSettingsElement = m_Xdoc.CreateElement("LSOutputSettings");
                XmlElement xmlLsFlickStickGroupElement = m_Xdoc.CreateElement("FlickStickSettings"); xmlLsOutputSettingsElement.AppendChild(xmlLsFlickStickGroupElement);
                XmlNode xmlLsFlickStickRWC = m_Xdoc.CreateNode(XmlNodeType.Element, "RealWorldCalibration", null); xmlLsFlickStickRWC.InnerText = store.lsOutputSettings[device].outputSettings.flickSettings.realWorldCalibration.ToString(); xmlLsFlickStickGroupElement.AppendChild(xmlLsFlickStickRWC);
                XmlNode xmlLsFlickStickThreshold = m_Xdoc.CreateNode(XmlNodeType.Element, "FlickThreshold", null); xmlLsFlickStickThreshold.InnerText = store.lsOutputSettings[device].outputSettings.flickSettings.flickThreshold.ToString(); xmlLsFlickStickGroupElement.AppendChild(xmlLsFlickStickThreshold);
                XmlNode xmlLsFlickStickTime = m_Xdoc.CreateNode(XmlNodeType.Element, "FlickTime", null); xmlLsFlickStickTime.InnerText = store.lsOutputSettings[device].outputSettings.flickSettings.flickTime.ToString(); xmlLsFlickStickGroupElement.AppendChild(xmlLsFlickStickTime);
                XmlNode xmlLsMinAngleThreshold = m_Xdoc.CreateNode(XmlNodeType.Element, "MinAngleThreshold", null); xmlLsMinAngleThreshold.InnerText = store.lsOutputSettings[device].outputSettings.flickSettings.minAngleThreshold.ToString(); xmlLsFlickStickGroupElement.AppendChild(xmlLsMinAngleThreshold);
                rootElement.AppendChild(xmlLsOutputSettingsElement);

                XmlElement xmlRsOutputSettingsElement = m_Xdoc.CreateElement("RSOutputSettings");
                XmlElement xmlRsFlickStickGroupElement = m_Xdoc.CreateElement("FlickStickSettings"); xmlRsOutputSettingsElement.AppendChild(xmlRsFlickStickGroupElement);
                XmlNode xmlRsFlickStickRWC = m_Xdoc.CreateNode(XmlNodeType.Element, "RealWorldCalibration", null); xmlRsFlickStickRWC.InnerText = store.rsOutputSettings[device].outputSettings.flickSettings.realWorldCalibration.ToString(); xmlRsFlickStickGroupElement.AppendChild(xmlRsFlickStickRWC);
                XmlNode xmlRsFlickStickThreshold = m_Xdoc.CreateNode(XmlNodeType.Element, "FlickThreshold", null); xmlRsFlickStickThreshold.InnerText = store.rsOutputSettings[device].outputSettings.flickSettings.flickThreshold.ToString(); xmlRsFlickStickGroupElement.AppendChild(xmlRsFlickStickThreshold);
                XmlNode xmlRsFlickStickTime = m_Xdoc.CreateNode(XmlNodeType.Element, "FlickTime", null); xmlRsFlickStickTime.InnerText = store.rsOutputSettings[device].outputSettings.flickSettings.flickTime.ToString(); xmlRsFlickStickGroupElement.AppendChild(xmlRsFlickStickTime);
                XmlNode xmlRsMinAngleThreshold = m_Xdoc.CreateNode(XmlNodeType.Element, "MinAngleThreshold", null); xmlRsMinAngleThreshold.InnerText = store.rsOutputSettings[device].outputSettings.flickSettings.minAngleThreshold.ToString(); xmlRsFlickStickGroupElement.AppendChild(xmlRsMinAngleThreshold);
                rootElement.AppendChild(xmlRsOutputSettingsElement);

                XmlNode xmlL2OutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "L2OutputCurveMode", null); xmlL2OutputCurveMode.InnerText = axisOutputCurveString(store.getL2OutCurveMode(device)); rootElement.AppendChild(xmlL2OutputCurveMode);
                XmlNode xmlL2OutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "L2OutputCurveCustom", null); xmlL2OutputCurveCustom.InnerText = store.l2OutBezierCurveObj[device].ToString(); rootElement.AppendChild(xmlL2OutputCurveCustom);

                XmlNode xmlL2TwoStageMode = m_Xdoc.CreateNode(XmlNodeType.Element, "L2TwoStageMode", null); xmlL2TwoStageMode.InnerText = store.l2OutputSettings[device].twoStageMode.ToString(); rootElement.AppendChild(xmlL2TwoStageMode);
                XmlNode xmlR2TwoStageMode = m_Xdoc.CreateNode(XmlNodeType.Element, "R2TwoStageMode", null); xmlR2TwoStageMode.InnerText = store.r2OutputSettings[device].twoStageMode.ToString(); rootElement.AppendChild(xmlR2TwoStageMode);

                XmlNode xmlL2HipFireTime = m_Xdoc.CreateNode(XmlNodeType.Element, "L2HipFireTime", null); xmlL2HipFireTime.InnerText = store.l2OutputSettings[device].hipFireMS.ToString(); rootElement.AppendChild(xmlL2HipFireTime);
                XmlNode xmlR2HipFireTime = m_Xdoc.CreateNode(XmlNodeType.Element, "R2HipFireTime", null); xmlR2HipFireTime.InnerText = store.r2OutputSettings[device].hipFireMS.ToString(); rootElement.AppendChild(xmlR2HipFireTime);

                XmlNode xmlL2TriggerEffect = m_Xdoc.CreateNode(XmlNodeType.Element, "L2TriggerEffect", null); xmlL2TriggerEffect.InnerText = store.l2OutputSettings[device].triggerEffect.ToString(); rootElement.AppendChild(xmlL2TriggerEffect);
                XmlNode xmlR2TriggerEffect = m_Xdoc.CreateNode(XmlNodeType.Element, "R2TriggerEffect", null); xmlR2TriggerEffect.InnerText = store.r2OutputSettings[device].triggerEffect.ToString(); rootElement.AppendChild(xmlR2TriggerEffect);

                XmlNode xmlR2OutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "R2OutputCurveMode", null); xmlR2OutputCurveMode.InnerText = axisOutputCurveString(store.getR2OutCurveMode(device)); rootElement.AppendChild(xmlR2OutputCurveMode);
                XmlNode xmlR2OutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "R2OutputCurveCustom", null); xmlR2OutputCurveCustom.InnerText = store.r2OutBezierCurveObj[device].ToString(); rootElement.AppendChild(xmlR2OutputCurveCustom);

                XmlNode xmlSXOutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "SXOutputCurveMode", null); xmlSXOutputCurveMode.InnerText = axisOutputCurveString(store.getSXOutCurveMode(device)); rootElement.AppendChild(xmlSXOutputCurveMode);
                XmlNode xmlSXOutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "SXOutputCurveCustom", null); xmlSXOutputCurveCustom.InnerText = store.sxOutBezierCurveObj[device].ToString(); rootElement.AppendChild(xmlSXOutputCurveCustom);

                XmlNode xmlSZOutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "SZOutputCurveMode", null); xmlSZOutputCurveMode.InnerText = axisOutputCurveString(store.getSZOutCurveMode(device)); rootElement.AppendChild(xmlSZOutputCurveMode);
                XmlNode xmlSZOutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "SZOutputCurveCustom", null); xmlSZOutputCurveCustom.InnerText = store.szOutBezierCurveObj[device].ToString(); rootElement.AppendChild(xmlSZOutputCurveCustom);

                XmlNode xmlTrackBallMode = m_Xdoc.CreateNode(XmlNodeType.Element, "TrackballMode", null); xmlTrackBallMode.InnerText = store.trackballMode[device].ToString(); rootElement.AppendChild(xmlTrackBallMode);
                XmlNode xmlTrackBallFriction = m_Xdoc.CreateNode(XmlNodeType.Element, "TrackballFriction", null); xmlTrackBallFriction.InnerText = store.trackballFriction[device].ToString(); rootElement.AppendChild(xmlTrackBallFriction);

                XmlNode xmlTouchRelMouseRotation = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchRelMouseRotation", null); xmlTouchRelMouseRotation.InnerText = Convert.ToInt32(store.touchpadRelMouse[device].rotation * 180.0 / Math.PI).ToString(); rootElement.AppendChild(xmlTouchRelMouseRotation);
                XmlNode xmlTouchRelMouseMinThreshold = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchRelMouseMinThreshold", null); xmlTouchRelMouseMinThreshold.InnerText = store.touchpadRelMouse[device].minThreshold.ToString(); rootElement.AppendChild(xmlTouchRelMouseMinThreshold);

                // Isolate as a group. More readable for this???
                //if (false)
                {
                    XmlElement xmlTouchMouseStickGroupEl = m_Xdoc.CreateElement("TouchpadMouseStick");
                    XmlElement xmlTouchMouseStickDeadZone = m_Xdoc.CreateElement("DeadZone"); xmlTouchMouseStickDeadZone.InnerText = store.touchMStickInfo[device].deadZone.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickDeadZone);
                    XmlElement xmlTouchMouseStickMaxZone = m_Xdoc.CreateElement("MaxZone"); xmlTouchMouseStickMaxZone.InnerText = store.touchMStickInfo[device].maxZone.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickMaxZone);
                    XmlElement xmlTouchMouseStickOutStick = m_Xdoc.CreateElement("OutputStick"); xmlTouchMouseStickOutStick.InnerText = store.touchMStickInfo[device].outputStick.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickOutStick);
                    XmlElement xmlTouchMouseStickOutStickAxes = m_Xdoc.CreateElement("OutputStickAxes"); xmlTouchMouseStickOutStickAxes.InnerText = store.touchMStickInfo[device].outputStickDir.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickOutStickAxes);
                    XmlElement xmlTouchMouseStickAntiDeadX = m_Xdoc.CreateElement("AntiDeadX"); xmlTouchMouseStickAntiDeadX.InnerText = store.touchMStickInfo[device].antiDeadX.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickAntiDeadX);
                    XmlElement xmlTouchMouseStickAntiDeadY = m_Xdoc.CreateElement("AntiDeadY"); xmlTouchMouseStickAntiDeadY.InnerText = store.touchMStickInfo[device].antiDeadY.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickAntiDeadY);
                    XmlElement xmlTouchMouseStickInvert = m_Xdoc.CreateElement("Invert"); xmlTouchMouseStickInvert.InnerText = store.touchMStickInfo[device].inverted.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickInvert);
                    XmlElement xmlTouchMouseStickMaxOutput = m_Xdoc.CreateElement("MaxOutput"); xmlTouchMouseStickMaxOutput.InnerText = store.touchMStickInfo[device].maxOutput.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickMaxOutput);
                    XmlElement xmlTouchMouseStickMaxOutputEnabled = m_Xdoc.CreateElement("MaxOutputEnabled"); xmlTouchMouseStickMaxOutputEnabled.InnerText = store.touchMStickInfo[device].maxOutputEnabled.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickMaxOutputEnabled);
                    XmlElement xmlTouchMouseStickVerticalScale = m_Xdoc.CreateElement("VerticalScale"); xmlTouchMouseStickVerticalScale.InnerText = store.touchMStickInfo[device].vertScale.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickVerticalScale);
                    XmlElement xmlTouchMouseStickOutputCurve = m_Xdoc.CreateElement("OutputCurve"); xmlTouchMouseStickOutputCurve.InnerText = store.touchMStickInfo[device].outputCurve.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickOutputCurve);
                    XmlElement xmlTouchMouseStickRotation = m_Xdoc.CreateElement("Rotation"); xmlTouchMouseStickRotation.InnerText = Convert.ToInt32(store.touchMStickInfo[device].rotationRad * 180.0 / Math.PI).ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickRotation);

                    XmlElement xmlTouchMouseStickSmoothSettingsEl = m_Xdoc.CreateElement("SmoothingSettings");
                    XmlElement xmlTouchMouseStickSmoothMethod = m_Xdoc.CreateElement("SmoothingMethod"); xmlTouchMouseStickSmoothMethod.InnerText = store.touchMStickInfo[device].smoothingMethod.ToString(); xmlTouchMouseStickSmoothSettingsEl.AppendChild(xmlTouchMouseStickSmoothMethod);
                    XmlElement xmlTouchMouseStickSmoothMinCutoff = m_Xdoc.CreateElement("SmoothingMinCutoff"); xmlTouchMouseStickSmoothMinCutoff.InnerText = store.touchMStickInfo[device].minCutoff.ToString(); xmlTouchMouseStickSmoothSettingsEl.AppendChild(xmlTouchMouseStickSmoothMinCutoff);
                    XmlElement xmlTouchMouseStickSmoothBeta = m_Xdoc.CreateElement("SmoothingBeta"); xmlTouchMouseStickSmoothBeta.InnerText = store.touchMStickInfo[device].beta.ToString(); xmlTouchMouseStickSmoothSettingsEl.AppendChild(xmlTouchMouseStickSmoothBeta);
                    xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickSmoothSettingsEl);
                    rootElement.AppendChild(xmlTouchMouseStickGroupEl);
                }

                XmlNode xmlTouchButtonMode = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchpadButtonMode", null); xmlTouchButtonMode.InnerText = store.touchpadButtonMode[device].ToString(); rootElement.AppendChild(xmlTouchButtonMode);
                // Start of DualSense specific settings
                // xmlDSRumbleGroupElement.AppendChild();
                XmlElement xmlDualSenseControllerSettingsElement = m_Xdoc.CreateElement("DualSenseControllerSettings");
                XmlElement xmlDSRumbleGroupElement = m_Xdoc.CreateElement("RumbleSettings"); xmlDualSenseControllerSettingsElement.AppendChild(xmlDSRumbleGroupElement);
                XmlNode xmlDSREmulationModeElement = m_Xdoc.CreateNode(XmlNodeType.Element, "EmulationMode", null); xmlDSREmulationModeElement.InnerText = store.dualSenseRumbleEmulationMode[device].ToString(); xmlDSRumbleGroupElement.AppendChild(xmlDSREmulationModeElement);
                XmlNode xmlDSREnableGenericRumbleRescaleElement = m_Xdoc.CreateNode(XmlNodeType.Element, "EnableGenericRumbleRescale", null); xmlDSREnableGenericRumbleRescaleElement.InnerText = store.useGenericRumbleRescaleForDualSenses[device].ToString(); xmlDSRumbleGroupElement.AppendChild(xmlDSREnableGenericRumbleRescaleElement);
                XmlNode xmlDSRHapticPowerLevelElement = m_Xdoc.CreateNode(XmlNodeType.Element, "HapticPowerLevel", null); xmlDSRHapticPowerLevelElement.InnerText = store.dualSenseHapticPowerLevel[device].ToString(); xmlDSRumbleGroupElement.AppendChild(xmlDSRHapticPowerLevelElement);
                rootElement.AppendChild(xmlDualSenseControllerSettingsElement);
                //
                // End of DualSense specific settings

                XmlNode xmlOutContDevice = m_Xdoc.CreateNode(XmlNodeType.Element, "OutputContDevice", null); xmlOutContDevice.InnerText = OutContDeviceString(store.outputDevType[device]); rootElement.AppendChild(xmlOutContDevice);

                XmlNode NodeControl = m_Xdoc.CreateNode(XmlNodeType.Element, "Control", null);
                XmlNode Key = m_Xdoc.CreateNode(XmlNodeType.Element, "Key", null);
                XmlNode Macro = m_Xdoc.CreateNode(XmlNodeType.Element, "Macro", null);
                XmlNode KeyType = m_Xdoc.CreateNode(XmlNodeType.Element, "KeyType", null);
                XmlNode Button = m_Xdoc.CreateNode(XmlNodeType.Element, "Button", null);
                XmlNode Extras = m_Xdoc.CreateNode(XmlNodeType.Element, "Extras", null);

                XmlNode NodeShiftControl = m_Xdoc.CreateNode(XmlNodeType.Element, "ShiftControl", null);

                XmlNode ShiftKey = m_Xdoc.CreateNode(XmlNodeType.Element, "Key", null);
                XmlNode ShiftMacro = m_Xdoc.CreateNode(XmlNodeType.Element, "Macro", null);
                XmlNode ShiftKeyType = m_Xdoc.CreateNode(XmlNodeType.Element, "KeyType", null);
                XmlNode ShiftButton = m_Xdoc.CreateNode(XmlNodeType.Element, "Button", null);
                XmlNode ShiftExtras = m_Xdoc.CreateNode(XmlNodeType.Element, "Extras", null);

                foreach (DS4ControlSettings dcs in store.ds4settings[device])
                {
                    if (dcs.actionType != DS4ControlSettings.ActionType.Default)
                    {
                        XmlNode buttonNode;
                        string keyType = string.Empty;

                        if (dcs.actionType == DS4ControlSettings.ActionType.Button &&
                            dcs.action.actionBtn == X360Controls.Unbound)
                        {
                            keyType += DS4KeyType.Unbound;
                        }

                        if (dcs.keyType.HasFlag(DS4KeyType.HoldMacro))
                            keyType += DS4KeyType.HoldMacro;
                        else if (dcs.keyType.HasFlag(DS4KeyType.Macro))
                            keyType += DS4KeyType.Macro;

                        if (dcs.keyType.HasFlag(DS4KeyType.Toggle))
                            keyType += DS4KeyType.Toggle;
                        if (dcs.keyType.HasFlag(DS4KeyType.ScanCode))
                            keyType += DS4KeyType.ScanCode;

                        if (keyType != string.Empty)
                        {
                            buttonNode = m_Xdoc.CreateNode(XmlNodeType.Element, dcs.control.ToString(), null);
                            buttonNode.InnerText = keyType;
                            KeyType.AppendChild(buttonNode);
                        }

                        buttonNode = m_Xdoc.CreateNode(XmlNodeType.Element, dcs.control.ToString(), null);
                        if (dcs.actionType == DS4ControlSettings.ActionType.Macro)
                        {
                            int[] ii = dcs.action.actionMacro;
                            buttonNode.InnerText = string.Join("/", ii);
                            Macro.AppendChild(buttonNode);
                        }
                        else if (dcs.actionType == DS4ControlSettings.ActionType.Key)
                        {
                            buttonNode.InnerText = dcs.action.actionKey.ToString();
                            Key.AppendChild(buttonNode);
                        }
                        else if (dcs.actionType == DS4ControlSettings.ActionType.Button)
                        {
                            buttonNode.InnerText = X360ControlTables.getX360ControlString((X360Controls)dcs.action.actionBtn);
                            Button.AppendChild(buttonNode);
                        }
                    }

                    bool hasvalue = false;
                    if (!string.IsNullOrEmpty(dcs.extras))
                    {
                        foreach (string s in dcs.extras.Split(','))
                        {
                            if (s != "0")
                            {
                                hasvalue = true;
                                break;
                            }
                        }
                    }

                    if (hasvalue)
                    {
                        XmlNode extraNode = m_Xdoc.CreateNode(XmlNodeType.Element, dcs.control.ToString(), null);
                        extraNode.InnerText = dcs.extras;
                        Extras.AppendChild(extraNode);
                    }

                    if (dcs.shiftActionType != DS4ControlSettings.ActionType.Default && dcs.shiftTrigger > 0)
                    {
                        XmlElement buttonNode;
                        string keyType = string.Empty;

                        if (dcs.shiftActionType == DS4ControlSettings.ActionType.Button &&
                            dcs.shiftAction.actionBtn == X360Controls.Unbound)
                        {
                            keyType += DS4KeyType.Unbound;
                        }

                        if (dcs.shiftKeyType.HasFlag(DS4KeyType.HoldMacro))
                            keyType += DS4KeyType.HoldMacro;
                        if (dcs.shiftKeyType.HasFlag(DS4KeyType.Macro))
                            keyType += DS4KeyType.Macro;
                        if (dcs.shiftKeyType.HasFlag(DS4KeyType.Toggle))
                            keyType += DS4KeyType.Toggle;
                        if (dcs.shiftKeyType.HasFlag(DS4KeyType.ScanCode))
                            keyType += DS4KeyType.ScanCode;

                        if (keyType != string.Empty)
                        {
                            buttonNode = m_Xdoc.CreateElement(dcs.control.ToString());
                            buttonNode.InnerText = keyType;
                            ShiftKeyType.AppendChild(buttonNode);
                        }

                        buttonNode = m_Xdoc.CreateElement(dcs.control.ToString());
                        buttonNode.SetAttribute("Trigger", dcs.shiftTrigger.ToString());
                        if (dcs.shiftActionType == DS4ControlSettings.ActionType.Macro)
                        {
                            int[] ii = dcs.shiftAction.actionMacro;
                            buttonNode.InnerText = string.Join("/", ii);
                            ShiftMacro.AppendChild(buttonNode);
                        }
                        else if (dcs.shiftActionType == DS4ControlSettings.ActionType.Key)
                        {
                            buttonNode.InnerText = dcs.shiftAction.actionKey.ToString();
                            ShiftKey.AppendChild(buttonNode);
                        }
                        else if (dcs.shiftActionType == DS4ControlSettings.ActionType.Button)
                        {
                            buttonNode.InnerText = dcs.shiftAction.actionBtn.ToString();
                            ShiftButton.AppendChild(buttonNode);
                        }
                    }

                    hasvalue = false;
                    if (!string.IsNullOrEmpty(dcs.shiftExtras))
                    {
                        foreach (string s in dcs.shiftExtras.Split(','))
                        {
                            if (s != "0")
                            {
                                hasvalue = true;
                                break;
                            }
                        }
                    }

                    if (hasvalue)
                    {
                        XmlNode extraNode = m_Xdoc.CreateNode(XmlNodeType.Element, dcs.control.ToString(), null);
                        extraNode.InnerText = dcs.shiftExtras;
                        ShiftExtras.AppendChild(extraNode);
                    }
                }

                rootElement.AppendChild(NodeControl);
                if (Button.HasChildNodes)
                    NodeControl.AppendChild(Button);
                if (Macro.HasChildNodes)
                    NodeControl.AppendChild(Macro);
                if (Key.HasChildNodes)
                    NodeControl.AppendChild(Key);
                if (Extras.HasChildNodes)
                    NodeControl.AppendChild(Extras);
                if (KeyType.HasChildNodes)
                    NodeControl.AppendChild(KeyType);

                if (NodeControl.HasChildNodes)
                    rootElement.AppendChild(NodeControl);

                rootElement.AppendChild(NodeShiftControl);
                if (ShiftButton.HasChildNodes)
                    NodeShiftControl.AppendChild(ShiftButton);
                if (ShiftMacro.HasChildNodes)
                    NodeShiftControl.AppendChild(ShiftMacro);
                if (ShiftKey.HasChildNodes)
                    NodeShiftControl.AppendChild(ShiftKey);
                if (ShiftKeyType.HasChildNodes)
                    NodeShiftControl.AppendChild(ShiftKeyType);
                if (ShiftExtras.HasChildNodes)
                    NodeShiftControl.AppendChild(ShiftExtras);

                m_Xdoc.AppendChild(rootElement);
                m_Xdoc.Save(path);
            }
            catch { Saved = false; }
            return Saved;
        }

        public void SaveLegacyExtraSettings(XmlNode rootElement, int device)
        {
            AppendLegacyElement(rootElement, "DebouncingMs", store.debouncingMs[device].ToString());
            AppendLegacyElement(rootElement, "InverseRumbleMotors", store.inverseRumbleMotors[device].ToString());
            AppendLegacyElement(rootElement, "UseDs3PitchRollSim", store.useDs3PitchRollSim.ToString());
            AppendLegacyElement(rootElement, "LeftStickDriftXAxis", store.leftStickDriftXAxis[device].ToString());
            AppendLegacyElement(rootElement, "LeftStickDriftYAxis", store.leftStickDriftYAxis[device].ToString());
            AppendLegacyElement(rootElement, "RightStickDriftXAxis", store.rightStickDriftXAxis[device].ToString());
            AppendLegacyElement(rootElement, "RightStickDriftYAxis", store.rightStickDriftYAxis[device].ToString());
            AppendLegacyElement(rootElement, "DS4OutputTriggerMode", store.outputDS4TriggerMode[device].ToString());
            AppendLegacyElement(rootElement, "GyroMouseJitterCompensation", store.gyroMouseInfo[device].jitterCompensation.ToString());
            AppendLegacyElement(rootElement, "GyroMouseStickJitterCompensation", store.gyroMStickInfo[device].jitterCompensation.ToString());
        }

        private void AppendLegacyElement(XmlNode parent, string name, string value)
        {
            XmlNode node = m_Xdoc.CreateNode(XmlNodeType.Element, name, null);
            node.InnerText = value;
            parent.AppendChild(node);
        }
    }
}
