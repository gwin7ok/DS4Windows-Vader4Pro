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
    }
}
