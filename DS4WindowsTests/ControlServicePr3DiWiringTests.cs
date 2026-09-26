using System;
using Xunit;
using DS4Windows;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step2-3 (PR-3): KBM 出力の送出（IVirtualKBM）とハンドラのライフサイクル（IVirtualKBMLifecycle、決定D3）が
    /// Composition Root から ControlService へ正しく配線されていること、および必須引数の null 拒否を検証する。
    /// </summary>
    public class ControlServicePr3DiWiringTests
    {
        [Fact]
        public void AppHost_ResolvesVirtualKBMServices_AsExpectedImplementations()
        {
            DS4WinWPF.AppHost.CreateHost();

            Assert.IsType<OutputKBMHandlerAdapter>(DS4WinWPF.AppHost.GetService<IVirtualKBM>());
            Assert.IsType<OutputKBMHandlerLifecycle>(DS4WinWPF.AppHost.GetService<IVirtualKBMLifecycle>());
        }

        [Fact]
        public void AppHost_ResolvesControlService_WithPr3Dependencies()
        {
            DS4WinWPF.AppHost.CreateHost();

            ControlService controlService = null;
            var ex = Record.Exception(() => controlService = DS4WinWPF.AppHost.GetService<ControlService>());

            Assert.Null(ex);
            Assert.NotNull(controlService);
        }

        [Theory]
        [InlineData("virtualKBM")]
        [InlineData("kbmLifecycle")]
        public void Constructor_NullRequiredParameter_ThrowsArgumentNullException(string parameterName)
        {
            var ex = Assert.Throws<ArgumentNullException>(() => ControlServiceTestFactory.Create(parameterName));

            Assert.Equal(parameterName, ex.ParamName);
        }
    }
}