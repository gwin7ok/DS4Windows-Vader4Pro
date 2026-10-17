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

    public enum FFB_CTRL
    {
        CTRL_ENACT = 1, // Enable all device actuators.
        CTRL_DISACT = 2,    // Disable all the device actuators.
        CTRL_STOPALL = 3,   // Stop All Effects­ Issues a stop on every running effect.
        CTRL_DEVRST = 4,    // Device Reset– Clears any device paused condition, enables all actuators and clears all effects from memory.
        CTRL_DEVPAUSE = 5,  // Device Pause– The all effects on the device are paused at the current time step.
        CTRL_DEVCONT = 6,   // Device Continue– The all effects that running when the device was paused are restarted from their last time step.
    };
}
