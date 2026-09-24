using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.DS4Control;
using DS4WinWPF;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step7-4（決定5＝A）: ServiceRegistration が ControlService を生成するとき、DI コンテナに登録された
    /// 実際の起動引数（<see cref="ArgumentParser"/>）を渡すことを検証する。
    /// 2026-09-04（4c89cd91）以降は常に空の new ArgumentParser() を渡していたため、起動引数 -virtualkbm が無視されていた
    /// （`Phase6-Step7-Plan.md` §0.6）。
    /// ServiceRegistration の登録は、Program.rootHub が設定済みならそれを返すため、各テストでは一時的に null にし、
    /// 終了時に元へ戻す（Phase6-Step2 の教訓: static 状態を必ず復元する）。
    /// </summary>
    public class ServiceRegistrationArgumentParserTests
    {
        private static ArgumentParser GetCmdParser(ControlService controlService)
        {
            FieldInfo field = typeof(ControlService).GetField("cmdParser", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            return (ArgumentParser)field.GetValue(controlService);
        }

        [Fact]
        public void ControlService_ReceivesRegisteredArgumentParser()
        {
            var parser = new ArgumentParser();
            parser.Parse(new[] { "-virtualkbm", "sendinput" });
            Assert.Equal("sendinput", parser.VirtualkbmHandler);

            var services = new ServiceCollection();
            services.RegisterServices();
            services.AddSingleton(parser); // AppHost.CreateHost(config, parser) と同じ登録

            ControlService savedRootHub = Program.rootHub;
            Program.rootHub = null;
            try
            {
                ServiceProvider provider = services.BuildServiceProvider();
                ControlService controlService = provider.GetRequiredService<ControlService>();

                Assert.Same(parser, GetCmdParser(controlService));
                Assert.Equal("sendinput", GetCmdParser(controlService).VirtualkbmHandler);
            }
            finally
            {
                Program.rootHub = savedRootHub;
            }
        }

        [Fact]
        public void ControlService_WithoutRegisteredArgumentParser_UsesEmptyParser()
        {
            // 引数なしの AppHost.CreateHost() やテストのホストでは、従来どおり空のパーサー（既定の出力方式）で生成できる。
            var services = new ServiceCollection();
            services.RegisterServices();

            ControlService savedRootHub = Program.rootHub;
            Program.rootHub = null;
            try
            {
                ServiceProvider provider = services.BuildServiceProvider();
                ControlService controlService = provider.GetRequiredService<ControlService>();

                ArgumentParser cmdParser = GetCmdParser(controlService);
                Assert.NotNull(cmdParser);
                Assert.Equal(VirtualKBMFactory.DEFAULT_IDENTIFIER, cmdParser.VirtualkbmHandler);
            }
            finally
            {
                Program.rootHub = savedRootHub;
            }
        }
    }
}
