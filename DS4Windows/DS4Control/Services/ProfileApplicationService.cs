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

            DS4Device device = _deviceState?.GetController(deviceIndex);
            bool loaded = false;

            Action restoreAction = () =>
            {
                if (previousProfileWasTemporary)
                    loaded = Global.LoadTempProfile(deviceIndex, profileName, true, _control);
                else
                {
                    Global.ProfilePath[deviceIndex] = profileName;
                    loaded = Global.LoadProfile(deviceIndex, false, _control);
                }

                if (loaded)
                {
                    Global.CompleteProfileApplication(deviceIndex, profileName, previousProfileWasTemporary,
                        _control, ProfileChangeSource.MappingAction, null,
                        _profileSettings.ProfileChangedNotification);
                }
            };

            if (device != null)
            {
                device.HaltReportingRunAction(restoreAction);
            }
            else
            {
                restoreAction();
            }

            if (!loaded)
                return false;

            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ProfileApplicationService.RestoreFromAction: Slot {deviceIndex}, Profile '{profileName}'");

            return true;
        }

        /// <summary>
        /// 指定されたスロットに対してプロファイルを適用します。
        /// デバイスが接続されている場合は HaltReportingRunAction により入力ループを安全に一時停止します（§5.2 ガードレール）。
        /// </summary>
        public bool ApplyProfile(int deviceIndex, string profileName, bool isTemp = false, bool launchProgram = false,
            ProfileChangeSource source = ProfileChangeSource.Manual,
            string prolog = null, bool displayNotification = true)
        {
            if (deviceIndex < 0 || deviceIndex >= 4 || string.IsNullOrWhiteSpace(profileName))
                return false;

            DS4Device device = _deviceState?.GetController(deviceIndex);
            bool success = false;

            try
            {
                Action applyAction = () =>
                {
                    Global.ApplyProfile(deviceIndex, profileName, isTemp, launchProgram,
                        _control, source, prolog, displayNotification);
                    success = true;
                };

                if (device != null)
                {
                    device.HaltReportingRunAction(applyAction);
                }
                else
                {
                    applyAction();
                }

                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace($"[DI] ProfileApplicationService.ApplyProfile: Slot {deviceIndex}, Profile '{profileName}', isTemp={isTemp}, success={success}");
            }
            catch (Exception ex)
            {
                try { AppLogger.LogTrace($"[DI] ProfileApplicationService.ApplyProfile failed: {ex}"); } catch { }
                success = false;
            }

            return success;
        }

        /// <summary>
        /// 切断時等に指定スロットの一時プロファイル復帰予約状態をクリアします（§5.6 ガードレール）。
        /// </summary>
        public void ClearPendingRestore(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= 4)
                return;

            while (Mapping.TakePendingRestoreProfileName(deviceIndex, out _) != null)
            {
            }

            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ProfileApplicationService.ClearPendingRestore: Cleared slot {deviceIndex}");
        }
    }
}
