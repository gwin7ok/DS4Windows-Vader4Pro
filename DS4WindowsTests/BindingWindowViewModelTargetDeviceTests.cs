using System;
using System.Windows;
using System.Windows.Media;
using Xunit;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step7b-2: ボタン設定画面の ViewModel（`BindingWindowViewModel`）が、設定の読み書き先
    /// （`DeviceNum`＝編集スロット）とは別に実機のスロット番号 `targetDevice` を持ち、ライトバー強制色の
    /// プレビューが `targetDevice` にだけ効くことを固定する。
    /// Step7b-3 以降、編集スロットは常に `Global.TEST_PROFILE_INDEX`（8）になるため、編集スロットに 8 を渡して確認する。
    /// `DS4LightBar` の静的配列は、テストの前後で保存・復元する（Phase6-Status.md §6.4-1）。
    /// </summary>
    public class BindingWindowViewModelTargetDeviceTests
    {
        // CURRENT_DS4_CONTROLLER_LIMIT は環境により 4 または 8 のため、どちらでも範囲内の 1 を使う
        private const int TargetSlot = 1;

        static BindingWindowViewModelTargetDeviceTests()
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

        private static BindingWindowViewModel CreateViewModel(int targetDevice)
        {
            return new BindingWindowViewModel(Global.TEST_PROFILE_INDEX,
                new DS4ControlSettings(DS4Controls.Cross), targetDevice);
        }

        [Fact]
        public void TargetDevice_IsKeptSeparateFromEditSlot()
        {
            var vm = CreateViewModel(TargetSlot);

            Assert.Equal(Global.TEST_PROFILE_INDEX, vm.DeviceNum);
            Assert.Equal(TargetSlot, vm.TargetDevice);
            Assert.True(vm.HasTargetDevice);
        }

        [Fact]
        public void TargetDeviceIsOptional_DefaultsToNoTargetDevice()
        {
            var vm = new BindingWindowViewModel(Global.TEST_PROFILE_INDEX, new DS4ControlSettings(DS4Controls.Cross));

            Assert.Equal(-1, vm.TargetDevice);
            Assert.False(vm.HasTargetDevice);
        }

        [Fact]
        public void OutOfRangeTargetDevice_IsTreatedAsNoTargetDevice()
        {
            using var scope = new LightBarStateScope();

            var vm = CreateViewModel(ControlService.CURRENT_DS4_CONTROLLER_LIMIT);

            Assert.False(vm.HasTargetDevice);
            // 範囲外の添字で DS4LightBar の配列に触れない（例外にならず、何も変えない）
            vm.StartForcedColor(Colors.Red);
            vm.UpdateForcedColor(Colors.Blue);
            vm.EndForcedColor();
            Assert.All(DS4LightBar.forcelight, b => Assert.False(b));
        }

        [Fact]
        public void ForcedColorPreview_AffectsOnlyTargetDevice()
        {
            using var scope = new LightBarStateScope();

            var vm = CreateViewModel(TargetSlot);

            vm.StartForcedColor(Color.FromRgb(10, 20, 30));
            Assert.True(DS4LightBar.forcelight[TargetSlot]);
            Assert.Equal(new DS4Color(10, 20, 30), DS4LightBar.forcedColor[TargetSlot]);
            Assert.Equal((byte)0, DS4LightBar.forcedFlash[TargetSlot]);

            vm.UpdateForcedColor(Color.FromRgb(40, 50, 60));
            Assert.Equal(new DS4Color(40, 50, 60), DS4LightBar.forcedColor[TargetSlot]);

            for (int i = 0; i < DS4LightBar.forcelight.Length; i++)
            {
                if (i == TargetSlot) continue;
                Assert.False(DS4LightBar.forcelight[i], $"slot {i} の forcelight が変わった");
            }

            vm.EndForcedColor();
            Assert.False(DS4LightBar.forcelight[TargetSlot]);
            Assert.Equal(new DS4Color(0, 0, 0), DS4LightBar.forcedColor[TargetSlot]);
        }

        [Fact]
        public void ForcedColorPreview_WithoutTargetDevice_DoesNothing()
        {
            using var scope = new LightBarStateScope();

            var vm = CreateViewModel(-1);

            vm.StartForcedColor(Colors.Red);
            vm.UpdateForcedColor(Colors.Blue);
            vm.EndForcedColor();
            Assert.All(DS4LightBar.forcelight, b => Assert.False(b));
            Assert.All(DS4LightBar.forcedColor, c => Assert.Equal(new DS4Color(0, 0, 0), c));
        }
    }
}
