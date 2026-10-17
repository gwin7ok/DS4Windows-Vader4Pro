using System;
using System.Xml.Serialization;
using System.Globalization;
using DS4Windows;

public abstract class BaseInputDeviceSettingsGroup
{
    [XmlIgnore]
    public bool Enabled
    {
        get; set;
    }

    [XmlElement("Enabled")]
    public string EnabledString
    {
        get => Enabled.ToString();
        set
        {
            if (bool.TryParse(value, out bool temp))
            {
                Enabled = temp;
            }
        }
    }
}
