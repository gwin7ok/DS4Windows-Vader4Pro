/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using DS4Windows;
using DS4Windows.DI;
using static DS4Windows.Util;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    public class SettingsViewModel : IDisposable
    {
        private readonly IAppSettingsService _appSettings;
        private readonly IAppearanceSettingsService _appearanceSettings;
        private readonly IEnvironmentService _environmentService;
        private readonly IPathService _pathService;

        // Re-Enable Ex Mode
        public bool HideDS4Controller
        {
            get => _appSettings.UseExclusiveMode;
            set => _appSettings.UseExclusiveMode = value;
        }

        public bool SwipeTouchSwitchProfile
        {
            get => _appSettings.SwipeProfiles;
            set => _appSettings.SwipeProfiles = value;
        }

        private bool runAtStartup;
        public bool RunAtStartup
        {
            get => runAtStartup;
            set
            {
                runAtStartup = value;
                RunAtStartupChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler RunAtStartupChanged;

        private bool runStartProg;
        public bool RunStartProg
        {
            get => runStartProg;
            set
            {
                runStartProg = value;
                RunStartProgChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler RunStartProgChanged;

        private bool runStartTask;
        public bool RunStartTask
        {
            get => runStartTask;
            set
            {
                runStartTask = value;
                RunStartTaskChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler RunStartTaskChanged;

        private bool canWriteTask;
        public bool CanWriteTask { get => canWriteTask; }

        public ImageSource uacSource;
        public ImageSource UACSource { get => uacSource; }

        public ImageSource questionMarkSource;
        public ImageSource QuestionMarkSource { get => questionMarkSource; }

        private Visibility showRunStartPanel = Visibility.Collapsed;
        public Visibility ShowRunStartPanel
        {
            get => showRunStartPanel;
            set
            {
                if (showRunStartPanel == value) return;
                showRunStartPanel = value;
                ShowRunStartPanelChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler ShowRunStartPanelChanged;

        public bool ProfileChangedNotification
        {
            get => _appSettings.ProfileChangedNotification;
            set => _appSettings.ProfileChangedNotification = value;
        }

        public int ShowNotificationsIndex
        {
            get => _appSettings.Notifications;
            set => _appSettings.Notifications = value;
        }

        public bool DisconnectBTStop { get => _appSettings.DCBTatStop; set => _appSettings.DCBTatStop = value; }
        public bool FlashHighLatency { get => _appSettings.FlashWhenLate; set => _appSettings.FlashWhenLate = value; }
        public int FlashHighLatencyAt { get => _appSettings.FlashWhenLateAt; set => _appSettings.FlashWhenLateAt = value; }
        public bool StartMinimize { get => _appSettings.StartMinimized; set => _appSettings.StartMinimized = value; }
        public bool MinimizeToTaskbar { get => _appSettings.MinimizeToTaskbar; set => _appSettings.MinimizeToTaskbar = value; }
        public bool CloseMinimizes { get => _appSettings.CloseMinimizes; set => _appSettings.CloseMinimizes = value; }
        public bool QuickCharge { get => _appSettings.QuickCharge; set => _appSettings.QuickCharge = value; }

        public int IconChoiceIndex
        {
            get => (int)_appearanceSettings.UseIconChoice;
            set
            {
                int temp = (int)_appearanceSettings.UseIconChoice;
                if (temp == value) return;
                _appearanceSettings.UseIconChoice = (DS4Windows.TrayIconChoice)value;
                IconChoiceIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler IconChoiceIndexChanged;

        public int AppChoiceIndex
        {
            get => (int)_appearanceSettings.UseCurrentTheme;
            set
            {
                int temp = (int)_appearanceSettings.UseCurrentTheme;
                if (temp == value) return;
                _appearanceSettings.UseCurrentTheme = (DS4Windows.AppThemeChoice)value;
                AppChoiceIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler AppChoiceIndexChanged;

        // CheckForUpdates: controls whether update checking is enabled.
        // We represent "disabled" with -1. A value of 0 means "check every startup".
        public bool CheckForUpdates
        {
            get => _appSettings.CheckUpdateStartupEnabled;
            set
            {
                _appSettings.CheckUpdateStartupEnabled = value;
                CheckForNoUpdatesWhen();
                _appSettings?.Save();
            }
        }
        public event EventHandler CheckForUpdatesChanged;

        public int CheckEvery
        {
            get
            {
                // Use the explicit persisted numeric value. The legacy `CheckWhen`
                // stores hours and was used before separate fields existed.
                // Now the numeric control maps to `CheckEveryValue` directly.
                return _appSettings.CheckEveryValue;
            }
            set
            {
                // If update checks are disabled (represented by -1), do not modify
                // underlying `CheckWhen` from the numeric control. This prevents
                // the numeric binding from re-enabling checks when the checkbox
                // is unchecked.
                if (!_appSettings.CheckUpdateStartupEnabled)
                {
                    return;
                }

                if (_appSettings.CheckEveryValue != value)
                {
                    _appSettings.CheckEveryValue = value;
                    CheckForNoUpdatesWhen();
                    _appSettings?.Save();
                }
            }
        }
        public event EventHandler CheckEveryChanged;

        private int checkEveryUnitIdx = 1;
        public int CheckEveryUnit
        {
            get
            {
                return checkEveryUnitIdx;
            }
            set
            {
                if (checkEveryUnitIdx == value) return;
                // Persist selection immediately to the backing store so saves include it.
                _appSettings.CheckEveryUnit = value;
                checkEveryUnitIdx = value;
                CheckEveryUnitChanged?.Invoke(this, EventArgs.Empty);
                _appSettings?.Save();
            }
        }
        public event EventHandler CheckEveryUnitChanged;

        public bool UseOSCServer
        {
            get => _appSettings.UseOscServer;
            set
            {
                if (_appSettings.UseOscServer == value) return;
                _appSettings.UseOscServer = value;
                UseOSCServerChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler UseOSCServerChanged;
        public int OscPort { get => _appSettings.OscServerPort; set => _appSettings.OscServerPort = value; }

        public bool InterpretingOscMonitoring { get => _appSettings.InterpretingOscMonitoring; set => _appSettings.InterpretingOscMonitoring = value; }

        public bool UseOSCSender
        {
            get => _appSettings.UseOscSender;
            set
            {
                if (_appSettings.UseOscSender == value) return;
                _appSettings.UseOscSender = value;
                UseOSCSenderChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler UseOSCSenderChanged;
        public int OscSendPort { get => _appSettings.OscSenderPort; set => _appSettings.OscSenderPort = value; }

        public string OscSenderAddress
        {
            get => _appSettings.OscSenderAddress;
            set => _appSettings.OscSenderAddress = value;
        }


        public bool UseUDPServer
        {
            get => _appSettings.UseUdpServer;
            set
            {
                if (_appSettings.UseUdpServer == value) return;
                _appSettings.UseUdpServer = value;
                UseUDPServerChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler UseUDPServerChanged;

        public string UdpIpAddress
        {
            get => _appSettings.UdpServerListenAddress;
            set => _appSettings.UdpServerListenAddress = value;
        }
        public int UdpPort { get => _appSettings.UdpServerPort; set => _appSettings.UdpServerPort = value; }

        public bool UseUdpSmoothing
        {
            get => _appSettings.UseUdpServerSmoothing;
            set
            {
                bool temp = _appSettings.UseUdpServerSmoothing;
                if (temp == value) return;
                _appSettings.UseUdpServerSmoothing = value;
                UseUdpSmoothingChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler UseUdpSmoothingChanged;

        public Visibility UdpServerOneEuroPanelVisibility
        {
            get => _appSettings.UseUdpServer && _appSettings.UseUdpServerSmoothing ? Visibility.Visible : Visibility.Collapsed;
        }
        public event EventHandler UdpServerOneEuroPanelVisibilityChanged;

        public double UdpSmoothMinCutoff
        {
            get => _appSettings.UDPServerSmoothingMincutoff;
            set
            {
                double temp = _appSettings.UDPServerSmoothingMincutoff;
                if (temp == value) return;
                _appSettings.UDPServerSmoothingMincutoff = value;
                UdpSmoothMinCutoffChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler UdpSmoothMinCutoffChanged;

        public double UdpSmoothBeta
        {
            get => _appSettings.UDPServerSmoothingBeta;
            set
            {
                double temp = _appSettings.UDPServerSmoothingBeta;
                if (temp == value) return;
                _appSettings.UDPServerSmoothingBeta = value;
                UdpSmoothBetaChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler UdpSmoothBetaChanged;

        public bool UseCustomSteamFolder
        {
            get => _appSettings.UseCustomSteamFolder;
            set
            {
                _appSettings.UseCustomSteamFolder = value;
                UseCustomSteamFolderChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler UseCustomSteamFolderChanged;

        public string CustomSteamFolder
        {
            get => _appSettings.CustomSteamFolder;
            set
            {
                string temp = _appSettings.CustomSteamFolder;
                if (temp == value) return;
                if (Directory.Exists(value) || value == string.Empty)
                {
                    _appSettings.CustomSteamFolder = value;
                }
            }
        }

        private bool viewEnabled = true;
        public bool ViewEnabled
        {
            get => viewEnabled;
            set
            {
                viewEnabled = value;
                ViewEnabledChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler ViewEnabledChanged;

        public string FakeExeName
        {
            get => _appSettings.FakeExeName;
            set
            {
                string temp = _appSettings.FakeExeName;
                if (temp == value) return;
                _appSettings.FakeExeName = value;
                // Phase6-Step9-3d（K9-2）: 古いファイルの削除（Compare）を、新しいファイルの作成（Changed）より先に行う。
                // 作成が例外になっても、古いファイルが残らないようにする（以前は作成が先で、例外で削除が実行されなかった）。
                FakeExeNameChangeCompare?.Invoke(this, temp, value);
                FakeExeNameChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler FakeExeNameChanged;
        public event FakeExeNameChangeHandler FakeExeNameChangeCompare;
        public delegate void FakeExeNameChangeHandler(SettingsViewModel sender,
            string oldvalue, string newvalue);


        public bool HidHideClientFound
        {
            get
            {
                bool result = _environmentService.HidHideInstalled &&
                    !string.IsNullOrEmpty(DS4Windows.Util.GetHidHideClientPath());

                return result;
            }
        }
        public event EventHandler HidHideClientFoundChanged;

        public int ProcessPriorityIndex
        {
            get => _appSettings.ProcessPriority;
            set
            {
                _appSettings.ProcessPriority = value;
                ProcessPriorityIndexChanged?.Invoke(this, EventArgs.Empty);
            }

        }

        public event EventHandler ProcessPriorityIndexChanged;

        // Phase6-Step9-3: 依存はコンストラクタで必須引数として受け取る（Pure DI。決定1＝S9-A）。
        // Phase6-Step10-1-5（決定D7＝A）: App.logHolder（静的フィールド）の直接参照の置換先。必須引数（null は拒否する）。
        private readonly ILogSettingsApplier _logSettingsApplier;

        public SettingsViewModel(IAppSettingsService appSettings, IAppearanceSettingsService appearanceSettings,
            IEnvironmentService environmentService, IPathService pathService, ILogSettingsApplier logSettingsApplier)
        {
            _logSettingsApplier = logSettingsApplier ?? throw new ArgumentNullException(nameof(logSettingsApplier));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _appearanceSettings = appearanceSettings ?? throw new ArgumentNullException(nameof(appearanceSettings));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));

            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace("[DI] SettingsViewModel: created");

            // Initialize unit index and numeric value from persistent settings
            checkEveryUnitIdx = _appSettings.CheckEveryUnit;
            // Ensure unit idx is valid
            if (checkEveryUnitIdx != 0 && checkEveryUnitIdx != 1) checkEveryUnitIdx = 1;
            // No longer derive from CheckWhen; numeric value is held separately

            CheckStartupOptions();

            Icon img = SystemIcons.Shield;
            Bitmap bitmap = img.ToBitmap();
            IntPtr hBitmap = bitmap.GetHbitmap();

            ImageSource wpfBitmap =
                 Imaging.CreateBitmapSourceFromHBitmap(
                      hBitmap, IntPtr.Zero, Int32Rect.Empty,
                      BitmapSizeOptions.FromEmptyOptions());
            uacSource = wpfBitmap;

            img = SystemIcons.Question;
            wpfBitmap =
                 Imaging.CreateBitmapSourceFromHBitmap(
                      img.ToBitmap().GetHbitmap(), IntPtr.Zero, Int32Rect.Empty,
                      BitmapSizeOptions.FromEmptyOptions());
            questionMarkSource = wpfBitmap;

            runStartProg = StartupMethods.HasStartProgEntry();
            try
            {
                runStartTask = StartupMethods.HasTaskEntry();
            }
            catch (COMException ex)
            {
                DS4Windows.AppLogger.LogToGui(string.Format("Error in TaskService. Check WinOS TaskScheduler service functionality. {0}", ex.Message), true);
            }

            runAtStartup = runStartProg || runStartTask;
            canWriteTask = _environmentService.IsAdministrator();

            if (!runAtStartup)
            {
                runStartProg = true;
            }
            else if (runStartProg && runStartTask)
            {
                runStartProg = false;
                if (StartupMethods.CanWriteStartEntry())
                {
                    StartupMethods.DeleteStartProgEntry();
                }
            }

            if (runAtStartup && runStartProg)
            {
                bool locChange = StartupMethods.CheckStartupExeLocation(_pathService.ExecutablePath);
                if (locChange)
                {
                    if (StartupMethods.CanWriteStartEntry())
                    {
                        StartupMethods.DeleteStartProgEntry();
                        StartupMethods.WriteStartProgEntry(_pathService.ExecutablePath, _pathService.ProgramDirectory);
                    }
                    else
                    {
                        runAtStartup = false;
                        showRunStartPanel = Visibility.Collapsed;
                    }
                }
            }
            else if (runAtStartup && runStartTask)
            {
                if (canWriteTask)
                {
                    StartupMethods.DeleteOldTaskEntry(_pathService.ProgramDirectory);
                    StartupMethods.WriteTaskEntry(_pathService.ProgramDirectory,
                        System.IO.Path.GetFileName(_pathService.ExecutablePath));
                }
            }

            if (runAtStartup)
            {
                showRunStartPanel = Visibility.Visible;
            }

            RunAtStartupChanged += SettingsViewModel_RunAtStartupChanged;
            RunStartProgChanged += SettingsViewModel_RunStartProgChanged;
            RunStartTaskChanged += SettingsViewModel_RunStartTaskChanged;
            FakeExeNameChanged += SettingsViewModel_FakeExeNameChanged;
            FakeExeNameChangeCompare += SettingsViewModel_FakeExeNameChangeCompare;
            UseUdpSmoothingChanged += SettingsViewModel_UseUdpSmoothingChanged;
            UseUDPServerChanged += SettingsViewModel_UseUDPServerChanged;
        }

        public void Dispose()
        {
        }

        private void SettingsViewModel_UseUDPServerChanged(object sender, EventArgs e)
        {
            UdpServerOneEuroPanelVisibilityChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SettingsViewModel_UseUdpSmoothingChanged(object sender, EventArgs e)
        {
            UdpServerOneEuroPanelVisibilityChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SettingsViewModel_FakeExeNameChangeCompare(SettingsViewModel sender,
            string oldvalue, string newvalue)
        {
            DeleteFakeExeFiles(_pathService.ProgramDirectory, oldvalue);
        }

        /// <summary>
        /// 偽装 exe 名の古いファイル（<c>名前.exe</c>／<c>.runtimeconfig.json</c>／<c>.deps.json</c>）を削除する。
        /// 名前が空の場合は何もしない。
        /// </summary>
        public static void DeleteFakeExeFiles(string programDirectory, string fakeName)
        {
            if (string.IsNullOrEmpty(fakeName))
            {
                return;
            }

            string old_exefile = Path.Combine(programDirectory, $"{fakeName}.exe");
            string old_conf_file = Path.Combine(programDirectory, $"{fakeName}.runtimeconfig.json");
            string old_deps_file = Path.Combine(programDirectory, $"{fakeName}.deps.json");

            if (File.Exists(old_exefile))
            {
                File.Delete(old_exefile);
            }

            if (File.Exists(old_conf_file))
            {
                File.Delete(old_conf_file);
            }

            if (File.Exists(old_deps_file))
            {
                File.Delete(old_deps_file);
            }
        }

        private void SettingsViewModel_FakeExeNameChanged(object sender, EventArgs e)
        {
            string temp = FakeExeName;
            if (!string.IsNullOrEmpty(temp))
            {
                CreateFakeExe(FakeExeName);
            }
        }

        private void SettingsViewModel_RunStartTaskChanged(object sender, EventArgs e)
        {
            if (runStartTask)
            {
                StartupMethods.WriteTaskEntry(_pathService.ProgramDirectory,
                        System.IO.Path.GetFileName(_pathService.ExecutablePath));
            }
            else
            {
                StartupMethods.DeleteTaskEntry();
            }
        }

        private void SettingsViewModel_RunStartProgChanged(object sender, EventArgs e)
        {
            if (runStartProg)
            {
                StartupMethods.WriteStartProgEntry(_pathService.ExecutablePath, _pathService.ProgramDirectory);
            }
            else
            {
                StartupMethods.DeleteStartProgEntry();
            }
        }

        private void SettingsViewModel_RunAtStartupChanged(object sender, EventArgs e)
        {
            if (runAtStartup)
            {
                RunStartProg = true;
                RunStartTask = false;
            }
            else
            {
                StartupMethods.DeleteStartProgEntry();
                StartupMethods.DeleteTaskEntry();
            }
        }

        private void CheckStartupOptions()
        {
            bool lnkExists = File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\DS4Windows.lnk");
            if (lnkExists)
            {
                runAtStartup = true;
            }
            else
            {
                runAtStartup = false;
            }
        }

        private void CheckForNoUpdatesWhen()
        {
            // If disabled, ensure UI shows sensible units. 0 means "every startup",
            // so only treat -1 as the explicit disabled sentinel.
            if (!_appSettings.CheckUpdateStartupEnabled)
            {
                // keep unit in sync with persisted value when disabled
                checkEveryUnitIdx = _appSettings.CheckEveryUnit;
            }

            CheckForUpdatesChanged?.Invoke(this, EventArgs.Empty);
            CheckEveryChanged?.Invoke(this, EventArgs.Empty);
            CheckEveryUnitChanged?.Invoke(this, EventArgs.Empty);
        }

        public void CreateFakeExe(string filename)
        {
            CreateFakeExeFiles(_pathService.ProgramDirectory, _pathService.ExecutablePath, filename);
        }

        /// <summary>
        /// 偽装名の exe と、その設定ファイル（<c>.runtimeconfig.json</c>／<c>.deps.json</c>）を作る。
        /// Phase6-Step9-3d（K9-2）: 設定ファイルの元は、<c>実行ファイル名.exe.runtimeconfig.json</c>（実在しない）ではなく、
        /// 拡張子を除いた <c>実行ファイル名.runtimeconfig.json</c>／<c>.deps.json</c>。元が無い場合は、コピーを省略する。
        /// </summary>
        public static void CreateFakeExeFiles(string programDirectory, string exeLocation, string fakeName)
        {
            string exeBase = Path.Combine(Path.GetDirectoryName(exeLocation),
                Path.GetFileNameWithoutExtension(exeLocation));
            string current_conf_file_path = $"{exeBase}.runtimeconfig.json";
            string current_deps_file_path = $"{exeBase}.deps.json";

            string exefile = Path.Combine(programDirectory, $"{fakeName}.exe");
            string fake_conf_file = Path.Combine(programDirectory, $"{fakeName}.runtimeconfig.json");
            string fake_deps_file = Path.Combine(programDirectory, $"{fakeName}.deps.json");

            File.Copy(exeLocation, exefile, true); // Copy exe

            // Copy needed app config and deps files
            if (File.Exists(current_conf_file_path))
            {
                File.Copy(current_conf_file_path, fake_conf_file, true);
            }

            if (File.Exists(current_deps_file_path))
            {
                File.Copy(current_deps_file_path, fake_deps_file, true);
            }
        }

        public void DriverCheckRefresh()
        {
            HidHideClientFoundChanged?.Invoke(this, EventArgs.Empty);
        }

        public int LogMaxArchiveFiles
        {
            get => _appSettings.LogMaxArchiveFiles;
            set
            {
                if (_appSettings.LogMaxArchiveFiles != value)
                {
                    _appSettings.LogMaxArchiveFiles = value;
                    _logSettingsApplier.UpdateLogSettings();
                    _logSettingsApplier.UpdateNLogConfig(value);
                    _appSettings?.Save();
                    _logSettingsApplier.RestoreArchiveSetting(); // Restore after all updates
                    LogMaxArchiveFilesChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public string LogMinLevel
        {
            get => _appSettings.LogMinLevel;
            set
            {
                if (_appSettings.LogMinLevel != value)
                {
                    _appSettings.LogMinLevel = value;
                    _logSettingsApplier.UpdateLogSettings();
                    _appSettings?.Save();
                    _logSettingsApplier.RestoreArchiveSetting(); // Restore after all updates
                    LogMinLevelChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler LogMaxArchiveFilesChanged;
        public event EventHandler LogMinLevelChanged;
    }
}