using System;
using DS4Windows;

namespace DS4Windows.DI
{
    public class DeviceStateChangedEventArgs : EventArgs
    {
        public int SlotIndex { get; }
        public bool IsConnected { get; }

        public DeviceStateChangedEventArgs(int slotIndex, bool isConnected)
        {
            SlotIndex = slotIndex;
            IsConnected = isConnected;
        }
    }

    public interface IDeviceStateService
    {
        DS4Device[] Devices { get; }
        DS4Device GetDevice(int slotIndex);
        bool IsDeviceConnected(int slotIndex);
        int ConnectedControllersCount { get; }

        string GetDeviceMacAddress(int slotIndex);
        ConnectionType GetConnectionType(int slotIndex);
        int GetBatteryLevel(int slotIndex);

        void SetDevice(int slotIndex, DS4Device device);

        event EventHandler<DeviceStateChangedEventArgs> DeviceStateChanged;
    }
}
