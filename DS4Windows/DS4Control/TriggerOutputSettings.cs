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
}