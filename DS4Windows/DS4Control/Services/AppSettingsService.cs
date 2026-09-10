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

        public AppSettingsService(IProfileXmlStore xmlStore, IPathService pathService)
        {
            _xmlStore = xmlStore ?? throw new ArgumentNullException(nameof(xmlStore));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        }

        public bool Save(string path = null)
        {
            try
            {
                var targetPath = path ?? _pathService.GetProfilesFilePath();
                return _xmlStore.SaveAppSettingsXml(targetPath);
            }
            catch (Exception ex)
            {
                AppLogger.LogToGui($"[AppSettingsService] Save failed: {ex.Message}", true);
                return false;
            }
        }

        public bool Load(string path = null)
        {
            var targetPath = path ?? _pathService.GetProfilesFilePath();
            return _xmlStore.LoadAppSettingsXml(targetPath);
        }

        public void NotifyChanged()
        {
            // 設定変更通知が必要な場合はここで発火
        }

        public bool StartMinimized
        {
            get => Global.StartMinimized;
            set => Global.StartMinimized = value;
        }

        public bool MinimizeToTaskbar
        {
            get => Global.MinToTaskbar;
            set => Global.MinToTaskbar = value;
        }

        public bool CloseMinimizes
        {
            get => Global.CloseMinimizes;
            set => Global.CloseMinimizes = value;
        }

        public int CheckWhen
        {
            get => _checkWhen;
            set => _checkWhen = value;
        }

        public bool UseUdpServer
        {
            get => Global.IsUsingUDPServer();
            set => Global.SetUseUDPServer(value);
        }

        public int UdpServerPort
        {
            get => Global.getUDPServerPortNum();
            set => Global.setUDPServerPortNum(value);
        }

        public string UdpServerListenAddress
        {
            get => Global.getUDPServerListenAddress();
            set => Global.setUDPServerListenAddress(value);
        }

        public bool UseExclusiveMode
        {
            get => Global.UseExclusiveMode;
            set => Global.UseExclusiveMode = value;
        }

        public bool AutoProfileRevertDefaultProfile
        {
            get => Global.AutoProfileRevertDefaultProfile;
            set => Global.AutoProfileRevertDefaultProfile = value;
        }

        public int FormWidth
        {
            get => Global.FormWidth;
            set => Global.FormWidth = value;
        }

        public int FormHeight
        {
            get => Global.FormHeight;
            set => Global.FormHeight = value;
        }

        public int FormLocationX
        {
            get => Global.FormLocationX;
            set => Global.FormLocationX = value;
        }

        public int FormLocationY
        {
            get => Global.FormLocationY;
            set => Global.FormLocationY = value;
        }

        public int ControllerIndexColWidth
        {
            get => Global.ControllerIndexColWidth;
            set => Global.ControllerIndexColWidth = value;
        }

        public int ControllerIdColWidth
        {
            get => Global.ControllerIdColWidth;
            set => Global.ControllerIdColWidth = value;
        }

        public int ControllerStatusColWidth
        {
            get => Global.ControllerStatusColWidth;
            set => Global.ControllerStatusColWidth = value;
        }

        public int ControllerExclusiveColWidth
        {
            get => Global.ControllerExclusiveColWidth;
            set => Global.ControllerExclusiveColWidth = value;
        }

        public int ControllerBatteryColWidth
        {
            get => Global.ControllerBatteryColWidth;
            set => Global.ControllerBatteryColWidth = value;
        }

        public int ControllerSelectProfileColWidth
        {
            get => Global.ControllerSelectProfileColWidth;
            set => Global.ControllerSelectProfileColWidth = value;
        }

        public int ControllerEditColWidth
        {
            get => Global.ControllerEditColWidth;
            set => Global.ControllerEditColWidth = value;
        }

        public int ControllerLinkedProfileColWidth
        {
            get => Global.ControllerLinkedProfileColWidth;
            set => Global.ControllerLinkedProfileColWidth = value;
        }

        public int ControllerLinkProfIdColWidth
        {
            get => Global.ControllerLinkProfIdColWidth;
            set => Global.ControllerLinkProfIdColWidth = value;
        }

        public int ControllerCustomColorColWidth
        {
            get => Global.ControllerCustomColorColWidth;
            set => Global.ControllerCustomColorColWidth = value;
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

        // ---- 全体設定（更新チェック・ログ・通知・スワイプ） ----
        public int LogMinLevel
        {
            get => Global.LogMinLevel;
            set => Global.LogMinLevel = value;
        }

        public bool CheckUpdateStartupEnabled
        {
            get => Global.CheckUpdateStartup;
            set => Global.CheckUpdateStartup = value;
        }

        public int CheckEveryValue
        {
            get => Global.CheckEvery;
            set => Global.CheckEvery = value;
        }

        public int CheckEveryUnit
        {
            get => Global.CheckEveryUnit;
            set => Global.CheckEveryUnit = value;
        }

        public DateTime LastChecked
        {
            get => Global.LastChecked;
            set => Global.LastChecked = value;
        }

        public string LastVersionCheckedNum
        {
            get => Global.LastVersionCheckedNumber;
            set => Global.LastVersionCheckedNumber = value;
        }

        public bool FirstRun
        {
            get => Global.FirstRun;
            set => Global.FirstRun = value;
        }

        public bool RunHotPlug
        {
            get => Global.RunHotPlug;
            set => Global.RunHotPlug = value;
        }

        public int Notifications
        {
            get => Global.Notifications;
            set => Global.Notifications = value;
        }

        public bool SwipeProfiles
        {
            get => Global.SwipeProfiles;
            set => Global.SwipeProfiles = value;
        }
    }
}
