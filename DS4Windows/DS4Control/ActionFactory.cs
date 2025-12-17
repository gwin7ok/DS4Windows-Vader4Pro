using System;

using DS4Windows;
namespace DS4Windows.Actions
{
    public static class ActionFactory
    {
        // Backwards-compatible static factory. If DI Provider is available, delegate to registered IActionFactory.
        public static Action CreateFrom(SpecialAction sa, int index)
        {
            if (sa == null) return null;

            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    try
                    {
                        var impl = sp.GetService(typeof(DS4Windows.Actions.IActionFactory)) as DS4Windows.Actions.IActionFactory;
                        if (impl != null) return impl.CreateFrom(sa, index);
                    }
                    catch { }
                }
            }
            catch { }

            // Fallback to minimal static behavior
            switch (sa.typeID)
            {
                case SpecialAction.ActionTypeId.Key:
                    return new KeyActionAdapter(sa, index);
                // Add other mappings as needed (Macro, Profile, Program, etc.)
                default:
                    return null;
            }
        }
    }
}
