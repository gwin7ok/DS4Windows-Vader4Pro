using Microsoft.Win32;
using System;

namespace DS4Windows
{
    // Simple holder that maps Windows keyboard settings to milliseconds used by RepeatHelper
    public static class KeyboardSettings
    {
        // Raw registry values
        public static int DelayIndex { get; private set; } = 1; // KeyboardDelay value (0..3)
        public static int SpeedValue { get; private set; } = 31; // KeyboardSpeed value (0..31)

        // Delay before repeating (ms)
        public static int InitialRepeatDelayMs { get; private set; } = 500;

        // Interval between repeated keypresses (ms)
        public static int RepeatIntervalMs { get; private set; } = 50;

        // Approx characters per second derived from speed
        public static double RepeatCharsPerSec { get; private set; } = 30.0;

        // Try to read HKCU:\Control Panel\Keyboard KeyboardDelay and KeyboardSpeed
        public static void LoadFromRegistry()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey("Control Panel\\Keyboard"))
                {
                    if (key == null)
                    {
                        try { AppLogger.LogInfo("KeyboardSettings.LoadFromRegistry: registry key not found: HKCU\\Control Panel\\Keyboard"); } catch { }
                        return;
                    }

                    object delayVal = key.GetValue("KeyboardDelay");
                    object speedVal = key.GetValue("KeyboardSpeed");

                    int delay = 1; // default index
                    int speed = 31; // default fastest
                    try { if (delayVal != null) delay = Convert.ToInt32(delayVal); } catch { }
                    try { if (speedVal != null) speed = Convert.ToInt32(speedVal); } catch { }

                    DelayIndex = Math.Max(0, Math.Min(3, delay));
                    SpeedValue = Math.Max(0, Math.Min(31, speed));

                    // Map delay index (0..3) -> ms. Use 250ms steps starting at 250ms
                    InitialRepeatDelayMs = 250 * (DelayIndex + 1);

                    // Map speed (0..31) to chars/sec range [2.5,30] then compute interval
                    RepeatCharsPerSec = 2.5 + (SpeedValue / 31.0) * (30.0 - 2.5);
                    int interval = (int)Math.Round(1000.0 / Math.Max(1.0, RepeatCharsPerSec));
                    // clamp to reasonable bounds
                    RepeatIntervalMs = Math.Max(30, Math.Min(400, interval));
                    try { AppLogger.LogInfo($"Keyboard settings loaded: KeyboardDelay[{DelayIndex}:{InitialRepeatDelayMs}ms], KeyboardSpeed[{SpeedValue}:{RepeatIntervalMs}ms interval]"); } catch { }
                }
            }
            catch (Exception ex)
            {
                try { AppLogger.LogError($"KeyboardSettings.LoadFromRegistry failed: {ex}"); } catch { }
            }
        }
    }
}
