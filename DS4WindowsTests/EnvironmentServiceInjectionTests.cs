using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step2-1c (決定O2=C/O3=A): ProfileSettingsService / AutoProfileService が
    /// スロット上限を IEnvironmentService から受け取れること（コンストラクタ注入）と、
    /// 省略時（既存の生成箇所）も従来どおり生成できることを検証する。
    /// 上限を使うサブ設定の配線・自動プロファイルのループ挙動そのものは、
    /// ProfileSettingsServiceSubSettingsTests / AutoProfileServiceTests が既定実装経由で引き続き検証する。
    /// </summary>
    public class EnvironmentServiceInjectionTests
    {
        [Fact]
        public void ProfileSettingsService_AcceptsInjectedEnvironmentService()
        {
            var service = new ProfileSettingsService(null, new EnvironmentService());

            Assert.NotNull(service);
        }

        [Fact]
        public void ProfileSettingsService_DefaultConstruction_StillWorks()
        {
            var service = new ProfileSettingsService();

            Assert.NotNull(service);
        }

        [Fact]
        public void AppHost_ResolvesProfileSettingsAndAutoProfileServices_WithEnvironmentService()
        {
            DS4WinWPF.AppHost.CreateHost();

            Assert.NotNull(DS4WinWPF.AppHost.GetService<IProfileSettingsService>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IAutoProfileService>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IEnvironmentService>());
        }
    }
}