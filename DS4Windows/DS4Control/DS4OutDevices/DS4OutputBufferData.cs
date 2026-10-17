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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DS4Windows
{

    /// <summary>
    /// Example struct for converting output report buffer array returned
    /// from IDualShock4Controller.AwaitRawOutputReport method to a struct.
    /// Used for testing and documentation. Probably will not use tbh
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    struct DS4OutputBufferData
    {
        [FieldOffset(0)]
        public byte reportID;
        [FieldOffset(1)]
        public byte featureFlags;
        [FieldOffset(2)]
        public byte padding1;
        [FieldOffset(3)]
        public byte padding2;
        [FieldOffset(4)]
        public byte rightFastRumble;
        [FieldOffset(5)]
        public byte leftSlowRumble;
        [FieldOffset(6)]
        public byte lightbarRedColor;
        [FieldOffset(7)]
        public byte lightbarGreenColor;
        [FieldOffset(8)]
        public byte lightbarBlueColor;
        [FieldOffset(9)]
        public byte flashOnDuration;
        [FieldOffset(10)]
        public byte flashOffDuration;
    }
}
