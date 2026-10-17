using System;
using System.Xml.Serialization;
using System.Globalization;
using DS4Windows;

public class UDPSrvSmoothingOptionsGroup
{
    [XmlIgnore]
    public bool UseSmoothing
    {
        get; set;
    }

    [XmlElement("UseSmoothing")]
    public string UseSmoothingString
    {
        get => UseSmoothing.ToString();
        set
        {
            if (bool.TryParse(value, out bool temp))
            {
                UseSmoothing = temp;
            }
        }
    }

    public double UdpSmoothMinCutoff
    {
        get; set;
    } = BackingStore.DEFAULT_UDP_SMOOTH_MINCUTOFF;

    public double UdpSmoothBeta
    {
        get; set;
    } = BackingStore.DEFAULT_UDP_SMOOTH_BETA;
}
