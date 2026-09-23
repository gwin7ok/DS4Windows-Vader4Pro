using System;
using System.Reflection;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step3-2: ControlService のコンストラクタ注入拡張（ISpecialActionRepository）が
    /// Composition Root から正しく配線されていることと、必須引数が null で受け付けられないことを検証する。
    /// あわせて、Mapping.cs への引数渡し（S3方針）用に新設した読み取り専用プロパティ（ProfileActionProvider、
    /// ProfileXmlStore、DisplayCoordinateService、ProfileRepository、SpecialActionRepository、
    /// いずれも internal）が、コンストラクタで受け取った実体をそのまま返すことを検証する。
    /// internal メンバーのため、本プロジェクトの既存の慣例（<see cref="MappingSpecialActionSuppressionTests"/> 等）
    /// と同様にリフレクション経由でアクセスする。
    /// パターンは ControlServicePr5DiWiringTests（Phase6-Step2-5）に倣う。
    /// </summary>
    public class ControlServiceStep3Step2DiWiringTests
    {
        [Fact]
        public void AppHost_ResolvesControlService_WithSpecialActionRepository()
        {
            DS4WinWPF.AppHost.CreateHost();

            ControlService controlService = null;
            var ex = Record.Exception(() => controlService = DS4WinWPF.AppHost.GetService<ControlService>());

            Assert.Null(ex);
            Assert.NotNull(controlService);
        }

        [Fact]
        public void AppHost_ResolvesISpecialActionRepository()
        {
            DS4WinWPF.AppHost.CreateHost();

            Assert.NotNull(DS4WinWPF.AppHost.GetService<ISpecialActionRepository>());
        }

        [Fact]
        public void Constructor_NullSpecialActionRepository_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => ControlServiceTestFactory.Create("specialActionRepository"));

            Assert.Equal("specialActionRepository", ex.ParamName);
        }

        private static object GetInternalProperty(ControlService controlService, string propertyName)
        {
            PropertyInfo prop = typeof(ControlService).GetProperty(
                propertyName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(prop); // プロパティ名変更等でシグネチャが失われた場合に検出する
            return prop.GetValue(controlService);
        }

        [Theory]
        [InlineData("ProfileActionProvider", typeof(IProfileActionProvider))]
        [InlineData("ProfileXmlStore", typeof(IProfileXmlStore))]
        [InlineData("DisplayCoordinateService", typeof(IDisplayCoordinateService))]
        [InlineData("ProfileRepository", typeof(IProfileRepository))]
        [InlineData("SpecialActionRepository", typeof(ISpecialActionRepository))]
        public void InternalAccessor_ReturnsSameInstanceAsAppHost(string propertyName, Type serviceType)
        {
            DS4WinWPF.AppHost.CreateHost();
            var controlService = DS4WinWPF.AppHost.GetService<ControlService>();

            var expected = DS4WinWPF.AppHost.GetService(serviceType);
            var actual = GetInternalProperty(controlService, propertyName);

            Assert.NotNull(expected);
            Assert.Same(expected, actual);
        }
    }
}