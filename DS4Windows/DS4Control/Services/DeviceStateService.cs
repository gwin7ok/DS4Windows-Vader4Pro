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
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] DeviceStateService.SetDevice: Slot {slotIndex}, Device {(device != null ? "Connected" : "Disconnected")}");
                    NotifyDeviceStateChanged(slotIndex, device != null);
                }
            }
        }

        public void NotifyDeviceStateChanged(int slotIndex, bool isConnected)
        {
            DeviceStateChanged?.Invoke(this, new DeviceStateChangedEventArgs(slotIndex, isConnected));
        }


        // ---- Phase6-Step2-2 (PR-2): 起動後の初回接続判定 ----
        public bool IsFirstConnection(int slotIndex) => Global.IsFirstConnection(slotIndex);

        public void MarkConnected(int slotIndex) => Global.MarkConnected(slotIndex);

        // ---- Phase6-Step7-1: 起動時の初回接続フラグの一括リセット（App.xaml.cs から使用）----
        public void ResetConnectionFlags() => Global.ResetConnectionFlags();

        // ---- Phase6-Step2-4 (PR-4): デバイスのシリアル変更通知 ----
        // Global.DeviceSerialChange（static イベント）の購読者へ、従来と同一の引数で通知する。
        public void OnDeviceSerialChange(object sender, int slotIndex, string serial)
            => Global.OnDeviceSerialChange(sender, slotIndex, serial);
    }
}