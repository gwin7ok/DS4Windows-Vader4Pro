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
        // CheckWhenのみ、対応するGlobal側の概念が既に撤廃されている(コメント "Legacy CheckWhen
        // removed. Use explicit CheckEveryValue/CheckEveryUnit instead." 参照)ため、
        // 単体テストのみで参照される非連動プロパティとして現状維持する。
        private int _checkWhen;

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

        // ---- Phase5-Step13-7: ウィンドウ位置・サイズ、コントローラー一覧列幅の永続化 ----
        // いずれも m_Config(BackingStore) への薄い公開アクセサ(Global.X)への正規シム。
        public int FormWidth
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
