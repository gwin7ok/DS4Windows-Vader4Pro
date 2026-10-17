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

        // Phase6-Step5-2: GetOutputDeviceType／SetOutputDeviceType（孤立配列 _deviceTypes）を削除したため、
        // それらを対象としたテスト（初期値・更新・グローバルシム経由の更新）は削除し、
        // イベント発行・範囲外の安全性のテストを SetOutputDevice／PluginSlot／UnplugSlot ベースへ置き換えた。
        // Phase7-Step2 W4: OutputDevices／GetOutputDevice／IsSlotPlugin／SetOutputDevice（常に null を返す孤立配列）を削除したため、
        // それらを対象としたテスト（初期状態、SetOutputDevice によるイベント発行、範囲外）も削除した。
        // 抜き差しの要求とイベントの確認は PluginSlot／UnplugSlot を対象とする（Step10FourTwoGlobalReferenceGuardTests など）。

        [Fact]
        public void OutOfBounds_ShouldBeHandledSafely()
        {
            var service = TestRepositoryFactory.CreateOutputSlotService();

            Assert.Null(service.GetOutSlotDevice(-1));
            Assert.Null(service.GetOutSlotDevice(8));
            Assert.False(service.PluginSlot(-1, OutContType.X360));
            Assert.False(service.PluginSlot(8, OutContType.DS4));
            Assert.False(service.UnplugSlot(-1));
            Assert.False(service.UnplugSlot(8));
        }

        [Fact]
        public void LoadAndSave_DelegatesToStoreSuccessfully()
        {
            var mockStore = new MockOutputSlotStore();
            var service = TestRepositoryFactory.CreateOutputSlotService(TestRepositoryFactory.CreateOutputSlotManager(), mockStore);

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
            var service = TestRepositoryFactory.CreateOutputSlotService(TestRepositoryFactory.CreateOutputSlotManager());
            var slots = service.OutputSlots;

            Assert.NotNull(slots);
            Assert.Equal(8, slots.Count); // OutputSlotManager の既定スロット数は 8
        }

        // Phase7-Step6 W1: 2 つの配列の実体は OutputSlotService が持つ（旧: Global の静的配列への参照）。
        // 参照は安定し（再代入されない）、初期値は Temp＝全スロット X360、Active＝全スロット None。
        [Fact]
        public void OutDevTypeTemp_IsOwnedByTheService_AndStable()
        {
            var service = TestRepositoryFactory.CreateOutputSlotService();
            Assert.Same(service.OutDevTypeTemp, service.OutDevTypeTemp);
            Assert.Equal(DS4Windows.ControllerLimits.TestProfileItemCount, service.OutDevTypeTemp.Length);
            Assert.All(service.OutDevTypeTemp, t => Assert.Equal(OutContType.X360, t));
        }

        [Fact]
        public void ActiveOutDevType_IsOwnedByTheService_AndStable()
        {
            var service = TestRepositoryFactory.CreateOutputSlotService();
            Assert.Same(service.ActiveOutDevType, service.ActiveOutDevType);
            Assert.Equal(DS4Windows.ControllerLimits.TestProfileItemCount, service.ActiveOutDevType.Length);
            Assert.All(service.ActiveOutDevType, t => Assert.Equal(OutContType.None, t));
        }

        [Fact]
        public void TwoServices_DoNotShareTheArrays()
        {
            var a = TestRepositoryFactory.CreateOutputSlotService();
            var b = TestRepositoryFactory.CreateOutputSlotService();
            Assert.NotSame(a.ActiveOutDevType, b.ActiveOutDevType);
            Assert.NotSame(a.OutDevTypeTemp, b.OutDevTypeTemp);
        }
    }
}