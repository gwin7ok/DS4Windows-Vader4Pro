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
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.ComponentModel;
using System.Threading;
using System.Runtime.InteropServices;

namespace DS4Windows
{

    public enum DsConnection : byte
    {
        [Description("None")]
        None = 0x00,
        [Description("Usb")]
        Usb = 0x01,
        [Description("Bluetooth")]
        Bluetooth = 0x02
    };
}
