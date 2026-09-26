using System;
using DS4Windows.Actions;

namespace DS4Windows.Actions.Tests
{
    // Deterministic fake repeater for tests: Start stores tickAction and exposes TriggerOnce to invoke it.
    public class FakeDeterministicRepeater : IRepeater
    {
        private System.Action tickAction;
        private bool running = false;

        public void Start(TimeSpan initialDelay, TimeSpan interval, System.Action tickAction)
        {
            this.tickAction = tickAction;
            running = true;
        }

        public void Stop()
        {
            running = false;
        }

        public void TriggerOnce()
        {
            if (running && tickAction != null)
            {
                try { tickAction(); } catch { }
            }
        }

        public void Dispose()
        {
            running = false;
            tickAction = null;
        }
    }
}
