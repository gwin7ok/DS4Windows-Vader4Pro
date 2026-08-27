using System;
using DS4Windows.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DS4Windows
{
    public static class AppHost
    {
        private static IHost _host;

        public static IHost Host => _host;
        public static IServiceProvider Services => _host?.Services;

        public static void Initialize()
        {
            _host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services);
                })
                .Build();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Phase 0: Services
            services.AddSingleton<IProfileSettingsService, DummyProfileSettingsService>();

            // Phase 1: Action executors
            services.AddTransient<IKeyOutputAction, KeyOutputActionAdapter>();
            services.AddTransient<IMacroPlayer, DefaultMacroPlayer>();
            services.AddTransient<IProfileSwitcher, DefaultProfileSwitcher>();
            services.AddTransient<IProcessLauncher, DefaultProcessLauncher>();

            // Phase 2: Virtual KBM Output
            services.AddSingleton<IVirtualKBM, OutputKBMHandlerAdapter>();

            // ViewModels, etc.
        }

        public static T GetService<T>() where T : class
        {
            return Services?.GetService<T>();
        }

        public static T GetRequiredService<T>() where T : class
        {
            return Services?.GetRequiredService<T>();
        }
    }
}
