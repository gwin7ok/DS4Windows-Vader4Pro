using System;
using System.Collections.Generic;
using DS4Windows;

namespace DS4Windows.Services
{
    public interface IDs4DeviceRegistry
    {
        // === デバイス検出・列挙契約 ===
        IEnumerable<DS4Device> FindControllers();
        IEnumerable<DS4Device> ConnectedDevices { get; }
        IEnumerable<DS4Device> GetDS4Controllers();
        int DeviceCount { get; }

        // === ライフサイクル・切断制御 ===
        void StopControllers();
        bool RemoveDevice(DS4Device device);
        void OnRemoval(object sender, EventArgs e);
        void UpdateSerial(object sender, EventArgs e);
        void ReEnableDevice(string deviceInstanceId);

        // === 動作モード・ドライバ状態 ===
        bool IsExclusiveMode { get; set; }
        bool IsHidHideInstalled { get; }

        // === 初期化・昇格イベントおよびデリゲート ===
        event RequestElevationDelegate RequestElevation;
        PrepareInitDelegate PrepareDS4Init { get; set; }
        PrepareInitDelegate PostDS4Init { get; set; }
        CheckPendingDevice PreparePendingDevice { get; set; }
    }
}
