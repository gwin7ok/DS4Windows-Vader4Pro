using System;
using System.Reflection;
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
            // Arrange: AppHost から解決
            DS4WinWPF.AppHost.CreateHost();
            var settingsService = DS4WinWPF.AppHost.GetService<IAppSettingsService>();
            var vm = DS4WinWPF.AppHost.GetService<SettingsViewModel>();
            Assert.NotNull(vm);

            // Dispose 前の検証: settingsService.SettingChanged に vm のハンドラが登録されていることを確認
            var eventField = settingsService.GetType().GetField("SettingChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);

            var delegateBefore = eventField?.GetValue(settingsService) as Delegate;
            Assert.NotNull(delegateBefore);
            var listBefore = delegateBefore.GetInvocationList();
            bool containsVmBefore = Array.Exists(listBefore, d => ReferenceEquals(d.Target, vm));
            Assert.True(containsVmBefore, "事前検証: Dispose 前は settingsService に ViewModel のハンドラが登録されていること");

            // Act: ViewModel を破棄 (Dispose して SettingChanged イベントをアンフック)
            vm.Dispose();

            // Assert: Dispose 後は settingsService の購読リストから vm のハンドラがアンフックされていること (ゴースト発火抑止)
            var delegateAfter = eventField?.GetValue(settingsService) as Delegate;
            if (delegateAfter != null)
            {
                var listAfter = delegateAfter.GetInvocationList();
                bool containsVmAfter = Array.Exists(listAfter, d => ReferenceEquals(d.Target, vm));
                Assert.False(containsVmAfter, "検証成功: Dispose 後は settingsService から ViewModel のハンドラが完全にアンフックされていること");
            }
            else
            {
                // 全購読が解除されて null になった場合も正常
                Assert.Null(delegateAfter);
            }
        }
    }
}