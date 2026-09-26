using System;
using System.Threading;
using DS4Windows.Actions;
using Xunit;

namespace DS4Windows.Actions.Tests
{
    // Simple mock repeater for testing Start/Stop calls
    class MockRepeater : IRepeater
    {
        public bool Started { get; private set; }
        public bool Stopped { get; private set; }
        public System.Action TickAction { get; private set; }
        public TimeSpan Interval { get; private set; }

        public void Start(TimeSpan initialDelay, TimeSpan interval, System.Action tickAction)
        {
            Started = true;
            Stopped = false;
            TickAction = tickAction;
            Interval = interval;
        }

        public void Stop()
        {
            Stopped = true;
            Started = false;
        }

        // Explicit interface implementations to ensure correct interface mapping
        void DS4Windows.Actions.IRepeater.Start(TimeSpan initialDelay, TimeSpan interval, System.Action tickAction)
        {
            Start(initialDelay, interval, tickAction);
        }

        void DS4Windows.Actions.IRepeater.Stop()
        {
            Stop();
        }

        public void Dispose() { }
    }

    // Minimal test-action-controller implementation used to verify repeater usage
    class TestActionController : IActionController
    {
        public int ControllerId { get; }
        private readonly IRepeater repeater;
        public bool Running { get; private set; }

        public TestActionController(int id, IRepeater repeater)
        {
            ControllerId = id;
            this.repeater = repeater;
        }

        public void Start(IActionBinding binding, ITriggerContext trigger)
        {
            Running = true;
            repeater.Start(TimeSpan.Zero, TimeSpan.FromMilliseconds(33), () => { });
        }

        public void Stop(IActionBinding binding, ITriggerContext trigger)
        {
            Running = false;
            repeater.Stop();
        }

        public void Handle(IActionBinding binding, ITriggerContext trigger)
        {
            // For tests treat established as Start, released as Stop
            if (trigger != null && trigger.IsEdgeEstablished)
                Start(binding, trigger);
            else
                Stop(binding, trigger);
        }

        public void Clear() { Running = false; }

        public void Dispose() { }
    }

    public class ActionControllerTests
    {
        [Fact]
        public void Start_Should_Invoke_RepeaterStart_And_Stop_Should_Invoke_RepeaterStop()
        {
            var mock = new MockRepeater();
            var controller = new TestActionController(1, mock);

            // Create minimal fake binding and trigger (nulls acceptable for this test)
            IActionBinding binding = null;
            ITriggerContext trigger = null;

            controller.Start(binding, trigger);
            Assert.True(mock.Started);
            Assert.False(mock.Stopped);

            controller.Stop(binding, trigger);
            Assert.True(mock.Stopped);
            Assert.False(mock.Started);
        }
    }
}
