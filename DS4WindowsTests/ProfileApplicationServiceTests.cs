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
                public bool? DisplayNotification { get; set; }
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
                string prolog = null, bool? displayNotification = null)
            {
                ApplyCalls.Add(new ApplyCall
                {
                    DeviceIndex = deviceIndex,
                    ProfileName = profileName,
                    IsTemp = isTemp,
                    LaunchProgram = launchProgram,
                    Source = source,
                    DisplayNotification = displayNotification
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
            var settings = new ProfileSettingsService();
            var service = new ProfileApplicationService(new FakeDeviceAccessor(), settings, new FakeActionChainService(), null);

            bool resNegative = service.ApplyProfile(-1, "Default");
            bool resTooHigh = service.ApplyProfile(4, "Default");

            Assert.False(resNegative);
            Assert.False(resTooHigh);
        }

        [Fact]
        public void ApplyProfile_NullOrWhitespaceProfile_ReturnsFalse()
        {
            var settings = new ProfileSettingsService();
            var service = new ProfileApplicationService(new FakeDeviceAccessor(), settings, new FakeActionChainService(), null);

            bool resNull = service.ApplyProfile(0, null);
            bool resEmpty = service.ApplyProfile(0, "");
            bool resWhitespace = service.ApplyProfile(0, "   ");

            Assert.False(resNull);
            Assert.False(resEmpty);
            Assert.False(resWhitespace);
        }

        [Fact]
        public void ApplyProfile_NullDisplayNotification_ResolvesFromSettings()
        {
            var pathService = new PathService();
            if (string.IsNullOrEmpty(Global.appdatapath))
            {
                Global.appdatapath = pathService.AppDataPath;
            }

            DS4WinWPF.AppHost.CreateHost();
            var control = DS4WinWPF.AppHost.GetService<ControlService>();

            var settings = new ProfileSettingsService();
            settings.ProfileChangedNotification = false;
            var service = new ProfileApplicationService(new FakeDeviceAccessor(), settings, new FakeActionChainService(), control);

            // displayNotification を省略（null）した状態で呼び出す
            bool result = service.ApplyProfile(0, "Default");

            Assert.True(result);
        }

        [Fact]
        public void ApplyProfile_ExplicitDisplayNotification_AcceptsExplicitValue()
        {
            var pathService = new PathService();
            if (string.IsNullOrEmpty(Global.appdatapath))
            {
                Global.appdatapath = pathService.AppDataPath;
            }

            DS4WinWPF.AppHost.CreateHost();
            var control = DS4WinWPF.AppHost.GetService<ControlService>();

            var settings = new ProfileSettingsService();
            settings.ProfileChangedNotification = false;
            var service = new ProfileApplicationService(new FakeDeviceAccessor(), settings, new FakeActionChainService(), control);

            // 明示的に true を渡す
            bool result = service.ApplyProfile(0, "Default", displayNotification: true);

            Assert.True(result);
        }

        [Fact]
        public void RestoreFromAction_InvalidDeviceIndex_ReturnsFalse()
        {
            var settings = new ProfileSettingsService();
            var service = new ProfileApplicationService(new FakeDeviceAccessor(), settings, new FakeActionChainService(), null);

            Assert.False(service.RestoreFromAction(-1));
            Assert.False(service.RestoreFromAction(4));
        }

        [Fact]
        public void ClearPendingRestore_ExecutesWithoutException()
        {
            var settings = new ProfileSettingsService();
            var service = new ProfileApplicationService(new FakeDeviceAccessor(), settings, new FakeActionChainService(), null);

            var ex = Record.Exception(() => service.ClearPendingRestore(0));
            Assert.Null(ex);
        }

        [Fact]
        public void DefaultProfileSwitcher_SwitchProfile_DelegatesToProfileApplicationService()
        {
            var mockAppService = new MockProfileAppService();
            var switcher = new DefaultProfileSwitcher(mockAppService);
            var action = new SpecialAction("SwitchTest", "Cross", "Profile", "TargetProfile", 0, "");
            action.typeID = SpecialAction.ActionTypeId.Profile;
            action.details = "TargetProfile";

            switcher.SwitchProfile(0, action);

            Assert.Single(mockAppService.ApplyCalls);
            Assert.Equal(0, mockAppService.ApplyCalls[0].DeviceIndex);
            Assert.Equal("TargetProfile", mockAppService.ApplyCalls[0].ProfileName);
            Assert.Equal(ProfileChangeSource.MappingAction, mockAppService.ApplyCalls[0].Source);
            Assert.Null(mockAppService.ApplyCalls[0].DisplayNotification);
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
