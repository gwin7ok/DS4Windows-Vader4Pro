using System.Collections.Generic;

namespace DS4Windows.Services
{
    public interface IDs4DeviceRegistry
    {
        IEnumerable<DS4Device> FindControllers();
        IEnumerable<DS4Device> ConnectedDevices { get; }

        // Phase 5 Step 11: 生デバイス管理操作の契約強化
        int DeviceCount { get; }
        IEnumerable<DS4Device> GetDevices();
        void ReIndexDevice(DS4Device device, int desiredIndex);
        bool RemoveDevice(DS4Device device);
        bool IsHidHideInstalled { get; }
    }
}
