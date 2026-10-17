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
using DS4Windows;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace DS4WinWPF.DS4Forms
{

    /// <summary>
    /// Interaction logic for LanguageSelectDialog.xaml
    /// </summary>
    public partial class LanguageSelectDialog : Window
    {
        public bool ChoiceMade { get; set; }
        public string SelectedCulture { get; private set; }

        // Phase6-Step10-1-2: Global.UseLang／Global.SetCulture の置換先。呼び出し側（App）が保持済みのサービスを渡す（Pure DI）。
        private readonly DS4Windows.DI.IAppSettingsService _appSettingsService;

        public LanguageSelectDialog(DS4Windows.DI.IAppSettingsService appSettingsService)
        {
            _appSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));
            InitializeComponent();
            LoadAvailableLanguages();
        }

        private void LoadAvailableLanguages()
        {
            List<LanguageInfo> languages = new List<LanguageInfo>();

            // Add English (default)
            languages.Add(new LanguageInfo
            {
                CultureCode = "en",
                DisplayName = "English",
                NativeName = "English"
            });

            // Scan for available language assemblies
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string langDir = Path.Combine(exeDir, "Lang");

            if (Directory.Exists(langDir))
            {
                var cultureDirs = Directory.GetDirectories(langDir);
                foreach (var dir in cultureDirs)
                {
                    string cultureName = Path.GetFileName(dir);
                    try
                    {
                        // Check if the language assembly exists
                        string assemblyPath = Path.Combine(dir, "DS4Windows.resources.dll");
                        if (File.Exists(assemblyPath))
                        {
                            CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
                            languages.Add(new LanguageInfo
                            {
                                CultureCode = cultureName,
                                DisplayName = culture.EnglishName,
                                NativeName = culture.NativeName
                            });
                        }
                    }
                    catch (CultureNotFoundException)
                    {
                        // Skip invalid culture names
                        continue;
                    }
                }
            }

            // Sort by display name
            languages = languages.OrderBy(l => l.DisplayName).ToList();

            languageListBox.ItemsSource = languages;

            // Default to English
            var defaultLang = languages.FirstOrDefault(l => l.CultureCode == "en");

            if (defaultLang != null)
            {
                languageListBox.SelectedItem = defaultLang;
            }
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            ApplySelection();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            ChoiceMade = false;
            Close();
        }

        private void LanguageListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (languageListBox.SelectedItem != null)
            {
                ApplySelection();
            }
        }

        private void ApplySelection()
        {
            if (languageListBox.SelectedItem is LanguageInfo selectedLang)
            {
                SelectedCulture = selectedLang.CultureCode;
                ChoiceMade = true;

                // Apply the selected culture immediately
                AppLogger.LogDebug($"LanguageSelectDialog: user selected culture '{SelectedCulture}'");
                _appSettingsService.UseLang = SelectedCulture;
                _appSettingsService.SetCulture(SelectedCulture);
                AppLogger.LogDebug($"LanguageSelectDialog: Global.UseLang set to '{_appSettingsService.UseLang}'");

                Close();
            }
        }
    }
}
