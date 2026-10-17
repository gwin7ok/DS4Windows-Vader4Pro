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
}
