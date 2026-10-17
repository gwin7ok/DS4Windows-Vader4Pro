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

    public class DS4SixAxis
    {
        //public event EventHandler<SixAxisEventArgs> SixAccelMoved = null;
        public event SixAxisHandler<SixAxisEventArgs> SixAccelMoved = null;
        private SixAxis sPrev = null, now = null;

        // Phase7-Step5 W1: DS3 のジャイロトリガー（Controls モード）が有効かを返す関数。引数はコントローラーのスロット番号。
        // ControlService が、デバイスをスロットへ接続するときに設定する（旧: App.rootHub.touchPad[device] を静的に参照していた）。
        // 未設定のとき（接続前、テスト）は「無効」として扱い、例外にしない。
        public Func<int, bool> GyroControlsTriggerActive { get; set; }
        private CalibData[] calibrationData = new CalibData[6] { new CalibData(), new CalibData(),
            new CalibData(), new CalibData(), new CalibData(), new CalibData()
        };
        private bool calibrationDone = false;

        // for continuous calibration (JoyShockLibrary)
        const int num_gyro_average_windows = 3;
        private int gyro_average_window_front_index = 0;
        const int gyro_average_window_ms = 5000;
        private GyroAverageWindow[] gyro_average_window = new GyroAverageWindow[num_gyro_average_windows];
        private int gyro_offset_x = 0;
        private int gyro_offset_y = 0;
        private int gyro_offset_z = 0;
        private double gyro_accel_magnitude = 1.0f;
        private Stopwatch gyroAverageTimer = new Stopwatch();

        private class SimulatePitchRoll
        {
            private const int SAMPLE_WINDOW = 4;
            private const int AVARAGE_WINDOW = 2;

            private struct AccelXZ
            {
                public int X;
                public int Y;
                public int Z;
                public ulong TimeStamp;
            }

            private Queue<AccelXZ> _cachedQueue = new();

            public void PushSample(int accelX, int accelY, int accelZ, ulong timeStamp)
            {
                if (_cachedQueue.Count >= SAMPLE_WINDOW)
                {
                    var entry = _cachedQueue.Dequeue();
                    entry.X = accelX;
                    entry.Y = accelY;
                    entry.Z = accelZ;
                    entry.TimeStamp = timeStamp;
                    _cachedQueue.Enqueue(entry);
                }
                else
                {
                    _cachedQueue.Enqueue(new AccelXZ { X = accelX, Y = accelY, Z = accelZ, TimeStamp = timeStamp });
                }
            }

            public void GetPitchRoll(ref int pitch, ref int roll)
            {
                if (_cachedQueue.Count < SAMPLE_WINDOW)
                {
                    return;
                }

                var cachedArray = _cachedQueue.ToArray();
                var timediff = (int)(cachedArray[SAMPLE_WINDOW - 1].TimeStamp - cachedArray[0].TimeStamp) * (SAMPLE_WINDOW - AVARAGE_WINDOW) / SAMPLE_WINDOW;
                if (timediff > 0)
                {
                    int avrageX1 = 0, avrageX2 = 0, avrageY1 = 0, avrageY2 = 0, avrageZ1 = 0, avrageZ2 = 0;
                    for (var i = 0; i < AVARAGE_WINDOW; i++)
                    {
                        avrageX1 += cachedArray[i].X;
                        avrageX2 += cachedArray[SAMPLE_WINDOW - AVARAGE_WINDOW + i].X;
                        avrageY1 += cachedArray[i].Y;
                        avrageY2 += cachedArray[SAMPLE_WINDOW - AVARAGE_WINDOW + i].Y;
                        avrageZ1 += cachedArray[i].Z;
                        avrageZ2 += cachedArray[SAMPLE_WINDOW - AVARAGE_WINDOW + i].Z;
                    }

                    avrageX1 /= AVARAGE_WINDOW;
                    avrageX2 /= AVARAGE_WINDOW;
                    avrageY1 /= AVARAGE_WINDOW;
                    avrageY2 /= AVARAGE_WINDOW;
                    avrageZ1 /= AVARAGE_WINDOW;
                    avrageZ2 /= AVARAGE_WINDOW;

                    var avrageY = (avrageY1 + avrageY2) / 2;
                    var direction = avrageY < 0 ? -1 : 1;

                    pitch = direction * (int)(1000000 * Math.Atan2(avrageZ1 - avrageZ2, SixAxis.ACC_RES_PER_G) * 180 * SixAxis.GYRO_RES_IN_DEG_SEC / Math.PI) / timediff;
                    roll = direction * (int)(1000000 * Math.Atan2(avrageX2 - avrageX1, SixAxis.ACC_RES_PER_G) * 180 * SixAxis.GYRO_RES_IN_DEG_SEC / Math.PI) / timediff;
                }
            }
        }

        private SimulatePitchRoll _simulatePitchRoll = new();

        public long CntCalibrating
        {
            get
            {
                return gyroAverageTimer.IsRunning ? gyroAverageTimer.ElapsedMilliseconds : 0;
            }
        }

        public DS4SixAxis()
        {
            sPrev = new SixAxis(0, 0, 0, 0, 0, 0, 0.0);
            now = new SixAxis(0, 0, 0, 0, 0, 0, 0.0);
            StartContinuousCalibration();
        }

        int temInt = 0;
        public void setCalibrationData(ref byte[] calibData, bool useAltGyroCalib)
        {
            int pitchPlus, pitchMinus, yawPlus, yawMinus, rollPlus, rollMinus,
                accelXPlus, accelXMinus, accelYPlus, accelYMinus, accelZPlus, accelZMinus,
                gyroSpeedPlus, gyroSpeedMinus;

            calibrationData[0].bias = (short)((ushort)(calibData[2] << 8) | calibData[1]);
            calibrationData[1].bias = (short)((ushort)(calibData[4] << 8) | calibData[3]);
            calibrationData[2].bias = (short)((ushort)(calibData[6] << 8) | calibData[5]);

            if (!useAltGyroCalib)
            {
                pitchPlus = temInt = (short)((ushort)(calibData[8] << 8) | calibData[7]);
                yawPlus = temInt = (short)((ushort)(calibData[10] << 8) | calibData[9]);
                rollPlus = temInt = (short)((ushort)(calibData[12] << 8) | calibData[11]);
                pitchMinus = temInt = (short)((ushort)(calibData[14] << 8) | calibData[13]);
                yawMinus = temInt = (short)((ushort)(calibData[16] << 8) | calibData[15]);
                rollMinus = temInt = (short)((ushort)(calibData[18] << 8) | calibData[17]);
            }
            else
            {
                pitchPlus = temInt = (short)((ushort)(calibData[8] << 8) | calibData[7]);
                pitchMinus = temInt = (short)((ushort)(calibData[10] << 8) | calibData[9]);
                yawPlus = temInt = (short)((ushort)(calibData[12] << 8) | calibData[11]);
                yawMinus = temInt = (short)((ushort)(calibData[14] << 8) | calibData[13]);
                rollPlus = temInt = (short)((ushort)(calibData[16] << 8) | calibData[15]);
                rollMinus = temInt = (short)((ushort)(calibData[18] << 8) | calibData[17]);
            }

            gyroSpeedPlus = temInt = (short)((ushort)(calibData[20] << 8) | calibData[19]);
            gyroSpeedMinus = temInt = (short)((ushort)(calibData[22] << 8) | calibData[21]);
            accelXPlus = temInt = (short)((ushort)(calibData[24] << 8) | calibData[23]);
            accelXMinus = temInt = (short)((ushort)(calibData[26] << 8) | calibData[25]);

            accelYPlus = temInt = (short)((ushort)(calibData[28] << 8) | calibData[27]);
            accelYMinus = temInt = (short)((ushort)(calibData[30] << 8) | calibData[29]);

            accelZPlus = temInt = (short)((ushort)(calibData[32] << 8) | calibData[31]);
            accelZMinus = temInt = (short)((ushort)(calibData[34] << 8) | calibData[33]);

            int gyroSpeed2x = temInt = (gyroSpeedPlus + gyroSpeedMinus);
            calibrationData[0].sensNumer = gyroSpeed2x * SixAxis.GYRO_RES_IN_DEG_SEC;
            calibrationData[0].sensDenom = pitchPlus - pitchMinus;

            calibrationData[1].sensNumer = gyroSpeed2x * SixAxis.GYRO_RES_IN_DEG_SEC;
            calibrationData[1].sensDenom = yawPlus - yawMinus;

            calibrationData[2].sensNumer = gyroSpeed2x * SixAxis.GYRO_RES_IN_DEG_SEC;
            calibrationData[2].sensDenom = rollPlus - rollMinus;

            int accelRange = temInt = accelXPlus - accelXMinus;
            calibrationData[3].bias = accelXPlus - accelRange / 2;
            calibrationData[3].sensNumer = 2 * SixAxis.ACC_RES_PER_G;
            calibrationData[3].sensDenom = accelRange;

            accelRange = temInt = accelYPlus - accelYMinus;
            calibrationData[4].bias = accelYPlus - accelRange / 2;
            calibrationData[4].sensNumer = 2 * SixAxis.ACC_RES_PER_G;
            calibrationData[4].sensDenom = accelRange;

            accelRange = temInt = accelZPlus - accelZMinus;
            calibrationData[5].bias = accelZPlus - accelRange / 2;
            calibrationData[5].sensNumer = 2 * SixAxis.ACC_RES_PER_G;
            calibrationData[5].sensDenom = accelRange;

            // Check that denom will not be zero.
            calibrationDone = calibrationData[0].sensDenom != 0 &&
                calibrationData[1].sensDenom != 0 &&
                calibrationData[2].sensDenom != 0 &&
                accelRange != 0;
        }

        private void applyCalibs(ref int yaw, ref int pitch, ref int roll,
            ref int accelX, ref int accelY, ref int accelZ)
        {
            CalibData current = calibrationData[0];
            temInt = pitch - current.bias;
            pitch = temInt = (int)(temInt * (current.sensNumer / (float)current.sensDenom));

            current = calibrationData[1];
            temInt = yaw - current.bias;
            yaw = temInt = (int)(temInt * (current.sensNumer / (float)current.sensDenom));

            current = calibrationData[2];
            temInt = roll - current.bias;
            roll = temInt = (int)(temInt * (current.sensNumer / (float)current.sensDenom));

            current = calibrationData[3];
            temInt = accelX - current.bias;
            accelX = temInt = (int)(temInt * (current.sensNumer / (float)current.sensDenom));

            current = calibrationData[4];
            temInt = accelY - current.bias;
            accelY = temInt = (int)(temInt * (current.sensNumer / (float)current.sensDenom));

            current = calibrationData[5];
            temInt = accelZ - current.bias;
            accelZ = temInt = (int)(temInt * (current.sensNumer / (float)current.sensDenom));
        }

        public unsafe void handleSixaxisVals(
            int yaw,
            int pitch,
            int roll,
            int accelX,
            int accelY,
            int accelZ, DS4State state,
            double elapsedDelta)
        {
            unchecked
            {
                if (calibrationDone)
                    applyCalibs(ref yaw, ref pitch, ref roll, ref accelX, ref accelY, ref accelZ);

                if (gyroAverageTimer.IsRunning)
                {
                    CalcSensorCamples(ref yaw, ref pitch, ref roll, ref accelX, ref accelY, ref accelZ);
                }

                yaw -= gyro_offset_x;
                pitch -= gyro_offset_y;
                roll -= gyro_offset_z;

                SixAxisEventArgs args = null;
                if (accelX != 0 || accelY != 0 || accelZ != 0)
                {
                    if (SixAccelMoved != null)
                    {
                        sPrev.copy(now);
                        now.populate(yaw, pitch, roll,
                            accelX, accelY, accelZ, elapsedDelta, sPrev);

                        args = new SixAxisEventArgs(state.ReportTimeStamp, now);
                        state.Motion = now;
                        SixAccelMoved(this, args);
                    }
                }
            }
        }
        public unsafe void handleSixaxis(byte* gyro, byte* accel, DS4State state,
            double elapsedDelta)
        {
            int currentYaw = (short)((ushort)(gyro[3] << 8) | gyro[2]);
            int currentPitch = (short)((ushort)(gyro[1] << 8) | gyro[0]);
            int currentRoll = (short)((ushort)(gyro[5] << 8) | gyro[4]);
            int AccelX = (short)((ushort)(accel[1] << 8) | accel[0]);
            int AccelY = (short)((ushort)(accel[3] << 8) | accel[2]);
            int AccelZ = (short)((ushort)(accel[5] << 8) | accel[4]);

            //Console.WriteLine("AccelZ: {0}", AccelZ);
            handleSixaxisVals(currentYaw, currentPitch, currentRoll, AccelX, AccelY, AccelZ, state, elapsedDelta);
        }

        public unsafe void handleDS3Sixaxis(byte* gyro, byte* accel, DS4State state,
            double elapsedDelta, int device)
        {
            unchecked
            {
                int currentYaw = (short)((ushort)(gyro[1] << 8) | gyro[0]);
                int currentPitch = 0;
                int currentRoll = 0;
                int AccelX = (short)((ushort)(accel[1] << 8) | accel[0]);
                int AccelZ = (short)((ushort)(accel[3] << 8) | accel[2]);
                int AccelY = (short)((ushort)(accel[5] << 8) | accel[4]);

                // DS3 to DS4
                currentYaw = (int)((currentYaw - 512) * 90 * SixAxis.GYRO_RES_IN_DEG_SEC / 123.0f);
                AccelX = (int)((512 - AccelX) * SixAxis.ACC_RES_PER_G / 113.0f);
                AccelZ = (int)((512 - AccelZ) * SixAxis.ACC_RES_PER_G / 113.0f);
                AccelY = (int)((512 - AccelY) * SixAxis.ACC_RES_PER_G / 113.0f);

                //Console.WriteLine("AccelZ: {0}", AccelZ);

                if (calibrationDone)
                    applyCalibs(ref currentYaw, ref currentPitch, ref currentRoll, ref AccelX, ref AccelY, ref AccelZ);

                // Phase7-Step5 W1: DI 移行（コミット 78ae8687）以降、_controlService は常に null で、DS3 の接続中にここで
                // NullReferenceException になり得た（引数なしコンストラクターが選ばれ、引数つきの方は呼ばれなかった）。移行前と同じ結果を返す。
                var triggerActive = GyroControlsTriggerActive?.Invoke(device) ?? false;
                if (Global.UseDs3PitchRollSim && triggerActive)
                {
                    _simulatePitchRoll.PushSample(AccelX, AccelY, AccelZ, state.totalMicroSec);
                    _simulatePitchRoll.GetPitchRoll(ref currentPitch, ref currentRoll);
                }

                if (gyroAverageTimer.IsRunning)
                {
                    CalcSensorCamples(ref currentYaw, ref currentPitch, ref currentRoll, ref AccelX, ref AccelY, ref AccelZ);
                }

                currentYaw -= gyro_offset_x;
                currentPitch -= gyro_offset_y;
                currentRoll -= gyro_offset_z;

                SixAxisEventArgs args = null;
                if (AccelX != 0 || AccelY != 0 || AccelZ != 0)
                {
                    if (SixAccelMoved != null)
                    {
                        sPrev.copy(now);
                        now.populate(currentYaw, currentPitch, currentRoll,
                            AccelX, AccelY, AccelZ, elapsedDelta, sPrev);

                        args = new SixAxisEventArgs(state.ReportTimeStamp, now);
                        state.Motion = now;
                        SixAccelMoved(this, args);
                    }
                }
            }
        }

        // Entry point to run continuous calibration for non-DS4 input devices
        public unsafe void PrepareNonDS4SixAxis(ref int currentYaw, ref int currentPitch, ref int currentRoll,
            ref int AccelX, ref int AccelY, ref int AccelZ)
        {
            unchecked
            {
                if (gyroAverageTimer.IsRunning)
                {
                    CalcSensorCamples(ref currentYaw, ref currentPitch, ref currentRoll, ref AccelX, ref AccelY, ref AccelZ);
                }

                currentYaw -= gyro_offset_x;
                currentPitch -= gyro_offset_y;
                currentRoll -= gyro_offset_z;
            }
        }

        private unsafe void CalcSensorCamples(ref int currentYaw, ref int currentPitch, ref int currentRoll, ref int AccelX, ref int AccelY, ref int AccelZ)
        {
            unchecked
            {
                double accelMag = Math.Sqrt(AccelX * AccelX + AccelY * AccelY + AccelZ * AccelZ);
                PushSensorSamples(currentYaw, currentPitch, currentRoll, (float)accelMag);
                if (gyroAverageTimer.ElapsedMilliseconds > 5000L)
                {
                    gyroAverageTimer.Stop();
                    AverageGyro(ref gyro_offset_x, ref gyro_offset_y, ref gyro_offset_z, ref gyro_accel_magnitude);
#if DEBUG
                    Console.WriteLine("AverageGyro {0} {1} {2} {3}", gyro_offset_x, gyro_offset_y, gyro_offset_z, gyro_accel_magnitude);
#endif
                }
            }
        }

        public bool fixupInvertedGyroAxis()
        {
            bool result = false;
            // Some, not all, DS4 rev1 gamepads have an inverted YAW gyro axis calibration value (sensNumber>0 but at the same time sensDenom value is <0 while other two axies have both attributes >0).
            // If this gamepad has YAW calibration with weird mixed values then fix it automatically to workaround inverted YAW axis problem.
            if (calibrationData[1].sensNumer > 0 && calibrationData[1].sensDenom < 0 &&
                calibrationData[0].sensDenom > 0 && calibrationData[2].sensDenom > 0)
            {
                calibrationData[1].sensDenom *= -1;
                result = true; // Fixed inverted axis
            }
            return result;
        }

        public void FireSixAxisEvent(SixAxisEventArgs args)
        {
            SixAccelMoved?.Invoke(this, args);
        }

        public void StartContinuousCalibration()
        {
            for (int i = 0; i < gyro_average_window.Length; i++) gyro_average_window[i] = new GyroAverageWindow();
            gyroAverageTimer.Start();
        }

        public void StopContinuousCalibration()
        {
            gyroAverageTimer.Stop();
            gyroAverageTimer.Reset();
            for (int i = 0; i < gyro_average_window.Length; i++) gyro_average_window[i].Reset();
        }

        public void ResetContinuousCalibration()
        {
            // Potential race condition with CalcSensorCamples() since this method is called after checking gyroAverageTimer.IsRunning == true
            StopContinuousCalibration();
            StartContinuousCalibration();
        }

        public unsafe void PushSensorSamples(int x, int y, int z, double accelMagnitude)
        {
            // push samples
            GyroAverageWindow windowPointer = gyro_average_window[gyro_average_window_front_index];

            if (windowPointer.StopIfElapsed(gyro_average_window_ms))
            {
                Console.WriteLine("GyroAvg[{0}], numSamples: {1}", gyro_average_window_front_index,
                    windowPointer.numSamples);

                // next
                gyro_average_window_front_index = (gyro_average_window_front_index + num_gyro_average_windows - 1) % num_gyro_average_windows;
                windowPointer = gyro_average_window[gyro_average_window_front_index];
                windowPointer.Reset();
            }
            // accumulate
            windowPointer.numSamples++;
            windowPointer.x += x;
            windowPointer.y += y;
            windowPointer.z += z;
            windowPointer.accelMagnitude += accelMagnitude;
        }

        public void AverageGyro(ref int x, ref int y, ref int z, ref double accelMagnitude)
        {
            double weight = 0.0;
            double totalX = 0.0;
            double totalY = 0.0;
            double totalZ = 0.0;
            double totalAccelMagnitude = 0.0;

            int wantedMs = 5000;
            for (int i = 0; i < num_gyro_average_windows && wantedMs > 0; i++)
            {
                int cycledIndex = (i + gyro_average_window_front_index) % num_gyro_average_windows;
                GyroAverageWindow windowPointer = gyro_average_window[cycledIndex];
                if (windowPointer.numSamples == 0 || windowPointer.DurationMs == 0) continue;

                double thisWeight;
                double fNumSamples = windowPointer.numSamples;
                if (wantedMs < windowPointer.DurationMs)
                {
                    thisWeight = (float)wantedMs / windowPointer.DurationMs;
                    wantedMs = 0;
                }
                else
                {
                    thisWeight = windowPointer.GetWeight(gyro_average_window_ms);
                    wantedMs -= windowPointer.DurationMs;
                }

                totalX += (windowPointer.x / fNumSamples) * thisWeight;
                totalY += (windowPointer.y / fNumSamples) * thisWeight;
                totalZ += (windowPointer.z / fNumSamples) * thisWeight;
                totalAccelMagnitude += (windowPointer.accelMagnitude / fNumSamples) * thisWeight;
                weight += thisWeight;
            }

            if (weight > 0.0)
            {
                x = (int)(totalX / weight);
                y = (int)(totalY / weight);
                z = (int)(totalZ / weight);
                accelMagnitude = totalAccelMagnitude / weight;
            }
        }
    }
}