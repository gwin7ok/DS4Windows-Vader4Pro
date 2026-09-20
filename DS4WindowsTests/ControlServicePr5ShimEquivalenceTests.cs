using System;
using Xunit;
using DS4Windows;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step2-5 (PR-5): 入力ループ（On_Report）の最頻度経路で参照するために追加・利用するシムが、
    /// Global（m_Config / BackingStore）と完全に同一の実体・同一の値を返すことを検証する。
    /// 各テストは変更した Global 状態を必ず元に戻す。
    /// </summary>
    public class ControlServicePr5ShimEquivalenceTests
    {
        private readonly AppSettingsService _appSettings = new AppSettingsService();
        private readonly AppearanceSettingsService _appearance = new AppearanceSettingsService();
        private readonly ProfileSettingsService _profileSettings = new ProfileSettingsService();
        private readonly ProfileActionProvider _actionProvider = new ProfileActionProvider();

        [Fact]
        public void UseUdpServerSmoothing_SharesStateWithGlobal()
        {
            bool original = Global.IsUsingUDPServerSmoothing();
            try
            {
                Global.UseUDPSeverSmoothing = !original;
                Assert.Equal(!original, _appSettings.UseUdpServerSmoothing);

                _appSettings.UseUdpServerSmoothing = original;
                Assert.Equal(original, Global.IsUsingUDPServerSmoothing());
            }
            finally
            {
                Global.UseUDPSeverSmoothing = original;
            }
        }

        [Fact]
        public void FlashWhenLateAt_SharesStateWithGlobal()
        {
            int original = Global.getFlashWhenLateAt();
            try
            {
                Global.FlashWhenLateAt = original + 7;
                Assert.Equal(original + 7, _appSettings.FlashWhenLateAt);

                _appSettings.FlashWhenLateAt = original + 3;
                Assert.Equal(original + 3, Global.getFlashWhenLateAt());
            }
            finally
            {
                Global.FlashWhenLateAt = original;
            }
        }

        [Fact]
        public void InvokeBatteryChanged_RaisesGlobalBatteryChangedWithSameSenderAndValue()
        {
            object receivedSender = null;
            byte receivedValue = 0;
            int fired = 0;
            EventHandler<byte> handler = (sender, value) =>
            {
                fired++;
                receivedSender = sender;
                receivedValue = value;
            };
            Global.BatteryChanged += handler;
            try
            {
                _appearance.InvokeBatteryChanged(42);

                Assert.Equal(1, fired);
                Assert.Same(typeof(Global), receivedSender);
                Assert.Equal((byte)42, receivedValue);
            }
            finally
            {
                Global.BatteryChanged -= handler;
            }
        }

        [Fact]
        public void UseIconChoice_SharesStateWithGlobal()
        {
            TrayIconChoice original = Global.UseIconChoice;
            TrayIconChoice other = original == TrayIconChoice.Battery ? TrayIconChoice.Default : TrayIconChoice.Battery;
            try
            {
                Global.UseIconChoice = other;
                Assert.Equal(other, _appearance.UseIconChoice);
            }
            finally
            {
                Global.UseIconChoice = original;
            }
        }

        [Fact]
        public void ContainsCustomAction_And_Extras_ShareStateWithGlobal()
        {
            const int slot = 0;
            bool originalAction = Global.store.containsCustomAction[slot];
            bool originalExtras = Global.store.containsCustomExtras[slot];
            try
            {
                Global.store.containsCustomAction[slot] = true;
                Global.store.containsCustomExtras[slot] = false;
                Assert.True(_profileSettings.ContainsCustomAction(slot));
                Assert.False(_profileSettings.ContainsCustomExtras(slot));
                Assert.Equal(Global.containsCustomAction(slot), _profileSettings.ContainsCustomAction(slot));

                Global.store.containsCustomAction[slot] = false;
                Global.store.containsCustomExtras[slot] = true;
                Assert.False(_profileSettings.ContainsCustomAction(slot));
                Assert.True(_profileSettings.ContainsCustomExtras(slot));
                Assert.Equal(Global.containsCustomExtras(slot), _profileSettings.ContainsCustomExtras(slot));
            }
            finally
            {
                Global.store.containsCustomAction[slot] = originalAction;
                Global.store.containsCustomExtras[slot] = originalExtras;
            }
        }

        [Fact]
        public void GetProfileActionCount_SharesStateWithGlobal()
        {
            const int slot = 0;
            int original = Global.store.profileActionCount[slot];
            try
            {
                Global.store.profileActionCount[slot] = original + 5;

                Assert.Equal(original + 5, _actionProvider.GetProfileActionCount(slot));
                Assert.Equal(Global.getProfileActionCount(slot), _actionProvider.GetProfileActionCount(slot));
            }
            finally
            {
                Global.store.profileActionCount[slot] = original;
            }
        }

        [Fact]
        public void TouchpadActiveArray_IsTheSameArrayAsGlobalTouchActive()
        {
            // CheckForTouchToggle／StartTPOff は TouchpadActiveArray の要素を書き換える。
            // Global.TouchActive／Global.touchpadActive と同一の配列を指していること（孤立配列でないこと）。
            Assert.Same(Global.touchpadActive, _profileSettings.TouchpadActiveArray);

            const int slot = 0;
            bool original = _profileSettings.TouchpadActiveArray[slot];
            try
            {
                _profileSettings.TouchpadActiveArray[slot] = !original;
                Assert.Equal(!original, Global.GetTouchActive(slot));
            }
            finally
            {
                _profileSettings.TouchpadActiveArray[slot] = original;
            }
        }

        [Fact]
        public void TouchOutMode_And_OutputDS4TriggerMode_MatchGlobalGetters()
        {
            const int slot = 0;

            Assert.Equal(Global.IsUsingTouchpadForControls(slot),
                _profileSettings.TouchOutMode[slot] == TouchpadOutMode.Controls);
            Assert.Equal(Global.GetOutputDS4TriggerMode(slot), _profileSettings.OutputDS4TriggerMode[slot]);
            Assert.Equal(Global.getEnableTouchToggle(slot), _profileSettings.GetEnableTouchToggle(slot));
            Assert.Equal(Global.getRumbleBoost(slot), _profileSettings.GetRumbleBoost(slot));
        }
    }
}