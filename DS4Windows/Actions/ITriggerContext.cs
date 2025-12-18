using System;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Lightweight DTO describing a detected trigger from input evaluation.
    /// </summary>
    public interface ITriggerContext
    {
        int Device { get; }
        bool IsEdgeEstablished { get; }
        ushort LogicalValue { get; }
        uint NativeValue { get; }
        DateTime Timestamp { get; }
        DS4Windows.DS4Control.VirtualKBMBase OutputHandler { get; }
    }
}
