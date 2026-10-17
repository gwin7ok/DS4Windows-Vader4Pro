using System;
using DS4Windows.Actions;
using DS4Windows.DS4Control;
using Xunit;

namespace DS4Windows.Actions.Tests
{
    // Lightweight test ServiceProvider that returns a single object for requested service types.
    class TestServiceProvider : IServiceProvider
    {
        private readonly object[] svcs;
        // Phase7-Step8 W2: KeyAction は ActionManager（静的ファサード）経由で IKeyButtonControllerRegistry を解決するため、複数のサービスを返せるようにした
        public TestServiceProvider(params object[] svcs) { this.svcs = svcs; }
        public object GetService(Type serviceType)
        {
            foreach (var svc in svcs)
            {
                if (svc != null && serviceType.IsInstanceOfType(svc)) return svc;
            }
            return null;
        }
    }
}