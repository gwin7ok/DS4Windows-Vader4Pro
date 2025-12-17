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
                // TODO: add Macro, Profile, Program, Disconnect etc.
                default:
                    return null;
            }
        }
    }
}
