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
    /// Phase7-Step10c W3（Q1＝A）: アプリ設定（<c>Profiles.xml</c>）の保存・読込。
    /// 旧: <c>BackingStore</c> の <c>Load</c>／<c>Save</c>／<c>LoadFakeExeName</c>（移動のみ。ロジック・ログ・要素の順序は変えない。
    /// 旧形式の読込の互換を含む。<c>BackingStore</c> の公開状態を <c>store.</c> で読み書きする）。
    /// <c>PostProcessLoad</c>（読込後の値の丸め）は状態の整形のため <c>BackingStore</c> に残し、<c>store.PostProcessLoad()</c> として呼ぶ。
    /// パス・環境の窓口は、<c>Global</c> ではなく <see cref="DS4Windows.DI.IPathService"/>／<see cref="DS4Windows.DI.IEnvironmentService"/> から得る。
    /// </summary>
    public class AppSettingsXmlSerializer
    {
        private readonly BackingStore store;
        // 偽の exe 名を保存するファイルの名前（旧: CustomExeConfigFileName。Global の同名の定数は、この値を指す別名）
        public const string CustomExeConfigFileName = "custom_exe_name.txt";

        private readonly DS4Windows.DI.IPathService _pathService;
        private readonly DS4Windows.DI.IEnvironmentService _environment;
        private readonly XmlDocument m_Xdoc = new XmlDocument(); // 旧 BackingStore.m_Xdoc と同じ使い方（排他は呼び出し側のロック）

        public AppSettingsXmlSerializer(BackingStore store, DS4Windows.DI.IPathService pathService, DS4Windows.DI.IEnvironmentService environment)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public bool Load()
        {
            bool Loaded = true;
            bool missingSetting = false;

            try
            {
                if (File.Exists(store.m_Profile))
                {
                    // First try DTO-based deserialization. Support both <AppSettingsDTO> and
                    // <Profile> roots by attempting XmlSerializer with a Profile root
                    // then falling back to the default DTO root.
                    string fileXml = File.ReadAllText(store.m_Profile);
                    bool dtoLoaded = false;
                    try
                    {
                        AppSettingsDTO dto = null;
                        try
                        {
                            var xr = new XmlRootAttribute("Profile");
                            var ser = new XmlSerializer(typeof(AppSettingsDTO), xr);
                            using (var sr = new StringReader(fileXml))
                            {
                                dto = ser.Deserialize(sr) as AppSettingsDTO;
                            }
                        }
                        catch { dto = null; }

                        if (dto == null)
                        {
                            try
                            {
                                var ser2 = new XmlSerializer(typeof(AppSettingsDTO));
                                using (var sr2 = new StringReader(fileXml))
                                {
                                    dto = ser2.Deserialize(sr2) as AppSettingsDTO;
                                }
                            }
                            catch { dto = null; }
                        }

                        if (dto != null)
                        {
                            // Map DTO into this backing store
                            dto.MapTo(store);
                            dtoLoaded = true;
                        }
                    }
                    catch { /* fall through to legacy loader below */ }

                    if (dtoLoaded)
                    {
                        // Phase7-Step1 W3（不具合 B の是正）: 旧形式の経路と同じ後処理を行う。
                        // これまでは「Post processing same as legacy path」とコメントされていたが、実際には何も実行されず、
                        // 偽の exe 名・スキップしたバージョンの数値・ポートの範囲・Distance プロファイルの判定が欠落していた
                        // （105575a7 〜。移行前から続く不具合で、回帰ではない）。
                        store.PostProcessLoad();
                        LoadFakeExeName();
                        Loaded = true;
                        return Loaded;
                    }

                    // Fall back to legacy DOM-based loader
                    XmlNode Item;

                    m_Xdoc.Load(store.m_Profile);

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/useExclusiveMode"); Boolean.TryParse(Item.InnerText, out store.useExclusiveMode); } // Ex Mode
                    catch { missingSetting = true; } // Ex Mode
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/startMinimized"); Boolean.TryParse(Item.InnerText, out store.startMinimized); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/minimizeToTaskbar"); Boolean.TryParse(Item.InnerText, out store.minToTaskbar); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/formWidth"); Int32.TryParse(Item.InnerText, out store.formWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/formHeight"); Int32.TryParse(Item.InnerText, out store.formHeight); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/formLocationX"); Int32.TryParse(Item.InnerText, out store.formLocationX); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/formLocationY"); Int32.TryParse(Item.InnerText, out store.formLocationY); }
                    catch { missingSetting = true; }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/profileEditorLeftWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out store.profileEditorLeftWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/profileEditorRightWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out store.profileEditorRightWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/specialActionNameColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out store.specialActionNameColWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/specialActionTriggerColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out store.specialActionTriggerColWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/specialActionDetailColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out store.specialActionDetailColWidth); }
                    catch { missingSetting = true; }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerIndexColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out store.controllerIndexColWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerIdColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out store.controllerIdColWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerStatusColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out store.controllerStatusColWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerExclusiveColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out store.controllerExclusiveColWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerBatteryColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out store.controllerBatteryColWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerSelectProfileColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out store.controllerSelectProfileColWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerEditColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out store.controllerEditColWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerLinkedProfileColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out store.controllerLinkedProfileColWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerLinkProfIdColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out store.controllerLinkProfIdColWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerCustomColorColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out store.controllerCustomColorColWidth); }
                    catch { missingSetting = true; }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/logMaxArchiveFiles"); Int32.TryParse(Item?.InnerText ?? string.Empty, out store.logMaxArchiveFiles); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/logMinLevel"); store.logMinLevel = Item?.InnerText ?? BackingStore.DEFAULT_LOG_MIN_LEVEL; }
                    catch { missingSetting = true; }

                    for (int i = 0; i < ControllerLimits.MaxControllerCount; i++)
                    {
                        string contTag = $"/Profile/Controller{i + 1}";
                        try
                        {
                            Item = m_Xdoc.SelectSingleNode(contTag); store.profilePath[i] = Item?.InnerText ?? string.Empty;
                            if (store.profilePath[i].ToLower().Contains("distance"))
                            {
                                store.distanceProfiles[i] = true;
                            }

                            store.olderProfilePath[i] = store.profilePath[i];
                        }
                        catch { store.profilePath[i] = store.olderProfilePath[i] = string.Empty; store.distanceProfiles[i] = false; }
                    }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/LastChecked"); DateTime.TryParse(Item.InnerText, out store.lastChecked); }
                    catch { missingSetting = true; }
                    // Read new explicit startup-check fields when available. If not present,
                    // fall back to legacy <CheckWhen> value and map it into daily/hourly values.
                    try
                    {
                        XmlNode everyValNode = m_Xdoc.SelectSingleNode("/Profile/CheckEveryValue");
                        XmlNode everyUnitNode = m_Xdoc.SelectSingleNode("/Profile/CheckEveryUnit");
                        if (everyValNode != null)
                        {
                            Int32.TryParse(everyValNode.InnerText, out store.checkEveryValue);
                            Int32.TryParse(everyUnitNode?.InnerText ?? "1", out store.checkEveryUnit);
                        }
                        else
                        {
                            // Legacy fallback: map CheckWhen (hours or days) into checkEveryValue/unit
                            Item = m_Xdoc.SelectSingleNode("/Profile/CheckWhen");
                            if (Item != null && Int32.TryParse(Item.InnerText, out int legacyCheckWhen))
                            {
                                if (legacyCheckWhen > 23)
                                {
                                    // stored in hours; convert to days
                                    store.checkEveryValue = legacyCheckWhen / 24;
                                    store.checkEveryUnit = 1; // days
                                }
                                else
                                {
                                    store.checkEveryValue = legacyCheckWhen;
                                    store.checkEveryUnit = 0; // hours
                                }
                            }
                        }
                    }
                    catch { missingSetting = true; }
                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/Profile/LastVersionChecked");
                        string tempVer = Item?.InnerText ?? string.Empty;
                        if (!string.IsNullOrEmpty(tempVer))
                        {
                            store.lastVersionCheckedNum = VersionUtil.CompileVersionNumberFromString(tempVer);
                            if (store.lastVersionCheckedNum > 0) store.lastVersionChecked = tempVer;
                        }
                    }
                    catch { missingSetting = true; }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/Profile/Notifications");
                        if (!int.TryParse(Item.InnerText, out store.notifications))
                            store.notifications = (Boolean.Parse(Item.InnerText) ? 2 : 0);
                    }
                    catch { missingSetting = true; }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/DisconnectBTAtStop"); Boolean.TryParse(Item.InnerText, out store.disconnectBTAtStop); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/SwipeProfiles"); Boolean.TryParse(Item.InnerText, out store.swipeProfiles); }
                    catch { missingSetting = true; }
                    //try { Item = m_Xdoc.SelectSingleNode("/Profile/UseDS4ForMapping"); Boolean.TryParse(Item.InnerText, out ds4Mapping); }
                    //catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/QuickCharge"); Boolean.TryParse(Item.InnerText, out store.quickCharge); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/CloseMinimizes"); Boolean.TryParse(Item.InnerText, out store.closeMini); }
                    catch { missingSetting = true; }
                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/Profile/UseLang"); store.useLang = Item.InnerText;
                        AppLogger.LogDebug($"ScpUtil: loaded UseLang='{store.useLang}' from profile (appdatapath={_pathService.AppDataPath})");
                    }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/DownloadLang"); Boolean.TryParse(Item.InnerText, out store.downloadLang); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/FlashWhenLate"); Boolean.TryParse(Item.InnerText, out store.flashWhenLate); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/FlashWhenLateAt"); int.TryParse(Item.InnerText, out store.flashWhenLateAt); }
                    catch { missingSetting = true; }

                    Item = m_Xdoc.SelectSingleNode("/Profile/AppIcon");
                    bool hasIconChoice = Item != null;
                    if (hasIconChoice)
                    {
                        hasIconChoice = Enum.TryParse(Item.InnerText ?? "", out store.useIconChoice);
                    }

                    if (!hasIconChoice)
                    {
                        missingSetting = true;

                        // Backwards compatible code from when only two icons existed
                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/WhiteIcon");
                            if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                            {
                                store.useIconChoice = temp ? TrayIconChoice.White : TrayIconChoice.Default;
                            }
                        }
                        catch { missingSetting = true; }
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/Profile/AppTheme");
                        string temp = Item.InnerText;
                        Enum.TryParse(temp, out AppThemeChoice choice);
                        store.useCurrentTheme = choice;
                    }
                    catch { missingSetting = true; store.useCurrentTheme = AppThemeChoice.Default; }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/UseOSCServer"); Boolean.TryParse(Item.InnerText, out store.useOSCServ); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/OSCServerPort"); int temp; int.TryParse(Item.InnerText, out temp); store.oscServPort = Math.Min(Math.Max(temp, 1024), 65535); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/InterpretingOscMonitoring"); Boolean.TryParse(Item.InnerText, out store.interpretingOscMonitoring); }
                    catch { missingSetting = true; }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/UseOSCSender"); Boolean.TryParse(Item.InnerText, out store.useOSCSend); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/OSCSenderPort"); int temp; int.TryParse(Item.InnerText, out temp); store.oscSendPort = Math.Min(Math.Max(temp, 1024), 65535); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/OSCSenderAddress"); store.oscSendAddress = Item.InnerText; }
                    catch { missingSetting = true; }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/UseUDPServer"); Boolean.TryParse(Item.InnerText, out store.useUDPServ); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/UDPServerPort"); int temp; int.TryParse(Item.InnerText, out temp); store.udpServPort = Math.Min(Math.Max(temp, 1024), 65535); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/UDPServerListenAddress"); store.udpServListenAddress = Item.InnerText; }
                    catch { missingSetting = true; }

                    bool udpServerSmoothingGroup = false;
                    XmlNode xmlUdpServerSmoothElement =
                        m_Xdoc.SelectSingleNode("/Profile/UDPServerSmoothingOptions");
                    udpServerSmoothingGroup = xmlUdpServerSmoothElement != null;
                    if (udpServerSmoothingGroup)
                    {
                        try
                        {
                            Item = xmlUdpServerSmoothElement.SelectSingleNode("UseSmoothing");
                            bool.TryParse(Item.InnerText, out bool temp);
                            store.useUdpSmoothing = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlUdpServerSmoothElement.SelectSingleNode("UdpSmoothMinCutoff");
                            double.TryParse(Item.InnerText, out double temp);
                            temp = Math.Min(Math.Max(temp, 0.00001), 100.0);
                            store.udpSmoothingMincutoff = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlUdpServerSmoothElement.SelectSingleNode("UdpSmoothBeta");
                            double.TryParse(Item.InnerText, out double temp);
                            temp = Math.Min(Math.Max(temp, 0.0), 1.0);
                            store.udpSmoothingBeta = temp;
                        }
                        catch { missingSetting = true; }
                    }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/UseCustomSteamFolder"); Boolean.TryParse(Item.InnerText, out store.useCustomSteamFolder); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/CustomSteamFolder"); store.customSteamFolder = Item.InnerText; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/AutoProfileRevertDefaultProfile"); Boolean.TryParse(Item.InnerText, out store.autoProfileRevertDefaultProfile); }
                    catch { missingSetting = true; }

                    XmlNode xmlDeviceOptions = m_Xdoc.SelectSingleNode("/Profile/DeviceOptions");
                    if (xmlDeviceOptions != null)
                    {
                        XmlNode xmlDS4Support = xmlDeviceOptions.SelectSingleNode("DS4SupportSettings");
                        if (xmlDS4Support != null)
                        {
                            try
                            {
                                XmlNode item = xmlDS4Support.SelectSingleNode("Enabled");
                                if (bool.TryParse(item?.InnerText ?? "", out bool temp))
                                {
                                    store.deviceOptions.DS4DeviceOpts.Enabled = temp;
                                }
                            }
                            catch { }
                        }

                        XmlNode xmlDualSenseSupport = xmlDeviceOptions.SelectSingleNode("DualSenseSupportSettings");
                        if (xmlDualSenseSupport != null)
                        {
                            try
                            {
                                XmlNode item = xmlDualSenseSupport.SelectSingleNode("Enabled");
                                if (bool.TryParse(item?.InnerText ?? "", out bool temp))
                                {
                                    store.deviceOptions.DualSenseOpts.Enabled = temp;
                                }
                            }
                            catch { }
                        }

                        XmlNode xmlSwitchProSupport = xmlDeviceOptions.SelectSingleNode("SwitchProSupportSettings");
                        if (xmlSwitchProSupport != null)
                        {
                            try
                            {
                                XmlNode item = xmlSwitchProSupport.SelectSingleNode("Enabled");
                                if (bool.TryParse(item?.InnerText ?? "", out bool temp))
                                {
                                    store.deviceOptions.SwitchProDeviceOpts.Enabled = temp;
                                }
                            }
                            catch { }
                        }

                        XmlNode xmlJoyConSupport = xmlDeviceOptions.SelectSingleNode("JoyConSupportSettings");
                        if (xmlJoyConSupport != null)
                        {
                            try
                            {
                                XmlNode item = xmlJoyConSupport.SelectSingleNode("Enabled");
                                if (bool.TryParse(item?.InnerText ?? "", out bool temp))
                                {
                                    store.deviceOptions.JoyConDeviceOpts.Enabled = temp;
                                }
                            }
                            catch { }

                            try
                            {
                                XmlNode item = xmlJoyConSupport.SelectSingleNode("LinkMode");
                                if (Enum.TryParse(item?.InnerText ?? "", out JoyConDeviceOptions.LinkMode temp))
                                {
                                    store.deviceOptions.JoyConDeviceOpts.LinkedMode = temp;
                                }
                            }
                            catch { }

                            try
                            {
                                XmlNode item = xmlJoyConSupport.SelectSingleNode("JoinedGyroProvider");
                                if (Enum.TryParse(item?.InnerText ?? "", out JoyConDeviceOptions.JoinedGyroProvider temp))
                                {
                                    store.deviceOptions.JoyConDeviceOpts.JoinGyroProv = temp;
                                }
                            }
                            catch { }
                        }
                    }

                    for (int i = 0; i < ControllerLimits.MaxControllerCount; i++)
                    {
                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/CustomLed" + (i + 1));
                            string[] ss = Item.InnerText.Split(':');
                            bool.TryParse(ss[0], out store.lightbarSettingInfo[i].ds4winSettings.useCustomLed);
                            DS4Color.TryParse(ss[1], ref store.lightbarSettingInfo[i].ds4winSettings.m_CustomLed);
                        }
                        catch { store.lightbarSettingInfo[i].ds4winSettings.useCustomLed = false; store.lightbarSettingInfo[i].ds4winSettings.m_CustomLed = new DS4Color(Color.Blue); missingSetting = true; }
                    }
                }
            }
            catch { }

            if (missingSetting)
            {
                // Do not auto-save changes discovered during legacy load. Auto-saving
                // at startup may overwrite files and cause conflicts (file locks).
                AppLogger.LogToGui("Profiles.xml is missing some settings; not auto-saving to avoid overwriting existing file.", false);
            }

            if (Loaded)
            {
                LoadFakeExeName();
            }

            return Loaded;
        }

        private void LoadFakeExeName()
        {
            string custom_exe_name_path = Path.Combine(_pathService.ProgramDirectory, CustomExeConfigFileName);
            bool fakeExeFileExists = File.Exists(custom_exe_name_path);
            if (fakeExeFileExists)
            {
                string fake_exe_name = File.ReadAllText(custom_exe_name_path).Trim();
                bool valid = !(fake_exe_name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0);
                if (valid)
                {
                    store.fakeExeFileName = fake_exe_name;
                }
            }
        }

        public bool Save()
        {
            // Use DTO-based serialization as canonical save format but keep compatibility
            // with older readers by writing a <Profile> root (same as legacy files).
            bool saved = true;
            string output_path = store.m_Profile;
            string testStr = string.Empty;

            XmlSerializer serializer = new XmlSerializer(typeof(AppSettingsDTO), new XmlRootAttribute("Profile"));
            using (Utf8StringWriter strWriter = new Utf8StringWriter())
            {
                using XmlWriter xmlWriter = XmlWriter.Create(strWriter,
                    new XmlWriterSettings()
                    {
                        Encoding = Encoding.UTF8,
                        Indent = true,
                    });

                // Write header comments
                xmlWriter.WriteComment(String.Format(" Profile Configuration Data. {0} ", DateTime.Now));
                xmlWriter.WriteWhitespace("\r\n");
                xmlWriter.WriteWhitespace("\r\n");

                // Serialize DTO with root <Profile>
                AppSettingsDTO dto = new AppSettingsDTO();
                dto.MapFrom(store);
                serializer.Serialize(xmlWriter, dto,
                    new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty }));
                xmlWriter.Flush();
                xmlWriter.Close();

                testStr = strWriter.ToString();
            }

            try
            {
                // The DTO now contains app_version/config_version as XmlAttributes, so write directly.
                using (StreamWriter sw = new StreamWriter(output_path, false))
                {
                    sw.Write(testStr);
                }
            }
            catch (UnauthorizedAccessException)
            {
                saved = false;
            }
            catch (IOException ex)
            {
                AppLogger.LogToGui($"Could not save Profiles.xml due to IO error: {ex.Message}", false);
                saved = false;
            }

            // Write fake exe config file if needed (same behavior as legacy save)
            bool adminNeeded = _environment.AdminNeeded();
            if (saved &&
                (!adminNeeded || (adminNeeded && _environment.IsAdministrator())))
            {
                string custom_exe_name_path = Path.Combine(_pathService.ProgramDirectory, CustomExeConfigFileName);
                bool fakeExeFileExists = File.Exists(custom_exe_name_path);
                if (!string.IsNullOrEmpty(store.fakeExeFileName) || fakeExeFileExists)
                {
                    File.WriteAllText(custom_exe_name_path, store.fakeExeFileName);
                }
            }

            return saved;
        }
    }
}
