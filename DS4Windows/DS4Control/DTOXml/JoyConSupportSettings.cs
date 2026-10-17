using System;
using System.Xml.Serialization;
using System.Globalization;
using DS4Windows;

public class JoyConSupportSettings : BaseInputDeviceSettingsGroup
{
    public JoyConDeviceOptions.LinkMode LinkMode
    {
        get; set;
    } = JoyConDeviceOptions.LINK_MODE_DEFAULT;

    public JoyConDeviceOptions.JoinedGyroProvider JoinedGyroProvider
    {
        get; set;
    }

    public JoyConSupportSettings() : base()
    {
        Enabled = JoyConDeviceOptions.DEFAULT_ENABLE;
    }
}