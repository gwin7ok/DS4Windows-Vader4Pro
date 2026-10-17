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

    public class SixAxis
    {
        public const int ACC_RES_PER_G = 8192;
        public const float F_ACC_RES_PER_G = ACC_RES_PER_G;
        public const int GYRO_RES_IN_DEG_SEC = 16;
        public const float F_GYRO_RES_IN_DEG_SEC = GYRO_RES_IN_DEG_SEC;

        public int gyroYaw, gyroPitch, gyroRoll, accelX, accelY, accelZ;
        public int outputAccelX, outputAccelY, outputAccelZ;
        public bool outputGyroControls;
        public double accelXG, accelYG, accelZG;
        public double angVelYaw, angVelPitch, angVelRoll;
        public int gyroYawFull, gyroPitchFull, gyroRollFull;
        public int accelXFull, accelYFull, accelZFull;
        public double elapsed;
        public SixAxis previousAxis = null;

        private double tempDouble = 0d;

        public SixAxis(int X, int Y, int Z,
            int aX, int aY, int aZ,
            double elapsedDelta, SixAxis prevAxis = null)
        {
            populate(X, Y, Z, aX, aY, aZ, elapsedDelta, prevAxis);
        }

        public void copy(SixAxis src)
        {
            gyroYaw = src.gyroYaw;
            gyroPitch = src.gyroPitch;
            gyroRoll = src.gyroRoll;

            gyroYawFull = src.gyroYawFull;
            accelXFull = src.accelXFull; accelYFull = src.accelYFull; accelZFull = src.accelZFull;

            angVelYaw = src.angVelYaw;
            angVelPitch = src.angVelPitch;
            angVelRoll = src.angVelRoll;

            accelXG = src.accelXG;
            accelYG = src.accelYG;
            accelZG = src.accelZG;

            // Put accel ranges between 0 - 128 abs
            accelX = src.accelX;
            accelY = src.accelY;
            accelZ = src.accelZ;
            outputAccelX = accelX;
            outputAccelY = accelY;
            outputAccelZ = accelZ;

            elapsed = src.elapsed;
            previousAxis = src.previousAxis;
            outputGyroControls = src.outputGyroControls;
        }

        public void populate(int X, int Y, int Z,
            int aX, int aY, int aZ,
            double elapsedDelta, SixAxis prevAxis = null)
        {
            gyroYaw = -X / 256;
            gyroPitch = Y / 256;
            gyroRoll = -Z / 256;

            gyroYawFull = -X; gyroPitchFull = Y; gyroRollFull = -Z;
            accelXFull = -aX; accelYFull = -aY; accelZFull = aZ;

            angVelYaw = gyroYawFull / F_GYRO_RES_IN_DEG_SEC;
            angVelPitch = gyroPitchFull / F_GYRO_RES_IN_DEG_SEC;
            angVelRoll = gyroRollFull / F_GYRO_RES_IN_DEG_SEC;

            accelXG = tempDouble = accelXFull / F_ACC_RES_PER_G;
            accelYG = tempDouble = accelYFull / F_ACC_RES_PER_G;
            accelZG = tempDouble = accelZFull / F_ACC_RES_PER_G;

            // Put accel ranges between 0 - 128 abs
            accelX = -aX / 64;
            accelY = -aY / 64;
            accelZ = aZ / 64;

            // Leave blank and have mapping routine alter values as needed
            outputAccelX = 0;
            outputAccelY = 0;
            outputAccelZ = 0;
            outputGyroControls = false;

            elapsed = elapsedDelta;
            previousAxis = prevAxis;
        }
    }
}