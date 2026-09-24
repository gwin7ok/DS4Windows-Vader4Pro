using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.DS4Control;
using DS4Windows.Services;
using DS4WinWPF;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step7-4（決定5＝A、決定7＝案H）: ServiceRegistration が ControlService を生成するとき、
    /// <see cref="IStartupArguments"/> に設定された実際の起動引数（<see cref="ArgumentParser"/>）を渡すこと、
    /// および DS4WinWPF.AppHost.CreateHost(config, parser) が、ホストが既に構築済みの場合も起動引数を設定することを検証する。
    ///
    /// 経緯: 2026-09-04（4c89cd91）以降、ControlService には常に空の new ArgumentParser() が渡され、起動引数 -virtualkbm が
    /// 無視されていた。最初の是正（コンテナに登録した ArgumentParser を使う）は、ホストが Global の静的初期化で起動引数なしに
    /// 先に作られるため実機で効かなかった（`Phase6-Step7-Plan.md` §0.4 の訂正・決定7）。このため、以下のテストは
    /// 「ホストが先に作られている」実際の起動順序を再現するものを含める。
    ///
    /// ServiceRegistration の登録は、Program.rootHub が設定済みならそれを返すため、各テストで一時的に null にし、
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

        private static ArgumentParser CreateVirtualKbmParser()
        {
            var parser = new ArgumentParser();
            parser.Parse(new[] { "-virtualkbm", "sendinput" });
            Assert.Equal("sendinput", parser.VirtualkbmHandler);
            return parser;
        }

        [Fact]
        public void StartupArguments_HoldsTheParserThatWasSet()
        {
            var startupArguments = new StartupArguments();
            Assert.Null(startupArguments.Parser);

            var parser = new ArgumentParser();
            startupArguments.SetParser(parser);
            Assert.Same(parser, startupArguments.Parser);
        }

        [Fact]
        public void ControlService_ReceivesParserSetOnStartupArguments_AfterContainerWasBuilt()
        {
            // 実際の起動順序の再現: コンテナは起動引数なしで先に作られ、その後で起動引数が設定される。
            var services = new ServiceCollection();
            services.RegisterServices();
            ServiceProvider provider = services.BuildServiceProvider();

            var parser = CreateVirtualKbmParser();
            provider.GetRequiredService<IStartupArguments>().SetParser(parser);

            ControlService savedRootHub = Program.rootHub;
            Program.rootHub = null;
            try
            {
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
        public void ControlService_WithoutStartupArguments_UsesEmptyParser()
        {
            // 引数なしの DS4WinWPF.AppHost.CreateHost() やテストのホストでは、従来どおり空のパーサー（既定の出力方式）で生成できる。
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

        [Fact]
        public void AppHostCreateHostWithParser_OnExistingHost_SetsStartupArguments()
        {
            // 実際の起動では、App.xaml.cs の CreateHost(config, parser) の時点でホストは構築済み（既存のホストを返す経路）。
            EnsureHostCreated();
            var startupArguments = DS4WinWPF.AppHost.GetService<IStartupArguments>();
            Assert.NotNull(startupArguments);

            ArgumentParser original = startupArguments.Parser;
            try
            {
                var parser = CreateVirtualKbmParser();
                var host = DS4WinWPF.AppHost.CreateHost(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(), parser);

                Assert.Same(DS4WinWPF.AppHost.Host, host);
                Assert.Same(parser, startupArguments.Parser);
            }
            finally
            {
                startupArguments.SetParser(original);
            }
        }

        private static void EnsureHostCreated()
        {
            // DS4WinWPF.AppHost.GetService はホストが未構築なら引数なしの CreateHost() で構築する（実際の起動と同じ経路）。
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IStartupArguments>());
            Assert.NotNull(DS4WinWPF.AppHost.Host);
        }
    }
}
