using System;

namespace DS4Windows.DI
{
    public interface IEnvironmentService
    {
        bool RunAtStartup { get; set; }
        bool StartMinimized { get; set; }
        bool CloseMinimizes { get; set; }
        string UseLang { get; set; }

        int FormWidth { get; set; }
        int FormHeight { get; set; }
        int FormLocationX { get; set; }
        int FormLocationY { get; set; }

        event EventHandler EnvironmentSettingChanged;

        // ---- Phase5-Step14前クリーンアップ: MainWindow.xaml.cs残存Global参照の解消 ----
        bool IsAdministrator();
        string ApplicationVersion { get; }
        void RefreshHidHideInfo();
        void RefreshFakerInputInfo();
    }
}