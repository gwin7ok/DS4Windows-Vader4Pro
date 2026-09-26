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
        private record RestoreCall(int Slot);
        private record ClearPendingCall(int Slot);

        private class MockProfileAppService : IProfileApplicationService
        {
            public int CallCount { get; set; }
            public string LastProfile { get; set; }
            public ProfileChangeSource LastSource { get; set; }

            public List<ApplyCall> ApplyCalls { get; } = new List<ApplyCall>();
            public List<int> RestoreCalls { get; } = new List<int>();
            public List<int> ClearPendingCalls { get; } = new List<int>();

            public bool ApplyProfile(int deviceIndex, string profileName, bool isTemp = false, bool launchProgram = true, ProfileChangeSource source = ProfileChangeSource.MappingAction, string prolog = "")
            {
                CallCount++;
                LastProfile = profileName;
                LastSource = source;
                ApplyCalls.Add(new ApplyCall(deviceIndex, profileName, isTemp, launchProgram, source, prolog));
                return true;
            }

            public void ApplyFromAction(int deviceIndex, SpecialAction action)
            {
                CallCount++;
                var profileName = action?.details ?? string.Empty;
                LastProfile = profileName;
                LastSource = ProfileChangeSource.MappingAction;
                ApplyCalls.Add(new ApplyCall(deviceIndex, profileName, false, false, ProfileChangeSource.MappingAction, string.Empty));
            }

            public bool RestoreFromAction(int deviceIndex)
            {
                RestoreCalls.Add(deviceIndex);
                return true;
            }

            public void ClearPendingRestore(int slot)
            {
                ClearPendingCalls.Add(slot);
            }
        }

        [Fact]
        public void DefaultProfileSwitcher_SwitchProfile_CallsAppServiceApplyFromAction()
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