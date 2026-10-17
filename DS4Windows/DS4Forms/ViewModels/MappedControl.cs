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

using DS4Windows;
using DS4Windows.DI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DS4WinWPF.DS4Forms.ViewModels
{

    public class MappedControl
    {
        // Phase6-Step10-2-5: Global.GetDS4CSetting／Global.defaultButtonMapping の置換先（必須引数）
        private readonly IProfileSettingsService profileSettingsService;
        private int devIndex;
        private OutContType devType;
        private DS4Controls control;
        private DS4ControlSettings setting;
        private string controlName;
        private string mappingName;
        private string shiftMappingName;

        public int DevIndex { get => devIndex; }
        public DS4Controls Control { get => control; }
        public DS4ControlSettings Setting { get => setting; }
        public string ControlName { get => controlName; }
        public string MappingName { get => mappingName; }
        public OutContType DevType
        {
            get => devType;
            set
            {
                devType = value;
                DevTypeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string ShiftMappingName { get => shiftMappingName; set => shiftMappingName = value; }

        public event EventHandler DevTypeChanged;

        public event EventHandler MappingNameChanged;

        public MappedControl(int devIndex, DS4Controls control, string controlName,
            OutContType devType, IProfileSettingsService profileSettingsService, bool initMap = false)
        {
            this.profileSettingsService = profileSettingsService ?? throw new ArgumentNullException(nameof(profileSettingsService));
            this.devIndex = devIndex;
            this.devType = devType;
            this.control = control;
            this.controlName = controlName;
            setting = profileSettingsService.GetDS4CSetting(devIndex, control);
            //mappingName = "?";
            if (initMap)
            {
                mappingName = GetMappingString();
                if (HasShiftAction())
                {
                    shiftMappingName = ShiftTrigger(setting.shiftTrigger) + " -> " + GetMappingString(true);
                }
            }

            DevTypeChanged += MappedControl_DevTypeChanged;
        }

        private void MappedControl_DevTypeChanged(object sender, EventArgs e)
        {
            UpdateMappingName();
        }

        public void UpdateMappingName()
        {
            mappingName = GetMappingString();
            if (HasShiftAction())
            {
                shiftMappingName = ShiftTrigger(setting.shiftTrigger) + " -> " + GetMappingString(true);
            }
            else
            {
                shiftMappingName = "";
            }

            MappingNameChanged?.Invoke(this, EventArgs.Empty);
        }

        public string GetMappingString(bool shift = false)
        {
            string temp = Properties.Resources.Unassigned;
            ControlActionData action = !shift ? setting.action : setting.shiftAction;
            bool sc = !shift ? setting.keyType.HasFlag(DS4KeyType.ScanCode) :
                setting.shiftKeyType.HasFlag(DS4KeyType.ScanCode);
            bool extra = control >= DS4Controls.GyroXPos && control <= DS4Controls.SwipeDown;
            DS4ControlSettings.ActionType actionType = !shift ? setting.actionType : setting.shiftActionType;
            if (actionType != DS4ControlSettings.ActionType.Default)
            {
                if (actionType == DS4ControlSettings.ActionType.Key)
                {
                    //return (Keys)int.Parse(action.ToString()) + (sc ? " (" + Properties.Resources.ScanCode + ")" : "");
                    temp = KeyInterop.KeyFromVirtualKey(action.actionKey) + (sc ? " (" + Properties.Resources.ScanCode + ")" : "");
                }
                else if (actionType == DS4ControlSettings.ActionType.Macro)
                {
                    temp = Properties.Resources.Macro + (sc ? " (" + Properties.Resources.ScanCode + ")" : "");
                }
                else if (actionType == DS4ControlSettings.ActionType.Button)
                {
                    string tag;
                    tag = X360ControlTables.getX360ControlString((X360Controls)action.actionBtn, devType);
                    temp = tag;
                }
                else
                {
                    temp = X360ControlTables.getX360ControlString(profileSettingsService.GetDefaultButtonMapping()[(int)control], devType);
                }
            }
            else if (!extra && !shift)
            {
                X360Controls tempOutControl = profileSettingsService.GetDefaultButtonMapping()[(int)control];
                if (tempOutControl != X360Controls.None)
                {
                    temp = X360ControlTables.getX360ControlString(tempOutControl, devType);
                }
            }
            else if (shift)
                temp = "";

            return temp;
        }

        public bool HasShiftAction()
        {
            return setting.shiftActionType != DS4ControlSettings.ActionType.Default;
        }

        private static string ShiftTrigger(int trigger)
        {
            switch (trigger)
            {
                case 1: return "Cross";
                case 2: return "Circle";
                case 3: return "Square";
                case 4: return "Triangle";
                case 5: return "Options";
                case 6: return "Share";
                case 7: return "Dpad Up";
                case 8: return "Dpad Down";
                case 9: return "Dpad Left";
                case 10: return "Dpad Right";
                case 11: return "PS";
                case 12: return "L1";
                case 13: return "R1";
                case 14: return "L2";
                case 15: return "R2";
                case 16: return "L3";
                case 17: return "R3";
                case 18: return "Left Touch";
                case 19: return "Upper Touch";
                case 20: return "Multi Touch";
                case 21: return "Right Touch";
                case 22: return Properties.Resources.TiltUp;
                case 23: return Properties.Resources.TiltDown;
                case 24: return Properties.Resources.TiltLeft;
                case 25: return Properties.Resources.TiltRight;
                case 26: return "Finger on Touchpad";
                case 27: return "Mute";
                case 28: return "Capture";
                case 29: return "Side L";
                case 30: return "Side R";
                case 31: return "Function Left";
                case 32: return "Function Right";
                case 33: return "Bottom Left Paddle";
                case 43: return "Bottom Right Paddle";
                default: return "";
            }
        }
    }
}
