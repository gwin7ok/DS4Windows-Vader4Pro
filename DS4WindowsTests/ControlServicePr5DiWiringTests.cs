using System;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step2-5 (PR-5): ControlService のコンストラクタ注入拡張（IAppearanceSettingsService、IProfileActionProvider）が
    /// Composition Root から正しく配線されていることと、必須引数が null で受け付けられないことを検証する。
    /// </summary>
    public class ControlServicePr5DiWiringTests
    {
        [Fact]
        public void AppHost_ResolvesControlService_WithPr5Dependencies()
        {
            DS4WinWPF.AppHost.CreateHost();

            ControlService controlService = null;
            var ex = Record.Exception(() => controlService = DS4WinWPF.AppHost.GetService<ControlService>());

            Assert.Null(ex);
            Assert.NotNull(controlService);
        }

        [Fact]
        public void AppHost_ResolvesPr5Services()
        {
            DS4WinWPF.AppHost.CreateHost();

            Assert.NotNull(DS4WinWPF.AppHost.GetService<IAppearanceSettingsService>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IProfileActionProvider>());
        }

        [Theory]
        [InlineData("appearanceSettings")]
        [InlineData("profileActionProvider")]
        public void Constructor_NullRequiredParameter_ThrowsArgumentNullException(string parameterName)
        {
            var ex = Assert.Throws<ArgumentNullException>(() => ControlServiceTestFactory.Create(parameterName));

            Assert.Equal(parameterName, ex.ParamName);
        }
    }
}