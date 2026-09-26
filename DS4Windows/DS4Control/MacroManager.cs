using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DS4Windows
{
    // Responsible for macro execution and synchronization.
    // Mapping previously contained ad-hoc PlayMacro/PlayMacroTask logic; this class centralizes that behavior.
    public static class MacroManager
    {
        // Per-device named macro queues (keyed by trigger combination string)
        private static readonly ConcurrentDictionary<int, ConcurrentDictionary<string, Task>> macroQueues = new ConcurrentDictionary<int, ConcurrentDictionary<string, Task>>();

        // Enqueue or run a macro. Synchronization behavior is based on the 'synchronized' flag on SpecialAction.
        public static Task PlayMacro(int device, string macroStr, List<int> macroLst, int[] macroArr, string triggerKey, bool synchronized, SpecialAction action = null)
        {
            var deviceQueues = macroQueues.GetOrAdd(device, _ => new ConcurrentDictionary<string, Task>());
            if (!synchronized || action == null)
            {
                // fire-and-forget
                return Task.Run(() => ExecuteMacroTask(device, macroStr, macroLst, macroArr, action));
            }

            // synchronized: chain onto existing task for this triggerKey
            return deviceQueues.AddOrUpdate(triggerKey,
                _ => Task.Run(() => ExecuteMacroTask(device, macroStr, macroLst, macroArr, action)),
                (_, prev) => prev.ContinueWith(t => ExecuteMacroTask(device, macroStr, macroLst, macroArr, action)).Unwrap());
        }

        private static async Task ExecuteMacroTask(int device, string macroStr, List<int> macroLst, int[] macroArr, SpecialAction action)
        {
            try
            {
                // Minimal placeholder: actual macro parsing/dispatch remains in Mapping.PlayMacroTask for now.
                // This method is the intended place to host macro playback logic when migrating.
                await Task.Yield();
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"MacroManager.ExecuteMacroTask failed: {ex}");
            }
        }
    }
}
