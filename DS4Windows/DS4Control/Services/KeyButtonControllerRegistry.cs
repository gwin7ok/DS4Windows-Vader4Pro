using System;
using System.Collections.Generic;
using DS4Windows.Actions;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase7-Step8 W2（Q3＝A）: <see cref="IKeyButtonControllerRegistry"/> の実装。旧 <c>Mapping</c> の静的な登録簿
    /// （<c>keyButtonControllers</c>／<c>keyButtonControllerLock</c>／<c>GetOrCreateKeyButtonController</c>×2／
    /// <c>ClearKeyButtonControllersForDevice</c>／<c>GetOrCreateKeyButtonControllerForAction</c>）を、キー形式（"{device}:{actionName}"）、
    /// ログ、ファクトリーが無いときの直接生成（<c>new KeyButtonActionController</c>）まで変えずに移した。
    /// 旧: ファクトリーと <see cref="IControllerRegistry"/> を <c>ServiceProviderHolder</c> で解決していた。コンストラクターで受ける。
    /// </summary>
    public class KeyButtonControllerRegistry : IKeyButtonControllerRegistry
    {
        private readonly object _lock = new object();
        private readonly Dictionary<string, KeyButtonActionController> _controllers = new Dictionary<string, KeyButtonActionController>();
        private readonly IKeyButtonActionControllerFactory _factory;
        private readonly IControllerRegistry _controllerRegistry;

        public KeyButtonControllerRegistry(IKeyButtonActionControllerFactory factory, IControllerRegistry controllerRegistry)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _controllerRegistry = controllerRegistry ?? throw new ArgumentNullException(nameof(controllerRegistry));
        }

        // Key format: "{device}:{actionName}". If actionName is null use "{device}:<default>" to preserve device-only fallbacks.
        private static string MakeKbcDictKey(int device, string actionName)
        {
            return device + ":" + (string.IsNullOrEmpty(actionName) ? "<default>" : actionName);
        }

        public KeyButtonActionController GetOrCreate(int device, KeyButtonActionController.Mode mode, string actionName = null)
        {
            try
            {
                lock (_lock)
                {
                    string dictKey = MakeKbcDictKey(device, actionName ?? "<mapping>");
                    if (!_controllers.TryGetValue(dictKey, out KeyButtonActionController inst) || inst == null)
                    {
                        KeyButtonActionController temp = null;
                        try
                        {
                            temp = _factory.Create(device, mode, actionName ?? "<mapping>");
                        }
                        catch { }

                        inst = temp ?? new KeyButtonActionController(device, mode, actionName ?? "<mapping>");
                        _controllers[dictKey] = inst;
                        try { AppLogger.LogTrace($"KBC DIAGNOSTIC: Created controller dictKey={dictKey} id={inst.InstanceId} assignedAction={(inst.AssignedActionName ?? "(null)")} mode={mode} device={device}"); } catch { }
                    }
                    else
                    {
                        try { AppLogger.LogTrace($"KBC DIAGNOSTIC: Reusing existing controller dictKey={dictKey} id={inst.InstanceId} assignedAction={(inst.AssignedActionName ?? "(null)")} mode={mode} device={device}"); } catch { }
                    }
                    return inst;
                }
            }
            catch { return null; }
        }

        public KeyButtonActionController GetOrCreate(int device, SpecialAction sa)
        {
            try
            {
                lock (_lock)
                {
                    string name = sa?.name ?? "<sa>";
                    string dictKey = MakeKbcDictKey(device, name);
                    if (!_controllers.TryGetValue(dictKey, out KeyButtonActionController inst) || inst == null)
                    {
                        KeyButtonActionController temp = null;
                        try
                        {
                            temp = _factory.Create(device, sa, name);
                        }
                        catch { }

                        inst = temp ?? new KeyButtonActionController(device, sa, name);
                        _controllers[dictKey] = inst;
                        try { AppLogger.LogTrace($"KBC DIAGNOSTIC: Created controller dictKey={dictKey} id={inst.InstanceId} assignedAction={(inst.AssignedActionName ?? "(null)")} device={device}"); } catch { }
                    }
                    else
                    {
                        try { AppLogger.LogTrace($"KBC DIAGNOSTIC: Reusing existing controller dictKey={dictKey} id={inst.InstanceId} assignedAction={(inst.AssignedActionName ?? "(null)")} device={device}"); } catch { }
                    }
                    // additional check: if existing assignedAction differs from requested, log warning
                    try
                    {
                        if (inst != null && !string.Equals(inst.AssignedActionName, name, StringComparison.OrdinalIgnoreCase))
                        {
                            AppLogger.LogDebug($"KBC DIAGNOSTIC: assignedAction mismatch for dictKey={dictKey} existing={(inst.AssignedActionName ?? "(null)")} requested={name} device={device}");
                        }
                    }
                    catch { }
                    return inst;
                }
            }
            catch { return null; }
        }

        public IActionController GetOrCreateForAction(int device, SpecialAction sa)
        {
            // Wrap existing KeyButtonActionController in adapter implementing IActionController
            var inner = GetOrCreate(device, sa);
            if (inner == null) return null;
            return new KeyButtonActionControllerAdapter(inner);
        }

        // Remove and destroy all per-device KeyButtonActionController instances for given device
        public void ClearForDevice(int device)
        {
            try
            {
                lock (_lock)
                {
                    var prefix = device + ":";
                    var keys = new List<string>(_controllers.Keys);
                    foreach (var k in keys)
                    {
                        if (k.StartsWith(prefix))
                        {
                            try
                            {
                                var inst = _controllers[k];
                                try { inst?.Dispose(); } catch { }
                                try { _controllerRegistry.Unregister(k); } catch { }
                                _controllers.Remove(k);
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
        }
    }
}
