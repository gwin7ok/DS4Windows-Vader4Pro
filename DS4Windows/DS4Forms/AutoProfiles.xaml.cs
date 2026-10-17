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
using System.IO;
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
using System.ComponentModel;
using Ookii.Dialogs.Wpf;
using DS4WinWPF.DS4Forms.ViewModels;
using Microsoft.Win32;

namespace DS4WinWPF.DS4Forms
{
    /// <summary>
    /// Interaction logic for AutoProfiles.xaml
    /// </summary>
    public partial class AutoProfiles : UserControl
    {
        // Phase6-Step10-2-3: 本コントロールは MainWindow.xaml から生成されるため、引数なしのコンストラクタが必要。
        // 依存は Initialize で受け取る（MainWindow のコンストラクタが、AutoProfileHolder を読む前に 1 回だけ呼ぶ）。
        private DS4Windows.DI.IPathService pathService;
        private DS4Windows.DI.IAppSettingsService appSettingsService;
        private DS4Windows.DI.IAutoProfileService autoProfileService;
        private DS4Windows.DI.IViewModelFactory viewModelFactory;
        protected String m_Profile;
        public const string steamCommx86Loc = @"C:\Program Files (x86)\Steam\steamapps\common";
        public const string steamCommLoc = @"C:\Program Files\Steam\steamapps\common";
        private string steamgamesdir;
        private AutoProfilesViewModel autoProfVM;
        private AutoProfileHolder autoProfileHolder;
        private ProfileList profileList;
        private bool autoDebug;

        public AutoProfileHolder AutoProfileHolder
        {
            get => autoProfileHolder;
            set => autoProfileHolder = value;
        }
        public AutoProfilesViewModel AutoProfVM { get => autoProfVM; }
        public bool AutoDebug { get => autoDebug; }
        public event EventHandler AutoDebugChanged;

        public AutoProfiles()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 依存を受け取り、画面の初期化（保存先ファイルの作成、Steam フォルダの判定、AutoProfileHolder の取得、
        /// 行数の調整）を行う。置換前はコンストラクタで行っていた処理で、順序は同じ。
        /// MainWindow のコンストラクタから、AutoProfileHolder を読む前に 1 回だけ呼ぶ。
        /// </summary>
        public void Initialize(DS4Windows.DI.IPathService pathService,
            DS4Windows.DI.IAppSettingsService appSettingsService,
            DS4Windows.DI.IAutoProfileService autoProfileService,
            DS4Windows.DI.IViewModelFactory viewModelFactory)
        {
            if (this.pathService != null) throw new InvalidOperationException("AutoProfiles is already initialized.");
            this.pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            this.appSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));
            this.autoProfileService = autoProfileService ?? throw new ArgumentNullException(nameof(autoProfileService));
            this.viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));

            m_Profile = pathService.AppDataPath + "\\Auto Profiles.xml";

            if (!File.Exists(pathService.AppDataPath + @"\Auto Profiles.xml"))
                autoProfileService.CreateAutoProfilesFile(m_Profile);

            //LoadP();

            if (appSettingsService.UseCustomSteamFolder &&
                Directory.Exists(appSettingsService.CustomSteamFolder))
                steamgamesdir = appSettingsService.CustomSteamFolder;
            else if (Directory.Exists(steamCommx86Loc))
                steamgamesdir = steamCommx86Loc;
            else if (Directory.Exists(steamCommLoc))
                steamgamesdir = steamCommLoc;
            else
                addProgramsBtn.ContextMenu.Items.Remove(steamMenuItem);

            // Phase5-Step13-4: IAutoProfileServiceが監視に使用している唯一のHolderを共有する。
            // 独自に new AutoProfileHolder() すると、本画面での編集・保存がバックグラウンド監視
            // (AutoProfileService.CheckProfiles)に反映されない二重インスタンス問題が発生するため。
            autoProfileHolder = autoProfileService.Holder;

            int currentRowCount = autoProfilesGrid.RowDefinitions.Count;
            if (currentRowCount > DS4Windows.ControlService.CURRENT_DS4_CONTROLLER_LIMIT)
            {
                for (int i = currentRowCount - 1; i >= DS4Windows.ControlService.CURRENT_DS4_CONTROLLER_LIMIT; i--)
                {
                    autoProfilesGrid.RowDefinitions.RemoveAt(i);
                }
            }
        }

        public void SetupDataContext(ProfileList profileList)
        {
            autoProfVM = viewModelFactory.CreateAutoProfilesViewModel(autoProfileHolder, profileList);
            programListLV.DataContext = autoProfVM;
            programListLV.ItemsSource = autoProfVM.ProgramColl;

            revertDefaultProfileOnUnknownCk.DataContext = autoProfVM;

            autoProfVM.SearchFinished += AutoProfVM_SearchFinished;
            autoProfVM.CurrentItemChange += AutoProfVM_CurrentItemChange;

            //autoProfilesGrid.DataContext = autoProfVM;
            outerEditControlsPanel.DataContext = autoProfVM;
            editControlsPanel.DataContext = autoProfVM.SelectedItem;
            this.profileList = profileList;

            // Sort auto profile list by application file name
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(programListLV.ItemsSource);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription("Filename", ListSortDirection.Ascending));
            view.Refresh();

            sidebarScrollViewer.ScrollToTop();
        }

        private void AutoProfVM_CurrentItemChange(AutoProfilesViewModel sender, ProgramItem item)
        {
            if (item != null)
            {
                if (item.MatchedAutoProfile != null)
                {
                    ProfileEntity tempProf = null;
                    string profileName = item.MatchedAutoProfile.ProfileNames[0];
                    if (!string.IsNullOrEmpty(profileName) && profileName != "(none)")
                    {
                        tempProf = profileList.ProfileListCol.SingleOrDefault(x => x.Name == profileName);
                        if (tempProf != null)
                        {
                            item.SelectedIndexCon1 = profileList.ProfileListCol.IndexOf(tempProf) + 1;
                        }
                    }
                    else
                    {
                        item.SelectedIndexCon1 = 0;
                    }

                    profileName = item.MatchedAutoProfile.ProfileNames[1];
                    if (!string.IsNullOrEmpty(profileName) && profileName != "(none)")
                    {
                        tempProf = profileList.ProfileListCol.SingleOrDefault(x => x.Name == profileName);
                        if (tempProf != null)
                        {
                            item.SelectedIndexCon2 = profileList.ProfileListCol.IndexOf(tempProf) + 1;
                        }
                    }
                    else
                    {
                        item.SelectedIndexCon2 = 0;
                    }

                    profileName = item.MatchedAutoProfile.ProfileNames[2];
                    if (!string.IsNullOrEmpty(profileName) && profileName != "(none)")
                    {
                        tempProf = profileList.ProfileListCol.SingleOrDefault(x => x.Name == profileName);
                        if (tempProf != null)
                        {
                            item.SelectedIndexCon3 = profileList.ProfileListCol.IndexOf(tempProf) + 1;
                        }
                    }
                    else
                    {
                        item.SelectedIndexCon3 = 0;
                    }

                    profileName = item.MatchedAutoProfile.ProfileNames[3];
                    if (!string.IsNullOrEmpty(profileName) && profileName != "(none)")
                    {
                        tempProf = profileList.ProfileListCol.SingleOrDefault(x => x.Name == profileName);
                        if (tempProf != null)
                        {
                            item.SelectedIndexCon4 = profileList.ProfileListCol.IndexOf(tempProf) + 1;
                        }
                    }
                    else
                    {
                        item.SelectedIndexCon4 = 0;
                    }

                    if (autoProfVM.UsingExpandedControllers)
                    {
                        profileName = item.MatchedAutoProfile.ProfileNames[4];
                        if (!string.IsNullOrEmpty(profileName) && profileName != "(none)")
                        {
                            tempProf = profileList.ProfileListCol.SingleOrDefault(x => x.Name == profileName);
                            if (tempProf != null)
                            {
                                item.SelectedIndexCon5 = profileList.ProfileListCol.IndexOf(tempProf) + 1;
                            }
                        }
                        else
                        {
                            item.SelectedIndexCon5 = 0;
                        }

                        profileName = item.MatchedAutoProfile.ProfileNames[5];
                        if (!string.IsNullOrEmpty(profileName) && profileName != "(none)")
                        {
                            tempProf = profileList.ProfileListCol.SingleOrDefault(x => x.Name == profileName);
                            if (tempProf != null)
                            {
                                item.SelectedIndexCon6 = profileList.ProfileListCol.IndexOf(tempProf) + 1;
                            }
                        }
                        else
                        {
                            item.SelectedIndexCon6 = 0;
                        }

                        profileName = item.MatchedAutoProfile.ProfileNames[6];
                        if (!string.IsNullOrEmpty(profileName) && profileName != "(none)")
                        {
                            tempProf = profileList.ProfileListCol.SingleOrDefault(x => x.Name == profileName);
                            if (tempProf != null)
                            {
                                item.SelectedIndexCon7 = profileList.ProfileListCol.IndexOf(tempProf) + 1;
                            }
                        }
                        else
                        {
                            item.SelectedIndexCon7 = 0;
                        }

                        profileName = item.MatchedAutoProfile.ProfileNames[7];
                        if (!string.IsNullOrEmpty(profileName) && profileName != "(none)")
                        {
                            tempProf = profileList.ProfileListCol.SingleOrDefault(x => x.Name == profileName);
                            if (tempProf != null)
                            {
                                item.SelectedIndexCon8 = profileList.ProfileListCol.IndexOf(tempProf) + 1;
                            }
                        }
                        else
                        {
                            item.SelectedIndexCon8 = 0;
                        }
                    }
                }

                editControlsPanel.DataContext = item;
                editControlsPanel.IsEnabled = true;
            }
            else
            {
                editControlsPanel.DataContext = null;
                editControlsPanel.IsEnabled = false;

                cont1AutoProf.SelectedIndex = 0;
                cont2AutoProf.SelectedIndex = 0;
                cont3AutoProf.SelectedIndex = 0;
                cont4AutoProf.SelectedIndex = 0;
                cont5AutoProf.SelectedIndex = 0;
                cont6AutoProf.SelectedIndex = 0;
                cont7AutoProf.SelectedIndex = 0;
                cont8AutoProf.SelectedIndex = 0;
            }
        }

        private void AutoProfVM_SearchFinished(object sender, EventArgs e)
        {
            IsEnabled = true;
        }

        private void SteamMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            steamMenuItem.Visibility = Visibility.Collapsed;
            programListLV.ItemsSource = null;
            autoProfVM.SearchFinished += AppsSearchFinished;
            autoProfVM.AddProgramsFromSteam(steamgamesdir);
        }

        private void BrowseProgsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog() == true)
            {
                //browseProgsMenuItem.Visibility = Visibility.Collapsed;
                programListLV.ItemsSource = null;
                autoProfVM.SearchFinished += AppsSearchFinished;
                autoProfVM.AddProgramsFromDir(dialog.SelectedPath);
            }
            else
            {
                this.IsEnabled = true;
            }
        }

        private void StartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            startMenuItem.Visibility = Visibility.Collapsed;
            programListLV.ItemsSource = null;
            autoProfVM.SearchFinished += AppsSearchFinished;
            autoProfVM.AddProgramsFromStartMenu();
        }

        private void AppsSearchFinished(object sender, EventArgs e)
        {
            autoProfVM.SearchFinished -= AppsSearchFinished;
            programListLV.ItemsSource = autoProfVM.ProgramColl;
        }

        private void AddProgramsBtn_Click(object sender, RoutedEventArgs e)
        {
            addProgramsBtn.ContextMenu.IsOpen = true;
            e.Handled = true;
        }

        private void HideUncheckedBtn_Click(object sender, RoutedEventArgs e)
        {
            programListLV.ItemsSource = null;
            autoProfVM.RemoveUnchecked();
            steamMenuItem.Visibility = Visibility.Visible;
            startMenuItem.Visibility = Visibility.Visible;
            browseProgsMenuItem.Visibility = Visibility.Visible;
            programListLV.ItemsSource = autoProfVM.ProgramColl;
        }

        private void ShowAutoDebugCk_Click(object sender, RoutedEventArgs e)
        {
            bool state = showAutoDebugCk.IsChecked == true;
            autoDebug = state;
            AutoDebugChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RemoveAutoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (autoProfVM.SelectedItem != null)
            {
                editControlsPanel.DataContext = null;
                autoProfVM.AddExeToHIDHideWhenSaving(autoProfVM.SelectedItem, false);
                autoProfVM.RemoveAutoProfileEntry(autoProfVM.SelectedItem);
                autoProfVM.AutoProfileHolder.Save(m_Profile);
                autoProfVM.SelectedItem = null;
            }
        }

        private void SaveAutoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (autoProfVM.SelectedItem != null)
            {
                if (autoProfVM.SelectedItem.MatchedAutoProfile == null)
                {
                    autoProfVM.CreateAutoProfileEntry(autoProfVM.SelectedItem);
                }
                else
                {
                    autoProfVM.PersistAutoProfileEntry(autoProfVM.SelectedItem);
                }

                autoProfVM.AddExeToHIDHideWhenSaving(autoProfVM.SelectedItem, autoProfVM.SelectedItem.Turnoff);
                autoProfVM.AutoProfileHolder.Save(m_Profile);
            }
        }

        private void BrowseAddProgMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.AddExtension = true;
            dialog.DefaultExt = ".exe";
            dialog.Filter = "Program (*.exe)|*.exe";
            dialog.Title = "Select Program";

            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (dialog.ShowDialog() == true)
            {
                programListLV.ItemsSource = null;
                autoProfVM.SearchFinished += AppsSearchFinished;
                autoProfVM.AddProgramExeLocation(dialog.FileName);
            }
            else
            {
                this.IsEnabled = true;
            }
        }

        private void MoveUpDownAutoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (autoProfVM.SelectedItem != null && sender != null)
            {
                if (autoProfVM.MoveItemUpDown(autoProfVM.SelectedItem, ((sender as MenuItem).Name == "MoveUp") ? -1 : 1))
                    autoProfVM.AutoProfileHolder.Save(m_Profile);
            }
        }
    }
}