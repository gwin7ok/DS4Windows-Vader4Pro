using System;
using System.Collections.Generic;
using DS4Windows.DS4Control;

namespace DS4Windows
{
    // Minimal PressActionController: manage Press-mode lifecycle and optional repeat.
    // OnTriggerOn -> send press immediately; OnTriggerOff -> send release.
    // Update() handles optional repeat when enableRepeat==true.
    public static class PressActionController
    {
        private class Entry
        {
            public int device;
            public ushort kvpKey;
            public uint nativeKey;
            public bool useScanCode;
            public VirtualKBMBase handler;
            public bool isPressed;
            public long firstPressUtcTicks;
            public long lastRepeatUtcTicks;
            public bool enableRepeat;
        }

        private static readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

        // timings (same defaults as Toggle controller initially)
        private const int InitialDelayMs = 100;
        private const int RepeatIntervalMs = 25;

        private static string MakeKey(int device, ushort kvpKey) => device + ":" + kvpKey;

        public static void OnTriggerOn(int device, ushort kvpKey, uint nativeKey, bool useScanCode, VirtualKBMBase handler, bool enableRepeat = false)
        {
            var key = MakeKey(device, kvpKey);
            var now = DateTime.UtcNow.Ticks;
            if (!entries.TryGetValue(key, out Entry e))
            {
                e = new Entry() { device = device, kvpKey = kvpKey, nativeKey = nativeKey, useScanCode = useScanCode, handler = handler, enableRepeat = enableRepeat };
                entries[key] = e;
            }
            if (!e.isPressed)
            {
                // resolve native if needed
                if (e.nativeKey == 0) e.nativeKey = SyntheticDispatcher.ResolveNativeKey(kvpKey);
                e.isPressed = true;
                e.firstPressUtcTicks = now;
                e.lastRepeatUtcTicks = now;
                try
                {
                    SyntheticDispatcher.SendPress(device, kvpKey, e.nativeKey, useScanCode, handler);
                }
                catch (Exception ex)
                {
                    AppLogger.LogTrace($"PressActionController.SendPress failed: {ex}");
                }
            }
            e.enableRepeat = enableRepeat;
        }

        public static void OnTriggerOff(int device, ushort kvpKey, uint nativeKey, bool useScanCode, VirtualKBMBase handler)
        {
            var key = MakeKey(device, kvpKey);
            if (entries.TryGetValue(key, out Entry e))
            {
                if (e.isPressed)
                {
                    try
                    {
                        SyntheticDispatcher.SendRelease(device, kvpKey, e.nativeKey != 0 ? e.nativeKey : nativeKey, useScanCode, handler);
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogTrace($"PressActionController.SendRelease failed: {ex}");
                    }
                }
                entries.Remove(key);
            }
            else
            {
                // Ensure any lingering timing is reset
                try { SyntheticDispatcher.ResetKeyTiming(device, kvpKey); } catch { }
            }
        }

        public static void Update()
        {
            if (entries.Count == 0) return;
            var now = DateTime.UtcNow.Ticks;
            var toRepeat = new List<Entry>();
            foreach (var kv in entries)
            {
                var e = kv.Value;
                if (!e.isPressed) continue;
                if (!e.enableRepeat) continue;
                long sinceFirstMs = (now - e.firstPressUtcTicks) / TimeSpan.TicksPerMillisecond;
                long sinceLastMs = (now - e.lastRepeatUtcTicks) / TimeSpan.TicksPerMillisecond;
                if (sinceFirstMs >= InitialDelayMs && sinceLastMs >= RepeatIntervalMs)
                    toRepeat.Add(e);
            }
            foreach (var e in toRepeat)
            {
                try
                {
                    SyntheticDispatcher.SendPress(e.device, e.kvpKey, e.nativeKey, e.useScanCode, e.handler);
                    e.lastRepeatUtcTicks = DateTime.UtcNow.Ticks;
                }
                catch (Exception ex)
                {
                    AppLogger.LogTrace($"PressActionController repeat failed: {ex}");
                }
            }
        }

        public static void ClearKeyEntries(ushort kvpKey)
        {
            var remove = new List<string>();
            foreach (var kv in entries)
            {
                if (kv.Value.kvpKey == kvpKey) remove.Add(kv.Key);
            }
            foreach (var k in remove) entries.Remove(k);
            // Reset timing in global/device states as well
            try { SyntheticDispatcher.ResetKeyTiming(0, kvpKey); } catch { }
        }
    }
}
