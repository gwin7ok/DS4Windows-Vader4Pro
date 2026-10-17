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

    public struct DS4HapticState : IEquatable<DS4HapticState>
    {
        public DS4LightbarState lightbarState;
        public DS4ForceFeedbackState rumbleState;
        public bool dirty;

        public bool Equals(DS4HapticState other)
        {
            return lightbarState.Equals(other.lightbarState) &&
                rumbleState.Equals(other.rumbleState);
        }

        public bool IsLightBarSet()
        {
            return lightbarState.IsLightBarSet();
        }

        public bool IsRumbleSet()
        {
            const byte zero = 0;
            return rumbleState.RumbleMotorsExplicitlyOff || rumbleState.RumbleMotorStrengthLeftHeavySlow != zero || rumbleState.RumbleMotorStrengthRightLightFast != zero;
        }
    }
}