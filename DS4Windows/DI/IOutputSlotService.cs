using System;
using System.Collections.Generic;
using DS4Windows;

namespace DS4Windows.DI
{
    public interface IOutputSlotService
    {
        // === 既存互換メンバー ===
        OutputDevice[] OutputDevices { get; }
        OutContType GetOutputDeviceType(int slot);
        void SetOutputDeviceType(int slot, OutContType type);
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
