using System;
using System.Collections.Generic;
using DS4Windows.DS4Control;

namespace DS4Windows
{
    // Per-action per-device instance state
    public class ActionInstanceState
    {
        public bool PressedOnce = false;
        public long LastToggleTimeUtcTicks = 0;
    }

    internal class ActionEntry
    {
        public SpecialAction ActionDef;
        public KeyAction ActionImpl;
        public ActionInstanceState[] States;

        public ActionEntry(SpecialAction action)
        {
            ActionDef = action;
            ActionImpl = new KeyAction(action, -1);
            States = new ActionInstanceState[Global.MAX_DS4_CONTROLLER_COUNT];
            for (int i = 0; i < States.Length; ++i) States[i] = new ActionInstanceState();
        }
    }

    // Manages Action instances and provides access to per-device state.
    public static class ActionManager
    {
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

        // Return the per-action per-device state object. Creates entry if needed.
        public static ActionInstanceState GetStateFor(SpecialAction action, int device)
        {
            try
            {
                var ent = GetOrCreateEntry(action);
                if (ent == null) return null;
                if (device < 0 || device >= ent.States.Length) return null;
                return ent.States[device];
            }
            catch { return null; }
        }

        // Notify ActionImpl and let the Action handle controller delegation / state changes.
        public static void NotifyTriggerEstablished(SpecialAction action, int device, ushort logicalValue, uint nativeValue, bool useScanCode, VirtualKBMBase outputKBMHandler)
        {
            try
            {
                var ent = GetOrCreateEntry(action);
                ent?.ActionImpl?.OnTrigger(device, logicalValue, nativeValue, useScanCode);
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
                var ent = GetOrCreateEntry(action);
                ent?.ActionImpl?.OnRelease(device, logicalValue, nativeValue, useScanCode);
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"ActionManager.NotifyTriggerReleased failed: {ex}");
            }
        }
    }
}
