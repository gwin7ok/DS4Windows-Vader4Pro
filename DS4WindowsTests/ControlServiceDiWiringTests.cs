using System;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step2-1 (PR-1): ControlService のコンストラクタ注入拡張（IAppSettingsService /
    /// IEnvironmentService / IPathService）が ServiceRegistration の Composition Root から
    /// 正しく配線されていること、および必須引数（Pure DI、決定D2）が null で受け付けられないことを検証する。
    /// ControlService の生成は <see cref="ControlServiceTestFactory"/> を用いる。
    /// </summary>
    public class ControlServiceDiWiringTests
    {
        [Fact]
        public void AppHost_ResolvesControlService_WithoutCircularDependencyOrMissingRegistration()
        {
            DS4WinWPF.AppHost.CreateHost();

            ControlService controlService = null;
            var ex = Record.Exception(() => controlService = DS4WinWPF.AppHost.GetService<ControlService>());

            Assert.Null(ex);
            Assert.NotNull(controlService);
        }

        [Fact]
        public void AppHost_ResolvedControlService_ReceivesDeviceOptionsThroughAppSettingsService()
        {
            DS4WinWPF.AppHost.CreateHost();

            var controlService = DS4WinWPF.AppHost.GetService<ControlService>();

            // コンストラクタで _appSettings.DeviceOptions（= Global.DeviceOptions）を受け取っていること。
            Assert.Same(Global.DeviceOptions, controlService.DeviceOptions);
        }

        [Fact]
        public void AppHost_ResolvesPr1Services_AsExpectedImplementations()
        {
            DS4WinWPF.AppHost.CreateHost();

            Assert.IsType<AppSettingsService>(DS4WinWPF.AppHost.GetService<IAppSettingsService>());
            Assert.IsType<EnvironmentService>(DS4WinWPF.AppHost.GetService<IEnvironmentService>());
            Assert.IsType<PathService>(DS4WinWPF.AppHost.GetService<IPathService>());
        }

        [Fact]
        public void Constructor_NullAppSettings_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => ControlServiceTestFactory.Create("appSettings"));

            Assert.Equal("appSettings", ex.ParamName);
        }

        [Fact]
        public void Constructor_NullEnvironmentService_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => ControlServiceTestFactory.Create("environmentService"));

            Assert.Equal("environmentService", ex.ParamName);
        }

        [Fact]
        public void Constructor_NullPathService_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => ControlServiceTestFactory.Create("pathService"));

            Assert.Equal("pathService", ex.ParamName);
        }
    }
}