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

    public class ControlSettingsGroup
    {
        public List<DS4ControlSettings> LS = new List<DS4ControlSettings>();
        public List<DS4ControlSettings> RS = new List<DS4ControlSettings>();
        public DS4ControlSettings L2;
        public DS4ControlSettings L2FullPull;
        public DS4ControlSettings R2;
        public DS4ControlSettings R2FullPull;

        public DS4ControlSettings GyroSwipeLeft;
        public DS4ControlSettings GyroSwipeRight;
        public DS4ControlSettings GyroSwipeUp;
        public DS4ControlSettings GyroSwipeDown;

        public List<DS4ControlSettings> ControlButtons =
            new List<DS4ControlSettings>();

        public List<DS4ControlSettings> ExtraDeviceButtons =
            new List<DS4ControlSettings>();

        private List<DS4ControlSettings> settingsList;

        public ControlSettingsGroup(List<DS4ControlSettings> settingsList)
        {
            LS.Add(settingsList[(int)DS4Controls.LSOuter - 1]);
            for (int i = (int)DS4Controls.LXNeg; i <= (int)DS4Controls.LYPos; i++)
            {
                LS.Add(settingsList[i - 1]);
            }

            RS.Add(settingsList[(int)DS4Controls.RSOuter - 1]);
            for (int i = (int)DS4Controls.RXNeg; i <= (int)DS4Controls.RYPos; i++)
            {
                RS.Add(settingsList[i - 1]);
            }

            L2 = settingsList[(int)DS4Controls.L2 - 1];
            R2 = settingsList[(int)DS4Controls.R2 - 1];

            L2FullPull = settingsList[(int)DS4Controls.L2FullPull - 1];
            R2FullPull = settingsList[(int)DS4Controls.R2FullPull - 1];

            GyroSwipeLeft = settingsList[(int)DS4Controls.GyroSwipeLeft - 1];
            GyroSwipeRight = settingsList[(int)DS4Controls.GyroSwipeRight - 1];
            GyroSwipeUp = settingsList[(int)DS4Controls.GyroSwipeUp - 1];
            GyroSwipeDown = settingsList[(int)DS4Controls.GyroSwipeDown - 1];

            ControlButtons.Add(settingsList[(int)DS4Controls.L1 - 1]);
            ControlButtons.Add(settingsList[(int)DS4Controls.L3 - 1]);
            ControlButtons.Add(settingsList[(int)DS4Controls.R1 - 1]);
            ControlButtons.Add(settingsList[(int)DS4Controls.R3 - 1]);

            // Populate basic buttons used for mapping before DualSense Edge extra
            // buttons in DS4Controls enum
            for (int i = (int)DS4Controls.Square; i <= (int)DS4Controls.Mute; i++)
            {
                ControlButtons.Add(settingsList[i - 1]);
            }

            // Populate basic buttons used for mapping after DualSense Edge extra
            // buttons in DS4Controls enum
            for (int i = (int)DS4Controls.GyroXPos; i <= (int)DS4Controls.SwipeDown; i++)
            {
                ControlButtons.Add(settingsList[i - 1]);
            }

            this.settingsList = settingsList;
        }

        public void EstablishExtraButtons(List<DS4Controls> buttonList)
        {
            foreach (DS4Controls control in buttonList)
            {
                ExtraDeviceButtons.Add(settingsList[(int)control - 1]);
            }
        }

        public void ResetExtraButtons()
        {
            ExtraDeviceButtons.Clear();
        }
    }
}

