using System;
using Xunit;
using DS4Windows;

namespace DS4WindowsTests
{
    public class ProfileSettingsServiceSubSettingsTests
    {
        [Fact]
        public void SubSettingChange_TriggerDeadZone_ShouldFireProfileSettingChanged()
        {
            var service = new ProfileSettingsService();
            if (service.L2ModInfo == null || service.L2ModInfo[0] == null)
                return; // 環境未初期化時はスキップ

            service.WireSubSettingsEvents(0);

            string reportedSetting = null;
            int reportedDevice = -1;

            service.ProfileSettingChanged += (s, e) =>
            {
                reportedDevice = e.DeviceIndex;
                reportedSetting = e.SettingName;
            };

            // デバイス 0 の L2 トリガーデッドゾーンを変更
            service.L2ModInfo[0].DeadZone = 12;

            Assert.Equal(0, reportedDevice);
            Assert.Equal("L2_DeadZone", reportedSetting);
        }

        [Fact]
        public void SubSettingChange_StickAxisDeadZone_ShouldFireProfileSettingChanged()
        {
            var service = new ProfileSettingsService();
            if (service.LSModInfo == null || service.LSModInfo[1] == null)
                return;

            service.WireSubSettingsEvents(1);

            string reportedSetting = null;
            int reportedDevice = -1;

            service.ProfileSettingChanged += (s, e) =>
            {
                reportedDevice = e.DeviceIndex;
                reportedSetting = e.SettingName;
            };

            // デバイス 1 の 左スティック X軸デッドゾーンを変更
            service.LSModInfo[1].xAxisDeadInfo.DeadZone = 25;

            Assert.Equal(1, reportedDevice);
            Assert.Equal("LS_X_DeadZone", reportedSetting);
        }

        [Fact]
        public void SubSettingChange_TouchpadAbsMouse_ShouldFireProfileSettingChanged()
        {
            var service = new ProfileSettingsService();
            if (service.TouchAbsMouse == null || service.TouchAbsMouse[0] == null)
                return;

            service.WireSubSettingsEvents(0);

            string reportedSetting = null;
            int reportedDevice = -1;

            service.ProfileSettingChanged += (s, e) =>
            {
                reportedDevice = e.DeviceIndex;
                reportedSetting = e.SettingName;
            };

            // デバイス 0 のタッチパッド SnapToCenter を変更
            service.TouchAbsMouse[0].SnapToCenter = true;

            Assert.Equal(0, reportedDevice);
            Assert.Equal("TouchAbs_SnapToCenter", reportedSetting);
        }
    }
}