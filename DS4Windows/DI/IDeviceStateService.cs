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


        // ---- Phase6-Step2-2 (PR-2): 起動後の初回接続判定（Global への薄い委譲）----
        bool IsFirstConnection(int slotIndex);
        void MarkConnected(int slotIndex);

        // ---- Phase6-Step2-4 (PR-4): デバイスのシリアル（MAC アドレス）変更通知（Global への薄い委譲）----
        void OnDeviceSerialChange(object sender, int slotIndex, string serial);
    }
}