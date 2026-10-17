using System;
using System.Xml.Serialization;
using System.Globalization;
using DS4Windows;

public class DS4SupportSettingsGroup : BaseInputDeviceSettingsGroup
{
    public DS4SupportSettingsGroup() : base()
    {
        Enabled = DS4DeviceOptions.DEFAULT_ENABLE;
    }
}
