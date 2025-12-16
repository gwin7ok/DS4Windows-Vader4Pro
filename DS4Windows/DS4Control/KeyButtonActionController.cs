using System;

namespace DS4Windows
{
    // Lightweight per-device wrapper that delegates to existing static controllers
    // Mode is fixed at construction time (Press or Toggle).
    public class KeyButtonActionController
    {
        public enum Mode { Press, Toggle }

        private readonly int device;
        private readonly Mode mode;
        private readonly string assignedActionName;
        private readonly IKeyController impl;

        public KeyButtonActionController(int device, Mode mode, string actionName = "<unknown>")
        {
            this.device = device;
            this.mode = mode;
            this.assignedActionName = actionName ?? "<null>";
            try
            {
                AppLogger.LogTrace($"KeyButtonActionController created: device={device} mode={mode} assignedAction={this.assignedActionName}");
            }
            catch { }

            // Create concrete implementation based on mode. These implementations are lightweight wrappers
            // around the existing static controllers for now; this allows per-device instance semantics later.
            if (mode == Mode.Toggle)
                impl = new ToggleImpl(device);
            else
                impl = new PressImpl(device);
        }

        // New constructor: determine mode from SpecialAction info (KeyButtonSwitchMode or keyType flags)
        public KeyButtonActionController(int device, SpecialAction sa, string actionName = "<unknown>")
        {
            this.device = device;
            this.assignedActionName = actionName ?? (sa?.name ?? "<null>");
            try
            {
                // Decide mode: explicit switch mode first, fall back to keyType Toggle flag
                if (sa != null && sa.KeyButtonSwitchMode.HasValue)
                    this.mode = sa.KeyButtonSwitchMode.Value == SpecialAction.KeyButtonSwitchModeEnum.Toggle ? Mode.Toggle : Mode.Press;
                else
                {
                    bool isToggle = false;
                    try { isToggle = sa != null && sa.keyType.HasFlag(DS4KeyType.Toggle); } catch { }
                    this.mode = isToggle ? Mode.Toggle : Mode.Press;
                }
                AppLogger.LogTrace($"KeyButtonActionController created: device={device} mode={this.mode} assignedAction={this.assignedActionName}");
            }
            catch { }

            if (this.mode == Mode.Toggle)
                impl = new ToggleImpl(device);
            else
                impl = new PressImpl(device);
        }

        // Internal interface for per-mode controllers
        private interface IKeyController
        {
            void OnDown(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler, bool isSpecialAction);
            void OnUp(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler);
            void Clear(ushort kvpKey);
        }

        // Toggle implementation delegates to existing ToggleActionController
        private class ToggleImpl : IKeyController
        {
            private readonly int device;
            public ToggleImpl(int device) { this.device = device; }
            public void OnDown(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler, bool isSpecialAction)
            {
                DS4Windows.ToggleActionController.OnToggleOn(device, kvpKey, nativeKey, useScanCode, handler);
            }
            public void OnUp(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler)
            {
                DS4Windows.ToggleActionController.OnToggleOff(device, kvpKey, nativeKey, useScanCode, handler);
            }
            public void Clear(ushort kvpKey) { DS4Windows.ToggleActionController.ClearKeyEntries(kvpKey); }
        }

        // Press implementation delegates to existing PressActionController
        private class PressImpl : IKeyController
        {
            private readonly int device;
            public PressImpl(int device) { this.device = device; }
            public void OnDown(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler, bool isSpecialAction)
            {
                try { DS4Windows.ToggleActionController.ClearKeyEntries(kvpKey); } catch { }
                DS4Windows.PressActionController.OnPressDown(device, kvpKey, nativeKey, useScanCode, handler, isSpecialAction);
            }
            public void OnUp(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler)
            {
                DS4Windows.PressActionController.OnPressUp(device, kvpKey, nativeKey, useScanCode, handler);
            }
            public void Clear(ushort kvpKey) { DS4Windows.PressActionController.ClearKeyEntries(kvpKey); }
        }

        // New, clearer API names reflecting Mapping-trigger notifications
        public void OnSATriggerEstablished(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler, bool isSpecialAction)
        {
            impl.OnDown(kvpKey, nativeKey, useScanCode, handler, isSpecialAction);
        }

        public void OnSATriggerReleased(ushort kvpKey, uint nativeKey, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler)
        {
            impl.OnUp(kvpKey, nativeKey, useScanCode, handler);
        }

        // (Removed backward-compatible wrappers to avoid accidental use of old API names.)

        public void ClearKeyEntries(ushort kvpKey)
        {
            impl.Clear(kvpKey);
        }
    }
}
