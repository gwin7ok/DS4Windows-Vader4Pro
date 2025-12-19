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
                try { AppLogger.LogTrace($"KeyAction.OnTrigger ENTER: name={action?.name} device={device} logical={logicalValue} native={nativeValue} useScan={useScan} isToggledOn={(state?.IsToggledOn ?? false)}"); } catch { }
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

                // Forward the trigger (established) to the controller and let it decide semantics (press vs toggle)
                try { ctrl?.Handle(binding, trigger); } catch { }
                AppLogger.LogTrace($"KeyAction: trigger(established) forwarded to controller name={action?.name} device={device} key={logicalValue}");
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
                // Forward release edge to controller and let controller instance decide how to handle it.
                try { ctrl?.Handle(binding, trigger); } catch { }
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"KeyAction.OnRelease failed: {ex}");
            }
        }
    }
}
