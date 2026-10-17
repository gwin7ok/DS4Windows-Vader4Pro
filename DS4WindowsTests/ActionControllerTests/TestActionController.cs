using System;
using System.Threading;
using DS4Windows.Actions;
using Xunit;

namespace DS4Windows.Actions.Tests
{

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
}
