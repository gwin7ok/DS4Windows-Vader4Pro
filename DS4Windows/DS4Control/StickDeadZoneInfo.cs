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

    public class StickDeadZoneInfo
    {
        public enum DeadZoneType : ushort
        {
            Radial,
            Axial
        }

        public const int DEFAULT_DEADZONE = 10;
        public const int DEFAULT_ANTIDEADZONE = 20;
        public const int DEFAULT_MAXZONE = 100;
        public const double DEFAULT_MAXOUTPUT = 100.0;
        public const bool DEFAULT_MAXOUTPUT_FORCE = false;
        public const int DEFAULT_FUZZ = 0;
        public const DeadZoneType DEFAULT_DEADZONE_TYPE = DeadZoneType.Radial;
        public const double DEFAULT_VERTICAL_SCALE = 100.0;
        public const double DEFAULT_OUTER_BIND_DEAD = 75.0;
        public const bool DEFAULT_OUTER_BIND_INVERT = false;

        public class AxisDeadZoneInfo : ProfileSubSettingBase
        {
            private int _deadZone;
            private int _antiDeadZone;
            private int _maxZone = 100;
            private double _maxOutput = 100.0;

            public int deadZone
            {
                get => _deadZone;
                set
                {
                    if (_deadZone != value)
                    {
                        _deadZone = value;
                        RaisePropertyChanged();
                        RaisePropertyChanged(nameof(DeadZone));
                    }
                }
            }

            public int antiDeadZone
            {
                get => _antiDeadZone;
                set
                {
                    if (_antiDeadZone != value)
                    {
                        _antiDeadZone = value;
                        RaisePropertyChanged();
                        RaisePropertyChanged(nameof(AntiDeadZone));
                    }
                }
            }

            public int maxZone
            {
                get => _maxZone;
                set
                {
                    if (_maxZone != value)
                    {
                        _maxZone = value;
                        RaisePropertyChanged();
                        RaisePropertyChanged(nameof(MaxZone));
                    }
                }
            }

            public double maxOutput
            {
                get => _maxOutput;
                set
                {
                    if (Math.Abs(_maxOutput - value) > 0.0001)
                    {
                        _maxOutput = value;
                        RaisePropertyChanged();
                        RaisePropertyChanged(nameof(MaxOutput));
                    }
                }
            }

            // PascalCase 互換プロパティ
            public int DeadZone { get => deadZone; set => deadZone = value; }
            public int AntiDeadZone { get => antiDeadZone; set => antiDeadZone = value; }
            public int MaxZone { get => maxZone; set => maxZone = value; }
            public double MaxOutput { get => maxOutput; set => maxOutput = value; }

            public void Reset()
            {
                _deadZone = 0;
                _antiDeadZone = 0;
                _maxZone = 100;
                _maxOutput = 100.0;
                RaiseAllPropertiesChanged();
            }
        }

        public int deadZone;
        public int antiDeadZone;
        public int maxZone = DEFAULT_MAXZONE;
        public double maxOutput = DEFAULT_MAXOUTPUT;
        public bool maxOutputForce = DEFAULT_MAXOUTPUT_FORCE;
        public int fuzz = DEFAULT_FUZZ;
        public double verticalScale = DEFAULT_VERTICAL_SCALE;
        public DeadZoneType deadzoneType = DEFAULT_DEADZONE_TYPE;
        public double outerBindDeadZone = DEFAULT_OUTER_BIND_DEAD;
        public bool outerBindInvert = DEFAULT_OUTER_BIND_INVERT;
        public AxisDeadZoneInfo xAxisDeadInfo = new AxisDeadZoneInfo();
        public AxisDeadZoneInfo yAxisDeadInfo = new AxisDeadZoneInfo();

        public void Reset()
        {
            deadZone = 0;
            antiDeadZone = 0;
            maxZone = DEFAULT_MAXZONE;
            maxOutput = DEFAULT_MAXOUTPUT;
            maxOutputForce = DEFAULT_MAXOUTPUT_FORCE;

            fuzz = DEFAULT_FUZZ;
            verticalScale = DEFAULT_VERTICAL_SCALE;
            deadzoneType = DEFAULT_DEADZONE_TYPE;
            outerBindDeadZone = DEFAULT_OUTER_BIND_DEAD;
            outerBindInvert = DEFAULT_OUTER_BIND_INVERT;
            xAxisDeadInfo.Reset();
            yAxisDeadInfo.Reset();
        }
    }
}