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
        private readonly ControlService _control;
        private readonly object _syncLock = new object();
        public const int MAX_SLOTS = 8;

        public event EventHandler<OutputSlotChangedEventArgs> OutputSlotChanged;

        private OutContType[] _deviceTypes = new OutContType[MAX_SLOTS] {
            OutContType.X360, OutContType.X360, OutContType.X360, OutContType.X360,
            OutContType.X360, OutContType.X360, OutContType.X360, OutContType.X360
        };

        private OutputDevice[] _outputDevices = new OutputDevice[MAX_SLOTS];

        public OutputSlotService(OutputSlotManager slotManager = null, IOutputSlotStore store = null, ControlService control = null)
        {
            _control = control ?? Program.rootHub;
            _slotManager = slotManager ?? _control?.OutputslotMan ?? new OutputSlotManager();
            _store = store ?? DS4WinWPF.AppHost.GetService<IOutputSlotStore>() ?? new Services.OutputSlotStore();
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
                    OutputSlotChanged?.Invoke(this, new OutputSlotChangedEventArgs(slotIndex, deviceType, _outputDevices[slotIndex]));
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
                    OutputSlotChanged?.Invoke(this, new OutputSlotChangedEventArgs(slotIndex, _deviceTypes[slotIndex], outputDevice));
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

                // §5.5 最重要ガードレール: ViGEm ネイティブドライバ保護
                // ControlService の正規公開 API（AttachUnboundOutDev）を経由して安全にプラグイン
                if (_control != null)
                {
                    _control.AttachUnboundOutDev(slotDevice, devType);
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
            if (slotNumber < 0 || slotNumber >= MAX_SLOTS) return false;

            try
            {
                var slotDevice = GetOutSlotDevice(slotNumber);
                if (slotDevice == null) return false;

                // §5.5 最重要ガードレール: ViGEm ネイティブドライバ保護
                // ControlService の正規公開 API（DetachUnboundOutDev）を経由して安全にアンプラグ
                if (_control != null)
                {
                    _control.DetachUnboundOutDev(slotDevice);
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
