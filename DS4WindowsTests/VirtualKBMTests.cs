using Xunit;
using DS4Windows.Services;
using DS4Windows.Actions;
using DS4Windows.DS4Control;

namespace DS4WindowsTests
{
    public class VirtualKBMTests
    {
        [Fact]
        public void OutputKBMHandlerAdapter_NullSafe_WhenHandlerIsNull()
        {
            // Global.outputKBMHandler が null の場合でも例外を投げないことを確認
            var adapter = new OutputKBMHandlerAdapter();

            bool connected = adapter.Connect();
            Assert.False(connected);

            bool disconnected = adapter.Disconnect();
            Assert.False(disconnected);

            // 各操作が例外なく安全に実行できることを検証
            adapter.MoveRelativeMouse(10, 20);
            adapter.MoveAbsoluteMouse(0.5, 0.5);
            adapter.PerformMouseButtonPress(1);
            adapter.PerformMouseButtonRelease(1);
            adapter.PerformKeyPress(0x41);
            adapter.PerformKeyRelease(0x41);
            adapter.PerformMouseWheelEvent(120, 0);
            adapter.Sync();

            Assert.Equal(string.Empty, adapter.ErrorMessage);
            Assert.Equal("0.0.0.0", adapter.Version);
        }

        [Fact]
        public void MockVirtualKBM_RecordsEventsAccurately()
        {
            var mock = new MockVirtualKBM();

            mock.Connect();
            mock.MoveRelativeMouse(5, -5);
            mock.PerformMouseButtonPress(1);
            mock.PerformKeyPress(0x1E);
            mock.PerformKeyRelease(0x1E);
            mock.PerformMouseButtonRelease(1);
            mock.Sync();
            mock.Disconnect();

            Assert.Equal(1, mock.ConnectCallCount);
            Assert.Equal(1, mock.DisconnectCallCount);
            Assert.Equal(1, mock.SyncCallCount);
            Assert.Single(mock.MoveRelativeCalls);
            Assert.Equal((5, -5), mock.MoveRelativeCalls[0]);
            Assert.Single(mock.KeyPressCalls);
            Assert.Equal((uint)0x1E, mock.KeyPressCalls[0]);
            Assert.Single(mock.KeyReleaseCalls);
        }

        [Fact]
        public void DefaultMacroPlayer_StateTracking_WorksProperly()
        {
            var player = new DefaultMacroPlayer();

            Assert.False(player.IsPlaying(0));
            Assert.False(player.IsPlaying(-1));
            Assert.False(player.IsPlaying(4));

            // Stop を呼んでも安全に動作する
            player.Stop(0);
            Assert.False(player.IsPlaying(0));
        }

        [Fact]
        public void MouseOutputAction_WithMock_ExecutesWithoutException()
        {
            var mockKbm = new MockVirtualKBM();
            var sa = new SpecialAction("TestMouseAction");
            var action = new MouseOutputAction(sa, mockKbm);

            Assert.Equal("TestMouseAction", action.Id);

            var ctx = new OutputContextImpl
            {
                Device = 0,
                OutputHandler = mockKbm
            };

            action.Execute(ctx);
            action.Stop(ctx);
        }
    }
}
