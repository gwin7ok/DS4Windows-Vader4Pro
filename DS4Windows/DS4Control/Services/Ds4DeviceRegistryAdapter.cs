using System;
using System.Collections.Generic;

namespace DS4Windows.Services
{
    public class Ds4DeviceRegistryAdapter : IDs4DeviceRegistry
    {
        public event RequestElevationDelegate RequestElevation
        {
            add { DS4Devices.RequestElevation += value; }
            remove { DS4Devices.RequestElevation -= value; }
        }

        public PrepareInitDelegate PrepareDS4Init
        {
            get => DS4Devices.PrepareDS4Init;
            set => DS4Devices.PrepareDS4Init = value;
        }

        public PrepareInitDelegate PostDS4Init
        {
            get => DS4Devices.PostDS4Init;
            set => DS4Devices.PostDS4Init = value;
        }

        public CheckPendingDevice PreparePendingDevice
        {
            get => DS4Devices.PreparePendingDevice;
            set => DS4Devices.PreparePendingDevice = value;
        }

        public bool IsExclusiveMode
        {
            get => DS4Devices.isExclusiveMode;
            set => DS4Devices.isExclusiveMode = value;
        }

        public void FindControllers() => DS4Devices.findControllers();
        public IEnumerable<DS4Device> GetDS4Controllers() => DS4Devices.getDS4Controllers();
        public void StopControllers() => DS4Devices.stopControllers();
        public void RemoveDevice(DS4Device device) => DS4Devices.RemoveDevice(device);
        public void UpdateSerial(object sender, EventArgs e) => DS4Devices.UpdateSerial(sender, e);
        public void OnRemoval(object sender, EventArgs e) => DS4Devices.On_Removal(sender, e);
        public void ReEnableDevice(string deviceInstanceId) => DS4Devices.reEnableDevice(deviceInstanceId);
    }
}