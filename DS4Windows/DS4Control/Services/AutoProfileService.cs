using System;
using System.Diagnostics;
using System.Threading;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4Windows
{
    public class AutoProfileService : IAutoProfileService
    {
        private readonly object _syncLock = new object();
        private readonly AutoProfileHolder _holder;
        private readonly IProfileApplicationService _profileAppService;
        private readonly IProfileSettingsService _profileSettings;
        private readonly IProcessInspector _processInspector;

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

        public event Action<bool> RequestServiceChange;

        public AutoProfileService(AutoProfileHolder holder = null,
            IProfileApplicationService profileAppService = null,
            IProfileSettingsService profileSettings = null,
            IProcessInspector processInspector = null)
        {
            _holder = holder ?? Global.AutoProfileHolderInstance;
            _profileAppService = profileAppService ?? DS4WinWPF.AppHost.GetService<IProfileApplicationService>();
            _profileSettings = profileSettings ?? DS4WinWPF.AppHost.GetService<IProfileSettingsService>();
            _processInspector = processInspector ?? DS4WinWPF.AppHost.GetService<IProcessInspector>();
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

                    for (int j = 0; j < ControlService.CURRENT_DS4_CONTROLLER_LIMIT; j++)
                    {
                        string tempname = matchedProfileEntity.ProfileNames[j];
                        if (!string.IsNullOrEmpty(tempname) && tempname != "(none)")
                        {
                            bool useTemp = _profileSettings?.GetUseTempProfile(j) ?? false;
                            string currentTemp = _profileSettings?.GetTempProfileName(j) ?? string.Empty;
                            string currentProfile = Global.ProfilePath[j];

                            if ((useTemp && tempname != currentTemp) ||
                                (!useTemp && tempname != currentProfile) ||
                                forceLoadProfile)
                            {
                                if (AutoProfileDebugLogLevel > 0)
                                    AppLogger.LogToGui($"DEBUG: Auto-Profile. LoadProfile Controller {j + 1}={tempname}", false, true);

                                string prolog = string.Format(DS4WinWPF.Properties.Resources.UsingAutoTempProfile, (j + 1).ToString(), tempname);

                                // Step 3 & Step 4 申し送り事項:
                                // IProfileApplicationService.ApplyProfile を呼び出す。
                                // Halt ガード（§5.2）はサービス内に内包され、Program.rootHub 直参照は排除。
                                // displayNotification は null を指定して _profileSettings.ProfileChangedNotification を自動解決（Step 4）。
                                _profileAppService.ApplyProfile(j, tempname, isTemp: true, launchProgram: true,
                                    source: ProfileChangeSource.AutoProfile, prolog: prolog, displayNotification: null);
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
                        if (App.rootHub != null && App.rootHub.running)
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
                    if (_turnOffTemp && Global.AutoProfileRevertDefaultProfile)
                    {
                        _turnOffTemp = false;
                        if (App.rootHub != null && !App.rootHub.running)
                        {
                            if (AutoProfileDebugLogLevel > 0)
                                AppLogger.LogToGui("DEBUG: Auto-Profile. Turning DS4Windows on before reverting to default profile", false, true);
                            SetAndWaitServiceStatus(true);
                        }
                    }

                    _tempAutoProfile = null;
                    for (int j = 0; j < ControlService.CURRENT_DS4_CONTROLLER_LIMIT; j++)
                    {
                        bool useTemp = _profileSettings?.GetUseTempProfile(j) ?? false;
                        if (useTemp)
                        {
                            if (Global.AutoProfileRevertDefaultProfile)
                            {
                                string defaultProfile = Global.ProfilePath[j];
                                if (AutoProfileDebugLogLevel > 0)
                                    AppLogger.LogToGui($"DEBUG: Auto-Profile. Unknown process. Reverting to default profile. Controller {j + 1}={defaultProfile} (default)", false, true);

                                string prolog = string.Format(DS4WinWPF.Properties.Resources.UsingProfile, (j + 1).ToString(), defaultProfile, "N/A");

                                // Step 3 & Step 4 申し送り事項:
                                // 一時プロファイル解除復帰（isTemp: false）、displayNotification: null で自動解決
                                _profileAppService.ApplyProfile(j, defaultProfile, isTemp: false, launchProgram: false,
                                    source: ProfileChangeSource.AutoProfile, prolog: prolog, displayNotification: null);
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
            if (App.rootHub != null && App.rootHub.running != serviceRunningStatus)
            {
                RequestServiceChange?.Invoke(serviceRunningStatus);
                Stopwatch sw = Stopwatch.StartNew();
                while (App.rootHub.running != serviceRunningStatus && sw.Elapsed.TotalSeconds < 10)
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
