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
                    Global.ApplyProfile(deviceIndex, action.details, action.IsTemporaryProfileAction, true, _control,
                        ProfileChangeSource.MappingAction, prolog, display);
                    _actionChain.DispatchNextActions(deviceIndex, action);
                });
            });

            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ProfileApplicationService.ApplyFromAction: Slot {deviceIndex}, Profile '{action.details}'");
        }

        public bool RestoreFromAction(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= 4)
                return false;

            bool previousProfileWasTemporary;
            string profileName = Mapping.TakePendingRestoreProfileName(deviceIndex, out previousProfileWasTemporary);
            if (profileName == null)
                return false;

            bool loaded;
            if (previousProfileWasTemporary)
                loaded = Global.LoadTempProfile(deviceIndex, profileName, true, _control);
            else
            {
                Global.ProfilePath[deviceIndex] = profileName;
                loaded = Global.LoadProfile(deviceIndex, false, _control);
            }

            if (!loaded)
                return false;

            Global.CompleteProfileApplication(deviceIndex, profileName, previousProfileWasTemporary,
                _control, ProfileChangeSource.MappingAction, null,
                _profileSettings.ProfileChangedNotification);

            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ProfileApplicationService.RestoreFromAction: Slot {deviceIndex}, Profile '{profileName}'");

            return true;
        }
    }
}
