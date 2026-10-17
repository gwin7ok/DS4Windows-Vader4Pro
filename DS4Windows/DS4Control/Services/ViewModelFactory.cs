using System;
using DS4Windows.DI;
using DS4WinWPF;
using DS4WinWPF.DS4Forms.ViewModels;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;

namespace DS4Windows
{
    public class ViewModelFactory : IViewModelFactory
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IProfileRepository _profileRepo;
        private readonly IProfilePresetLoader _presetLoader;
        private readonly ISpecialActionRepository _actionRepo;
        private readonly IOutputSlotService _outputSlotService;
        private readonly IEnvironmentService _environmentService;
        private readonly IPathService _pathService;
        private readonly IAppSettingsService _appSettings;
        private readonly Func<IAutoProfileService> _autoProfileServiceAccessor;
        private readonly Func<ControlService> _controlServiceAccessor;

        // Phase6-Step9-4: 依存はすべて必須引数（Pure DI。決定1＝S9-A）。
        // ControlService は、構築時ではなく生成のたびに Func で遅延解決する（決定2＝C1。Step2 決定D1 と同じ形。
        // 明示的なホスト構築より前に ControlService を解決しない: Phase6-Status.md K7-1）。
        public ViewModelFactory(
            IProfileSettingsService profileSettings,
            IProfileRepository profileRepo,
            IProfilePresetLoader presetLoader,
            ISpecialActionRepository actionRepo,
            IOutputSlotService outputSlotService,
            IEnvironmentService environmentService,
            IPathService pathService,
            IAppSettingsService appSettings,
            Func<IAutoProfileService> autoProfileServiceAccessor,
            Func<ControlService> controlServiceAccessor)
        {
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _profileRepo = profileRepo ?? throw new ArgumentNullException(nameof(profileRepo));
            _presetLoader = presetLoader ?? throw new ArgumentNullException(nameof(presetLoader));
            _actionRepo = actionRepo ?? throw new ArgumentNullException(nameof(actionRepo));
            _outputSlotService = outputSlotService ?? throw new ArgumentNullException(nameof(outputSlotService));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            // Phase6-Step10-2-3: IAutoProfileService は、生成時に AutoProfileHolder を読む（設定の場所が決まった後でなければ生成できない）
            // ため、ControlService と同じく生成のたびに Func で遅延解決する。
            _autoProfileServiceAccessor = autoProfileServiceAccessor ?? throw new ArgumentNullException(nameof(autoProfileServiceAccessor));
            _controlServiceAccessor = controlServiceAccessor ?? throw new ArgumentNullException(nameof(controlServiceAccessor));
        }

        public ProfileSettingsViewModel CreateProfileSettingsViewModel(int device, int targetDevice = -1)
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ViewModelFactory: Created ProfileSettingsViewModel for Device {device}, targetDevice {targetDevice}");
            return new ProfileSettingsViewModel(device, _profileSettings, _controlServiceAccessor(),
                _outputSlotService, _profileRepo, _presetLoader, _environmentService, _pathService, targetDevice);
        }

        public RecordBoxViewModel CreateRecordBoxViewModel(int device, DS4ControlSettings controlSettings, bool recordMacro = true, bool extraHold = false, int targetDevice = -1)
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ViewModelFactory: Created RecordBoxViewModel for Device {device}, targetDevice {targetDevice}");
            return new RecordBoxViewModel(device, controlSettings, recordMacro, extraHold,
                _profileSettings, _controlServiceAccessor(), targetDevice);
        }

        public SpecialActEditorViewModel CreateSpecialActEditorViewModel(int device, SpecialAction action = null)
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ViewModelFactory: Created SpecialActEditorViewModel for Device {device}");
            return new SpecialActEditorViewModel(device, action, _actionRepo);
        }

        public SpecialActionsListViewModel CreateSpecialActionsListViewModel(int device)
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ViewModelFactory: Created SpecialActionsListViewModel for Device {device}");
            return new SpecialActionsListViewModel(device, _actionRepo, _profileRepo);
        }

        public AutoProfilesViewModel CreateAutoProfilesViewModel(AutoProfileHolder autoProfileHolder, ProfileList profileList)
        {
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace("[DI] ViewModelFactory: Created AutoProfilesViewModel");
            return new AutoProfilesViewModel(autoProfileHolder, profileList,
                _appSettings, _autoProfileServiceAccessor(), _controlServiceAccessor(), _environmentService);
        }
    }
}
