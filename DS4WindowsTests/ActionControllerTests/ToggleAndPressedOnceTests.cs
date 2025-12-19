using System;
using DS4Windows.Actions;
using DS4Windows.DS4Control;
using Xunit;

namespace DS4Windows.Actions.Tests
{
    // Lightweight test ServiceProvider that returns a single object for requested service types.
    class TestServiceProvider : IServiceProvider
    {
        private readonly object svc;
        public TestServiceProvider(object svc) { this.svc = svc; }
        public object GetService(Type serviceType)
        {
            if (svc != null && serviceType.IsInstanceOfType(svc)) return svc;
            return null;
        }
    }

    // Minimal fake KBM handler that reports fakeKeyRepeat=true and no-ops on key events.
    class FakeKBMHandler : VirtualKBMBase
    {
        public FakeKBMHandler()
        {
            // Use false so controllers create internal RepeatHelper paths
            fakeKeyRepeat = false;
        }

        public override bool Connect() => true;
        public override bool Disconnect() => true;
        public override void MoveRelativeMouse(int x, int y) { }
        public override void MoveAbsoluteMouse(double x, double y) { }
        public override void PerformMouseWheelEvent(int vertical, int horizontal) { }
        public override void PerformMouseButtonEvent(uint mouseButton) { }
        public override void PerformMouseButtonPress(uint mouseButton) { }
        public override void PerformMouseButtonRelease(uint mouseButton) { }
        public override void PerformKeyPress(uint key) { }
        public override void PerformKeyPressAlt(uint key) { }
        public override void PerformKeyRelease(uint key) { }
        public override void PerformKeyReleaseAlt(uint key) { }
        public override string GetDisplayName() => "FakeKBM";
        public override string GetIdentifier() => "FakeKBM";
        public override string GetFullDisplayName() => "FakeKBM";
    }

    public class ToggleAndToggledOnTests
    {
        [Fact]
        public void Toggle_OnTrigger_Toggles_IsToggledOn_State()
        {
            var dam = new DS4Windows.Actions.DefaultActionManager();
            var srv = new TestServiceProvider(dam);
            DS4Windows.DI.ServiceProviderHolder.SetProvider(srv);

            try
            {
                // create a SpecialAction configured as Toggle
                var sa = new DS4Windows.SpecialAction("toggletest", "0", "Key", "0");
                sa.KeyButtonSwitchMode = DS4Windows.SpecialAction.KeyButtonSwitchModeEnum.Toggle;

                var ka = new DS4Windows.KeyAction(sa, 0);
                var handler = new FakeKBMHandler();

                // Ensure a clean starting state
                dam.ClearAllToggledOn();
                dam.ClearAllEntries();

                // First trigger should set IsToggledOn true
                ka.OnTrigger(0, 0, 0, false, handler);
                var stateAfter = ActionManager.GetStateFor(sa, 0);
                Assert.True(stateAfter.IsToggledOn);

                // Simulate explicit Toggle-OFF by invoking controller's OnSATriggerToggleOff
                var mappingType = typeof(DS4Windows.Mapping);
                var dictField = mappingType.GetField("keyButtonControllers", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                var dict = dictField.GetValue(null) as System.Collections.IDictionary;
                // Find controller instance matching action name (sa.name) and invoke ToggleOff
                if (dict != null)
                {
                    // As a last resort in tests, invoke ToggleOff on every controller to ensure any active toggle is cleared.
                    foreach (System.Collections.DictionaryEntry de in dict)
                    {
                        var inst = de.Value as DS4Windows.KeyButtonActionController;
                        try { inst?.OnSATriggerToggleOff(0, 0, false, handler); } catch { }
                    }
                }

                var stateAfterOff = ActionManager.GetStateFor(sa, 0);
                Assert.False(stateAfterOff.IsToggledOn);
            }
            finally
            {
                DS4Windows.DI.ServiceProviderHolder.SetProvider(null);
            }
        }

        [Fact]
        public void Press_OnTrigger_DoesNot_Set_IsToggledOn()
        {
            var dam = new DS4Windows.Actions.DefaultActionManager();
            var srv = new TestServiceProvider(dam);
            DS4Windows.DI.ServiceProviderHolder.SetProvider(srv);

            try
            {
                var sa = new DS4Windows.SpecialAction("presstest", "0", "Key", "0");
                // ensure Press mode
                sa.KeyButtonSwitchMode = DS4Windows.SpecialAction.KeyButtonSwitchModeEnum.Press;

                var ka = new DS4Windows.KeyAction(sa, 0);
                var handler = new FakeKBMHandler();

                // Ensure a clean starting state
                dam.ClearAllToggledOn();
                dam.ClearAllEntries();

                ka.OnTrigger(0, 0, 0, false, handler);
                var stateAfter = ActionManager.GetStateFor(sa, 0);
                Assert.False(stateAfter.IsToggledOn);
            }
            finally
            {
                DS4Windows.DI.ServiceProviderHolder.SetProvider(null);
            }
        }

        [Fact]
        public void IsToggledOn_SingleWriter_EventOnlyFromManager()
        {
            var dam = new DS4Windows.Actions.DefaultActionManager();
            var srv = new TestServiceProvider(dam);
            DS4Windows.DI.ServiceProviderHolder.SetProvider(srv);

            try
            {
                var sa = new DS4Windows.SpecialAction("invarianttest", "0", "Key", "0");
                sa.KeyButtonSwitchMode = DS4Windows.SpecialAction.KeyButtonSwitchModeEnum.Toggle;

                dam.ClearAllToggledOn();
                dam.ClearAllEntries();

                bool eventFired = false;
                Action<DS4Windows.SpecialAction, int, bool, bool> handler = (a, d, oldv, newv) => { eventFired = true; };
                try { DS4Windows.ActionManager.ToggledOnChanged += handler; } catch { }

                // Directly mutate state: should NOT fire ToggledOnChanged event
                var st = DS4Windows.ActionManager.GetStateFor(sa, 0);
                Assert.NotNull(st);
                st.IsToggledOn = true;
                Assert.True(st.IsToggledOn);
                Assert.False(eventFired);

                // Now use manager API to change: this SHOULD fire the event
                DS4Windows.ActionManager.SetToggledOn(sa, 0, false);
                Assert.False(st.IsToggledOn);
                Assert.True(eventFired);

                try { DS4Windows.ActionManager.ToggledOnChanged -= handler; } catch { }
            }
            finally
            {
                DS4Windows.DI.ServiceProviderHolder.SetProvider(null);
            }
        }

        [Fact]
        public void DeterministicRepeater_TriggerViaFake()
        {
            var dam = new DS4Windows.Actions.DefaultActionManager();
            var srv = new TestServiceProvider(dam);
            DS4Windows.DI.ServiceProviderHolder.SetProvider(srv);

            // Install override to create FakeDeterministicRepeater for any adapter constructions
            DS4Windows.Actions.RepeatHelperToIRepeaterAdapter.RepeaterFactoryOverride = (origFactory) => new FakeDeterministicRepeater();

            try
            {
                // Create a SpecialAction configured as Toggle to drive controller path
                var sa = new DS4Windows.SpecialAction("repeattest", "0", "Key", "0");
                sa.KeyButtonSwitchMode = DS4Windows.SpecialAction.KeyButtonSwitchModeEnum.Toggle;
                var ka = new DS4Windows.KeyAction(sa, 0);
                var handler = new FakeKBMHandler();

                dam.ClearAllToggledOn();
                dam.ClearAllEntries();

                // Trigger action: controller should create adapter that wraps FakeDeterministicRepeater
                ka.OnTrigger(0, 0, 0, false, handler);

                // Inspect mapping controllers and invoke TriggerOnce on any fake repeater found
                var mappingType = typeof(DS4Windows.Mapping);
                var dictField = mappingType.GetField("keyButtonControllers", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                var dict = dictField.GetValue(null) as System.Collections.IDictionary;
                if (dict != null)
                {
                    foreach (System.Collections.DictionaryEntry de in dict)
                    {
                        var inst = de.Value as DS4Windows.KeyButtonActionController;
                        if (inst == null) continue;
                        // attempt to access internal entries via reflection to find the IRepeater and trigger it
                        var implField = typeof(DS4Windows.KeyButtonActionController).GetField("impl", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        var impl = implField?.GetValue(inst);
                        if (impl == null) continue;
                        var toggleImplType = impl.GetType();
                        var entriesField = toggleImplType.GetField("entries", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        var entries = entriesField?.GetValue(impl) as System.Collections.IDictionary;
                        if (entries == null) continue;
                        foreach (System.Collections.DictionaryEntry e in entries)
                        {
                            var entry = e.Value;
                            var repField = entry.GetType().GetField("repeater", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                            var rep = repField?.GetValue(entry) as FakeDeterministicRepeater;
                            try { rep?.TriggerOnce(); } catch { }
                        }
                    }
                }
            }
            finally
            {
                DS4Windows.Actions.RepeatHelperToIRepeaterAdapter.RepeaterFactoryOverride = null;
                DS4Windows.DI.ServiceProviderHolder.SetProvider(null);
            }
        }

        [Fact]
        public void KeyButtonActionControllerAdapter_StartStop_Toggle_Clears_IsToggledOn()
        {
            var dam = new DS4Windows.Actions.DefaultActionManager();
            var srv = new TestServiceProvider(dam);
            DS4Windows.DI.ServiceProviderHolder.SetProvider(srv);

            try
            {
                var sa = new DS4Windows.SpecialAction("adaptertoggle", "0", "Key", "0");
                sa.KeyButtonSwitchMode = DS4Windows.SpecialAction.KeyButtonSwitchModeEnum.Toggle;

                dam.ClearAllToggledOn();
                dam.ClearAllEntries();

                var handler = new FakeKBMHandler();
                var adapter = new DS4Windows.Actions.KeyButtonActionControllerAdapter(0, sa);
                var binding = new DS4Windows.Actions.KeyActionBinding(sa);
                var trigger = new DS4Windows.Actions.TriggerContextImpl
                {
                    Device = 0,
                    IsEdgeEstablished = true,
                    LogicalValue = 0,
                    NativeValue = 0,
                    OutputHandler = handler,
                    Timestamp = DateTime.UtcNow
                };

                // Start should set IsToggledOn
                adapter.Start(binding, trigger);
                var st = ActionManager.GetStateFor(sa, 0);
                Assert.NotNull(st);
                Assert.True(st.IsToggledOn);

                // Stop should clear IsToggledOn via ToggleOff path
                adapter.Stop(binding, trigger);
                Assert.False(st.IsToggledOn);
            }
            finally
            {
                DS4Windows.DI.ServiceProviderHolder.SetProvider(null);
            }
        }

        [Fact]
        public void KeyButtonActionControllerAdapter_Handle_Release_DoesNot_Clear_Toggle()
        {
            var dam = new DS4Windows.Actions.DefaultActionManager();
            var srv = new TestServiceProvider(dam);
            DS4Windows.DI.ServiceProviderHolder.SetProvider(srv);

            try
            {
                var sa = new DS4Windows.SpecialAction("adaptertoggle_handle", "0", "Key", "0");
                sa.KeyButtonSwitchMode = DS4Windows.SpecialAction.KeyButtonSwitchModeEnum.Toggle;

                dam.ClearAllToggledOn();
                dam.ClearAllEntries();

                var handler = new FakeKBMHandler();
                var adapter = new DS4Windows.Actions.KeyButtonActionControllerAdapter(0, sa);
                var binding = new DS4Windows.Actions.KeyActionBinding(sa);
                var triggerEst = new DS4Windows.Actions.TriggerContextImpl
                {
                    Device = 0,
                    IsEdgeEstablished = true,
                    LogicalValue = 0,
                    NativeValue = 0,
                    OutputHandler = handler,
                    Timestamp = DateTime.UtcNow
                };

                // Start should set IsToggledOn
                adapter.Start(binding, triggerEst);
                var st = ActionManager.GetStateFor(sa, 0);
                Assert.NotNull(st);
                Assert.True(st.IsToggledOn);

                // Now call Handle with a release edge — toggle-mode controllers should ignore release
                var triggerRel = new DS4Windows.Actions.TriggerContextImpl
                {
                    Device = 0,
                    IsEdgeEstablished = false,
                    LogicalValue = 0,
                    NativeValue = 0,
                    OutputHandler = handler,
                    Timestamp = DateTime.UtcNow
                };

                adapter.Handle(binding, triggerRel);

                // Toggled state should remain true after release handled
                Assert.True(st.IsToggledOn);
            }
            finally
            {
                DS4Windows.DI.ServiceProviderHolder.SetProvider(null);
            }
        }
    }
}
