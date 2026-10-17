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
using System.IO;
using System.Windows;


namespace DS4WinWPF.DS4Forms
{
    /// <summary>
    /// Interaction logic for SaveWhere.xaml
    /// </summary>
    public partial class SaveWhere : Window
    {
        private bool multisaves;
        // Phase6-Step10-1-2: Global 直接参照の置換先。呼び出し側（App）が保持済みのサービスを渡す（Pure DI）。
        private readonly DS4Windows.DI.IPathService _pathService;
        private readonly DS4Windows.DI.IEnvironmentService _environmentService;
        public bool ChoiceMade { get; set; }

        public SaveWhere(bool multisavespots, DS4Windows.DI.IPathService pathService,
            DS4Windows.DI.IEnvironmentService environmentService)
        {
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));

            InitializeComponent();
            multisaves = multisavespots;
            if (!multisavespots)
            {
                multipleSavesDockP.Visibility = Visibility.Collapsed;
            }

            if (_environmentService.AdminNeeded())
            {
                progFolderPanel.IsEnabled = false;
            }
        }

        private void ProgFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            // SetSaveLocation は Global.SaveWhere と同じ動作（IPathService.AppDataPath の setter は使わない）
            _pathService.SetSaveLocation(_pathService.ProgramDirectory);
            if (multisaves && dontDeleteCk.IsChecked == false)
            {
                try
                {
                    if (Directory.Exists(_pathService.RoamingAppDataPath))
                    {
                        Directory.Delete(_pathService.RoamingAppDataPath, true);
                    }
                }
                catch { }
            }
            else if (!multisaves)
            {
                _pathService.CreateEmptyProfilesFile(_pathService.ProgramDirectory + "\\Profiles.xml");
            }

            ChoiceMade = true;
            Close();
        }

        private void AppdataBtn_Click(object sender, RoutedEventArgs e)
        {
            if (multisaves && dontDeleteCk.IsChecked == false)
            {
                // 置換前は Profiles／Profiles.xml／Auto Profiles.xml の 3 つだけを削除していた（Actions.xml 等が残った）。
                // 設定フォルダのアプリ管理ファイルをすべて削除する（ProgramFolderSettingsCleaner）。
                if (!ProgramFolderSettingsCleaner.DeleteSettings(_pathService.ProgramDirectory))
                {
                    MessageBox.Show("Cannot Delete old settings, please manaully delete", "DS4Windows");
                }
            }
            else if (!multisaves)
            {
                _pathService.CreateEmptyProfilesFile(Path.Combine(_pathService.RoamingAppDataPath, "Profiles.xml"));
            }

            _pathService.SetSaveLocation(_pathService.RoamingAppDataPath);
            ChoiceMade = true;
            Close();
        }
    }
}
