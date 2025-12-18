using System;
using DS4Windows.DS4Control;

namespace DS4Windows
{
    // KeyAction implementation that delegates to Mapping's KeyButtonActionController cache.
    public class KeyAction
    {
        private readonly SpecialAction action;
        private readonly int index;

        // Detailed constructor below initializes parsed key/native values.

        private bool IsToggle()
        {
            try
            {
                if (action == null) return false;
                if (action.KeyButtonSwitchMode.HasValue)
                    return action.KeyButtonSwitchMode.Value == SpecialAction.KeyButtonSwitchModeEnum.Toggle;
                return action.keyType.HasFlag(DS4KeyType.Toggle);
            }
            catch { return false; }
        }

        private readonly ushort keyId;
        private readonly uint nativeKey;
        private readonly bool useScan;
        // No per-action repeater here; RepeatHelper should be used only by controllers.

        public KeyAction(SpecialAction action, int index)
        {
            this.action = action;
            this.index = index;
            ushort k = 0;
            if (action != null && !string.IsNullOrEmpty(action.details))
                ushort.TryParse(action.details, out k);
            keyId = k;
            try { nativeKey = SyntheticDispatcher.ResolveNativeKey(k); } catch { nativeKey = 0; }
            useScan = action != null && action.keyType.HasFlag(DS4KeyType.ScanCode);
            // Do not parse repeater options here; controllers handle repeat behavior.
        }

        public void OnTrigger(int device, ushort logicalValue, uint nativeValue, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler)
        {
            try
            {
                var state = ActionManager.GetStateFor(action, device);
                try { AppLogger.LogTrace($"KeyAction.OnTrigger ENTER: name={action?.name} device={device} logical={logicalValue} native={nativeValue} useScan={useScan} pressedOnce={(state?.PressedOnce ?? false)}"); } catch { }
                var ctrl = ActionManager.GetOrCreateControllerForAction(device, action);
                try { AppLogger.LogTrace($"KeyAction.OnTrigger: obtained controller={(ctrl==null?"<null>":ctrl.ControllerId.ToString())} for name={action?.name} device={device}"); } catch { }

                var trigger = new DS4Windows.Actions.TriggerContextImpl
                {
                    Device = device,
                    IsEdgeEstablished = true,
                    LogicalValue = logicalValue,
                    NativeValue = nativeValue == 0 ? nativeKey : nativeValue,
                    OutputHandler = handler,
                    Timestamp = DateTime.UtcNow
                };

                var binding = new DS4Windows.Actions.KeyActionBinding(action);

                if (IsToggle())
                {
                    if (!state.PressedOnce)
                    {
                        try { ctrl?.Start(binding, trigger); } catch { }
                        AppLogger.LogTrace($"KeyAction: trigger(established) delegated to controller for TOGGLE name={action?.name} device={device} key={logicalValue}");
                    }
                    else
                    {
                        try { ctrl?.Stop(binding, trigger); } catch { }
                        AppLogger.LogTrace($"KeyAction: trigger(released) delegated to controller for TOGGLE name={action?.name} device={device} key={logicalValue}");
                    }
                }
                else
                {
                    try { ctrl?.Start(binding, trigger); } catch { }
                    AppLogger.LogTrace($"KeyAction: press sent name={action?.name} device={device} key={logicalValue}");
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"KeyAction.OnTrigger failed: {ex}");
            }
        }

        public void OnRelease(int device, ushort logicalValue, uint nativeValue, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler)
        {
            try
            {
                var state = ActionManager.GetStateFor(action, device);
                var ctrl = ActionManager.GetOrCreateControllerForAction(device, action);
                var trigger = new DS4Windows.Actions.TriggerContextImpl
                {
                    Device = device,
                    IsEdgeEstablished = false,
                    LogicalValue = logicalValue,
                    NativeValue = nativeValue == 0 ? nativeKey : nativeValue,
                    OutputHandler = handler,
                    Timestamp = DateTime.UtcNow
                };
                var binding = new DS4Windows.Actions.KeyActionBinding(action);
                try { ctrl?.Stop(binding, trigger); } catch { }

                // Release notifications are delegated to controller; pressed-once lifecycle is managed by controller.
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"KeyAction.OnRelease failed: {ex}");
            }
        }
    }
}
