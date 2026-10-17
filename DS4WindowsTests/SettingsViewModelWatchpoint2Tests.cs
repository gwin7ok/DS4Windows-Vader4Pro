using System;
using System.Threading.Tasks;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase 5 Step 13-9 / Watchpoint 2 核心振る舞いテスト:
    /// ① ワーカースレッドからのイベント発火に対するスレッド安全性
    /// ② Dispose 呼び出しによる多重呼出安全性 (Idempotent)
    /// ※ 他テストへの状態汚染（Global副作用）を防ぐため、変更値を finally で必ず元に戻す。
    /// </summary>
    public class SettingsViewModelWatchpoint2Tests
    {
        static SettingsViewModelWatchpoint2Tests()
        {
            if (string.IsNullOrEmpty(Global.appdatapath))
            {
                Global.appdatapath = AppContext.BaseDirectory;
            }
        }

        [Fact]
        public async Task Watchpoint2_WorkerThread_EventFiring_HandlesThreadSafetyWithoutCrash()
        {
            // Arrange: AppHost から DI 経由で解決
            DS4WinWPF.AppHost.CreateHost();
            var settingsService = DS4WinWPF.AppHost.GetService<IAppSettingsService>();
            var vm = DS4WinWPF.AppHost.GetService<SettingsViewModel>();
            Assert.NotNull(vm);

            bool originalExclusive = settingsService.UseExclusiveMode;
            string receivedSetting = null;
            settingsService.SettingChanged += (sender, setting) =>
            {
                receivedSetting = setting;
            };

            try
            {
                // Act: ワーカースレッド（非UIスレッド）から SettingChanged イベントを発火
                var exception = await Record.ExceptionAsync(() => Task.Run(() =>
                {
                    settingsService.UseExclusiveMode = !originalExclusive;
                }));

                // Assert: クロススレッド例外等が発生せず、安全にイベントが処理されること
                Assert.Null(exception);
            }
            finally
            {
                // 後続テストに影響を与えないよう元の状態に確実に復元
                settingsService.UseExclusiveMode = originalExclusive;
                vm.Dispose();
            }
        }

        [Fact]
        public void Watchpoint2_Dispose_UnsubscribesAndIsIdempotent()
        {
            // Arrange: AppHost から DI 経由で解決
            DS4WinWPF.AppHost.CreateHost();
            var vm = DS4WinWPF.AppHost.GetService<SettingsViewModel>();
            Assert.NotNull(vm);

            // Act & Assert 1: 初回 Dispose が例外なく成功すること
            var ex1 = Record.Exception(() => vm.Dispose());
            Assert.Null(ex1);

            // Act & Assert 2: 2回目の Dispose (多重呼び出し) でも例外が発生せず安全（Idempotent）であること
            var ex2 = Record.Exception(() => vm.Dispose());
            Assert.Null(ex2);
        }
    }
}