using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4Windows
{
    public class OutputSlotService : IOutputSlotService
    {
        private readonly OutputSlotManager _slotManager;
        private readonly IOutputSlotStore _store;
        private readonly OutContType[] _deviceTypes = new OutContType[8];
        private readonly OutputDevice[] _outputDevices = new OutputDevice[8];

        public event EventHandler<OutputSlotChangedEventArgs> OutputSlotChanged;

        public OutputSlotService(OutputSlotManager slotManager = null, IOutputSlotStore store = null)
        {
            _slotManager = slotManager ?? (Program.rootHub?.outputslotMan ?? new OutputSlotManager());
            _store = store ?? DS4WinWPF.AppHost.GetService<IOutputSlotStore>() ?? new OutputSlotStore();

            for (int i = 0; i < 8; i++)
            {
                _deviceTypes[i] = OutContType.None;
            }
        }

        public OutputDevice[] OutputDevices => _outputDevices;

        public IReadOnlyList<OutSlotDevice> OutputSlots
        {
            get
            {
                if (_slotManager?.OutputSlots != null)
                {
                    return _slotManager.OutputSlots.ToList().AsReadOnly();
                }
                return Array.Empty<OutSlotDevice>();
            }
        }

        public OutContType GetOutputDeviceType(int slot)
        {
            if (slot < 0 || slot >= 8) return OutContType.None;
            return _deviceTypes[slot];
        }

        public void SetOutputDeviceType(int slot, OutContType type)
        {
            if (slot < 0 || slot >= 8) return;
            OutContType oldType = _deviceTypes[slot];
            if (oldType != type)
            {
                _deviceTypes[slot] = type;
                OutputSlotChanged?.Invoke(this, new OutputSlotChangedEventArgs(slot, type));
            }
        }

        public OutSlotDevice GetOutSlotDevice(int slotNumber)
        {
            if (slotNumber < 0 || slotNumber >= 8) return null;
            return _slotManager?.GetOutSlotDevice(slotNumber);
        }

        public bool PluginSlot(int slotNumber, OutContType devType)
        {
            if (slotNumber < 0 || slotNumber >= 8) return false;

            try
            {
                var slotDevice = GetOutSlotDevice(slotNumber);
                if (slotDevice == null) return false;

                // §5.5 ガードレール: ViGEm ネイティブドライバ保護
                // OutputSlotManager の内部キューイング（DeferredPlugin）を経由して安全にプラグイン
                OutputDevice outDevice = null;
                if (devType == OutContType.X360)
                {
                    outDevice = new Xbox360OutDevice();
                }
                else if (devType == OutContType.DS4)
                {
                    outDevice = DS4OutDeviceFactory.CreateDS4Device(slotNumber);
                }

                if (outDevice != null && _slotManager != null)
                {
                    _slotManager.DeferredPlugin(outDevice, slotNumber, slotDevice, "");
                }

                SetOutputDeviceType(slotNumber, devType);
                return true;
            }
            catch (Exception ex)
            {
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace($"[DI] OutputSlotService.PluginSlot failed: {ex}");
                return false;
            }
        }

        public bool UnplugSlot(int slotNumber)
        {
            if (slotNumber < 0 || slotNumber >= 8) return false;

            try
            {
                var slotDevice = GetOutSlotDevice(slotNumber);
                if (slotDevice == null) return false;

                // §5.5 ガードレール: ViGEm ネイティブドライバ保護
                // OutputSlotManager の内部キューイング（DeferredUnplug）を経由して安全にアンプラグ
                if (_slotManager != null)
                {
                    _slotManager.DeferredUnplug(slotDevice, slotNumber);
                }

                SetOutputDeviceType(slotNumber, OutContType.None);
                return true;
            }
            catch (Exception ex)
            {
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace($"[DI] OutputSlotService.UnplugSlot failed: {ex}");
                return false;
            }
        }

        public bool LoadOutputSlots()
        {
            if (_slotManager == null || _store == null) return false;
            return _store.Load(_slotManager);
        }

        public bool SaveOutputSlots()
        {
            if (_slotManager == null || _store == null) return false;
            return _store.Save(_slotManager);
        }
    }
}
