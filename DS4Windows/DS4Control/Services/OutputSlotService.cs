using System;
using DS4Windows.DI;

namespace DS4Windows
{
    public class OutputSlotService : IOutputSlotService
    {
        private readonly object _syncLock = new object();
        public const int MAX_SLOTS = 8;

        public event EventHandler<OutputSlotChangedEventArgs> OutputSlotChanged;

        private OutContType[] _deviceTypes = new OutContType[MAX_SLOTS] {
            OutContType.X360,
            OutContType.X360,
            OutContType.X360,
            OutContType.X360,
            OutContType.X360,
            OutContType.X360,
            OutContType.X360,
            OutContType.X360
        };

        private OutputDevice[] _outputDevices = new OutputDevice[MAX_SLOTS];

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
    }
}
