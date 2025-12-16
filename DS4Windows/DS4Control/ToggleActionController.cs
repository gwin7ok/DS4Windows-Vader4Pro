using System;
using System.Collections.Generic;
using DS4Windows.DS4Control;

namespace DS4Windows
{
    // ToggleActionController: minimal migration target from ToggleRepeatController.
    // Keeps same semantics but delegates actual sends to SyntheticDispatcher.
    public static class ToggleActionController
    {
        private class Entry
        {
            public int device;
            public ushort kvpKey;
            public uint nativeKey;
            public bool useScanCode;
            public long firstPressUtcTicks;
            public long lastRepeatUtcTicks;
            public VirtualKBMBase handler;
        }

        private static readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

        private const int InitialDelayMs = 100;
        private const int RepeatIntervalMs = 25;

        private static string MakeKey(int device, ushort kvpKey) => device + ":" + kvpKey;

        public static void OnToggleOn(int device, ushort kvpKey, uint nativeKey, bool useScanCode, VirtualKBMBase handler)
        {
            if (!IsActive(device))
            {
                AppLogger.LogTrace($"ToggleActionController ignored OnToggleOn device={device} kvpKey={kvpKey} (controller inactive)");
                return;
            }
            var key = MakeKey(device, kvpKey);
            var now = DateTime.UtcNow.Ticks;
            if (!entries.TryGetValue(key, out Entry e))
            {
                e = new Entry() { device = device, kvpKey = kvpKey, nativeKey = nativeKey, useScanCode = useScanCode };
                entries[key] = e;
            }
            e.firstPressUtcTicks = now;
            e.lastRepeatUtcTicks = now;
            e.handler = handler;

            // immediate press via dispatcher
            SyntheticDispatcher.SendPress(device, kvpKey, nativeKey, useScanCode, handler);
        }

        public static void OnToggleOff(int device, ushort kvpKey, uint nativeKey, bool useScanCode, VirtualKBMBase handler)
        {
            if (!IsActive(device))
            {
                AppLogger.LogTrace($"ToggleActionController ignored OnToggleOff device={device} kvpKey={kvpKey} (controller inactive)");
                return;
            }
            var key = MakeKey(device, kvpKey);
            // send single release via dispatcher
            SyntheticDispatcher.SendRelease(device, kvpKey, nativeKey, useScanCode, handler);
            entries.Remove(key);
            // Do not change IsActive here; activation controlled by profile application.
        }

        public static void Update()
        {
            // Note: HasAnyActive check removed per current design; per-device IsActive checks remain.
            if (entries.Count == 0) return;
            var now = DateTime.UtcNow.Ticks;
            var toSend = new List<Entry>();
            foreach (var kv in entries)
            {
                var e = kv.Value;
                if (!IsActive(e.device)) continue;
                long sinceFirstMs = (now - e.firstPressUtcTicks) / TimeSpan.TicksPerMillisecond;
                long sinceLastMs = (now - e.lastRepeatUtcTicks) / TimeSpan.TicksPerMillisecond;
                if (sinceFirstMs >= InitialDelayMs && sinceLastMs >= RepeatIntervalMs)
                    toSend.Add(e);
            }
            foreach (var e in toSend)
            {
                AppLogger.LogTrace($"SYNTHETIC TRACE toggle-repeat device={e.device} kvpKey={e.kvpKey} nativeKey={e.nativeKey} event=KeyPress(repeat)");
                AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] toggle-repeat device={e.device} kvpKey={e.kvpKey} nativeKey={e.nativeKey} event=KeyPress(repeat)");
                SyntheticDispatcher.SendPress(e.device, e.kvpKey, e.nativeKey, e.useScanCode, e.handler);
                e.lastRepeatUtcTicks = DateTime.UtcNow.Ticks;
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
            try { SyntheticDispatcher.ResetKeyTiming(0, kvpKey); } catch { }
            // Do not change IsActive here; activation controlled by profile application.
        }

        private static readonly HashSet<int> activeDevices = new HashSet<int>();

        public static bool IsActive(int device) => activeDevices.Contains(device);

        public static void SetActive(int device, bool active)
        {
            if (active) activeDevices.Add(device); else activeDevices.Remove(device);
        }
    }
}
