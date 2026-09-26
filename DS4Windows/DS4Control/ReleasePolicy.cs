using System;

namespace DS4Windows
{
    // Encapsulates evaluating whether an action should be untriggered/released based on device runtime state.
    public static class ReleasePolicy
    {
        // Evaluate whether a SpecialAction should be considered untriggered given current boolean of triggers.
        // This is a small helper for Migration: keep logic simple and delegated to Mapping until fully migrated.
        public static bool EvaluateUntrigger(bool[] triggerStates, bool automaticUntrigger)
        {
            if (automaticUntrigger)
            {
                // untrigger if any trigger control is released
                for (int i = 0; i < triggerStates.Length; i++) if (!triggerStates[i]) return true;
                return false;
            }
            else
            {
                // require all untrigger controls to be pressed
                for (int i = 0; i < triggerStates.Length; i++) if (!triggerStates[i]) return false;
                return true;
            }
        }
    }
}
