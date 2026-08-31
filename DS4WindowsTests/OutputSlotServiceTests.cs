using System;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    public class OutputSlotServiceTests
    {
        [Fact]
        public void InitialState_ShouldHaveDefaultTypes()
        {
            var service = new OutputSlotService();

            Assert.Equal(8, service.OutputDevices.Length);

            for (int i = 0; i < 8; i++)
            {
                Assert.Equal(OutContType.X360, service.GetOutputDeviceType(i));
                Assert.Null(service.GetOutputDevice(i));
                Assert.False(service.IsSlotPlugin(i));
            }
        }

        [Fact]
        public void SetOutputDeviceType_ShouldUpdateSlot()
        {
            var service = new OutputSlotService();

            service.SetOutputDeviceType(1, OutContType.DS4);
            service.SetOutputDeviceType(3, OutContType.None);

            Assert.Equal(OutContType.DS4, service.GetOutputDeviceType(1));
            Assert.Equal(OutContType.None, service.GetOutputDeviceType(3));
            Assert.Equal(OutContType.X360, service.GetOutputDeviceType(0));
        }

        [Fact]
        public void OutputSlotChangedEvent_ShouldFire()
        {
            var service = new OutputSlotService();
            OutputSlotChangedEventArgs eventArgs = null;
            service.OutputSlotChanged += (s, e) => eventArgs = e;

            service.SetOutputDevice(2, null);

            Assert.NotNull(eventArgs);
            Assert.Equal(2, eventArgs.SlotIndex);
            Assert.Null(eventArgs.OutputDevice);
        }

        [Fact]
        public void OutOfBounds_ShouldBeHandledSafely()
        {
            var service = new OutputSlotService();

            Assert.Null(service.GetOutputDevice(-1));
            Assert.Null(service.GetOutputDevice(99));
            Assert.False(service.IsSlotPlugin(-1));
            Assert.False(service.IsSlotPlugin(99));
            Assert.Equal(OutContType.X360, service.GetOutputDeviceType(-1));
            Assert.Equal(OutContType.X360, service.GetOutputDeviceType(99));

            service.SetOutputDeviceType(-1, OutContType.DS4);
            service.SetOutputDeviceType(99, OutContType.DS4);
            service.SetOutputDevice(-1, null);
            service.SetOutputDevice(99, null);
        }

        [Fact]
        public void GlobalShim_ShouldSynchronizeWithService()
        {
            var service = new OutputSlotService();
            Global.OutputSlotServiceInstance = service;

            Assert.NotNull(Global.OutputSlotServiceInstance);
            Assert.Equal(OutContType.X360, Global.OutputSlotServiceInstance.GetOutputDeviceType(0));

            Global.OutputSlotServiceInstance.SetOutputDeviceType(0, OutContType.DS4);
            Assert.Equal(OutContType.DS4, service.GetOutputDeviceType(0));
        }
    }
}
