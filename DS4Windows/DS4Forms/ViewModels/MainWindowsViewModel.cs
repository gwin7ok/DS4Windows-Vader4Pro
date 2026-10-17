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
using System.Diagnostics;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    public class MainWindowsViewModel
    {
        private readonly IEnvironmentService _environmentService;
        private readonly IPathService _pathService;

        private bool fullTabsEnabled = true;

        public bool FullTabsEnabled
        {
            get => fullTabsEnabled;
            set
            {
                fullTabsEnabled = value;
                FullTabsEnabledChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler FullTabsEnabledChanged;

        // Phase6-Step9-2: 依存はコンストラクタで必須引数として受け取る（Pure DI。決定1＝S9-A）。
        // 旧 RunUpdaterCheck／DownloadUpstreamVersionInfo／LauchDS4Updater／IsNET8Available と updaterExe は、
        // 呼出元 0 件の上流の更新経路の残骸（2025-12-12 の b55d160b で呼び出しが消え、現行の更新処理は
        // UpdaterWindow が担う）のため、決定6＝D1 により削除した。
        public MainWindowsViewModel(IEnvironmentService environmentService, IPathService pathService)
        {
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));

            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace("[DI] MainWindowsViewModel: created (IEnvironmentService, IPathService injected)");
        }

        public void CheckDrivers()
        {
            bool deriverinstalled = _environmentService.IsViGEmBusInstalled();
            if (!deriverinstalled || !_environmentService.IsRunningSupportedViGEmBus())
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = $"{_pathService.ExecutablePath}";
                startInfo.Arguments = "-driverinstall";
                startInfo.Verb = "runas";
                startInfo.UseShellExecute = true;
                try
                {
                    using (Process temp = Process.Start(startInfo))
                    {
                    }
                }
                catch { }
            }
        }
    }
}
