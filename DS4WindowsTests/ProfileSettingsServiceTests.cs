using System;
using System.Globalization;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    public class ProfileSettingsServiceTests
    {
        [Fact]
        public void Defaults_ShouldMatchInitialValues()
        {
            var service = TestRepositoryFactory.CreateProfileSettingsService();

            Assert.Equal(9, service.TouchpadActiveArray.Length);
            Assert.Equal(9, service.UseTempProfileArray.Length);
            Assert.Equal(9, service.TempProfileDistanceArray.Length);
            Assert.Equal(9, service.UseDInputOnlyArray.Length);

            for (int i = 0; i < 9; i++)
            {
                Assert.True(service.GetTouchpadActive(i));
                Assert.False(service.GetUseTempProfile(i));
                Assert.Equal(string.Empty, service.GetTempProfileName(i));
                Assert.False(service.TempProfileDistanceArray[i]);
                Assert.True(service.UseDInputOnlyArray[i]);
            }

            for (int i = 0; i < 8; i++)
            {
                Assert.False(service.GetLinkedProfileCheck(i));
            }
        }

        [Fact]
        public void SetAndGet_ShouldUpdateCorrectSlot()
        {
            var service = TestRepositoryFactory.CreateProfileSettingsService();

            service.TouchpadActiveArray[2] = false;
            service.SetUseTempProfile(3, true);
            service.SetTempProfileName(4, "TestProfile");
            service.SetTempProfileDistance(1, true);
            service.UseDInputOnlyArray[0] = false;
            service.SetLinkedProfileCheck(5, true);

            Assert.False(service.GetTouchpadActive(2));
            Assert.True(service.GetTouchpadActive(1));

            Assert.True(service.GetUseTempProfile(3));
            Assert.False(service.GetUseTempProfile(2));

            Assert.Equal("TestProfile", service.GetTempProfileName(4));
            Assert.Equal(string.Empty, service.GetTempProfileName(0));

            Assert.True(service.TempProfileDistanceArray[1]);
            Assert.False(service.UseDInputOnlyArray[0]);
            Assert.True(service.GetLinkedProfileCheck(5));
        }

        [Fact]
        public void ProfileActions_GetSet_ShouldPersist()
        {
            // Phase7-Step2 W2: 旧 ProfileSettingsServiceSubSettingsTests.ProfileActions_GetSet_ShouldPersistAndNotify から、
            // 通知（ProfileSettingChanged）の確認を除いて移した。
            var service = TestRepositoryFactory.CreateProfileSettingsService();

            var newActions = new System.Collections.Generic.List<string>[DS4Windows.ControllerLimits.MaxControllerCount];
            for (int i = 0; i < DS4Windows.ControllerLimits.MaxControllerCount; i++)
            {
                newActions[i] = new System.Collections.Generic.List<string> { $"Action_{i}_A", $"Action_{i}_B" };
            }

            // Phase6-Step9-1: ProfileActions の setter は Global.store.profileActions の配列そのものを置き換える。
            // 戻さないと、後続のテストが編集スロット（Global.TEST_PROFILE_INDEX = 8）を引いたときに範囲外になる
            // （Phase6-Status.md §6.4-1）。置き換え前の配列へ必ず戻す。
            System.Collections.Generic.List<string>[] originalActions = Global.store.profileActions;
            try
            {
                service.ProfileActions = newActions;

                Assert.NotNull(service.ProfileActions);
                Assert.Equal("Action_0_A", service.ProfileActions[0][0]);
            }
            finally
            {
                Global.store.profileActions = originalActions;
            }
        }

        [Fact]
        public void OutOfBounds_ShouldBeHandledSafely()
        {
            var service = TestRepositoryFactory.CreateProfileSettingsService();

            var touchpad = service.GetTouchpadActive(99);
            Assert.True(touchpad);

            service.SetUseTempProfile(99, true);
            Assert.False(service.GetUseTempProfile(99));
            service.SetTempProfileName(-1, "Invalid");
            Assert.Equal(string.Empty, service.GetTempProfileName(-1));
        }

        [Fact]
        public void TriggerCurveMode_ShouldRoundTripThroughService()
        {
            var service = TestRepositoryFactory.CreateProfileSettingsService();

            service.SetL2OutCurveMode(0, 2);
            service.SetR2OutCurveMode(0, 3);

            Assert.Equal(2, service.GetL2OutCurveMode(0));
            Assert.Equal(3, service.GetR2OutCurveMode(0));

            service.SetL2OutCurveMode(0, 0);
            service.SetR2OutCurveMode(0, 0);
            Assert.Equal(0, service.GetL2OutCurveMode(0));
            Assert.Equal(0, service.GetR2OutCurveMode(0));
        }

        [Fact]
        public void OutContType_ShouldShareBackingStoreWithGlobalShim()
        {
            // Issue7是正（Phase5-Step14-Issue7-Fix-Plan.md タスク7）の回帰防止テスト。
            // IOutputSlotService.GetOutputDeviceType が参照していた孤立配列（Global非連動）に
            // 起因するバグの再発を検知するため、ProfileSettingsService.OutContType が
            // Global.OutContType（m_Config.outputDevType）と同一の実体を参照していることを
            // 意図的に確認する（copilot-instructions.md §3.3-4 における「孤立プロパティ」検知の
            // ためのテストであり、削除・書き換えを行ってはならない）。
            var service = TestRepositoryFactory.CreateProfileSettingsService();
            Global.ProfileSettingsServiceInstance = service;

            Assert.Same(service.OutContType, Global.OutContType);

            OutContType original = service.OutContType[0];
            try
            {
                service.OutContType[0] = OutContType.DS4;
                Assert.Equal(OutContType.DS4, Global.OutContType[0]);

                Global.OutContType[0] = OutContType.X360;
                Assert.Equal(OutContType.X360, service.OutContType[0]);
            }
            finally
            {
                service.OutContType[0] = original;
            }
        }
    }
}