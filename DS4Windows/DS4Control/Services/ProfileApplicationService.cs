using System;
using System.Threading.Tasks;
using DS4Windows;
using DS4Windows.DI;

namespace DS4Windows
{
    public class ProfileApplicationService : IProfileApplicationService
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IProfileActionChainService _actionChain;
        private readonly IDeviceStateService _deviceState;
        private readonly ControlService _control;
        private readonly IProfileRepository _profileRepo;

        public ProfileApplicationService(
            IProfileSettingsService profileSettings = null,
            IProfileActionChainService actionChain = null,
            IDeviceStateService deviceState = null,
            ControlService control = null,
            IProfileRepository profileRepo = null)
        {
            _profileSettings = profileSettings ?? DS4WinWPF.AppHost.GetService<IProfileSettingsService>();
            _actionChain = actionChain ?? DS4WinWPF.AppHost.GetService<IProfileActionChainService>();
            _deviceState = deviceState ?? DS4WinWPF.AppHost.GetService<IDeviceStateService>();
            _control = control ?? DS4WinWPF.AppHost.GetService<ControlService>();
            _profileRepo = profileRepo ?? DS4WinWPF.AppHost.GetService<IProfileRepository>();
        }

        public void ApplyFromAction(int deviceIndex, SpecialAction action)
        {
            if (deviceIndex < 0 || deviceIndex >= 4 || action == null)
                return;

            DS4Device device = _control?.DS4Controllers?[deviceIndex];

            string prolog = string.Format(DS4WinWPF.Properties.Resources.UsingProfile,
                (deviceIndex + 1).ToString(), action.details, device != null ? $"{device.Battery}" : "N/A");
            bool display = _profileSettings?.ProfileChangedNotification ?? false;

            Task.Run(() =>
            {
                if (device != null)
                {
                    device.HaltReportingRunAction(() =>
                    {
                        Global.ApplyProfile(deviceIndex, action.details, action.IsTemporaryProfileAction, true, _control,
                            ProfileChangeSource.MappingAction, prolog, display);
                        _actionChain?.DispatchNextActions(deviceIndex, action);
                    });
                }
                else
                {
                    Global.ApplyProfile(deviceIndex, action.details, action.IsTemporaryProfileAction, true, _control,
                        ProfileChangeSource.MappingAction, prolog, display);
                    _actionChain?.DispatchNextActions(deviceIndex, action);
                }
            });
        }

        public bool RestoreFromAction(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= 4)
                return false;

            string previousProfile = Global.OlderProfilePath?[deviceIndex];
            if (string.IsNullOrWhiteSpace(previousProfile))
            {
                AppLogger.LogWarn($"[DI] ProfileApplicationService.RestoreFromAction: No OlderProfilePath for device {deviceIndex}");
                return false;
            }

            return ApplyProfile(deviceIndex, previousProfile, false, false, ProfileChangeSource.MappingAction);
        }

        public void ClearPendingRestore(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= 4)
                return;

            if (Global.OlderProfilePath != null && deviceIndex < Global.OlderProfilePath.Length)
            {
                Global.OlderProfilePath[deviceIndex] = string.Empty;
            }
        }

        public bool ApplyProfile(int deviceIndex, string profileName, bool isTemp = false,
            bool launchProgram = false, ProfileChangeSource source = ProfileChangeSource.Manual,
            string prolog = null, bool? displayNotification = null)
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                AppLogger.LogWarn($"[DI] ProfileApplicationService.ApplyProfile FAILED: profileName is null or whitespace for slot {deviceIndex}");
                return false;
            }

            if (deviceIndex < 0 || deviceIndex >= 4)
            {
                AppLogger.LogWarn($"[DI] ProfileApplicationService.ApplyProfile FAILED: deviceIndex {deviceIndex} is out of bounds");
                return false;
            }

            bool success = false;
            try
            {
                DS4Device device = _control?.DS4Controllers?[deviceIndex];
                bool shouldDisplay = displayNotification ?? _profileSettings?.ProfileChangedNotification ?? false;

                Action applyAction = () =>
                {
                    Global.ApplyProfile(deviceIndex, profileName, isTemp, launchProgram,
                        _control, source, prolog, shouldDisplay);
                    success = true;
                };

                if (device != null)
                {
                    // SpecialAction (MappingAction) からの呼び出し時は、入力スレッド自身が実行しているため、
                    // HaltReportingRunAction を呼ぶと自己待機タイムアウトを起こす。
                    // そのため直接 applyAction を実行し、UI/手動操作時のみ Halt 待機を行う。
                    if (source == ProfileChangeSource.MappingAction)
                    {
                        applyAction();
                    }
                    else
                    {
                        device.HaltReportingRunAction(applyAction);
                    }
                }
                else
                {
                    applyAction();
                }

                AppLogger.LogTrace($"[DI] ProfileApplicationService.ApplyProfile: Slot {deviceIndex}, Profile '{profileName}', isTemp={isTemp}, displayNotification={shouldDisplay}, success={success}");
                if (!success)
                {
                    AppLogger.LogWarn($"[DI] ProfileApplicationService.ApplyProfile Result: FAILED for Slot {deviceIndex}, Profile '{profileName}', isTemp={isTemp}, source={source}");
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[DI] ProfileApplicationService.ApplyProfile failed: {ex}");
                success = false;
            }

            return success;
        }
    }
}