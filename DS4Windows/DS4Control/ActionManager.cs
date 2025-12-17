using System;
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
        // Whether the action is considered 'done' (previously stored in Mapping.actionDone[index].dev[device])
        public bool ActionDone = false;

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
                                for (int d = 0; d < ent.States.Length; d++) ent.States[d].PressedOnce = false;
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
                            for (int d = 0; d < ent.States.Length; d++) ent.States[d].PressedOnce = false;
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
                            }
                        }
                        catch { }
                    }
                }

                try { Mapping.ClearKeyButtonControllersForDevice(device); } catch { }
            }
            catch { }
        }

        // Notify ActionImpl and let the Action handle controller delegation / state changes.
        public static void NotifyTriggerEstablished(SpecialAction action, int device, ushort logicalValue, uint nativeValue, bool useScanCode, VirtualKBMBase outputKBMHandler)
        {
            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                    if (mgr != null) { mgr.NotifyTriggerEstablished(action, device, logicalValue, nativeValue, useScanCode, outputKBMHandler); return; }
                }

                var ent = GetOrCreateEntry(action);
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
                    ent?.ActionImpl?.OnTrigger(device, ctx);
                }
                catch { }
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"ActionManager.NotifyTriggerEstablished failed: {ex}");
            }
        }

        public static void NotifyTriggerReleased(SpecialAction action, int device, ushort logicalValue, uint nativeValue, bool useScanCode, VirtualKBMBase outputKBMHandler)
        {
            try
            {
                var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                if (sp != null)
                {
                    var mgr = sp.GetService(typeof(DS4Windows.Actions.IManagedActionManager)) as DS4Windows.Actions.IManagedActionManager;
                    if (mgr != null) { mgr.NotifyTriggerReleased(action, device, logicalValue, nativeValue, useScanCode, outputKBMHandler); return; }
                }

                var ent = GetOrCreateEntry(action);
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
                    ent?.ActionImpl?.OnRelease(device, ctx);
                }
                catch { }
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"ActionManager.NotifyTriggerReleased failed: {ex}");
            }
        }
    }
}
