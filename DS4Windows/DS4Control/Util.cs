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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Collections.Generic;
using Microsoft.Win32;

namespace DS4Windows
{
    [SuppressUnmanagedCodeSecurity]
    public class Util
    {
        public static Guid sysGuid = Guid.Parse("{4d36e97d-e325-11ce-bfc1-08002be10318}");
        public static Guid fakerInputGuid = Guid.Parse("{ab67b0fa-d0f5-4f60-81f4-346e18fd0805}");

        public enum PROCESS_INFORMATION_CLASS : int
        {
            ProcessBasicInformation = 0,
            ProcessQuotaLimits,
            ProcessIoCounters,
            ProcessVmCounters,
            ProcessTimes,
            ProcessBasePriority,
            ProcessRaisePriority,
            ProcessDebugPort,
            ProcessExceptionPort,
            ProcessAccessToken,
            ProcessLdtInformation,
            ProcessLdtSize,
            ProcessDefaultHardErrorMode,
            ProcessIoPortHandlers,
            ProcessPooledUsageAndLimits,
            ProcessWorkingSetWatch,
            ProcessUserModeIOPL,
            ProcessEnableAlignmentFaultFixup,
            ProcessPriorityClass,
            ProcessWx86Information,
            ProcessHandleCount,
            ProcessAffinityMask,
            ProcessPriorityBoost,
            ProcessDeviceMap,
            ProcessSessionInformation,
            ProcessForegroundInformation,
            ProcessWow64Information,
            ProcessImageFileName,
            ProcessLUIDDeviceMapsEnabled,
            ProcessBreakOnTermination,
            ProcessDebugObjectHandle,
            ProcessDebugFlags,
            ProcessHandleTracing,
            ProcessIoPriority,
            ProcessExecuteFlags,
            ProcessResourceManagement,
            ProcessCookie,
            ProcessImageInformation,
            ProcessCycleTime,
            ProcessPagePriority,
            ProcessInstrumentationCallback,
            ProcessThreadStackAllocation,
            ProcessWorkingSetWatchEx,
            ProcessImageFileNameWin32,
            ProcessImageFileMapping,
            ProcessAffinityUpdateMode,
            ProcessMemoryAllocationMode,
            MaxProcessInfoClass
        }

        [StructLayout(LayoutKind.Sequential)]
        public class DEV_BROADCAST_DEVICEINTERFACE
        {
            internal Int32 dbcc_size;
            internal Int32 dbcc_devicetype;
            internal Int32 dbcc_reserved;
            internal Guid dbcc_classguid;
            internal Int16 dbcc_name;
        }

        public const Int32 DBT_DEVTYP_DEVICEINTERFACE = 0x0005;

        public const Int32 DEVICE_NOTIFY_WINDOW_HANDLE = 0x0000;
        public const Int32 DEVICE_NOTIFY_SERVICE_HANDLE = 0x0001;
        public const Int32 DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 0x0004;

        public const Int32 WM_CREATE = 0x0001;
        public const Int32 WM_DEVICECHANGE = 0x0219;

        public const Int32 DIGCF_PRESENT = 0x0002;
        public const Int32 DIGCF_DEVICEINTERFACE = 0x0010;

        public const uint EDD_GET_DEVICE_INTERFACE_NAME = 0x00000001;

        // Common helper to normalize action names for comparison (Trim by default)
        public static string NormalizeActionName(string name)
        {
            if (name == null) return string.Empty;
            return name.Trim();
        }


        [Flags]
        public enum DisplayDeviceStateFlags : int
        {
            /// <summary>The device is part of the desktop.</summary>
            AttachedToDesktop = 0x1,
            MultiDriver = 0x2,
            /// <summary>The device is part of the desktop.</summary>
            PrimaryDevice = 0x4,
            /// <summary>Represents a pseudo device used to mirror application drawing for remoting or other purposes.</summary>
            MirroringDriver = 0x8,
            /// <summary>The device is VGA compatible.</summary>
            VGACompatible = 0x10,
            /// <summary>The device is removable; it cannot be the primary display.</summary>
            Removable = 0x20,
            /// <summary>The device has more display modes than its output devices support.</summary>
            ModesPruned = 0x8000000,
            Remote = 0x4000000,
            Disconnect = 0x2000000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct DISPLAY_DEVICE
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            [MarshalAs(UnmanagedType.U4)]
            public DisplayDeviceStateFlags StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern int NtSetInformationProcess(IntPtr processHandle,
           PROCESS_INFORMATION_CLASS processInformationClass, ref IntPtr processInformation, uint processInformationLength);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        protected static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, Int32 Flags);

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern Boolean UnregisterDeviceNotification(IntPtr Handle);

        [DllImport("winmm.dll")]
        internal static extern uint timeBeginPeriod(uint period);
        [DllImport("winmm.dll")]
        internal static extern uint timeEndPeriod(uint period);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool EnumDisplayDevicesW(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        public static Boolean RegisterNotify(IntPtr Form, Guid Class, ref IntPtr Handle, Boolean Window = true)
        {
            IntPtr devBroadcastDeviceInterfaceBuffer = IntPtr.Zero;

            try
            {
                DEV_BROADCAST_DEVICEINTERFACE devBroadcastDeviceInterface = new DEV_BROADCAST_DEVICEINTERFACE();
                Int32 Size = Marshal.SizeOf(devBroadcastDeviceInterface);

                devBroadcastDeviceInterface.dbcc_size = Size;
                devBroadcastDeviceInterface.dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE;
                devBroadcastDeviceInterface.dbcc_reserved = 0;
                devBroadcastDeviceInterface.dbcc_classguid = Class;

                devBroadcastDeviceInterfaceBuffer = Marshal.AllocHGlobal(Size);
                Marshal.StructureToPtr(devBroadcastDeviceInterface, devBroadcastDeviceInterfaceBuffer, true);

                Handle = RegisterDeviceNotification(Form, devBroadcastDeviceInterfaceBuffer, Window ? DEVICE_NOTIFY_WINDOW_HANDLE : DEVICE_NOTIFY_SERVICE_HANDLE);

                Marshal.PtrToStructure(devBroadcastDeviceInterfaceBuffer, devBroadcastDeviceInterface);

                return Handle != IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} {1}", ex.HelpLink, ex.Message);
                throw;
            }
            finally
            {
                if (devBroadcastDeviceInterfaceBuffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(devBroadcastDeviceInterfaceBuffer);
                }
            }
        }

        public static Boolean UnregisterNotify(IntPtr Handle)
        {
            try
            {
                return UnregisterDeviceNotification(Handle);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} {1}", ex.HelpLink, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Use admin check to determine if process can be launched normally (normal user)
        /// or launched through Windows Explorer to de-elevate a process
        /// </summary>
        /// <param name="path">Program path or URL</param>
        /// <param name="argument">Extra arguments to pass to the launching program</param>
        public static void StartProcessHelper(string path, string arguments = null)
        {
            if (!Global.IsAdministrator())
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(path);
                startInfo.UseShellExecute = true;
                if (!string.IsNullOrEmpty(arguments))
                {
                    startInfo.Arguments = arguments;
                }

                try
                {
                    using (Process temp = Process.Start(startInfo))
                    {
                    }
                }
                catch { }
            }
            else
            {
                StartProcessInExplorer(path, arguments);
            }
        }

        /// <summary>
        /// Launch process in Explorer to de-elevate the process if DS4Windows is running
        /// as under the Admin account
        /// </summary>
        /// <param name="path">Program path or URL</param>
        /// <param name="argument">Extra arguments to pass to the launching program</param>
        public static void StartProcessInExplorer(string path, string arguments = null)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "explorer.exe";
            // Need to place Path/URL in double quotes to allow equals sign to not be
            // interpreted as a delimiter
            if (string.IsNullOrEmpty(arguments))
            {
                startInfo.Arguments = $"\"{path}\"";
            }
            else
            {
                startInfo.Arguments = $"\"{path}\" \"{arguments}\"";
            }

            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.UseShellExecute = true;
            try
            {
                using (Process temp = Process.Start(startInfo)) { }
            }
            catch { }
        }

        public static void LogAssistBackgroundTask(Task task)
        {
            task.ContinueWith((t) =>
            {
                if (t.IsFaulted)
                {
                    AppLogger.LogToGui(t.Exception.ToString(), true);
                }
            });
        }

        public static int ElevatedCopyUpdater(string tmpUpdaterPath, bool deleteUpdatesDir=false)
        {
            int result = -1;
            string tmpPath = Path.Combine(Path.GetTempPath(), "updatercopy.bat");
            //string tmpPath = Path.GetTempFileName();
            // Create temporary bat script that will later be executed
            using (StreamWriter w = new StreamWriter(new FileStream(tmpPath,
                FileMode.Create, FileAccess.Write)))
            {
                w.WriteLine("@echo off"); // Turn off echo
                w.WriteLine("@echo Attempting to replace updater, please wait...");
                // Move temp downloaded file to destination
                w.WriteLine($"@mov /Y \"{tmpUpdaterPath}\" \"{Global.exedirpath}\\DS4Updater.exe\"");
                if (deleteUpdatesDir)
                {
                    w.WriteLine($"@del /S \"{Global.exedirpath}\\Update Files\\DS4Windows\"");
                }

                w.Close();
            }

            // Execute temp batch script with admin privileges
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = tmpPath;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Verb = "runas";
            startInfo.UseShellExecute = true;
            startInfo.CreateNoWindow = true;
            try
            {
                // Launch process, wait and then save exit code
                using (Process temp = Process.Start(startInfo))
                {
                    temp.WaitForExit();
                    result = temp.ExitCode;
                }
            }
            catch { }

            return result;
        }

        public static string GetOSProductName()
        {
            string productName =
                Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", "").ToString();
            return productName;
        }

        public static string GetOSReleaseId()
        {
            string releaseId =
                Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "").ToString();
            return releaseId;
        }

        public static bool IsNet8DesktopRuntimeAvailable()
        {
            bool result = false;
            string archString = Environment.Is64BitProcess ? "x64" : "x86";

            using (RegistryKey subKey =
                Registry.LocalMachine.OpenSubKey($@"SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\{archString}\sharedfx\Microsoft.WindowsDesktop.App"))
            {
                if (subKey != null)
                {
                    foreach (string valueName in subKey.GetValueNames())
                    {
                        if (!string.IsNullOrEmpty(valueName) && valueName.Contains("8."))
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        public static bool SystemAppsUsingDarkTheme()
        {
            bool result = false;
            if (int.TryParse(Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", "0").ToString(), out int lightEnabled))
            {
                result = lightEnabled == 0;
            }

            return result;
        }

        /// <summary>
        /// Use HidHide MSI registry info to try to find HidHideClient
        /// install path. Return found path or string.Empty if path not found.
        /// Good enough for me.
        /// </summary>
        /// <returns></returns>
        public static string GetHidHideClientPath()
        {
            string result = string.Empty;
            // Try known HidHide installer GUIDs for InstallLocation. Some installers/register keys differ
            // across versions; check the known GUIDs in order.
            string[] hidHideInstallerGuids = new string[]
            {
                "{48DD38C8-443E-4474-A249-AB32389E08F6}", // original GUID
                "{01E0AB21-D1CC-42B4-9DFF-84FFE4F26DAF}"  // HidHide v1.5.230 (provided)
            };

            string installLocation = string.Empty;
            foreach (var guid in hidHideInstallerGuids)
            {
                installLocation = Registry.GetValue($@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{guid}", "InstallLocation", "")?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(installLocation))
                    break;
            }
            if (!string.IsNullOrEmpty(installLocation))
            {
                string[] testPaths = new string[]
                {
                    Path.Combine(installLocation, "HidHideClient.exe"),
                    Path.Combine(installLocation, "x64", "HidHideClient.exe"),
                    Path.Combine(installLocation, "x86", "HidHideClient.exe"),
                };

                foreach (string testPath in testPaths)
                {
                    if (File.Exists(testPath))
                    {
                        result = testPath;
                        break;
                    }
                }
            }

            // If we didn't find an InstallLocation via known GUIDs, try scanning the
            // Uninstall registry keys for any entry whose DisplayName contains "HidHide".
            if (string.IsNullOrEmpty(result))
            {
                string[] uninstallRoots = new string[]
                {
                    @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall",
                    @"SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall"
                };

                foreach (var root in uninstallRoots)
                {
                    try
                    {
                        using (var rk = Registry.LocalMachine.OpenSubKey(root))
                        {
                            if (rk == null) continue;
                            foreach (var subName in rk.GetSubKeyNames())
                            {
                                try
                                {
                                    using (var sub = rk.OpenSubKey(subName))
                                    {
                                        if (sub == null) continue;
                                        var display = sub.GetValue("DisplayName") as string ?? string.Empty;
                                        if (display.IndexOf("HidHide", StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            // Prefer InstallLocation if present
                                            var loc = sub.GetValue("InstallLocation") as string ?? string.Empty;
                                            if (!string.IsNullOrEmpty(loc))
                                            {
                                                string[] paths = new string[]
                                                {
                                                    Path.Combine(loc, "HidHideClient.exe"),
                                                    Path.Combine(loc, "x64", "HidHideClient.exe"),
                                                    Path.Combine(loc, "x86", "HidHideClient.exe"),
                                                };

                                                foreach (var p in paths)
                                                {
                                                    if (File.Exists(p))
                                                    {
                                                        result = p;
                                                        break;
                                                    }
                                                }
                                            }

                                            // If still not found, try UninstallString directory
                                            if (string.IsNullOrEmpty(result))
                                            {
                                                var uninstallStr = sub.GetValue("UninstallString") as string ?? string.Empty;
                                                if (!string.IsNullOrEmpty(uninstallStr))
                                                {
                                                    // Clean quotes and parameters
                                                    try
                                                    {
                                                        string cleaned = uninstallStr.Trim();
                                                        if (cleaned.StartsWith("\"") && cleaned.IndexOf('\"', 1) > 0)
                                                        {
                                                            cleaned = cleaned.Substring(1, cleaned.IndexOf('\"', 1) - 1);
                                                        }
                                                        // If there are parameters, remove them
                                                        if (cleaned.Contains(" "))
                                                            cleaned = cleaned.Split(' ')[0];

                                                        var dir = Path.GetDirectoryName(cleaned) ?? string.Empty;
                                                        if (!string.IsNullOrEmpty(dir))
                                                        {
                                                            string[] tryDirs = new string[]
                                                            {
                                                                Path.Combine(dir, "HidHideClient.exe"),
                                                                Path.Combine(Directory.GetParent(dir)?.FullName ?? string.Empty, "HidHideClient.exe")
                                                            };

                                                            foreach (var p in tryDirs)
                                                            {
                                                                if (!string.IsNullOrEmpty(p) && File.Exists(p))
                                                                {
                                                                    result = p;
                                                                    break;
                                                                }
                                                            }
                                                        }
                                                    }
                                                    catch { }
                                                }
                                            }
                                        }

                                        if (!string.IsNullOrEmpty(result)) break;
                                    }
                                }
                                catch { }

                                if (!string.IsNullOrEmpty(result)) break;
                            }
                        }
                    }
                    catch { }

                    if (!string.IsNullOrEmpty(result)) break;
                }
            }

            // As a last resort, search Program Files directories for HidHideClient.exe
            if (string.IsNullOrEmpty(result))
            {
                try
                {
                    string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                    string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                    List<string> roots = new List<string>() { pf };
                    if (!string.IsNullOrEmpty(pf86) && !roots.Contains(pf86)) roots.Add(pf86);

                    foreach (var r in roots)
                    {
                        if (Directory.Exists(r))
                        {
                            foreach (var d in Directory.GetDirectories(r))
                            {
                                try
                                {
                                    if (d.IndexOf("HidHide", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        var candidate = Path.Combine(d, "HidHideClient.exe");
                                        if (File.Exists(candidate))
                                        {
                                            result = candidate;
                                            break;
                                        }
                                        // check deeper x64/x86 subfolders
                                        var candidate64 = Path.Combine(d, "x64", "HidHideClient.exe");
                                        var candidate86 = Path.Combine(d, "x86", "HidHideClient.exe");
                                        if (File.Exists(candidate64)) { result = candidate64; break; }
                                        if (File.Exists(candidate86)) { result = candidate86; break; }
                                    }
                                }
                                catch { }
                            }
                        }

                        if (!string.IsNullOrEmpty(result)) break;
                    }
                }
                catch { }
            }

            return result;
        }
    }
}
