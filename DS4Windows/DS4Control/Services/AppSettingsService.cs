using System;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    public class AppSettingsService : IAppSettingsService
    {
        private readonly IProfileXmlStore _xmlStore;
        private readonly IPathService _pathService;

        private bool _startMinimized;
        private bool _minimizeToTaskbar;
        private bool _closeMinimizes;
        private int _checkWhen;
        private bool _useUdpServer;
        private int _udpServerPort = 26760;
        private string _udpServerListenAddress = "127.0.0.1";
        private bool _useExclusiveMode;

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
            get => _startMinimized;
            set
            {
                if (_startMinimized != value)
                {
                    _startMinimized = value;
                    NotifyChanged(nameof(StartMinimized));
                }
            }
        }

        public bool MinimizeToTaskbar
        {
            get => _minimizeToTaskbar;
            set
            {
                if (_minimizeToTaskbar != value)
                {
                    _minimizeToTaskbar = value;
                    NotifyChanged(nameof(MinimizeToTaskbar));
                }
            }
        }

        public bool CloseMinimizes
        {
            get => _closeMinimizes;
            set
            {
                if (_closeMinimizes != value)
                {
                    _closeMinimizes = value;
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
            get => _useUdpServer;
            set
            {
                if (_useUdpServer != value)
                {
                    _useUdpServer = value;
                    NotifyChanged(nameof(UseUdpServer));
                }
            }
        }

        public int UdpServerPort
        {
            get => _udpServerPort;
            set
            {
                if (_udpServerPort != value)
                {
                    _udpServerPort = value;
                    NotifyChanged(nameof(UdpServerPort));
                }
            }
        }

        public string UdpServerListenAddress
        {
            get => _udpServerListenAddress;
            set
            {
                if (_udpServerListenAddress != value)
                {
                    _udpServerListenAddress = value;
                    NotifyChanged(nameof(UdpServerListenAddress));
                }
            }
        }

        public bool UseExclusiveMode
        {
            get => _useExclusiveMode;
            set
            {
                if (_useExclusiveMode != value)
                {
                    _useExclusiveMode = value;
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
    }
}
