using System.Collections.Generic;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Registry for bindings and controllers per device.
    /// </summary>
    public interface IActionRegistry
    {
        void Register(IActionBinding binding, int device);
        void Unregister(IActionBinding binding, int device);
        IEnumerable<IActionBinding> GetBindingsForDevice(int device);
    }
}
