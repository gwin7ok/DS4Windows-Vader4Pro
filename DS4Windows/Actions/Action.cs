using DS4Windows.Services;
using System;

using DS4Windows;
namespace DS4Windows.Actions
{

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
