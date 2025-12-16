using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DS4Windows.DS4Control;

namespace DS4Windows
{
    // Lightweight per-device wrapper that delegates to existing static controllers
    // Mode is fixed at construction time (Press or Toggle).
    public class KeyButtonActionController
    {
        public enum Mode { Press, Toggle }

        private readonly int device;
        private readonly Mode mode;
        private readonly string assignedActionName;
        private readonly IKeyController impl;

        public KeyButtonActionController(int device, Mode mode, string actionName = "<unknown>")
        {
            this.device = device;
            this.mode = mode;
            this.assignedActionName = actionName ?? "<null>";
            try
            {
                AppLogger.LogTrace($"KeyButtonActionController created: device={device} mode={mode} assignedAction={this.assignedActionName}");
            }
            catch { }

            // Create concrete implementation based on mode. These implementations are lightweight wrappers
            // around the existing static controllers for now; this allows per-device instance semantics later.
            if (mode == Mode.Toggle)
                impl = new ToggleImpl(device);
            else
                impl = new PressImpl(device);

        }

        // New constructor: determine mode from SpecialAction info (KeyButtonSwitchMode or keyType flags)
        public KeyButtonActionController(int device, SpecialAction sa, string actionName = "<unknown>")
        {
            this.device = device;
            this.assignedActionName = actionName ?? (sa?.name ?? "<null>");
            try
            {
                // Decide mode: explicit switch mode first, fall back to keyType Toggle flag
                if (sa != null && sa.KeyButtonSwitchMode.HasValue)
                    this.mode = sa.KeyButtonSwitchMode.Value == SpecialAction.KeyButtonSwitchModeEnum.Toggle ? Mode.Toggle : Mode.Press;
                else
                {
                    bool isToggle = false;
                    try { isToggle = sa != null && sa.keyType.HasFlag(DS4KeyType.Toggle); } catch { }
                    this.mode = isToggle ? Mode.Toggle : Mode.Press;
                }
                AppLogger.LogTrace($"KeyButtonActionController created: device={device} mode={this.mode} assignedAction={this.assignedActionName}");
            }
            catch { }

            if (this.mode == Mode.Toggle)
                impl = new ToggleImpl(device);
            else
                impl = new PressImpl(device);

        }

        // Internal interface for per-mode controllers
        private interface IKeyController
        {
            void OnDown(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler, bool isSpecialAction);
            void OnUp(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler);
            void Clear(ushort kvpKey);
        }

        // Toggle implementation: per-instance management of toggle state and repeat using RepeatHelper
        private class ToggleImpl : IKeyController
        {
            private readonly int device;
            private readonly Dictionary<ushort, RepeatHelper> repeaters = new Dictionary<ushort, RepeatHelper>();
            private readonly HashSet<ushort> states = new HashSet<ushort>();
            public ToggleImpl(int device) { this.device = device; }

            public void OnDown(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler, bool isSpecialAction)
            {
                if (!states.Contains(kvpKey))
                {
                    // Toggle on: record state, send initial press and start repeating immediately
                    states.Add(kvpKey);
                    try { SyntheticDispatcher.SendPress(device, kvpKey, nativeKey, useScanCode, handler); } catch { }
                    try
                    {
                        var rep = new RepeatHelper(device, kvpKey, nativeKey == 0 ? SyntheticDispatcher.ResolveNativeKey(kvpKey) : nativeKey, useScanCode, handler);
                        repeaters[kvpKey] = rep;
                    }
                    catch { }
                }
            }

            public void OnUp(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler)
            {
                if (states.Contains(kvpKey))
                {
                    // Stop repeating and send single release
                    try
                    {
                        if (repeaters.TryGetValue(kvpKey, out RepeatHelper rep))
                        {
                            rep.Stop();
                            repeaters.Remove(kvpKey);
                        }
                        else
                        {
                            SyntheticDispatcher.SendRelease(device, kvpKey, nativeKey, useScanCode, handler);
                        }
                    }
                    catch { }
                    states.Remove(kvpKey);
                }
            }

            public void Clear(ushort kvpKey)
            {
                if (states.Contains(kvpKey)) states.Remove(kvpKey);
                try
                {
                    if (repeaters.TryGetValue(kvpKey, out RepeatHelper rep))
                    {
                        rep.Stop();
                        repeaters.Remove(kvpKey);
                    }
                }
                catch { }
                try { SyntheticDispatcher.ResetKeyTiming(0, kvpKey); } catch { }
            }
        }

        // Press implementation: per-instance management of press state and optional repeat (minimal)
        private class PressImpl : IKeyController
        {
            private readonly int device;
            private class Entry
            {
                public bool isPressed;
                public uint nativeKey;
                public bool useScanCode;
                public DS4Windows.DS4Control.VirtualKBMBase handler;
                public RepeatHelper repeater;
                public CancellationTokenSource delayCts;
            }
            private readonly Dictionary<ushort, Entry> entries = new Dictionary<ushort, Entry>();

            public PressImpl(int device) { this.device = device; }

            public void OnDown(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler, bool isSpecialAction)
            {
                if (!entries.TryGetValue(kvpKey, out Entry e))
                {
                    e = new Entry() { isPressed = false, nativeKey = nativeKey, useScanCode = useScanCode, handler = handler };
                    entries[kvpKey] = e;
                }
                if (!e.isPressed)
                {
                    if (e.nativeKey == 0) e.nativeKey = SyntheticDispatcher.ResolveNativeKey(kvpKey);
                    e.isPressed = true;
                    try { SyntheticDispatcher.SendPress(device, kvpKey, e.nativeKey, e.useScanCode, e.handler); } catch { }

                    // Start delayed creation of repeater after 100ms for press-repeat semantics
                    try
                    {
                        e.delayCts = new CancellationTokenSource();
                        var localEntry = e;
                        Task.Run(async () =>
                        {
                            try
                            {
                                await Task.Delay(100, localEntry.delayCts.Token).ConfigureAwait(false);
                                if (localEntry.delayCts.IsCancellationRequested) return;
                                // create repeater that starts immediate repeating at 50ms
                                localEntry.repeater = new DS4Windows.DS4Control.RepeatHelper(device, kvpKey, localEntry.nativeKey, localEntry.useScanCode, localEntry.handler);
                            }
                            catch (OperationCanceledException) { }
                            catch { }
                        });
                    }
                    catch { }
                }
            }

            public void OnUp(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler)
            {
                if (entries.TryGetValue(kvpKey, out Entry e) && e.isPressed)
                {
                    try
                    {
                        // cancel pending delayed repeater creation
                        try { e.delayCts?.Cancel(); } catch { }
                        // stop repeater if running; Stop() sends single release
                        if (e.repeater != null)
                        {
                            e.repeater.Stop();
                            e.repeater = null;
                        }
                        else
                        {
                            SyntheticDispatcher.SendRelease(device, kvpKey, e.nativeKey != 0 ? e.nativeKey : nativeKey, useScanCode, handler);
                        }
                    }
                    catch { }
                    entries.Remove(kvpKey);
                }
                else
                {
                    try { SyntheticDispatcher.ResetKeyTiming(device, kvpKey); } catch { }
                }
            }

            public void Clear(ushort kvpKey)
            {
                if (entries.TryGetValue(kvpKey, out Entry e))
                {
                    try { e.delayCts?.Cancel(); } catch { }
                    try { e.repeater?.Stop(); } catch { }
                    entries.Remove(kvpKey);
                }
                try { SyntheticDispatcher.ResetKeyTiming(0, kvpKey); } catch { }
            }
        }

        // New, clearer API names reflecting Mapping-trigger notifications
        public void OnSATriggerEstablished(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler, bool isSpecialAction)
        {
            impl.OnDown(kvpKey, nativeKey, useScanCode, handler, isSpecialAction);
        }

        public void OnSATriggerReleased(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler)
        {
            impl.OnUp(kvpKey, nativeKey, useScanCode, handler);
        }

        // (Removed backward-compatible wrappers to avoid accidental use of old API names.)

        public void ClearKeyEntries(ushort kvpKey)
        {
            impl.Clear(kvpKey);
        }
    }
}
