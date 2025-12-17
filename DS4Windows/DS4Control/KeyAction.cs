using System;

namespace DS4Windows
{
    // Minimal KeyAction placeholder for migration. Full behavior will be implemented later.
    public class KeyAction
    {
        private readonly SpecialAction action;
        private readonly int index;

        public KeyAction(SpecialAction action, int index)
        {
            this.action = action;
            this.index = index;
        }

        public void OnTrigger(int device, ushort logicalValue, uint nativeValue, bool useScanCode)
        {
            try
            {
                AppLogger.LogTrace($"KeyAction.OnTrigger: name={action?.name} index={index} device={device} key={logicalValue}");
            }
            catch { }
        }

        public void OnRelease(int device, ushort logicalValue, uint nativeValue, bool useScanCode)
        {
            try
            {
                AppLogger.LogTrace($"KeyAction.OnRelease: name={action?.name} index={index} device={device} key={logicalValue}");
            }
            catch { }
        }
    }
}
