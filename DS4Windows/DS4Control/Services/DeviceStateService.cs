using System;
using System.Linq;
using DS4Windows.DI;

namespace DS4Windows
{
    public class DeviceStateService : IDeviceStateService
    {
        private readonly object _syncLock = new object();
        public const int MAX_SLOTS = 8;

        public event EventHandler<DeviceStateChangedEventArgs> DeviceStateChanged;

        private readonly DS4Device[] _devices = new DS4Device[MAX_SLOTS];

        public DS4Device[] Devices
        {
            get
            {
                lock (_syncLock)
                {
                    return _devices.ToArray();
                }
            }
        }

        public DS4Device GetDevice(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MAX_SLOTS)
                return null;

            lock (_syncLock)
            {
                return _devices[slotIndex];
            }
        }

        public bool IsDeviceConnected(int slotIndex)
        {
            var device = GetDevice(slotIndex);
            return device != null;
        }

        public int ConnectedControllersCount
        {
            get
            {
                lock (_syncLock)
                {
                    return _devices.Count(d => d != null);
                }
            }
        }

        public string GetDeviceMacAddress(int slotIndex)
        {
            var device = GetDevice(slotIndex);
            return device != null ? device.MacAddress : string.Empty;
        }

        public ConnectionType GetConnectionType(int slotIndex)
        {
            var device = GetDevice(slotIndex);
            return device != null ? device.ConnectionType : ConnectionType.BT;
        }

        public int GetBatteryLevel(int slotIndex)
        {
            var device = GetDevice(slotIndex);
            return device != null ? device.Battery : 0;
        }

        public void SetDevice(int slotIndex, DS4Device device)
        {
            if (slotIndex >= 0 && slotIndex < MAX_SLOTS)
            {
                lock (_syncLock)
                {
                    _devices[slotIndex] = device;
                    NotifyDeviceStateChanged(slotIndex, device != null);
                }
            }
        }

        public void NotifyDeviceStateChanged(int slotIndex, bool isConnected)
        {
            DeviceStateChanged?.Invoke(this, new DeviceStateChangedEventArgs(slotIndex, isConnected));
        }
    }
}
