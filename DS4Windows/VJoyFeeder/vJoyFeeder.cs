// VJoy C# interface file taken from an excellent Shaul's virtual joystick driver project.
// Licensed to public domain as is (http://vjoystick.sourceforge.net/site/index.php/forum/5-Discussion/104-what-is-the-usage-license-for-this-software).
// http://vjoystick.sourceforge.net/site/
// https://github.com/shauleiz/vJoy/tree/master/apps/common/vJoyInterfaceCS
//
// This module is a feeder for VJoy virtual joystick driver. DS4Windows can optionally re-map and feed buttons and analog axis values from DS4 Controller to VJoy device.
// At first this may seem silly because DS4Windows can already do re-mapping by using a virtual X360 Controller driver, so why feed VJoy virtual driver also? 
// Sometimes X360 driver may run out of analog axis options, so for example "SA motion sensor steering wheel emulation" in DS4Windows would reserve a thumbstick X or Y 
// axis for SA steering wheel emulation usage. That thumbstick axis would be unavailable for "normal" thumbstick usage after this re-mapping. 
// The problem can be solved by configuring DS4Windows to re-map SA steering wheel emulation axis to VJoy axis, so all analog axies in DS4 controller are still available for normal usage.
//

using System;
using System.Runtime.InteropServices;
using System.Security;    // SuppressUnmanagedCodeSecurity support to optimize for performance instead of code security

namespace DS4Windows.VJoyFeeder
{
    //}

    public class vJoyFeeder
    {
        private static readonly object vJoyLocker = new object();

        static bool[] vJoyInitialized = new bool[2] { false, false };
        static bool[] vJoyAvailable = new bool[2] { false, false };
        static VJoy[] vJoyObj = new VJoy[2] { null, null }; 

        vJoyFeeder()
        {
            // Do nothing
        }

        ~vJoyFeeder()
        {
            // Do nothing
        }

        public static void InitializeVJoyDevice(uint vJoyID, HID_USAGES axis)
        {
            lock (vJoyLocker)
            {
                if (vJoyInitialized[vJoyID - 1]) return;

                vJoyInitialized[vJoyID - 1] = true;
                AppLogger.LogToGui("Initializing VJoy virtual joystick driver via vJoyInterface.dll interface", false);

                try
                {
                    VJoy currVJoy;

                    if (vJoyObj[vJoyID - 1] == null) vJoyObj[vJoyID - 1] = new VJoy();
                    currVJoy = vJoyObj[vJoyID - 1];

                    if (currVJoy.vJoyEnabled() && currVJoy.GetVJDAxisExist(vJoyID, axis))
                    {
                        AppLogger.LogToGui("Connection to VJoy virtual joystick established", false);
                        AppLogger.LogToGui($"VJoy driver. Vendor={currVJoy.GetvJoyManufacturerString()}  Product={currVJoy.GetvJoyProductString()}  Version={currVJoy.GetvJoySerialNumberString()}  Device#={vJoyID}  Axis={axis}", false);

                        // Test if DLL matches the driver
                        UInt32 DllVer = 0, DrvVer = 0;
                        if (!currVJoy.DriverMatch(ref DllVer, ref DrvVer))
                            AppLogger.LogToGui("WARNING. VJoy version of Driver {DrvVer}) does not match interface DLL Version {DllVer}. This may lead to unexpected problems or crashes. Update VJoy driver and vJoyInterface.dll", false);

                        VjdStat status = currVJoy.GetVJDStatus(vJoyID);
                        if ((status == VjdStat.VJD_STAT_OWN) || ((status == VjdStat.VJD_STAT_FREE) && (!currVJoy.AcquireVJD(vJoyID))))
                        {
                            vJoyAvailable[vJoyID - 1] = false;
                            AppLogger.LogToGui("ERROR. Failed to acquire vJoy device# {vJoyID}. Use another VJoy device or make sure there are no other VJoy feeder apps using the same device", false);
                        }
                        else
                        {
                            //vJoyObj.GetVJDAxisMax(vJoyID, axis, ref vJoyAxisMaxValue);
                            //AppLogger.LogToGui($"VJoy axis {axis} max value={vJoyAxisMaxValue}", false);
                            currVJoy.ResetVJD(vJoyID);
                            vJoyAvailable[vJoyID - 1] = true;
                        }
                    }
                    else
                    {
                        vJoyAvailable[vJoyID - 1] = false;
                        AppLogger.LogToGui($"ERROR. VJoy device# {vJoyID} or {axis} axis not available. Check vJoy driver installation and configuration", false);
                    }
                }
                catch
                {
                    vJoyAvailable[vJoyID - 1] = false;
                    AppLogger.LogToGui("ERROR. vJoy initialization failed. Make sure that DS4Windows application can find vJoyInterface.dll library file", false);
                }
            }
        }

        // Feed axis value to VJoy virtual joystic driver (DS4Windows sixaxis (SA) motion sensor steering wheel emulation feature can optionally feed VJoy analog axis instead of ScpVBus x360 axis
        public static void FeedAxisValue(int value, uint vJoyID, HID_USAGES axis)
        {
            if (vJoyAvailable[vJoyID - 1])
            {
                vJoyObj[vJoyID - 1].SetAxis(value, vJoyID, axis);
            }
            else if (!vJoyInitialized[vJoyID - 1])
            {
                // If this was the first call to this FeedAxisValue function and VJoy driver connection is not yet initialized
                // then try to do it now. Subsequent calls will see the the vJoy as available (if connection succeeded) and 
                // there is no need to re-initialize the connection everytime the feeder is used.
                InitializeVJoyDevice(vJoyID, axis);
            }
        }
    }
}
