using System;
using System.Security;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WinWPF
{
    [SuppressUnmanagedCodeSecurity]
    public class AutoProfileChecker
    {
        private readonly IAutoProfileService _service;

        public int AutoProfileDebugLogLevel
        {
            get => _service.AutoProfileDebugLogLevel;
            set => _service.AutoProfileDebugLogLevel = value;
        }

        public bool Running
        {
            get => _service.Running;
            set => _service.Running = value;
        }

        public delegate void ChangeServiceHandler(AutoProfileChecker sender, bool state);
        public event ChangeServiceHandler RequestServiceChange;

        public AutoProfileChecker(AutoProfileHolder holder, DS4Windows.DI.IProfileSettingsService profileSettings = null)
        {
            var appService = DS4WinWPF.AppHost.GetService<IAutoProfileService>();
            if (appService != null)
            {
                _service = appService;
            }
            else
            {
                _service = new AutoProfileService(holder, null, profileSettings);
            }

            _service.RequestServiceChange += (state) =>
            {
                RequestServiceChange?.Invoke(this, state);
            };
        }

        public void Process()
        {
            _service.CheckProfiles();
        }
    }
}
