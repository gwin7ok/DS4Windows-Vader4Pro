using System;

using DS4Windows;
namespace DS4Windows.Actions
{
    public static class ActionFactory
    {
        // Minimal factory: create Action wrapper for known SpecialAction types.
        public static Action CreateFrom(SpecialAction sa, int index)
        {
            if (sa == null) return null;

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
