using System;
using Microsoft.Extensions.DependencyInjection;
using DS4Windows.Actions;
using DS4Windows.Services;
using DS4WinWPF;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase 3 / Phase 4 / Phase 5: アプリケーション全体のDIコンテナ初期登録を一元管理する。
    /// </summary>
    public static class ServiceRegistration
    {
        public static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            // === 第4層: 4-c DIサービス群 (Singleton) ===
            services.AddSingleton<IPathService, PathService>();
            services.AddSingleton<IDeviceStateService, DeviceStateService>();
            services.AddSingleton<IEnvironmentService, EnvironmentService>();
            services.AddSingleton<INotificationService, AppNotificationService>();
            services.AddSingleton<IOutputSlotService, OutputSlotService>();
            services.AddSingleton<IProfileSettingsService, ProfileSettingsService>();
            services.AddSingleton<IProfileXmlStore, ProfileXmlStore>();
            services.AddSingleton<IProfileRepository, ProfileRepository>();
            services.AddSingleton<ISpecialActionRepository, SpecialActionRepository>();

            // Phase 3 Step 3-6: プロセス検査・昇格起動サービスの登録
            services.AddSingleton<IProcessInspector, DefaultProcessInspector>();
            services.AddSingleton<IElevatedProcessLauncher, DefaultElevatedProcessLauncher>();

            // Phase 5 Step 3: プロファイル適用サービス
            services.AddSingleton<IProfileApplicationService, ProfileApplicationService>();

            // Phase 5 Step 5: 自動プロファイル設定コレクション・実行サービス
            services.AddSingleton<AutoProfileHolder>();
            services.AddSingleton<IAutoProfileService, AutoProfileService>();

            // === 第3層: Actions基盤サービス ===
            services.AddSingleton<IActionFactory, DefaultActionFactory>();
            services.AddSingleton<IKeyActionCreator, DefaultKeyActionCreator>();
            services.AddSingleton<IKeyButtonActionControllerFactory, DefaultKeyButtonActionControllerFactory>();
            services.AddSingleton<IRepeater, RepeatHelperToIRepeaterAdapter>();
            services.AddSingleton<IProcessLauncher, DefaultProcessLauncher>();
            services.AddSingleton<IProfileSwitcher, DefaultProfileSwitcher>();
            services.AddSingleton<IVirtualKBM, OutputKBMHandlerAdapter>();
            services.AddSingleton<IMacroPlayer, DefaultMacroPlayer>();

            // Phase 4: UI層 ViewModel ファクトリの登録
            services.AddSingleton<IViewModelFactory, ViewModelFactory>();

            // === 既存Singletonインスタンスの取得登録 ===
            services.AddSingleton<ControlService>(sp => Program.rootHub);
            services.AddSingleton<IDeviceStateAccessor>(sp => Program.rootHub);
            services.AddSingleton<IDs4DeviceRegistry>(sp => new Ds4DeviceRegistryAdapter());

            return services;
        }
    }
}
