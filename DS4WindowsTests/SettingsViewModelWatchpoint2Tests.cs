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
            // Arrange: AppHost から実サービスと ViewModel を解決
            DS4WinWPF.AppHost.CreateHost();
            var settingsService = DS4WinWPF.AppHost.GetService<IAppSettingsService>();
            var vm = DS4WinWPF.AppHost.GetService<SettingsViewModel>();
            Assert.NotNull(vm);

            // Act: ワーカースレッド（非UIスレッド）から設定プロパティを変更し、SettingChanged イベントを発火
            var exception = await Record.ExceptionAsync(() => Task.Run(() =>
            {
                settingsService.UseExclusiveMode = !settingsService.UseExclusiveMode;
                settingsService.StartMinimized = !settingsService.StartMinimized;
            }));

            // Assert: クロススレッド違反等の例外が発生せず、安全に完了すること
            Assert.Null(exception);

            vm.Dispose();
        }

        [Fact]
        public void Watchpoint2_Dispose_UnsubscribesFromEvents_PreventsGhostFiring()
        {
            // Arrange
            DS4WinWPF.AppHost.CreateHost();
            var settingsService = DS4WinWPF.AppHost.GetService<IAppSettingsService>();
            settingsService.UseExclusiveMode = false;

            var vm = DS4WinWPF.AppHost.GetService<SettingsViewModel>();
            Assert.NotNull(vm);

            int eventNotificationCount = 0;
            vm.HideDS4ControllerChanged += (sender, args) =>
            {
                eventNotificationCount++;
            };

            // 事前検証: Dispose 前はサービス値変更により vm.HideDS4ControllerChanged が発火すること
            settingsService.UseExclusiveMode = true;
            Assert.True(eventNotificationCount > 0, "事前検証: Dispose 前はイベント受信により UI 通知イベントが発火すること");

            // Act: ViewModel を破棄 (Dispose してアンフック)
            vm.Dispose();

            int countAtDispose = eventNotificationCount;

            // 破棄後にサービス側の値を変更（サービス側イベントは発火するが vm は購読解除済み）
            settingsService.UseExclusiveMode = false;

            // Assert: Dispose 済みのため、通知イベントカウントが増加しないこと (ゴースト発火抑止)
            Assert.Equal(countAtDispose, eventNotificationCount);
        }
    }
}