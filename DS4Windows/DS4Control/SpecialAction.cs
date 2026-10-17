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

using DS4Windows.DS4Control;
using DS4Windows.InputDevices;
using DS4Windows.StickModifiers;
using DS4WinWPF.DS4Control.DTOXml;
using DS4WinWPF.DS4Forms.ViewModels;
using Sensorit.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;
using DS4WinWPF;
using DS4WinWPF.ApiDTO;
using WpfScreenHelper;
using static DS4Windows.Mouse;
using static DS4Windows.Util;
using LightbarMacro = DS4WinWPF.DS4Forms.ViewModels.LightbarMacro;
namespace DS4Windows
{

    public class SpecialAction
    {
        public enum ActionTypeId { None, Key, Program, Profile, Macro, DisconnectBT, BatteryCheck, MultiAction, XboxGameDVR, SASteeringWheelEmulationCalibrate, GyroCalibrate, Button }

        public enum KeyButtonSwitchModeEnum { Press, Toggle }

        public string name;
        public List<DS4Controls> trigger = new List<DS4Controls>();
        public string type;
        public ActionTypeId typeID;
        public string controls;
        public List<int> macro = new List<int>();
        public string details;
        public List<DS4Controls> uTrigger = new List<DS4Controls>();
        public string ucontrols;
        public double delayTime = 0;
        public string extra;
        // null when not applicable (type != Key && type != Button) or not set
        public KeyButtonSwitchModeEnum? KeyButtonSwitchMode = null;
        public bool pressRelease = false;
        public DS4KeyType keyType;
        public bool tappedOnce = false;
        public bool firstTouch = false;
        public bool secondtouchbegin = false;
        public DateTime pastTime;
        public DateTime firstTap;
        public DateTime TimeofEnd;
        public bool automaticUntrigger = false;
        public string prevProfileName;  // Name of the previous profile where automaticUntrigger would jump back to (could be regular or temporary profile. Empty name is the same as regular profile)
        public bool prevProfileWasTemporary;
        public bool synchronized = false; // If the same trigger has both "key down" and "key released" macros then run those synchronized if this attribute is TRUE (ie. key down macro fully completed before running the key release macro)
        public bool keepKeyState = false; // By default special action type "Macro" resets all keys used in the macro back to default "key up" state after completing the macro even when the macro itself doesn't do it explicitly. If this is TRUE then key states are NOT reset automatically (macro is expected to do it or to leave a key to down state on purpose)

        public bool IsTemporaryProfileAction =>
            typeID == ActionTypeId.Profile &&
            (automaticUntrigger || (uTrigger != null && uTrigger.Count > 0));

        public SpecialAction(string name, string controls, string type, string details, double delay = 0, string extras = "")
        {
            this.name = name;
            this.type = type;
            this.typeID = ActionTypeId.None;
            this.controls = controls;
            delayTime = delay;
            string[] ctrls = controls.Split('/');
            foreach (string s in ctrls)
                trigger.Add(getDS4ControlsByName(s));

            if (type == "Key")
            {
                typeID = ActionTypeId.Key;
                this.details = details.Split(' ')[0];
                if (!string.IsNullOrEmpty(extras))
                {
                    extra = extras;
                    string[] exts = extras.Split('\n');
                    // exts[0] is the SwitchMode value (new). Expected values: "Press" or "Toggle".
                    string switchMode = exts.Length > 0 ? exts[0] : string.Empty;
                    if (!string.IsNullOrEmpty(switchMode))
                    {
                        if (switchMode.Equals("Toggle", StringComparison.OrdinalIgnoreCase))
                        {
                            KeyButtonSwitchMode = KeyButtonSwitchModeEnum.Toggle;
                            keyType |= DS4KeyType.Toggle;
                        }
                        else if (switchMode.Equals("Press", StringComparison.OrdinalIgnoreCase))
                        {
                            KeyButtonSwitchMode = KeyButtonSwitchModeEnum.Press;
                        }
                    }

                    if (exts.Length > 1 && !string.IsNullOrEmpty(exts[1]))
                    {
                        this.ucontrols = exts[1];
                        string[] uctrls = exts[1].Split('/');
                        foreach (string s in uctrls)
                            uTrigger.Add(getDS4ControlsByName(s));
                    }
                }

                if (details.Contains("Scan Code"))
                    keyType |= DS4KeyType.ScanCode;
            }
            else if (type == "Button")
            {
                typeID = ActionTypeId.Button;
                this.details = details; // store button id as details
                // Button may also carry SwitchMode in extras first line
                if (!string.IsNullOrEmpty(extras))
                {
                    extra = extras;
                    string[] exts = extras.Split('\n');
                    string switchMode = exts.Length > 0 ? exts[0] : string.Empty;
                    if (!string.IsNullOrEmpty(switchMode))
                    {
                        if (switchMode.Equals("Toggle", StringComparison.OrdinalIgnoreCase))
                            KeyButtonSwitchMode = KeyButtonSwitchModeEnum.Toggle;
                        else if (switchMode.Equals("Press", StringComparison.OrdinalIgnoreCase))
                            KeyButtonSwitchMode = KeyButtonSwitchModeEnum.Press;
                    }
                }
            }
            else if (type == "Program")
            {
                typeID = ActionTypeId.Program;
                this.details = details;
                if (extras != string.Empty)
                    extra = extras;
            }
            else if (type == "Profile")
            {
                typeID = ActionTypeId.Profile;
                this.details = details;
                if (extras != string.Empty)
                {
                    extra = extras;
                }
            }
            else if (type == "Macro")
            {
                typeID = ActionTypeId.Macro;
                this.details = details;

                string[] macs = details.Split('/');
                foreach (string s in macs)
                {
                    int v;
                    if (int.TryParse(s, out v))
                        macro.Add(v);
                }

                if (extras != string.Empty)
                {
                    extra = extras;

                    if (extras.Contains("Scan Code"))
                        keyType |= DS4KeyType.ScanCode;
                    if (extras.Contains("RunOnRelease"))
                        pressRelease = true;
                    if (extras.Contains("Sync"))
                        synchronized = true;
                    if (extras.Contains("KeepKeyState"))
                        keepKeyState = true;
                    if (extras.Contains("Repeat"))
                        keyType |= DS4KeyType.RepeatMacro;
                }
            }
            else if (type == "DisconnectBT")
            {
                typeID = ActionTypeId.DisconnectBT;
                this.details = details;
            }
            else if (type == "BatteryCheck")
            {
                typeID = ActionTypeId.BatteryCheck;
                string[] dets = details.Split('|');
                this.details = string.Join(",", dets);
            }
            else if (type == "MultiAction")
            {
                typeID = ActionTypeId.MultiAction;
                this.details = details;
            }
            else if (type == "XboxGameDVR")
            {
                this.typeID = ActionTypeId.XboxGameDVR;
                string[] dets = details.Split(',');
                List<string> macros = new List<string>();
                //string dets = "";
                int typeT = 0;
                for (int i = 0; i < 3; i++)
                {
                    if (int.TryParse(dets[i], out typeT))
                    {
                        switch (typeT)
                        {
                            case 0: macros.Add("91/71/71/91"); break;
                            case 1: macros.Add("91/164/82/82/164/91"); break;
                            case 2: macros.Add("91/164/44/44/164/91"); break;
                            case 3: macros.Add(dets[3] + "/" + dets[3]); break;
                            case 4: macros.Add("91/164/71/71/164/91"); break;
                        }
                    }
                }
                this.type = "MultiAction";
                type = "MultiAction";
                this.details = string.Join(",", macros);
            }
            else if (type == "SASteeringWheelEmulationCalibrate")
            {
                typeID = ActionTypeId.SASteeringWheelEmulationCalibrate;
            }
            else if (type == "GyroCalibration")
            {
                typeID = ActionTypeId.GyroCalibrate;
            }
            else
                this.details = details;

            if (type != "Key" && !string.IsNullOrEmpty(extras))
            {
                this.ucontrols = extras;
                string[] uctrls = extras.Split('/');
                foreach (string s in uctrls)
                {
                    if (s == "AutomaticUntrigger") this.automaticUntrigger = true;
                    else uTrigger.Add(getDS4ControlsByName(s));
                }
            }
        }

        private DS4Controls getDS4ControlsByName(string key)
        {
            switch (key)
            {
                case "Share": return DS4Controls.Share;
                case "L3": return DS4Controls.L3;
                case "R3": return DS4Controls.R3;
                case "Options": return DS4Controls.Options;
                case "Up": return DS4Controls.DpadUp;
                case "Right": return DS4Controls.DpadRight;
                case "Down": return DS4Controls.DpadDown;
                case "Left": return DS4Controls.DpadLeft;

                case "L1": return DS4Controls.L1;
                case "R1": return DS4Controls.R1;
                case "Triangle": return DS4Controls.Triangle;
                case "Circle": return DS4Controls.Circle;
                case "Cross": return DS4Controls.Cross;
                case "Square": return DS4Controls.Square;

                case "PS": return DS4Controls.PS;
                case "Mute": return DS4Controls.Mute;
                case "Function Left": return DS4Controls.FnL;
                case "Function Right": return DS4Controls.FnR;
                case "Bottom Left Paddle": return DS4Controls.BLP;
                case "Bottom Right Paddle": return DS4Controls.BRP;
                case "Capture": return DS4Controls.Capture;
                case "SideL": return DS4Controls.SideL;
                case "SideR": return DS4Controls.SideR;
                case "Left Stick Left": return DS4Controls.LXNeg;
                case "Left Stick Up": return DS4Controls.LYNeg;
                case "Right Stick Left": return DS4Controls.RXNeg;
                case "Right Stick Up": return DS4Controls.RYNeg;

                case "Left Stick Right": return DS4Controls.LXPos;
                case "Left Stick Down": return DS4Controls.LYPos;
                case "Right Stick Right": return DS4Controls.RXPos;
                case "Right Stick Down": return DS4Controls.RYPos;
                case "L2": return DS4Controls.L2;
                case "L2 Full Pull": return DS4Controls.L2FullPull;
                case "R2": return DS4Controls.R2;
                case "R2 Full Pull": return DS4Controls.R2FullPull;

                case "Left Touch": return DS4Controls.TouchLeft;
                case "Multitouch": return DS4Controls.TouchMulti;
                case "Upper Touch": return DS4Controls.TouchUpper;
                case "Right Touch": return DS4Controls.TouchRight;

                case "Swipe Up": return DS4Controls.SwipeUp;
                case "Swipe Down": return DS4Controls.SwipeDown;
                case "Swipe Left": return DS4Controls.SwipeLeft;
                case "Swipe Right": return DS4Controls.SwipeRight;

                case "Tilt Up": return DS4Controls.GyroZNeg;
                case "Tilt Down": return DS4Controls.GyroZPos;
                case "Tilt Left": return DS4Controls.GyroXPos;
                case "Tilt Right": return DS4Controls.GyroXNeg;

                case "Touch Started": return DS4Controls.TouchStarted;
                case "Touch Ended": return DS4Controls.TouchEnded;

            }

            return 0;
        }
    }
}

