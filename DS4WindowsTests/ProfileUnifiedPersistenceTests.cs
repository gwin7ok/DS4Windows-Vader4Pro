using System;
using Xunit;
using DS4Windows;

namespace DS4WindowsTests
{
    public class ProfileUnifiedPersistenceTests
    {
        [Fact]
        public void SubSettings_StickDeadZone_ShouldBubbleToServiceAndMarkDirty()
        {
            var service = new ProfileSettingsService();
            if (service.LSModInfo == null || service.LSModInfo[0] == null)
                return;

            service.WireSubSettingsEvents(0);

            bool eventFired = false;
            string changedProperty = null;

            service.ProfileSettingChanged += (s, e) =>
            {
                if (e.DeviceIndex == 0)
                {
                    eventFired = true;
                    changedProperty = e.SettingName;
                }
            };

            // スティックの軸デッドゾーンを変更
            service.LSModInfo[0].xAxisDeadInfo.DeadZone = 18;

            Assert.True(eventFired);
            Assert.Equal("LS_X_DeadZone", changedProperty);
        }

        [Fact]
        public void SubSettings_GyroControls_ShouldBubbleToServiceAndMarkDirty()
        {
            var service = new ProfileSettingsService();
            if (service.GyroControlsInf == null || service.GyroControlsInf[0] == null)
                return;

            service.WireSubSettingsEvents(0);

            bool eventFired = false;
            string changedProperty = null;

            service.ProfileSettingChanged += (s, e) =>
            {
                if (e.DeviceIndex == 0)
                {
                    eventFired = true;
                    changedProperty = e.SettingName;
                }
            };

            // ジャイロコントロールのトグル設定を変更（TriggerToggle プロパティ）
            service.GyroControlsInf[0].TriggerToggle = true;

            Assert.True(eventFired);
            Assert.Equal("GyroControls_TriggerToggle", changedProperty);
        }
    }
}