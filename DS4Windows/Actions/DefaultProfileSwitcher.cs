using System;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    public class DefaultProfileSwitcher : IProfileSwitcher
    {
        private readonly IAppSettingsService appSettingsService;

        public DefaultProfileSwitcher(IAppSettingsService appSettingsService)
        {
            this.appSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));
        }

        public void SwitchProfile(int deviceIndex, SpecialAction action)
        {
            Global.Instance.SwitchProfile(deviceIndex, action);
        }

        public void RestoreProfile(int deviceIndex)
        {
            Global.Instance.RestoreProfile(deviceIndex);
        }

        public void ApplyManualProfile(int deviceIndex, string profileName, bool launchProgram,
            bool xinputChange, ControlService control, ProfileChangeSource source,
            string prolog)
        {
            Global.Instance.ApplyManualProfile(deviceIndex, profileName, launchProgram,
                xinputChange, control, source, prolog);
        }

        public void ClearState(int deviceIndex)
        {
            Global.Instance.ClearState(deviceIndex);
        }
    }
}
