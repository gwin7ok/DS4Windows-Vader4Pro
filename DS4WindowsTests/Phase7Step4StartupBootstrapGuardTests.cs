using System.IO;
using System.Text.RegularExpressions;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step4 W4（D3＝S1）: 起動前（Pre-Host）のブートストラップと、<c>-reenableDevice</c> の接続（Q1・Q4）の回帰ガード。
    /// - コンテナの構築より前に作った <see cref="PathService"/>／<see cref="EnvironmentService"/> を、<c>CreateHost</c> が同じインスタンスとして登録する。
    /// - App の Pre-Host は、ブートストラップのインスタンスを使う（個別の <c>new</c> や <c>Global</c> 経由の保存場所の参照をしない）。
    /// - <c>-reenableDevice</c> 分岐は、<c>DS4Devices</c> を静的に呼ばず、<see cref="IDs4DeviceRegistry"/> を通す。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。判断の根拠は <c>Phase7-Step4-Plan.md</c>。
    /// </summary>
    public class Phase7Step4StartupBootstrapGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step4StartupBootstrapGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static string StripComments(string text)
        {
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        private string ReadApp()
        {
            string path = SourceFileLocator.Find("DS4Windows/App.xaml.cs");
            if (path == null)
            {
                _output.WriteLine("App.xaml.cs が見つからないため検査を省略した。");
                return null;
            }

            return StripComments(File.ReadAllText(path));
        }

        [Fact]
        public void LaterRegistration_ReplacesTheDefault_SoTheBootstrapInstancesAreResolved()
        {
            // AppHost.CreateHost(config, parser, pathService, environmentService) が使う登録の仕方（既定の登録の後に、同じ型を登録する）を、
            // 共有の静的なホストを触らずに確かめる。
            var services = new ServiceCollection();
            services.RegisterServices();
            var path = new PathService();
            var environment = new EnvironmentService();
            services.AddSingleton<IPathService>(path);
            services.AddSingleton<IEnvironmentService>(environment);

            using (ServiceProvider provider = services.BuildServiceProvider())
            {
                Assert.Same(path, provider.GetRequiredService<IPathService>());
                Assert.Same(environment, provider.GetRequiredService<IEnvironmentService>());
            }
        }

        [Fact]
        public void AppHost_RegistersTheBootstrapInstancesAfterTheDefaults()
        {
            string path = SourceFileLocator.Find("DS4Windows/DI/AppHost.cs");
            if (path == null) return;

            string text = StripComments(File.ReadAllText(path));
            int defaults = text.IndexOf("RegisterServices(services);\r\n                        services.AddSingleton(parser);", System.StringComparison.Ordinal);
            if (defaults < 0)
                defaults = text.IndexOf("RegisterServices(services);\n                        services.AddSingleton(parser);", System.StringComparison.Ordinal);
            int bootstrap = text.IndexOf("AddSingleton<DS4Windows.DI.IPathService>(bootstrapPathService)", System.StringComparison.Ordinal);
            int bootstrapEnv = text.IndexOf("AddSingleton<DS4Windows.DI.IEnvironmentService>(bootstrapEnvironmentService)", System.StringComparison.Ordinal);

            Assert.True(defaults >= 0, "既定の登録が見つからない。");
            Assert.True(bootstrap > defaults && bootstrapEnv > defaults, "ブートストラップのインスタンスの登録が、既定の登録より前にある（既定が優先されてしまう）。");
        }

        [Fact]
        public void App_UsesTheBootstrapInstancesInPreHost()
        {
            string text = ReadApp();
            if (text == null) return;

            // ブートストラップは、Application_Startup の最初（FindConfigLocation より前）に作る
            int create = text.IndexOf("_bootstrapPathService = new DS4Windows.PathService();", System.StringComparison.Ordinal);
            int createEnv = text.IndexOf("_bootstrapEnvironmentService = new DS4Windows.EnvironmentService();", System.StringComparison.Ordinal);
            int find = text.IndexOf("_bootstrapPathService.FindConfigLocation();", System.StringComparison.Ordinal);
            Assert.True(create >= 0 && createEnv >= 0 && find > create && find > createEnv, "ブートストラップの生成が、使用より前にない。");

            // 個別の new をしない（生成はブートストラップの 2 箇所だけ）
            Assert.Single(Regex.Matches(text, @"new\s+DS4Windows\.PathService\(\)"));
            Assert.Single(Regex.Matches(text, @"new\s+DS4Windows\.EnvironmentService\(\)"));

            // 2 箇所のホスト構築（通常の起動と -driverinstall）が、ブートストラップのインスタンスを渡す
            Assert.Equal(2, Regex.Matches(text, @"_bootstrapPathService,\s*_bootstrapEnvironmentService\)").Count);

            // 保存場所は Global 経由ではなく PathService の状態を読む
            Assert.DoesNotContain("Global.appdatapath", text);
            Assert.DoesNotContain("Global.FindConfigLocation", text);
            Assert.DoesNotContain("Global.RefreshViGEmBusInfo", text);
        }

        [Fact]
        public void ReenableDeviceBranch_GoesThroughTheRegistryContract()
        {
            string text = ReadApp();
            if (text == null) return;

            Assert.DoesNotContain("DS4Devices.reEnableDevice", text);
            Assert.Contains("new DS4Windows.Services.Ds4DeviceRegistryAdapter()", text);
            Assert.Contains(".ReEnableDevice(parser.DeviceInstanceId)", text);
        }
    }
}
