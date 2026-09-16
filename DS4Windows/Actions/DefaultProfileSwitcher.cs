using System;
using DS4Windows;
using DS4Windows.DI;

namespace DS4Windows.Actions
{
    public class DefaultProfileSwitcher : IProfileSwitcher
    {
        private readonly IAppSettingsService appSettingsService;

        public DefaultProfileSwitcher()
        {
        }

        public DefaultProfileSwitcher(IAppSettingsService appSettingsService)
        {
            this.appSettingsService = appSettingsService;
        }

        public void SwitchProfile(int deviceIndex, SpecialAction action)
        {
            // TODO: DI移行に伴う実装
        }

        public void RestoreProfile(int deviceIndex)
        {
            // TODO: DI移行に伴う実装
        }

        public void ApplyManualProfile(int deviceIndex, string profileName, bool launchProgram,
            bool xinputChange, ControlService control, ProfileChangeSource source,
            string prolog)
        {
            // TODO: DI移行に伴う実装
        }

        public void ClearState(int deviceIndex)
        {
            // TODO: DI移行に伴う実装
        }
    }
}