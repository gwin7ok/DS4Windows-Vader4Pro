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
using DS4Windows.StickModifiers;
using DS4WinWPF.DS4Control;
using Sensorit.Base;
using DS4Windows.InputDevices;

namespace DS4Windows
{

    public class RumbleSettings : ProfileSubSettingBase
    {
        public const bool DEFAULT_ENABLE_RUMBLE = true;
        public const byte DEFAULT_RUMBLE_BOOST = 100;
        public const int DEFAULT_AUTOSTOP_TIME = 0;
        public const byte DEFAULT_RUMBLE_LIGHT = 50;
        public const byte DEFAULT_RUMBLE_HEAVY = 50;
        public const bool DEFAULT_ENABLE_RESCALE = false;
        public const byte DEFAULT_HAPTIC_POWER = 100;

        private bool enableRumble = DEFAULT_ENABLE_RUMBLE;
        private byte rumbleBoost = DEFAULT_RUMBLE_BOOST;
        private int rumbleAutostopTime = DEFAULT_AUTOSTOP_TIME;
        private byte rumbleLight = DEFAULT_RUMBLE_LIGHT;
        private byte rumbleHeavy = DEFAULT_RUMBLE_HEAVY;
        private DualSenseDevice.RumbleEmulationMode emulationMode = DualSenseDevice.RumbleEmulationMode.Accurate;
        private bool enableGenericRumbleRescale = DEFAULT_ENABLE_RESCALE;
        private byte hapticPowerLevel = DEFAULT_HAPTIC_POWER;

        public bool EnableRumble
        {
            get => enableRumble;
            set
            {
                if (enableRumble == value) return;
                enableRumble = value;
                RaisePropertyChanged();
                RumbleSettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public byte RumbleBoost
        {
            get => rumbleBoost;
            set
            {
                if (rumbleBoost == value) return;
                rumbleBoost = value;
                RaisePropertyChanged();
                RumbleSettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public int RumbleAutostopTime
        {
            get => rumbleAutostopTime;
            set
            {
                if (rumbleAutostopTime == value) return;
                rumbleAutostopTime = value;
                RaisePropertyChanged();
                RumbleSettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public byte RumbleLight
        {
            get => rumbleLight;
            set
            {
                if (rumbleLight == value) return;
                rumbleLight = value;
                RaisePropertyChanged();
                RumbleSettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public byte RumbleHeavy
        {
            get => rumbleHeavy;
            set
            {
                if (rumbleHeavy == value) return;
                rumbleHeavy = value;
                RaisePropertyChanged();
                RumbleSettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public DualSenseDevice.RumbleEmulationMode EmulationMode
        {
            get => emulationMode;
            set
            {
                if (emulationMode == value) return;
                emulationMode = value;
                RaisePropertyChanged();
                RumbleSettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool EnableGenericRumbleRescale
        {
            get => enableGenericRumbleRescale;
            set
            {
                if (enableGenericRumbleRescale == value) return;
                enableGenericRumbleRescale = value;
                RaisePropertyChanged();
                RumbleSettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public byte HapticPowerLevel
        {
            get => hapticPowerLevel;
            set
            {
                if (hapticPowerLevel == value) return;
                hapticPowerLevel = value;
                RaisePropertyChanged();
                RumbleSettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler RumbleSettingsChanged;

        public void Reset()
        {
            enableRumble = DEFAULT_ENABLE_RUMBLE;
            rumbleBoost = DEFAULT_RUMBLE_BOOST;
            rumbleAutostopTime = DEFAULT_AUTOSTOP_TIME;
            rumbleLight = DEFAULT_RUMBLE_LIGHT;
            rumbleHeavy = DEFAULT_RUMBLE_HEAVY;
            emulationMode = DualSenseDevice.RumbleEmulationMode.Accurate;
            enableGenericRumbleRescale = DEFAULT_ENABLE_RESCALE;
            hapticPowerLevel = DEFAULT_HAPTIC_POWER;
            RaiseAllPropertiesChanged();
            RumbleSettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ResetEvents()
        {
            RumbleSettingsChanged = null;
        }
    }
}