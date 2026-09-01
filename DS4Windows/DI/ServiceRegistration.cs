using System;
using Microsoft.Extensions.DependencyInjection;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using DS4Windows.Actions;
using DS4WinWPF;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4Windows.DI
{
    public static class ServiceRegistration
    {
        public static void RegisterServices(IServiceCollection services)
        {
            // 第4層 4-c 設定・プロファイル・アクション・環境・通知サービス
            services.AddSingleton<IProfileSettingsService, ProfileSettingsService>();
            services.AddSingleton<IProfileRepository, ProfileRepository>();
            services.AddSingleton<IProfileSwitcher, DefaultProfileSwitcher>();
            services.AddSingleton<ISpecialActionRepository, SpecialActionRepository>();
            services.AddSingleton<IPathService, PathService>();
            services.AddSingleton<IEnvironmentService, EnvironmentService>();
            services.AddSingleton<INotificationService, AppNotificationService>();

            // 第1層 入力監視層・デバイス状態管理サービス
            services.AddSingleton<IDeviceStateService, DeviceStateService>();
            services.AddSingleton<IDs4DeviceRegistry, Ds4DeviceRegistryAdapter>();

            // 第3層 信号出力層（仮想コントローラー出力スロット・プロセス起動）
            services.AddSingleton<IOutputSlotService, OutputSlotService>();
            services.AddSingleton<IElevatedProcessLauncher, DefaultElevatedProcessLauncher>();
            services.AddSingleton<IProcessInspector, DefaultProcessInspector>();

            // 第4層 4-c ViewModel Factory (Pattern C: 実行時引数付き ViewModel 生成)
            services.AddSingleton<IViewModelFactory, ViewModelFactory>();

            // 第4層 4-b ViewModel (Pattern A: 引数なし ViewModel)
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<LogViewModel>();
            services.AddTransient<AboutViewModel>();

            // 第4層 4-b ViewModel (Pattern B: 共有依存 ViewModel)
            services.AddSingleton<ControllersViewModel>();
            services.AddSingleton<MainWindowsViewModel>();
        }
    }
}
