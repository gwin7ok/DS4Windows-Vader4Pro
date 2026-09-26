using System;
using System.Windows;
using System.Windows.Media;
using Xunit;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step7b-1: `ProfileSettingsViewModel` が、設定の読み書き先（`Device`＝編集スロット）とは別に、
    /// 実機のスロット番号 `targetDevice` を持ち、実機を使う機能（`FuncDevNum`、ライトバー強制色のプレビュー）が
    /// `targetDevice` だけを使うことを固定する。
    /// Step7b-3 以降、プロファイル編集画面の編集スロットは常に `Global.TEST_PROFILE_INDEX`（8）になるため、
    /// 編集スロットに 8 を渡したときにも、実機側の処理が `targetDevice` の添字で動くことを確認する。
    /// `DS4LightBar` の静的配列は、テストの前後で保存・復元する（Phase6-Status.md §6.4-1）。
    /// </summary>
    public class ProfileSettingsViewModelTargetDeviceTests
    {
        // CURRENT_DS4_CONTROLLER_LIMIT は環境により 4 または 8 のため、どちらでも範囲内の 1 を使う
        private const int TargetSlot = 1;

        static ProfileSettingsViewModelTargetDeviceTests()
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
        }

        /// <summary>DS4LightBar の強制色関連の静的配列と、OutDevTypeTemp の編集スロットの値を保存・復元する。</summary>
        private sealed class StaticStateScope : IDisposable
        {
            private readonly bool[] forcelight = (bool[])DS4LightBar.forcelight.Clone();
            private readonly DS4Color[] forcedColor = (DS4Color[])DS4LightBar.forcedColor.Clone();
            private readonly byte[] forcedFlash = (byte[])DS4LightBar.forcedFlash.Clone();
            private readonly OutContType outDevTypeTempEditSlot = Global.outDevTypeTemp[Global.TEST_PROFILE_INDEX];

            public void Dispose()
            {
                Array.Copy(forcelight, DS4LightBar.forcelight, forcelight.Length);
                Array.Copy(forcedColor, DS4LightBar.forcedColor, forcedColor.Length);
                Array.Copy(forcedFlash, DS4LightBar.forcedFlash, forcedFlash.Length);
                Global.outDevTypeTemp[Global.TEST_PROFILE_INDEX] = outDevTypeTempEditSlot;
            }
        }

        private static ProfileSettingsViewModel CreateViewModel(int device, int targetDevice)
        {
            // Global.ProfileSettingsServiceInstance は差し替えない（引数で明示的に渡す）
            var service = new ProfileSettingsService();
            var outputSlotService = new OutputSlotService();
            return new ProfileSettingsViewModel(device, service,
                outputSlotService: outputSlotService, targetDevice: targetDevice);
        }

        private static void ResetForcedColorArrays()
        {
            for (int i = 0; i < DS4LightBar.forcelight.Length; i++)
            {
                DS4LightBar.forcelight[i] = false;
                DS4LightBar.forcedColor[i] = new DS4Color(0, 0, 0);
                DS4LightBar.forcedFlash[i] = 0;
            }
        }

        [Fact]
        public void TargetDevice_IsKeptSeparateFromEditSlot_AndUsedForFuncDevNum()
        {
            using var scope = new StaticStateScope();

            var vm = CreateViewModel(Global.TEST_PROFILE_INDEX, TargetSlot);

            Assert.Equal(Global.TEST_PROFILE_INDEX, vm.Device);
            Assert.Equal(TargetSlot, vm.TargetDevice);
            Assert.Equal(TargetSlot, vm.FuncDevNum);
        }

        [Fact]
        public void NoTargetDevice_FallsBackToController0ForFuncDevNum()
        {
            using var scope = new StaticStateScope();

            var vm = CreateViewModel(Global.TEST_PROFILE_INDEX, -1);

            Assert.Equal(-1, vm.TargetDevice);
            // プロファイル一覧から開いた場合の従来動作（ランブルテスト等はコントローラー0）を維持する
            Assert.Equal(0, vm.FuncDevNum);
        }

        [Fact]
        public void TargetDeviceIsOptional_DefaultsToNoTargetDevice()
        {
            using var scope = new StaticStateScope();

            var vm = new ProfileSettingsViewModel(Global.TEST_PROFILE_INDEX, new ProfileSettingsService(),
                outputSlotService: new OutputSlotService());

            Assert.Equal(-1, vm.TargetDevice);
            Assert.Equal(0, vm.FuncDevNum);
        }

        [Fact]
        public void OutOfRangeTargetDevice_IsTreatedAsNoTargetDevice()
        {
            using var scope = new StaticStateScope();
            ResetForcedColorArrays();

            int outOfRange = ControlService.CURRENT_DS4_CONTROLLER_LIMIT;
            var vm = CreateViewModel(Global.TEST_PROFILE_INDEX, outOfRange);

            Assert.Equal(0, vm.FuncDevNum);

            // 範囲外の添字で DS4LightBar の配列に触れない（例外にならず、何も変えない）
            vm.StartForcedColor(Colors.Red);
            vm.UpdateForcedColor(Colors.Blue);
            vm.EndForcedColor();
            Assert.All(DS4LightBar.forcelight, b => Assert.False(b));
        }

        [Fact]
        public void ForcedColorPreview_AffectsOnlyTargetDevice()
        {
            using var scope = new StaticStateScope();
            ResetForcedColorArrays();

            // 編集スロットに 8 を渡しても、プレビューは targetDevice に対して行われる
            var vm = CreateViewModel(Global.TEST_PROFILE_INDEX, TargetSlot);

            vm.StartForcedColor(Color.FromRgb(10, 20, 30));
            Assert.True(DS4LightBar.forcelight[TargetSlot]);
            Assert.Equal(new DS4Color(10, 20, 30), DS4LightBar.forcedColor[TargetSlot]);
            Assert.Equal((byte)0, DS4LightBar.forcedFlash[TargetSlot]);

            vm.UpdateForcedColor(Color.FromRgb(40, 50, 60));
            Assert.True(DS4LightBar.forcelight[TargetSlot]);
            Assert.Equal(new DS4Color(40, 50, 60), DS4LightBar.forcedColor[TargetSlot]);

            for (int i = 0; i < DS4LightBar.forcelight.Length; i++)
            {
                if (i == TargetSlot) continue;
                Assert.False(DS4LightBar.forcelight[i], $"slot {i} の forcelight が変わった");
                Assert.Equal(new DS4Color(0, 0, 0), DS4LightBar.forcedColor[i]);
            }

            vm.EndForcedColor();
            Assert.False(DS4LightBar.forcelight[TargetSlot]);
            Assert.Equal(new DS4Color(0, 0, 0), DS4LightBar.forcedColor[TargetSlot]);
        }

        [Fact]
        public void ForcedColorPreview_WithoutTargetDevice_DoesNothing()
        {
            using var scope = new StaticStateScope();
            ResetForcedColorArrays();

            var vm = CreateViewModel(Global.TEST_PROFILE_INDEX, -1);

            vm.StartForcedColor(Colors.Red);
            vm.UpdateForcedColor(Colors.Blue);
            Assert.All(DS4LightBar.forcelight, b => Assert.False(b));
            Assert.All(DS4LightBar.forcedColor, c => Assert.Equal(new DS4Color(0, 0, 0), c));

            vm.EndForcedColor();
            Assert.All(DS4LightBar.forcelight, b => Assert.False(b));
        }
    }
}
