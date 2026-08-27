using DS4Windows.Services;
using System;
using DS4Windows;

namespace DS4Windows.Actions
{
    public class DefaultActionFactory : IActionFactory
    {
        public Action CreateFrom(SpecialAction sa, int index)
        {
            if (sa == null) return null;

            switch (sa.typeID)
            {
                case SpecialAction.ActionTypeId.Key:
                    return new KeyActionAdapter(sa, index);
                case SpecialAction.ActionTypeId.Program:
                    return new LaunchProcessActionAdapter(sa, index);
                case SpecialAction.ActionTypeId.Macro:
                    return new MacroActionAdapter(sa, index);
                case SpecialAction.ActionTypeId.Profile:
                    return new ProfileSwitchActionAdapter(sa, index);
                // TODO: add Disconnect etc.
                default:
                    return null;
            }
        }
    }
}
