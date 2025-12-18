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
                var kbc = ActionManager.GetOrCreateControllerForAction(device, action);
                try { AppLogger.LogTrace($"KeyAction.OnTrigger: obtained KBC={(kbc==null?"<null>":kbc.InstanceId.ToString())} for name={action?.name} device={device}"); } catch { }
                if (IsToggle())
                {
                    if (!state.PressedOnce)
                    {
                        kbc?.OnSATriggerEstablished(logicalValue, nativeValue == 0 ? nativeKey : nativeValue, useScan ? true : useScanCode, handler, true);
                        AppLogger.LogTrace($"KeyAction: trigger(established) delegated to controller for TOGGLE name={action?.name} device={device} key={logicalValue}");
                    }
                    else
                    {
                        // If already toggled ON and trigger occurs again, treat it as OFF (toggle off).
                        try
                        {
                            // Explicit toggle-off path: ask controller to stop repeater and send release.
                            kbc?.OnSATriggerToggleOff(logicalValue, nativeValue == 0 ? nativeKey : nativeValue, useScan ? true : useScanCode, handler);
                        }
                        catch { }
                        AppLogger.LogTrace($"KeyAction: trigger(released) delegated to controller for TOGGLE name={action?.name} device={device} key={logicalValue}");
                    }
                }
                else
                {
                    kbc?.OnSATriggerEstablished(logicalValue, nativeValue == 0 ? nativeKey : nativeValue, useScan ? true : useScanCode, handler, true);
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
                var kbc = ActionManager.GetOrCreateControllerForAction(device, action);
                kbc?.OnSATriggerReleased(logicalValue, nativeValue == 0 ? nativeKey : nativeValue, useScan ? true : useScanCode, handler);

                // Release notifications are delegated to controller; pressed-once lifecycle is managed by controller.
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"KeyAction.OnRelease failed: {ex}");
            }
        }
    }
}
