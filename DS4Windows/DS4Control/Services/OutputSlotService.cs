using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows.DI;
using DS4Windows.Services;
using DS4WinWPF.DS4Control;

namespace DS4Windows
{
    public class OutputSlotService : IOutputSlotService
    {
        private readonly OutputSlotManager _slotManager;
        private readonly IOutputSlotStore _store;
        private readonly object _syncLock = new object();
        public const int MAX_SLOTS = 8;

        public event EventHandler<OutputSlotChangedEventArgs> OutputSlotChanged;

        private OutContType[] _deviceTypes = new OutContType[MAX_SLOTS] {
            OutContType.X360, OutContType.X360, OutContType.X360, OutContType.X360,
            OutContType.X360, OutContType.X360, OutContType.X360, OutContType.X360
        };

        private OutputDevice[] _outputDevices = new OutputDevice[MAX_SLOTS];

        public OutputSlotService(OutputSlotManager slotManager = null, IOutputSlotStore store = null)
        {
            _slotManager = slotManager ?? (Program.rootHub?.outputslotMan ?? new OutputSlotManager());
            _store = store ?? DS4WinWPF.AppHost.GetService<IOutputSlotStore>() ?? new OutputSlotStore();
        }

        public OutputDevice[] OutputDevices
        {
            get
            {
                lock (_syncLock)
                {
                    return _outputDevices;
                }
            }
        }

        public OutputDevice GetOutputDevice(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MAX_SLOTS)
                return null;

            lock (_syncLock)
            {
                return _outputDevices[slotIndex];
            }
        }

        public bool IsSlotPlugin(int slotIndex)
        {
            var device = GetOutputDevice(slotIndex);
            return device != null;
        }

        public OutContType GetOutputDeviceType(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < _deviceTypes.Length)
                return _deviceTypes[slotIndex];
            return OutContType.X360;
        }

        public void SetOutputDeviceType(int slotIndex, OutContType deviceType)
        {
            if (slotIndex >= 0 && slotIndex < _deviceTypes.Length)
            {
                lock (_syncLock)
                {
                    _deviceTypes[slotIndex] = deviceType;
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] OutputSlotService.SetOutputDeviceType: Slot {slotIndex} = {deviceType}");
                }
            }
        }

        public void SetOutputDevice(int slotIndex, OutputDevice outputDevice)
        {
            if (slotIndex >= 0 && slotIndex < MAX_SLOTS)
            {
                lock (_syncLock)
                {
                    _outputDevices[slotIndex] = outputDevice;
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] OutputSlotService.SetOutputDevice: Slot {slotIndex} output device updated");
                    OutputSlotChanged?.Invoke(this, new OutputSlotChangedEventArgs(slotIndex, outputDevice));
                }
            }
        }

        // === Step 12 拡充: 実体 OutputSlotManager 連動操作 ===
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

        public OutSlotDevice GetOutSlotDevice(int slotNumber)
        {
            if (slotNumber < 0 || slotNumber >= MAX_SLOTS) return null;
            return _slotManager?.GetOutSlotDevice(slotNumber);
        }

        public bool PluginSlot(int slotNumber, OutContType devType)
        {
            if (slotNumber < 0 || slotNumber >= MAX_SLOTS) return false;

            try
            {
                var slotDevice = GetOutSlotDevice(slotNumber);
                if (slotDevice == null) return false;

                // §5.5 ガードレール: ViGEm ネイティブドライバ保護
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

                SetOutputDevice(slotNumber, outDevice);
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
            if (slotNumber < 0 || slotNumber >= MAX_SLOTS) return false;

            try
            {
                var slotDevice = GetOutSlotDevice(slotNumber);
                if (slotDevice == null) return false;

                // §5.5 ガードレール: ViGEm ネイティブドライバ保護
                if (_slotManager != null)
                {
                    _slotManager.DeferredUnplug(slotDevice, slotNumber);
                }

                SetOutputDevice(slotNumber, null);
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
