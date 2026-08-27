using System;
using DS4WinWPF.DI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DS4WinWPF
{
    public static class AppHost
    {
        private static IHost _host;

        public static IHost Host => _host;
        public static IServiceProvider Services => _host?.Services;

        public static IHost CreateHost(IConfiguration configuration = null)
        {
            _host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddAppServices(configuration ?? context.Configuration);
                })
                .Build();

            return _host;
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
