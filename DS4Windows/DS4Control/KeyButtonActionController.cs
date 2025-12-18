using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DS4Windows.DS4Control;
using DS4Windows.Actions;

namespace DS4Windows
{
    // Lightweight per-device wrapper that delegates to existing static controllers
    // Mode is fixed at construction time (Press or Toggle).
    public class KeyButtonActionController : IDisposable, IInstanceIdentifiable
    {
        public int InstanceId => this.GetHashCode();
        public enum Mode { Press, Toggle }

        private readonly int device;
        private readonly Mode mode;
        private readonly string assignedActionName;
        private readonly SpecialAction assignedActionDef;
        private readonly IKeyController impl;
        private readonly Action<SpecialAction, int, bool, bool> pressedOnceHandler;

        // Expose assigned action name for diagnostics
        public string AssignedActionName => assignedActionName;

        public KeyButtonActionController(int device, Mode mode, string actionName = "<unknown>")
        {
            this.device = device;
            this.mode = mode;
            this.assignedActionName = actionName ?? "<null>";
            this.assignedActionDef = null;
            try
            {
                AppLogger.LogTrace($"KeyButtonActionController created: id={this.GetHashCode()} device={device} mode={mode} assignedAction={this.assignedActionName}");
            }
            catch { }

            // Create concrete implementation based on mode. These implementations are lightweight wrappers
            // around the existing static controllers for now; this allows per-device instance semantics later.
            if (mode == Mode.Toggle)
                impl = new ToggleImpl(device, this.GetHashCode(), this.assignedActionDef);
            else
                impl = new PressImpl(device, this.GetHashCode());

            // Subscribe to PressedOnceChanged so controllers react to Action-level state changes.
            pressedOnceHandler = (sa, dev, oldv, newv) =>
            {
                try
                {
                    if (sa == this.assignedActionDef && dev == this.device && oldv == true && newv == false)
                    {
                        try { impl.ClearAll(); } catch { }
                    }
                }
                catch { }
            };
            try { ActionManager.PressedOnceChanged += pressedOnceHandler; } catch { }

        }

        // New constructor: determine mode from SpecialAction info (KeyButtonSwitchMode or keyType flags)
        public KeyButtonActionController(int device, SpecialAction sa, string actionName = "<unknown>")
        {
            this.device = device;
            this.assignedActionName = actionName ?? (sa?.name ?? "<null>");
            this.assignedActionDef = sa;
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
                AppLogger.LogTrace($"KeyButtonActionController created: id={this.GetHashCode()} device={device} mode={this.mode} assignedAction={this.assignedActionName}");
            }
            catch { }

            if (this.mode == Mode.Toggle)
                impl = new ToggleImpl(device, this.GetHashCode(), this.assignedActionDef);
            else
                impl = new PressImpl(device, this.GetHashCode());

            // Subscribe to PressedOnceChanged so controllers react to Action-level state changes.
            pressedOnceHandler = (sa, dev, oldv, newv) =>
            {
                try
                {
                    if (sa == this.assignedActionDef && dev == this.device && oldv == true && newv == false)
                    {
                        try { impl.ClearAll(); } catch { }
                    }
                }
                catch { }
            };
            try { ActionManager.PressedOnceChanged += pressedOnceHandler; } catch { }

        }

        // Internal interface for per-mode controllers
        private interface IKeyController
        {
            void OnDown(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler, bool isSpecialAction);
            void OnUp(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler);
            // Invoked when a Toggle-mode action is explicitly toggled OFF (controller-level stop + release).
            void OnToggleOff(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler);
            void Clear(ushort kvpKey);
            void ClearAll();
        }

        // Toggle implementation: manage per-key repeater and explicit toggle-off behavior
        private class ToggleImpl : IKeyController
        {
            private readonly int device;
            private readonly int controllerId;
            private readonly SpecialAction assignedActionDef;

            private class Entry
            {
                public uint nativeKey;
                public bool useScanCode;
                public DS4Windows.DS4Control.VirtualKBMBase handler;
                public IRepeater repeater;
            }

            private readonly Dictionary<ushort, Entry> entries = new Dictionary<ushort, Entry>();

            public ToggleImpl(int device, int controllerId, SpecialAction assignedActionDef)
            {
                this.device = device;
                this.controllerId = controllerId;
                this.assignedActionDef = assignedActionDef;
            }

            public void OnDown(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler, bool isSpecialAction)
            {
                if (!entries.TryGetValue(kvpKey, out Entry e))
                {
                    e = new Entry() { nativeKey = nativeKey, useScanCode = useScanCode, handler = handler };
                    entries[kvpKey] = e;
                }

                try
                {
                    var st = assignedActionDef != null ? ActionManager.GetStateFor(assignedActionDef, device) : null;
                    bool wasPressed = st?.PressedOnce ?? false;
                    if (!wasPressed)
                    {
                        // If output handler implements its own fake key repeat (SendInput), avoid creating
                        // a second repeater here. Mapping.Commit will generate repeats for fakeKeyRepeat handlers.
                        if (e.handler != null && e.handler.fakeKeyRepeat)
                        {
                            if (e.nativeKey == 0) e.nativeKey = SyntheticDispatcher.ResolveNativeKey(kvpKey);
                            try { SyntheticDispatcher.SendPress(device, kvpKey, e.nativeKey, e.useScanCode, e.handler); } catch { }
                        }
                        else
                        {
                                if (e.repeater == null)
                                {
                                    if (e.nativeKey == 0) e.nativeKey = SyntheticDispatcher.ResolveNativeKey(kvpKey);
                                    e.repeater = new RepeatHelperToIRepeaterAdapter(() => new DS4Windows.DS4Control.RepeatHelper(device, kvpKey, e.nativeKey, e.useScanCode, e.handler, DS4Windows.KeyboardSettings.RepeatIntervalMs, true, controllerId));
                                    try { AppLogger.LogTrace($"ToggleImpl.OnDown: controller-repeater link controllerId={controllerId} kvpKey={kvpKey} device={device}"); } catch { }
                                    try { e.repeater.Start(TimeSpan.Zero, TimeSpan.FromMilliseconds(DS4Windows.KeyboardSettings.RepeatIntervalMs), null); } catch { }
                                }
                                else
                                {
                                    try { e.repeater.Start(TimeSpan.Zero, TimeSpan.FromMilliseconds(DS4Windows.KeyboardSettings.RepeatIntervalMs), null); } catch { }
                                }
                        }

                        try { if (assignedActionDef != null) ActionManager.SetPressedOnce(assignedActionDef, device, true); } catch { }
                    }
                    else
                    {
                        // already pressedOnce: do nothing on additional OnDown
                    }
                }
                catch { }
            }

            public void OnUp(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler)
            {
                // For Toggle mode, input-edge release should not stop repeaters or clear PressedOnce.
                // Ignore input-level release here; rely on explicit toggle-off path.
                try { SyntheticDispatcher.ResetKeyTiming(device, kvpKey); } catch { }
            }

            public void OnToggleOff(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler)
            {
                if (entries.TryGetValue(kvpKey, out Entry e) && e.repeater != null)
                {
                    try
                    {
                        try { e.repeater.Stop(); } catch { }
                        try { AppLogger.LogTrace($"ToggleImpl.OnToggleOff: controllerId={controllerId} kvpKey={kvpKey} device={device} repeater stopped"); } catch { }
                        try
                        {
                            // Ensure PressedOnce is cleared by the same component that set it true.
                            try { if (assignedActionDef != null) ActionManager.SetPressedOnce(assignedActionDef, device, false); } catch { }
                            if (e.nativeKey == 0) e.nativeKey = SyntheticDispatcher.ResolveNativeKey(kvpKey);
                            SyntheticDispatcher.SendRelease(device, kvpKey, e.nativeKey, e.useScanCode, e.handler);
                        }
                        catch { }
                    }
                    catch { }
                }
                else
                {
                    try { SyntheticDispatcher.ResetKeyTiming(0, kvpKey); } catch { }
                }
            }

            public void Clear(ushort kvpKey)
            {
                if (entries.TryGetValue(kvpKey, out Entry e))
                {
                    try { e.repeater?.Stop(); } catch { }
                    try
                    {
                        if (e.nativeKey == 0) e.nativeKey = SyntheticDispatcher.ResolveNativeKey(kvpKey);
                        SyntheticDispatcher.SendRelease(device, kvpKey, e.nativeKey, e.useScanCode, e.handler);
                    }
                    catch { }
                    // keep entry for reuse; do not dispose here
                }
                try { SyntheticDispatcher.ResetKeyTiming(0, kvpKey); } catch { }
            }

            public void ClearAll()
            {
                try
                {
                    var keys = new List<ushort>(entries.Keys);
                    foreach (var k in keys)
                    {
                        try
                        {
                            var e = entries[k];
                            try
                            {
                                if (e.nativeKey == 0) e.nativeKey = SyntheticDispatcher.ResolveNativeKey(k);
                                SyntheticDispatcher.SendRelease(device, k, e.nativeKey, e.useScanCode, e.handler);
                            }
                            catch { }
                            try { e.repeater?.Dispose(); } catch { }
                            entries.Remove(k);
                            try { SyntheticDispatcher.ResetKeyTiming(0, k); } catch { }
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }

        // Press implementation: per-instance management of press state and optional repeat (minimal)
        private class PressImpl : IKeyController
        {
            private readonly int device;
            private readonly int controllerId;
            private class Entry
            {
                public bool isPressed;
                public uint nativeKey;
                public bool useScanCode;
                public DS4Windows.DS4Control.VirtualKBMBase handler;
                public IRepeater repeater;
                public CancellationTokenSource delayCts;
            }
            private readonly Dictionary<ushort, Entry> entries = new Dictionary<ushort, Entry>();

            public PressImpl(int device, int controllerId) { this.device = device; this.controllerId = controllerId; }

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
                    try
                    {
                        AppLogger.LogTrace($"PressImpl.OnDown: controllerId={controllerId} device={device} kvpKey={kvpKey} native={e.nativeKey} useScan={e.useScanCode}");
                        // Primary behavior: send one immediate press (hold). Release will be sent on OnUp.
                        SyntheticDispatcher.SendPress(device, kvpKey, e.nativeKey, e.useScanCode, e.handler);
                    }
                    catch (Exception ex) { AppLogger.LogTrace($"PressImpl.OnDown failed: {ex}"); }

                    // Start delayed creation of repeater after InitialRepeatDelayMs for press-repeat semantics
                    try
                    {
                        e.delayCts = new CancellationTokenSource();
                        var localEntry = e;
                        Task.Run(async () =>
                        {
                                try
                                {
                                    await Task.Delay(DS4Windows.KeyboardSettings.InitialRepeatDelayMs, localEntry.delayCts.Token).ConfigureAwait(false);
                                    if (localEntry.delayCts.IsCancellationRequested) return;
                                    // create or start repeater that begins immediate repeating (send first immediate press)
                                    // If handler provides fakeKeyRepeat (e.g., SendInput), Mapping will perform repeats.
                                    if (localEntry.handler == null || !localEntry.handler.fakeKeyRepeat)
                                    {
                                        if (localEntry.repeater == null)
                                        {
                                            localEntry.repeater = new RepeatHelperToIRepeaterAdapter(() => new DS4Windows.DS4Control.RepeatHelper(device, kvpKey, localEntry.nativeKey, localEntry.useScanCode, localEntry.handler, DS4Windows.KeyboardSettings.RepeatIntervalMs, true, controllerId));
                                            try { localEntry.repeater.Start(TimeSpan.Zero, TimeSpan.FromMilliseconds(DS4Windows.KeyboardSettings.RepeatIntervalMs), null); } catch { }
                                        }
                                        else
                                        {
                                            try { localEntry.repeater.Start(TimeSpan.Zero, TimeSpan.FromMilliseconds(DS4Windows.KeyboardSettings.RepeatIntervalMs), null); } catch { }
                                        }
                                    }
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
                        // stop repeater if running; Stop() sends single release. Keep instance for reuse.
                        if (e.repeater != null)
                        {
                            try { e.repeater.Stop(); } catch { }
                        }
                        // Ensure release is sent now for the held press
                        try { SyntheticDispatcher.SendRelease(device, kvpKey, e.nativeKey, e.useScanCode, e.handler); } catch { }
                        e.isPressed = false;
                    }
                    catch { }
                }
                else
                {
                    try { SyntheticDispatcher.ResetKeyTiming(device, kvpKey); } catch { }
                }
            }

            public void OnToggleOff(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler)
            {
                // For press-mode controller, treat toggle-off as a clear of any press state.
                try { Clear(kvpKey); } catch { }
            }

            public void Clear(ushort kvpKey)
            {
                if (entries.TryGetValue(kvpKey, out Entry e))
                {
                    try { e.delayCts?.Cancel(); } catch { }
                    try { e.repeater?.Stop(); } catch { }
                    try { if (e.isPressed) SyntheticDispatcher.SendRelease(device, kvpKey, e.nativeKey, e.useScanCode, e.handler); } catch { }
                    e.isPressed = false;
                    // keep entry for reuse; do not dispose here
                }
                try { SyntheticDispatcher.ResetKeyTiming(0, kvpKey); } catch { }
            }
                public void ClearAll()
                {
                    try
                    {
                        var keys = new List<ushort>(entries.Keys);
                        foreach (var k in keys)
                        {
                            try
                            {
                                var e = entries[k];
                                        try { e.delayCts?.Cancel(); } catch { }
                                        try { if (e.isPressed) SyntheticDispatcher.SendRelease(device, k, e.nativeKey, e.useScanCode, e.handler); } catch { }
                                        try { e.repeater?.Dispose(); } catch { }
                                entries.Remove(k);
                                try { SyntheticDispatcher.ResetKeyTiming(0, k); } catch { }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
        }

        // New, clearer API names reflecting Mapping-trigger notifications
        public void OnSATriggerEstablished(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler, bool isSpecialAction)
        {
            try
            {
                AppLogger.LogTrace($"KBC OnSATriggerEstablished: controllerId={this.GetHashCode()} assignedAction={this.assignedActionName} device={device} kvpKey={kvpKey} isSpecialAction={isSpecialAction}");
            }
            catch { }
            impl.OnDown(kvpKey, nativeKey, useScanCode, handler, isSpecialAction);
        }

        public void OnSATriggerReleased(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler)
        {
            try
            {
                AppLogger.LogTrace($"KBC OnSATriggerReleased: controllerId={this.GetHashCode()} assignedAction={this.assignedActionName} device={device} kvpKey={kvpKey}");
            }
            catch { }
            // For Toggle-mode, ignore input-level release here; actual toggle-off should be handled explicitly.
            impl.OnUp(kvpKey, nativeKey, useScanCode, handler);
        }

        // Explicit API to indicate Toggle OFF (called by KeyAction when it decides to toggle off)
        public void OnSATriggerToggleOff(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler)
        {
            try
            {
                AppLogger.LogTrace($"KBC OnSATriggerToggleOff: controllerId={this.GetHashCode()} assignedAction={this.assignedActionName} device={device} kvpKey={kvpKey}");
            }
            catch { }
            impl.OnToggleOff(kvpKey, nativeKey, useScanCode, handler);
        }

        // (Removed backward-compatible wrappers to avoid accidental use of old API names.)

        public void ClearKeyEntries(ushort kvpKey)
        {
            impl.Clear(kvpKey);
        }

        private bool disposed = false;

        public virtual void Dispose()
        {
            if (disposed) return;
            disposed = true;
            try
            {
                impl.ClearAll();
            }
            catch { }
            try { AppLogger.LogTrace($"KeyButtonActionController destroyed: device={device} assignedAction={this.assignedActionName}"); } catch { }
        }

        public void Destroy()
        {
            try { Dispose(); } catch { }
        }
    }
}
