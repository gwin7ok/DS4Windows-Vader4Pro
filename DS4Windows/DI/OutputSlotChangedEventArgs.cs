using System;
using System.Collections.Generic;
using DS4Windows;
using DS4WinWPF.DS4Control;

namespace DS4Windows.DI
{
    public class OutputSlotChangedEventArgs : EventArgs
    {
        public int Slot { get; }
        public int SlotIndex => Slot;
        public OutContType DeviceType { get; }
        public OutputDevice OutputDevice { get; }

        public OutputSlotChangedEventArgs(int slot, OutContType deviceType, OutputDevice outputDevice = null)
        {
            Slot = slot;
            DeviceType = deviceType;
            OutputDevice = outputDevice;
        }

        public OutputSlotChangedEventArgs(int slotIndex, OutputDevice outputDevice)
            : this(slotIndex, OutContType.None, outputDevice)
        {
        }
    }
}