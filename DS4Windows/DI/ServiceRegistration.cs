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

            return services;
        }
    }
}
