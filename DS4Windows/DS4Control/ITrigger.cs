using DS4Windows.Services;
using System;
using DS4Windows.DS4Control;

namespace DS4Windows
{

    // Minimal trigger interface. Concrete trigger implementations can carry richer semantics.
    public interface ITrigger
    {
        TriggerType Type { get; }
        TriggerContext Context { get; }
    }
}
