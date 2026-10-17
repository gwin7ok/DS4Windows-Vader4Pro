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

    public class CompositeDeviceModel : INotifyPropertyChanged
    {
        private DS4Device device;
        private string selectedProfile;
        private ProfileList profileListHolder;
        private ProfileEntity selectedEntity;
        private int selectedIndex = -1;
        internal bool suppressSelectedIndexChanged = false; // ★統合抑制フラグ（Global_SelectedProfileChangedからアクセス可能）
        private int devIndex;
        private readonly IProfileSettingsService profileSettingsService;
        private readonly IProfileRepository profileRepo;

        public event PropertyChangedEventHandler PropertyChanged;

        public DS4Device Device { get => device; set => device = value; }
        public string SelectedProfile { get => selectedProfile; set => selectedProfile = value; }
        public ProfileList ProfileEntities { get => profileListHolder; set => profileListHolder = value; }
        public ObservableCollection<ProfileEntity> ProfileListCol => profileListHolder.ProfileListCol;

        public string LightColor
        {
            get
            {
                DS4Color color;
                if (profileSettingsService.LightbarSettingsInfo[devIndex].ds4winSettings.useCustomLed)
                {
                    color = profileSettingsService.LightbarSettingsInfo[devIndex].ds4winSettings.m_CustomLed;
                }
                else
                {
                    color = profileSettingsService.LightbarSettingsInfo[devIndex].ds4winSettings.m_Led;
                }
                return $"#FF{color.red.ToString("X2")}{color.green.ToString("X2")}{color.blue.ToString("X2")}";
            }
        }

        public event EventHandler LightColorChanged;

        public Color CustomLightColor
        {
            get
            {
                DS4Color color;
                color = profileSettingsService.LightbarSettingsInfo[devIndex].ds4winSettings.m_CustomLed;
                return new Color() { R = color.red, G = color.green, B = color.blue, A = 255 };
            }
        }

        public string BatteryState
        {
            get
            {
                string temp = $"{device.Battery}%{(device.Charging ? "+" : "")}";
                return temp;
            }
        }
        public event EventHandler BatteryStateChanged;

        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                if (selectedIndex == value) return;
                selectedIndex = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedIndex)));

                if (!suppressSelectedIndexChanged)
                {
                    SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public event EventHandler SelectedIndexChanged;

        public string StatusSource
        {
            get
            {
                string imgName = (string)App.Current.FindResource(device.ConnectionType == ConnectionType.USB ? "UsbImg" : "BtImg");
                string source = $"{ResourcePaths.ResourcesPrefix}/{imgName}";
                return source;
            }
        }

        public string ExclusiveSource
        {
            get
            {
                string imgName = (string)App.Current.FindResource("CancelImg");
                string source = $"{ResourcePaths.ResourcesPrefix}/{imgName}";
                switch (device.CurrentExclusiveStatus)
                {
                    case DS4Device.ExclusiveStatus.Exclusive:
                        imgName = (string)App.Current.FindResource("CheckedImg");
                        source = $"{ResourcePaths.ResourcesPrefix}/{imgName}";
                        break;
                    case DS4Device.ExclusiveStatus.HidHideAffected:
                    case DS4Device.ExclusiveStatus.HidGuardAffected:
                        imgName = (string)App.Current.FindResource("KeyImageImg");
                        source = $"{ResourcePaths.ResourcesPrefix}/{imgName}";
                        break;
                    default:
                        break;
                }

                return source;
            }
        }

        public bool LinkedProfile
        {
            get
            {
                return profileSettingsService.GetLinkedProfileCheck(devIndex);
            }
            set
            {
                bool temp = profileSettingsService.GetLinkedProfileCheck(devIndex);
                if (temp == value) return;
                profileSettingsService.SetLinkedProfileCheck(devIndex, value);
                SaveLinked(value);
            }
        }
        public event EventHandler LinkedProfileChanged;

        public void RaiseLinkedProfileChanged()
        {
            LinkedProfileChanged?.Invoke(this, EventArgs.Empty);
        }

        public string SelectedProfileName
        {
            get => profileRepo.SelectedProfile[devIndex];
            set
            {
                if (profileRepo.SelectedProfile[devIndex] == value) return;
                profileRepo.SelectedProfile[devIndex] = value;
                profileRepo.ProfilePath[devIndex] = value;
                profileRepo.OlderProfilePath[devIndex] = value;
                SelectedProfileChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler SelectedProfileChanged;

        public ProfileEntity LinkedProfileName
        {
            get
            {
                string linkedProfileUI = profileRepo.LinkedProfileUI[devIndex];
                if (!string.IsNullOrEmpty(linkedProfileUI))
                {
                    return profileListHolder.ProfileListCol.SingleOrDefault(x => x.Name == linkedProfileUI);
                }
                return null;
            }
            set
            {
                string newValue = value?.Name ?? string.Empty;
                if (profileRepo.LinkedProfileUI[devIndex] == newValue) return;

                profileRepo.LinkedProfileUI[devIndex] = newValue;

                if (profileSettingsService.GetLinkedProfileCheck(devIndex) && device?.isValidSerial() == true)
                {
                    if (!string.IsNullOrEmpty(newValue))
                    {
                        profileRepo.ChangeLinkedProfile(device.getMacAddress(), newValue);
                        profileRepo.SaveLinkedProfiles();
                    }
                }

                LinkedProfileNameChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler LinkedProfileNameChanged;

        public bool LinkEnabled
        {
            get => profileSettingsService.GetLinkedProfileCheck(devIndex);
            set
            {
                bool temp = profileSettingsService.GetLinkedProfileCheck(devIndex);
                if (temp == value) return;

                AppLogger.LogDebug($"LinkEnabled setter: device={devIndex}, oldValue={temp}, newValue={value}");
                profileSettingsService.SetLinkedProfileCheck(devIndex, value);

                if (value)
                {
                    string linkedValue = profileRepo.LinkedProfileUI[devIndex];
                    if (string.IsNullOrEmpty(linkedValue))
                    {
                        linkedValue = profileRepo.SelectedProfile[devIndex];
                        AppLogger.LogDebug($"LinkEnabled ON: LinkedProfileUI is empty, using SelectedProfile '{linkedValue}' for device {devIndex}");
                        profileRepo.LinkedProfileUI[devIndex] = linkedValue;
                    }

                    AppLogger.LogDebug($"LinkEnabled ON: Registering profile '{linkedValue}' for device {devIndex}");

                    if (!string.IsNullOrEmpty(linkedValue) && device?.isValidSerial() == true)
                    {
                        profileRepo.ChangeLinkedProfile(device.getMacAddress(), linkedValue);
                        profileRepo.SaveLinkedProfiles();
                        AppLogger.LogDebug($"LinkEnabled ON: Saved to LinkedProfiles.xml for MAC={device.getMacAddress()}");
                    }
                    else
                    {
                        AppLogger.LogDebug($"LinkEnabled ON: Could not save - linkedValue='{linkedValue}', validSerial={device?.isValidSerial()}");
                    }

                    LinkedProfileNameChanged?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    AppLogger.LogDebug($"LinkEnabled OFF: Removing from LinkedProfiles.xml for device {devIndex}");
                    if (device?.isValidSerial() == true)
                    {
                        profileRepo.RemoveLinkedProfile(device.getMacAddress());
                        profileRepo.SaveLinkedProfiles();
                        AppLogger.LogDebug($"LinkEnabled OFF: Removed from LinkedProfiles.xml for MAC={device.getMacAddress()}");
                    }

                    AppLogger.LogDebug($"LinkEnabled OFF: Keeping LinkedProfileUI value '{profileRepo.LinkedProfileUI[devIndex]}' for device {devIndex}");
                }

                LinkedProfileChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public int DevIndex { get => devIndex; }
        public int DisplayDevIndex { get => devIndex + 1; }

        public string TooltipIDText
        {
            get
            {
                string temp = string.Format(Properties.Resources.InputDelay, device.Latency);
                return temp;
            }
        }

        public event EventHandler TooltipIDTextChanged;

        private bool useCustomColor;
        public bool UseCustomColor { get => useCustomColor; set => useCustomColor = value; }

        private ContextMenu lightContext;
        public ContextMenu LightContext { get => lightContext; set => lightContext = value; }

        public string IdText
        {
            get => $"{device.DisplayName} ({device.MacAddress})";
        }
        public event EventHandler IdTextChanged;

        public string IsExclusiveText
        {
            get
            {
                if (device.CurrentExclusiveStatus == DS4Device.ExclusiveStatus.Exclusive)
                {
                    return Translations.Strings.ExclusiveAccess;
                }
                else if (device.CurrentExclusiveStatus == DS4Device.ExclusiveStatus.HidHideAffected)
                {
                    return Translations.Strings.HidHideAccess;
                }
                else if (device.CurrentExclusiveStatus == DS4Device.ExclusiveStatus.HidGuardAffected)
                {
                    return Translations.Strings.HidGuardianAccess;
                }

                return Translations.Strings.SharedAccess;
            }
        }

        public bool PrimaryDevice
        {
            get => device.PrimaryDevice;
        }

        public delegate void CustomColorHandler(CompositeDeviceModel sender);
        public event CustomColorHandler RequestColorPicker;

        public CompositeDeviceModel(DS4Device device, int devIndex, string profile,
            ProfileList collection, IProfileSettingsService profileSettingsService,
            IProfileRepository profileRepo)
        {
            // Phase6-Step10-2-2: 必須引数（null は拒否する）
            this.profileSettingsService = profileSettingsService ?? throw new ArgumentNullException(nameof(profileSettingsService));
            this.profileRepo = profileRepo ?? throw new ArgumentNullException(nameof(profileRepo));
            this.device = device;
            device.BatteryChanged += (sender, e) => BatteryStateChanged?.Invoke(this, e);
            device.ChargingChanged += (sender, e) => BatteryStateChanged?.Invoke(this, e);
            device.MacAddressChanged += (sender, e) => IdTextChanged?.Invoke(this, e);
            this.devIndex = devIndex;
            this.selectedProfile = profile;
            profileListHolder = collection;
            if (!string.IsNullOrEmpty(selectedProfile))
            {
                this.selectedEntity = profileListHolder.ProfileListCol.SingleOrDefault(x => x.Name == selectedProfile);
            }

            if (this.selectedEntity != null)
            {
                selectedIndex = profileListHolder.ProfileListCol.IndexOf(this.selectedEntity);
                HookEvents(true);
            }

            useCustomColor = profileSettingsService.LightbarSettingsInfo[devIndex].ds4winSettings.useCustomLed;
        }

        public void HookEvents(bool state)
        {
            if (state)
            {
                selectedEntity.ProfileDeleted += SelectedEntity_ProfileDeleted;
            }
            else
            {
                selectedEntity.ProfileDeleted -= SelectedEntity_ProfileDeleted;
            }
        }

        private void SelectedEntity_ProfileDeleted(object sender, EventArgs e)
        {
            HookEvents(false);
            ProfileEntity entity = profileListHolder.ProfileListCol.FirstOrDefault();
            if (entity != null)
            {
                SelectedIndex = profileListHolder.ProfileListCol.IndexOf(entity);
            }
        }

        public void RequestUpdatedTooltipID()
        {
            TooltipIDTextChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SaveLinked(bool status)
        {
            if (device != null && device.isSynced())
            {
                if (status)
                {
                    if (device.isValidSerial())
                    {
                        profileRepo.ChangeLinkedProfile(device.getMacAddress(), profileRepo.ProfilePath[devIndex]);
                    }
                }
                else
                {
                    profileRepo.RemoveLinkedProfile(device.getMacAddress());
                    profileRepo.ProfilePath[devIndex] = profileRepo.OlderProfilePath[devIndex];
                }

                profileRepo.SaveLinkedProfiles();
            }
        }

        public void AddLightContextItems()
        {
            MenuItem thing = new MenuItem() { Header = "Use Profile Color", IsChecked = !useCustomColor };
            thing.Click += ProfileColorMenuClick;
            lightContext.Items.Add(thing);
            thing = new MenuItem() { Header = "Use Custom Color", IsChecked = useCustomColor };
            thing.Click += CustomColorItemClick;
            lightContext.Items.Add(thing);
        }

        private void ProfileColorMenuClick(object sender, System.Windows.RoutedEventArgs e)
        {
            useCustomColor = false;
            RefreshLightContext();
            profileSettingsService.LightbarSettingsInfo[devIndex].ds4winSettings.useCustomLed = false;
            LightColorChanged?.Invoke(this, EventArgs.Empty);
        }

        private void CustomColorItemClick(object sender, System.Windows.RoutedEventArgs e)
        {
            useCustomColor = true;
            RefreshLightContext();
            profileSettingsService.LightbarSettingsInfo[devIndex].ds4winSettings.useCustomLed = true;
            LightColorChanged?.Invoke(this, EventArgs.Empty);
            RequestColorPicker?.Invoke(this);
        }

        private void RefreshLightContext()
        {
            (lightContext.Items[0] as MenuItem).IsChecked = !useCustomColor;
            (lightContext.Items[1] as MenuItem).IsChecked = useCustomColor;
        }

        public void UpdateCustomLightColor(Color color)
        {
            profileSettingsService.LightbarSettingsInfo[devIndex].ds4winSettings.m_CustomLed = new DS4Color() { red = color.R, green = color.G, blue = color.B };
            LightColorChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RaiseLightColorChanged()
        {
            LightColorChanged?.Invoke(this, EventArgs.Empty);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LightColor)));
        }

        public void RequestDisconnect()
        {
            if (device.Synced && !device.Charging)
            {
                if (device.ConnectionType == ConnectionType.BT)
                {
                    device.queueEvent(() =>
                    {
                        device.DisconnectBT();
                    });
                }
                else if (device.ConnectionType == ConnectionType.SONYWA)
                {
                    device.DisconnectDongle();
                }
            }
        }
    }
}