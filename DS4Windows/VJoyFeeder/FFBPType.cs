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


    // FFB Declarations

    // HID Descriptor definitions - FFB Report IDs

    public enum FFBPType // FFB Packet Type
    {
        // Write
        PT_EFFREP = 0x01,   // Usage Set Effect Report
        PT_ENVREP = 0x02,   // Usage Set Envelope Report
        PT_CONDREP = 0x03,  // Usage Set Condition Report
        PT_PRIDREP = 0x04,  // Usage Set Periodic Report
        PT_CONSTREP = 0x05, // Usage Set Constant Force Report
        PT_RAMPREP = 0x06,  // Usage Set Ramp Force Report
        PT_CSTMREP = 0x07,  // Usage Custom Force Data Report
        PT_SMPLREP = 0x08,  // Usage Download Force Sample
        PT_EFOPREP = 0x0A,  // Usage Effect Operation Report
        PT_BLKFRREP = 0x0B, // Usage PID Block Free Report
        PT_CTRLREP = 0x0C,  // Usage PID Device Control
        PT_GAINREP = 0x0D,  // Usage Device Gain Report
        PT_SETCREP = 0x0E,  // Usage Set Custom Force Report

        // Feature
        PT_NEWEFREP = 0x01 + 0x10,  // Usage Create New Effect Report
        PT_BLKLDREP = 0x02 + 0x10,  // Usage Block Load Report
        PT_POOLREP = 0x03 + 0x10,   // Usage PID Pool Report
    };
}
