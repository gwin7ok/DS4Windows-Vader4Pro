using System;
using System.Collections.Generic;

namespace DS4Windows.Actions
{
    public interface IControllerRegistry
    {
        void Register(string key, IDisposable controller);
        void Unregister(string key);
        IReadOnlyList<IDisposable> GetControllersForDevice(int device);
        void ClearControllersForDevice(int device);
    }
}
