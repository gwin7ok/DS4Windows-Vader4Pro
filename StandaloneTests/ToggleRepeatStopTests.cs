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
        private class TestServiceProvider : IServiceProvider
        {
            private readonly object svc;
            public TestServiceProvider(object svc) { this.svc = svc; }
            public object GetService(Type serviceType)
            {
                if (svc != null && serviceType.IsInstanceOfType(svc)) return svc;
                return null;
            }
        }

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
            var dam = new DS4Windows.Actions.DefaultActionManager();
            var srv = new TestServiceProvider(dam);
            DS4Windows.DI.ServiceProviderHolder.SetProvider(srv);

            try
            {
                int device = 0;
                ushort logicalKey = 70;

                var sa = TestHelpers.CreateToggleAction(logicalKey);
                ActionRegistry.Initialize(new SpecialAction[] { sa });

                dam.ClearAllToggledOn();
                dam.ClearAllEntries();
                ActionManager.ClearDeviceState(device);

                var handler = new TestVirtualKBM();

                // 1st trigger: Turns Toggle ON
                bool dispatched = ActionManager.DispatchTriggerEstablished(sa, device, logicalKey, logicalKey, false, handler);
                Assert.IsTrue(dispatched, "Expected dispatch to succeed");
                ActionManager.DispatchTriggerReleased(sa, device, logicalKey, logicalKey, false, handler);

                // Allow repeat intervals while toggle is ON
                Thread.Sleep(160);

                int pressesBefore = handler.PressCount;
                Assert.IsTrue(pressesBefore >= 1, "Expected at least one synthetic press before toggle off");

                // 2nd trigger: Turns Toggle OFF
                bool toggleOffDispatched = ActionManager.DispatchTriggerEstablished(sa, device, logicalKey, logicalKey, false, handler);
                Assert.IsTrue(toggleOffDispatched, "Expected toggle-off dispatch to succeed");
                ActionManager.DispatchTriggerReleased(sa, device, logicalKey, logicalKey, false, handler);

                // Wait to observe repeats have stopped
                Thread.Sleep(250);

                int pressesAfter = handler.PressCount;

                // Assert: no further presses after turning toggle off
                Assert.IsTrue(pressesAfter - pressesBefore <= 1, $"Expected repeater to stop after toggle off (before={pressesBefore}, after={pressesAfter})");

                ActionManager.ClearDeviceState(device);
                ActionRegistry.Initialize(null);
            }
            finally
            {
                DS4Windows.DI.ServiceProviderHolder.SetProvider(null);
            }
        }
    }
}