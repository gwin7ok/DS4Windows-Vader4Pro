using System;
using System.Diagnostics;
using System.Threading;
using DS4Windows.DI;
using DS4Windows.Services;
using DS4WinWPF;

namespace DS4Windows
{
    public class AutoProfileService : IAutoProfileService
    {
        private readonly object _syncLock = new object();
        private readonly AutoProfileHolder _holder;
        private readonly IProfileApplicationService _profileAppService;
        private readonly IProfileSettingsService _profileSettings;
        private readonly IProcessInspector _processInspector;
        // Phase6-Step2-1c (決定O2=C/O3=A): スロット上限（現在接続台数ではない）の取得元。
        private readonly IEnvironmentService _environmentService;

        private bool _turnOffTemp;
        private AutoProfileEntity _tempAutoProfile;
        private bool _running;
        private int _autoProfileDebugLogLevel;

        public bool Running
        {
            get => _running;
            set => _running = value;
        }

        public int AutoProfileDebugLogLevel
        {
            get => _autoProfileDebugLogLevel;
            set => _autoProfileDebugLogLevel = value;
        }

        public AutoProfileHolder Holder => _holder;

        // Phase7-Step3 W5: Global 経由をやめ、注入された BackingStore を直接使う。
        private readonly BackingStore _config;

        public AutoProfileDisplayProfileSwitchChoices AutoProfileSwitchNotifyChoice
        {
            get => _config.autoProfileSwitchNotifyChoice;
            set => _config.autoProfileSwitchNotifyChoice = value;
        }

        // Phase6-Step10-2-1: Global への薄い委譲
        public bool CreateAutoProfilesFile(string path) => Global.CreateAutoProfiles(path);

        public event Action<bool> RequestServiceChange;

        // Phase6-Step10-4-2b: 全ての依存を必須化（new／AppHost／Program.rootHub／Global の設定参照を廃止）。
        // ControlService は、起動順序と依存循環を避けるため、遅延解決（Func）で受け取る（決定 D4-3）。
        private readonly Func<ControlService> _controlProvider;
        private readonly IAppSettingsService _appSettings;
        private readonly IProfileRepository _profileRepository;
        private ControlService Control => _controlProvider();

        public AutoProfileService(AutoProfileHolder holder,
            IProfileApplicationService profileAppService,
            IProfileSettingsService profileSettings,
            IProcessInspector processInspector,
            IEnvironmentService environmentService,
            IAppSettingsService appSettings,
            IProfileRepository profileRepository,
            Func<ControlService> controlProvider,
            BackingStore config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _holder = holder ?? throw new ArgumentNullException(nameof(holder));
            _profileAppService = profileAppService ?? throw new ArgumentNullException(nameof(profileAppService));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _processInspector = processInspector ?? throw new ArgumentNullException(nameof(processInspector));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
            _controlProvider = controlProvider ?? throw new ArgumentNullException(nameof(controlProvider));
        }

        public void CheckProfiles()
        {
            lock (_syncLock)
            {
                if (_holder == null || _processInspector == null || _profileAppService == null)
                    return;

                string topProcessName, topWindowTitle;
                if (!_processInspector.GetForegroundProcessInfo(out topProcessName, out topWindowTitle))
                    return;

                bool turnOffDS4WinApp = false;
                AutoProfileEntity matchedProfileEntity = null;

                for (int i = 0, pathsLen = _holder.AutoProfileColl.Count; i < pathsLen; i++)
                {
                    AutoProfileEntity tempEntity = _holder.AutoProfileColl[i];
                    if (tempEntity.IsMatch(topProcessName, topWindowTitle))
                    {
                        if (AutoProfileDebugLogLevel > 0)
                            AppLogger.LogToGui($"DEBUG: Auto-Profile. Rule#{i + 1} Path={tempEntity.path} Title={tempEntity.title}", false, true);

                        turnOffDS4WinApp = tempEntity.Turnoff;
                        matchedProfileEntity = tempEntity;
                        break;
                    }
                }

                if (matchedProfileEntity != null)
                {
                    bool forceLoadProfile = false;
                    if (!turnOffDS4WinApp && _turnOffTemp)
                    {
                        _turnOffTemp = false;
                        SetAndWaitServiceStatus(true);
                        forceLoadProfile = true;
                    }

                    for (int j = 0; j < _environmentService.ControllerSlotLimit; j++)
                    {
                        string tempname = matchedProfileEntity.ProfileNames[j];
                        if (!string.IsNullOrEmpty(tempname) && tempname != "(none)")
                        {
                            bool useTemp = _profileSettings?.GetUseTempProfile(j) ?? false;
                            string currentTemp = _profileSettings?.GetTempProfileName(j) ?? string.Empty;
                            string currentProfile = _profileRepository.ProfilePath[j];

                            if ((useTemp && tempname != currentTemp) ||
                                (!useTemp && tempname != currentProfile) ||
                                forceLoadProfile)
                            {
                                if (AutoProfileDebugLogLevel > 0)
                                    AppLogger.LogToGui($"DEBUG: Auto-Profile. LoadProfile Controller {j + 1}={tempname}", false, true);

                                string prolog = string.Format(DS4WinWPF.Properties.Resources.UsingAutoTempProfile, (j + 1).ToString(), tempname);

                                // Step 3 & Step 4 申し送り事項:
                                // IProfileApplicationService.ApplyProfile を呼び出す。
                                // Halt ガード（§5.2）はサービス内に内包され、ControlService の直参照は排除。
                                // displayNotification は null を指定して _profileSettings.ProfileChangedNotification を自動解決（Step 4）。
                                _profileAppService.ApplyProfile(j, tempname, isTemp: true, launchProgram: true,
                                    source: ProfileChangeSource.AutoProfile, prolog: prolog);
                            }
                            else if (AutoProfileDebugLogLevel > 0)
                            {
                                AppLogger.LogToGui($"DEBUG: Auto-Profile. LoadProfile Controller {j + 1}={tempname} (already loaded)", false, true);
                            }
                        }
                    }

                    if (turnOffDS4WinApp)
                    {
                        _turnOffTemp = true;
                        if (Control != null && Control.running)
                        {
                            if (AutoProfileDebugLogLevel > 0)
                                AppLogger.LogToGui("DEBUG: Auto-Profile. Turning DS4Windows temporarily off", false, true);
                            SetAndWaitServiceStatus(false);
                        }
                    }

                    _tempAutoProfile = matchedProfileEntity;
                }
                else if (_tempAutoProfile != null)
                {
                    if (_turnOffTemp && _appSettings.AutoProfileRevertDefaultProfile)
                    {
                        _turnOffTemp = false;
                        if (Control != null && !Control.running)
                        {
                            if (AutoProfileDebugLogLevel > 0)
                                AppLogger.LogToGui("DEBUG: Auto-Profile. Turning DS4Windows on before reverting to default profile", false, true);
                            SetAndWaitServiceStatus(true);
                        }
                    }

                    _tempAutoProfile = null;
                    for (int j = 0; j < _environmentService.ControllerSlotLimit; j++)
                    {
                        bool useTemp = _profileSettings?.GetUseTempProfile(j) ?? false;
                        if (useTemp)
                        {
                            if (_appSettings.AutoProfileRevertDefaultProfile)
                            {
                                string defaultProfile = _profileRepository.ProfilePath[j];
                                if (AutoProfileDebugLogLevel > 0)
                                    AppLogger.LogToGui($"DEBUG: Auto-Profile. Unknown process. Reverting to default profile. Controller {j + 1}={defaultProfile} (default)", false, true);

                                string prolog = string.Format(DS4WinWPF.Properties.Resources.UsingProfile, (j + 1).ToString(), defaultProfile, "N/A");

                                // Step 3 & Step 4 申し送り事項:
                                // 一時プロファイル解除復帰（isTemp: false）、displayNotification: null で自動解決
                                _profileAppService.ApplyProfile(j, defaultProfile, isTemp: false, launchProgram: false,
                                    source: ProfileChangeSource.AutoProfile, prolog: prolog);
                            }
                            else if (AutoProfileDebugLogLevel > 0)
                            {
                                string tempName = _profileSettings?.GetTempProfileName(j) ?? string.Empty;
                                AppLogger.LogToGui($"DEBUG: Auto-Profile. Unknown process. Existing profile left as active. Controller {j + 1}={tempName}", false, true);
                            }
                        }
                    }
                }
            }
        }

        private void SetAndWaitServiceStatus(bool serviceRunningStatus)
        {
            if (Control != null && Control.running != serviceRunningStatus)
            {
                RequestServiceChange?.Invoke(serviceRunningStatus);
                Stopwatch sw = Stopwatch.StartNew();
                while (Control.running != serviceRunningStatus && sw.Elapsed.TotalSeconds < 10)
                {
                    Thread.SpinWait(1000);
                }
                Thread.SpinWait(1000);
            }
        }

        public void ClearState()
        {
            lock (_syncLock)
            {
                _tempAutoProfile = null;
                _turnOffTemp = false;
            }
        }
    }
}