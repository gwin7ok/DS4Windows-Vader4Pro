using System;
using System.Collections.Generic;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DS4Control;

namespace DS4Windows.Actions
{
    // Instance-based ActionManager implementation for DI registration.
    public class DefaultActionManager : IManagedActionManager, IInstanceIdentifiable
    {
        public int InstanceId => this.GetHashCode();
        private readonly Dictionary<int, Actions.Action> actionInstances = new Dictionary<int, Actions.Action>();
        private readonly Dictionary<string, ActionEntry> actions = new Dictionary<string, ActionEntry>(StringComparer.OrdinalIgnoreCase);
        private const int ToggleReleaseHoldMsLocal = 200;

        private static ActionEntry GetOrCreateEntryInternal(Dictionary<string, ActionEntry> actionsDict, SpecialAction action)
        {
            if (action == null) return null;
            lock (actionsDict)
            {
                if (!actionsDict.TryGetValue(action.name, out ActionEntry ent) || ent == null)
                {
                    ent = new ActionEntry(action);
                    actionsDict[action.name] = ent;
                }
                return ent;
            }
        }

        public Actions.Action GetActionByIndex(int index)
        {
            try
            {
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

        public Actions.Action GetActionByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            try
            {
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

        public IReadOnlyList<Actions.Action> Actions
        {
            get
            {
                try
                {
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

        public ActionInstanceState GetStateFor(SpecialAction action, int device)
        {
            try
            {
                var ent = GetOrCreateEntryInternal(actions, action);
                if (ent == null) return null;
                if (device < 0 || device >= ent.States.Length) return null;
                return ent.States[device];
            }
            catch { return null; }
        }

        public void ClearPressedOnceForKey(ushort key)
        {
            try
            {
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

        public void ClearAllPressedOnce()
        {
            try
            {
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

        public void ClearAllEntries()
        {
            try
            {
                lock (actions)
                {
                    actions.Clear();
                }
            }
            catch { }
        }

        public void ClearDeviceState(int device)
        {
            try
            {
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

        public void NotifyTriggerEstablished(SpecialAction action, int device, ushort logicalValue, uint nativeValue, bool useScanCode, VirtualKBMBase outputKBMHandler)
        {
            try
            {
                var ent = GetOrCreateEntryInternal(actions, action);
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
                AppLogger.LogTrace($"DefaultActionManager.NotifyTriggerEstablished failed: {ex}");
            }
        }

        public void NotifyTriggerReleased(SpecialAction action, int device, ushort logicalValue, uint nativeValue, bool useScanCode, VirtualKBMBase outputKBMHandler)
        {
            try
            {
                var ent = GetOrCreateEntryInternal(actions, action);
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
                AppLogger.LogTrace($"DefaultActionManager.NotifyTriggerReleased failed: {ex}");
            }
        }

        public bool DispatchTriggerEstablished(SpecialAction action, int device, ushort logicalValue, uint nativeValue, bool useScanCode, VirtualKBMBase outputKBMHandler)
        {
            try
            {
                var ent = GetOrCreateEntryInternal(actions, action);
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
                    ent.ActionImpl.OnTrigger(device, ctx);
                }
                catch { }
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"DefaultActionManager.DispatchTriggerEstablished failed: {ex}");
                return false;
            }
        }

        public bool DispatchTriggerReleased(SpecialAction action, int device, ushort logicalValue, uint nativeValue, bool useScanCode, VirtualKBMBase outputKBMHandler)
        {
            try
            {
                var ent = GetOrCreateEntryInternal(actions, action);
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
                AppLogger.LogTrace($"DefaultActionManager.DispatchTriggerReleased failed: {ex}");
                return false;
            }
        }
    }
}
