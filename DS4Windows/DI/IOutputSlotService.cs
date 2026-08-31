using System;
using DS4Windows;

namespace DS4Windows.DI
{
    public class OutputSlotChangedEventArgs : EventArgs
    {
        public int SlotIndex { get; }
        public OutputDevice OutputDevice { get; }

        public OutputSlotChangedEventArgs(int slotIndex, OutputDevice outputDevice)
        {
            SlotIndex = slotIndex;
            OutputDevice = outputDevice;
        }
    }

    public interface IOutputSlotService
    {
        OutputDevice[] OutputDevices { get; }
        OutputDevice GetOutputDevice(int slotIndex);
        bool IsSlotPlugin(int slotIndex);

        OutContType GetOutputDeviceType(int slotIndex);
        void SetOutputDeviceType(int slotIndex, OutContType deviceType);

        event EventHandler<OutputSlotChangedEventArgs> OutputSlotChanged;
    }
}
