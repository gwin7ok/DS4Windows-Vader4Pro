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

    public class TouchMouseStickInfo : ProfileSubSettingBase
    {
        public enum SmoothingMethod : byte
        {
            None,
            OneEuro,
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

        public const double DEFAULT_MINCUTOFF = 0.8;
        public const double DEFAULT_BETA = 0.7;
        public const OutputStick DEFAULT_OUTPUT_STICK = OutputStick.RightStick;
        public const OutputStickAxes DEFAULT_OUTPUT_STICK_AXES = OutputStickAxes.XY;
        public const int DEFAULT_DEADZONE = 0;
        public const int MAX_ZONE_DEFAULT = 8;
        public const double ANTI_DEADZONE_DEFAULT = 0.40;
        public const bool TRACKBALL_MODE_DEFAULT = true;
        public const double TRACKBALL_FRICTION_DEFAULT = 10.0;
        public const StickOutCurve.Curve OUTPUT_CURVE_DEFAULT = StickOutCurve.Curve.Linear;
        public const double ANG_DEGREE_DEFAULT = 0.0;
        public const double ANG_RAD_DEFAULT = ANG_DEGREE_DEFAULT * Math.PI / 180.0;
        public const int DEFAULT_VERT_SCALE = 100;
        public const double DEFAULT_MAX_OUTPUT = 100.0;
        public const uint DEFAULT_INVERTED = 0;
        public const bool DEFAULT_MAX_OUTPUT_ENABLED = false;

        public int deadZone = DEFAULT_DEADZONE;
        public int maxZone = MAX_ZONE_DEFAULT;
        public double antiDeadX = ANTI_DEADZONE_DEFAULT;
        public double antiDeadY = ANTI_DEADZONE_DEFAULT;
        public int vertScale = DEFAULT_VERT_SCALE;
        public bool maxOutputEnabled = DEFAULT_MAX_OUTPUT_ENABLED;
        public double maxOutput = DEFAULT_MAX_OUTPUT;
        public uint inverted = DEFAULT_INVERTED;
        public SmoothingMethod smoothingMethod;
        public double minCutoff = DEFAULT_MINCUTOFF;
        public double beta = DEFAULT_BETA;
        public OutputStick outputStick = DEFAULT_OUTPUT_STICK;
        public OutputStickAxes outputStickDir = DEFAULT_OUTPUT_STICK_AXES;
        public bool trackballMode = TRACKBALL_MODE_DEFAULT;
        public double trackballFriction = TRACKBALL_FRICTION_DEFAULT;
        public StickOutCurve.Curve outputCurve;
        public double rotationRad = ANG_RAD_DEFAULT;

        public delegate void TouchMouseStickInfoEventHandler(TouchMouseStickInfo sender, EventArgs args);

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
        public event TouchMouseStickInfoEventHandler MinCutoffChanged;

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
        public event TouchMouseStickInfoEventHandler BetaChanged;

        public bool UseSmoothing
        {
            get => smoothingMethod != SmoothingMethod.None;
            set
            {
                if (value) smoothingMethod = SmoothingMethod.OneEuro;
                else smoothingMethod = SmoothingMethod.None;
                RaisePropertyChanged();
            }
        }

        public void Reset()
        {
            deadZone = DEFAULT_DEADZONE; maxZone = MAX_ZONE_DEFAULT;
            antiDeadX = ANTI_DEADZONE_DEFAULT; antiDeadY = ANTI_DEADZONE_DEFAULT;
            inverted = DEFAULT_INVERTED; vertScale = DEFAULT_VERT_SCALE;
            maxOutputEnabled = DEFAULT_MAX_OUTPUT_ENABLED; maxOutput = DEFAULT_MAX_OUTPUT;
            outputStick = DEFAULT_OUTPUT_STICK;
            outputStickDir = DEFAULT_OUTPUT_STICK_AXES;
            trackballMode = TRACKBALL_MODE_DEFAULT;
            trackballFriction = TRACKBALL_FRICTION_DEFAULT;
            outputCurve = OUTPUT_CURVE_DEFAULT;
            rotationRad = ANG_RAD_DEFAULT;

            minCutoff = DEFAULT_MINCUTOFF;
            beta = DEFAULT_BETA;
            smoothingMethod = SmoothingMethod.None;
            RemoveRefreshEvents();
            RaiseAllPropertiesChanged();
        }

        public void ResetSmoothing()
        {
            ResetSmoothingMethods();
            RaisePropertyChanged(nameof(smoothingMethod));
        }

        public void ResetSmoothingMethods()
        {
            smoothingMethod = SmoothingMethod.None;
            RaisePropertyChanged(nameof(smoothingMethod));
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