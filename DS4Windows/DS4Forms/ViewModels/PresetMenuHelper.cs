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
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.StickModifiers;
using DS4WinWPF.DS4Forms.ViewModels.Util;
using DS4Windows.InputDevices;

namespace DS4WinWPF.DS4Forms.ViewModels
{

    public class PresetMenuHelper
    {
        public enum ControlSelection : uint
        {
            None,
            LeftStick,
            RightStick,
            DPad,
            FaceButtons,
        }

        private Dictionary<ControlSelection, string> presetInputLabelDict =
            new Dictionary<ControlSelection, string>()
            {
                [ControlSelection.None] = "None",
                [ControlSelection.DPad] = "DPad",
                [ControlSelection.LeftStick] = "Left Stick",
                [ControlSelection.RightStick] = "Right Stick",
                [ControlSelection.FaceButtons] = "Face Buttons",
            };

        public Dictionary<ControlSelection, string> PresetInputLabelDict
        {
            get => presetInputLabelDict;
        }

        public string PresetInputLabel
        {
            get => presetInputLabelDict[highlightControl];
        }

        private ControlSelection highlightControl = ControlSelection.None;

        public ControlSelection HighlightControl
        {
            get => highlightControl;
        }

        private int deviceNum;
        private readonly IProfileSettingsService profileSettings;

        // Phase6-Step9-4: 依存は必須引数（Pure DI）
        public PresetMenuHelper(int device, IProfileSettingsService profileSettings)
        {
            deviceNum = device;
            this.profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
        }

        public ControlSelection PresetTagIndex(DS4Controls control)
        {
            ControlSelection controlInput = ControlSelection.None;
            switch (control)
            {
                case DS4Controls.DpadUp:
                case DS4Controls.DpadDown:
                case DS4Controls.DpadLeft:
                case DS4Controls.DpadRight:
                    controlInput = ControlSelection.DPad;
                    break;
                case DS4Controls.LXNeg:
                case DS4Controls.LXPos:
                case DS4Controls.LYNeg:
                case DS4Controls.LYPos:
                case DS4Controls.L3:
                    controlInput = ControlSelection.LeftStick;
                    break;
                case DS4Controls.RXNeg:
                case DS4Controls.RXPos:
                case DS4Controls.RYNeg:
                case DS4Controls.RYPos:
                case DS4Controls.R3:
                    controlInput = ControlSelection.RightStick;
                    break;
                case DS4Controls.Cross:
                case DS4Controls.Circle:
                case DS4Controls.Triangle:
                case DS4Controls.Square:
                    controlInput = ControlSelection.FaceButtons;
                    break;
                default:
                    break;
            }


            return controlInput;
        }

        public void SetHighlightControl(DS4Controls control)
        {
            ControlSelection controlInput = PresetTagIndex(control);
            highlightControl = controlInput;
        }

        public List<DS4Controls> ModifySettingWithPreset(int baseTag, int subTag)
        {
            List<object> actionBtns = new List<object>(5);
            List<DS4Controls> inputControls = new List<DS4Controls>(5);
            if (baseTag == 0)
            {
                actionBtns.AddRange(new object[5]
                {
                    null, null, null, null, null,
                });
            }
            else if (baseTag == 1)
            {
                switch (subTag)
                {
                    case 0:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.DpadUp, X360Controls.DpadDown,
                            X360Controls.DpadLeft, X360Controls.DpadRight, X360Controls.Unbound,
                        });
                        break;
                    case 1:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.DpadDown, X360Controls.DpadUp,
                            X360Controls.DpadRight, X360Controls.DpadLeft, X360Controls.Unbound,
                        });
                        break;
                    case 2:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.DpadUp, X360Controls.DpadDown,
                            X360Controls.DpadRight, X360Controls.DpadLeft, X360Controls.Unbound,
                        });
                        break;
                    case 3:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.DpadDown, X360Controls.DpadUp,
                            X360Controls.DpadLeft, X360Controls.DpadRight, X360Controls.Unbound,
                        });
                        break;
                    case 4:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.DpadRight, X360Controls.DpadLeft,
                            X360Controls.DpadUp, X360Controls.DpadDown, X360Controls.Unbound,
                        });
                        break;
                    case 5:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.DpadLeft, X360Controls.DpadRight,
                            X360Controls.DpadDown, X360Controls.DpadUp, X360Controls.Unbound,
                        });
                        break;
                    default:
                        break;
                }
            }
            else if (baseTag == 2)
            {
                switch (subTag)
                {
                    case 0:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.LYNeg, X360Controls.LYPos,
                            X360Controls.LXNeg, X360Controls.LXPos, X360Controls.LS,
                        });
                        break;
                    case 1:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.LYPos, X360Controls.LYNeg,
                            X360Controls.LXPos, X360Controls.LXNeg, X360Controls.LS,
                        });
                        break;
                    case 2:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.LYNeg, X360Controls.LYPos,
                            X360Controls.LXPos, X360Controls.LXNeg, X360Controls.LS,
                        });
                        break;
                    case 3:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.LYPos, X360Controls.LYNeg,
                            X360Controls.LXNeg, X360Controls.LXPos, X360Controls.LS,
                        });
                        break;
                    case 4:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.LXPos, X360Controls.LXNeg,
                            X360Controls.LYNeg, X360Controls.LYPos, X360Controls.LS,
                        });
                        break;
                    case 5:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.LXNeg, X360Controls.LXPos,
                            X360Controls.LYPos, X360Controls.LYNeg, X360Controls.LS,
                        });
                        break;
                    default:
                        break;
                }
            }
            else if (baseTag == 3)
            {
                switch (subTag)
                {
                    case 0:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.RYNeg, X360Controls.RYPos,
                            X360Controls.RXNeg, X360Controls.RXPos, X360Controls.RS,
                        });
                        break;
                    case 1:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.RYPos, X360Controls.RYNeg,
                            X360Controls.RXPos, X360Controls.RXNeg, X360Controls.RS,
                        });
                        break;
                    case 2:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.RYNeg, X360Controls.RYPos,
                            X360Controls.RXPos, X360Controls.RXNeg, X360Controls.RS,
                        });
                        break;
                    case 3:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.RYPos, X360Controls.RYNeg,
                            X360Controls.RXNeg, X360Controls.RXPos, X360Controls.RS,
                        });
                        break;
                    case 4:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.RXPos, X360Controls.RXNeg,
                            X360Controls.RYNeg, X360Controls.RYPos, X360Controls.RS,
                        });
                        break;
                    case 5:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.RXNeg, X360Controls.RXPos,
                            X360Controls.RYPos, X360Controls.RYNeg, X360Controls.RS,
                        });
                        break;
                    default:
                        break;
                }
            }
            else if (baseTag == 4)
            {
                switch (subTag)
                {
                    case 0:
                        // North, South, West, East
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.Y, X360Controls.A, X360Controls.X, X360Controls.B, X360Controls.Unbound,
                        });
                        break;
                    case 1:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.B, X360Controls.X, X360Controls.Y, X360Controls.A, X360Controls.Unbound,
                        });
                        break;
                    case 2:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.X, X360Controls.B, X360Controls.A, X360Controls.Y, X360Controls.Unbound,
                        });
                        break;
                    default:
                        break;
                }
            }
            else if (baseTag == 5)
            {
                switch (subTag)
                {
                    case 0:
                        // North, South, West, East
                        actionBtns.AddRange(new object[5]
                        {
                            KeyInterop.VirtualKeyFromKey(Key.W), KeyInterop.VirtualKeyFromKey(Key.S),
                            KeyInterop.VirtualKeyFromKey(Key.A), KeyInterop.VirtualKeyFromKey(Key.D),
                            X360Controls.Unbound,
                        });
                        break;
                    case 1:
                        actionBtns.AddRange(new object[5]
                        {
                            KeyInterop.VirtualKeyFromKey(Key.D), KeyInterop.VirtualKeyFromKey(Key.A),
                            KeyInterop.VirtualKeyFromKey(Key.W), KeyInterop.VirtualKeyFromKey(Key.S),
                            X360Controls.Unbound,
                        });
                        break;
                    case 2:
                        actionBtns.AddRange(new object[5]
                        {
                            KeyInterop.VirtualKeyFromKey(Key.A), KeyInterop.VirtualKeyFromKey(Key.D),
                            KeyInterop.VirtualKeyFromKey(Key.S), KeyInterop.VirtualKeyFromKey(Key.W),
                            X360Controls.Unbound,
                        });
                        break;
                    default:
                        break;
                }
            }
            else if (baseTag == 6)
            {
                switch (subTag)
                {
                    case 0:
                        // North, South, West, East
                        actionBtns.AddRange(new object[5]
                        {
                            KeyInterop.VirtualKeyFromKey(Key.Up), KeyInterop.VirtualKeyFromKey(Key.Down),
                            KeyInterop.VirtualKeyFromKey(Key.Left), KeyInterop.VirtualKeyFromKey(Key.Right),
                            X360Controls.Unbound,
                        });
                        break;
                    case 1:
                        actionBtns.AddRange(new object[5]
                        {
                            KeyInterop.VirtualKeyFromKey(Key.Right), KeyInterop.VirtualKeyFromKey(Key.Left),
                            KeyInterop.VirtualKeyFromKey(Key.Up), KeyInterop.VirtualKeyFromKey(Key.Down),
                            X360Controls.Unbound,
                        });
                        break;
                    case 2:
                        actionBtns.AddRange(new object[5]
                        {
                            KeyInterop.VirtualKeyFromKey(Key.Left), KeyInterop.VirtualKeyFromKey(Key.Right),
                            KeyInterop.VirtualKeyFromKey(Key.Down), KeyInterop.VirtualKeyFromKey(Key.Up),
                            X360Controls.Unbound,
                        });
                        break;
                    default:
                        break;
                }
            }
            else if (baseTag == 7)
            {
                switch (subTag)
                {
                    case 0:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.MouseUp, X360Controls.MouseDown,
                            X360Controls.MouseLeft, X360Controls.MouseRight,
                            X360Controls.Unbound,
                        });
                        break;
                    case 1:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.MouseDown, X360Controls.MouseUp,
                            X360Controls.MouseRight, X360Controls.MouseLeft,
                            X360Controls.Unbound,
                        });
                        break;
                    case 2:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.MouseUp, X360Controls.MouseDown,
                            X360Controls.MouseRight, X360Controls.MouseLeft,
                            X360Controls.Unbound,
                        });
                        break;
                    case 3:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.MouseDown, X360Controls.MouseUp,
                            X360Controls.MouseLeft, X360Controls.MouseRight,
                            X360Controls.Unbound,
                        });
                        break;
                    case 4:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.MouseRight, X360Controls.MouseLeft,
                            X360Controls.MouseUp, X360Controls.MouseDown,
                            X360Controls.Unbound,
                        });
                        break;
                    case 5:
                        actionBtns.AddRange(new object[5]
                        {
                            X360Controls.MouseLeft, X360Controls.MouseRight,
                            X360Controls.MouseDown, X360Controls.MouseUp,
                            X360Controls.Unbound,
                        });
                        break;
                    default:
                        break;
                }
            }
            else if (baseTag == 8)
            {
                actionBtns.AddRange(new object[5]
                {
                    X360Controls.Unbound, X360Controls.Unbound,
                    X360Controls.Unbound, X360Controls.Unbound,
                    X360Controls.Unbound,
                });
            }


            switch (highlightControl)
            {
                case ControlSelection.DPad:
                    inputControls.AddRange(new DS4Controls[4]
                    {
                        DS4Controls.DpadUp, DS4Controls.DpadDown,
                        DS4Controls.DpadLeft, DS4Controls.DpadRight,
                    });
                    break;
                case ControlSelection.LeftStick:
                    inputControls.AddRange(new DS4Controls[5]
                    {
                        DS4Controls.LYNeg, DS4Controls.LYPos,
                        DS4Controls.LXNeg, DS4Controls.LXPos, DS4Controls.L3,
                    });
                    break;
                case ControlSelection.RightStick:
                    inputControls.AddRange(new DS4Controls[5]
                    {
                        DS4Controls.RYNeg, DS4Controls.RYPos,
                        DS4Controls.RXNeg, DS4Controls.RXPos, DS4Controls.R3,
                    });
                    break;
                case ControlSelection.FaceButtons:
                    inputControls.AddRange(new DS4Controls[4]
                    {
                        DS4Controls.Triangle, DS4Controls.Cross,
                        DS4Controls.Square, DS4Controls.Circle,
                    });
                    break;
                case ControlSelection.None:
                default:
                    break;
            }

            // GetDefaultButtonMapping() は複製を返すため、ループの前に 1 回だけ取得する
            X360Controls[] defaultButtonMapping = profileSettings.GetDefaultButtonMapping();
            int idx = 0;
            foreach (DS4Controls dsControl in inputControls)
            {
                DS4ControlSettings setting = profileSettings.GetDS4CSetting(deviceNum, dsControl);
                setting.Reset();
                if (idx < actionBtns.Count && actionBtns[idx] != null)
                {
                    object outAct = actionBtns[idx];
                    X360Controls defaultControl = defaultButtonMapping[(int)dsControl];
                    if (!(outAct is X360Controls) || defaultControl != (X360Controls)outAct)
                    {
                        setting.UpdateSettings(false, outAct, null, DS4KeyType.None);
                        profileSettings.RefreshActionAlias(setting, false);
                    }
                }

                idx++;
            }

            return inputControls;
        }
    }
}