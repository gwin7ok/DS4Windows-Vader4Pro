using System;
using System.Collections.Generic;

namespace DS4Windows
{
    // Manages the collection of SpecialAction definitions and registry-level concerns
    // such as initialization, size checks and diagnostic snapshots.
    public static class ActionRegistry
    {
        private static List<SpecialAction> actions = new List<SpecialAction>();
        private static volatile bool initialized = false;

        public static void Initialize(IEnumerable<SpecialAction> source)
        {
            actions = new List<SpecialAction>(source ?? Array.Empty<SpecialAction>());
            initialized = true;
        }

        public static int Count => actions?.Count ?? 0;

        public static SpecialAction GetByIndex(int index)
        {
            if (index < 0 || index >= Count) return null;
            return actions[index];
        }

        public static IEnumerable<SpecialAction> AllActions() => actions;

        public static bool IsInitialized => initialized;

        public static string SnapshotSummary()
        {
            return $"ActionRegistry: count={Count}, initialized={initialized}";
        }
    }
}
