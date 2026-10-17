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
using DS4Windows.StickModifiers;
using DS4WinWPF.DS4Control;
using Sensorit.Base;
using DS4Windows.InputDevices;

namespace DS4Windows
{

    public class StickAntiSnapbackInfo
    {
        public const double DEFAULT_DELTA = 135;
        public const int DEFAULT_TIMEOUT = 50;
        public const bool DEFAULT_ENABLED = false;

        public bool enabled = DEFAULT_ENABLED;
        public double delta = DEFAULT_DELTA;
        public int timeout = DEFAULT_TIMEOUT;
    }
}