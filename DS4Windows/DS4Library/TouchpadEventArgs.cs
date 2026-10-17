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

namespace DS4Windows
{
    public class TouchpadEventArgs : EventArgs
    {
        private readonly Touch[] touches = null;
        private readonly DateTime timeStamp;
        private readonly bool touchButtonPressed;
        private readonly bool touchActive;

        public Touch[] Touches => touches;

        public DateTime TimeStamp => timeStamp;

        public bool TouchButtonPressed => touchButtonPressed;

        public bool TouchActive => touchActive;

        public TouchpadEventArgs(DateTime utcTimestamp, bool tButtonDown, bool touchActive, Touch t0, Touch t1 = null)
        {
            if (t1 != null)
            {
                touches = new Touch[2];
                touches[0] = t0;
                touches[1] = t1;
            }
            else if (t0 != null)
            {
                touches = new Touch[1];
                touches[0] = t0;
            }

            touchButtonPressed = tButtonDown;
            this.touchActive = touchActive;
            timeStamp = utcTimestamp;
        }
    }
}
