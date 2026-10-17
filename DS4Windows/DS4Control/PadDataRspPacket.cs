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
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.ComponentModel;
using System.Threading;
using System.Runtime.InteropServices;

namespace DS4Windows
{

    [StructLayout(LayoutKind.Explicit, Size = 100)]
    unsafe struct PadDataRspPacket
    {
        // Header section
        [FieldOffset(0)]
        public fixed byte initCode[4];
        [FieldOffset(4)]
        public ushort protocolVersion;
        [FieldOffset(6)]
        public ushort messageLen;
        [FieldOffset(8)]
        public int crc;
        [FieldOffset(12)]
        public uint serverId;
        [FieldOffset(16)]
        public uint messageType;

        // Pad meta section
        [FieldOffset(20)]
        public byte padId;
        [FieldOffset(21)]
        public byte padState;
        [FieldOffset(22)]
        public byte model;
        [FieldOffset(23)]
        public byte connectionType;
        [FieldOffset(24)]
        public fixed byte address[6];
        [FieldOffset(30)]
        public byte batteryStatus;
        [FieldOffset(31)]
        public byte isActive;
        [FieldOffset(32)]
        public uint packetCounter;

        // Primary controls
        [FieldOffset(36)]
        public byte buttons1;
        [FieldOffset(37)]
        public byte buttons2;
        [FieldOffset(38)]
        public byte psButton;
        [FieldOffset(39)]
        public byte touchButton;
        [FieldOffset(40)]
        public byte lx;
        [FieldOffset(41)]
        public byte ly;
        [FieldOffset(42)]
        public byte rx;
        [FieldOffset(43)]
        public byte ry;
        [FieldOffset(44)]
        public byte dpadLeft;
        [FieldOffset(45)]
        public byte dpadDown;
        [FieldOffset(46)]
        public byte dpadRight;
        [FieldOffset(47)]
        public byte dpadUp;
        [FieldOffset(48)]
        public byte square;
        [FieldOffset(49)]
        public byte cross;
        [FieldOffset(50)]
        public byte circle;
        [FieldOffset(51)]
        public byte triangle;
        [FieldOffset(52)]
        public byte r1;
        [FieldOffset(53)]
        public byte l1;
        [FieldOffset(54)]
        public byte r2;
        [FieldOffset(55)]
        public byte l2;

        // Touch 1
        [FieldOffset(56)]
        public byte touch1Active;
        [FieldOffset(57)]
        public byte touch1PacketId;
        [FieldOffset(58)]
        public ushort touch1X;
        [FieldOffset(60)]
        public ushort touch1Y;

        // Touch 2
        [FieldOffset(62)]
        public byte touch2Active;
        [FieldOffset(63)]
        public byte touch2PacketId;
        [FieldOffset(64)]
        public ushort touch2X;
        [FieldOffset(66)]
        public ushort touch2Y;

        // Accel
        [FieldOffset(68)]
        public ulong totalMicroSec;
        [FieldOffset(76)]
        public float accelXG;
        [FieldOffset(80)]
        public float accelYG;
        [FieldOffset(84)]
        public float accelZG;

        // Gyro
        [FieldOffset(88)]
        public float angVelPitch;
        [FieldOffset(92)]
        public float angVelYaw;
        [FieldOffset(96)]
        public float angVelRoll;
    }
}
