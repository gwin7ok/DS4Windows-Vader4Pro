using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.Serialization;
using System.Threading;
using DS4Windows.DS4Control;
using DS4Windows;

namespace StandaloneTests
{
    [TestClass]
    public class ToggleRepeatStopTests
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
        public void Toggle_OnThenOff_StopsRepeater()
        {
            // Arrange
            int device = 0;
            ushort logicalKey = 70;

            // Create a SpecialAction via public constructor with safe defaults and mark it as Toggle
            var sa = TestHelpers.CreateToggleAction(logicalKey);

            // Initialize registry with our action
            ActionRegistry.Initialize(new SpecialAction[] { sa });

            // Ensure mapping/controller dict is clean for device
            ActionManager.ClearDeviceState(device);

            var handler = new TestVirtualKBM();

            // Act: dispatch via ActionManager (canonical path)
            bool dispatched = ActionManager.DispatchTriggerEstablished(sa, device, logicalKey, logicalKey, false, handler);
            Assert.IsTrue(dispatched, "Expected dispatch to succeed");

            // Allow a few repeat intervals to occur
            Thread.Sleep(160);

            int pressesBefore = handler.PressCount;
            Assert.IsTrue(pressesBefore >= 1, "Expected at least one synthetic press before release");

            // Now send release via ActionManager
            bool released = ActionManager.DispatchTriggerReleased(sa, device, logicalKey, logicalKey, false, handler);
            Assert.IsTrue(released, "Expected release dispatch to succeed");

            // Wait to observe whether repeats continue
            Thread.Sleep(250);

            int pressesAfter = handler.PressCount;

            // Assert: no further presses after release (allowing 1 extra in-flight)
            Assert.IsTrue(pressesAfter - pressesBefore <= 1, $"Expected repeater to stop after release (before={pressesBefore}, after={pressesAfter})");

            // Cleanup
            ActionManager.ClearDeviceState(device);
            ActionRegistry.Initialize(null);
        }
    }
}
