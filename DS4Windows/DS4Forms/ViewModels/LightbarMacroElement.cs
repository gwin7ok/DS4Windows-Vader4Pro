using System;
using System.Collections.ObjectModel;
using System.Windows.Media;
using DS4Windows;

namespace DS4WinWPF.DS4Forms.ViewModels;

public class LightbarMacroElement(DS4Color color, uint length)
{
    public DS4Color Color { get; init; } = color;
    public uint Length { get; init; } = length;

    public override string ToString()
    {
        return $"{Color.ToString()}, {Length} ms";
    }

    public override bool Equals(object obj)
    {
        var casted = (LightbarMacroElement)obj;
        if (casted is null) return false;
        return casted.Color.Equals(Color) && casted.Length.Equals(Length);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 23 + Color.GetHashCode();
            hash = hash * 23 + Length.GetHashCode();
            return hash;
        }
    }
}
