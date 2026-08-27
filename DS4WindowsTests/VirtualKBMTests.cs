using NUnit.Framework;
using DS4Windows.Services;
using DS4Windows.Actions;
using DS4Windows.DS4Control;

namespace DS4WindowsTests
{
    [TestFixture]
    public class VirtualKBMTests
    {
        [Test]
        public void OutputKBMHandlerAdapter_NullSafe_WhenHandlerIsNull()
        {
            // Global.outputKBMHandler が null の場合でも例外を投げないことを確認
            var adapter = new OutputKBMHandlerAdapter();

            Assert.DoesNotThrow(() =>
            {
                bool connected = adapter.Connect();
                Assert.IsFalse(connected);

                bool disconnected = adapter.Disconnect();
                Assert.IsFalse(disconnected);

                adapter.MoveRelativeMouse(10, 20);
                adapter.MoveAbsoluteMouse(0.5, 0.5);
                adapter.PerformMouseButtonPress(1);
                adapter.PerformMouseButtonRelease(1);
                adapter.PerformKeyPress(0x41);
                adapter.PerformKeyRelease(0x41);
                adapter.PerformMouseWheelEvent(120, 0);
                adapter.Sync();
            });

            Assert.AreEqual(string.Empty, adapter.ErrorMessage);
            Assert.AreEqual("0.0.0.0", adapter.Version);
        }

        [Test]
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

            Assert.AreEqual(1, mock.ConnectCallCount);
            Assert.AreEqual(1, mock.DisconnectCallCount);
            Assert.AreEqual(1, mock.SyncCallCount);
            Assert.AreEqual(1, mock.MoveRelativeCalls.Count);
            Assert.AreEqual((5, -5), mock.MoveRelativeCalls[0]);
            Assert.AreEqual(1, mock.KeyPressCalls.Count);
            Assert.AreEqual((uint)0x1E, mock.KeyPressCalls[0]);
            Assert.AreEqual(1, mock.KeyReleaseCalls.Count);
        }

        [Test]
        public void DefaultMacroPlayer_StateTracking_WorksProperly()
        {
            var player = new DefaultMacroPlayer();

            Assert.IsFalse(player.IsPlaying(0));
            Assert.IsFalse(player.IsPlaying(-1));
            Assert.IsFalse(player.IsPlaying(4));

            // Stop を呼んでも安全に動作する
            Assert.DoesNotThrow(() => player.Stop(0));
            Assert.IsFalse(player.IsPlaying(0));
        }

        [Test]
        public void MouseOutputAction_WithMock_ExecutesWithoutException()
        {
            var mockKbm = new MockVirtualKBM();
            var sa = new SpecialAction("TestMouseAction");
            var action = new MouseOutputAction(sa, mockKbm);

            Assert.AreEqual("TestMouseAction", action.Id);

            var ctx = new OutputContextImpl
            {
                Device = 0,
                OutputHandler = mockKbm
            };

            Assert.DoesNotThrow(() => action.Execute(ctx));
            Assert.DoesNotThrow(() => action.Stop(ctx));
        }
    }
}
