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

    public class FlickStickSettings : ProfileSubSettingBase
    {
        public const double DEFAULT_FLICK_THRESHOLD = 0.9;
        public const double DEFAULT_FLICK_TIME = 0.1;
        public const double DEFAULT_REAL_WORLD_CALIBRATION = 5.3;
        public const double DEFAULT_MIN_ANGLE_THRESHOLD = 0.0;

        public const double DEFAULT_MINCUTOFF = 0.4;
        public const double DEFAULT_BETA = 0.4;

        public double flickThreshold = DEFAULT_FLICK_THRESHOLD;
        public double flickTime = DEFAULT_FLICK_TIME;
        public double realWorldCalibration = DEFAULT_REAL_WORLD_CALIBRATION;
        public double minAngleThreshold = DEFAULT_MIN_ANGLE_THRESHOLD;

        public double minCutoff = DEFAULT_MINCUTOFF;
        public double beta = DEFAULT_BETA;

        public delegate void FlickStickSettingsEventHandler(FlickStickSettings sender, EventArgs args);

        public double FlickThreshold
        {
            get => flickThreshold;
            set
            {
                if (Math.Abs(flickThreshold - value) > 0.0001)
                {
                    flickThreshold = value;
                    RaisePropertyChanged();
                }
            }
        }

        public double FlickTime
        {
            get => flickTime;
            set
            {
                if (Math.Abs(flickTime - value) > 0.0001)
                {
                    flickTime = value;
                    RaisePropertyChanged();
                }
            }
        }

        public double RealWorldCalibration
        {
            get => realWorldCalibration;
            set
            {
                if (Math.Abs(realWorldCalibration - value) > 0.0001)
                {
                    realWorldCalibration = value;
                    RaisePropertyChanged();
                }
            }
        }

        public double MinAngleThreshold
        {
            get => minAngleThreshold;
            set
            {
                if (Math.Abs(minAngleThreshold - value) > 0.0001)
                {
                    minAngleThreshold = value;
                    RaisePropertyChanged();
                }
            }
        }

        public double MinCutoff
        {
            get => minCutoff;
            set
            {
                if (minCutoff == value) return;
                minCutoff = value;
                MinCutoffChanged?.Invoke(this, EventArgs.Empty);
                RaisePropertyChanged();
            }
        }
        public event FlickStickSettingsEventHandler MinCutoffChanged;

        public double Beta
        {
            get => beta;
            set
            {
                if (beta == value) return;
                beta = value;
                BetaChanged?.Invoke(this, EventArgs.Empty);
                RaisePropertyChanged();
            }
        }
        public event FlickStickSettingsEventHandler BetaChanged;

        public void Reset()
        {
            flickThreshold = DEFAULT_FLICK_THRESHOLD;
            flickTime = DEFAULT_FLICK_TIME;
            realWorldCalibration = DEFAULT_REAL_WORLD_CALIBRATION;
            minAngleThreshold = DEFAULT_MIN_ANGLE_THRESHOLD;
            minCutoff = DEFAULT_MINCUTOFF;
            beta = DEFAULT_BETA;
            RaiseAllPropertiesChanged();
        }

        public void SetRefreshEvents(OneEuroFilter euroFilter)
        {
            BetaChanged += (sender, args) => { euroFilter.Beta = beta; };
            MinCutoffChanged += (sender, args) => { euroFilter.MinCutoff = minCutoff; };
        }

        public void RemoveRefreshEvents()
        {
            BetaChanged = null;
            MinCutoffChanged = null;
        }
    }
}