using System;
using System.Collections.Generic;

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

        // Toggle implementation: per-instance management of toggle state and repeat (minimal)
        private class ToggleImpl : IKeyController
        {
            private readonly int device;
            private readonly Dictionary<ushort, bool> states = new Dictionary<ushort, bool>();
            public ToggleImpl(int device) { this.device = device; }

            public void OnDown(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler, bool isSpecialAction)
            {
                if (!states.TryGetValue(kvpKey, out bool isOn) || !isOn)
                {
                    // Toggle on
                    states[kvpKey] = true;
                    try { SyntheticDispatcher.SendPress(device, kvpKey, nativeKey, useScanCode, handler); } catch { }
                }
            }

            public void OnUp(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler)
            {
                if (states.TryGetValue(kvpKey, out bool isOn) && isOn)
                {
                    try { SyntheticDispatcher.SendRelease(device, kvpKey, nativeKey, useScanCode, handler); } catch { }
                    states[kvpKey] = false;
                }
            }

            public void Clear(ushort kvpKey)
            {
                if (states.ContainsKey(kvpKey)) states.Remove(kvpKey);
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
                }
            }

            public void OnUp(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler)
            {
                if (entries.TryGetValue(kvpKey, out Entry e) && e.isPressed)
                {
                    try { SyntheticDispatcher.SendRelease(device, kvpKey, e.nativeKey != 0 ? e.nativeKey : nativeKey, useScanCode, handler); } catch { }
                    entries.Remove(kvpKey);
                }
                else
                {
                    try { SyntheticDispatcher.ResetKeyTiming(device, kvpKey); } catch { }
                }
            }

            public void Clear(ushort kvpKey)
            {
                if (entries.ContainsKey(kvpKey)) entries.Remove(kvpKey);
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
