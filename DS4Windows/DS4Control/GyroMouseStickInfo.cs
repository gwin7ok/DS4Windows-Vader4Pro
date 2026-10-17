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

    public class GyroMouseStickInfo : ProfileSubSettingBase
    {
        public enum SmoothingMethod : byte
        {
            None,
            OneEuro,
            WeightedAverage,
        }

        public enum OutputStick : byte
        {
            None,
            LeftStick,
            RightStick,
        }

        public enum OutputStickAxes : byte
        {
            None,
            XY,
            X,
            Y
        }

        public const double DEFAULT_MINCUTOFF = 0.4;
        public const double DEFAULT_BETA = 0.7;
        public const string DEFAULT_SMOOTH_TECHNIQUE = "one-euro";
        public const OutputStick DEFAULT_OUTPUT_STICK = OutputStick.RightStick;
        public const OutputStickAxes DEFAULT_OUTPUT_STICK_AXES = OutputStickAxes.XY;
        public const double SMOOTHING_WEIGHT_DEFAULT = 0.5;
        public const bool JITTER_COMPENSATION_DEFAULT = false;
        public const int DEFAULT_DEADZONE = 30;
        public const int DEFAULT_MAXZONE = 830;
        public const double DEFAULT_ANTI_DEAD = 0.4;
        public const double DEFAULT_MAX_OUTPUT = 100.0;
        public const int DEFAULT_VERTICAL_SCALE = 100;
        public const uint DEFAULT_INVERTED = 0;

        public int deadZone = DEFAULT_DEADZONE;
        public int maxZone = DEFAULT_MAXZONE;
        public double antiDeadX = DEFAULT_ANTI_DEAD;
        public double antiDeadY = DEFAULT_ANTI_DEAD;
        public int vertScale = DEFAULT_VERTICAL_SCALE;
        public bool maxOutputEnabled;
        public double maxOutput = DEFAULT_MAX_OUTPUT;
        public uint inverted = DEFAULT_INVERTED;
        public bool useSmoothing;
        public double smoothWeight = SMOOTHING_WEIGHT_DEFAULT;
        public SmoothingMethod smoothingMethod;
        public double minCutoff = DEFAULT_MINCUTOFF;
        public double beta = DEFAULT_BETA;
        public OutputStick outputStick = DEFAULT_OUTPUT_STICK;
        public OutputStickAxes outputStickDir = DEFAULT_OUTPUT_STICK_AXES;
        public bool jitterCompensation = JITTER_COMPENSATION_DEFAULT;

        public delegate void GyroMouseStickInfoEventHandler(GyroMouseStickInfo sender, EventArgs args);

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
        public event GyroMouseStickInfoEventHandler MinCutoffChanged;

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
        public event GyroMouseStickInfoEventHandler BetaChanged;

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
            deadZone = DEFAULT_DEADZONE; maxZone = DEFAULT_MAXZONE;
            antiDeadX = DEFAULT_ANTI_DEAD; antiDeadY = DEFAULT_ANTI_DEAD;
            inverted = DEFAULT_INVERTED; vertScale = DEFAULT_VERTICAL_SCALE;
            maxOutputEnabled = false; maxOutput = DEFAULT_MAX_OUTPUT;
            outputStick = DEFAULT_OUTPUT_STICK;
            outputStickDir = DEFAULT_OUTPUT_STICK_AXES;

            minCutoff = DEFAULT_MINCUTOFF;
            beta = DEFAULT_BETA;
            smoothingMethod = SmoothingMethod.None;
            useSmoothing = false;
            smoothWeight = SMOOTHING_WEIGHT_DEFAULT;
            jitterCompensation = JITTER_COMPENSATION_DEFAULT;
            RaiseAllPropertiesChanged();
        }

        public void ResetSmoothing()
        {
            useSmoothing = false;
            ResetSmoothingMethods();
            RaisePropertyChanged(nameof(useSmoothing));
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
            switch (smoothingMethod)
            {
                case SmoothingMethod.WeightedAverage:
                    result = "weighted-average";
                    break;
                case SmoothingMethod.OneEuro:
                    result = "one-euro";
                    break;
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

        public bool OutputHorizontal() => outputStickDir == OutputStickAxes.XY || outputStickDir == OutputStickAxes.X;
        public bool OutputVertical() => outputStickDir == OutputStickAxes.XY || outputStickDir == OutputStickAxes.Y;
    }
}