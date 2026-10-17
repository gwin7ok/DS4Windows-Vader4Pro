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
using System.Diagnostics;
using DS4WinWPF;

namespace DS4Windows
{

    public class GyroAverageWindow
    {
        public int x;
        public int y;
        public int z;
        public double accelMagnitude;
        public int numSamples;
        public DateTime start;
        public DateTime stop;

        public int DurationMs   // property
        {
            get
            {
                TimeSpan timeDiff = stop - start;
                return Convert.ToInt32(timeDiff.TotalMilliseconds);
            }
        }

        public GyroAverageWindow()
        {
            Reset();
        }

        public void Reset()
        {
            x = y = z = numSamples = 0;
            accelMagnitude = 0.0;
            start = stop = DateTime.UtcNow;
        }

        public bool StopIfElapsed(int ms)
        {
            DateTime end = DateTime.UtcNow;
            TimeSpan timeDiff = end - start;
            bool shouldStop = Convert.ToInt32(timeDiff.TotalMilliseconds) >= ms;
            if (!shouldStop) stop = end;
            return shouldStop;
        }
        public double GetWeight(int expectedMs)
        {
            if (expectedMs == 0) return 0;
            return Math.Min(1.0, 1.0 * DurationMs / expectedMs);
        }
    }
}