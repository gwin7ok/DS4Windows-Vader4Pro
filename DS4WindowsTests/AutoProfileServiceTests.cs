using System;
using System.Collections.Generic;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using DS4WinWPF;

namespace DS4WindowsTests
{
    public class AutoProfileServiceTests
    {
        public AutoProfileServiceTests()
        {
            var pathService = new PathService();
            if (string.IsNullOrEmpty(Global.appdatapath))
            {
                Global.appdatapath = pathService.AppDataPath;
            }
        }

        private class MockProcessInspector : IProcessInspector
        {
            public string ForegroundPath { get; set; } = string.Empty;
            public string ForegroundTitle { get; set; } = string.Empty;
            public bool ReturnSuccess { get; set; } = true;

            public bool IsProcessRunning(string exePath) => false;

            public bool GetForegroundProcessInfo(out string processPath, out string windowTitle)
            {
                processPath = ForegroundPath;
                windowTitle = ForegroundTitle;
                return ReturnSuccess;
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

            public void ApplyFromAction(int deviceIndex, SpecialAction action) { }
            public bool RestoreFromAction(int deviceIndex) => true;

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

            public void ClearPendingRestore(int deviceIndex) { }
        }

        [Fact]
        public void CheckProfiles_WhenProcessInspectorReturnsFalse_DoesNotApply()
        {
            var mockInspector = new MockProcessInspector { ReturnSuccess = false };
            var mockAppService = new MockProfileAppService();
            var holder = new AutoProfileHolder();
            var service = new AutoProfileService(holder, mockAppService, new ProfileSettingsService(), mockInspector);

            service.CheckProfiles();

            Assert.Empty(mockAppService.ApplyCalls);
        }

        [Fact]
        public void CheckProfiles_MatchingRule_AppliesProfileWithAutoProfileSource()
        {
            var mockInspector = new MockProcessInspector
            {
                ForegroundPath = @"c:\games\testgame.exe",
                ForegroundTitle = "test game window",
                ReturnSuccess = true
            };
            var mockAppService = new MockProfileAppService();
            var settings = new ProfileSettingsService();
            var holder = new AutoProfileHolder();

            var entity = new AutoProfileEntity(@"c:\games\testgame.exe", "test game window");
            entity.ProfileNames[0] = "GameProfile";
            holder.AutoProfileColl.Add(entity);

            var service = new AutoProfileService(holder, mockAppService, settings, mockInspector);

            service.CheckProfiles();

            Assert.Single(mockAppService.ApplyCalls);
            Assert.Equal(0, mockAppService.ApplyCalls[0].DeviceIndex);
            Assert.Equal("GameProfile", mockAppService.ApplyCalls[0].ProfileName);
            Assert.True(mockAppService.ApplyCalls[0].IsTemp);
            Assert.Equal(ProfileChangeSource.AutoProfile, mockAppService.ApplyCalls[0].Source);
            // Step 4 申し送り事項: 通知設定が自動解決されるよう null で渡されていることを検証
            Assert.Null(mockAppService.ApplyCalls[0].DisplayNotification);
        }

        [Fact]
        public void CheckProfiles_UnknownProcessAfterMatch_RevertsDefaultProfile()
        {
            var mockInspector = new MockProcessInspector
            {
                ForegroundPath = @"c:\games\testgame.exe",
                ForegroundTitle = "test game window",
                ReturnSuccess = true
            };
            var mockAppService = new MockProfileAppService();
            var settings = new ProfileSettingsService();
            var holder = new AutoProfileHolder();

            var entity = new AutoProfileEntity(@"c:\games\testgame.exe", "test game window");
            entity.ProfileNames[0] = "GameProfile";
            holder.AutoProfileColl.Add(entity);

            var service = new AutoProfileService(holder, mockAppService, settings, mockInspector);

            // 1回目のチェック: ゲーム起動
            service.CheckProfiles();
            Assert.Single(mockAppService.ApplyCalls);

            // 一時プロファイルが適用された状態をセット
            settings.SetUseTempProfile(0, true);
            settings.SetTempProfileName(0, "GameProfile");
            Global.AutoProfileRevertDefaultProfile = true;

            // 2回目のチェック: ゲーム終了・未知プロセス（デスクトップ等）
            mockInspector.ForegroundPath = @"c:\windows\explorer.exe";
            mockInspector.ForegroundTitle = "";
            service.CheckProfiles();

            // デフォルトプロファイルへの復帰（isTemp = false）が呼ばれていることを確認
            Assert.Equal(2, mockAppService.ApplyCalls.Count);
            Assert.Equal(0, mockAppService.ApplyCalls[1].DeviceIndex);
            Assert.False(mockAppService.ApplyCalls[1].IsTemp);
            Assert.Equal(ProfileChangeSource.AutoProfile, mockAppService.ApplyCalls[1].Source);
            Assert.Null(mockAppService.ApplyCalls[1].DisplayNotification);
        }
    }
}
