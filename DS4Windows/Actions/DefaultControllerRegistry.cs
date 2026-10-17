using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DS4Windows.Actions
{
    public class DefaultControllerRegistry : IControllerRegistry
    {
        // key -> controller
        private readonly ConcurrentDictionary<string, IDisposable> controllers = new ConcurrentDictionary<string, IDisposable>(StringComparer.Ordinal);

        public void Register(string key, IDisposable controller)
        {
            if (string.IsNullOrEmpty(key) || controller == null) return;
            controllers[key] = controller;
        }

        public void Unregister(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            controllers.TryRemove(key, out _);
        }

        public IReadOnlyList<IDisposable> GetControllersForDevice(int device)
        {
            var prefix = device + ":";
            return controllers.Keys.Where(k => k.StartsWith(prefix)).Select(k => controllers[k]).ToList().AsReadOnly();
        }

        public void ClearControllersForDevice(int device)
        {
            var prefix = device + ":";
            var keys = controllers.Keys.Where(k => k.StartsWith(prefix)).ToArray();
            foreach (var k in keys)
            {
                try { controllers[k]?.Dispose(); } catch { }
                controllers.TryRemove(k, out _);
            }
        }
    }
}
