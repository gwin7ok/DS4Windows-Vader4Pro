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

    public enum VjdStat  /* Declares an enumeration data type called BOOLEAN */
    {
        VJD_STAT_OWN,   // The  vJoy Device is owned by this application.
        VJD_STAT_FREE,  // The  vJoy Device is NOT owned by any application (including this one).
        VJD_STAT_BUSY,  // The  vJoy Device is owned by another application. It cannot be acquired by this application.
        VJD_STAT_MISS,  // The  vJoy Device is missing. It either does not exist or the driver is down.
        VJD_STAT_UNKN   // Unknown
    };
}
