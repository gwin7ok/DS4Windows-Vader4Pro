using System;
using System.Collections.ObjectModel;
using System.Windows.Media;
using DS4Windows;

namespace DS4WinWPF.DS4Forms.ViewModels;

public class LightbarMacroViewModel
{
    public ObservableCollection<LightbarMacroElement> MacroList { get; set; }

    private Color _currentColor;
    public Color CurrentColor
    {
        get => _currentColor;
        set
        {
            _currentColor = value;
            CurrentColorString = value.ToString();
            CurrentColorStringChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public string CurrentColorString { get; private set; }

    public event EventHandler CurrentColorStringChanged;

    private uint _currentInterval;
    public uint CurrentInterval
    {
        get => _currentInterval;
        set
        {
            _currentInterval = value;
            CurrentIntervalChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler CurrentIntervalChanged;

    private LightbarMacroTrigger _macroTrigger;

    public LightbarMacroTrigger MacroTrigger
    {
        get => _macroTrigger;
        set
        {
            _macroTrigger = value;
            MacroTriggerChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler MacroTriggerChanged;

    public LightbarMacroViewModel()
    {
        CurrentColor = Color.FromRgb(255, 255, 255);
    }
}
