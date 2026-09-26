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
            services.AddSingleton<IAppearanceSettingsService, AppearanceSettingsService>();
            services.AddSingleton<INotificationService, AppNotificationService>();

            // Phase6-Step7-4（決定7＝案H）: 起動引数の保持役。値は AppHost.CreateHost(config, parser) が設定する。
            services.AddSingleton<IStartupArguments, StartupArguments>();

            // Phase 5 Step 12: 出力スロット永続化・管理サービス
            services.AddSingleton<IOutputSlotStore, OutputSlotStore>();
            services.AddSingleton<IOutputSlotService, OutputSlotService>();

            services.AddSingleton<IProfileSettingsService, ProfileSettingsService>();
            services.AddSingleton<IProfileXmlStore, ProfileXmlStore>();
            services.AddSingleton<IProfileRepository, ProfileRepository>();
            services.AddSingleton<IProfileSlotApplier, ProfileSlotApplier>();
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
            // 修正前:
            // services.AddSingleton<IAppSettingsService, AppSettingsService>();

            // 修正後（完全修飾名で確実にバインド）:
            services.AddSingleton<IAppSettingsService, DS4Windows.Services.AppSettingsService>();

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
            services.AddSingleton<IVirtualKBMLifecycle, OutputKBMHandlerLifecycle>();
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
                    // Phase6-Step7-4（決定5＝A、決定7＝案H）: AppHost.CreateHost(config, parser) が IStartupArguments に
                    // 設定した実際の起動引数を渡す。2026-09-04（4c89cd91）以降は常に空の new ArgumentParser() を渡していたため、
                    // 起動引数 -virtualkbm（キーボード・マウス出力方式の指定）が無視されていた。
                    // ホストは Global の静的初期化で起動引数なしに先に作られるため、コンテナへの登録（AddSingleton(parser)）
                    // ではなく、登録済みの保持役を経由する（Phase6-Step7-Plan.md §0.4 の訂正・決定7）。
                    // 起動引数が設定されていない場合（引数なしの CreateHost()、テスト）は、従来どおり空のパーサーを使う。
                    // 注意: ControlService はこの時点の値を保持するため、CreateHost(config, parser) より前に解決してはならない
                    // （現在は App.CreateControlService で、明示的なホスト構築の後に解決している）。
                    sp.GetRequiredService<IStartupArguments>().Parser ?? new ArgumentParser(),
                    sp.GetRequiredService<IDs4DeviceRegistry>(),
                    sp.GetRequiredService<IProfileSettingsService>(),
                    sp.GetRequiredService<IAppSettingsService>(),
                    sp.GetRequiredService<IEnvironmentService>(),
                    sp.GetRequiredService<IPathService>(),
                    () => sp.GetRequiredService<IOutputSlotService>(), // 決定D1: 遅延解決（OutputSlotService → ControlService の逆依存を回避）
                    sp.GetRequiredService<IProfileRepository>(),
                    sp.GetRequiredService<IDeviceStateService>(),
                    sp.GetRequiredService<IProfileXmlStore>(),
                    sp.GetRequiredService<IProfileSlotApplier>(),
                    sp.GetRequiredService<IVirtualKBM>(),
                    sp.GetRequiredService<IVirtualKBMLifecycle>(),
                    sp.GetRequiredService<IAppearanceSettingsService>(),
                    sp.GetRequiredService<IProfileActionProvider>(),
                    sp.GetRequiredService<ISpecialActionRepository>()
                );
            });
            services.AddSingleton<IDeviceStateAccessor>(sp => sp.GetRequiredService<ControlService>());

            return services;
        }
    }
}