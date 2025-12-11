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
            string openLabel = DS4WinWPF.Translations.Strings.ResourceManager.GetString("OpenReleasePageButton", engCulture) ?? "Open Release Page";
            string closeLabel = DS4WinWPF.Translations.Strings.ResourceManager.GetString("CloseButton", engCulture) ?? "Close";

            skipVersionBtn.Content = skipLabel;
            yesBtn.Content = openLabel;
            noBtn.Content = closeLabel;
        }

        private void YesBtn_Click(object sender, RoutedEventArgs e)
        {
            // Open the releases page in user's browser and close
            try
            {
                Util.StartProcessHelper("https://github.com/gwin7ok/DS4Windows-Vader4Pro/releases/latest");
            }
            catch { }
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
