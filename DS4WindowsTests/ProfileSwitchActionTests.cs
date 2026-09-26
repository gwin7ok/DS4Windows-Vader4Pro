using System;
using System.Collections.Generic;
using Xunit;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    public class ProfileSwitchActionTests
    {
        private class MockOutputContext : IOutputContext
        {
            public int Device { get; }
            public IVirtualKBM OutputHandler { get; }
            public MockOutputContext(int device) { Device = device; }
        }

        private class MockProfileSwitcher : IProfileSwitcher
        {
            public record SwitchCall(int DeviceIndex, SpecialAction Action);

            public List<SwitchCall> SwitchProfileCalls { get; } = new List<SwitchCall>();
            public List<int> RestoreProfileCalls { get; } = new List<int>();

            public void SwitchProfile(int deviceIndex, SpecialAction action)
            {
                SwitchProfileCalls.Add(new SwitchCall(deviceIndex, action));
            }

            public void RestoreProfile(int slot)
            {
                RestoreProfileCalls.Add(slot);
            }

            public void ClearState(int slot)
            {
            }

            public void ApplyManualProfile(
                int deviceIndex,
                string profileName,
                bool isTemp = false,
                bool launchProgram = true,
                ControlService control = null,
                ProfileChangeSource source = ProfileChangeSource.MappingAction,
                string prolog = "")
            {
            }

            public void Reset()
            {
                SwitchProfileCalls.Clear();
                RestoreProfileCalls.Clear();
            }
        }

        [Fact]
        public void T1_Execute_CallsProfileSwitcherWithCorrectDeviceAndAction()
        {
            var mockSwitcher = new MockProfileSwitcher();
            var sa = new SpecialAction("SwitchAction1", "Cross", "Profile", "Profile", 0, "");
            sa.typeID = SpecialAction.ActionTypeId.Profile;
            var action = new ProfileSwitchActionAdapter(sa, 0, mockSwitcher);
            var ctx = new MockOutputContext(device: 0);

            action.Execute(ctx);

            Assert.Single(mockSwitcher.SwitchProfileCalls);
            Assert.Equal(0, mockSwitcher.SwitchProfileCalls[0].DeviceIndex);
            Assert.Same(sa, mockSwitcher.SwitchProfileCalls[0].Action);
        }

        [Fact]
        public void T2_Execute_PassesTargetDeviceIndexCorrectly()
        {
            var mockSwitcher = new MockProfileSwitcher();
            var sa = new SpecialAction("SwitchAction2", "Cross", "Profile", "Profile", 0, "");
            sa.typeID = SpecialAction.ActionTypeId.Profile;
            var action = new ProfileSwitchActionAdapter(sa, 3, mockSwitcher);
            var ctx = new MockOutputContext(device: 0);

            action.Execute(ctx);

            Assert.Single(mockSwitcher.SwitchProfileCalls);
            Assert.Equal(3, mockSwitcher.SwitchProfileCalls[0].DeviceIndex);
        }

        [Fact]
        public void T3_Stop_CallsRestoreProfileWithCorrectDevice()
        {
            var mockSwitcher = new MockProfileSwitcher();
            var sa = new SpecialAction("SwitchAction3", "Cross", "Profile", "Profile", 0, "");
            sa.typeID = SpecialAction.ActionTypeId.Profile;
            var action = new ProfileSwitchActionAdapter(sa, 1, mockSwitcher);
            var ctx = new MockOutputContext(device: 1);

            action.Stop(ctx);

            Assert.Single(mockSwitcher.RestoreProfileCalls);
            Assert.Equal(1, mockSwitcher.RestoreProfileCalls[0]);
        }

        [Fact]
        public void T4_MultipleExecutionsAndReset_TracksCorrectly()
        {
            var mockSwitcher = new MockProfileSwitcher();
            var sa = new SpecialAction("SwitchAction4", "Cross", "Profile", "Profile", 0, "");
            sa.typeID = SpecialAction.ActionTypeId.Profile;
            var action = new ProfileSwitchActionAdapter(sa, 0, mockSwitcher);
            var ctx = new MockOutputContext(device: 0);

            action.Execute(ctx);
            action.Execute(ctx);

            Assert.Equal(2, mockSwitcher.SwitchProfileCalls.Count);

            mockSwitcher.Reset();
            Assert.Empty(mockSwitcher.SwitchProfileCalls);
            Assert.Empty(mockSwitcher.RestoreProfileCalls);
        }
    }
}