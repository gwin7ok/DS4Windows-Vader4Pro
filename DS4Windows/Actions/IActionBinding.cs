using System.Collections.Generic;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Binds an input action to one or more output actions and mediates lifecycle events.
    /// </summary>
    public interface IActionBinding
    {
        IInputAction Input { get; }
        IReadOnlyList<IOutputAction> Outputs { get; }
        void OnTriggered(ITriggerContext trigger);
        void OnReleased(ITriggerContext trigger);
    }
}
