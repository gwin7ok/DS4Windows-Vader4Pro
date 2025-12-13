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

                AppLogger.LogDebug($"[UpdaterInstall] Install Latest pressed. ds4WindowsDir={ds4WindowsDir}, ds4UpdaterDir={ds4UpdaterDir}, ds4UpdaterExe={ds4UpdaterExe}");

                    if (System.IO.File.Exists(ds4UpdaterExe))
                {
                    // Show notification first, then launch existing updater and monitor exit in background
                    try
                    {
                        DS4WinWPF.NotificationService.ShowToast(string.Empty, DS4WinWPF.Translations.Strings.InstallSuccess_Notification);
                    }
                    catch
                    {
                        AppLogger.LogToTray(DS4WinWPF.Translations.Strings.InstallSuccess_Notification);
                    }

                    string ds4WindowsExe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                    bool runningAsAdmin = DS4Windows.Global.IsAdministrator();
                    string launchMode = runningAsAdmin ? "admin" : "user";
                    var psi = new System.Diagnostics.ProcessStartInfo(ds4UpdaterExe)
                    {
                        UseShellExecute = false,
                        Arguments = $"--ds4windows-path \"{ds4WindowsDir}\" --ds4updater-path \"{ds4UpdaterDir}\" -autolaunch --launchExe \"{ds4WindowsExe}\" --launch-mode={launchMode}"
                    };

                    var proc = System.Diagnostics.Process.Start(psi);
                    AppLogger.LogDebug($"[UpdaterInstall] ProcessStartInfo: FileName={psi.FileName} Arguments={psi.Arguments} UseShellExecute={psi.UseShellExecute} Verb={psi.Verb} WorkingDirectory={psi.WorkingDirectory}");
                    if (proc != null)
                    {
                        var _ = Task.Run(() =>
                        {
                            try
                            {
                                proc.WaitForExit();
                                if (proc.ExitCode != 0)
                                {
                                    Util.StartProcessHelper("https://github.com/gwin7ok/DS4Windows-Vader4Pro/releases/latest");
                                }
                            }
                            catch { }
                        });
                    }
                }
                else
                {
                    // Updater not present yet: attempt A-plan download->extract->install
                    string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DS4Updater_tmp");
                    try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
                    Directory.CreateDirectory(tempDir);
                    AppLogger.LogDebug($"[UpdaterInstall] Temp dir prepared: {tempDir}");

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

                            AppLogger.LogDebug($"[UpdaterInstall] Found asset: url={downloadUrl} expectedSize={expectedSize}");

                            // Download asset and verify size against expectedSize when available
                            using (var resp = await client.GetAsync(downloadUrl))
                            {
                                if (!resp.IsSuccessStatusCode) throw new Exception($"Download failed: {resp.StatusCode}");
                                using (var fs = new System.IO.FileStream(tempZip, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                                {
                                    await resp.Content.CopyToAsync(fs);
                                }

                                var fi = new System.IO.FileInfo(tempZip);
                                AppLogger.LogDebug($"[UpdaterInstall] Downloaded asset to {tempZip} (size={fi.Length})");
                                if (expectedSize > 0)
                                {
                                    if (fi.Length != expectedSize) throw new Exception($"Downloaded size mismatch: expected {expectedSize}, got {fi.Length}");
                                }
                            }
                        }

                        // Extract
                        string extractDir = System.IO.Path.Combine(tempDir, "extract");
                        System.IO.Compression.ZipFile.ExtractToDirectory(tempZip, extractDir);

                        // Ensure extracted files exist
                        if (!Directory.Exists(extractDir)) throw new Exception("Extraction directory missing after unzip");

                        // Attempt non-elevated install of entire extracted directory into ds4UpdaterDir
                        AppLogger.LogDebug($"[UpdaterInstall] Attempting TryInstallDirectory (non-elevated): extractDir={extractDir} target={ds4UpdaterDir}");
                        bool ok = UpdaterInstaller.TryInstallDirectory(extractDir, ds4UpdaterDir, allowElevation: false);
                        AppLogger.LogDebug($"[UpdaterInstall] TryInstallDirectory non-elevated result: {ok}");
                        if (!ok)
                        {
                            // Need elevation - ask user
                            var elevateMb = MessageBox.Show(DS4WinWPF.Translations.Strings.Elevation_Body,
                                DS4WinWPF.Translations.Strings.Elevation_Title,
                                MessageBoxButton.YesNo, MessageBoxImage.Question);
                            AppLogger.LogDebug($"[UpdaterInstall] User elevation prompt result: {elevateMb}");
                            if (elevateMb == MessageBoxResult.Yes)
                            {
                                // Caller permits elevation; attempt install which will perform elevation
                                AppLogger.LogDebug($"[UpdaterInstall] Attempting TryInstallDirectory with elevation: {extractDir} -> {ds4UpdaterDir}");
                                bool elevatedOk = UpdaterInstaller.TryInstallDirectory(extractDir, ds4UpdaterDir, allowElevation: true);
                                AppLogger.LogDebug($"[UpdaterInstall] TryInstallDirectory with elevation result: {elevatedOk}");
                                if (!elevatedOk)
                                {
                                    AppLogger.LogDebug("[UpdaterInstall] Elevation attempt failed; prompting to open release page");
                                    var failMb = MessageBox.Show(DS4WinWPF.Translations.Strings.InstallFailed_Body + "\n\nOpen release page?",
                                        DS4WinWPF.Translations.Strings.InstallFailed_Title,
                                        MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                    AppLogger.LogDebug($"[UpdaterInstall] User chose on InstallFailed dialog: {failMb}");
                                    if (failMb == MessageBoxResult.Yes)
                                        Util.StartProcessHelper("https://github.com/gwin7ok/DS4Windows-Vader4Pro/releases/latest");
                                }
                                else
                                {
                                    // Installed successfully — use existing tray/toast notification pathway then launch updater
                                    try
                                    {
                                        DS4WinWPF.NotificationService.ShowToast(string.Empty, DS4WinWPF.Translations.Strings.InstallSuccess_Notification);
                                    }
                                    catch
                                    {
                                        AppLogger.LogToTray(DS4WinWPF.Translations.Strings.InstallSuccess_Notification);
                                    }
                                    string ds4WindowsExe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                                    bool runningAsAdmin2 = DS4Windows.Global.IsAdministrator();
                                    string launchMode2 = runningAsAdmin2 ? "admin" : "user";
                                    var psi2 = new System.Diagnostics.ProcessStartInfo(System.IO.Path.Combine(ds4UpdaterDir, "DS4Updater.exe"))
                                    {
                                        UseShellExecute = false,
                                        Arguments = $"--ds4windows-path \"{ds4WindowsDir}\" --ds4updater-path \"{ds4UpdaterDir}\" -autolaunch --launchExe \"{ds4WindowsExe}\" --launch-mode={launchMode2}"
                                    };
                                    AppLogger.LogDebug($"[UpdaterInstall] ProcessStartInfo (elevated launch): FileName={psi2.FileName} Arguments={psi2.Arguments} UseShellExecute={psi2.UseShellExecute} Verb={psi2.Verb} WorkingDirectory={psi2.WorkingDirectory}");
                                    var proc2 = System.Diagnostics.Process.Start(psi2);
                                    if (proc2 != null)
                                    {
                                        var _ = Task.Run(() =>
                                        {
                                            try
                                            {
                                                proc2.WaitForExit();
                                                AppLogger.LogDebug($"[UpdaterInstall] Updater process (elevated-launch) exited with code {proc2.ExitCode}");
                                                if (proc2.ExitCode != 0)
                                                {
                                                    Util.StartProcessHelper("https://github.com/gwin7ok/DS4Windows-Vader4Pro/releases/latest");
                                                }
                                            }
                                            catch (Exception ex) { AppLogger.LogError($"[UpdaterInstall] proc2 wait error: {ex.Message}"); }
                                        });
                                    }
                                }
                            }
                            else
                            {
                                // User declined elevation -> offer release page
                                var mb = MessageBox.Show(DS4WinWPF.Translations.Strings.UpdaterMissing_Body + "\n\nOpen release page?",
                                    DS4WinWPF.Translations.Strings.UpdaterMissing_Title,
                                    MessageBoxButton.YesNo, MessageBoxImage.Information);
                                AppLogger.LogDebug($"[UpdaterInstall] User declined elevation; chosen to open release page? {mb == MessageBoxResult.Yes}");
                                if (mb == MessageBoxResult.Yes)
                                    Util.StartProcessHelper("https://github.com/gwin7ok/DS4Windows-Vader4Pro/releases/latest");
                            }
                        }
                        else
                        {
                            // Installed without elevation — use existing tray/toast notification pathway then launch updater
                            try
                            {
                                DS4WinWPF.NotificationService.ShowToast(string.Empty, DS4WinWPF.Translations.Strings.InstallSuccess_Notification);
                            }
                            catch
                            {
                                AppLogger.LogToTray(DS4WinWPF.Translations.Strings.InstallSuccess_Notification);
                            }
                            string ds4WindowsExe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                            var psi2 = new System.Diagnostics.ProcessStartInfo(System.IO.Path.Combine(ds4UpdaterDir, "DS4Updater.exe"))
                            {
                                UseShellExecute = false,
                                Arguments = $"--ds4windows-path \"{ds4WindowsDir}\" --ds4updater-path \"{ds4UpdaterDir}\" -autolaunch --launchExe \"{ds4WindowsExe}\""
                            };
                            AppLogger.LogDebug($"[UpdaterInstall] ProcessStartInfo (non-elevated launch): FileName={psi2.FileName} Arguments={psi2.Arguments} UseShellExecute={psi2.UseShellExecute} Verb={psi2.Verb} WorkingDirectory={psi2.WorkingDirectory}");
                            var proc2 = System.Diagnostics.Process.Start(psi2);
                            if (proc2 != null)
                            {
                                var _ = Task.Run(() =>
                                {
                                    try
                                    {
                                        proc2.WaitForExit();
                                        AppLogger.LogDebug($"[UpdaterInstall] Updater process (non-elevated-launch) exited with code {proc2.ExitCode}");
                                        if (proc2.ExitCode != 0)
                                        {
                                            Util.StartProcessHelper("https://github.com/gwin7ok/DS4Windows-Vader4Pro/releases/latest");
                                        }
                                    }
                                    catch (Exception ex) { AppLogger.LogError($"[UpdaterInstall] proc2 wait error: {ex.Message}"); }
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogError($"[UpdaterInstall] Download/Extraction/Install error: {ex.Message}");
                        try
                        {
                            var mb = MessageBox.Show(DS4WinWPF.Translations.Strings.UpdaterMissing_Body + "\n\nOpen release page?\n\n" + ex.Message,
                                DS4WinWPF.Translations.Strings.UpdaterMissing_Title,
                                MessageBoxButton.YesNo, MessageBoxImage.Information);
                            AppLogger.LogDebug($"[UpdaterInstall] User selected open release on exception dialog: {mb == MessageBoxResult.Yes}");
                            if (mb == MessageBoxResult.Yes)
                                Util.StartProcessHelper("https://github.com/gwin7ok/DS4Windows-Vader4Pro/releases/latest");
                        }
                        catch (Exception ex2) { AppLogger.LogError($"[UpdaterInstall] error showing updater missing dialog: {ex2.Message}"); }
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
