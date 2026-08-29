using System;
using System.Collections.Generic;
using DS4Windows;

namespace DS4Windows.Services
{
    public interface IDs4DeviceRegistry
    {
        event RequestElevationDelegate RequestElevation;
        PrepareInitDelegate PrepareDS4Init { get; set; }
        PrepareInitDelegate PostDS4Init { get; set; }
        CheckPendingDevice PreparePendingDevice { get; set; }
        bool IsExclusiveMode { get; set; }

        void FindControllers();
        IEnumerable<DS4Device> GetDS4Controllers();
        void StopControllers();
        void RemoveDevice(DS4Device device);
        void UpdateSerial(object sender, EventArgs e);
        void OnRemoval(object sender, EventArgs e);
        void ReEnableDevice(string deviceInstanceId);
    }
}