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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WinWPF.DS4Forms
{
    /// <summary>
    /// Interaction logic for LanguagePackControl.xaml
    /// </summary>
    public partial class LanguagePackControl : UserControl
    {
        private LanguagePackViewModel langPackVM;
        private DS4Windows.DI.IAppSettingsService _appSettingsService;

        // Phase6-Step10-1-4: 本コントロールは MainWindow.xaml から生成されるため、引数なしのコンストラクタが必要。
        // 依存は Initialize で受け取る（MainWindow のコンストラクタが、生成直後に呼ぶ）。
        public LanguagePackControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 依存を受け取り、言語パックの一覧の取得を開始する（置換前はコンストラクタで行っていた処理）。
        /// MainWindow のコンストラクタから 1 回だけ呼ぶ。
        /// </summary>
        public void Initialize(DS4Windows.DI.IAppSettingsService appSettingsService)
        {
            if (appSettingsService == null) throw new ArgumentNullException(nameof(appSettingsService));
            if (_appSettingsService != null) throw new InvalidOperationException("LanguagePackControl is already initialized.");

            _appSettingsService = appSettingsService;
            langPackVM = new LanguagePackViewModel(appSettingsService);
            this.DataContext = null;
            langPackVM.ScanFinished += LangPackVM_ScanFinished;
            langPackVM.SelectedIndexChanged += CheckForCultureChange;

            langPackVM.ScanForLangPacks();
        }

        private void CheckForCultureChange(object sender, EventArgs e)
        {
            // Enable apply button when selection changes
            applyLangBtn.IsEnabled = true;
        }

        private void ApplyLangBtn_Click(object sender, RoutedEventArgs e)
        {
            if (langPackVM.ChangeLanguagePack())
            {
                // Apply language change immediately using App.ApplyLanguageSetting
                var app = Application.Current as App;
                if (app != null)
                {
                    LangPackItem selectedItem = langPackVM.LangPackList[langPackVM.SelectedIndex];
                    string cultureCode = selectedItem.Name;
                    
                    // Apply to both Global and UI
                    app.ApplyLanguageSettingPublic(cultureCode);
                    
                    // Save to config
                    _appSettingsService.Save();
                    
                    MessageBox.Show(Translations.Strings.ResourceManager.GetString("LangPackControl.LanguageApplied") ?? "Language applied successfully",
                        "DS4Windows", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
                applyLangBtn.IsEnabled = false;
            }
        }

        private void LangPackVM_ScanFinished(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                cbCulture.IsEnabled = true;
                this.DataContext = langPackVM;
            }));
        }
    }
}
