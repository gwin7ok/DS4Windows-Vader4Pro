using System;
using System.Threading.Tasks;
using DS4Windows;
using DS4Windows.DI;

namespace DS4Windows.DS4Control.Services
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

            DS4Device device = _deviceState?.GetController(deviceIndex);
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
        }

        public void RestoreFromAction(int deviceIndex, SpecialAction action)
        {
            if (deviceIndex < 0 || deviceIndex >= 4 || action == null)
                return;

            DS4Device device = _deviceState?.GetController(deviceIndex);
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
                        ProfileChangeSource.RestoreFromAction, prolog, display);
                    _actionChain.DispatchNextActions(deviceIndex, action);
                });
            });
        }

        public void ClearPendingRestore(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= 4)
                return;

            Global.ClearPendingRestore(deviceIndex);
        }

        public bool ApplyProfile(int deviceIndex, string profileName, bool isTemp = false,
            bool launchProgram = false, ProfileChangeSource source = ProfileChangeSource.Default,
            string prolog = "", bool? displayNotification = null)
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                AppLogger.LogWarn($"[DI] ProfileApplicationService.ApplyProfile FAILED: profileName is null or whitespace for slot {deviceIndex}");
                AppLogger.LogWarn($"[DI] ProfileApplicationService.ApplyProfile FAILED: profileName is null or whitespace for slot {deviceIndex}");
                return false;
            }

            if (deviceIndex < 0 || deviceIndex >= ControlService.MAX_SLOTS)
            {
                AppLogger.LogWarn($"[DI] ProfileApplicationService.ApplyProfile FAILED: deviceIndex {deviceIndex} is out of bounds");
                return false;
            }

            bool success = false;
            try
            {
                DS4Device device = _deviceState?.GetController(deviceIndex);

                bool shouldDisplay = displayNotification ?? _profileSettings?.ProfileChangedNotification ?? false;

                Action applyAction = () =>
                {
                    Global.ApplyProfile(deviceIndex, profileName, isTemp, launchProgram,
                        _control, source, prolog, shouldDisplay);
                    success = true;
                };

                if (device != null)
                {
                    // SpecialAction (MappingAction) 縺九ｉ縺ｮ蜻ｼ縺ｳ蜃ｺ縺玲凾縺ｯ縲∝・蜉帙せ繝ｬ繝・ラ閾ｪ霄ｫ縺悟ｮ溯｡後＠縺ｦ縺・ｋ縺溘ａ縲・                    // HaltReportingRunAction 繧貞他縺ｶ縺ｨ閾ｪ蟾ｱ蠕・ｩ溘ち繧､繝繧｢繧ｦ繝茨ｼ医ョ繝・ラ繝ｭ繝・け蝗樣∩・峨ｒ襍ｷ縺薙☆縲・                    // 縺昴・縺溘ａ逶ｴ謗･ applyAction 繧貞ｮ溯｡後＠縲ゞI/謇句虚謫堺ｽ懈凾縺ｮ縺ｿ Halt 蠕・ｩ溘ｒ陦後≧縲・                    if (source == ProfileChangeSource.MappingAction)
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

        public void ApplyDefaultProfile(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= 4)
                return;

            string defaultProfile = _profileRepo?.GetProfileName(deviceIndex) ?? "Default";
            ApplyProfile(deviceIndex, defaultProfile, false, false, ProfileChangeSource.Default);
        }
    }
}