using System;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    public class MockProfileSwitcher : IProfileSwitcher
    {
        public int SwitchProfileCalledCount { get; private set; }
        public int RestoreProfileCalledCount { get; private set; }
        public int ApplyManualProfileCalledCount { get; private set; }
        public int ClearStateCalledCount { get; private set; }

        public int LastDeviceIndex { get; private set; }
        public SpecialAction LastAction { get; private set; }
        public string LastProfileName { get; private set; }
        public bool LastLaunchProgram { get; private set; }
        public bool LastXinputChange { get; private set; }
        public ProfileChangeSource LastSource { get; private set; }
        public string LastProlog { get; private set; }

        public void SwitchProfile(int deviceIndex, SpecialAction action)
        {
            SwitchProfileCalledCount++;
            LastDeviceIndex = deviceIndex;
            LastAction = action;
        }

        public void RestoreProfile(int deviceIndex)
        {
            RestoreProfileCalledCount++;
            LastDeviceIndex = deviceIndex;
        }

        public void ApplyManualProfile(int deviceIndex, string profileName, bool launchProgram,
            bool xinputChange, ControlService control, ProfileChangeSource source,
            string prolog)
        {
            ApplyManualProfileCalledCount++;
            LastDeviceIndex = deviceIndex;
            LastProfileName = profileName;
            LastLaunchProgram = launchProgram;
            LastXinputChange = xinputChange;
            LastSource = source;
            LastProlog = prolog;
        }

        public void ClearState(int deviceIndex)
        {
            ClearStateCalledCount++;
            LastDeviceIndex = deviceIndex;
        }
    }
}
