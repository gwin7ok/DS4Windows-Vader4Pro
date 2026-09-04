using System;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    public class OutputSlotServiceTests
    {
        private class MockOutputSlotStore : IOutputSlotStore
        {
            public bool LoadResult { get; set; } = true;
            public bool SaveResult { get; set; } = true;
            public int LoadCalls { get; private set; }
            public int SaveCalls { get; private set; }

            public bool Load(OutputSlotManager slotManager)
            {
                LoadCalls++;
                return LoadResult;
            }

            public bool Save(OutputSlotManager slotManager)
            {
                SaveCalls++;
                return SaveResult;
            }
        }

        [Fact]
        public void InitialState_ShouldHaveDefaultTypes()
        {
            var service = new OutputSlotService();

            for (int i = 0; i < 8; i++)
            {
                Assert.Equal(OutContType.None, service.GetOutputDeviceType(i));
            }
        }

        [Fact]
        public void SetOutputDeviceType_ShouldUpdateSlot()
        {
            var service = new OutputSlotService();

            service.SetOutputDeviceType(0, OutContType.X360);
            service.SetOutputDeviceType(1, OutContType.DS4);

            Assert.Equal(OutContType.X360, service.GetOutputDeviceType(0));
            Assert.Equal(OutContType.DS4, service.GetOutputDeviceType(1));
            Assert.Equal(OutContType.None, service.GetOutputDeviceType(2));
        }

        [Fact]
        public void OutputSlotChangedEvent_ShouldFire()
        {
            var service = new OutputSlotService();
            OutputSlotChangedEventArgs receivedArgs = null;

            service.OutputSlotChanged += (s, e) => receivedArgs = e;

            service.SetOutputDeviceType(2, OutContType.X360);

            Assert.NotNull(receivedArgs);
            Assert.Equal(2, receivedArgs.Slot);
            Assert.Equal(OutContType.X360, receivedArgs.DeviceType);
        }

        [Fact]
        public void OutOfBounds_ShouldBeHandledSafely()
        {
            var service = new OutputSlotService();

            Assert.Equal(OutContType.None, service.GetOutputDeviceType(-1));
            Assert.Equal(OutContType.None, service.GetOutputDeviceType(8));

            service.SetOutputDeviceType(-1, OutContType.X360);
            service.SetOutputDeviceType(8, OutContType.DS4);

            Assert.Null(service.GetOutSlotDevice(-1));
            Assert.Null(service.GetOutSlotDevice(8));
            Assert.False(service.PluginSlot(-1, OutContType.X360));
            Assert.False(service.UnplugSlot(-1));
        }

        [Fact]
        public void GlobalShim_ShouldSynchronizeWithService()
        {
            var service = new OutputSlotService();
            Global.OutputSlotServiceInstance = service;

            Assert.NotNull(Global.OutputSlotServiceInstance);

            Global.OutputSlotServiceInstance.SetOutputDeviceType(0, OutContType.DS4);
            Assert.Equal(OutContType.DS4, service.GetOutputDeviceType(0));
        }

        [Fact]
        public void LoadAndSave_DelegatesToStoreSuccessfully()
        {
            var mockStore = new MockOutputSlotStore();
            var service = new OutputSlotService(new OutputSlotManager(), mockStore);

            bool loadResult = service.LoadOutputSlots();
            Assert.True(loadResult);
            Assert.Equal(1, mockStore.LoadCalls);

            bool saveResult = service.SaveOutputSlots();
            Assert.True(saveResult);
            Assert.Equal(1, mockStore.SaveCalls);
        }

        [Fact]
        public void OutputSlots_ReturnsInitializedSlots()
        {
            var service = new OutputSlotService(new OutputSlotManager());
            var slots = service.OutputSlots;

            Assert.NotNull(slots);
            Assert.Equal(8, slots.Count); // OutputSlotManager の既定スロット数は 8
        }
    }
}
