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

    public class GyroMouseInfo : ProfileSubSettingBase
    {
        public enum SmoothingMethod : byte
        {
            None,
            OneEuro,
            WeightedAverage,
        }

        public const double DEFAULT_MINCUTOFF = 1.0;
        public const double DEFAULT_BETA = 0.7;
        public const string DEFAULT_SMOOTH_TECHNIQUE = "one-euro";
        public const double DEFAULT_MIN_THRESHOLD = 1.0;
        public const bool JITTER_COMPENSATION_DEFAULT = true;

        public bool enableSmoothing = false;
        public double smoothingWeight = 0.5;
        public SmoothingMethod smoothingMethod;

        public double minCutoff = DEFAULT_MINCUTOFF;
        public double beta = DEFAULT_BETA;
        public double minThreshold = DEFAULT_MIN_THRESHOLD;
        public bool jitterCompensation = JITTER_COMPENSATION_DEFAULT;

        public delegate void GyroMouseInfoEventHandler(GyroMouseInfo sender, EventArgs args);

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
        public event GyroMouseInfoEventHandler MinCutoffChanged;

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
        public event GyroMouseInfoEventHandler BetaChanged;

        public bool JitterCompensation
        {
            get => jitterCompensation;
            set
            {
                if (jitterCompensation == value) return;
                jitterCompensation = value;
                RaisePropertyChanged();
            }
        }

        public void Reset()
        {
            minCutoff = DEFAULT_MINCUTOFF;
            beta = DEFAULT_BETA;
            enableSmoothing = false;
            smoothingMethod = SmoothingMethod.None;
            smoothingWeight = 0.5;
            minThreshold = DEFAULT_MIN_THRESHOLD;
            jitterCompensation = JITTER_COMPENSATION_DEFAULT;
            RaiseAllPropertiesChanged();
        }

        public void ResetSmoothing()
        {
            enableSmoothing = false;
            ResetSmoothingMethods();
            RaisePropertyChanged(nameof(enableSmoothing));
        }

        public void ResetSmoothingMethods()
        {
            smoothingMethod = SmoothingMethod.None;
            RaisePropertyChanged(nameof(smoothingMethod));
        }

        public void DetermineSmoothMethod(string identier)
        {
            ResetSmoothingMethods();
            smoothingMethod = SmoothingMethodParse(identier);
            RaisePropertyChanged(nameof(smoothingMethod));
        }

        public static SmoothingMethod SmoothingMethodParse(string identifier)
        {
            SmoothingMethod result = SmoothingMethod.None;
            switch (identifier)
            {
                case "weighted-average":
                    result = SmoothingMethod.WeightedAverage;
                    break;
                case "one-euro":
                    result = SmoothingMethod.OneEuro;
                    break;
                default:
                    result = SmoothingMethod.None;
                    break;
            }
            return result;
        }

        public string SmoothMethodIdentifier()
        {
            string result = "none";
            if (smoothingMethod == SmoothingMethod.OneEuro)
            {
                result = "one-euro";
            }
            else if (smoothingMethod == SmoothingMethod.WeightedAverage)
            {
                result = "weighted-average";
            }
            return result;
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