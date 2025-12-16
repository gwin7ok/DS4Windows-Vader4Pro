using Microsoft.Win32;
using System;

namespace DS4Windows
{
    // Simple holder that maps Windows keyboard settings to milliseconds used by RepeatHelper
    public static class KeyboardSettings
    {
        // Delay before repeating (ms)
        public static int InitialRepeatDelayMs { get; private set; } = 500;

        // Interval between repeated keypresses (ms)
        public static int RepeatIntervalMs { get; private set; } = 50;

        // Try to read HKCU:\Control Panel\Keyboard KeyboardDelay and KeyboardSpeed
        public static void LoadFromRegistry()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey("Control Panel\\Keyboard"))
                {
                    if (key == null) return;

                    object delayVal = key.GetValue("KeyboardDelay");
                    object speedVal = key.GetValue("KeyboardSpeed");

                    int delay = 1; // default index
                    int speed = 31; // default fastest
                    try { if (delayVal != null) delay = Convert.ToInt32(delayVal); } catch { }
                    try { if (speedVal != null) speed = Convert.ToInt32(speedVal); } catch { }

                    // Map delay index (0..3) -> ms. Use 250ms steps starting at 250ms
                    int dIdx = Math.Max(0, Math.Min(3, delay));
                    InitialRepeatDelayMs = 250 * (dIdx + 1);

                    // Map speed (0..31) to chars/sec range [2.5,30] then compute interval
                    int s = Math.Max(0, Math.Min(31, speed));
                    double charsPerSec = 2.5 + (s / 31.0) * (30.0 - 2.5);
                    int interval = (int)Math.Round(1000.0 / Math.Max(1.0, charsPerSec));
                    // clamp to reasonable bounds
                    RepeatIntervalMs = Math.Max(30, Math.Min(400, interval));
                }
            }
            catch { }
        }
    }
}
