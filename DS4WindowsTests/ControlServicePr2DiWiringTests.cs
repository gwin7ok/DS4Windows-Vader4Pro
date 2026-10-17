using System;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step2-2 (PR-2): ControlService のコンストラクタ注入拡張（IOutputSlotService の遅延解決〔決定D1〕、
    /// IProfileRepository / IDeviceStateService / IProfileXmlStore / IProfileSlotApplier〔決定O1=B・O4=B-2〕）が
    /// Composition Root から正しく配線されていることと、必須引数が null で受け付けられないことを検証する。
    /// ControlService の生成は <see cref="ControlServiceTestFactory"/> を用いる。
    /// </summary>
    public class ControlServicePr2DiWiringTests
    {
        [Fact]
        public void AppHost_ResolvesProfileSlotApplier_AsDefaultImplementation()
        {
            DS4WinWPF.AppHost.CreateHost();

            Assert.IsType<ProfileSlotApplier>(DS4WinWPF.AppHost.GetService<IProfileSlotApplier>());
        }

        [Fact]
        public void AppHost_ResolvesControlService_WithPr2Dependencies()
        {
            DS4WinWPF.AppHost.CreateHost();

            ControlService controlService = null;
            var ex = Record.Exception(() => controlService = DS4WinWPF.AppHost.GetService<ControlService>());

            Assert.Null(ex);
            Assert.NotNull(controlService);
        }

        [Fact]
        public void Constructor_DoesNotInvokeOutputSlotServiceFactory()
        {
            // 決定D1: OutputSlotService → ControlService の逆依存があるため、
            // コンストラクタ内で IOutputSlotService を解決してはならない（生成中の解決は二重実体化の恐れがある）。
            int factoryCalls = 0;

            var controlService = ControlServiceTestFactory.Create(outputSlotServiceFactory: () =>
            {
                factoryCalls++;
                return DS4WinWPF.AppHost.GetService<IOutputSlotService>();
            });

            Assert.NotNull(controlService);
            Assert.Equal(0, factoryCalls);
        }

        [Theory]
        [InlineData("outputSlotServiceFactory")]
        [InlineData("profileRepository")]
        [InlineData("deviceStateService")]
        [InlineData("profileXmlStore")]
        [InlineData("profileSlotApplier")]
        public void Constructor_NullRequiredParameter_ThrowsArgumentNullException(string parameterName)
        {
            var ex = Assert.Throws<ArgumentNullException>(() => ControlServiceTestFactory.Create(parameterName));

            Assert.Equal(parameterName, ex.ParamName);
        }
    }
}