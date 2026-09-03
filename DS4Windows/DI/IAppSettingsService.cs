using System;

namespace DS4Windows.DI
{
    /// <summary>
    /// アプリ全体設定（Profiles.xml）の永続化・状態管理および変更通知を提供するサービス。
    /// </summary>
    public interface IAppSettingsService
    {
        bool Save();
        bool Load();

        event EventHandler<string> SettingChanged;

        bool StartMinimized { get; set; }
        bool MinimizeToTaskbar { get; set; }
        bool CloseMinimizes { get; set; }
        int CheckWhen { get; set; }
        bool UseUdpServer { get; set; }
        int UdpServerPort { get; set; }
        string UdpServerListenAddress { get; set; }
        bool UseExclusiveMode { get; set; }
        bool AutoProfileRevertDefaultProfile { get; set; }
    }
}
