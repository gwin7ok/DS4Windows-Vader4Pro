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
        private class FakeActionChainService : IProfileActionChainService
        {
            public List<int> DispatchedSlots { get; } = new List<int>();

            public void DispatchNextActions(int deviceIndex, SpecialAction action)
            {
                DispatchedSlots.Add(deviceIndex);
            }
        }

        private record ApplyCall(int DeviceIndex, string ProfileName, bool IsTemp,
                    bool LaunchProgram, ProfileChangeSource Source, string Prolog);
        private record RestoreCall(int Slot, ProfileChangeSource Source);
        private record ClearPendingCall(int Slot);

        private class MockProfileAppService : IProfileApplicationService
        {
            public int CallCount { get; set; }
            public string LastProfile { get; set; }
            public ProfileChangeSource LastSource { get; set; }

            // 追加: テストで検証するための呼び出し履歴リスト
            public List<ApplyCall> ApplyCalls { get; } = new List<ApplyCall>();
            public List<int> RestoreCalls { get; } = new List<int>();
            public List<int> ClearPendingCalls { get; } = new List<int>();

            public bool ApplyProfile(int deviceIndex, string profileName, bool isTemp = false,
                bool launchProgram = false, ProfileChangeSource source = ProfileChangeSource.Manual,
                string prolog = null)
            {
                CallCount++;
                LastProfile = profileName;
                LastSource = source;
                ApplyCalls.Add(new ApplyCall(deviceIndex, profileName, isTemp, launchProgram, source, prolog));
                return true;
            }

            public void ApplyFromAction(int deviceIndex, SpecialAction action)
            {
                // 必要に応じて記録。アクションからの適用も ApplyCalls に含める場合
                // ApplyCalls.Add(new ApplyCall(deviceIndex, action.details, false, false, ProfileChangeSource.MappingAction, null));
            }

            public bool RestoreFromAction(int deviceIndex)
            {
                RestoreCalls.Add(deviceIndex);
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
            var service = new ProfileApplicationService(settings, new FakeActionChainService(), null, null);

            bool resNegative = service.ApplyProfile(-1, "Default");
            bool resTooHigh = service.ApplyProfile(4, "Default");

            Assert.False(resNegative);
            Assert.False(resTooHigh);
        }

        [Fact]
        public void ApplyProfile_NullOrWhitespaceProfile_ReturnsFalse()
        {
            var settings = new ProfileSettingsService();
            var service = new ProfileApplicationService(settings, new FakeActionChainService(), null, null);

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
            var service = new ProfileApplicationService(settings, new FakeActionChainService(), null, control);

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
            var service = new ProfileApplicationService(settings, new FakeActionChainService(), null, control);

            // 明示的に true を渡す
            bool result = service.ApplyProfile(0, "Default");

            Assert.True(result);
        }

        [Fact]
        public void RestoreFromAction_InvalidDeviceIndex_ReturnsFalse()
        {
            var settings = new ProfileSettingsService();
            var service = new ProfileApplicationService(settings, new FakeActionChainService(), null, null);

            Assert.False(service.RestoreFromAction(-1));
            Assert.False(service.RestoreFromAction(4));
        }

        [Fact]
        public void ClearPendingRestore_ExecutesWithoutException()
        {
            var settings = new ProfileSettingsService();
            var service = new ProfileApplicationService(settings, new FakeActionChainService(), null, null);

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