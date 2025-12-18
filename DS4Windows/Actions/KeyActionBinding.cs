using System.Collections.Generic;
using DS4Windows.DS4Control;

namespace DS4Windows.Actions
{
    public class KeyActionBinding : IActionBinding
    {
        public KeyActionBinding(SpecialAction sa)
        {
            Special = sa;
        }

        public SpecialAction Special { get; }

        public IInputAction Input => null;

        public IReadOnlyList<IOutputAction> Outputs => new List<IOutputAction>().AsReadOnly();

        public void OnTriggered(ITriggerContext trigger)
        {
            // No-op for now; controllers are started via registry/adapters directly.
        }

        public void OnReleased(ITriggerContext trigger)
        {
            // No-op
        }
    }
}
