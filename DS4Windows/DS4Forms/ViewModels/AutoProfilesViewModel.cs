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
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    public class AutoProfilesViewModel
    {
        private object _colLockobj = new object();
        private ObservableCollection<ProgramItem> programColl;
        private AutoProfileHolder autoProfileHolder;
        private ProfileList profileList;
        private int selectedIndex = -1;
        private ProgramItem selectedItem;
        private HashSet<string> existingapps;
        private readonly IAppSettingsService _appSettings;
        private readonly IAutoProfileService _autoProfileService;
        private readonly ControlService _controlService;

        public ObservableCollection<ProgramItem> ProgramColl { get => programColl; }

        public AutoProfileHolder AutoProfileHolder { get => autoProfileHolder; }

        public int SelectedIndex { get => selectedIndex; set => selectedIndex = value; }
        public ProgramItem SelectedItem
        {
            get => selectedItem;
            set
            {
                selectedItem = value;
                CurrentItemChange?.Invoke(this, value);
            }
        }

        public ProfileList ProfileList { get => profileList; }

        public delegate void CurrentItemChangeHandler(AutoProfilesViewModel sender, ProgramItem item);
        public event CurrentItemChangeHandler CurrentItemChange;

        public event EventHandler SearchFinished;
        public delegate void AutoProfileStateHandler(AutoProfilesViewModel sender, bool state);
        public event AutoProfileStateHandler AutoProfileSystemChange;

        public bool RevertDefaultProfileOnUnknown
        {
            get => _appSettings.AutoProfileRevertDefaultProfile;
            set => _appSettings.AutoProfileRevertDefaultProfile = value;
        }

        // Phase7-Step10b W4: 旧 ControlService.USING_MAX_CONTROLLERS（静的シム）を IEnvironmentService.UsingMaxControllers に置き換えた。
        private readonly IEnvironmentService _environmentService;

        public bool UsingExpandedControllers
        {
            get => _environmentService.UsingMaxControllers;
        }

        private List<EnumChoiceSelection<AutoProfileDisplayProfileSwitchChoices>> displayProfileSwitchList =
            new List<EnumChoiceSelection<AutoProfileDisplayProfileSwitchChoices>>()
            {
                new EnumChoiceSelection<AutoProfileDisplayProfileSwitchChoices>(Translations.Strings.None, AutoProfileDisplayProfileSwitchChoices.None),
                new EnumChoiceSelection<AutoProfileDisplayProfileSwitchChoices>(Translations.Strings.Log, AutoProfileDisplayProfileSwitchChoices.Log),
                new EnumChoiceSelection<AutoProfileDisplayProfileSwitchChoices>(Translations.Strings.Notification, AutoProfileDisplayProfileSwitchChoices.Notification),
                new EnumChoiceSelection<AutoProfileDisplayProfileSwitchChoices>(Translations.Strings.AutoProfiles_LogAndNotification, AutoProfileDisplayProfileSwitchChoices.LogAndNotification),
            };

        public List<EnumChoiceSelection<AutoProfileDisplayProfileSwitchChoices>> DisplayProfileSwitchList => displayProfileSwitchList;

        public AutoProfileDisplayProfileSwitchChoices ProfileSwitchChoice
        {
            get => _autoProfileService.AutoProfileSwitchNotifyChoice;
            set => _autoProfileService.AutoProfileSwitchNotifyChoice = value;
        }

        // Phase6-Step10-2-3: 依存はすべて必須引数（Pure DI。決定1＝S9-A と同じ方針）。
        // 置換前は任意引数（= null）と、AppHost/Program.rootHub/Global のフォールバックだった。
        public AutoProfilesViewModel(AutoProfileHolder autoProfileHolder, ProfileList profileList,
            IAppSettingsService appSettings,
            IAutoProfileService autoProfileService,
            ControlService controlService,
            IEnvironmentService environmentService)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _autoProfileService = autoProfileService ?? throw new ArgumentNullException(nameof(autoProfileService));
            _controlService = controlService ?? throw new ArgumentNullException(nameof(controlService));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            programColl = new ObservableCollection<ProgramItem>();
            existingapps = new HashSet<string>();
            this.autoProfileHolder = autoProfileHolder;
            this.profileList = profileList;
            PopulateCurrentEntries();

            BindingOperations.EnableCollectionSynchronization(programColl, _colLockobj);
        }

        private void PopulateCurrentEntries()
        {
            foreach (AutoProfileEntity entry in autoProfileHolder.AutoProfileColl)
            {
                ProgramItem item = new ProgramItem(entry.Path, UsingExpandedControllers, entry);

                programColl.Add(item);
                existingapps.Add(entry.Path);
            }
        }

        public void RemoveUnchecked()
        {
            AutoProfileSystemChange?.Invoke(this, false);
            programColl.Clear();
            existingapps.Clear();
            PopulateCurrentEntries();
            AutoProfileSystemChange?.Invoke(this, true);
        }

        public async void AddProgramsFromStartMenu()
        {
            AutoProfileSystemChange?.Invoke(this, false);
            await Task.Run(() =>
            {
                AddFromStartMenu(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + "\\Programs");
            });

            SearchFinished?.Invoke(this, EventArgs.Empty);
            AutoProfileSystemChange?.Invoke(this, true);
        }

        public async void AddProgramsFromSteam(string location)
        {
            AutoProfileSystemChange?.Invoke(this, false);
            await Task.Run(() =>
            {
                AddAppsFromLocation(location);
            });

            SearchFinished?.Invoke(this, EventArgs.Empty);
            AutoProfileSystemChange?.Invoke(this, true);
        }

        public async void AddProgramsFromDir(string location)
        {
            AutoProfileSystemChange?.Invoke(this, false);
            await Task.Run(() =>
            {
                AddAppsFromLocation(location);
            });

            SearchFinished?.Invoke(this, EventArgs.Empty);
            AutoProfileSystemChange?.Invoke(this, true);
        }

        public async void AddProgramExeLocation(string location)
        {
            AutoProfileSystemChange?.Invoke(this, false);
            await Task.Run(() =>
            {
                AddAppExeLocation(location);
            });

            SearchFinished?.Invoke(this, EventArgs.Empty);
            AutoProfileSystemChange?.Invoke(this, true);
        }

        private void AddFromStartMenu(string path)
        {
            List<string> lnkpaths = new List<string>();
            lnkpaths.AddRange(Directory.GetFiles(path, "*.lnk", SearchOption.AllDirectories));
            lnkpaths.AddRange(Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu) + "\\Programs", "*.lnk", SearchOption.AllDirectories));
            List<string> exepaths = new List<string>();
            foreach (string link in lnkpaths)
            {
                string target = GetTargetPath(link);
                exepaths.Add(target);
            }

            ScanApps(exepaths);
        }

        private void AddAppsFromLocation(string path)
        {
            List<string> exepaths = new List<string>();
            exepaths.AddRange(Directory.GetFiles(path, "*.exe", SearchOption.AllDirectories));
            ScanApps(exepaths);
        }

        private void AddAppExeLocation(string path)
        {
            List<string> exepaths = new List<string>();
            exepaths.Add(path);
            ScanApps(exepaths, checkexisting: false, skipsetupapps: false);
        }

        private void ScanApps(List<string> exepaths, bool checkexisting = true,
            bool skipsetupapps = true)
        {
            foreach (string target in exepaths)
            {
                bool skip = !File.Exists(target) || Path.GetExtension(target).ToLower() != ".exe";
                skip = skip || (skipsetupapps && (target.Contains("etup") || target.Contains("dotnet") || target.Contains("SETUP")
                    || target.Contains("edist") || target.Contains("nstall") || string.IsNullOrEmpty(target)));
                skip = skip || (checkexisting && existingapps.Contains(target));
                if (!skip)
                {
                    ProgramItem item = new ProgramItem(target, UsingExpandedControllers);
                    /*if (autoProfileHolder.AutoProfileDict.TryGetValue(target, out AutoProfileEntity autoEntity))
                    {
                        item.MatchedAutoProfile = autoEntity;
                    }
                    */

                    programColl.Add(item);
                    existingapps.Add(target);
                }
            }
        }

        public void CreateAutoProfileEntry(ProgramItem item)
        {
            if (item.MatchedAutoProfile == null)
            {
                AutoProfileEntity tempEntry = new AutoProfileEntity(item.Path, item.Title);
                tempEntry.Turnoff = item.Turnoff;
                int tempindex = item.SelectedIndexCon1;
                tempEntry.ProfileNames[0] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name :
                    AutoProfileEntity.NONE_STRING;

                tempindex = item.SelectedIndexCon2;
                tempEntry.ProfileNames[1] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name :
                    AutoProfileEntity.NONE_STRING;

                tempindex = item.SelectedIndexCon3;
                tempEntry.ProfileNames[2] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name :
                    AutoProfileEntity.NONE_STRING;

                tempindex = item.SelectedIndexCon4;
                tempEntry.ProfileNames[3] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name :
                    AutoProfileEntity.NONE_STRING;

                if (UsingExpandedControllers)
                {
                    tempindex = item.SelectedIndexCon5;
                    tempEntry.ProfileNames[4] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name :
                        AutoProfileEntity.NONE_STRING;

                    tempindex = item.SelectedIndexCon6;
                    tempEntry.ProfileNames[5] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name :
                        AutoProfileEntity.NONE_STRING;

                    tempindex = item.SelectedIndexCon7;
                    tempEntry.ProfileNames[6] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name :
                        AutoProfileEntity.NONE_STRING;

                    tempindex = item.SelectedIndexCon8;
                    tempEntry.ProfileNames[7] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name :
                        AutoProfileEntity.NONE_STRING;
                }

                item.MatchedAutoProfile = tempEntry;
                autoProfileHolder.AutoProfileColl.Add(item.MatchedAutoProfile);
            }
        }

        public void PersistAutoProfileEntry(ProgramItem item)
        {
            if (item.MatchedAutoProfile != null)
            {
                AutoProfileEntity tempEntry = item.MatchedAutoProfile;
                int tempindex = item.SelectedIndexCon1;
                tempEntry.ProfileNames[0] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name :
                    AutoProfileEntity.NONE_STRING;

                tempindex = item.SelectedIndexCon2;
                tempEntry.ProfileNames[1] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name :
                    AutoProfileEntity.NONE_STRING;

                tempindex = item.SelectedIndexCon3;
                tempEntry.ProfileNames[2] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name :
                    AutoProfileEntity.NONE_STRING;

                tempindex = item.SelectedIndexCon4;
                tempEntry.ProfileNames[3] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name :
                    AutoProfileEntity.NONE_STRING;

                if (UsingExpandedControllers)
                {
                    tempindex = item.SelectedIndexCon5;
                    tempEntry.ProfileNames[4] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name :
                        AutoProfileEntity.NONE_STRING;

                    tempindex = item.SelectedIndexCon6;
                    tempEntry.ProfileNames[5] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name :
                        AutoProfileEntity.NONE_STRING;

                    tempindex = item.SelectedIndexCon7;
                    tempEntry.ProfileNames[6] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name :
                        AutoProfileEntity.NONE_STRING;

                    tempindex = item.SelectedIndexCon8;
                    tempEntry.ProfileNames[7] = tempindex > 0 ? profileList.ProfileListCol[tempindex - 1].Name :
                        AutoProfileEntity.NONE_STRING;
                }
            }
        }

        public void RemoveAutoProfileEntry(ProgramItem item)
        {
            autoProfileHolder.AutoProfileColl.Remove(item.MatchedAutoProfile);
            item.MatchedAutoProfile = null;
        }

        private string GetTargetPath(string filePath)
        {
            string targetPath = ResolveMsiShortcut(filePath);
            if (targetPath == null)
            {
                targetPath = ResolveShortcut(filePath);
            }

            return targetPath;
        }

        public string ResolveShortcutAndArgument(string filePath)
        {
            Type t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")); // Windows Script Host Shell Object
            dynamic shell = Activator.CreateInstance(t);
            string result;

            try
            {
                var shortcut = shell.CreateShortcut(filePath);
                result = shortcut.TargetPath + " " + shortcut.Arguments;
                Marshal.FinalReleaseComObject(shortcut);
            }
            catch (COMException)
            {
                // A COMException is thrown if the file is not a valid shortcut (.lnk) file 
                result = null;
            }
            finally
            {
                Marshal.FinalReleaseComObject(shell);
            }

            return result;
        }

        public string ResolveMsiShortcut(string file)
        {
            StringBuilder product = new StringBuilder(NativeMethods2.MaxGuidLength + 1);
            StringBuilder feature = new StringBuilder(NativeMethods2.MaxFeatureLength + 1);
            StringBuilder component = new StringBuilder(NativeMethods2.MaxGuidLength + 1);

            NativeMethods2.MsiGetShortcutTarget(file, product, feature, component);

            int pathLength = NativeMethods2.MaxPathLength;
            StringBuilder path = new StringBuilder(pathLength);

            NativeMethods2.InstallState installState = NativeMethods2.MsiGetComponentPath(product.ToString(), component.ToString(), path, ref pathLength);
            if (installState == NativeMethods2.InstallState.Local)
            {
                return path.ToString();
            }
            else
            {
                return null;
            }
        }

        public string ResolveShortcut(string filePath)
        {
            Type t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")); // Windows Script Host Shell Object
            dynamic shell = Activator.CreateInstance(t);
            string result;

            try
            {
                var shortcut = shell.CreateShortcut(filePath);
                result = shortcut.TargetPath;
                Marshal.FinalReleaseComObject(shortcut);
            }
            catch (COMException)
            {
                // A COMException is thrown if the file is not a valid shortcut (.lnk) file 
                result = null;
            }
            finally
            {
                Marshal.FinalReleaseComObject(shell);
            }

            return result;
        }

        public bool MoveItemUpDown(ProgramItem item, int moveDirection)
        {
            // Move autoprofile item up (-1) or down (1) both in listView (programColl) and in autoProfileHolder data structure (will be written into AutoProfiles.xml file)
            bool itemMoved = true;
            int oldIdx = programColl.IndexOf(item);

            if (moveDirection == -1 && oldIdx > 0 && oldIdx < autoProfileHolder.AutoProfileColl.Count)
            {
                programColl.Move(oldIdx, oldIdx - 1);
                autoProfileHolder.AutoProfileColl.Move(oldIdx, oldIdx - 1);
            }
            else if (moveDirection == 1 && oldIdx >= 0 && oldIdx < programColl.Count - 1 && oldIdx < autoProfileHolder.AutoProfileColl.Count - 1)
            {
                programColl.Move(oldIdx, oldIdx + 1);
                autoProfileHolder.AutoProfileColl.Move(oldIdx, oldIdx + 1);
            }
            else
                itemMoved = false;

            return itemMoved;
        }

        public void AddExeToHIDHideWhenSaving(ProgramItem autoProf, bool addExe)
        {
            if (autoProf.Path.Substring((autoProf.Path.Length) - 4, 4) == ".exe") //Filter out autoprofiles that do not lead to EXEs.
            {
                _controlService?.CheckHidHidePresence(autoProf.Path, autoProf.Filename, addExe);
            }
        }
    }
}