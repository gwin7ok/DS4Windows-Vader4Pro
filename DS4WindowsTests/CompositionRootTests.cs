using System;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using DS4WinWPF;

namespace DS4WindowsTests
{
    public class CompositionRootTests
    {
        [Fact]
        public void AppHost_CreateHost_ShouldBuildHost()
        {
            var host = DS4WinWPF.AppHost.CreateHost();
            Assert.NotNull(host);
            Assert.NotNull(DS4WinWPF.AppHost.Host);
        }

        [Fact]
        public void AppHost_AllServices_ShouldResolveSuccessfully()
        {
            DS4WinWPF.AppHost.CreateHost();

            // 第4層 4-c 設定・永続化・環境・通知サービス
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IProfileSettingsService>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IProfileRepository>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<ISpecialActionRepository>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IPathService>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IEnvironmentService>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<INotificationService>());

            // 第1層 入力監視層・デバイス状態管理サービス
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IDeviceStateService>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IDs4DeviceRegistry>());

            // 第3層 信号出力層（仮想コントローラー出力スロット・プロセス起動）
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IOutputSlotService>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IElevatedProcessLauncher>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IProcessInspector>());
        }

        [Fact]
        public void AppHost_Singletons_ShouldReturnSameInstance()
        {
            DS4WinWPF.AppHost.CreateHost();

            var instance1 = DS4WinWPF.AppHost.GetService<IProfileSettingsService>();
            var instance2 = DS4WinWPF.AppHost.GetService<IProfileSettingsService>();
            Assert.Same(instance1, instance2);

            var path1 = DS4WinWPF.AppHost.GetService<IPathService>();
            var path2 = DS4WinWPF.AppHost.GetService<IPathService>();
            Assert.Same(path1, path2);
        }
    }
}
