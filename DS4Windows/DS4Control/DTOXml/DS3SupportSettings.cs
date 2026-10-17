using System;
using System.Xml.Serialization;
using System.Globalization;
using DS4Windows;

public class DS3SupportSettings : BaseInputDeviceSettingsGroup
{
    public DS3SupportSettings() : base()
    {
        Enabled = DS3DeviceOptions.DEFAULT_ENABLE;
    }
}
