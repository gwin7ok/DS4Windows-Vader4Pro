using System;
using DS4Windows.DS4Control;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Context passed to output actions containing device and handler references.
    /// </summary>
    public interface IOutputContext
    {
        int Device { get; }
        VirtualKBMBase OutputHandler { get; }
    }
}
