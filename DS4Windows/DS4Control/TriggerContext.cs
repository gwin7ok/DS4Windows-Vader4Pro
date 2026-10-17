using DS4Windows.Services;
using System;
using DS4Windows.DS4Control;

namespace DS4Windows
{
    // Represents a generic trigger event context passed to dispatchers.
    public class TriggerContext
    {
        public SpecialAction ActionDef { get; set; }
        public int Device { get; set; }
        public ushort LogicalValue { get; set; }
        public uint NativeValue { get; set; }
        public bool UseScanCode { get; set; }
        public IVirtualKBM OutputHandler { get; set; }
        public bool IsEstablished { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }
}
