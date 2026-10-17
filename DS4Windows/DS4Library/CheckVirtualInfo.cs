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

using DS4Windows.InputDevices;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace DS4Windows
{

    public class CheckVirtualInfo : EventArgs
    {
        private string deviceInstanceId;
        public string DeviceInstanceId
        {
            get => deviceInstanceId;
            set => deviceInstanceId = value;
        }

        private string propertyValue;
        public string PropertyValue { get => propertyValue; set => propertyValue = value; }

        public CheckVirtualInfo() : base()
        {
        }
    }
}
