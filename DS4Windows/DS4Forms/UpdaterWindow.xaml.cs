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

using DS4WinWPF.DS4Forms.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Linq;
using System.Globalization;
using DS4Windows;

namespace DS4WinWPF.DS4Forms
{
    /// <summary>
    /// Interaction logic for UpdaterWindow.xaml
    /// </summary>
    public partial class UpdaterWindow : Window
    {
        private MessageBoxResult result = MessageBoxResult.No;
        public MessageBoxResult Result { get => result; }

        private UpdaterWindowViewModel updaterWinVM;

        public UpdaterWindow(string newversion)
        {
            InitializeComponent();

            // Keep window title localized via Properties.Resources (preserve existing Japanese title)
            Title = Properties.Resources.DS4Update;

            // Display main message and buttons in English
            var engCulture = CultureInfo.GetCultureInfo("en");
            string downloadStr = DS4WinWPF.Translations.Strings.ResourceManager.GetString("DownloadVersion", engCulture) ?? "A new version *number* has been released.";
            captionTextBlock.Text = downloadStr.Replace("*number*", newversion);
            updaterWinVM = new UpdaterWindowViewModel(newversion);
            updaterWinVM.BlankSkippedVersion();

            DataContext = updaterWinVM;

            Task.Run(async () =>
            {
                await Dispatcher.InvokeAsync(async () => await updaterWinVM.DisplayChangelog());
            });

            // Use English labels for buttons regardless of app culture
            string skipLabel = DS4WinWPF.Translations.Strings.ResourceManager.GetString("SkipVersion", engCulture) ?? "Skip Version";
            string openLabel = DS4WinWPF.Translations.Strings.ResourceManager.GetString("Install_LatestBtn", engCulture) ?? "Install Latest";
            string closeLabel = DS4WinWPF.Translations.Strings.ResourceManager.GetString("CloseButton", engCulture) ?? "Close";

            skipVersionBtn.Content = skipLabel;
            yesBtn.Content = openLabel;
            noBtn.Content = closeLabel;
        }

        private async void YesBtn_Click(object sender, RoutedEventArgs e)
        {
            // Install / Launch latest updater
            try
            {
                string ds4WindowsDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
                string ds4UpdaterDir = System.IO.Path.Combine(ds4WindowsDir, "DS4Updater");
                string ds4UpdaterExe = System.IO.Path.Combine(ds4UpdaterDir, "DS4Updater.exe");

                if (System.IO.File.Exists(ds4UpdaterExe))
                {
                    // Launch existing updater with GUI args and wait for result
                    string ds4WindowsExe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                    var psi = new System.Diagnostics.ProcessStartInfo(ds4UpdaterExe)
                    {
                        UseShellExecute = false,
                        Arguments = $"--ds4windows-path \"{ds4WindowsDir}\" --ds4updater-path \"{ds4UpdaterDir}\" -autolaunch --launchExe \"{ds4WindowsExe}\""
                    };

                    var proc = System.Diagnostics.Process.Start(psi);
                    if (proc != null)
                    {
                        proc.WaitForExit();
                        if (proc.ExitCode != 0)
                        {
                            // On failure, open releases page as fallback
                            Util.StartProcessHelper("https://github.com/gwin7ok/DS4Windows-Vader4Pro/releases/latest");
                        }
                    }
                }
                else
                {
                    // Updater not present yet: attempt A-plan download->extract->install
                    string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DS4Updater_tmp");
                    try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
                    Directory.CreateDirectory(tempDir);

                    string tempZip = System.IO.Path.Combine(tempDir, "DS4Updater_x64.zip");

                    try
                    {
                        // Determine repo for DS4Updater (default)
                        string updaterRepo = "gwin7ok/DS4Updater";

                        // Query GitHub Releases API for latest release
                        string apiUrl = $"https://api.github.com/repos/{updaterRepo}/releases/latest";
                        using (var client = new System.Net.Http.HttpClient())
                        {
                            client.DefaultRequestHeaders.UserAgent.ParseAdd("DS4Windows-Updater-Installer");
                            var apiResp = await client.GetAsync(apiUrl);
                            if (!apiResp.IsSuccessStatusCode) throw new Exception($"Failed to query releases: {apiResp.StatusCode}");

                            using var doc = System.Text.Json.JsonDocument.Parse(await apiResp.Content.ReadAsStringAsync());
                            var root = doc.RootElement;
                            if (!root.TryGetProperty("assets", out var assets)) throw new Exception("No assets in latest release");

                            string downloadUrl = null;
                            long expectedSize = -1;
                            foreach (var asset in assets.EnumerateArray())
                            {
                                if (asset.TryGetProperty("name", out var nameEl))
                                {
                                    var name = nameEl.GetString();
                                    if (name != null && name.Contains("x64") && name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (asset.TryGetProperty("browser_download_url", out var urlEl))
                                        {
                                            downloadUrl = urlEl.GetString();
                                        }
                                        if (asset.TryGetProperty("size", out var sizeEl))
                                        {
                                            expectedSize = sizeEl.GetInt64();
                                        }
                                        break;
                                    }
                                }
                            }

                            if (string.IsNullOrEmpty(downloadUrl)) throw new Exception("No suitable x64 zip asset found in latest release");

                            // Download asset and verify size against expectedSize when available
                            using (var resp = await client.GetAsync(downloadUrl))
                            {
                                if (!resp.IsSuccessStatusCode) throw new Exception($"Download failed: {resp.StatusCode}");
                                using (var fs = new System.IO.FileStream(tempZip, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                                {
                                    await resp.Content.CopyToAsync(fs);
                                }

                                if (expectedSize > 0)
                                {
                                    var fi = new System.IO.FileInfo(tempZip);
                                    if (fi.Length != expectedSize) throw new Exception($"Downloaded size mismatch: expected {expectedSize}, got {fi.Length}");
                                }
                            }
                        }

                        // Extract
                        string extractDir = System.IO.Path.Combine(tempDir, "extract");
                        System.IO.Compression.ZipFile.ExtractToDirectory(tempZip, extractDir);

                        // Find DS4Updater.exe inside extracted files
                        var exeFile = Directory.EnumerateFiles(extractDir, "DS4Updater.exe", System.IO.SearchOption.AllDirectories).FirstOrDefault();
                        if (exeFile == null) throw new Exception("DS4Updater.exe not found inside archive");

                        // Attempt non-elevated install first
                        string targetExe = System.IO.Path.Combine(ds4UpdaterDir, "DS4Updater.exe");
                        bool ok = UpdaterInstaller.TryInstall(exeFile, targetExe, allowElevation: false);
                        if (!ok)
                        {
                            // Need elevation - ask user
                            var elevateMb = MessageBox.Show(DS4WinWPF.Translations.Strings.Elevation_Body,
                                DS4WinWPF.Translations.Strings.Elevation_Title,
                                MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (elevateMb == MessageBoxResult.Yes)
                            {
                                // Caller permits elevation; attempt install which will perform elevation
                                bool elevatedOk = UpdaterInstaller.TryInstall(exeFile, targetExe, allowElevation: true);
                                if (!elevatedOk)
                                {
                                    var failMb = MessageBox.Show(DS4WinWPF.Translations.Strings.InstallFailed_Body + "\n\nOpen release page?",
                                        DS4WinWPF.Translations.Strings.InstallFailed_Title,
                                        MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                    if (failMb == MessageBoxResult.Yes)
                                        Util.StartProcessHelper("https://github.com/gwin7ok/DS4Windows-Vader4Pro/releases/latest");
                                }
                                else
                                {
                                    // Installed successfully — show success toast then launch updater
                                    ProfileNotificationWindow.ShowNotification(DS4WinWPF.Translations.Strings.InstallSuccess_Notification);
                                    string ds4WindowsExe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                                    var psi2 = new System.Diagnostics.ProcessStartInfo(targetExe)
                                    {
                                        UseShellExecute = false,
                                        Arguments = $"--ds4windows-path \"{ds4WindowsDir}\" --ds4updater-path \"{ds4UpdaterDir}\" -autolaunch --launchExe \"{ds4WindowsExe}\""
                                    };
                                    var proc2 = System.Diagnostics.Process.Start(psi2);
                                    if (proc2 != null)
                                    {
                                        proc2.WaitForExit();
                                        if (proc2.ExitCode != 0)
                                        {
                                            Util.StartProcessHelper("https://github.com/gwin7ok/DS4Windows-Vader4Pro/releases/latest");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // User declined elevation -> offer release page
                                var mb = MessageBox.Show(DS4WinWPF.Translations.Strings.UpdaterMissing_Body + "\n\nOpen release page?",
                                    DS4WinWPF.Translations.Strings.UpdaterMissing_Title,
                                    MessageBoxButton.YesNo, MessageBoxImage.Information);
                                if (mb == MessageBoxResult.Yes)
                                    Util.StartProcessHelper("https://github.com/gwin7ok/DS4Windows-Vader4Pro/releases/latest");
                            }
                        }
                        else
                        {
                            // Installed without elevation — show success toast then launch updater
                            ProfileNotificationWindow.ShowNotification(DS4WinWPF.Translations.Strings.InstallSuccess_Notification);
                            string ds4WindowsExe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                            var psi2 = new System.Diagnostics.ProcessStartInfo(targetExe)
                            {
                                UseShellExecute = false,
                                Arguments = $"--ds4windows-path \"{ds4WindowsDir}\" --ds4updater-path \"{ds4UpdaterDir}\" -autolaunch --launchExe \"{ds4WindowsExe}\""
                            };
                            var proc2 = System.Diagnostics.Process.Start(psi2);
                            if (proc2 != null)
                            {
                                proc2.WaitForExit();
                                if (proc2.ExitCode != 0)
                                {
                                    Util.StartProcessHelper("https://github.com/gwin7ok/DS4Windows-Vader4Pro/releases/latest");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            var mb = MessageBox.Show(DS4WinWPF.Translations.Strings.UpdaterMissing_Body + "\n\nOpen release page?\n\n" + ex.Message,
                                DS4WinWPF.Translations.Strings.UpdaterMissing_Title,
                                MessageBoxButton.YesNo, MessageBoxImage.Information);
                            if (mb == MessageBoxResult.Yes)
                                Util.StartProcessHelper("https://github.com/gwin7ok/DS4Windows-Vader4Pro/releases/latest");
                        }
                        catch { }
                    }
                    finally
                    {
                        try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
                    }
                }
            }
            catch
            {
                try { Util.StartProcessHelper("https://github.com/gwin7ok/DS4Windows-Vader4Pro/releases/latest"); } catch { }
            }
            result = MessageBoxResult.Yes;
            Close();
        }

        private void NoBtn_Click(object sender, RoutedEventArgs e)
        {
            result = MessageBoxResult.No;
            Close();
        }

        private void SkipVersionBtn_Click(object sender, RoutedEventArgs e)
        {
            result = MessageBoxResult.No;
            updaterWinVM.SetSkippedVersion();
            Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            DataContext = null;
        }
    }
}
