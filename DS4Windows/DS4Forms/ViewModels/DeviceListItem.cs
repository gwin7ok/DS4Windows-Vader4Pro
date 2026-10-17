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
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels.Util;
using LEDBarMode = DS4Windows.DualSenseControllerOptions.LEDBarMode;
using MuteLEDMode = DS4Windows.DualSenseControllerOptions.MuteLEDMode;
using LinkMode = DS4Windows.JoyConDeviceOptions.LinkMode;
using JoinedGyroProvider = DS4Windows.JoyConDeviceOptions.JoinedGyroProvider;

namespace DS4WinWPF.DS4Forms.ViewModels
{

    public class DeviceListItem
    {
        private DS4Device device;
        public DS4Device Device { get => device; }

        public string IdText
        {
            get => $"{device.DisplayName} ({device.MacAddress})";
        }

        public DeviceListItem(DS4Device device)
        {
            this.device = device;
        }
    }

}
