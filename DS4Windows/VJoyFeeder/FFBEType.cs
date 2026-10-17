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

    public enum FFBEType // FFB Effect Type
    {

        // Effect Type
        ET_NONE = 0,      //    No Force
        ET_CONST = 1,    //    Constant Force
        ET_RAMP = 2,    //    Ramp
        ET_SQR = 3,    //    Square
        ET_SINE = 4,    //    Sine
        ET_TRNGL = 5,    //    Triangle
        ET_STUP = 6,    //    Sawtooth Up
        ET_STDN = 7,    //    Sawtooth Down
        ET_SPRNG = 8,    //    Spring
        ET_DMPR = 9,    //    Damper
        ET_INRT = 10,   //    Inertia
        ET_FRCTN = 11,   //    Friction
        ET_CSTM = 12,   //    Custom Force Data
    };
}
