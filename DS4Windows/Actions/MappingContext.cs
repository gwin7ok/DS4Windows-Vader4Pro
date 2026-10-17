using DS4Windows.Services;
using System;

using DS4Windows;
namespace DS4Windows.Actions
{
    // Minimal MappingContext used by Action API. Extend as needed.
    public class MappingContext
    {
        public ushort LogicalValue { get; set; }
        public uint NativeValue { get; set; }
        public bool UseScanCode { get; set; }
        public IVirtualKBM OutputHandler { get; set; }
        public SpecialAction ActionDef { get; set; }
        public int Index { get; set; }
    }
}
