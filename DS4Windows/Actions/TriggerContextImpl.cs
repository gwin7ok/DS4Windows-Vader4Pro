using System;
using DS4Windows.Actions;
using DS4Windows.DS4Control;

namespace DS4Windows.Actions
{
    public class TriggerContextImpl : ITriggerContext
    {
        public int Device { get; set; }
        public bool IsEdgeEstablished { get; set; }
        public ushort LogicalValue { get; set; }
        public uint NativeValue { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public VirtualKBMBase OutputHandler { get; set; }
    }
}
