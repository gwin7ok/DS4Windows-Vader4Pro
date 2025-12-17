using System;

namespace DS4Windows
{
    // Encapsulates toggle semantics, debounce, and pressed-once handling for Key-type SpecialActions.
    public static class ToggleController
    {
        // Decide whether a toggle should flip, given last toggle timestamp and debounce window.
        public static bool ShouldFlipToggle(long lastToggleTimeUtcTicks, long nowUtcTicks, int debounceMs)
        {
            if (lastToggleTimeUtcTicks == 0) return true;
            long delta = nowUtcTicks - lastToggleTimeUtcTicks;
            return delta > TimeSpan.FromMilliseconds(debounceMs).Ticks;
        }

        // Convenience: flip stored toggle state and set last toggle time.
        public static void ApplyToggle(ref bool toggleState, out long newLastToggleTimeUtcTicks)
        {
            toggleState = !toggleState;
            newLastToggleTimeUtcTicks = DateTime.UtcNow.Ticks;
        }
    }
}
