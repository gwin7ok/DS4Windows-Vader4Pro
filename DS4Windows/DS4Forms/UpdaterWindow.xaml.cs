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

        private void YesBtn_Click(object sender, RoutedEventArgs e)
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
                    // Updater not present yet: ask user to open releases or cancel
                    var mb = MessageBox.Show(DS4WinWPF.Translations.Strings.UpdaterMissing_Body + "\n\nOpen release page?",
                        DS4WinWPF.Translations.Strings.UpdaterMissing_Title,
                        MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (mb == MessageBoxResult.Yes)
                    {
                        Util.StartProcessHelper("https://github.com/gwin7ok/DS4Windows-Vader4Pro/releases/latest");
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
