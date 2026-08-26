using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DS4Windows.Actions;

namespace DS4Windows.DI
{
    public static class AppHost
    {
        private static IHost _host;

        public static IServiceProvider Services => _host?.Services;

        /// <summary>
        /// App.xaml.cs から IConfigurationRoot を受け取ってホストを初期化するエントリーポイント
        /// </summary>
        public static IHost CreateHost(IConfigurationRoot configuration)
        {
            Initialize(configuration);
            return _host;
        }

        public static IHost CreateHost(string[] args = null)
        {
            Initialize(args: args);
            return _host;
        }

        public static void Initialize(IConfigurationRoot configuration = null, string[] args = null)
        {
            if (_host != null) return;

            var builder = Host.CreateDefaultBuilder(args ?? Array.Empty<string>());

            if (configuration != null)
            {
                builder.ConfigureAppConfiguration((context, configBuilder) =>
                {
                    configBuilder.AddConfiguration(configuration);
                });
            }

            builder.ConfigureServices((context, services) =>
            {
                // Actions サブシステムのサービス登録
                services.AddSingleton<IProcessLauncher, DefaultProcessLauncher>();
                services.AddSingleton<IMacroPlayer, DefaultMacroPlayer>();
                services.AddSingleton<IProfileSwitcher, DefaultProfileSwitcher>();
                services.AddSingleton<IActionFactory, DefaultActionFactory>();
            });

            _host = builder.Build();

            // Bridge to ServiceProviderHolder for legacy/hybrid access
            ServiceProviderHolder.SetProvider(_host.Services);
        }

        public static void Shutdown()
        {
            try
            {
                _host?.Dispose();
                _host = null;
                ServiceProviderHolder.SetProvider(null);
            }
            catch { }
        }
    }
}