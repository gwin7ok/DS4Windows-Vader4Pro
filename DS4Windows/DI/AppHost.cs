using System;
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
        /// App.xaml.cs から呼び出されるホスト作成・初期化エントリーポイント（引数対応）
        /// </summary>
        public static IHost CreateHost(string[] args = null)
        {
            Initialize(args);
            return _host;
        }

        public static void Initialize(string[] args = null)
        {
            if (_host != null) return;

            var builder = Host.CreateDefaultBuilder(args ?? Array.Empty<string>());
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