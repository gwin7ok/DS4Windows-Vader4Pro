using System;
using DS4Windows.DI;
using DS4WinWPF;
using DS4WinWPF.DS4Forms;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4Windows.Services
{
    /// <summary>
    /// <see cref="IMainWindowFactory"/> の実装。メイン画面の生成と、画面が必要とするサービスの受け渡しを集約する（Phase7-Step5 W5）。
    /// <see cref="ControlService"/> と <see cref="IAutoProfileService"/> は、生成のたびに Func で遅延解決する
    /// （自動プロファイルの設定コレクションは、設定の場所が決まった後でなければ生成できないため。ViewModelFactory と同じ形）。
    /// </summary>
    public class MainWindowFactory : IMainWindowFactory
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IAppSettingsService _appSettings;
        private readonly IProfileXmlStore _profileXmlStore;
        private readonly IEditorWindowFactory _editorWindowFactory;
        private readonly IPathService _pathService;
        private readonly IEnvironmentService _environmentService;
        private readonly IAppearanceSettingsService _appearanceSettings;
        private readonly IProfileApplicationService _profileApplication;
        private readonly IOutputSlotService _outputSlotService;
        private readonly IProfileRepository _profileRepository;
        private readonly INotificationService _notificationService;
        private readonly IProfileSlotApplier _profileSlotApplier;
        private readonly MainWindowsViewModel _mainWindowsViewModel;
        private readonly ILogSettingsApplier _logSettingsApplier;
        private readonly IViewModelFactory _viewModelFactory;
        private readonly Func<ControlService> _controlServiceAccessor;
        private readonly Func<IAutoProfileService> _autoProfileServiceAccessor;

        public MainWindowFactory(
            IProfileSettingsService profileSettings,
            IAppSettingsService appSettings,
            IProfileXmlStore profileXmlStore,
            IEditorWindowFactory editorWindowFactory,
            IPathService pathService,
            IEnvironmentService environmentService,
            IAppearanceSettingsService appearanceSettings,
            IProfileApplicationService profileApplication,
            IOutputSlotService outputSlotService,
            IProfileRepository profileRepository,
            INotificationService notificationService,
            IProfileSlotApplier profileSlotApplier,
            MainWindowsViewModel mainWindowsViewModel,
            ILogSettingsApplier logSettingsApplier,
            IViewModelFactory viewModelFactory,
            Func<ControlService> controlServiceAccessor,
            Func<IAutoProfileService> autoProfileServiceAccessor)
        {
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _profileXmlStore = profileXmlStore ?? throw new ArgumentNullException(nameof(profileXmlStore));
            _editorWindowFactory = editorWindowFactory ?? throw new ArgumentNullException(nameof(editorWindowFactory));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _appearanceSettings = appearanceSettings ?? throw new ArgumentNullException(nameof(appearanceSettings));
            _profileApplication = profileApplication ?? throw new ArgumentNullException(nameof(profileApplication));
            _outputSlotService = outputSlotService ?? throw new ArgumentNullException(nameof(outputSlotService));
            _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _profileSlotApplier = profileSlotApplier ?? throw new ArgumentNullException(nameof(profileSlotApplier));
            _mainWindowsViewModel = mainWindowsViewModel ?? throw new ArgumentNullException(nameof(mainWindowsViewModel));
            _logSettingsApplier = logSettingsApplier ?? throw new ArgumentNullException(nameof(logSettingsApplier));
            _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
            _controlServiceAccessor = controlServiceAccessor ?? throw new ArgumentNullException(nameof(controlServiceAccessor));
            _autoProfileServiceAccessor = autoProfileServiceAccessor ?? throw new ArgumentNullException(nameof(autoProfileServiceAccessor));
        }

        public MainWindow Create(ArgumentParser parser)
            => new MainWindow(parser, _profileSettings, _appSettings, _profileXmlStore, _editorWindowFactory,
                _pathService, _environmentService, _appearanceSettings, _profileApplication, _outputSlotService,
                _profileRepository, _notificationService, _controlServiceAccessor(), _profileSlotApplier,
                _mainWindowsViewModel, _logSettingsApplier, _autoProfileServiceAccessor(), _viewModelFactory);
    }
}
