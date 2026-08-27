using DS4Windows.Services;
using System;
using DS4Windows.DS4Control;

namespace DS4Windows.Actions
{
    public class OutputContextImpl : IOutputContext
    {
        public OutputContextImpl(int device, IVirtualKBM handler)
        {
            Device = device;
            OutputHandler = handler;
        }

        public int Device { get; }
        public IVirtualKBM OutputHandler { get; }
    }
}
