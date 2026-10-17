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

    public class Touch
    {
        private int deltaY;
        private byte touchID;
        private Touch previousTouch;
        private int hwX;
        private int hwY;
        private int deltaX;

        internal Touch(int X, int Y, byte tID, Touch prevTouch = null)
        {
            populate(X, Y, tID, prevTouch);
        }

        public int HwX { get => hwX; set => hwX = value; }
        public int HwY { get => hwY; set => hwY = value; }
        public int DeltaX { get => deltaX; set => deltaX = value; }
        public int DeltaY { get => deltaY; set => deltaY = value; }
        public byte TouchID { get => touchID; set => touchID = value; }
        public Touch PreviousTouch { get => previousTouch; set => previousTouch = value; }

        internal void populate(int X, int Y, byte tID, Touch prevTouch = null)
        {
            HwX = X;
            HwY = Y;
            TouchID = tID;
            PreviousTouch = prevTouch;
            if (PreviousTouch != null)
            {
                DeltaX = X - PreviousTouch.HwX;
                DeltaY = Y - PreviousTouch.HwY;
            }
        }
    }
}
