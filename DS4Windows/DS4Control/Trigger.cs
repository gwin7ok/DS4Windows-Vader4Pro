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
        public DS4Windows.DS4Control.VirtualKBMBase OutputHandler { get; set; }
        public bool IsEstablished { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }

    public enum TriggerType
    {
        SingleButton,
        Combination,
        LongPress,
        Synthetic
    }

    // Minimal trigger interface. Concrete trigger implementations can carry richer semantics.
    public interface ITrigger
    {
        TriggerType Type { get; }
        TriggerContext Context { get; }
    }

    public class SingleButtonTrigger : ITrigger
    {
        public TriggerType Type => TriggerType.SingleButton;
        public TriggerContext Context { get; private set; }

        public SingleButtonTrigger(int device, ushort logicalValue, uint nativeValue, bool useScanCode, bool isEstablished, SpecialAction action = null, DS4Windows.DS4Control.VirtualKBMBase handler = null)
        {
            Context = new TriggerContext
            {
                Device = device,
                LogicalValue = logicalValue,
                NativeValue = nativeValue,
                UseScanCode = useScanCode,
                IsEstablished = isEstablished,
                ActionDef = action,
                OutputHandler = handler
            };
        }
    }
}
