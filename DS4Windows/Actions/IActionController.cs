using System;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Runtime executor for a binding on a per-device basis. Manages repeaters and state callbacks.
    /// </summary>
    public interface IActionController : IDisposable
    {
        int ControllerId { get; }
        void Start(IActionBinding binding, ITriggerContext trigger);
        void Stop(IActionBinding binding, ITriggerContext trigger);
        // Handle: forward a trigger (established or released) to the controller
        // The controller implementation should examine trigger.IsEdgeEstablished
        // and act accordingly. This avoids callers deciding Press/Toggle semantics.
        void Handle(IActionBinding binding, ITriggerContext trigger);
        void Clear();
    }
}
