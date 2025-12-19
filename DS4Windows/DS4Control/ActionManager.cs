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
        public bool IsToggledOn = false;
        public long LastToggleTimeUtcTicks = 0;
        public bool FirstTouch = false;
        // Whether a macro iteration is currently executing on this device.
        // This flag indicates a single macro run (one iteration) is in progress.
        // Do not use it to represent the lifetime of a repeat-in-place task.
        public bool IsMacroRunning = false;
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
                AppLogger.LogTrace($"ActionEntry created: action={(action?.name ?? "(null)")} - initialized ActionInstanceState (IsToggledOn cleared)");
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

        // Clear toggled-on flag for actions that map to given native key
        public static void ClearToggledOnForKey(ushort key)
        {
            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                    if (mgr != null) { mgr.ClearToggledOnForKey(key); return; }
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
                                for (int d = 0; d < ent.States.Length; d++) SetToggledOn(ent.ActionDef, d, false);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        // Clear pressed-once flags for all known actions
        public static void ClearAllToggledOn()
        {
            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                    if (mgr != null) { mgr.ClearAllToggledOn(); return; }
                }

                lock (actions)
                {
                    foreach (var ent in actions.Values)
                    {
                        try
                        {
                            if (ent?.States == null) continue;
                            for (int d = 0; d < ent.States.Length; d++) SetToggledOn(ent.ActionDef, d, false);
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
                                try { AppLogger.LogTrace($"ActionManager.ClearDeviceState: reset ActionInstanceState for action={(ent?.ActionDef?.name ?? "(null)")} device={device} (IsToggledOn cleared)"); } catch { }
                            }
                        }
                        catch { }
                    }
                }

                try { Mapping.HandleDeviceDisconnect(device); } catch { }
            }
            catch { }
        }

        // Preallocate runtime instances at startup: create Action instances and per-action states
        // This forces creation of `Actions.Action` and `ActionEntry` objects so first-use latency
        // does not hit the input path.
        public static void PreallocateOnStartup()
        {
            try
            {
                int count = ActionRegistry.Count;
                for (int i = 0; i < count; ++i)
                {
                    try { GetActionByIndex(i); } catch { }
                }

                // Ensure ActionEntry/States are created for all actions and devices.
                try
                {
                    foreach (var sa in ActionRegistry.AllActions())
                    {
                        try { GetStateFor(sa, 0); } catch { }
                    }
                }
                catch { }

                try { AppLogger.LogTrace($"ActionManager.PreallocateOnStartup: preallocated {count} actions"); } catch { }
            }
            catch { }
        }

        // NOTE: NotifyTriggerEstablished removed — use DispatchTriggerEstablished or DispatchTriggerEdge

        // Event fired when toggled-on state changes for an action/device.
        // Parameters: (SpecialAction action, int device, bool oldValue, bool newValue)
        public static event Action<SpecialAction, int, bool, bool> ToggledOnChanged;

            // Ensure the ToggledOnChanged event is always traced when fired.
            static ActionManager()
            {
                try
                {
                    ToggledOnChanged += (sa, dev, oldv, newv) =>
                    {
                        try { AppLogger.LogTrace($"ActionManager.ToggledOnChanged: name={sa?.name} device={dev} old={oldv} new={newv}"); } catch { }
                    };
                }
                catch { }
            }

            // Helper for external components (such as DI-managed managers) to notify the static event.
            public static void FireToggledOnChanged(SpecialAction action, int device, bool oldValue, bool newValue)
            {
                try
                {
                    try { ToggledOnChanged?.Invoke(action, device, oldValue, newValue); } catch { }
                }
                catch { }
            }

        // Helper to set toggled-on flag with change notification.
        public static void SetToggledOn(SpecialAction action, int device, bool value)
        {
            // Prefer DI-managed implementation and fail loudly if none present.
            var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
            if (sp != null)
            {
                var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                if (mgr != null)
                {
                    mgr.SetToggledOn(action, device, value);
                    return;
                }
            }

            // No DI manager available -> explicit failure to avoid silent state divergence
            var msg = $"ActionManager.SetToggledOn called but no IManagedActionManager is registered. action={(action?.name ?? "(null)")} device={device} value={value}";
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
        // This is a simple forwarder used as a fallback when no ActionInstanceState is available.
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

        // Edge-aware dispatcher: accepts a TriggerContext and only fires established/released once per input edge
        // by consulting the per-action ActionInstanceState.BeingTriggered flag. If no state exists, falls back
        // to the simple DispatchTrigger forwarder.
        public static bool DispatchTriggerEdge(DS4Windows.TriggerContext ctx)
        {
            try
            {
                if (ctx == null || ctx.ActionDef == null) return false;

                var st = GetStateFor(ctx.ActionDef, ctx.Device);
                if (st == null)
                {
                    try { AppLogger.LogTrace($"DispatchTriggerEdge: no ActionInstanceState for name={ctx.ActionDef?.name} device={ctx.Device}; falling back to full dispatch"); } catch { }
                    return DispatchTrigger(ctx);
                }

                if (ctx.IsEstablished)
                {
                    // Only fire when transitioning from not-being-triggered -> being-triggered
                    bool shouldFire = false;
                    try
                    {
                        lock (st)
                        {
                            if (!st.BeingTriggered)
                            {
                                st.SetBeingTriggeredInternal(true);
                                shouldFire = true;
                            }
                        }
                    }
                    catch { }

                    if (shouldFire)
                    {
                        try { AppLogger.LogTrace($"DispatchTriggerEdge: firing established (edge) for name={ctx.ActionDef?.name} device={ctx.Device} (now BeingTriggered={st.BeingTriggered})"); } catch { }
                        return DispatchTriggerEstablished(ctx.ActionDef, ctx.Device, ctx.LogicalValue, ctx.NativeValue, ctx.UseScanCode, ctx.OutputHandler);
                    }

                    // suppressed duplicate established
                    return false;
                }
                else
                {
                    // Only fire when transitioning from being-triggered -> not-being-triggered
                    bool shouldFire = false;
                    try
                    {
                        lock (st)
                        {
                            if (st.BeingTriggered)
                            {
                                st.SetBeingTriggeredInternal(false);
                                shouldFire = true;
                            }
                        }
                    }
                    catch { }

                    if (shouldFire)
                    {
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
        public static DS4Windows.Actions.IActionController GetOrCreateControllerForAction(int device, SpecialAction action)
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

        // Preallocate runtime instances for a specific device when a profile is applied to that device.
        // Creates Action instances, per-device ActionInstanceState entries and key/button controllers
        // for the SpecialActions present in the applied profile.
        public static void PreallocateForProfileApply(int device)
        {
            try
            {
                var profileActions = Global.getProfileActions(device);
                if (profileActions == null) return;

                foreach (var actionName in profileActions)
                {
                    try
                    {
                        var sa = Global.GetProfileAction(device, actionName);
                        if (sa == null) continue;

                        // Ensure Action instance exists
                        try { GetActionByName(sa.name); } catch { }

                        // Ensure per-device state exists
                        try { GetStateFor(sa, device); } catch { }

                        // Create key/button controller if needed
                        try { Mapping.GetOrCreateKeyButtonControllerForAction(device, sa); } catch { }
                    }
                    catch { }
                }

                try { AppLogger.LogTrace($"ActionManager.PreallocateForProfileApply: preallocated actions/controllers for device {device}"); } catch { }
            }
            catch { }
        }
    }
}
