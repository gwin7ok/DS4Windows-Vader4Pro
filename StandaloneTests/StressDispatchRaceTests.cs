using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Collections.Generic;
using DS4Windows.DS4Control;
using DS4Windows;

namespace StandaloneTests
{
    [TestClass]
    public class StressDispatchRaceTests
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
        public void ConcurrentDispatchAndDirectControllerCalls_NoPersistentRepeats()
        {
            int device = 0;
            ushort logicalKey = 72;
            var sa = TestHelpers.CreateKeyAction("stress-dispatch", "", logicalKey.ToString());

            ActionRegistry.Initialize(new SpecialAction[] { sa });
            ActionManager.ClearDeviceState(device);

            var handler = new TestVirtualKBM();

            int threadCount = 8;
            int iterationsPerThread = 200;

            var threads = new List<Thread>();
            var startSignal = new ManualResetEventSlim(false);

            // Half threads will call ActionManager.Dispatch*, half will call Mapping direct controller calls
            for (int i = 0; i < threadCount; ++i)
            {
                int idx = i;
                var t = new Thread(() =>
                {
                    // Wait for start
                    startSignal.Wait();
                    var rnd = new Random(Environment.TickCount ^ idx);
                    for (int j = 0; j < iterationsPerThread; ++j)
                    {
                        try
                        {
                            if (idx % 2 == 0)
                            {
                                // Use ActionManager dispatch
                                ActionManager.DispatchTriggerEstablished(sa, device, logicalKey, logicalKey, false, handler);
                                // small jitter
                                Thread.Sleep(rnd.Next(0, 3));
                                ActionManager.DispatchTriggerReleased(sa, device, logicalKey, logicalKey, false, handler);
                            }
                            else
                            {
                                // Direct controller invocation (legacy path)
                                var kbc = Mapping.GetOrCreateKeyButtonControllerForAction(device, sa);
                                if (kbc != null)
                                {
                                    kbc.OnSATriggerEstablished(logicalKey, logicalKey, false, handler, true);
                                    Thread.Sleep(rnd.Next(0, 3));
                                    kbc.OnSATriggerReleased(logicalKey, logicalKey, false, handler);
                                }
                            }
                        }
                        catch { }
                    }
                });
                t.IsBackground = true;
                threads.Add(t);
            }

            // Start all threads
            foreach (var t in threads) t.Start();
            startSignal.Set();

            // Wait for threads to complete
            foreach (var t in threads) t.Join();

            // Allow any in-flight repeats to settle
            Thread.Sleep(500);

            int presses = handler.PressCount;

            // Expect that presses remain bounded reasonably (not growing unboundedly)
            // With 8 threads * 200 iterations we expect many presses, but after threads finish no persistent repeater should keep producing presses.
            // To be conservative, assert that no presses occurred in the last 500ms window beyond a small allowance.
            int before = presses;
            Thread.Sleep(300);
            int after = handler.PressCount;
            int delta = after - before;

            Assert.IsTrue(delta <= 2, $"Expected no persistent repeater after concurrent operations (delta={delta})");

            // Cleanup
            ActionManager.ClearDeviceState(device);
            ActionRegistry.Initialize(null);
        }
    }
}
