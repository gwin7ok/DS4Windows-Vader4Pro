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

        private readonly System.Collections.Generic.List<IOutputAction> outputs = new System.Collections.Generic.List<IOutputAction>();

        public KeyActionBinding(SpecialAction sa, bool populateOutputs = true) : this(sa)
        {
            if (populateOutputs && sa != null)
            {
                outputs.Add(new KeyOutputAction(sa));
            }
        }

        public IReadOnlyList<IOutputAction> Outputs => outputs.AsReadOnly();

        public void OnTriggered(ITriggerContext trigger)
        {
            try
            {
                // Invoke each output's Execute with a concrete context
                var ctx = new OutputContextImpl(trigger.Device, trigger.OutputHandler);
                foreach (var o in Outputs)
                {
                    try { o.Execute(ctx); } catch { }
                }
            }
            catch { }
        }

        public void OnReleased(ITriggerContext trigger)
        {
            try
            {
                var ctx = new OutputContextImpl(trigger.Device, trigger.OutputHandler);
                foreach (var o in Outputs)
                {
                    try { o.Stop(ctx); } catch { }
                }
            }
            catch { }
        }
    }
}
