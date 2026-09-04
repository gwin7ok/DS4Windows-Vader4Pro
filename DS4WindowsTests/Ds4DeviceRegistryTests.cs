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
            public List<int> ReIndexCalls { get; } = new List<int>();
            public List<DS4Device> RemoveCalls { get; } = new List<DS4Device>();
            public bool MockHidHideInstalled { get; set; } = true;

            public IEnumerable<DS4Device> FindControllers()
            {
                FindControllersCalls++;
                return MockDevices;
            }

            public IEnumerable<DS4Device> ConnectedDevices => MockDevices;
            public int DeviceCount => MockDevices.Count;
            public IEnumerable<DS4Device> GetDevices() => MockDevices;

            public void ReIndexDevice(DS4Device device, int desiredIndex)
            {
                ReIndexCalls.Add(desiredIndex);
            }

            public bool RemoveDevice(DS4Device device)
            {
                RemoveCalls.Add(device);
                return MockDevices.Remove(device);
            }

            public bool IsHidHideInstalled => MockHidHideInstalled;
        }

        [Fact]
        public void Adapter_InstantiatesAndImplementsInterfaceSafely()
        {
            var adapter = new Ds4DeviceRegistryAdapter();

            Assert.NotNull(adapter);
            Assert.NotNull(adapter.ConnectedDevices);
            Assert.NotNull(adapter.GetDevices());
            Assert.True(adapter.DeviceCount >= 0);

            // Null 引数に対する安全性の検証
            var reIndexEx = Record.Exception(() => adapter.ReIndexDevice(null, 0));
            Assert.Null(reIndexEx);

            bool removeNull = adapter.RemoveDevice(null);
            Assert.False(removeNull);
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

            // ReIndexDevice
            mockRegistry.ReIndexDevice(null, 2);
            Assert.Single(mockRegistry.ReIndexCalls);
            Assert.Equal(2, mockRegistry.ReIndexCalls[0]);

            // RemoveDevice
            bool removed = mockRegistry.RemoveDevice(null);
            Assert.False(removed);
            Assert.Single(mockRegistry.RemoveCalls);

            // HidHide
            Assert.True(mockRegistry.IsHidHideInstalled);
        }
    }
}
