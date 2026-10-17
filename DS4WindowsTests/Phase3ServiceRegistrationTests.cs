﻿using Xunit;
using DS4Windows.Services;
using DS4Windows;
using DS4WinWPF;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase3-Step3-5／Phase3-Step3-6でAppHost(正式DIルート)に登録したサービスが
    /// 実際に解決可能であることを検証する回帰テスト。
    ///
    /// 背景: Phase3-Step3-5着手時の実コード確認で、Phase3-Followup完了報告書が
    /// 「IDeviceStateAccessorのDI登録・配線を完了した」としていたにもかかわらず、
    /// 実際にはServiceRegistration.csに登録されておらず、常にフォールバック経路
    /// （Program.rootHub直接参照）のみが動作していた、という不具合が発覚した
    /// （詳細: Phase3-Step3-5-IElevatedProcessLauncher-Design.md §0.5、
    /// Phase3-Step3-6-Completion-Report.md §4）。
    ///
    /// このテストは「新設インターフェースがAppHostから実際に解決できること」を
    /// 機械的に検証することで、同種の「登録し忘れたままドキュメントだけ完了扱いになる」
    /// 不具合の再発を防止する。
    /// </summary>
    public class Phase3ServiceRegistrationTests
    {
        [Fact]
        public void AppHost_ResolvesIElevatedProcessLauncher_AsDefaultImplementation()
        {
            DS4WinWPF.AppHost.CreateHost();

            var launcher = DS4WinWPF.AppHost.GetService<IElevatedProcessLauncher>();

            Assert.NotNull(launcher);
            Assert.IsType<DefaultElevatedProcessLauncher>(launcher);
        }

        [Fact]
        public void AppHost_ResolvesIProcessInspector_AsDefaultImplementation()
        {
            DS4WinWPF.AppHost.CreateHost();

            var inspector = DS4WinWPF.AppHost.GetService<IProcessInspector>();

            Assert.NotNull(inspector);
            Assert.IsType<DefaultProcessInspector>(inspector);
        }

        [Fact]
        public void AppHost_ResolvesIDs4DeviceRegistry_AsAdapterImplementation()
        {
            DS4WinWPF.AppHost.CreateHost();

            var registry = DS4WinWPF.AppHost.GetService<IDs4DeviceRegistry>();

            Assert.NotNull(registry);
            Assert.IsType<Ds4DeviceRegistryAdapter>(registry);
        }

        [Fact]
        public void AppHost_ResolvesIDeviceStateAccessor_FromControlServiceSingleton()
        {
            DS4WinWPF.AppHost.CreateHost();

            IDeviceStateAccessor accessor = null;
            var ex = Record.Exception(() => accessor = DS4WinWPF.AppHost.GetService<IDeviceStateAccessor>());
            var controlService = DS4WinWPF.AppHost.GetService<ControlService>();

            Assert.Null(ex);
            Assert.NotNull(accessor);
            Assert.Same(controlService, accessor);
        }
    }
}
