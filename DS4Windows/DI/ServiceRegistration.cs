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
            services.AddSingleton<IProfileSettingsService, ProfileSettingsService>();
            services.AddSingleton<IProfileRepository, ProfileRepository>();
            services.AddSingleton<ISpecialActionRepository, SpecialActionRepository>();
            services.AddSingleton<IDeviceStateService, DeviceStateService>();
            services.AddSingleton<IOutputSlotService, OutputSlotService>();

            // Phase 2: Virtual KBM Output
            services.AddSingleton<IVirtualKBM, OutputKBMHandlerAdapter>();

                        // Phase 3 Step 3-3: DS4 Device Registry
            services.AddSingleton<IDs4DeviceRegistry, Ds4DeviceRegistryAdapter>();

            // Phase 3 Step 3-5: Elevated Process Launcher
            services.AddSingleton<IElevatedProcessLauncher, DefaultElevatedProcessLauncher>();

            // Phase 3 Step 3-6-A: Device State Accessor (ControlService/Program.rootHub への委譲)
            // 注意: ControlServiceはDIコンテナ管理下ではなく、App.xaml.cs の CreateControlService() で
            // 手動生成され Program.rootHub に保持される。DIコンテナが独自に新しい ControlService を
            // 生成しないよう、必ず Program.rootHub を指すファクトリ委譲で登録すること。
            services.AddSingleton<IDeviceStateAccessor>(sp => (IDeviceStateAccessor)DS4Windows.Program.rootHub);

            // Phase 3 Step 3-6-B: Process Inspector (multi-launch check)
            services.AddSingleton<IProcessInspector, DefaultProcessInspector>();

            return services;
        }
    }
}
