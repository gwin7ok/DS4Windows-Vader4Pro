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

    public class GyroDirectionalSwipeInfo
    {
        public enum XAxisSwipe : ushort
        {
            Yaw,
            Roll,
        }

        public const string DEFAULT_TRIGGERS = "-1";
        public const int DEFAULT_GYRO_DIR_SPEED = 80;
        public const bool DEFAULT_TRIGGER_COND = true;
        public const bool DEFAULT_TRIGGER_TURNS = true;
        public const XAxisSwipe DEFAULT_X_AXIS = XAxisSwipe.Yaw;
        public const int DEFAULT_DELAY_TIME = 0;

        public int deadzoneX = DEFAULT_GYRO_DIR_SPEED;
        public int deadzoneY = DEFAULT_GYRO_DIR_SPEED;
        public string triggers = DEFAULT_TRIGGERS;
        public bool triggerCond = DEFAULT_TRIGGER_COND;
        public bool triggerTurns = DEFAULT_TRIGGER_TURNS;
        public XAxisSwipe xAxis = DEFAULT_X_AXIS;
        public int delayTime = DEFAULT_DELAY_TIME;

        public void Reset()
        {
            deadzoneX = DEFAULT_GYRO_DIR_SPEED;
            deadzoneY = DEFAULT_GYRO_DIR_SPEED;
            triggers = DEFAULT_TRIGGERS;
            triggerCond = DEFAULT_TRIGGER_COND;
            triggerTurns = DEFAULT_TRIGGER_TURNS;
            xAxis = DEFAULT_X_AXIS;
            delayTime = DEFAULT_DELAY_TIME;
        }
    }
}