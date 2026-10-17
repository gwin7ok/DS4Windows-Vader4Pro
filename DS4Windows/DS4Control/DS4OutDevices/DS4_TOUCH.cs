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
    [StructLayout(LayoutKind.Sequential, Size = 9)]
    unsafe struct DS4_TOUCH
    {
        public byte bPacketCounter;
        public byte bIsUpTrackingNum1;
        public fixed byte bTouchData1[3];
        public byte bIsUpTrackingNum2;
        public fixed byte bTouchData2[3];
    }
}
