using System;
using System.Collections.Generic;
using DS4Windows.DS4Control;

namespace DS4Windows
{
    // Minimal SyntheticDispatcher: centralized small wrapper for synthetic sends.
    // Responsibilities (minimal): resolve native key, perform press/release via KBM handler,
    // update basic timing counters on Mapping.deviceState / Mapping.globalState, and provide ResetKeyTiming.
    public static class SyntheticDispatcher
    {
        public static uint ResolveNativeKey(ushort kvpKey)
        {
            try
            {
                if (Global.outputKBMMapping != null)
                    return Global.outputKBMMapping.GetRealEventKey(kvpKey);
            }
            catch { }
            return kvpKey;
        }

        public static void SendPress(int device, ushort kvpKey, uint nativeKey, bool useScanCode, VirtualKBMBase handler = null)
        {
            try
            {
                var h = handler ?? Global.outputKBMHandler;
                if (h == null) return;

                AppLogger.LogTrace($"SYNTHETIC TRACE dispatcher device={device} key={kvpKey} nativeKey={nativeKey} event=KeyPress");
                AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] dispatcher device={device} key={kvpKey} nativeKey={nativeKey} event=KeyPress");

                if (useScanCode) h.PerformKeyPressAlt(nativeKey); else h.PerformKeyPress(nativeKey);

                // Update simple timing counters to help throttle logic elsewhere
                try
                {
                    var now = DateTime.UtcNow.Ticks;
                    if (Mapping.deviceState != null && device >= 0 && device < Mapping.deviceState.Length)
                    {
                        var ds = Mapping.deviceState[device];
                        if (ds.keyPresses == null) ds.keyPresses = new Dictionary<UInt16, Mapping.SyntheticState.KeyPresses>();
                        if (!ds.keyPresses.TryGetValue(kvpKey, out var kp))
                        {
                            kp = new Mapping.SyntheticState.KeyPresses();
                            ds.keyPresses[kvpKey] = kp;
                            try { ds.nativeKeyAlias[kvpKey] = nativeKey; } catch { }
                        }
                        kp.current.lastSyntheticSendUtcTicks = now;
                        kp.current.repeatCount++;
                    }

                    if (Mapping.globalState != null)
                    {
                        if (Mapping.globalState.keyPresses == null) Mapping.globalState.keyPresses = new Dictionary<UInt16, Mapping.SyntheticState.KeyPresses>();
                        if (!Mapping.globalState.keyPresses.TryGetValue(kvpKey, out var gkp))
                        {
                            gkp = new Mapping.SyntheticState.KeyPresses();
                            Mapping.globalState.keyPresses[kvpKey] = gkp;
                        }
                        gkp.current.lastSyntheticSendUtcTicks = DateTime.UtcNow.Ticks;
                        gkp.current.repeatCount++;
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"SyntheticDispatcher.SendPress failed: {ex}");
            }
        }

        public static void SendRelease(int device, ushort kvpKey, uint nativeKey, bool useScanCode, VirtualKBMBase handler = null)
        {
            try
            {
                var h = handler ?? Global.outputKBMHandler;
                if (h == null) return;

                AppLogger.LogTrace($"SYNTHETIC TRACE dispatcher device={device} key={kvpKey} nativeKey={nativeKey} event=KeyRelease");
                AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] dispatcher device={device} key={kvpKey} nativeKey={nativeKey} event=KeyRelease");

                if (useScanCode) h.PerformKeyReleaseAlt(nativeKey); else h.PerformKeyRelease(nativeKey);

                try
                {
                    // Mark last send time and clear counts to avoid duplicates
                    var now = DateTime.UtcNow.Ticks;
                    if (Mapping.deviceState != null && device >= 0 && device < Mapping.deviceState.Length)
                    {
                        var ds = Mapping.deviceState[device];
                        if (ds.keyPresses != null && ds.keyPresses.TryGetValue(kvpKey, out var kp))
                        {
                            kp.current.lastSyntheticSendUtcTicks = now;
                            kp.current.vkCount = 0;
                            kp.current.scanCodeCount = 0;
                            kp.current.repeatCount = 0;
                            kp.current.toggleCount = 0;
                            kp.current.toggle = false;
                        }
                    }

                    if (Mapping.globalState != null && Mapping.globalState.keyPresses != null && Mapping.globalState.keyPresses.TryGetValue(kvpKey, out var gkp))
                    {
                        gkp.current.lastSyntheticSendUtcTicks = now;
                        gkp.current.vkCount = 0;
                        gkp.current.scanCodeCount = 0;
                        gkp.current.repeatCount = 0;
                        gkp.current.toggleCount = 0;
                        gkp.current.toggle = false;
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"SyntheticDispatcher.SendRelease failed: {ex}");
            }
        }

        public static void ResetKeyTiming(int device, ushort kvpKey)
        {
            try
            {
                var now = DateTime.UtcNow.Ticks;
                if (Mapping.deviceState != null && device >= 0 && device < Mapping.deviceState.Length)
                {
                    var ds = Mapping.deviceState[device];
                    if (ds.keyPresses != null && ds.keyPresses.TryGetValue(kvpKey, out var kp))
                    {
                        kp.current.lastSyntheticSendUtcTicks = 0;
                        kp.current.repeatCount = 0;
                    }
                }
                if (Mapping.globalState != null && Mapping.globalState.keyPresses != null && Mapping.globalState.keyPresses.TryGetValue(kvpKey, out var gkp))
                {
                    gkp.current.lastSyntheticSendUtcTicks = 0;
                    gkp.current.repeatCount = 0;
                }
            }
            catch { }
        }
    }
}
