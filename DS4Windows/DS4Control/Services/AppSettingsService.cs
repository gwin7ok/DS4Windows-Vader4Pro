using System;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    public class AppSettingsService : IAppSettingsService
    {
        private readonly IProfileXmlStore _xmlStore;
        private readonly IPathService _pathService;

        // Phase5-Step13-7根本修正: StartMinimized等はGlobal(m_Config/BackingStore)への
        // 正規シムとし、UI層(MainWindow.xaml.cs等)からGlobal直参照した場合と完全に同一の実体を
        // 参照するようにする。従来はここに独立したprivateフィールドを持ち、Global側と非連動という
        // 「孤立した重複状態」バグがあった(Step13投入時に発覚)。
        // CheckWhenのみ、対応するGlobal側の概念が既に撤廃されているため、
        // 単体テストのみで参照される非連動プロパティとして現状維持する。
        private int _checkWhen = 24;

        public event EventHandler<string> SettingChanged;

        public AppSettingsService(IProfileXmlStore xmlStore = null, IPathService pathService = null)
        {
            _xmlStore = xmlStore;
            _pathService = pathService;
        }

        public bool Save()
        {
            try
            {
                return _xmlStore != null ? _xmlStore.SaveAppSettingsXml(_pathService?.GetProfilesFilePath()) : Global.Save();
            }
            catch
            {
                return false;
            }
        }

        public bool Load()
        {
            try
            {
                return _xmlStore != null ? _xmlStore.LoadAppSettingsXml(_pathService?.GetProfilesFilePath()) : Global.Load();
            }
            catch
            {
                return false;
            }
        }

        public void NotifyChanged(string propertyName = null)
        {
            SettingChanged?.Invoke(this, propertyName);
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
            get => Global.CloseMinimizes;
            set
            {
                if (Global.CloseMinimizes != value)
                {
                    Global.CloseMinimizes = value;
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
            get => Global.IsUsingUDPServer();
            set
            {
                if (Global.IsUsingUDPServer() != value)
                {
                    Global.SetUseUDPServer(value);
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
                    Global.setUDPServerPortNum(value);
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

        // ---- Phase5-Step14前クリーンアップ: MainWindow.xaml.cs残存Global参照の解消 ----
        public string LogMinLevel
        {
            get => Global.LogMinLevel.ToString();
            set
            {
                if (int.TryParse(value, out var lvl))
                {
                    if (Global.LogMinLevel != lvl)
                    {
                        Global.LogMinLevel = lvl;
                        NotifyChanged(nameof(LogMinLevel));
                    }
                }
            }
        }

        public bool CheckUpdateStartupEnabled
        {
            get => Global.CheckUpdateStartup;
            set
            {
                if (Global.CheckUpdateStartup != value)
                {
                    Global.CheckUpdateStartup = value;
                    NotifyChanged(nameof(CheckUpdateStartupEnabled));
                }
            }
        }

        public int CheckEveryValue
        {
            get => Global.CheckEvery;
            set
            {
                if (Global.CheckEvery != value)
                {
                    Global.CheckEvery = value;
                    NotifyChanged(nameof(CheckEveryValue));
                }
            }
        }

        public int CheckEveryUnit
        {
            get => Global.CheckEveryUnit;
            set
            {
                if (Global.CheckEveryUnit != value)
                {
                    Global.CheckEveryUnit = value;
                    NotifyChanged(nameof(CheckEveryUnit));
                }
            }
        }

        public DateTime LastChecked
        {
            get => Global.LastChecked;
            set
            {
                if (Global.LastChecked != value)
                {
                    Global.LastChecked = value;
                    NotifyChanged(nameof(LastChecked));
                }
            }
        }

        public ulong LastVersionCheckedNum
        {
            get => Global.LastVersionCheckedNumber;
        }

        public bool FirstRun
        {
            get => Global.FirstRun;
            set
            {
                if (Global.FirstRun != value)
                {
                    Global.FirstRun = value;
                    NotifyChanged(nameof(FirstRun));
                }
            }
        }

        public bool RunHotPlug
        {
            get => Global.RunHotPlug;
            set
            {
                if (Global.RunHotPlug != value)
                {
                    Global.RunHotPlug = value;
                    NotifyChanged(nameof(RunHotPlug));
                }
            }
        }

        // ---- Phase5-Step13-7: ウィンドウ位置・サイズ、コントローラー一覧列幅の永続化 ----
        public int FormWidth
        {
            get => Global.FormWidth;
            set
            {
                if (Global.FormWidth != value)
                {
                    Global.FormWidth = value;
                    NotifyChanged(nameof(FormWidth));
                }
            }
        }

        public int FormHeight
        {
            get => Global.FormHeight;
            set
            {
                if (Global.FormHeight != value)
                {
                    Global.FormHeight = value;
                    NotifyChanged(nameof(FormHeight));
                }
            }
        }

        public int FormLocationX
        {
            get => Global.FormLocationX;
            set
            {
                if (Global.FormLocationX != value)
                {
                    Global.FormLocationX = value;
                    NotifyChanged(nameof(FormLocationX));
                }
            }
        }

        public int FormLocationY
        {
            get => Global.FormLocationY;
            set
            {
                if (Global.FormLocationY != value)
                {
                    Global.FormLocationY = value;
                    NotifyChanged(nameof(FormLocationY));
                }
            }
        }

        public int ControllerIndexColWidth
        {
            get => Global.ControllerIndexColWidth;
            set
            {
                if (Global.ControllerIndexColWidth != value)
                {
                    Global.ControllerIndexColWidth = value;
                    NotifyChanged(nameof(ControllerIndexColWidth));
                }
            }
        }

        public int ControllerIdColWidth
        {
            get => Global.ControllerIdColWidth;
            set
            {
                if (Global.ControllerIdColWidth != value)
                {
                    Global.ControllerIdColWidth = value;
                    NotifyChanged(nameof(ControllerIdColWidth));
                }
            }
        }

        public int ControllerStatusColWidth
        {
            get => Global.ControllerStatusColWidth;
            set
            {
                if (Global.ControllerStatusColWidth != value)
                {
                    Global.ControllerStatusColWidth = value;
                    NotifyChanged(nameof(ControllerStatusColWidth));
                }
            }
        }

        public int ControllerExclusiveColWidth
        {
            get => Global.ControllerExclusiveColWidth;
            set
            {
                if (Global.ControllerExclusiveColWidth != value)
                {
                    Global.ControllerExclusiveColWidth = value;
                    NotifyChanged(nameof(ControllerExclusiveColWidth));
                }
            }
        }

        public int ControllerBatteryColWidth
        {
            get => Global.ControllerBatteryColWidth;
            set
            {
                if (Global.ControllerBatteryColWidth != value)
                {
                    Global.ControllerBatteryColWidth = value;
                    NotifyChanged(nameof(ControllerBatteryColWidth));
                }
            }
        }

        public int ControllerSelectProfileColWidth
        {
            get => Global.ControllerSelectProfileColWidth;
            set
            {
                if (Global.ControllerSelectProfileColWidth != value)
                {
                    Global.ControllerSelectProfileColWidth = value;
                    NotifyChanged(nameof(ControllerSelectProfileColWidth));
                }
            }
        }

        public int ControllerEditColWidth
        {
            get => Global.ControllerEditColWidth;
            set
            {
                if (Global.ControllerEditColWidth != value)
                {
                    Global.ControllerEditColWidth = value;
                    NotifyChanged(nameof(ControllerEditColWidth));
                }
            }
        }

        public int ControllerLinkedProfileColWidth
        {
            get => Global.ControllerLinkedProfileColWidth;
            set
            {
                if (Global.ControllerLinkedProfileColWidth != value)
                {
                    Global.ControllerLinkedProfileColWidth = value;
                    NotifyChanged(nameof(ControllerLinkedProfileColWidth));
                }
            }
        }

        public int ControllerLinkProfIdColWidth
        {
            get => Global.ControllerLinkProfIdColWidth;
            set
            {
                if (Global.ControllerLinkProfIdColWidth != value)
                {
                    Global.ControllerLinkProfIdColWidth = value;
                    NotifyChanged(nameof(ControllerLinkProfIdColWidth));
                }
            }
        }

        public int ControllerCustomColorColWidth
        {
            get => Global.ControllerCustomColorColWidth;
            set
            {
                if (Global.ControllerCustomColorColWidth != value)
                {
                    Global.ControllerCustomColorColWidth = value;
                    NotifyChanged(nameof(ControllerCustomColorColWidth));
                }
            }
        }

        // ---- Phase5-Step13-7: 通知レベル・スワイプ操作設定 ----
        public int Notifications
        {
            get => Global.Notifications;
            set
            {
                if (Global.Notifications != value)
                {
                    Global.Notifications = value;
                    NotifyChanged(nameof(Notifications));
                }
            }
        }

        public bool SwipeProfiles
        {
            get => Global.SwipeProfiles;
            set
            {
                if (Global.SwipeProfiles != value)
                {
                    Global.SwipeProfiles = value;
                    NotifyChanged(nameof(SwipeProfiles));
                }
            }
        }

        // ---- Phase5-Step14: プロファイル編集画面・SpecialAction カラム幅の永続化 ----
        public int ProfileEditorLeftWidth
        {
            get => Global.ProfileEditorLeftWidth;
            set
            {
                if (Global.ProfileEditorLeftWidth != value)
                {
                    Global.ProfileEditorLeftWidth = value;
                    NotifyChanged(nameof(ProfileEditorLeftWidth));
                }
            }
        }

        public int ProfileEditorRightWidth
        {
            get => Global.ProfileEditorRightWidth;
            set
            {
                if (Global.ProfileEditorRightWidth != value)
                {
                    Global.ProfileEditorRightWidth = value;
                    NotifyChanged(nameof(ProfileEditorRightWidth));
                }
            }
        }

        public int SpecialActionNameColWidth
        {
            get => Global.SpecialActionNameColWidth;
            set
            {
                if (Global.SpecialActionNameColWidth != value)
                {
                    Global.SpecialActionNameColWidth = value;
                    NotifyChanged(nameof(SpecialActionNameColWidth));
                }
            }
        }

        public int SpecialActionTriggerColWidth
        {
            get => Global.SpecialActionTriggerColWidth;
            set
            {
                if (Global.SpecialActionTriggerColWidth != value)
                {
                    Global.SpecialActionTriggerColWidth = value;
                    NotifyChanged(nameof(SpecialActionTriggerColWidth));
                }
            }
        }

        public int SpecialActionDetailColWidth
        {
            get => Global.SpecialActionDetailColWidth;
            set
            {
                if (Global.SpecialActionDetailColWidth != value)
                {
                    Global.SpecialActionDetailColWidth = value;
                    NotifyChanged(nameof(SpecialActionDetailColWidth));
                }
            }
        }
    }
}
