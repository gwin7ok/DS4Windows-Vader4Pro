using System;
using System.Xml.Serialization;
using System.Globalization;
using DS4Windows;

public class DualSenseSupportSettings : BaseInputDeviceSettingsGroup
{
    public DualSenseSupportSettings() : base()
    {
        Enabled = DualSenseDeviceOptions.DEFAULT_ENABLE;
    }
}
