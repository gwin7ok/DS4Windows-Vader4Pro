using System;
using System.Windows;
using System.Windows.Media;
using Xunit;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step7b-2: マクロ記録画面の ViewModel（`RecordBoxViewModel`）が、設定の読み書き先
    /// （`DeviceNum`＝編集スロット）とは別に実機のスロット番号 `targetDevice` を持ち、
    /// (a) 記録中のタッチパッド Passthru を実機のスロットにだけ設定して、`RevertControlsSettings` で元に戻すこと、
    /// (b) ライトバー強制色のプレビューが実機にだけ効くこと、を固定する。
    /// 編集スロットには、Step7b-3 以降と同じ `Global.TEST_PROFILE_INDEX`（8）を渡す。
    /// 設定は独立した `BackingStore` に対して操作し、`Global.store` は変えない。
    /// `DS4LightBar` の静的配列は、テストの前後で保存・復元する（Phase6-Status.md §6.4-1）。
    /// </summary>
    public class RecordBoxViewModelTargetDeviceTests
    {
        // CURRENT_DS4_CONTROLLER_LIMIT は環境により 4 または 8 のため、どちらでも範囲内の 1 を使う
        private const int TargetSlot = 1;

        static RecordBoxViewModelTargetDeviceTests()
        {
            if (string.IsNullOrEmpty(Global.appdatapath))
            {
                Global.appdatapath = AppContext.BaseDirectory;
            }

            if (Application.Current == null)
            {
                try
                {
                    _ = new Application();
                }
                catch { }
            }

            if (Application.ResourceAssembly == null)
            {
                try
                {
                    Application.ResourceAssembly = typeof(DS4WinWPF.AppHost).Assembly;
                }
                catch { }
            }

            // RecordBoxViewModel のコンストラクタ（MacroStepItem.CacheImgLocations）が参照するリソースキー。
            // PatternCViewModelTests と同じ準備（実際の画像は不要）
            if (Application.Current != null)
            {
                string[] resourceKeys = new string[]
                {
                    "KeyDownImg", "KeyUpImg", "KeyWaitImg", "KeyHoldImg",
                    "ClockImg", "MouseImg", "CustomKeyImg", "TouchpadImg",
                    "GyroImg", "WheelImg", "UnboundImg", "WaitImg", "HoldImg",
                    "LeftImg", "RightImg", "MiddleImg", "Clock", "Mouse", "CustomKey"
                };

                foreach (var key in resourceKeys)
                {
                    if (!Application.Current.Resources.Contains(key))
                    {
                        Application.Current.Resources[key] = "";
                    }
                }
            }
        }

        /// <summary>DS4LightBar の強制色関連の静的配列を保存・復元する。</summary>
        private sealed class LightBarStateScope : IDisposable
        {
            private readonly bool[] forcelight = (bool[])DS4LightBar.forcelight.Clone();
            private readonly DS4Color[] forcedColor = (DS4Color[])DS4LightBar.forcedColor.Clone();
            private readonly byte[] forcedFlash = (byte[])DS4LightBar.forcedFlash.Clone();

            public LightBarStateScope()
            {
                for (int i = 0; i < DS4LightBar.forcelight.Length; i++)
                {
                    DS4LightBar.forcelight[i] = false;
                    DS4LightBar.forcedColor[i] = new DS4Color(0, 0, 0);
                    DS4LightBar.forcedFlash[i] = 0;
                }
            }

            public void Dispose()
            {
                Array.Copy(forcelight, DS4LightBar.forcelight, forcelight.Length);
                Array.Copy(forcedColor, DS4LightBar.forcedColor, forcedColor.Length);
                Array.Copy(forcedFlash, DS4LightBar.forcedFlash, forcedFlash.Length);
            }
        }

        private static ProfileSettingsService CreateIsolatedSettings()
        {
            var settings = new ProfileSettingsService(new BackingStore());
            for (int i = 0; i < settings.TouchOutMode.Length; i++)
            {
                settings.TouchOutMode[i] = TouchpadOutMode.Mouse;
            }
            return settings;
        }

        private static RecordBoxViewModel CreateViewModel(ProfileSettingsService settings, int targetDevice)
        {
            return new RecordBoxViewModel(Global.TEST_PROFILE_INDEX, new DS4ControlSettings(DS4Controls.Cross),
                false, true, settings, null, targetDevice);
        }

        [Fact]
        public void TouchpadPassthru_IsAppliedToTargetDeviceOnly_AndReverted()
        {
            var settings = CreateIsolatedSettings();

            var vm = CreateViewModel(settings, TargetSlot);

            Assert.Equal(Global.TEST_PROFILE_INDEX, vm.DeviceNum);
            Assert.Equal(TargetSlot, vm.TargetDevice);
            Assert.Equal(TouchpadOutMode.Passthru, settings.TouchOutMode[TargetSlot]);
            // 編集スロットとほかのスロットの設定は変えない
            for (int i = 0; i < settings.TouchOutMode.Length; i++)
            {
                if (i == TargetSlot) continue;
                Assert.Equal(TouchpadOutMode.Mouse, settings.TouchOutMode[i]);
            }

            vm.RevertControlsSettings();
            Assert.Equal(TouchpadOutMode.Mouse, settings.TouchOutMode[TargetSlot]);

            // Save と Cancel の両方から呼ばれても、2 回目で値を壊さない
            vm.RevertControlsSettings();
            Assert.Equal(TouchpadOutMode.Mouse, settings.TouchOutMode[TargetSlot]);
        }

        [Fact]
        public void WithoutTargetDevice_NoTouchpadModeIsChanged()
        {
            var settings = CreateIsolatedSettings();

            var vm = CreateViewModel(settings, -1);
            Assert.Equal(-1, vm.TargetDevice);
            Assert.All(settings.TouchOutMode, m => Assert.Equal(TouchpadOutMode.Mouse, m));

            vm.RevertControlsSettings();
            Assert.All(settings.TouchOutMode, m => Assert.Equal(TouchpadOutMode.Mouse, m));
        }

        [Fact]
        public void TargetDeviceIsOptional_DefaultsToNoTargetDevice()
        {
            var settings = CreateIsolatedSettings();

            var vm = new RecordBoxViewModel(Global.TEST_PROFILE_INDEX, new DS4ControlSettings(DS4Controls.Cross),
                false, true, settings);

            Assert.Equal(-1, vm.TargetDevice);
            Assert.All(settings.TouchOutMode, m => Assert.Equal(TouchpadOutMode.Mouse, m));
        }

        [Fact]
        public void ForcedColorPreview_AffectsOnlyTargetDevice()
        {
            using var scope = new LightBarStateScope();
            var settings = CreateIsolatedSettings();

            var vm = CreateViewModel(settings, TargetSlot);

            vm.StartForcedColor(Color.FromRgb(10, 20, 30));
            Assert.True(DS4LightBar.forcelight[TargetSlot]);
            Assert.Equal(new DS4Color(10, 20, 30), DS4LightBar.forcedColor[TargetSlot]);

            vm.UpdateForcedColor(Color.FromRgb(40, 50, 60));
            Assert.Equal(new DS4Color(40, 50, 60), DS4LightBar.forcedColor[TargetSlot]);

            for (int i = 0; i < DS4LightBar.forcelight.Length; i++)
            {
                if (i == TargetSlot) continue;
                Assert.False(DS4LightBar.forcelight[i], $"slot {i} の forcelight が変わった");
            }

            vm.EndForcedColor();
            Assert.False(DS4LightBar.forcelight[TargetSlot]);
            vm.RevertControlsSettings();
        }

        [Fact]
        public void ForcedColorPreview_WithoutTargetDevice_DoesNothing()
        {
            using var scope = new LightBarStateScope();
            var settings = CreateIsolatedSettings();

            var vm = CreateViewModel(settings, -1);

            vm.StartForcedColor(Colors.Red);
            vm.UpdateForcedColor(Colors.Blue);
            vm.EndForcedColor();
            Assert.All(DS4LightBar.forcelight, b => Assert.False(b));
            Assert.All(DS4LightBar.forcedColor, c => Assert.Equal(new DS4Color(0, 0, 0), c));
        }
    }
}
