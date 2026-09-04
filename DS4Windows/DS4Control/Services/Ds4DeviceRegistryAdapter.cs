using System;
using System.Collections.Generic;

namespace DS4Windows.Services
{
    public class Ds4DeviceRegistryAdapter : IDs4DeviceRegistry
    {
        public IEnumerable<DS4Device> FindControllers()
        {
            DS4Devices.findControllers();
            return DS4Devices.getDS4Controllers();
        }

        public IEnumerable<DS4Device> ConnectedDevices => DS4Devices.getDS4Controllers();

        public int DeviceCount => DS4Devices.count();

        public IEnumerable<DS4Device> GetDevices() => DS4Devices.getDS4Controllers();

        public void ReIndexDevice(DS4Device device, int desiredIndex)
        {
            if (device == null) return;
            DS4Devices.reIndexDevice(device, desiredIndex);
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] Ds4DeviceRegistryAdapter.ReIndexDevice: Device reindexed to slot {desiredIndex}");
        }

        public bool RemoveDevice(DS4Device device)
        {
            if (device == null) return false;
            bool result = DS4Devices.removeDevice(device);
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] Ds4DeviceRegistryAdapter.RemoveDevice: Device removed (result={result})");
            return result;
        }

        public bool IsHidHideInstalled => DS4Devices.isHidHideInstalled;
    }
}
