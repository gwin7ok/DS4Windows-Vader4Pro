using System;

using DS4Windows;
namespace DS4Windows.Actions
{
    // Base class for SpecialAction-backed Action implementations.
    public abstract class SpecialActionBase : Action
    {
        protected SpecialAction sa;
        protected int index;

        protected SpecialActionBase(SpecialAction sa, int index)
        {
            this.sa = sa;
            this.index = index;
            Name = sa?.name ?? string.Empty;
            TypeId = sa?.typeID ?? SpecialAction.ActionTypeId.None;
            Details = sa?.details;
        }

        public override void ResetDeviceState(int device)
        {
            try { ActionManager.ClearDeviceState(device); } catch { }
        }
    }
}
