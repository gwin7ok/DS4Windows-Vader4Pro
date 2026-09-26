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
    // ==========================================
    // パターン A: ProfileActions および Sensitivity
    // ==========================================

    public class ProfileActions
    {
        public const int MAX_ACTIONS = 6;
        public string[] actions = new string[MAX_ACTIONS];
        public string[] actionExtras = new string[MAX_ACTIONS];

        public event EventHandler ActionsChanged;

        public string GetAction(int index) => index >= 0 && index < MAX_ACTIONS ? actions[index] : string.Empty;

        public void SetAction(int index, string value)
        {
            if (index < 0 || index >= MAX_ACTIONS) return;
            if (actions[index] == value) return;
            actions[index] = value;
            ActionsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Reset()
        {
            Array.Clear(actions, 0, actions.Length);
            Array.Clear(actionExtras, 0, actionExtras.Length);
            ActionsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class Sensitivity : ProfileSubSettingBase
    {
        private double _xSensitivity = 1.0;
        private double _ySensitivity = 1.0;

        public double XSensitivity
        {
            get => _xSensitivity;
            set
            {
                if (Math.Abs(_xSensitivity - value) > 0.0001)
                {
                    _xSensitivity = value;
                    RaisePropertyChanged();
                }
            }
        }

        public double YSensitivity
        {
            get => _ySensitivity;
            set
            {
                if (Math.Abs(_ySensitivity - value) > 0.0001)
                {
                    _ySensitivity = value;
                    RaisePropertyChanged();
                }
            }
        }

        public void Reset()
        {
            _xSensitivity = 1.0;
            _ySensitivity = 1.0;
            RaiseAllPropertiesChanged();
        }
    }

    // ==========================================
    // パターン B: ネストされたサブ設定クラス群
    // ==========================================

    public class SquareStickInfo
    {
        public const double DEFAULT_ROUNDNESS = 5.0;

        public bool lsMode;
        public bool rsMode;
        public double lsRoundness = DEFAULT_ROUNDNESS;
        public double rsRoundness = DEFAULT_ROUNDNESS;

        public void Reset()
        {
            lsMode = false;
            rsMode = false;
            lsRoundness = DEFAULT_ROUNDNESS;
            rsRoundness = DEFAULT_ROUNDNESS;
        }
    }

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

    public class StickAntiSnapbackInfo
    {
        public const double DEFAULT_DELTA = 135;
        public const int DEFAULT_TIMEOUT = 50;
        public const bool DEFAULT_ENABLED = false;

        public bool enabled = DEFAULT_ENABLED;
        public double delta = DEFAULT_DELTA;
        public int timeout = DEFAULT_TIMEOUT;
    }

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

    public class GyroDirectionalSwipeInfo
    {
        public enum XAxisSwipe : ushort
        {
            Yaw,
            Roll,
        }

        public const string DEFAULT_TRIGGERS = "-1";
        public const int DEFAULT_GYRO_DIR_SPEED = 80;
        public const bool DEFAULT_TRIGGER_COND = true;
        public const bool DEFAULT_TRIGGER_TURNS = true;
        public const XAxisSwipe DEFAULT_X_AXIS = XAxisSwipe.Yaw;
        public const int DEFAULT_DELAY_TIME = 0;

        public int deadzoneX = DEFAULT_GYRO_DIR_SPEED;
        public int deadzoneY = DEFAULT_GYRO_DIR_SPEED;
        public string triggers = DEFAULT_TRIGGERS;
        public bool triggerCond = DEFAULT_TRIGGER_COND;
        public bool triggerTurns = DEFAULT_TRIGGER_TURNS;
        public XAxisSwipe xAxis = DEFAULT_X_AXIS;
        public int delayTime = DEFAULT_DELAY_TIME;

        public void Reset()
        {
            deadzoneX = DEFAULT_GYRO_DIR_SPEED;
            deadzoneY = DEFAULT_GYRO_DIR_SPEED;
            triggers = DEFAULT_TRIGGERS;
            triggerCond = DEFAULT_TRIGGER_COND;
            triggerTurns = DEFAULT_TRIGGER_TURNS;
            xAxis = DEFAULT_X_AXIS;
            delayTime = DEFAULT_DELAY_TIME;
        }
    }

    public class GyroControlsInfo : ProfileSubSettingBase
    {
        public const string DEFAULT_TRIGGERS = "-1";
        public const bool DEFAULT_TRIGGER_COND = true;
        public const bool DEFAULT_TRIGGER_TURNS = true;
        public const bool DEFAULT_TRIGGER_TOGGLE = false;

        private string _triggers = DEFAULT_TRIGGERS;
        private bool _triggerCond = DEFAULT_TRIGGER_COND;
        private bool _triggerTurns = DEFAULT_TRIGGER_TURNS;
        private bool _triggerToggle = DEFAULT_TRIGGER_TOGGLE;

        public string triggers
        {
            get => _triggers;
            set
            {
                if (_triggers != value)
                {
                    _triggers = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(Triggers));
                }
            }
        }

        public bool triggerCond
        {
            get => _triggerCond;
            set
            {
                if (_triggerCond != value)
                {
                    _triggerCond = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(TriggerCond));
                }
            }
        }

        public bool triggerTurns
        {
            get => _triggerTurns;
            set
            {
                if (_triggerTurns != value)
                {
                    _triggerTurns = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(TriggerTurns));
                }
            }
        }

        public bool triggerToggle
        {
            get => _triggerToggle;
            set
            {
                if (_triggerToggle != value)
                {
                    _triggerToggle = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(TriggerToggle));
                }
            }
        }

        // PascalCase 互換プロパティ
        public string Triggers { get => triggers; set => triggers = value; }
        public bool TriggerCond { get => triggerCond; set => triggerCond = value; }
        public bool TriggerTurns { get => triggerTurns; set => triggerTurns = value; }
        public bool TriggerToggle { get => triggerToggle; set => triggerToggle = value; }

        public void Reset()
        {
            _triggers = DEFAULT_TRIGGERS;
            _triggerCond = DEFAULT_TRIGGER_COND;
            _triggerTurns = DEFAULT_TRIGGER_TURNS;
            _triggerToggle = DEFAULT_TRIGGER_TOGGLE;
            RaiseAllPropertiesChanged();
        }
    }

    public class ButtonMouseInfo
    {
        public const double MOUSESTICKANTIOFFSET = 0.008;
        public const int DEFAULT_BUTTON_SENS = 25;
        public const double DEFAULT_BUTTON_VERTICAL_SCALE = 1.0;
        public const int DEFAULT_TEMP_SENS = -1;

        public int buttonSensitivity = DEFAULT_BUTTON_SENS;
        public int ButtonSensitivity
        {
            get => buttonSensitivity;
            set
            {
                if (buttonSensitivity == value) return;
                buttonSensitivity = value;
                ButtonMouseInfoChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler ButtonMouseInfoChanged;

        public bool mouseAccel;
        public int activeButtonSensitivity = DEFAULT_BUTTON_SENS;
        public int tempButtonSensitivity = DEFAULT_TEMP_SENS;
        public double mouseVelocityOffset = MOUSESTICKANTIOFFSET;
        public double buttonVerticalScale = DEFAULT_BUTTON_VERTICAL_SCALE;

        public ButtonMouseInfo()
        {
            ButtonMouseInfoChanged += ButtonMouseInfo_ButtonMouseInfoChanged;
        }

        private void ButtonMouseInfo_ButtonMouseInfoChanged(object sender, EventArgs e)
        {
            if (tempButtonSensitivity == DEFAULT_TEMP_SENS)
            {
                activeButtonSensitivity = buttonSensitivity;
            }
        }

        public void SetActiveButtonSensitivity(int sens)
        {
            activeButtonSensitivity = sens;
        }

        public void Reset()
        {
            buttonSensitivity = DEFAULT_BUTTON_SENS;
            mouseAccel = false;
            activeButtonSensitivity = DEFAULT_BUTTON_SENS;
            tempButtonSensitivity = DEFAULT_TEMP_SENS;
            mouseVelocityOffset = MOUSESTICKANTIOFFSET;
            buttonVerticalScale = DEFAULT_BUTTON_VERTICAL_SCALE;
        }
    }

    public enum LightbarMode : uint
    {
        None,
        DS4Win,
        Passthru,
    }

    public class LightbarDS4WinInfo
    {
        public const double DEFAULT_MAX_RAINBOW_SAT = 1.0;
        public static DS4Color DEFAULT_CUSTOM_LED = new DS4Color(0, 0, 255);

        public bool useCustomLed;
        public bool ledAsBattery;
        public DS4Color m_CustomLed = new DS4Color(0, 0, 255);
        public DS4Color m_Led;
        public DS4Color m_LowLed;
        public DS4Color m_ChargingLed;
        public DS4Color m_FlashLed;
        public double rainbow;
        public double maxRainbowSat = DEFAULT_MAX_RAINBOW_SAT;
        public int flashAt;
        public byte flashType;
        public int chargingType;
    }

    public class LightbarSettingInfo
    {
        public const LightbarMode DEFAULT_MODE = LightbarMode.DS4Win;

        public LightbarMode mode = DEFAULT_MODE;
        public LightbarDS4WinInfo ds4winSettings = new LightbarDS4WinInfo();
        public LightbarMode Mode
        {
            get => mode;
            set
            {
                if (mode == value) return;
                mode = value;
                ModeChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler ModeChanged;
    }

    public class SteeringWheelSmoothingInfo
    {
        private double minCutoff = OneEuroFilterPair.DEFAULT_WHEEL_CUTOFF;
        private double beta = OneEuroFilterPair.DEFAULT_WHEEL_BETA;
        public bool enabled = false;

        public delegate void SmoothingInfoEventHandler(SteeringWheelSmoothingInfo sender, EventArgs args);

        public double MinCutoff
        {
            get => minCutoff;
            set
            {
                if (minCutoff == value) return;
                minCutoff = value;
                MinCutoffChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event SmoothingInfoEventHandler MinCutoffChanged;

        public double Beta
        {
            get => beta;
            set
            {
                if (beta == value) return;
                beta = value;
                BetaChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event SmoothingInfoEventHandler BetaChanged;

        public bool Enabled
        {
            get => enabled;
            set => enabled = value;
        }

        public void Reset()
        {
            MinCutoff = OneEuroFilterPair.DEFAULT_WHEEL_CUTOFF;
            Beta = OneEuroFilterPair.DEFAULT_WHEEL_BETA;
            enabled = false;
        }

        public void SetFilterAttrs(OneEuroFilter euroFilter)
        {
            euroFilter.MinCutoff = minCutoff;
            euroFilter.Beta = beta;
        }

        public void SetRefreshEvents(OneEuroFilter euroFilter)
        {
            BetaChanged += (sender, args) => { euroFilter.Beta = beta; };
            MinCutoffChanged += (sender, args) => { euroFilter.MinCutoff = minCutoff; };
        }
    }

    public class TouchpadRelMouseSettings
    {
        public const double DEFAULT_ANG_DEGREE = 0.0;
        public const double DEFAULT_ANG_RAD = DEFAULT_ANG_DEGREE * Math.PI / 180.0;
        public const double DEFAULT_MIN_THRESHOLD = 1.0;

        public double rotation = DEFAULT_ANG_RAD;
        public double minThreshold = DEFAULT_MIN_THRESHOLD;

        public void Reset()
        {
            rotation = DEFAULT_ANG_RAD;
            minThreshold = DEFAULT_MIN_THRESHOLD;
        }
    }

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

    public enum StickMode : uint
    {
        None,
        Controls,
        FlickStick,
    }

    public enum TriggerMode : uint
    {
        Normal,
        TwoStage,
    }

    public enum TwoStageTriggerMode : uint
    {
        Disabled,
        Normal,
        ExclusiveButtons,
        HairTrigger,
        HipFire,
        HipFireExclusiveButtons,
    }

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

    public class StickControlSettings
    {
        public DeltaAccelSettings deltaAccelSettings = new DeltaAccelSettings();

        public void Reset()
        {
            deltaAccelSettings.Reset();
        }
    }

    public class StickModeSettings
    {
        public FlickStickSettings flickSettings = new FlickStickSettings();
        public StickControlSettings controlSettings = new StickControlSettings();
    }

    public class StickOutputSetting
    {
        public StickMode mode = StickMode.Controls;
        public StickModeSettings outputSettings = new StickModeSettings();

        public void ResetSettings()
        {
            mode = StickMode.Controls;
            outputSettings.controlSettings.Reset();
            outputSettings.flickSettings.Reset();
        }
    }

    public class TriggerOutputSettings
    {
        public const TwoStageTriggerMode DEFAULT_TRIG_MODE = TwoStageTriggerMode.Disabled;
        public const int DEFAULT_HIP_TIME = 100;
        public const InputDevices.TriggerEffects DEFAULT_TRIGGER_EFFECT = InputDevices.TriggerEffects.None;

        public TwoStageTriggerMode twoStageMode = DEFAULT_TRIG_MODE;
        public TwoStageTriggerMode TwoStageMode
        {
            get => twoStageMode;
            set
            {
                if (twoStageMode == value) return;
                twoStageMode = value;
                TwoStageModeChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler TwoStageModeChanged;

        public int hipFireMS = DEFAULT_HIP_TIME;
        public InputDevices.TriggerEffects triggerEffect = DEFAULT_TRIGGER_EFFECT;
        public InputDevices.TriggerEffects TriggerEffect
        {
            get => triggerEffect;
            set
            {
                if (triggerEffect == value) return;
                triggerEffect = value;
                TriggerEffectChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler TriggerEffectChanged;

        public InputDevices.TriggerEffectSettings effectSettings = new InputDevices.TriggerEffectSettings();
        public ref InputDevices.TriggerEffectSettings TrigEffectSettings => ref effectSettings;

        public void ResetSettings()
        {
            twoStageMode = DEFAULT_TRIG_MODE;
            hipFireMS = DEFAULT_HIP_TIME;
            triggerEffect = DEFAULT_TRIGGER_EFFECT;
            TwoStageModeChanged?.Invoke(this, EventArgs.Empty);
            TriggerEffectChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ResetEvents()
        {
            TwoStageModeChanged = null;
            TriggerEffectChanged = null;
        }
    }

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