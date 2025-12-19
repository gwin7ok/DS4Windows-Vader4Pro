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

    public class ToggleAndPressedOnceTests
    {
        [Fact]
        public void Toggle_OnTrigger_Toggles_PressedOnce_State()
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
                dam.ClearAllPressedOnce();
                dam.ClearAllEntries();

                // First trigger should set PressedOnce true
                ka.OnTrigger(0, 0, 0, false, handler);
                var stateAfter = ActionManager.GetStateFor(sa, 0);
                Assert.True(stateAfter.PressedOnce);

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
                Assert.False(stateAfterOff.PressedOnce);
            }
            finally
            {
                DS4Windows.DI.ServiceProviderHolder.SetProvider(null);
            }
        }

        [Fact]
        public void Press_OnTrigger_DoesNot_Set_PressedOnce()
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
                dam.ClearAllPressedOnce();
                dam.ClearAllEntries();

                ka.OnTrigger(0, 0, 0, false, handler);
                var stateAfter = ActionManager.GetStateFor(sa, 0);
                Assert.False(stateAfter.PressedOnce);
            }
            finally
            {
                DS4Windows.DI.ServiceProviderHolder.SetProvider(null);
            }
        }
    }
}
