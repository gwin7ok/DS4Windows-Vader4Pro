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
            get { lock (_syncLock) { return Global.ProfileEditorLeftWidth; } }
            set { lock (_syncLock) { Global.ProfileEditorLeftWidth = value; } }
        }

        public int ProfileEditorRightWidth
        {
            get { lock (_syncLock) { return Global.ProfileEditorRightWidth; } }
            set { lock (_syncLock) { Global.ProfileEditorRightWidth = value; } }
        }

        public int SpecialActionNameColWidth
        {
            get { lock (_syncLock) { return Global.SpecialActionNameColWidth; } }
            set { lock (_syncLock) { Global.SpecialActionNameColWidth = value; } }
        }

        public int SpecialActionTriggerColWidth
        {
            get { lock (_syncLock) { return Global.SpecialActionTriggerColWidth; } }
            set { lock (_syncLock) { Global.SpecialActionTriggerColWidth = value; } }
        }

        public int SpecialActionDetailColWidth
        {
            get { lock (_syncLock) { return Global.SpecialActionDetailColWidth; } }
            set { lock (_syncLock) { Global.SpecialActionDetailColWidth = value; } }
        }

        // ---- Phase5-Step13-7: 通知レベル・スワイプ操作設定 ----
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

        // ---- Phase5-Step14前クリーンアップ: MainWindow.xaml.cs残存Global参照の解消 ----
        // いずれも Global(m_Config/BackingStore) への薄い公開アクセサへの正規シム。
        // Notifications 等と同一パターン（独立フィールドを持たない）。
        public string LogMinLevel
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

        // Global.LastVersionCheckedNum は読み取り専用（LastVersionChecked代入時に自動算出）のため、
        // 本シムも読み取り専用として公開する。
        public ulong LastVersionCheckedNum => Global.LastVersionCheckedNum;

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
    }
}
