using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;
using DS4Windows;

namespace StandaloneTests
{
    [TestClass]
    public class MappingControllerDisposeBehaviorTests
    {
        private class TestServiceProvider : IServiceProvider
        {
            private readonly object factory;
            public TestServiceProvider(object factory) { this.factory = factory; }
            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(DS4Windows.Actions.IKeyButtonActionControllerFactory)) return factory;
                return null;
            }
        }

        private class TestFactory : DS4Windows.Actions.IKeyButtonActionControllerFactory
        {
            private readonly Func<int, KeyButtonActionController.Mode, string, KeyButtonActionController> creator;
            public TestFactory(Func<int, KeyButtonActionController.Mode, string, KeyButtonActionController> creator)
            {
                this.creator = creator;
            }
            public KeyButtonActionController Create(int device, KeyButtonActionController.Mode mode, string actionName = null)
            {
                return creator(device, mode, actionName);
            }
            public KeyButtonActionController Create(int device, SpecialAction sa, string actionName = null)
            {
                // Not used in these tests
                return creator(device, KeyButtonActionController.Mode.Press, actionName);
            }
        }

        private class TestKeyButtonActionController : KeyButtonActionController
        {
            public bool WasDisposed { get; private set; }
            private readonly bool throwOnDispose;
            public TestKeyButtonActionController(int device, Mode mode, string name = null, bool throwOnDispose = false) : base(device, mode, name)
            {
                this.throwOnDispose = throwOnDispose;
            }
            public override void Dispose()
            {
                WasDisposed = true;
                if (throwOnDispose) throw new InvalidOperationException("Dispose failed");
                try { base.Dispose(); } catch { }
            }
        }

        [TestMethod]
        public void ClearDeviceState_UsesDispose_FromFactoryCreatedControllers()
        {
            int device = 7;
            // arrange: install service provider returning factory that creates TestKeyButtonActionController
            var factory = new TestFactory((d, m, n) => new TestKeyButtonActionController(d, m, n));
            DS4Windows.DI.ServiceProviderHolder.SetProvider(new TestServiceProvider(factory));

            try
            {
                // create controllers via private Mapping factory
                var mappingType = typeof(Mapping);
                var getOrCreate = mappingType.GetMethod("GetOrCreateKeyButtonController", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(int), typeof(KeyButtonActionController.Mode), typeof(string) }, null);
                Assert.IsNotNull(getOrCreate);
                getOrCreate.Invoke(null, new object[] { device, KeyButtonActionController.Mode.Press, "t1" });
                getOrCreate.Invoke(null, new object[] { device, KeyButtonActionController.Mode.Toggle, "t2" });

                // capture instances
                var dictField = mappingType.GetField("keyButtonControllers", BindingFlags.Static | BindingFlags.NonPublic);
                var dict = dictField.GetValue(null) as System.Collections.IDictionary;
                var instances = dict.Values.Cast<object>().Where(v => v is TestKeyButtonActionController).Cast<TestKeyButtonActionController>().ToArray();
                Assert.IsTrue(instances.Length >= 1);

                // act
                ActionManager.ClearDeviceState(device);

                // assert: each instance was disposed and mapping entries removed
                foreach (var inst in instances) Assert.IsTrue(inst.WasDisposed, "Expected controller disposed");
                var keysAfter = dict.Keys.Cast<object>().Select(k => k.ToString()).Where(s => s.StartsWith(device + ":")).ToArray();
                Assert.AreEqual(0, keysAfter.Length);
            }
            finally
            {
                DS4Windows.DI.ServiceProviderHolder.SetProvider(null);
            }
        }

        [TestMethod]
        public void ClearDeviceState_Continues_WhenDisposeThrows()
        {
            int device = 8;
            var throwing = new TestKeyButtonActionController(device, KeyButtonActionController.Mode.Press, "bad", true);
            var normal = new TestKeyButtonActionController(device, KeyButtonActionController.Mode.Toggle, "good", false);

            var factory = new TestFactory((d, m, n) =>
            {
                // alternate creations to return throwing then normal
                if (n == "bad") return (KeyButtonActionController)throwing;
                return (KeyButtonActionController)normal;
            });

            DS4Windows.DI.ServiceProviderHolder.SetProvider(new TestServiceProvider(factory));
            try
            {
                var mappingType = typeof(Mapping);
                var getOrCreate = mappingType.GetMethod("GetOrCreateKeyButtonController", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(int), typeof(KeyButtonActionController.Mode), typeof(string) }, null);
                getOrCreate.Invoke(null, new object[] { device, KeyButtonActionController.Mode.Press, "bad" });
                getOrCreate.Invoke(null, new object[] { device, KeyButtonActionController.Mode.Toggle, "good" });

                var dictField = mappingType.GetField("keyButtonControllers", BindingFlags.Static | BindingFlags.NonPublic);
                var dict = dictField.GetValue(null) as System.Collections.IDictionary;

                // act
                ActionManager.ClearDeviceState(device);

                // assert: mapping entries cleared and normal controller disposed (throwing one may have thrown but should not block)
                Assert.IsTrue(normal.WasDisposed, "Expected normal controller disposed even if another threw");
                var keysAfter = dict.Keys.Cast<object>().Select(k => k.ToString()).Where(s => s.StartsWith(device + ":")).ToArray();
                Assert.AreEqual(0, keysAfter.Length);
            }
            finally
            {
                DS4Windows.DI.ServiceProviderHolder.SetProvider(null);
            }
        }
    }
}
