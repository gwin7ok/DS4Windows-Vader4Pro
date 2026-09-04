using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using DS4Windows;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    public class Ds4DeviceRegistryTests
    {
        private class MockDs4DeviceRegistry : IDs4DeviceRegistry
        {
            public List<DS4Device> MockDevices { get; } = new List<DS4Device>();
            public int FindControllersCalls { get; private set; }
            public int StopControllersCalls { get; private set; }
            public List<DS4Device> RemoveCalls { get; } = new List<DS4Device>();
            public bool MockHidHideInstalled { get; set; } = true;
            public bool IsExclusiveMode { get; set; }

            public event RequestElevationDelegate RequestElevation;
            public PrepareInitDelegate PrepareDS4Init { get; set; }
            public PrepareInitDelegate PostDS4Init { get; set; }
            public CheckPendingDevice PreparePendingDevice { get; set; }

            public IEnumerable<DS4Device> FindControllers()
            {
                FindControllersCalls++;
                return MockDevices;
            }

            public IEnumerable<DS4Device> ConnectedDevices => MockDevices;
            public IEnumerable<DS4Device> GetDS4Controllers() => MockDevices;
            public int DeviceCount => MockDevices.Count;

            public void StopControllers()
            {
                StopControllersCalls++;
            }

            public bool RemoveDevice(DS4Device device)
            {
                RemoveCalls.Add(device);
                return MockDevices.Remove(device);
            }

            public void OnRemoval(HidDevice hidDevice) { }
            public void UpdateSerial(HidDevice hidDevice, bool warn = true) { }
            public void ReEnableDevice(string deviceInstanceId) { }

            public bool IsHidHideInstalled => MockHidHideInstalled;
        }

        [Fact]
        public void Adapter_InstantiatesAndImplementsInterfaceSafely()
        {
            var adapter = new Ds4DeviceRegistryAdapter();

            Assert.NotNull(adapter);
            Assert.NotNull(adapter.ConnectedDevices);
            Assert.NotNull(adapter.GetDS4Controllers());
            Assert.True(adapter.DeviceCount >= 0);

            // Null 引数に対する安全性の検証
            bool removeNull = adapter.RemoveDevice(null);
            Assert.False(removeNull);

            var onRemovalEx = Record.Exception(() => adapter.OnRemoval(null));
            Assert.Null(onRemovalEx);

            var updateSerialEx = Record.Exception(() => adapter.UpdateSerial(null));
            Assert.Null(updateSerialEx);

            var reEnableEx = Record.Exception(() => adapter.ReEnableDevice(null));
            Assert.Null(reEnableEx);
        }

        [Fact]
        public void MockRegistry_TracksDeviceOperationsAccurately()
        {
            var mockRegistry = new MockDs4DeviceRegistry();

            // FindControllers
            var found = mockRegistry.FindControllers();
            Assert.NotNull(found);
            Assert.Equal(1, mockRegistry.FindControllersCalls);

            // DeviceCount
            Assert.Equal(0, mockRegistry.DeviceCount);

            // StopControllers
            mockRegistry.StopControllers();
            Assert.Equal(1, mockRegistry.StopControllersCalls);

            // RemoveDevice
            bool removed = mockRegistry.RemoveDevice(null);
            Assert.False(removed);
            Assert.Single(mockRegistry.RemoveCalls);

            // HidHide
            Assert.True(mockRegistry.IsHidHideInstalled);
        }
    }
}
