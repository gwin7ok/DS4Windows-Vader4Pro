using System;
using System.Collections.Generic;
using Xunit;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    public class ProfileApplicationServiceTests
    {
        private class FakeDeviceAccessor : IDeviceStateAccessor
        {
            private readonly DS4Device[] _devices = new DS4Device[4];

            public void SetController(int index, DS4Device device)
            {
                _devices[index] = device;
            }

            public DS4Device GetController(int index)
            {
                if (index < 0 || index >= 4) return null;
                return _devices[index];
            }

            public IEnumerable<DS4Device> GetControllers() => _devices;
        }

        private class FakeProfileSettings : IProfileSettingsService
        {
            public bool ProfileChangedNotification { get; set; } = false;
            public string GetProfilePath(int deviceIndex) => string.Empty;
            public void SetProfilePath(int deviceIndex, string path) { }
            public string GetTempProfileName(int deviceIndex) => string.Empty;
            public void SetTempProfileName(int deviceIndex, string name) { }
            public bool GetUseTempProfile(int deviceIndex) => false;
            public void SetUseTempProfile(int deviceIndex, bool useTemp) { }
        }

        private class FakeActionChainService : IProfileActionChainService
        {
            public List<int> DispatchedSlots { get; } = new List<int>();

            public void DispatchNextActions(int deviceIndex, SpecialAction action)
            {
                DispatchedSlots.Add(deviceIndex);
            }
        }

        private class MockProfileAppService : IProfileApplicationService
        {
            public class ApplyCall
            {
                public int DeviceIndex { get; set; }
                public string ProfileName { get; set; }
                public bool IsTemp { get; set; }
                public bool LaunchProgram { get; set; }
                public ProfileChangeSource Source { get; set; }
            }

            public List<ApplyCall> ApplyCalls { get; } = new List<ApplyCall>();
            public List<int> RestoreCalls { get; } = new List<int>();
            public List<int> ClearPendingCalls { get; } = new List<int>();

            public void ApplyFromAction(int deviceIndex, SpecialAction action) { }

            public bool RestoreFromAction(int deviceIndex)
            {
                RestoreCalls.Add(deviceIndex);
                return true;
            }

            public bool ApplyProfile(int deviceIndex, string profileName, bool isTemp = false, bool launchProgram = false,
                ProfileChangeSource source = ProfileChangeSource.Manual,
                string prolog = null, bool displayNotification = true)
            {
                ApplyCalls.Add(new ApplyCall
                {
                    DeviceIndex = deviceIndex,
                    ProfileName = profileName,
                    IsTemp = isTemp,
                    LaunchProgram = launchProgram,
                    Source = source
                });
                return true;
            }

            public void ClearPendingRestore(int deviceIndex)
            {
                ClearPendingCalls.Add(deviceIndex);
            }
        }

        [Fact]
        public void ApplyProfile_InvalidDeviceIndex_ReturnsFalse()
        {
            var service = new ProfileApplicationService(new FakeDeviceAccessor(), new FakeProfileSettings(), new FakeActionChainService(), null);

            bool resNegative = service.ApplyProfile(-1, "Default");
            bool resTooHigh = service.ApplyProfile(4, "Default");

            Assert.False(resNegative);
            Assert.False(resTooHigh);
        }

        [Fact]
        public void ApplyProfile_NullOrWhitespaceProfile_ReturnsFalse()
        {
            var service = new ProfileApplicationService(new FakeDeviceAccessor(), new FakeProfileSettings(), new FakeActionChainService(), null);

            bool resNull = service.ApplyProfile(0, null);
            bool resEmpty = service.ApplyProfile(0, "");
            bool resWhitespace = service.ApplyProfile(0, "   ");

            Assert.False(resNull);
            Assert.False(resEmpty);
            Assert.False(resWhitespace);
        }

        [Fact]
        public void RestoreFromAction_InvalidDeviceIndex_ReturnsFalse()
        {
            var service = new ProfileApplicationService(new FakeDeviceAccessor(), new FakeProfileSettings(), new FakeActionChainService(), null);

            Assert.False(service.RestoreFromAction(-1));
            Assert.False(service.RestoreFromAction(4));
        }

        [Fact]
        public void ClearPendingRestore_ExecutesWithoutException()
        {
            var service = new ProfileApplicationService(new FakeDeviceAccessor(), new FakeProfileSettings(), new FakeActionChainService(), null);

            var ex = Record.Exception(() => service.ClearPendingRestore(0));
            Assert.Null(ex);
        }

        [Fact]
        public void DefaultProfileSwitcher_SwitchProfile_DelegatesToProfileApplicationService()
        {
            var mockAppService = new MockProfileAppService();
            var switcher = new DefaultProfileSwitcher(mockAppService);
            var action = new SpecialAction("SwitchTest", "Cross", "Profile", "Profile", 0, "TargetProfile");

            switcher.SwitchProfile(0, action);

            Assert.Single(mockAppService.ApplyCalls);
            Assert.Equal(0, mockAppService.ApplyCalls[0].DeviceIndex);
            Assert.Equal("TargetProfile", mockAppService.ApplyCalls[0].ProfileName);
            Assert.Equal(ProfileChangeSource.MappingAction, mockAppService.ApplyCalls[0].Source);
        }

        [Fact]
        public void DefaultProfileSwitcher_RestoreProfile_DelegatesToRestoreFromAction()
        {
            var mockAppService = new MockProfileAppService();
            var switcher = new DefaultProfileSwitcher(mockAppService);

            switcher.RestoreProfile(1);

            Assert.Single(mockAppService.RestoreCalls);
            Assert.Equal(1, mockAppService.RestoreCalls[0]);
        }

        [Fact]
        public void DefaultProfileSwitcher_ClearState_ClearsAppServicePendingRestore()
        {
            var mockAppService = new MockProfileAppService();
            var switcher = new DefaultProfileSwitcher(mockAppService);

            switcher.ClearState(2);

            Assert.Single(mockAppService.ClearPendingCalls);
            Assert.Equal(2, mockAppService.ClearPendingCalls[0]);
        }
    }
}
