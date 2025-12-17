using System;

using DS4Windows;
namespace DS4Windows.Actions
{
    // Adapter that exposes the existing KeyAction implementation via the new Action API.
    public class KeyActionAdapter : SpecialActionBase
    {
        private readonly KeyAction inner;

        public KeyActionAdapter(SpecialAction sa, int index) : base(sa, index)
        {
            KeyAction temp = null;
            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    var creator = sp.GetService(typeof(DS4Windows.Actions.IKeyActionCreator)) as DS4Windows.Actions.IKeyActionCreator;
                    if (creator != null) temp = creator.CreateKeyAction(sa, index);
                }
            }
            catch { }

            inner = temp ?? new KeyAction(sa, index);
        }

        public override void OnTrigger(int device, MappingContext ctx)
        {
            try
            {
                if (inner == null) return;
                inner.OnTrigger(device, ctx?.LogicalValue ?? 0, ctx?.NativeValue ?? 0, ctx?.UseScanCode ?? false, ctx?.OutputHandler);
            }
            catch { }
        }

        public override void OnRelease(int device, MappingContext ctx)
        {
            try
            {
                if (inner == null) return;
                inner.OnRelease(device, ctx?.LogicalValue ?? 0, ctx?.NativeValue ?? 0, ctx?.UseScanCode ?? false, ctx?.OutputHandler);
            }
            catch { }
        }
    }
}
