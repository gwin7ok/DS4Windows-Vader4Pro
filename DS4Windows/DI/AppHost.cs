using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DS4Windows.DI
{
    public static class AppHost
    {
        public static IHost CreateHost(IConfiguration configuration)
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((ctx, cfg) => {
                    // cfg.AddConfiguration(configuration);
                })
                .ConfigureServices((ctx, services) => {
                    ServiceRegistration.ConfigureServices(services, ctx.Configuration);
                })
                .ConfigureLogging(logging => {
                    // ログ設定はここで行う
                });

            return builder.Build();
        }

        public static async Task StartAsync(IHost host)
        {
            await host.StartAsync();
        }

        public static async Task StopAsync(IHost host)
        {
            await host.StopAsync();
            host.Dispose();
        }
    }
}
