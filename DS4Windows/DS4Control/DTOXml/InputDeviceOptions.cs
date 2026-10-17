using System;
using System.Xml.Serialization;
using System.Globalization;
using DS4Windows;

public class InputDeviceOptions
{
    public DS4SupportSettingsGroup DS4SupportSettings
    {
        get; set;
    } = new DS4SupportSettingsGroup();

    public DualSenseSupportSettings DualSenseSupportSettings
    {
        get; set;
    } = new DualSenseSupportSettings();

    public SwitchProSupportSettings SwitchProSupportSettings
    {
        get; set;
    } = new SwitchProSupportSettings();

    public JoyConSupportSettings JoyConSupportSettings
    {
        get; set;
    } = new JoyConSupportSettings();

    public DS3SupportSettings DS3SupportSettings
    {
        get; set;
    } = new DS3SupportSettings();
}
