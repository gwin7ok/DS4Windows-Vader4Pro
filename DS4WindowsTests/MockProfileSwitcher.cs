using System;
using System.Collections.Generic;
using DS4Windows;
using DS4Windows.Actions;

namespace DS4WindowsTests
{
    /// <summary>
    /// C4 ProfileSwitchAction 単体テスト用の IProfileSwitcher モック
    /// </summary>
    public class MockProfileSwitcher : IProfileSwitcher
    {
        public class SwitchCall
        {
            public int DeviceIndex { get; set; }
            public SpecialAction Action { get; set; }
        }

        public List<SwitchCall> SwitchProfileCalls { get; } = new List<SwitchCall>();
        public List<int> RestoreProfileCalls { get; } = new List<int>();
        public List<int> ClearStateCalls { get; } = new List<int>();

        public void SwitchProfile(int deviceIndex, SpecialAction action)
        {
            SwitchProfileCalls.Add(new SwitchCall
            {
                DeviceIndex = deviceIndex,
                Action = action
            });
        }

        public void RestoreProfile(int deviceIndex)
        {
            RestoreProfileCalls.Add(deviceIndex);
        }

        public void ApplyManualProfile(int deviceIndex, string profileName, bool launchProgram,
            bool xinputChange, ControlService control, ProfileChangeSource source,
            string prolog, bool showNotification)
        {
        }

        public void ClearState(int deviceIndex)
        {
            ClearStateCalls.Add(deviceIndex);
        }

        public void Reset()
        {
            SwitchProfileCalls.Clear();
            RestoreProfileCalls.Clear();
            ClearStateCalls.Clear();
        }
    }
}
