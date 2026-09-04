using System;
using Microsoft.Extensions.DependencyInjection;
using DS4Windows.Actions;
using DS4Windows.Services;
using DS4WinWPF;
using DS4WinWPF.DS4Forms.ViewModels;

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

            // Phase 5 Step 12: 出力スロット永続化・管理サービス
            services.AddSingleton<IOutputSlotStore, OutputSlotStore>();
            services.AddSingleton<IOutputSlotService, OutputSlotService>();

            services.AddSingleton<IProfileSettingsService, ProfileSettingsService>();
            services.AddSingleton<IProfileXmlStore, ProfileXmlStore>();
            services.AddSingleton<IProfileRepository, ProfileRepository>();
            services.AddSingleton<ISpecialActionRepository, SpecialActionRepository>();

            // Phase 3 Step 3-6: プロセス検査・昇格起動サービスの登録
            services.AddSingleton<IProcessInspector, DefaultProcessInspector>();
            services.AddSingleton<IElevatedProcessLauncher, DefaultElevatedProcessLauncher>();

            // アクション発火ディスパッチャー（Mapping.cs境界化）
            services.AddSingleton<IMappingActionDispatcher, MappingActionDispatcher>();

            // プロファイルアクション連鎖サービス（ProfileApplicationServiceの依存先）
            services.AddSingleton<IProfileActionProvider, ProfileActionProvider>();
            services.AddSingleton<IProfileActionChainService, ProfileActionChainService>();

            // Phase 5 Step 3: プロファイル適用サービス
            services.AddSingleton<IProfileApplicationService, ProfileApplicationService>();

            // Phase 5 Step 5: 自動プロファイル設定コレクション・実行サービス
            services.AddSingleton<AutoProfileHolder>();
            services.AddSingleton<IAutoProfileService, AutoProfileService>();

            // Phase 5 Step 6: アプリ全体設定サービス
            services.AddSingleton<IAppSettingsService, AppSettingsService>();

            // Phase 5 Step 10: UDP サーバーサービス（Cemuhook モーションサーバー境界化）
            services.AddSingleton<IUdpServerService, UdpServerService>();

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

            // Phase 4: Pattern A ViewModel (Transient)
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<LogViewModel>();
            services.AddTransient<AboutViewModel>();

            // Phase 4: Pattern B ViewModel (Singleton)
            services.AddSingleton<ControllersViewModel>();
            services.AddSingleton<MainWindowsViewModel>();

            // === 既存Singletonインスタンスの取得登録 ===
            services.AddSingleton<IDs4DeviceRegistry>(sp => new Ds4DeviceRegistryAdapter());
            services.AddSingleton<ControlService>(sp =>
            {
                return Program.rootHub ?? new ControlService(
                    new ArgumentParser(),
                    sp.GetRequiredService<IDs4DeviceRegistry>(),
                    sp.GetRequiredService<IProfileSettingsService>()
                );
            });
            services.AddSingleton<IDeviceStateAccessor>(sp => sp.GetRequiredService<ControlService>());

            return services;
        }
    }
}
