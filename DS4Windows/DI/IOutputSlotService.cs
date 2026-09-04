using System;
using System.Collections.Generic;
using DS4Windows;
using DS4WinWPF.DS4Control;

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
        // === 既存互換メンバー ===
        OutputDevice[] OutputDevices { get; }
        OutputDevice GetOutputDevice(int slotIndex);
        bool IsSlotPlugin(int slotIndex);
        OutContType GetOutputDeviceType(int slotIndex);
        void SetOutputDeviceType(int slotIndex, OutContType deviceType);
        event EventHandler<OutputSlotChangedEventArgs> OutputSlotChanged;

        // === Step 12 拡充: 実体 OutputSlotManager 連動操作 ===
        IReadOnlyList<OutSlotDevice> OutputSlots { get; }
        OutSlotDevice GetOutSlotDevice(int slotNumber);
        bool PluginSlot(int slotNumber, OutContType devType);
        bool UnplugSlot(int slotNumber);
        bool LoadOutputSlots();
        bool SaveOutputSlots();
    }
}
