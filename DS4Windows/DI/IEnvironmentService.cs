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
    }
}
