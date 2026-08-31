using System;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    public class DeviceStateServiceTests
    {
        [Fact]
        public void InitialState_ShouldBeEmpty()
        {
            var service = new DeviceStateService();

            Assert.Equal(8, service.Devices.Length);
            Assert.Equal(0, service.ConnectedControllersCount);

            for (int i = 0; i < 8; i++)
            {
                Assert.Null(service.GetDevice(i));
                Assert.False(service.IsDeviceConnected(i));
                Assert.Equal(string.Empty, service.GetDeviceMacAddress(i));
                Assert.Equal(0, service.GetBatteryLevel(i));
            }
        }

        [Fact]
        public void SetDevice_ShouldUpdateSlotAndCount()
        {
            var service = new DeviceStateService();
            service.SetDevice(0, null);
            Assert.False(service.IsDeviceConnected(0));
            Assert.Equal(0, service.ConnectedControllersCount);
        }

        [Fact]
        public void DeviceStateChangedEvent_ShouldFire()
        {
            var service = new DeviceStateService();
            DeviceStateChangedEventArgs eventArgs = null;
            service.DeviceStateChanged += (s, e) => eventArgs = e;

            service.SetDevice(2, null);

            Assert.NotNull(eventArgs);
            Assert.Equal(2, eventArgs.SlotIndex);
            Assert.False(eventArgs.IsConnected);
        }

        [Fact]
        public void OutOfBounds_ShouldBeHandledSafely()
        {
            var service = new DeviceStateService();

            Assert.Null(service.GetDevice(-1));
            Assert.Null(service.GetDevice(99));
            Assert.False(service.IsDeviceConnected(-1));
            Assert.False(service.IsDeviceConnected(99));
            Assert.Equal(string.Empty, service.GetDeviceMacAddress(99));
            Assert.Equal(0, service.GetBatteryLevel(99));

            service.SetDevice(-1, null);
            service.SetDevice(99, null);
        }

        [Fact]
        public void GlobalShim_ShouldSynchronizeWithService()
        {
            var service = new DeviceStateService();
            Global.DeviceStateServiceInstance = service;

            Assert.NotNull(Global.DeviceStateServiceInstance);
            Assert.Equal(0, Global.DeviceStateServiceInstance.ConnectedControllersCount);
        }
    }
}
