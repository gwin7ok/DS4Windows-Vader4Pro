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

using DS4Windows;
using DS4WinWPF.ApiDTO;
using HttpProgress;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    public class MainWindowsViewModel
    {
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

        // Use only the x64 updater asset (DS4Updater.exe). x86 support not provided.
        public string updaterExe = "DS4Updater.exe";

        private string DownloadUpstreamUpdaterVersion()
        {
            // Use this repository's releases to determine the upstream version
            Uri url = new Uri("https://api.github.com/repos/gwin7ok/DS4Windows-Vader4Pro/releases/latest");

            Task<System.Net.Http.HttpResponseMessage> requestTask = App.requestClient.GetAsync(url.ToString());
            requestTask.Wait();
            if (requestTask.Result.IsSuccessStatusCode)
            {
                var gitHubReleaseTask = requestTask.Result.Content.ReadFromJsonAsync<GithubRelease>();
                gitHubReleaseTask.Wait();
                if (!gitHubReleaseTask.IsFaulted && gitHubReleaseTask.Result != null && !string.IsNullOrEmpty(gitHubReleaseTask.Result.TagName))
                {
                    return gitHubReleaseTask.Result.TagName.StartsWith("v") ? gitHubReleaseTask.Result.TagName.Substring(1) : gitHubReleaseTask.Result.TagName;
                }
            }

            return string.Empty;
        }

        public bool RunUpdaterCheck(bool launch, out string upstreamVersion)
        {
            string destPath = Path.Combine(Global.exedirpath, "DS4Updater.exe");
            bool updaterExists = File.Exists(destPath);
            upstreamVersion = DownloadUpstreamUpdaterVersion();
            // If the updater executable does not exist, do not attempt to download it here.
            // Assume users will place DS4Updater.exe (e.g. from schmaldeo) into the program folder.
            // If updater exists, use it as-is and proceed to launch it to perform the main program update.
            if (!updaterExists)
            {
                launch = false;
            }
            else
            {
                launch = true;
            }

            return launch;
        }

        public void DownloadUpstreamVersionInfo()
        {
            // Sorry other devs, gonna have to find your own server
            Uri url = new Uri("https://api.github.com/repos/gwin7ok/DS4Windows-Vader4Pro/releases/latest");
            string filename = Global.appdatapath + "\\version.txt";
            bool success = false;
            using (StreamWriter streamWriter = new(filename, false))
            {
                Task<System.Net.Http.HttpResponseMessage> requestTask = App.requestClient.GetAsync(url.ToString());
                try
                {
                    requestTask.Wait();
                    if (requestTask.Result.IsSuccessStatusCode)
                    {
                        var gitHubReleaseTask = requestTask.Result.Content.ReadFromJsonAsync<GithubRelease>();
                        gitHubReleaseTask.Wait();
                        if (!gitHubReleaseTask.IsFaulted)
                        {
                            streamWriter.Write(gitHubReleaseTask.Result.TagName.Substring(1));
                            success = true;
                        }
                    }
                }
                catch (AggregateException) { }
            }

            if (!success && File.Exists(filename))
            {
                File.Delete(filename);
            }
        }

        public void CheckDrivers()
        {
            bool deriverinstalled = Global.IsViGEmBusInstalled();
            if (!deriverinstalled || !Global.IsRunningSupportedViGEmBus())
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = $"{Global.exelocation}";
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

        public bool LauchDS4Updater()
        {
            bool launch = false;
            using (Process p = new Process())
            {
                p.StartInfo.FileName = Path.Combine(Global.exedirpath, "DS4Updater.exe");
                bool isAdmin = Global.IsAdministrator();
                List<string> argList = new List<string>();
                argList.Add("-autolaunch");
                if (!isAdmin)
                {
                    argList.Add("-user");
                }

                // Specify current exe to have DS4Updater launch
                argList.Add("--launchExe");
                argList.Add(Global.exeFileName);

                p.StartInfo.Arguments = string.Join(" ", argList);
                if (Global.AdminNeeded())
                    p.StartInfo.Verb = "runas";

                try { launch = p.Start(); }
                catch (InvalidOperationException) { }
            }

            return launch;
        }

        public bool IsNET8Available()
        {
            return DS4Windows.Util.IsNet8DesktopRuntimeAvailable();
        }
    }
}
