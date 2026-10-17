using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step9-4: <see cref="ProfileSettingsViewModel"/> のコンストラクタの依存が必須引数になったため、
    /// テストでの生成を 1 か所にまとめる。引数を省略した場合は、従来のコンストラクタのフォールバックと同じ解決をする
    /// （profileSettings は Global.ProfileSettingsServiceInstance、outputSlotService は DI（なければ Global の実体）、
    /// ControlService は null）。引数が増えてもテスト側の修正が不要になる。
    /// </summary>
    internal static class ProfileSettingsViewModelTestFactory
    {
        public static ProfileSettingsViewModel Create(
            int device,
            IProfileSettingsService profileSettings = null,
            IOutputSlotService outputSlotService = null,
            int targetDevice = -1)
        {
            DS4WinWPF.AppHost.CreateHost();

            return new ProfileSettingsViewModel(
                device,
                profileSettings ?? Global.ProfileSettingsServiceInstance,
                null,
                outputSlotService ?? DS4WinWPF.AppHost.GetService<IOutputSlotService>(),
                DS4WinWPF.AppHost.GetService<IProfileRepository>(),
                DS4WinWPF.AppHost.GetService<IProfilePresetLoader>(),
                DS4WinWPF.AppHost.GetService<IEnvironmentService>(),
                DS4WinWPF.AppHost.GetService<IPathService>(),
                targetDevice);
        }
    }
}
