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
            // Arrange: AppHost から解決
            DS4WinWPF.AppHost.CreateHost();
            var settingsService = DS4WinWPF.AppHost.GetService<IAppSettingsService>();
            var vm = DS4WinWPF.AppHost.GetService<SettingsViewModel>();
            Assert.NotNull(vm);

            // AppSettingsService 内の全フィールドから Delegate（イベントハンドラ）を探索するヘルパー
            bool IsSubscribedToVm(object targetService, object targetVm)
            {
                var type = targetService.GetType();
                while (type != null && type != typeof(object))
                {
                    var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var f in fields)
                    {
                        if (typeof(Delegate).IsAssignableFrom(f.FieldType))
                        {
                            if (f.GetValue(targetService) is Delegate del)
                            {
                                foreach (var inv in del.GetInvocationList())
                                {
                                    if (ReferenceEquals(inv.Target, targetVm))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    type = type.BaseType;
                }
                return false;
            }

            // 事前検証: Dispose 前は settingsService に vm のイベントハンドラが購読登録されていること
            bool subscribedBefore = IsSubscribedToVm(settingsService, vm);
            Assert.True(subscribedBefore, "事前検証: Dispose 前は settingsService のイベントに ViewModel が購読登録されていること");

            // Act: ViewModel を破棄 (Dispose してアンフック)
            vm.Dispose();

            // Assert: Dispose 後は settingsService の全デリゲートから vm のハンドラが完全に解除されていること (ゴースト発火抑止)
            bool subscribedAfter = IsSubscribedToVm(settingsService, vm);
            Assert.False(subscribedAfter, "検証成功: Dispose 後は settingsService から ViewModel のハンドラが完全にアンフックされていること");
        }
    }
}