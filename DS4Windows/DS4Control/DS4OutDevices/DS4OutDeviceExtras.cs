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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DS4Windows
{

    internal static class DS4OutDeviceExtras
    {
        public static void CopyBytes(ref DS4_REPORT_EX outReport, byte[] outBuffer)
        {
            GCHandle h = GCHandle.Alloc(outReport, GCHandleType.Pinned);
            Marshal.Copy(h.AddrOfPinnedObject(), outBuffer, 0, 63);
            h.Free();
        }

        // Mainly adding this as an example how to convert from a byte array
        // to a struct. Probably will not use
        public static DS4OutputBufferData ConvertOutputBufferArrayToStruct(byte[] rawOutputBuffer)
        {
            GCHandle pData = GCHandle.Alloc(rawOutputBuffer, GCHandleType.Pinned);
            DS4OutputBufferData outputBufferData =
                Marshal.PtrToStructure<DS4OutputBufferData>(pData.AddrOfPinnedObject());
            pData.Free();
            return outputBufferData;

            //int size = Marshal.SizeOf<DS4OutputBufferData>();
            //IntPtr ptr = Marshal.AllocHGlobal(size);
            //Marshal.Copy(rawOutputBuffer, 0, ptr, size);
            //Marshal.PtrToStructure<DS4OutputBufferData>(ptr, outputBufferData);
            //Marshal.FreeHGlobal(ptr);
        }
    }
}
