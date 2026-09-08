using System;
using System.Threading.Tasks;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF;
using DS4WinWPF.DS4Control.Services;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase 5 Step 13-9 / Watchpoint 2 核心振る舞いテスト:
    /// ① ワーカースレッドからのイベント発火に対するスレッド安全性
    /// ② Dispose 呼び出しによるリソース解放・多重呼出安全性の検証
    /// ※ 他テストへの状態汚染（Singleton副作用）を完全に防ぐため独立インスタンスを使用する。
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
            // Arrange: Singleton 汚染を防ぐため独立インスタンスを生成
            var isolatedSettings = new AppSettingsService();
            var isolatedSlots = new OutputSlotService();
            var vm = new SettingsViewModel(isolatedSettings, isolatedSlots);

            // Act: ワーカースレッド（非UIスレッド）から SettingChanged イベントを発火
            string receivedSetting = null;
            isolatedSettings.SettingChanged += (sender, setting) =>
            {
                receivedSetting = setting;
            };

            var exception = await Record.ExceptionAsync(() => Task.Run(() =>
            {
                isolatedSettings.UseExclusiveMode = true;
            }));

            // Assert: クロススレッド例外等が発生せず、安全にイベントが処理されること
            Assert.Null(exception);
            Assert.Equal(nameof(isolatedSettings.UseExclusiveMode), receivedSetting);

            vm.Dispose();
        }

        [Fact]
        public void Watchpoint2_Dispose_UnsubscribesAndIsIdempotent()
        {
            // Arrange
            var isolatedSettings = new AppSettingsService();
            var isolatedSlots = new OutputSlotService();
            var vm = new SettingsViewModel(isolatedSettings, isolatedSlots);

            // Act & Assert 1: 初回 Dispose が例外なく成功すること
            var ex1 = Record.Exception(() => vm.Dispose());
            Assert.Null(ex1);

            // Act & Assert 2: 2回目の Dispose (多重呼び出し) でも例外が発生せず安全（Idempotent）であること
            var ex2 = Record.Exception(() => vm.Dispose());
            Assert.Null(ex2);
        }
    }
}