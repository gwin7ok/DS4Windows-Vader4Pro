using System;
using DS4Windows;

namespace DS4Windows.Actions
{
    public interface IProfileSwitcher
    {
        void SwitchProfile(int deviceIndex, SpecialAction action);
        void RestoreProfile(int deviceIndex);
        void ApplyManualProfile(int deviceIndex, string profileName, bool launchProgram,
            bool xinputChange, ControlService control, ProfileChangeSource source,
            string prolog);
        void ClearState(int deviceIndex);
    }
}
