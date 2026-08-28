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

        [Fact]
        public void T1_Execute_CallsProfileSwitcherWithCorrectDeviceAndAction()
        {
            var mockSwitcher = new MockProfileSwitcher();
            var sa = new SpecialAction("SwitchAction1", "Cross", "Profile", "Profile", 0, "");
            sa.typeID = SpecialAction.ActionTypeId.Profile;
            var action = new ProfileSwitchActionAdapter(sa, 0, mockSwitcher);
            var ctx = new MockOutputContext(device: 0);

            action.Execute(ctx);

            Assert.Single(mockSwitcher.SwitchCalls);
            Assert.Equal(0, mockSwitcher.SwitchCalls[0].Device);
            Assert.Same(sa, mockSwitcher.SwitchCalls[0].Action);
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

            Assert.Single(mockSwitcher.SwitchCalls);
            Assert.Equal(3, mockSwitcher.SwitchCalls[0].Device);
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

            Assert.Single(mockSwitcher.RestoreCalls);
            Assert.Equal(1, mockSwitcher.RestoreCalls[0]);
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

            Assert.Equal(2, mockSwitcher.SwitchCalls.Count);

            mockSwitcher.Reset();
            Assert.Empty(mockSwitcher.SwitchCalls);
            Assert.Empty(mockSwitcher.RestoreCalls);
        }
    }
}