using System;
using DS4Windows.DS4Control;

namespace DS4Windows.Actions
{
    public class OutputContextImpl : IOutputContext
    {
        public OutputContextImpl(int device, VirtualKBMBase handler)
        {
            Device = device;
            OutputHandler = handler;
        }

        public int Device { get; }
        public VirtualKBMBase OutputHandler { get; }
    }
}
