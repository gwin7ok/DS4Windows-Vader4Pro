using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        // Button state table: device x controlIndex
        private bool[,] buttonStates = null;
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

        // Preallocate ActionEntry objects for all registered SpecialActions and initialize button state table.
        public void PreallocateEntries()
        {
            try
            {
                // Initialize ActionEntry for every SpecialAction
                lock (actions)
                {
                    foreach (var sa in ActionRegistry.AllActions())
                    {
                        try { GetOrCreateEntryInternal(actions, sa); } catch { }
                    }
                }

                // Initialize button state table for all devices × supported control indices
                try
                {
                    int devices = Global.MAX_DS4_CONTROLLER_COUNT;
                    int controls = (int)DS4ControlSettings.LAST_DS4_ACTION + 1;
                    buttonStates = new bool[devices, controls];
                    AppLogger.LogTrace($"DefaultActionManager.PreallocateEntries: allocated buttonStates {devices}x{controls} and {actions.Count} action entries");
                }
                catch { }
            }
            catch { }
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
                                for (int d = 0; d < ent.States.Length; d++) ActionManager.SetPressedOnce(ent.ActionDef, d, false);
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
                            for (int d = 0; d < ent.States.Length; d++) ActionManager.SetPressedOnce(ent.ActionDef, d, false);
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
                                try
                                {
                                    bool old = ent.States[device]?.PressedOnce ?? false;
                                    ent.States[device] = new ActionInstanceState();
                                    try { AppLogger.LogTrace($"DefaultActionManager.ClearDeviceState: reset ActionInstanceState for action={(ent?.ActionDef?.name ?? "(null)")} device={device} (PressedOnce cleared)"); } catch { }
                                    if (old != false)
                                    {
                                        try { ActionManager.FirePressedOnceChanged(ent.ActionDef, device, old, false); } catch { }
                                    }
                                }
                                catch { }
                            }
                        }
                        catch { }
                    }
                }

                try { Mapping.ClearKeyButtonControllersForDevice(device); } catch { }
            }
            catch { }
        }

        public void SetPressedOnce(SpecialAction action, int device, bool value)
        {
            try
            {
                var ent = GetOrCreateEntryInternal(actions, action);
                if (ent == null) return;
                if (device < 0 || device >= ent.States.Length) return;
                var st = ent.States[device];
                if (st == null) return;
                bool old = st.PressedOnce;
                if (old == value) return;
                st.PressedOnce = value;
                try { ActionManager.FirePressedOnceChanged(action, device, old, value); } catch { }

                try
                {
                    var trace = new StackTrace(1, false);
                    string callerInfo = "(unknown)";
                    try
                    {
                        for (int i = 0; i < trace.FrameCount; i++)
                        {
                            var fr = trace.GetFrame(i);
                            var method = fr.GetMethod();
                            if (method == null) continue;
                            var declaring = method.DeclaringType;
                            if (declaring == null) continue;
                            if (declaring == typeof(DefaultActionManager)) continue;
                            callerInfo = declaring.FullName + "." + method.Name;
                            break;
                        }
                    }
                    catch { }
                    string stackSnippet = trace.ToString();
                    AppLogger.LogTrace($"DefaultActionManager.SetPressedOnce: action={(action?.name ?? "(null)")} device={device} old={old} new={value} caller={callerInfo} stack={stackSnippet}");
                }
                catch { }
            }
            catch { }
        }

        // Button state accessors: device in [0, MAX_DS4_CONTROLLER_COUNT), controlIndex as DS4Controls enum value
        public bool GetButtonState(int device, int controlIndex)
        {
            try
            {
                if (buttonStates == null) return false;
                if (device < 0 || device >= buttonStates.GetLength(0)) return false;
                if (controlIndex < 0 || controlIndex >= buttonStates.GetLength(1)) return false;
                return buttonStates[device, controlIndex];
            }
            catch { return false; }
        }

        public void SetButtonState(int device, int controlIndex, bool value)
        {
            try
            {
                if (buttonStates == null) return;
                if (device < 0 || device >= buttonStates.GetLength(0)) return;
                if (controlIndex < 0 || controlIndex >= buttonStates.GetLength(1)) return;
                buttonStates[device, controlIndex] = value;
            }
            catch { }
        }

        // NotifyTriggerEstablished removed; use DispatchTriggerEstablished or DispatchTriggerEdge instead.

        // NotifyTriggerReleased removed; use DispatchTriggerReleased or DispatchTriggerEdge instead.

        public bool DispatchTriggerEstablished(SpecialAction action, int device, ushort logicalValue, uint nativeValue, bool useScanCode, VirtualKBMBase outputKBMHandler)
        {
            try
            {
                var ent = GetOrCreateEntryInternal(actions, action);
                if (ent?.ActionImpl == null) return false;
                try
                {
                    var st = GetStateFor(action, device);
                    try { AppLogger.LogTrace($"DefaultActionManager.DispatchTriggerEstablished: before OnTrigger BeingTriggered={st?.BeingTriggered ?? false} name={action?.name} device={device}"); } catch { }
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
                    try { AppLogger.LogTrace($"DefaultActionManager.DispatchTriggerEstablished: after OnTrigger BeingTriggered={st?.BeingTriggered ?? false} name={action?.name} device={device}"); } catch { }
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
