using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DS4Windows.DI
{
    public static class ServiceRegistration
    {
        public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // 例: サービスの登録箇所
            // services.AddSingleton<IManagedActionManager, DefaultActionManager>();
            // services.AddSingleton<IActionBindingFactory, ActionBindingFactory>();
            // services.AddSingleton<IVirtualKBM, VirtualKBMHandler>();
            // services.AddHostedService<MacroHostedService>();

            // フェーズ0-2: 最初の雛形サービス登録（実装なし、コンパイル通過のみを目標）
            services.AddSingleton<IProfileSettingsService, ProfileSettingsServicePlaceholder>();

            // TODO: 実装時に具体的な型を登録してください。
        }
    }
}
