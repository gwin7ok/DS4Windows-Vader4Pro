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
        }

        public void OnTrigger(int device, ushort logicalValue, uint nativeValue, bool useScanCode, DS4Windows.DS4Control.VirtualKBMBase handler)
        {
            try
            {
                var state = ActionManager.GetStateFor(action, device);
                var kbc = Mapping.GetOrCreateKeyButtonControllerForAction(device, action);
                if (IsToggle())
                {
                    if (!state.PressedOnce)
                    {
                        kbc?.OnSATriggerEstablished(logicalValue, nativeValue == 0 ? nativeKey : nativeValue, useScan ? true : useScanCode, handler, true);
                        state.PressedOnce = true;
                        state.LastToggleTimeUtcTicks = DateTime.UtcNow.Ticks;
                        AppLogger.LogTrace($"KeyAction: toggled ON name={action?.name} device={device} key={logicalValue}");
                    }
                    else
                    {
                        AppLogger.LogTrace($"KeyAction: toggle ignored (already pressedOnce) name={action?.name} device={device} key={logicalValue}");
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
                var kbc = Mapping.GetOrCreateKeyButtonControllerForAction(device, action);
                kbc?.OnSATriggerReleased(logicalValue, nativeValue == 0 ? nativeKey : nativeValue, useScan ? true : useScanCode, handler);

                if (IsToggle() && state != null && state.PressedOnce)
                {
                    long delta = DateTime.UtcNow.Ticks - state.LastToggleTimeUtcTicks;
                    if (delta > TimeSpan.FromMilliseconds(200).Ticks)
                    {
                        state.PressedOnce = false;
                        AppLogger.LogTrace($"KeyAction: pressedOnce cleared name={action?.name} device={device} key={logicalValue}");
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"KeyAction.OnRelease failed: {ex}");
            }
        }
    }
}
