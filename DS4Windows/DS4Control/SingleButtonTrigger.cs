using DS4Windows.Services;
using System;
using DS4Windows.DS4Control;

namespace DS4Windows
{

    public class SingleButtonTrigger : ITrigger
    {
        public TriggerType Type => TriggerType.SingleButton;
        public TriggerContext Context { get; private set; }

        public SingleButtonTrigger(int device, ushort logicalValue, uint nativeValue, bool useScanCode, bool isEstablished, SpecialAction action = null, IVirtualKBM handler = null)
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
