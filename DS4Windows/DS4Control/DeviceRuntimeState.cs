using System;
using System.Collections.Generic;

namespace DS4Windows
{
    // Represents per-device runtime items that previously lived as globals in Mapping (eg. untriggeraction/untriggerindex, macro queues, lightbar state).
    public class DeviceRuntimeState
    {
        public SpecialAction UntriggerAction = null;
        public int UntriggerIndex = -1;
        public bool[] MacroControl = new bool[26];
        public uint MacroCount = 0;

        // Lightbar/fade state mirrors some Mapping globals
        public int FadeTimer = 0;
        public DS4Color LastColor = new DS4Color();
        public bool ForceLight = false;

        public DeviceRuntimeState() { }
    }
}
