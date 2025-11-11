using System;
using System.Collections.Generic;
using System.Threading;
using DS4Windows;

class Program
{
    static int Main()
    {
        try
        {
            var received = new List<string>();
            AppLogger.GuiLog += (s, e) => { received.Add(e.Data); Console.WriteLine("GUI LOG: " + e.Data); };

            // Ensure clean suppression
            Global.loggedInvalidActions.Clear();

            // Prepare test: make sure there's a missing action name on device 0
            if (Global.store.profileActions == null || Global.store.profileActions.Length == 0)
            {
                Console.WriteLine("Global.store.profileActions not initialized");
                return 2;
            }

            // Add a unique missing action name
            const string missingName = "ZZZ_MISSING_ACTION_TEST_UNIT_20251112";
            var list = Global.store.profileActions[0];
            if (!list.Contains(missingName)) list.Add(missingName);

            // Ensure actions list does not contain it (so GetAction returns null)
            bool exists = false;
            foreach (var a in Global.store.actions)
            {
                if (a.name == missingName) { exists = true; break; }
            }
            if (exists)
            {
                Console.WriteLine("Test cannot run because action name already exists in actions list");
                return 3;
            }

            // 1) Emit once (should produce a log)
            Global.loggedInvalidActions.Clear();
            received.Clear();
            Global.store.EmitMissingActionLogsForDevice(0, false);
            Thread.Sleep(100);
            if (received.Count == 0)
            {
                Console.WriteLine("FAILED: No log emitted on first call");
                return 4;
            }

            // 2) Call again without forceEmit -> should NOT emit
            received.Clear();
            Global.store.EmitMissingActionLogsForDevice(0, false);
            Thread.Sleep(100);
            if (received.Count != 0)
            {
                Console.WriteLine("FAILED: Duplicate log emitted when suppression should prevent it");
                return 5;
            }

            // 3) Call with forceEmit -> should emit again
            received.Clear();
            Global.store.EmitMissingActionLogsForDevice(0, true);
            Thread.Sleep(100);
            if (received.Count == 0)
            {
                Console.WriteLine("FAILED: No log emitted when forceEmit=true");
                return 6;
            }

            Console.WriteLine("All checks passed");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception during test: " + ex);
            return 1;
        }
    }
}
