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
using System.ComponentModel;
using System.Threading;

using System.Runtime.InteropServices;
using System.Diagnostics;

using System.Linq;
using System.Drawing;

using DS4WinWPF.DS4Control;

namespace DS4Windows
{

    public struct DS4LightbarState : IEquatable<DS4LightbarState>
    {
        public DS4Color LightBarColor;
        public bool LightBarExplicitlyOff;
        public byte LightBarFlashDurationOn, LightBarFlashDurationOff;

        public bool Equals(DS4LightbarState other)
        {
            return LightBarColor.Equals(other.LightBarColor) &&
                LightBarExplicitlyOff == other.LightBarExplicitlyOff &&
                LightBarFlashDurationOn == other.LightBarFlashDurationOn &&
                LightBarFlashDurationOff == other.LightBarFlashDurationOff;
        }

        public bool IsLightBarSet()
        {
            return LightBarExplicitlyOff || LightBarColor.red != 0 || LightBarColor.green != 0 || LightBarColor.blue != 0;
        }
    }
}