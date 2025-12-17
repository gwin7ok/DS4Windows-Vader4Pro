using System;
using DS4Windows.DS4Control;

namespace DS4Windows
{
    // Minimal ActionManager / Action classes to enable parallel migration path.
    public static class ActionManager
    {
        // Notify the new Action system that a SpecialAction trigger was established.
        public static void NotifyTriggerEstablished(SpecialAction action, int device, ushort logicalValue, uint nativeValue, bool useScanCode, VirtualKBMBase outputKBMHandler)
        {
            try
            {
                // For now, keep this minimal: log and no-op. Future work: construct Action instances and invoke OnTrigger.
                if (action != null)
                    AppLogger.LogTrace($"ActionManager.NotifyTriggerEstablished: action={action.name} device={device} key={logicalValue}");
            }
            catch { }
        }

        // Notify release
        public static void NotifyTriggerReleased(SpecialAction action, int device, ushort logicalValue, uint nativeValue, bool useScanCode, VirtualKBMBase outputKBMHandler)
        {
            try
            {
                if (action != null)
                    AppLogger.LogTrace($"ActionManager.NotifyTriggerReleased: action={action.name} device={device} key={logicalValue}");
            }
            catch { }
        }
    }
}
