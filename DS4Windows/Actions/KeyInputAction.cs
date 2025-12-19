using System;
using DS4Windows.DS4Control;

namespace DS4Windows.Actions
{
    public class KeyInputAction : IInputAction
    {
        private readonly SpecialAction sa;
        public KeyInputAction(SpecialAction sa) { this.sa = sa; }
        public string Name => sa?.name ?? "KeyInput";

        public ITriggerContext Evaluate(DS4State state)
        {
            // Legacy trigger evaluation remains in Mapping/KeyAction; this is a placeholder for future use.
            return null;
        }

        public void Reset() { }
    }
}
