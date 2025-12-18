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
        void Clear();
    }
}
