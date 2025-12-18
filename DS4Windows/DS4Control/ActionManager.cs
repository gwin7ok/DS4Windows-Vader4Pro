using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using DS4Windows.DS4Control;
using DS4Windows.Actions;

namespace DS4Windows
{
    // Per-action per-device instance state
    public class ActionInstanceState
    {
        public bool PressedOnce = false;
        public long LastToggleTimeUtcTicks = 0;
        public bool FirstTouch = false;
        // Whether the action is currently considered 'being triggered' (replaces legacy actionDone)
        // Exposed as a read-only property to prevent direct assignment from other code.
        private bool _beingTriggered = false;
        public bool BeingTriggered { get { return _beingTriggered; } }

        // Internal setter used by ActionManager to mutate the state in a controlled way.
        internal void SetBeingTriggeredInternal(bool value)
        {
            _beingTriggered = value;
        }

        // Index of an expected untrigger action for this state (-1 if none). Mirrors Mapping.untriggerindex[device] semantics
        public int UntriggerIndex = -1;

        // Bitmask for one-shot flags (GyroCalibrate, BatteryCheck, etc.) so we can record multiple one-shot states per action/device.
        // Interpretation of bits is up to higher-level managers; using uint for compactness.
        public uint OneShotFlags = 0u;
    }

    internal class ActionEntry
    {
        public SpecialAction ActionDef;
        public Actions.Action ActionImpl;
        public ActionInstanceState[] States;

        public ActionEntry(SpecialAction action)
        {
            ActionDef = action;
            ActionImpl = ActionFactory.CreateFrom(action, -1);
            States = new ActionInstanceState[Global.MAX_DS4_CONTROLLER_COUNT];
            for (int i = 0; i < States.Length; ++i) States[i] = new ActionInstanceState();
            try
            {
                AppLogger.LogTrace($"ActionEntry created: action={(action?.name ?? "(null)")} - initialized ActionInstanceState (PressedOnce cleared)");
            }
            catch { }
        }
    }

    // Manages Action instances and provides access to per-device state.
    public static class ActionManager
    {
        // Cache of created Action instances by SpecialAction index (lazy-created via ActionFactory)
        private static readonly Dictionary<int, Actions.Action> actionInstances = new Dictionary<int, Actions.Action>();
        private static readonly Dictionary<string, ActionEntry> actions = new Dictionary<string, ActionEntry>(StringComparer.OrdinalIgnoreCase);
        private const int ToggleReleaseHoldMsLocal = 200;

        private static ActionEntry GetOrCreateEntry(SpecialAction action)
        {
            if (action == null) return null;
            lock (actions)
            {
                if (!actions.TryGetValue(action.name, out ActionEntry ent) || ent == null)
                {
                    ent = new ActionEntry(action);
                    actions[action.name] = ent;
                }
                return ent;
            }
        }

        // Return an Action instance for given SpecialAction index (lazy-created via ActionFactory)
        public static Actions.Action GetActionByIndex(int index)
        {
            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                    if (mgr != null) return mgr.GetActionByIndex(index);
                }

                var sa = ActionRegistry.GetByIndex(index);
                if (sa == null) return null;
                lock (actions)
                {
                    if (!actionInstances.TryGetValue(index, out Actions.Action act) || act == null)
                    {
                        act = ActionFactory.CreateFrom(sa, index);
                        actionInstances[index] = act;
                    }
                    return act;
                }
            }
            catch { return null; }
        }

        public static Actions.Action GetActionByName(string name)
        {
            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                    if (mgr != null) return mgr.GetActionByName(name);
                }

                if (string.IsNullOrEmpty(name)) return null;

                // Find index from registry
                int idx = -1;
                int i = 0;
                foreach (var sa in ActionRegistry.AllActions())
                {
                    if (sa != null && string.Equals(sa.name, name, StringComparison.OrdinalIgnoreCase)) { idx = i; break; }
                    i++;
                }
                if (idx == -1) return null;
                return GetActionByIndex(idx);
            }
            catch { return null; }
        }

        public static IReadOnlyList<Actions.Action> Actions
        {
            get
            {
                try
                {
                    var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                    if (sp != null)
                    {
                        var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                        if (mgr != null) return mgr.Actions;
                    }

                    var list = new List<Actions.Action>();
                    int count = ActionRegistry.Count;
                    for (int i = 0; i < count; ++i)
                    {
                        var a = GetActionByIndex(i);
                        list.Add(a);
                    }
                    return list.AsReadOnly();
                }
                catch { return Array.Empty<Actions.Action>(); }
            }
        }

        // Return the per-action per-device state object. Creates entry if needed.
        public static ActionInstanceState GetStateFor(SpecialAction action, int device)
        {
            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                    if (mgr != null) return mgr.GetStateFor(action, device);
                }

                var ent = GetOrCreateEntry(action);
                if (ent == null) return null;
                if (device < 0 || device >= ent.States.Length) return null;
                return ent.States[device];
            }
            catch { return null; }
        }

        // Query whether given action/device is currently marked as being triggered.
        public static bool IsBeingTriggered(SpecialAction action, int device)
        {
            try
            {
                var st = GetStateFor(action, device);
                if (st != null) return st.BeingTriggered;
            }
            catch { }
            return false;
        }

        // Controlled setter for BeingTriggered — centralizes mutations so callers don't assign the field directly.
        // This API is private to prevent external callers from mutating BeingTriggered; use DispatchTriggerEdge instead.
        private static void SetBeingTriggeredFor(SpecialAction action, int device, bool value)
        {
            try
            {
                var st = GetStateFor(action, device);
                if (st == null) return;
                bool old = st.BeingTriggered;
                if (old == value) return;
                st.SetBeingTriggeredInternal(value);
                try { AppLogger.LogTrace($"ActionManager.SetBeingTriggeredFor: name={action?.name} device={device} old={old} new={value}"); } catch { }
            }
            catch { }
        }

        // Clear pressed-once flag for actions that map to given native key
        public static void ClearPressedOnceForKey(ushort key)
        {
            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                    if (mgr != null) { mgr.ClearPressedOnceForKey(key); return; }
                }

                lock (actions)
                {
                    foreach (var ent in actions.Values)
                    {
                        try
                        {
                            if (ent?.ActionDef == null) continue;
                            if (ushort.TryParse(ent.ActionDef.details, out ushort k) && k == key)
                            {
                                for (int d = 0; d < ent.States.Length; d++) SetPressedOnce(ent.ActionDef, d, false);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        // Clear pressed-once flags for all known actions
        public static void ClearAllPressedOnce()
        {
            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                    if (mgr != null) { mgr.ClearAllPressedOnce(); return; }
                }

                lock (actions)
                {
                    foreach (var ent in actions.Values)
                    {
                        try
                        {
                            if (ent?.States == null) continue;
                            for (int d = 0; d < ent.States.Length; d++) SetPressedOnce(ent.ActionDef, d, false);
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        // Clear all registered Action entries. Used during profile reload / actions reparse.
        public static void ClearAllEntries()
        {
            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                    if (mgr != null) { mgr.ClearAllEntries(); return; }
                }

                lock (actions)
                {
                    actions.Clear();
                }
            }
            catch { }
        }

        // Clear per-device ActionEntry state and controllers
        public static void ClearDeviceState(int device)
        {
            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                    if (mgr != null) { mgr.ClearDeviceState(device); return; }
                }

                lock (actions)
                {
                    foreach (var ent in actions.Values)
                    {
                        try
                        {
                            if (ent?.States == null) continue;
                            if (device >= 0 && device < ent.States.Length)
                            {
                                ent.States[device] = new ActionInstanceState();
                                try { AppLogger.LogTrace($"ActionManager.ClearDeviceState: reset ActionInstanceState for action={(ent?.ActionDef?.name ?? "(null)")} device={device} (PressedOnce cleared)"); } catch { }
                            }
                        }
                        catch { }
                    }
                }

                try { Mapping.HandleDeviceDisconnect(device); } catch { }
            }
            catch { }
        }

        // NOTE: NotifyTriggerEstablished removed — use DispatchTriggerEstablished or DispatchTriggerEdge

        // Event fired when PressedOnce state changes for an action/device.
        // Parameters: (SpecialAction action, int device, bool oldValue, bool newValue)
        public static event Action<SpecialAction, int, bool, bool> PressedOnceChanged;

            // Ensure the PressedOnceChanged event is always traced when fired.
            static ActionManager()
            {
                try
                {
                    PressedOnceChanged += (sa, dev, oldv, newv) =>
                    {
                        try { AppLogger.LogTrace($"ActionManager.PressedOnceChanged: name={sa?.name} device={dev} old={oldv} new={newv}"); } catch { }
                    };
                }
                catch { }
            }

            // Helper for external components (such as DI-managed managers) to notify the static event.
            public static void FirePressedOnceChanged(SpecialAction action, int device, bool oldValue, bool newValue)
            {
                try
                {
                    try { PressedOnceChanged?.Invoke(action, device, oldValue, newValue); } catch { }
                }
                catch { }
            }

        // Helper to set PressedOnce with change notification.
        public static void SetPressedOnce(SpecialAction action, int device, bool value)
        {
            // Prefer DI-managed implementation and fail loudly if none present.
            var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
            if (sp != null)
            {
                var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                if (mgr != null)
                {
                    mgr.SetPressedOnce(action, device, value);
                    return;
                }
            }

            // No DI manager available -> explicit failure to avoid silent state divergence
            var msg = $"ActionManager.SetPressedOnce called but no IManagedActionManager is registered. action={(action?.name ?? "(null)")} device={device} value={value}";
            try { AppLogger.LogError(msg); } catch { }
            throw new InvalidOperationException(msg);
        }

        // Dispatch that returns true if an Action instance existed and was invoked to handle the trigger.
        public static bool DispatchTriggerEstablished(SpecialAction action, int device, ushort logicalValue, uint nativeValue, bool useScanCode, VirtualKBMBase outputKBMHandler)
        {
            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                    if (mgr != null) return mgr.DispatchTriggerEstablished(action, device, logicalValue, nativeValue, useScanCode, outputKBMHandler);
                }

                var ent = GetOrCreateEntry(action);
                if (ent?.ActionImpl == null) return false;
                try
                {
                    var st = GetStateFor(action, device);
                    try { AppLogger.LogTrace($"DispatchTriggerEstablished: before OnTrigger BeingTriggered={st?.BeingTriggered ?? false} name={action?.name} device={device}"); } catch { }
                    var ctx = new DS4Windows.Actions.MappingContext
                    {
                        LogicalValue = logicalValue,
                        NativeValue = nativeValue,
                        UseScanCode = useScanCode,
                        OutputHandler = outputKBMHandler,
                        ActionDef = action,
                        Index = -1
                    };
                    ent.ActionImpl.OnTrigger(device, ctx);
                    try { AppLogger.LogTrace($"DispatchTriggerEstablished: after OnTrigger BeingTriggered={st?.BeingTriggered ?? false} name={action?.name} device={device}"); } catch { }
                }
                catch (Exception ex) { AppLogger.LogTrace($"DispatchTriggerEstablished handler failed: {ex}"); }
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"ActionManager.DispatchTriggerEstablished failed: {ex}");
                return false;
            }
        }

        // NOTE: NotifyTriggerReleased removed — use DispatchTriggerReleased or DispatchTriggerEdge

        public static bool DispatchTriggerReleased(SpecialAction action, int device, ushort logicalValue, uint nativeValue, bool useScanCode, VirtualKBMBase outputKBMHandler)
        {
            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                    if (mgr != null) return mgr.DispatchTriggerReleased(action, device, logicalValue, nativeValue, useScanCode, outputKBMHandler);
                }

                var ent = GetOrCreateEntry(action);
                if (ent?.ActionImpl == null) return false;
                try
                {
                    var ctx = new DS4Windows.Actions.MappingContext
                    {
                        LogicalValue = logicalValue,
                        NativeValue = nativeValue,
                        UseScanCode = useScanCode,
                        OutputHandler = outputKBMHandler,
                        ActionDef = action,
                        Index = -1
                    };
                    ent.ActionImpl.OnRelease(device, ctx);
                }
                catch { }
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"ActionManager.DispatchTriggerReleased failed: {ex}");
                return false;
            }
        }

        // Generic dispatch entry that accepts a TriggerContext and routes to established/released handlers.
        public static bool DispatchTrigger(DS4Windows.TriggerContext ctx)
        {
            try
            {
                if (ctx == null || ctx.ActionDef == null) return false;
                if (ctx.IsEstablished)
                {
                    return DispatchTriggerEstablished(ctx.ActionDef, ctx.Device, ctx.LogicalValue, ctx.NativeValue, ctx.UseScanCode, ctx.OutputHandler);
                }
                else
                {
                    return DispatchTriggerReleased(ctx.ActionDef, ctx.Device, ctx.LogicalValue, ctx.NativeValue, ctx.UseScanCode, ctx.OutputHandler);
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"ActionManager.DispatchTrigger failed: {ex}");
                return false;
            }
        }

        // Edge-only dispatch: notify only when a trigger first becomes established
        // and when it later becomes released. This uses the per-action BeingTriggered
        // flag (via ActionInstanceState) to gate repeated notifications.
        public static bool DispatchTriggerEdge(DS4Windows.TriggerContext ctx)
        {
            try
            {
                if (ctx == null || ctx.ActionDef == null) return false;

                var st = GetStateFor(ctx.ActionDef, ctx.Device);
                // If we cannot access state, fall back to full dispatch behavior and log once.
                if (st == null)
                {
                    try { AppLogger.LogTrace($"DispatchTriggerEdge: no ActionInstanceState for name={ctx.ActionDef?.name} device={ctx.Device}; falling back to full dispatch"); } catch { }
                    return DispatchTrigger(ctx);
                }

                if (ctx.IsEstablished)
                {
                    // Only log when we transition from not-being-triggered -> being-triggered
                    if (!st.BeingTriggered)
                    {
                        st.SetBeingTriggeredInternal(true);
                        try { AppLogger.LogTrace($"DispatchTriggerEdge: firing established (edge) for name={ctx.ActionDef?.name} device={ctx.Device} (now BeingTriggered={st.BeingTriggered})"); } catch { }
                        return DispatchTriggerEstablished(ctx.ActionDef, ctx.Device, ctx.LogicalValue, ctx.NativeValue, ctx.UseScanCode, ctx.OutputHandler);
                    }
                    // suppressed duplicate established
                    return false;
                }
                else
                {
                    // Only log when we transition from being-triggered -> not-being-triggered
                    if (st.BeingTriggered)
                    {
                        st.SetBeingTriggeredInternal(false);
                        try { AppLogger.LogTrace($"DispatchTriggerEdge: firing released (edge) for name={ctx.ActionDef?.name} device={ctx.Device}"); } catch { }
                        return DispatchTriggerReleased(ctx.ActionDef, ctx.Device, ctx.LogicalValue, ctx.NativeValue, ctx.UseScanCode, ctx.OutputHandler);
                    }
                    // suppressed duplicate release
                    return false;
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"ActionManager.DispatchTriggerEdge failed: {ex}");
                return false;
            }
        }

        // Abstraction: allow Actions to obtain controllers via ActionManager so implementations
        // do not directly depend on Mapping internals. Default implementation delegates to Mapping.
        public static KeyButtonActionController GetOrCreateControllerForAction(int device, SpecialAction action)
        {
            try
            {
                // If DI provider exposes a managed action manager, let it provide controllers.
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                    // IManagedActionManager may be extended later to provide controller factory; for now fall back.
                }

                // Default behavior: delegate to Mapping helper.
                return Mapping.GetOrCreateKeyButtonControllerForAction(device, action);
            }
            catch { return null; }
        }
    }
}
