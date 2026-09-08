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
    /// ② Dispose 呼び出しによるイベント購読解除 (Unsubscribe) とゴースト発火抑止
    /// AppHost 経由の実サービスを用いて厳密に検証する。
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
            // Arrange: AppHost から実サービスを解決して ViewModel を生成
            DS4WinWPF.AppHost.CreateHost();
            var settingsService = DS4WinWPF.AppHost.GetService<IAppSettingsService>();
            var outputSlotService = DS4WinWPF.AppHost.GetService<IOutputSlotService>();
            var vm = new SettingsViewModel(settingsService, outputSlotService);

            // Act: ワーカースレッド（非UIスレッド）からプロパティを変更し、SettingChanged イベントを発火
            var exception = await Record.ExceptionAsync(() => Task.Run(() =>
            {
                settingsService.UseExclusiveMode = !settingsService.UseExclusiveMode;
                settingsService.StartMinimized = !settingsService.StartMinimized;
            }));

            // Assert: クロススレッド違反例外等が発生せず、安全に完了すること
            Assert.Null(exception);

            vm.Dispose();
        }

        [Fact]
        public void Watchpoint2_Dispose_UnsubscribesFromEvents_PreventsGhostFiring()
        {
            // Arrange
            DS4WinWPF.AppHost.CreateHost();
            var settingsService = DS4WinWPF.AppHost.GetService<IAppSettingsService>();
            var outputSlotService = DS4WinWPF.AppHost.GetService<IOutputSlotService>();

            // 初期値を確実にセット
            settingsService.UseExclusiveMode = false;
            var vm = new SettingsViewModel(settingsService, outputSlotService);

            // 事前検証: Dispose 前はイベント受信により ViewModel が更新される
            settingsService.UseExclusiveMode = true;
            Assert.True(vm.UseExclusiveMode, "事前検証: Dispose 前はイベント受信により ViewModel が更新されること");

            // Act: ViewModel を破棄 (Dispose して SettingChanged イベントをアンフック)
            vm.Dispose();

            // 破棄後にサービス側の値を変更（サービス側イベントは発火するが vm は購読解除済み）
            settingsService.UseExclusiveMode = false;

            // Assert: Dispose 済みのためハンドラが呼ばれず、vm のプロパティは更新されない（ゴースト発火抑止）
            Assert.True(vm.UseExclusiveMode, "検証成功: Dispose 後はイベント購読が解除されているため、プロパティが更新されてはならない");
        }
    }
}