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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    public class ControllerListViewModel : IDisposable
    {
        private ReaderWriterLockSlim _colListLocker = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private ObservableCollection<CompositeDeviceModel> controllerCol =
            new ObservableCollection<CompositeDeviceModel>();
        private Dictionary<int, CompositeDeviceModel> controllerDict =
            new Dictionary<int, CompositeDeviceModel>();

        public ObservableCollection<CompositeDeviceModel> ControllerCol
        { get => controllerCol; set => controllerCol = value; }

        private ProfileList profileListHolder;
        private ControlService controlService;
        private readonly IProfileSettingsService profileSettingsService;
        private readonly IProfileRepository profileRepo;
        private readonly IAppSettingsService appSettings;
        private int currentIndex;
        public int CurrentIndex { get => currentIndex; set => currentIndex = value; }
        public CompositeDeviceModel CurrentItem
        {
            get
            {
                if (currentIndex == -1) return null;
                controllerDict.TryGetValue(currentIndex, out CompositeDeviceModel item);
                return item;
            }
        }

        public Dictionary<int, CompositeDeviceModel> ControllerDict { get => controllerDict; set => controllerDict = value; }

        public ControllerListViewModel(ControlService service, ProfileList profileListHolder,
            IProfileSettingsService profileSettingsService,
            IProfileRepository profileRepo,
            IAppSettingsService appSettings)
        {
            // Phase6-Step10-2-2: 依存はすべて必須引数（Pure DI。決定1＝S9-A と同じ方針）。
            // 置換前は、任意引数の null を AppHost/Global.*Instance で補っていた。さらに、コンストラクタ本体の
            // profileRepo（引数）が null のままだと NullReferenceException になる状態だった。
            this.profileSettingsService = profileSettingsService ?? throw new ArgumentNullException(nameof(profileSettingsService));
            this.profileRepo = profileRepo ?? throw new ArgumentNullException(nameof(profileRepo));
            this.appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            this.profileListHolder = profileListHolder;
            this.controlService = service;
            service.ServiceStarted += ControllersChanged;
            service.PreServiceStop += ClearControllerList;
            service.HotplugController += Service_HotplugController;

            // Subscribe to SelectedProfile change event (スペシャルアクション対応)
            this.profileRepo.SelectedProfileChanged += Global_SelectedProfileChanged;

            int idx = 0;
            foreach (DS4Device currentDev in controlService.slotManager.ControllerColl)
            {
                CompositeDeviceModel temp = new CompositeDeviceModel(currentDev,
                    idx, profileRepo.ProfilePath[idx], profileListHolder, profileSettingsService, profileRepo);
                controllerCol.Add(temp);
                controllerDict.Add(idx, temp);
                currentDev.Removal += Controller_Removal;
                idx++;
            }

            BindingOperations.EnableCollectionSynchronization(controllerCol, _colListLocker,
                ColLockCallback);
        }

        private void ColLockCallback(IEnumerable collection, object context,
            Action accessMethod, bool writeAccess)
        {
            if (writeAccess)
            {
                using (WriteLocker locker = new WriteLocker(_colListLocker))
                {
                    accessMethod?.Invoke();
                }
            }
            else
            {
                using (ReadLocker locker = new ReadLocker(_colListLocker))
                {
                    accessMethod?.Invoke();
                }
            }
        }

        private void Service_HotplugController(ControlService sender,
            DS4Device device, int index)
        {
            using (WriteLocker readLock = new WriteLocker(_colListLocker))
            {
                if (!controllerDict.ContainsKey(index) && !device.IsRemoving)
                {
                    CompositeDeviceModel temp = new CompositeDeviceModel(device,
                        index, profileRepo.ProfilePath[index], profileListHolder, profileSettingsService, profileRepo);
                    controllerCol.Add(temp);
                    controllerDict.Add(index, temp);

                    device.Removal += Controller_Removal;
                }
            }
        }

        private void ClearControllerList(object sender, EventArgs e)
        {
            _colListLocker.EnterReadLock();
            try
            {
                foreach (CompositeDeviceModel temp in controllerCol)
                {
                    temp.Device.Removal -= Controller_Removal;
                }
            }
            finally
            {
                _colListLocker.ExitReadLock();
            }

            _colListLocker.EnterWriteLock();
            try
            {
                controllerCol.Clear();
                controllerDict.Clear();
            }
            finally
            {
                _colListLocker.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            try
            {
                ClearControllerList(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                AppLogger.LogToGui($"ControllerListViewModel.Dispose: ClearControllerList failed: {ex}", true);
            }
            finally
            {
                controlService.ServiceStarted -= ControllersChanged;
                controlService.PreServiceStop -= ClearControllerList;
                controlService.HotplugController -= Service_HotplugController;
                profileRepo.SelectedProfileChanged -= Global_SelectedProfileChanged;
            }
        }

        private void ControllersChanged(object sender, EventArgs e)
        {
            using (ReadLocker locker = new ReadLocker(controlService.slotManager.CollectionLocker))
            {
                foreach (DS4Device currentDev in controlService.slotManager.ControllerColl)
                {
                    bool found = false;
                    _colListLocker.EnterReadLock();
                    foreach (CompositeDeviceModel temp in controllerCol)
                    {
                        if (temp.Device == currentDev)
                        {
                            found = true;
                            break;
                        }
                    }
                    _colListLocker.ExitReadLock();

                    if (!found && !currentDev.IsRemoving)
                    {
                        _colListLocker.EnterWriteLock();
                        int idx = controlService.slotManager.ReverseControllerDict[currentDev];
                        CompositeDeviceModel temp = new CompositeDeviceModel(currentDev,
                            idx, profileRepo.ProfilePath[idx], profileListHolder, profileSettingsService, profileRepo);
                        controllerCol.Add(temp);
                        controllerDict.Add(idx, temp);
                        _colListLocker.ExitWriteLock();

                        currentDev.Removal += Controller_Removal;
                    }
                }
            }
        }

        private void Controller_Removal(object sender, EventArgs e)
        {
            DS4Device currentDev = sender as DS4Device;
            CompositeDeviceModel found = null;
            _colListLocker.EnterReadLock();
            foreach (CompositeDeviceModel temp in controllerCol)
            {
                if (temp.Device == currentDev)
                {
                    found = temp;
                    break;
                }
            }
            _colListLocker.ExitReadLock();

            if (found != null)
            {
                _colListLocker.EnterWriteLock();
                controllerCol.Remove(found);
                controllerDict.Remove(found.DevIndex);
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    appSettings?.Save();
                });
                profileSettingsService.SetLinkedProfileCheck(found.DevIndex, false);
                _colListLocker.ExitWriteLock();
            }
        }

        // Handle SelectedProfile change from special actions
        private void Global_SelectedProfileChanged(object sender, SelectedProfileChangedEventArgs e)
        {
            try
            {
                int deviceIndex = e.DeviceIndex;
                string profileName = e.ProfileName;

                AppLogger.LogDebug($"Global_SelectedProfileChanged: deviceIndex={deviceIndex}, profileName='{profileName}'");

                // Update UI (use BeginInvoke to avoid deadlock when already on UI thread)
                System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        AppLogger.LogDebug($"Global_SelectedProfileChanged: Dispatcher callback started for device {deviceIndex}");

                        if (controllerDict.ContainsKey(deviceIndex))
                        {
                            CompositeDeviceModel item = controllerDict[deviceIndex];
                            AppLogger.LogDebug($"Global_SelectedProfileChanged: Found controller item for device {deviceIndex}");

                            ProfileEntity newProfile = profileListHolder.ProfileListCol.SingleOrDefault(x => x.Name == profileName);
                            if (newProfile != null)
                            {
                                int newIndex = profileListHolder.ProfileListCol.IndexOf(newProfile);
                                AppLogger.LogDebug($"Global_SelectedProfileChanged: Profile '{profileName}' found at index {newIndex}, current SelectedIndex={item.SelectedIndex}");

                                if (item.SelectedIndex != newIndex)
                                {
                                    // ★統合抑制フラグ: WPF ComboBox の SelectionChanged 完了まで確実に抑制状態を保持
                                    item.suppressSelectedIndexChanged = true;
                                    item.SelectedIndex = newIndex;

                                    System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                                    {
                                        item.suppressSelectedIndexChanged = false;
                                    }), System.Windows.Threading.DispatcherPriority.ContextIdle);

                                    AppLogger.LogDebug($"Global_SelectedProfileChanged: Updated SelectedIndex to {newIndex}");
                                }
                            }
                            else
                            {
                                AppLogger.LogDebug($"Global_SelectedProfileChanged: Profile '{profileName}' not found in profile list");
                            }

                            item.RaiseLightColorChanged();
                            AppLogger.LogDebug($"Global_SelectedProfileChanged: LightColorChanged event fired for device {deviceIndex}");
                            AppLogger.LogDebug($"Global_SelectedProfileChanged: Completed successfully for device {deviceIndex}");
                        }
                        else
                        {
                            AppLogger.LogDebug($"Global_SelectedProfileChanged: Controller item not found for device {deviceIndex}");
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogToGui($"ERROR in Global_SelectedProfileChanged Dispatcher callback: {ex.Message}", true);
                        AppLogger.LogDebug($"Global_SelectedProfileChanged Dispatcher exception: {ex}");
                    }
                });

                AppLogger.LogDebug($"Global_SelectedProfileChanged: BeginInvoke queued for device {deviceIndex}");
            }
            catch (Exception ex)
            {
                AppLogger.LogToGui($"ERROR in Global_SelectedProfileChanged: {ex.Message}", true);
                AppLogger.LogDebug($"Global_SelectedProfileChanged exception: {ex}");
            }
        }
    }
}