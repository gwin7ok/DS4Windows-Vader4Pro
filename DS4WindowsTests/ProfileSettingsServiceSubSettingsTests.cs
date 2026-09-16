using System;
using System.Collections.Generic;
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

        [Fact]
        public void WireSubSettingsEvents_MultipleCalls_ShouldNotAccumulateHandlers()
        {
            var service = new ProfileSettingsService();
            if (service.LSModInfo == null || service.LSModInfo[0] == null || service.LSModInfo[0].xAxisDeadInfo == null)
                return;

            // 複数回 WireSubSettingsEvents を呼び出して多重購読を試みる
            service.WireSubSettingsEvents(0);
            service.WireSubSettingsEvents(0);
            service.WireSubSettingsEvents(0);

            int eventCount = 0;
            service.ProfileSettingChanged += (s, e) =>
            {
                if (e.DeviceIndex == 0 && e.SettingName == "LS_X_DeadZone")
                {
                    eventCount++;
                }
            };

            // 1 回のみ変更
            service.LSModInfo[0].xAxisDeadInfo.DeadZone = 42;

            // 冪等化により、ハンドラが累積せず 1 回のみ発火すること
            Assert.Equal(1, eventCount);
        }

        [Fact]
        public void UnwireSubSettingsEvents_ShouldStopNotifications()
        {
            var service = new ProfileSettingsService();
            if (service.LSModInfo == null || service.LSModInfo[0] == null || service.LSModInfo[0].xAxisDeadInfo == null)
                return;

            service.WireSubSettingsEvents(0);
            // 購読全解除
            service.UnwireSubSettingsEvents(0);

            int eventCount = 0;
            service.ProfileSettingChanged += (s, e) =>
            {
                eventCount++;
            };

            // 解除後に変更
            service.LSModInfo[0].xAxisDeadInfo.DeadZone = 99;

            // イベントが発火しないこと
            Assert.Equal(0, eventCount);
        }

        [Fact]
        public void SubSettingChange_DeltaAccel_ShouldFireProfileSettingChanged()
        {
            var service = new ProfileSettingsService();
            if (service.LSOutputSettings == null || service.LSOutputSettings[0] == null ||
                service.LSOutputSettings[0].outputSettings?.controlSettings?.deltaAccelSettings == null)
                return;

            service.WireSubSettingsEvents(0);

            string reportedSetting = null;
            int reportedDevice = -1;

            service.ProfileSettingChanged += (s, e) =>
            {
                reportedDevice = e.DeviceIndex;
                reportedSetting = e.SettingName;
            };

            // デバイス 0 の左スティック DeltaAccel Multiplier を変更
            service.LSOutputSettings[0].outputSettings.controlSettings.deltaAccelSettings.Multiplier = 2.5;

            Assert.Equal(0, reportedDevice);
            Assert.Equal("LS_DeltaAccel_Multiplier", reportedSetting);
        }

        [Fact]
        public void ProfileActions_GetSet_ShouldPersistAndNotify()
        {
            var service = new ProfileSettingsService();

            var newActions = new List<string>[Global.MAX_DS4_CONTROLLER_COUNT];
            for (int i = 0; i < Global.MAX_DS4_CONTROLLER_COUNT; i++)
            {
                newActions[i] = new List<string> { $"Action_{i}_A", $"Action_{i}_B" };
            }

            string reportedSetting = null;
            service.ProfileSettingChanged += (s, e) =>
            {
                reportedSetting = e.SettingName;
            };

            service.ProfileActions = newActions;

            Assert.Equal("ProfileActions", reportedSetting);
            Assert.NotNull(service.ProfileActions);
            Assert.Equal("Action_0_A", service.ProfileActions[0][0]);
        }
    }
}