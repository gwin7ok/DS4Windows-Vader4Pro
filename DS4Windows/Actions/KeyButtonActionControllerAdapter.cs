using System;
using DS4Windows.DS4Control;

namespace DS4Windows.Actions
{
    // Adapter that wraps existing KeyButtonActionController and exposes IActionController
    public class KeyButtonActionControllerAdapter : IActionController
    {
        private readonly KeyButtonActionController inner;

        public int ControllerId => inner?.InstanceId ?? -1;

        public KeyButtonActionControllerAdapter(int device, SpecialAction sa)
        {
            inner = new KeyButtonActionController(device, sa, sa?.name ?? "<unknown>");
        }

        // Wrap an existing KeyButtonActionController instance (used by Mapping to avoid duplicate instances)
        public KeyButtonActionControllerAdapter(KeyButtonActionController existing)
        {
            inner = existing ?? throw new ArgumentNullException(nameof(existing));
        }

        public void Start(IActionBinding binding, ITriggerContext trigger)
        {
            try
            {
                if (inner == null || trigger == null) return;
                // Delegate to existing controller entry point
                inner.OnSATriggerEstablished(trigger.LogicalValue, trigger.NativeValue, false, trigger.OutputHandler, true);
            }
            catch { }
        }

        public void Stop(IActionBinding binding, ITriggerContext trigger)
        {
            try
            {
                if (inner == null || trigger == null) return;
                inner.OnSATriggerReleased(trigger.LogicalValue, trigger.NativeValue, false, trigger.OutputHandler);
            }
            catch { }
        }

        public void Clear()
        {
            try { inner?.ClearKeyEntries(0); } catch { }
        }

        public void Dispose()
        {
            try { inner?.Dispose(); } catch { }
        }
    }
}
