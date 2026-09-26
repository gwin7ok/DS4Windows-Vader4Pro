using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace DS4Windows
{
    public class HidDevice : IDisposable
    {
        public enum ReadStatus
        {
            Success = 0,
            WaitTimedOut = 1,
            WaitFail = 2,
            NoDataRead = 3,
            ReadError = 4,
            NotConnected = 5
        }

        #region Win32 Native Constants & P/Invoke (WPF tmpビルド及びOmniSharpキャッシュ喪失耐性用)
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_FLAG_OVERLAPPED = 0x40000000;
        private const int ERROR_IO_PENDING = 997;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern unsafe bool ReadFile(
            SafeFileHandle hFile,
            void* lpBuffer,
            uint nNumberOfBytesToRead,
            uint* lpNumberOfBytesRead,
            NativeOverlapped* lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern unsafe bool WriteFile(
            SafeFileHandle hFile,
            void* lpBuffer,
            uint nNumberOfBytesToWrite,
            uint* lpNumberOfBytesWritten,
            NativeOverlapped* lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetOverlappedResult(
            SafeFileHandle hFile,
            in NativeOverlapped lpOverlapped,
            out uint lpNumberOfBytesTransferred,
            bool bWait);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetOverlappedResultEx(
            SafeFileHandle hFile,
            in NativeOverlapped lpOverlapped,
            out uint lpNumberOfBytesTransferred,
            uint dwMilliseconds,
            bool bAlertable);
        #endregion

        private readonly string _description;
        private readonly string _devicePath;
        private readonly string _parentPath;
        private readonly HidDeviceAttributes _deviceAttributes;

        private readonly HidDeviceCapabilities _deviceCapabilities;
        private string serial = null;
        private SafeFileHandle safeReadHandle;
        private bool isOpen;
        private bool isExclusive;
        private const string BLANK_SERIAL = "00:00:00:00:00:00";

        internal HidDevice(string devicePath, string description = null, string parentPath = null)
        {
            _devicePath = devicePath;
            _description = description;
            _parentPath = parentPath;

            try
            {
                var hidHandle = OpenHandle(_devicePath, false, enumerate: true);

                _deviceAttributes = GetDeviceAttributes(hidHandle);
                _deviceCapabilities = GetDeviceCapabilities(hidHandle);

                hidHandle.Close();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                throw new Exception(string.Format("Error querying HID device '{0}'.", devicePath), exception);
            }
        }

        public SafeFileHandle SafeReadHandle { get => safeReadHandle; private set => safeReadHandle = value; }
        public bool IsOpen { get => isOpen; private set => isOpen = value; }
        public bool IsExclusive { get => isExclusive; private set => isExclusive = value; }
        public bool IsConnected { get { return HidDevices.IsConnected(_devicePath); } }
        public string Description { get { return _description; } }
        public HidDeviceCapabilities Capabilities { get { return _deviceCapabilities; } }
        public HidDeviceAttributes Attributes { get { return _deviceAttributes; } }
        public string DevicePath { get { return _devicePath; } }
        public string ParentPath { get => _parentPath; }

        public override string ToString()
        {
            return string.Format("VendorID={0}, ProductID={1}, Version={2}, DevicePath={3}",
                                _deviceAttributes.VendorHexId,
                                _deviceAttributes.ProductHexId,
                                _deviceAttributes.Version,
                                _devicePath);
        }

        public void OpenDevice(bool exclusive)
        {
            if (IsOpen) return;
            try
            {
                if (SafeReadHandle == null || SafeReadHandle.IsInvalid)
                    SafeReadHandle = OpenHandle(_devicePath, exclusive, enumerate: false);
            }
            catch (Exception exception)
            {
                IsOpen = false;
                throw new Exception("Error opening HID device.", exception);
            }

            IsOpen = !SafeReadHandle.IsInvalid;
            IsExclusive = exclusive;
        }

        public void CloseDevice()
        {
            if (!IsOpen) return;

            IsOpen = false;
        }

        public void Dispose()
        {
            CancelIO();
            CloseDevice();
        }

        public void CancelIO()
        {
            if (IsOpen)
                NativeMethods.CancelIoEx(SafeReadHandle.DangerousGetHandle(), IntPtr.Zero);
        }

        [Obsolete("Unused.")]
        public bool ReadInputReport(byte[] data)
        {
            if (SafeReadHandle == null)
                SafeReadHandle = OpenHandle(_devicePath, true, enumerate: false);
            return NativeMethods.HidD_GetInputReport(SafeReadHandle, data, data.Length);
        }

        public bool WriteFeatureReport(byte[] data)
        {
            bool result = false;
            if (IsOpen && SafeReadHandle != null)
            {
                result = NativeMethods.HidD_SetFeature(SafeReadHandle, data, data.Length);
            }

            return result;
        }

        private static HidDeviceAttributes GetDeviceAttributes(SafeFileHandle hidHandle)
        {
            var deviceAttributes = default(NativeMethods.HIDD_ATTRIBUTES);
            deviceAttributes.Size = Marshal.SizeOf(deviceAttributes);
            NativeMethods.HidD_GetAttributes(hidHandle.DangerousGetHandle(), ref deviceAttributes);
            return new HidDeviceAttributes(deviceAttributes);
        }

        private static HidDeviceCapabilities GetDeviceCapabilities(SafeFileHandle hidHandle)
        {
            var capabilities = default(NativeMethods.HIDP_CAPS);
            var preparsedDataPointer = default(IntPtr);

            if (!NativeMethods.HidD_GetPreparsedData(hidHandle.DangerousGetHandle(), ref preparsedDataPointer))
                return new HidDeviceCapabilities(capabilities);

            NativeMethods.HidP_GetCaps(preparsedDataPointer, ref capabilities);
            NativeMethods.HidD_FreePreparsedData(preparsedDataPointer);

            return new HidDeviceCapabilities(capabilities);
        }

        [Obsolete("Unused.")]
        public void flush_Queue()
        {
            if (SafeReadHandle != null)
            {
                NativeMethods.HidD_FlushQueue(SafeReadHandle);
            }
        }

        public unsafe ReadStatus ReadFile(Span<byte> inputBuffer, uint timeout = uint.MaxValue)
        {
            SafeReadHandle ??= OpenHandle(_devicePath, true, false);

            using AutoResetEvent wait = new(false);

            var ov = new NativeOverlapped { EventHandle = wait.SafeWaitHandle.DangerousGetHandle() };

            fixed (byte* pBuffer = inputBuffer)
            {
                if (ReadFile(SafeReadHandle, pBuffer, (uint)inputBuffer.Length, null, &ov))
                    return ReadStatus.Success;
            }

            if (Marshal.GetLastWin32Error() != ERROR_IO_PENDING) return ReadStatus.ReadError;

            if (!GetOverlappedResultEx(SafeReadHandle, ov, out _, timeout, true))
                return ReadStatus.ReadError;

            return ReadStatus.Success;
        }

        public bool WriteOutputReportViaControl(byte[] outputBuffer)
        {
            SafeReadHandle ??= OpenHandle(_devicePath, true, enumerate: false);

            return NativeMethods.HidD_SetOutputReport(SafeReadHandle, outputBuffer, outputBuffer.Length);
        }

        public unsafe bool WriteOutputReportViaInterrupt(byte[] outputBuffer, int timeout)
        {
            SafeReadHandle ??= OpenHandle(_devicePath, true, false);
            using AutoResetEvent wait = new(false);
            var ov = new NativeOverlapped { EventHandle = wait.SafeWaitHandle.DangerousGetHandle() };

            fixed (byte* pBuffer = outputBuffer)
            {
                if (WriteFile(SafeReadHandle, pBuffer, (uint)outputBuffer.Length, null, &ov))
                    return true;
            }

            if (Marshal.GetLastWin32Error() != ERROR_IO_PENDING) return false;

            if (!GetOverlappedResult(SafeReadHandle, ov, out _, true))
                return false;

            return true;
        }

        private SafeFileHandle OpenHandle(string devicePathName, bool isExclusive, bool enumerate)
        {
            uint desiredAccess = enumerate
                ? GENERIC_READ
                : (GENERIC_READ | GENERIC_WRITE);

            uint shareMode = isExclusive
                ? 0
                : (FILE_SHARE_READ | FILE_SHARE_WRITE);

            return CreateFile(
                devicePathName,
                desiredAccess,
                shareMode,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_FLAG_OVERLAPPED,
                IntPtr.Zero
            );
        }

        public bool readFeatureData(byte[] inputBuffer)
        {
            return NativeMethods.HidD_GetFeature(SafeReadHandle.DangerousGetHandle(), inputBuffer, inputBuffer.Length);
        }

        public void resetSerial()
        {
            serial = null;
        }

        public string ReadSerial(byte featureID = 18)
        {
            if (serial != null)
                return serial;

            if (Capabilities.InputReportByteLength == 64)
            {
                byte[] buffer = new byte[64];
                buffer[0] = featureID;
                if (readFeatureData(buffer))
                    serial = String.Format("{0:X02}:{1:X02}:{2:X02}:{3:X02}:{4:X02}:{5:X02}",
                        buffer[6], buffer[5], buffer[4], buffer[3], buffer[2], buffer[1]);
            }
            else
            {
                byte[] buffer = new byte[126];
#if WIN64
                ulong bufferLen = 126;
#else
                uint bufferLen = 126;
#endif
                if (NativeMethods.HidD_GetSerialNumberString(SafeReadHandle.DangerousGetHandle(), buffer, bufferLen))
                {
                    string MACAddr = System.Text.Encoding.Unicode.GetString(buffer).Replace("\0", string.Empty).ToUpper();
                    if (MACAddr.Length == 12)
                    {
                        MACAddr = $"{MACAddr[0]}{MACAddr[1]}:{MACAddr[2]}{MACAddr[3]}:{MACAddr[4]}{MACAddr[5]}:{MACAddr[6]}{MACAddr[7]}:{MACAddr[8]}{MACAddr[9]}:{MACAddr[10]}{MACAddr[11]}";
                        serial = MACAddr;
                    }
                }
            }

            if (serial == null)
            {
                AppLogger.LogToGui($"WARNING: Failed to read serial# from a gamepad ({this._deviceAttributes.VendorHexId}/{this._deviceAttributes.ProductHexId}). Generating MAC address from a device path. From now on you should connect this gamepad always into the same USB port or BT pairing host to keep the same device path.", true);
                serial = GenerateFakeHwSerial();
            }

            return serial;
        }

        public string GenerateFakeHwSerial()
        {
            string MACAddr = string.Empty;

            try
            {
                int endPos = this.DevicePath.LastIndexOf('{');
                if (endPos < 0)
                    endPos = this.DevicePath.Length;

                string[] devPathItems = this.DevicePath.Substring(0, endPos).Replace("#", "").Replace("-", "").Replace("{", "").Replace("}", "").Split('&');

                if (devPathItems.Length >= 3)
                    MACAddr = devPathItems[devPathItems.Length - 3].ToUpper()
                              + devPathItems[devPathItems.Length - 2].ToUpper()
                              + devPathItems[devPathItems.Length - 1].TrimStart('0').ToUpper();
                else if (devPathItems.Length >= 1)
                    MACAddr = this._deviceAttributes.VendorId.ToString("X4")
                              + this._deviceAttributes.ProductId.ToString("X4")
                              + devPathItems[devPathItems.Length - 1].TrimStart('0').ToUpper();

                if (!string.IsNullOrEmpty(MACAddr))
                {
                    MACAddr = MACAddr.PadRight(12, '0');
                    MACAddr = $"{MACAddr[0]}{MACAddr[1]}:{MACAddr[2]}{MACAddr[3]}:{MACAddr[4]}{MACAddr[5]}:{MACAddr[6]}{MACAddr[7]}:{MACAddr[8]}{MACAddr[9]}:{MACAddr[10]}{MACAddr[11]}";
                }
                else
                    MACAddr = BLANK_SERIAL;
            }
            catch (Exception e)
            {
                AppLogger.LogToGui($"ERROR: Failed to generate runtime MAC address from device path {this.DevicePath}. {e.Message}", true);
                MACAddr = BLANK_SERIAL;
            }

            return MACAddr;
        }

        public string GetVader4ProMacAddress(int timeout = 1000)
        {
            byte[] command = new byte[32];
            command[0] = 0x05;
            command[1] = 236;
            bool writeStatus = WriteOutputReportViaInterrupt(command, timeout);
            if (!writeStatus) return GenerateFakeHwSerial();
            var sw = Stopwatch.StartNew();
            byte[] buffer = new byte[this.Capabilities.InputReportByteLength];
            while (sw.ElapsedMilliseconds < timeout)
            {
                var status = ReadFile(buffer);

                if (status == ReadStatus.Success)
                {
                    if (buffer[1] == 0xFF && buffer[15] == 236)
                    {
                        Span<byte> macSpan = buffer.AsSpan(5, 4);
                        return string.Join(":", macSpan.ToArray().Select(b => b.ToString("X2")));
                    }
                }
                else if (status == ReadStatus.ReadError)
                {
                    continue;
                }
            }
            return GenerateFakeHwSerial();
        }
    }
}