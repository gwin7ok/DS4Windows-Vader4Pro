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
}