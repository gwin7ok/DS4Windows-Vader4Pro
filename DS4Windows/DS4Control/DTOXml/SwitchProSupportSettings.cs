using System;
using System.Xml.Serialization;
using System.Globalization;
using DS4Windows;

public class SwitchProSupportSettings : BaseInputDeviceSettingsGroup
{
    public SwitchProSupportSettings() : base()
    {
        Enabled = SwitchProDeviceOptions.DEFAULT_ENABLE;
    }
}
