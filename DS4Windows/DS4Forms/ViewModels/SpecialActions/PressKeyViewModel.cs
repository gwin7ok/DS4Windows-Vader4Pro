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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels.Util;
using System.ComponentModel;

namespace DS4WinWPF.DS4Forms.ViewModels.SpecialActions
{
    public class PressKeyViewModel : NotifyDataErrorBase, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        private string switchMode = string.Empty;
        public string SwitchMode
        {
            get => switchMode;
            set
            {
                switchMode = value;
                // Update UI strings and toggle-related controls when SwitchMode changes
                UpdateDescribeText();
                UpdateToggleControls();
                OnPropertyChanged(nameof(SwitchMode));
            }
        }
        private int editorDeviceNum = -1;
        private DS4ControlSettings.ActionType lastActionType = DS4ControlSettings.ActionType.Key;
        private int lastActionBtn = -1;
        private string describeText;
        private DS4KeyType keyType;
        private int value;
        private int pressReleaseIndex = 0;
        private bool normalTrigger = true;
        public bool IsToggle => (keyType & DS4KeyType.Toggle) != 0;
        public event EventHandler IsToggleChanged;

        public Visibility ShowToggleControls
        {
            get
            {
                return ((keyType & DS4KeyType.Toggle) != 0) ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        public event EventHandler ShowToggleControlsChanged;

        public string DescribeText
        {
            get
            {
                string result = "Select a Key";
                if (!string.IsNullOrEmpty(describeText))
                {
                    result = describeText;
                };

                return result;
            }
        }
        public event EventHandler DescribeTextChanged;
        public DS4KeyType KeyType { get => keyType; set => keyType = value; }
        public int Value { get => value; set => this.value = value; }
        public int PressReleaseIndex { get => pressReleaseIndex; set => pressReleaseIndex = value; }
        public bool NormalTrigger { get => normalTrigger; set => normalTrigger = value; }
        public bool UnloadError
        {
            get => errors.TryGetValue("UnloadError", out _);
        }

        public void LoadAction(SpecialAction action)
        {
            // device number may be set by the editor so we can pick the proper
            // emulated controller type for label strings (X360 vs DS4)
            keyType = action.keyType;
            // Read explicit SwitchMode if provided by SpecialAction; fallback to keyType flag
            if (action.KeyButtonSwitchMode.HasValue)
            {
                SwitchMode = action.KeyButtonSwitchMode.Value == SpecialAction.KeyButtonSwitchModeEnum.Toggle ? "Toggle" : "Press";
            }
            else
            {
                SwitchMode = keyType.HasFlag(DS4KeyType.Toggle) ? "Toggle" : "Press";
            }
            // If action is Button, details contains button id
                if (action.typeID == SpecialAction.ActionTypeId.Button)
            {
                lastActionType = DS4ControlSettings.ActionType.Button;
                int.TryParse(action.details, out lastActionBtn);
                    try
                    {
                        // Prefer using editor's device-specific output type when available
                        string btnName;
                        if (editorDeviceNum >= 0 && editorDeviceNum < Global.OutContType.Length)
                        {
                            btnName = Global.getX360ControlString((X360Controls)lastActionBtn, Global.OutContType[editorDeviceNum]);
                        }
                        else
                        {
                            btnName = Global.getX360ControlString((X360Controls)lastActionBtn);
                        }

                        // Display mode suffix: (Toggle) or (Press)
                        describeText = btnName + (action.keyType.HasFlag(DS4KeyType.Toggle) ? " (Toggle)" : " (Press)");
                    }
                    catch { describeText = string.Empty; }
                value = 0;
            }
            else
            {
                int.TryParse(action.details, out value);
                lastActionType = DS4ControlSettings.ActionType.Key;
                lastActionBtn = -1;
            }

            if (action.pressRelease)
            {
                pressReleaseIndex = 1;
            }

            UpdateDescribeText();
            UpdateToggleControls();
        }

        public void UpdateDescribeText()
        {
            // If last binding was a Button, prefer showing the button name.
            if (lastActionType == DS4ControlSettings.ActionType.Button && lastActionBtn >= 0)
            {
                try
                {
                    if (editorDeviceNum >= 0 && editorDeviceNum < Global.OutContType.Length)
                    {
                        describeText = Global.getX360ControlString((X360Controls)lastActionBtn, Global.OutContType[editorDeviceNum]);
                    }
                    else
                    {
                        describeText = Global.getX360ControlString((X360Controls)lastActionBtn);
                    }
                }
                catch
                {
                    describeText = Properties.Resources.Unassigned;
                }
            }
            else
            {
                    describeText = KeyInterop.KeyFromVirtualKey(value).ToString() +
                        (keyType.HasFlag(DS4KeyType.ScanCode) ? " (SC)" : "") +
                        (string.Equals(switchMode, "Toggle", StringComparison.OrdinalIgnoreCase) ? " (Toggle)" : " (Press)");
            }

            DescribeTextChanged?.Invoke(this, EventArgs.Empty);
            OnPropertyChanged(nameof(DescribeText));
        }

        public void UpdateToggleControls()
        {
            IsToggleChanged?.Invoke(this, EventArgs.Empty);
            ShowToggleControlsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetDeviceNum(int deviceNum)
        {
            editorDeviceNum = deviceNum;
        }

        public DS4ControlSettings PrepareSettings()
        {
            DS4ControlSettings settings = new DS4ControlSettings(DS4Controls.None);
            settings.action.actionKey = value;
            // Ensure the keyType passed to the binding window reflects the editor's SwitchMode
            var keyTypeToPass = keyType;
            if (string.Equals(switchMode, "Toggle", StringComparison.OrdinalIgnoreCase))
            {
                keyTypeToPass |= DS4KeyType.Toggle;
            }
            else
            {
                keyTypeToPass &= ~DS4KeyType.Toggle;
            }
            settings.keyType = keyTypeToPass;
            settings.actionType = DS4ControlSettings.ActionType.Key;
            return settings;
        }

        public void ReadSettings(DS4ControlSettings settings, int deviceNum = -1)
        {
            // If the binding produced a Button action, show the button name
            if (settings.actionType == DS4ControlSettings.ActionType.Button)
            {
                lastActionType = DS4ControlSettings.ActionType.Button;
                lastActionBtn = (int)settings.action.actionBtn;
                // Try to use device-specific output type when available
                string btnName;
                try
                {
                    if (deviceNum >= 0)
                    {
                        btnName = Global.getX360ControlString((X360Controls)settings.action.actionBtn, Global.outDevTypeTemp[deviceNum]);
                    }
                    else
                    {
                        btnName = Global.getX360ControlString((X360Controls)settings.action.actionBtn);
                    }
                }
                catch
                {
                    btnName = Global.getX360ControlString((X360Controls)settings.action.actionBtn);
                }

                describeText = btnName;
                // clear numeric key value since this is a button mapping
                value = 0;
                keyType = 0;
                DescribeTextChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            // Default: treat as a key action
            value = (int)settings.action.actionKey;
            keyType = settings.keyType;
            // Update SwitchMode editor value to reflect the binding window's toggle flag
            SwitchMode = keyType.HasFlag(DS4KeyType.Toggle) ? "Toggle" : "Press";
            lastActionType = DS4ControlSettings.ActionType.Key;
            lastActionBtn = -1;
            // Update the describe text so the UI immediately reflects the key binding
            UpdateDescribeText();
            UpdateToggleControls();
        }

        public void SaveAction(SpecialAction action, bool edit = false)
        {
            // If last binding was a Button, save as Button type
            if (lastActionType == DS4ControlSettings.ActionType.Button && lastActionBtn >= 0)
            {
                Global.SaveAction(action.name, action.controls, 10, lastActionBtn.ToString(), edit);
                return;
            }

            // Determine SwitchMode to save: use explicit editor selection when available,
            // otherwise fallback to existing keyType flag behavior.
            string modeToSave = !string.IsNullOrEmpty(switchMode) ? switchMode : (keyType.HasFlag(DS4KeyType.Toggle) ? "Toggle" : "Press");
            Global.SaveAction(action.name, action.controls, 4,
                $"{value}{(keyType.HasFlag(DS4KeyType.ScanCode) ? " Scan Code" : "")}", edit,
                extras: $"{modeToSave}\n{action.ucontrols}");
        }

        public override bool IsValid(SpecialAction action)
        {
            ClearOldErrors();

            bool valid = true;
            List<string> valueErrors = new List<string>();
            List<string> toggleErrors = new List<string>();

            if (lastActionType == DS4ControlSettings.ActionType.Key)
            {
                if (value == 0)
                {
                    valueErrors.Add("No key defined");
                    errors["Value"] = valueErrors;
                    RaiseErrorsChanged("Value");
                }
            }
            else if (lastActionType == DS4ControlSettings.ActionType.Button)
            {
                if (lastActionBtn < 0)
                {
                    valueErrors.Add("No button defined");
                    errors["Value"] = valueErrors;
                    RaiseErrorsChanged("Value");
                }
            }
            // If explicit SwitchMode says Toggle then require unload triggers
            if (string.Equals(switchMode, "Toggle", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(action.ucontrols))
            {
                toggleErrors.Add("No unload triggers specified");
                errors["UnloadError"] = toggleErrors;
                RaiseErrorsChanged("UnloadError");
            }

            return valid;
        }

        public override void ClearOldErrors()
        {
            if (errors.Count > 0)
            {
                errors.Clear();
                RaiseErrorsChanged("Value");
                RaiseErrorsChanged("UnloadError");
            }
        }
    }
}
