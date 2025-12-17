using System;

using DS4Windows;
namespace DS4Windows.Actions
{
    // Minimal MappingContext used by Action API. Extend as needed.
    public class MappingContext
    {
        public ushort LogicalValue { get; set; }
        public uint NativeValue { get; set; }
        public bool UseScanCode { get; set; }
        public DS4Windows.DS4Control.VirtualKBMBase OutputHandler { get; set; }
        public SpecialAction ActionDef { get; set; }
        public int Index { get; set; }
    }

    public abstract class Action
    {
        public string Name { get; protected set; }
        public SpecialAction.ActionTypeId TypeId { get; protected set; }
        public string Details { get; protected set; }

        public abstract void OnTrigger(int device, MappingContext ctx);
        public abstract void OnRelease(int device, MappingContext ctx);
        public virtual void ResetDeviceState(int device) { }
    }
}
