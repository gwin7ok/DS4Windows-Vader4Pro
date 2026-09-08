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
            // Arrange: AppHost から解決
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

            // 初期値をセット
            settingsService.UseExclusiveMode = false;
            var vm = DS4WinWPF.AppHost.GetService<SettingsViewModel>();
            Assert.NotNull(vm);

            // 事前検証: Dispose 前はイベント受信により ViewModel (HideDS4Controller) が更新される
            settingsService.UseExclusiveMode = true;
            Assert.True(vm.HideDS4Controller, "事前検証: Dispose 前はイベント受信により ViewModel が更新されること");

            // Act: ViewModel を破棄 (Dispose して SettingChanged イベントをアンフック)
            vm.Dispose();

            // 破棄後にサービス側の値を変更（サービス側イベントは発火するが vm は購読解除済み）
            settingsService.UseExclusiveMode = false;

            // Assert: Dispose 済みのためハンドラが呼ばれず、vm の HideDS4Controller は更新されない（ゴースト発火抑止）
            Assert.True(vm.HideDS4Controller, "検証成功: Dispose 後はイベント購読が解除されているため、プロパティが更新されてはならない");
        }
    }
}