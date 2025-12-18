using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using DS4Windows.DS4Control;
using DS4Windows;

namespace StandaloneTests
{
    [TestClass]
    public class DispatchTriggerTests
    {
        private class TestVirtualKBM : VirtualKBMBase
        {
            public int PressCount = 0;
            public int ReleaseCount = 0;

            public override bool Connect() => true;
            public override bool Disconnect() => true;
            public override void MoveRelativeMouse(int x, int y) { }
            public override void MoveAbsoluteMouse(double x, double y) { }
            public override void PerformMouseWheelEvent(int vertical, int horizontal) { }
            public override void PerformMouseButtonEvent(uint mouseButton) { }
            public override void PerformMouseButtonEventAlt(uint mouseButton, int type) { }
            public override void PerformMouseButtonPress(uint mouseButton) { }
            public override void PerformMouseButtonRelease(uint mouseButton) { }

            public override void PerformKeyPress(uint key)
            {
                Interlocked.Increment(ref PressCount);
            }

            public override void PerformKeyPressAlt(uint key)
            {
                Interlocked.Increment(ref PressCount);
            }

            public override void PerformKeyRelease(uint key)
            {
                Interlocked.Increment(ref ReleaseCount);
            }

            public override void PerformKeyReleaseAlt(uint key)
            {
                Interlocked.Increment(ref ReleaseCount);
            }

            public override void Sync() { }
            public override string GetDisplayName() => "TestVirtualKBM";
            public override string GetIdentifier() => "TestVirtualKBM";
            public override string GetFullDisplayName() => "TestVirtualKBM";
        }

        [TestMethod]
        public void DispatchViaManager_StartsOnlyOneRepeater()
        {
            int device = 0;
            ushort logicalKey = 71;

            var sa = TestHelpers.CreateKeyAction("dispatch-test", "", logicalKey.ToString());
            ActionRegistry.Initialize(new SpecialAction[] { sa });

            ActionManager.ClearDeviceState(device);

            var handler = new TestVirtualKBM();

            // Act: dispatch via ActionManager.DispatchTriggerEstablished directly
            bool dispatched = ActionManager.DispatchTriggerEstablished(sa, device, logicalKey, logicalKey, false, handler);
            Assert.IsTrue(dispatched, "Expected dispatch to succeed");

            Thread.Sleep(160);

            int pressesBefore = handler.PressCount;
            Assert.IsTrue(pressesBefore >= 1, "Expected at least one synthetic press before release");

            // Release via ActionManager.DispatchTriggerReleased
            bool released = ActionManager.DispatchTriggerReleased(sa, device, logicalKey, logicalKey, false, handler);
            Assert.IsTrue(released, "Expected release dispatch to succeed");

            Thread.Sleep(250);

            int pressesAfter = handler.PressCount;
            Assert.IsTrue(pressesAfter - pressesBefore <= 1, $"Expected repeater to stop after release (before={pressesBefore}, after={pressesAfter})");

            // Cleanup
            ActionManager.ClearDeviceState(device);
            ActionRegistry.Initialize(null);
        }
    }
}
