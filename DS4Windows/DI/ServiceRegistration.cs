using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DS4WinWPF.DI
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Phase 0: Profile Settings Service
            services.AddSingleton<IProfileSettingsService, ProfileSettingsServicePlaceholder>();

            // Phase 2: Virtual KBM Output
            services.AddSingleton<IVirtualKBM, OutputKBMHandlerAdapter>();

                        // Phase 3 Step 3-3: DS4 Device Registry
            services.AddSingleton<IDs4DeviceRegistry, Ds4DeviceRegistryAdapter>();

            // Phase 3 Step 3-5: Elevated Process Launcher
            services.AddSingleton<IElevatedProcessLauncher, DefaultElevatedProcessLauncher>();

            return services;
        }
    }
}
