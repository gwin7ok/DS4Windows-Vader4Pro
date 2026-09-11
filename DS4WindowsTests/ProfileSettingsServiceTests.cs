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
            var service = new ProfileSettingsService();

            Assert.Equal(new CultureInfo("en-US"), service.ConfigDecimalCulture);
            Assert.Equal(9, service.TouchpadActiveArray.Length);
            Assert.Equal(9, service.UseTempProfileArray.Length);
            Assert.Equal(9, service.TempProfileNameArray.Length);
            Assert.Equal(9, service.TempProfileDistanceArray.Length);
            Assert.Equal(9, service.UseDInputOnlyArray.Length);
            Assert.Equal(8, service.LinkedProfileCheckArray.Length);

            for (int i = 0; i < 9; i++)
            {
                Assert.True(service.GetTouchpadActive(i));
                Assert.False(service.GetUseTempProfile(i));
                Assert.Equal(string.Empty, service.GetTempProfileName(i));
                Assert.False(service.GetTempProfileDistance(i));
                Assert.True(service.GetUseDInputOnly(i));
            }

            for (int i = 0; i < 8; i++)
            {
                Assert.False(service.GetLinkedProfileCheck(i));
            }
        }

        [Fact]
        public void SetAndGet_ShouldUpdateCorrectSlot()
        {
            var service = new ProfileSettingsService();

            service.SetTouchpadActive(2, false);
            service.SetUseTempProfile(3, true);
            service.SetTempProfileName(4, "TestProfile");
            service.SetTempProfileDistance(1, true);
            service.SetUseDInputOnly(0, false);
            service.SetLinkedProfileCheck(5, true);

            Assert.False(service.GetTouchpadActive(2));
            Assert.True(service.GetTouchpadActive(1));

            Assert.True(service.GetUseTempProfile(3));
            Assert.False(service.GetUseTempProfile(2));

            Assert.Equal("TestProfile", service.GetTempProfileName(4));
            Assert.Equal(string.Empty, service.GetTempProfileName(0));

            Assert.True(service.GetTempProfileDistance(1));
            Assert.False(service.GetUseDInputOnly(0));
            Assert.True(service.GetLinkedProfileCheck(5));
        }

        [Fact]
        public void SettingChangedEvent_ShouldFire()
        {
            var service = new ProfileSettingsService();
            ProfileSettingChangedEventArgs eventArgs = null;
            service.ProfileSettingChanged += (s, e) => eventArgs = e;

            service.SetTouchpadActive(1, false);

            Assert.NotNull(eventArgs);
            Assert.Equal(1, eventArgs.DeviceIndex);
            Assert.Equal(nameof(service.TouchpadActiveArray), eventArgs.SettingName);
            Assert.Equal(true, eventArgs.OldValue);
            Assert.Equal(false, eventArgs.NewValue);
        }

        [Fact]
        public void ResetToDefaults_ShouldRestoreValues()
        {
            var service = new ProfileSettingsService();

            service.SetTouchpadActive(0, false);
            service.SetUseTempProfile(0, true);
            service.SetTempProfileName(0, "Temp");
            service.SetLinkedProfileCheck(0, true);

            service.ResetToDefaults(0);

            Assert.True(service.GetTouchpadActive(0));
            Assert.False(service.GetUseTempProfile(0));
            Assert.Equal(string.Empty, service.GetTempProfileName(0));
            Assert.False(service.GetLinkedProfileCheck(0));
        }

        [Fact]
        public void GlobalShim_ShouldSynchronizeWithService()
        {
            var service = new ProfileSettingsService();
            Global.ProfileSettingsServiceInstance = service;

            Global.touchpadActive[2] = false;
            Global.tempprofilename[3] = "ShimTest";

            Assert.False(service.GetTouchpadActive(2));
            Assert.Equal("ShimTest", service.GetTempProfileName(3));

            service.SetTouchpadActive(2, true);
            Assert.True(Global.touchpadActive[2]);
        }

        [Fact]
        public void OutOfBounds_ShouldBeHandledSafely()
        {
            var service = new ProfileSettingsService();

            var touchpad = service.GetTouchpadActive(99);
            Assert.True(touchpad);

            service.SetTouchpadActive(99, false);
            service.SetTempProfileName(-1, "Invalid");
            Assert.Equal(string.Empty, service.GetTempProfileName(-1));
        }

        [Fact]
        public void TriggerSettings_ShouldShareBackingStoreWithGlobalShim()
        {
            var service = new ProfileSettingsService();
            Global.ProfileSettingsServiceInstance = service;

            Assert.Same(service.L2ModInfo, Global.L2ModInfo);
            Assert.Same(service.R2ModInfo, Global.R2ModInfo);
            Assert.Same(service.L2Sens, Global.L2Sens);
            Assert.Same(service.R2Sens, Global.R2Sens);
            Assert.Same(service.L2OutputSettings, Global.L2OutputSettings);
            Assert.Same(service.R2OutputSettings, Global.R2OutputSettings);
            Assert.Same(service.L2OutBezierCurveObj, Global.l2OutBezierCurveObj);
            Assert.Same(service.R2OutBezierCurveObj, Global.r2OutBezierCurveObj);
            Assert.Same(service.OutputVirtualTriggerButton, Global.OutputVirtualTriggerButton);
            Assert.Same(service.OutputDS4TriggerMode, Global.OutputDS4TriggerMode);
        }

        [Fact]
        public void TriggerCurveMode_ShouldDelegateThroughServiceAndGlobalShim()
        {
            var service = new ProfileSettingsService();
            Global.ProfileSettingsServiceInstance = service;

            service.SetL2OutCurveMode(0, 2);
            service.SetR2OutCurveMode(0, 3);

            Assert.Equal(2, Global.getL2OutCurveMode(0));
            Assert.Equal(3, Global.getR2OutCurveMode(0));

            Global.setL2OutCurveMode(0, 0);
            Global.setR2OutCurveMode(0, 0);
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
            var service = new ProfileSettingsService();
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