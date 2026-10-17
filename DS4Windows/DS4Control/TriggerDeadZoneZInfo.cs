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

    public class TriggerDeadZoneZInfo : ProfileSubSettingBase
    {
        // Default Dead Zone value
        public const byte DEFAULT_DEADZONE = 0;
        public const int DEFAULT_ANTIDEADZONE = 0;
        // Default Max Zone value
        public const int DEFAULT_MAX_ZONE = 100;
        // Default Max Output value
        public const double DEFAULT_MAX_OUTPUT = 100.0;

        // ref / out で渡されるため public フィールドとして保持
        public byte deadZone = DEFAULT_DEADZONE;
        public int antiDeadZone = DEFAULT_ANTIDEADZONE;
        public int maxZone = DEFAULT_MAX_ZONE;
        public double maxOutput = DEFAULT_MAX_OUTPUT;

        public delegate void DeadZoneChangedHandler(TriggerDeadZoneZInfo sender, EventArgs args);
        public event DeadZoneChangedHandler DeadZoneChanged;

        public delegate void AntiDeadZoneChangedHandler(TriggerDeadZoneZInfo sender, EventArgs args);
        public event AntiDeadZoneChangedHandler AntiDeadZoneChanged;

        public delegate void MaxZoneChangedHandler(TriggerDeadZoneZInfo sender, EventArgs args);
        public event MaxZoneChangedHandler MaxZoneChanged;

        public delegate void MaxOutputChangedHandler(TriggerDeadZoneZInfo sender, EventArgs args);
        public event MaxOutputChangedHandler MaxOutputChanged;

        public byte DeadZone
        {
            get => deadZone;
            set
            {
                if (deadZone == value) return;
                deadZone = value;
                DeadZoneChanged?.Invoke(this, EventArgs.Empty);
                RaisePropertyChanged();
            }
        }

        public int AntiDeadZone
        {
            get => antiDeadZone;
            set
            {
                if (antiDeadZone == value) return;
                antiDeadZone = value;
                AntiDeadZoneChanged?.Invoke(this, EventArgs.Empty);
                RaisePropertyChanged();
            }
        }

        public int MaxZone
        {
            get => maxZone;
            set
            {
                if (maxZone == value) return;
                maxZone = value;
                MaxZoneChanged?.Invoke(this, EventArgs.Empty);
                RaisePropertyChanged();
            }
        }

        public double MaxOutput
        {
            get => maxOutput;
            set
            {
                if (Math.Abs(maxOutput - value) > 0.0001)
                {
                    maxOutput = value;
                    MaxOutputChanged?.Invoke(this, EventArgs.Empty);
                    RaisePropertyChanged();
                }
            }
        }

        public void Reset()
        {
            deadZone = DEFAULT_DEADZONE;
            antiDeadZone = DEFAULT_ANTIDEADZONE;
            maxZone = DEFAULT_MAX_ZONE;
            maxOutput = DEFAULT_MAX_OUTPUT;
            RaiseAllPropertiesChanged();
        }

        public void ResetEvents()
        {
            DeadZoneChanged = null;
            AntiDeadZoneChanged = null;
            MaxZoneChanged = null;
            MaxOutputChanged = null;
        }
    }
}