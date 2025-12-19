using System;
using DS4Windows.DS4Control;
using DS4Windows;

namespace DS4Windows.Actions
{
    // Adapter that wraps existing KeyButtonActionController and exposes IActionController
    public class KeyButtonActionControllerAdapter : IActionController
    {
        private readonly KeyButtonActionController inner;
        private readonly bool isToggle;

        public int ControllerId => inner?.InstanceId ?? -1;
        public KeyButtonActionControllerAdapter(int device, SpecialAction sa)
        {
            inner = new KeyButtonActionController(device, sa, sa?.name ?? "<unknown>");
            try
            {
                if (sa != null)
                {
                    if (sa.KeyButtonSwitchMode.HasValue)
                        isToggle = sa.KeyButtonSwitchMode.Value == SpecialAction.KeyButtonSwitchModeEnum.Toggle;
                    else
                        isToggle = sa.keyType.HasFlag(DS4KeyType.Toggle);
                }
                else
                {
                    isToggle = false;
                }
            }
            catch { isToggle = false; }
            try { AppLogger.LogTrace($"KBC Adapter ctor(device={device}) created adapter ControllerId={ControllerId} assigned={(sa?.name??"(null)")} isToggle={isToggle}"); } catch { }
        }

        // Wrap an existing KeyButtonActionController instance (used by Mapping to avoid duplicate instances)
        public KeyButtonActionControllerAdapter(KeyButtonActionController existing)
        {
            inner = existing ?? throw new ArgumentNullException(nameof(existing));
            // Query inner controller for its configured mode to avoid mismatches with registry timing
            try { isToggle = inner.IsToggleMode; } catch { isToggle = false; }
            try { AppLogger.LogTrace($"KBC Adapter wrap ctor created adapter ControllerId={ControllerId} assigned={(inner?.AssignedActionName??"(null)")} isToggle={isToggle}"); } catch { }
        }

        public void Start(IActionBinding binding, ITriggerContext trigger)
        {
            try
            {
                if (inner == null || trigger == null) return;
                try { AppLogger.LogTrace($"KBC Adapter Start: ControllerId={ControllerId} assigned={(inner?.AssignedActionName??"(null)")} isToggle={isToggle} binding={(binding!=null?binding.ToString():"(null)")} triggerVal={trigger.LogicalValue}"); } catch { }
                // Delegate to existing controller entry point
                inner.OnSATriggerEstablished(trigger.LogicalValue, trigger.NativeValue, false, trigger.OutputHandler, true);
            }
            catch { }
        }
        
        public void Handle(IActionBinding binding, ITriggerContext trigger)
        {
            try
            {
                if (inner == null || trigger == null) return;
                try { AppLogger.LogTrace($"KBC Adapter Handle: ControllerId={ControllerId} assigned={(inner?.AssignedActionName??"(null)")} isToggle={isToggle} binding={(binding!=null?binding.ToString():"(null)")} isEstablished={trigger.IsEdgeEstablished} triggerVal={trigger.LogicalValue}"); } catch { }
                if (trigger.IsEdgeEstablished)
                {
                    inner.OnSATriggerEstablished(trigger.LogicalValue, trigger.NativeValue, false, trigger.OutputHandler, true);
                }
                else
                {
                    // Forward release edge to controller; controller impl decides whether to ignore (toggle) or clear (press)
                    inner.OnSATriggerReleased(trigger.LogicalValue, trigger.NativeValue, false, trigger.OutputHandler);
                }
            }
            catch { }
        }

        public void Stop(IActionBinding binding, ITriggerContext trigger)
        {
            try
            {
                if (inner == null || trigger == null) return;
                try { AppLogger.LogTrace($"KBC Adapter Stop: ControllerId={ControllerId} assigned={(inner?.AssignedActionName??"(null)")} isToggle={isToggle} binding={(binding!=null?binding.ToString():"(null)")} triggerVal={trigger.LogicalValue}"); } catch { }
                // Preserve Stop for backward compatibility: treat as explicit toggle-off for toggle-mode
                if (isToggle)
                {
                    inner.OnSATriggerToggleOff(trigger.LogicalValue, trigger.NativeValue, false, trigger.OutputHandler);
                    return;
                }

                inner.OnSATriggerReleased(trigger.LogicalValue, trigger.NativeValue, false, trigger.OutputHandler);
            }
            catch { }
        }

        public void Clear()
        {
            try { inner?.ClearKeyEntries(0); } catch { }
        }

        public void Dispose()
        {
            try { inner?.Dispose(); } catch { }
        }
    }
}
