using System;
using System.Threading.Tasks;
using DS4Windows.DI;

namespace DS4Windows
{
    public class ProfileApplicationService : IProfileApplicationService
    {
        private readonly Services.IDeviceStateAccessor _deviceState;
        private readonly IProfileSettingsService _profileSettings;
        private readonly IProfileActionChainService _actionChain;
        private readonly ControlService _control;

        public ProfileApplicationService(Services.IDeviceStateAccessor deviceState,
            IProfileSettingsService profileSettings,
            IProfileActionChainService actionChain,
            ControlService control)
        {
            _deviceState = deviceState;
            _profileSettings = profileSettings;
            _actionChain = actionChain;
            _control = control;
        }

        public void ApplyFromAction(int deviceIndex, SpecialAction action)
        {
            if (deviceIndex < 0 || deviceIndex >= 4 || action == null)
                return;

            DS4Device device = _deviceState.GetController(deviceIndex);
            if (device == null)
                return;

            string prolog = string.Format(DS4WinWPF.Properties.Resources.UsingProfile,
                (deviceIndex + 1).ToString(), action.details, $"{device.Battery}");
            bool display = _profileSettings.ProfileChangedNotification;

            Task.Run(() =>
            {
                device.HaltReportingRunAction(() =>
                {
                    Global.ApplyProfile(deviceIndex, action.details, false, true, _control,
                        ProfileChangeSource.MappingAction, prolog, display);
                    _actionChain.DispatchNextActions(deviceIndex, action);
                });
            });

            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ProfileApplicationService.ApplyFromAction: Slot {deviceIndex}, Profile '{action.details}'");
        }

        public void RestoreFromAction(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= 4)
                return;

            string profileName = Mapping.TakePendingRestoreProfileName(deviceIndex);
            if (profileName == null)
                return;

            if (string.IsNullOrEmpty(profileName))
                Global.LoadProfile(deviceIndex, false, _control);
            else
                Global.LoadTempProfile(deviceIndex, profileName, true, _control);

            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ProfileApplicationService.RestoreFromAction: Slot {deviceIndex}, Profile '{profileName}'");
        }
    }
}
