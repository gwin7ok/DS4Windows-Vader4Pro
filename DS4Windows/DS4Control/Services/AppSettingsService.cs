using System;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    public class AppSettingsService : IAppSettingsService
    {
        private readonly IProfileXmlStore _xmlStore;
        private readonly IPathService _pathService;

        // Phase5-Step13-7譬ｹ譛ｬ菫ｮ豁｣: StartMinimized遲峨・Global(m_Config/BackingStore)縺ｸ縺ｮ
        // 豁｣隕上す繝縺ｨ縺励ゞI螻､(MainWindow.xaml.cs遲・縺九ｉGlobal逶ｴ蜿ら・縺励◆蝣ｴ蜷医→螳悟・縺ｫ蜷御ｸ縺ｮ螳滉ｽ薙ｒ
        // 蜿ら・縺吶ｋ繧医≧縺ｫ縺吶ｋ縲ょｾ捺擂縺ｯ縺薙％縺ｫ迢ｬ遶九＠縺殫rivate繝輔ぅ繝ｼ繝ｫ繝峨ｒ謖√■縲；lobal蛛ｴ縺ｨ髱樣｣蜍輔→縺・≧
        // 縲悟ｭ､遶九＠縺滄㍾隍・憾諷九阪ヰ繧ｰ縺後≠縺｣縺・Step13謚募・譎ゅ↓逋ｺ隕・縲・        // CheckWhen縺ｮ縺ｿ縲∝ｯｾ蠢懊☆繧季lobal蛛ｴ縺ｮ讎ょｿｵ縺梧里縺ｫ謦､蟒・＆繧後※縺・ｋ(繧ｳ繝｡繝ｳ繝・"Legacy CheckWhen
        // removed. Use explicit CheckEveryValue/CheckEveryUnit instead." 蜿ら・)縺溘ａ縲・        // 蜊倅ｽ薙ユ繧ｹ繝医・縺ｿ縺ｧ蜿ら・縺輔ｌ繧矩撼騾｣蜍輔・繝ｭ繝代ユ繧｣縺ｨ縺励※迴ｾ迥ｶ邯ｭ謖√☆繧九・        private int _checkWhen;

        public event EventHandler<string> SettingChanged;

        public AppSettingsService(IProfileXmlStore xmlStore = null, IPathService pathService = null)
        {
            _xmlStore = xmlStore ?? DS4WinWPF.AppHost.GetService<IProfileXmlStore>() ?? new ProfileXmlStore();
            _pathService = pathService ?? DS4WinWPF.AppHost.GetService<IPathService>() ?? new PathService();
        }

        public bool Save()
        {
            bool success = _xmlStore.SaveAppSettingsXml();
            if (success)
            {
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace("[DI] AppSettingsService.Save succeeded");
            }
            else
            {
                AppLogger.LogToGui("Failed to save application settings", true);
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace("[DI] AppSettingsService.Save failed");
            }
            return success;
        }

        public bool Load()
        {
            bool success = _xmlStore.LoadAppSettingsXml();
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] AppSettingsService.Load: success={success}");
            return success;
        }

        private void NotifyChanged(string settingName)
        {
            SettingChanged?.Invoke(this, settingName);
        }

        public bool StartMinimized
        {
            get => Global.StartMinimized;
            set
            {
                if (Global.StartMinimized != value)
                {
                    Global.StartMinimized = value;
                    NotifyChanged(nameof(StartMinimized));
                }
            }
        }

        public bool MinimizeToTaskbar
        {
            get => Global.MinToTaskbar;
            set
            {
                if (Global.MinToTaskbar != value)
                {
                    Global.MinToTaskbar = value;
                    NotifyChanged(nameof(MinimizeToTaskbar));
                }
            }
        }

        public bool CloseMinimizes
        {
            get => Global.CloseMini;
            set
            {
                if (Global.CloseMini != value)
                {
                    Global.CloseMini = value;
                    NotifyChanged(nameof(CloseMinimizes));
                }
            }
        }

        public int CheckWhen
        {
            get => _checkWhen;
            set
            {
                if (_checkWhen != value)
                {
                    _checkWhen = value;
                    NotifyChanged(nameof(CheckWhen));
                }
            }
        }

        public bool UseUdpServer
        {
            get => Global.isUsingUDPServer();
            set
            {
                if (Global.isUsingUDPServer() != value)
                {
                    Global.setUsingUDPServer(value);
                    NotifyChanged(nameof(UseUdpServer));
                }
            }
        }

        public int UdpServerPort
        {
            get => Global.getUDPServerPortNum();
            set
            {
                if (Global.getUDPServerPortNum() != value)
                {
                    Global.setUDPServerPort(value);
                    NotifyChanged(nameof(UdpServerPort));
                }
            }
        }

        public string UdpServerListenAddress
        {
            get => Global.getUDPServerListenAddress();
            set
            {
                if (Global.getUDPServerListenAddress() != value)
                {
                    Global.setUDPServerListenAddress(value);
                    NotifyChanged(nameof(UdpServerListenAddress));
                }
            }
        }

        public bool UseExclusiveMode
        {
            get => Global.UseExclusiveMode;
            set
            {
                if (Global.UseExclusiveMode != value)
                {
                    Global.UseExclusiveMode = value;
                    NotifyChanged(nameof(UseExclusiveMode));
                }
            }
        }

        public bool AutoProfileRevertDefaultProfile
        {
            get => Global.AutoProfileRevertDefaultProfile;
            set
            {
                if (Global.AutoProfileRevertDefaultProfile != value)
                {
                    Global.AutoProfileRevertDefaultProfile = value;
                    NotifyChanged(nameof(AutoProfileRevertDefaultProfile));
                }
            }
        }

        // ---- Phase5-Step13-7: 繧ｦ繧｣繝ｳ繝峨え菴咲ｽｮ繝ｻ繧ｵ繧､繧ｺ縲√さ繝ｳ繝医Ο繝ｼ繝ｩ繝ｼ荳隕ｧ蛻怜ｹ・・豌ｸ邯壼喧 ----
        // 縺・★繧後ｂ m_Config(BackingStore) 縺ｸ縺ｮ阮・＞蜈ｬ髢九い繧ｯ繧ｻ繧ｵ(Global.X)縺ｸ縺ｮ豁｣隕上す繝縲・        public int FormWidth
        {
            get => Global.FormWidth;
            set { if (Global.FormWidth != value) { Global.FormWidth = value; NotifyChanged(nameof(FormWidth)); } }
        }

        public int FormHeight
        {
            get => Global.FormHeight;
            set { if (Global.FormHeight != value) { Global.FormHeight = value; NotifyChanged(nameof(FormHeight)); } }
        }

        public int FormLocationX
        {
            get => Global.FormLocationX;
            set { if (Global.FormLocationX != value) { Global.FormLocationX = value; NotifyChanged(nameof(FormLocationX)); } }
        }

        public int FormLocationY
        {
            get => Global.FormLocationY;
            set { if (Global.FormLocationY != value) { Global.FormLocationY = value; NotifyChanged(nameof(FormLocationY)); } }
        }

        public int ControllerIndexColWidth
        {
            get => Global.ControllerIndexColWidth;
            set { if (Global.ControllerIndexColWidth != value) { Global.ControllerIndexColWidth = value; NotifyChanged(nameof(ControllerIndexColWidth)); } }
        }

        public int ControllerIdColWidth
        {
            get => Global.ControllerIdColWidth;
            set { if (Global.ControllerIdColWidth != value) { Global.ControllerIdColWidth = value; NotifyChanged(nameof(ControllerIdColWidth)); } }
        }

        public int ControllerStatusColWidth
        {
            get => Global.ControllerStatusColWidth;
            set { if (Global.ControllerStatusColWidth != value) { Global.ControllerStatusColWidth = value; NotifyChanged(nameof(ControllerStatusColWidth)); } }
        }

        public int ControllerExclusiveColWidth
        {
            get => Global.ControllerExclusiveColWidth;
            set { if (Global.ControllerExclusiveColWidth != value) { Global.ControllerExclusiveColWidth = value; NotifyChanged(nameof(ControllerExclusiveColWidth)); } }
        }

        public int ControllerBatteryColWidth
        {
            get => Global.ControllerBatteryColWidth;
            set { if (Global.ControllerBatteryColWidth != value) { Global.ControllerBatteryColWidth = value; NotifyChanged(nameof(ControllerBatteryColWidth)); } }
        }

        public int ControllerSelectProfileColWidth
        {
            get => Global.ControllerSelectProfileColWidth;
            set { if (Global.ControllerSelectProfileColWidth != value) { Global.ControllerSelectProfileColWidth = value; NotifyChanged(nameof(ControllerSelectProfileColWidth)); } }
        }

        public int ControllerEditColWidth
        {
            get => Global.ControllerEditColWidth;
            set { if (Global.ControllerEditColWidth != value) { Global.ControllerEditColWidth = value; NotifyChanged(nameof(ControllerEditColWidth)); } }
        }

        public int ControllerLinkedProfileColWidth
        {
            get => Global.ControllerLinkedProfileColWidth;
            set { if (Global.ControllerLinkedProfileColWidth != value) { Global.ControllerLinkedProfileColWidth = value; NotifyChanged(nameof(ControllerLinkedProfileColWidth)); } }
        }

        public int ControllerLinkProfIdColWidth
        {
            get => Global.ControllerLinkProfIdColWidth;
            set { if (Global.ControllerLinkProfIdColWidth != value) { Global.ControllerLinkProfIdColWidth = value; NotifyChanged(nameof(ControllerLinkProfIdColWidth)); } }
        }

        public int ControllerCustomColorColWidth
        {
            get => Global.ControllerCustomColorColWidth;
            set { if (Global.ControllerCustomColorColWidth != value) { Global.ControllerCustomColorColWidth = value; NotifyChanged(nameof(ControllerCustomColorColWidth)); } }
        }

        // ---- Phase5-Step13-7: 騾夂衍繝ｬ繝吶Ν繝ｻ繧ｹ繝ｯ繧､繝玲桃菴懆ｨｭ螳・----
        public int Notifications
        {
            get => Global.Notifications;
            set { if (Global.Notifications != value) { Global.Notifications = value; NotifyChanged(nameof(Notifications)); } }
        }

        public bool SwipeProfiles
        {
            get => Global.SwipeProfiles;
            set { if (Global.SwipeProfiles != value) { Global.SwipeProfiles = value; NotifyChanged(nameof(SwipeProfiles)); } }
        }

        // ---- Phase5-Step14蜑阪け繝ｪ繝ｼ繝ｳ繧｢繝・・: MainWindow.xaml.cs谿句ｭ賂lobal蜿ら・縺ｮ隗｣豸・----
        // 縺・★繧後ｂ Global(m_Config/BackingStore) 縺ｸ縺ｮ阮・＞蜈ｬ髢九い繧ｯ繧ｻ繧ｵ縺ｸ縺ｮ豁｣隕上す繝縲・        // Notifications 遲峨→蜷御ｸ繝代ち繝ｼ繝ｳ・育峡遶九ヵ繧｣繝ｼ繝ｫ繝峨ｒ謖√◆縺ｪ縺・ｼ峨・        public string LogMinLevel
        {
            get => Global.LogMinLevel;
            set { if (Global.LogMinLevel != value) { Global.LogMinLevel = value; NotifyChanged(nameof(LogMinLevel)); } }
        }

        public bool CheckUpdateStartupEnabled
        {
            get => Global.CheckUpdateStartupEnabled;
            set { if (Global.CheckUpdateStartupEnabled != value) { Global.CheckUpdateStartupEnabled = value; NotifyChanged(nameof(CheckUpdateStartupEnabled)); } }
        }

        public int CheckEveryValue
        {
            get => Global.CheckEveryValue;
            set { if (Global.CheckEveryValue != value) { Global.CheckEveryValue = value; NotifyChanged(nameof(CheckEveryValue)); } }
        }

        public int CheckEveryUnit
        {
            get => Global.CheckEveryUnit;
            set { if (Global.CheckEveryUnit != value) { Global.CheckEveryUnit = value; NotifyChanged(nameof(CheckEveryUnit)); } }
        }

        public DateTime LastChecked
        {
            get => Global.LastChecked;
            set { if (Global.LastChecked != value) { Global.LastChecked = value; NotifyChanged(nameof(LastChecked)); } }
        }

        // Global.LastVersionCheckedNum 縺ｯ隱ｭ縺ｿ蜿悶ｊ蟆ら畑・・astVersionChecked莉｣蜈･譎ゅ↓閾ｪ蜍慕ｮ怜・・峨・縺溘ａ縲・        // 譛ｬ繧ｷ繝繧りｪｭ縺ｿ蜿悶ｊ蟆ら畑縺ｨ縺励※蜈ｬ髢九☆繧九・        public ulong LastVersionCheckedNum => Global.LastVersionCheckedNum;

        public bool FirstRun
        {
            get => Global.firstRun;
            set { if (Global.firstRun != value) { Global.firstRun = value; NotifyChanged(nameof(FirstRun)); } }
        }

        public bool RunHotPlug
        {
            get => Global.runHotPlug;
            set { if (Global.runHotPlug != value) { Global.runHotPlug = value; NotifyChanged(nameof(RunHotPlug)); } }
        }

        // ---- Phase5-Step14: プロファイル編集画面・SpecialAction カラム幅の永続化 ----
        public int ProfileEditorLeftWidth
        {
            get => Global.ProfileEditorLeftWidth;
            set => Global.ProfileEditorLeftWidth = value;
        }

        public int ProfileEditorRightWidth
        {
            get => Global.ProfileEditorRightWidth;
            set => Global.ProfileEditorRightWidth = value;
        }

        public int SpecialActionNameColWidth
        {
            get => Global.SpecialActionNameColWidth;
            set => Global.SpecialActionNameColWidth = value;
        }

        public int SpecialActionTriggerColWidth
        {
            get => Global.SpecialActionTriggerColWidth;
            set => Global.SpecialActionTriggerColWidth = value;
        }

        public int SpecialActionDetailColWidth
        {
            get => Global.SpecialActionDetailColWidth;
            set => Global.SpecialActionDetailColWidth = value;
        }
    }
}
