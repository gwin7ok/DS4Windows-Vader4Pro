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
        // Phase7-Step8 W2: 登録簿は Mapping から KeyButtonControllerRegistry へ移った。ActionManager.ClearDeviceState（静的ファサード）は、
        // コンテナから IMapping を解決して HandleDeviceDisconnect を呼ぶ。ここでは IMapping の代わりに、登録簿を破棄するだけの偽物を差す。
        private class TestServiceProvider : IServiceProvider
        {
            private readonly object factory;
            private readonly DS4Windows.Services.KeyButtonControllerRegistry registry;
            private readonly object mapping;
            public TestServiceProvider(object factory)
            {
                this.factory = factory;
                registry = new DS4Windows.Services.KeyButtonControllerRegistry(
                    (DS4Windows.Actions.IKeyButtonActionControllerFactory)factory, new DS4Windows.Actions.DefaultControllerRegistry());
                mapping = new DisconnectOnlyMapping(registry);
            }
            public DS4Windows.Services.KeyButtonControllerRegistry Registry => registry;
            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(DS4Windows.Actions.IKeyButtonActionControllerFactory)) return factory;
                if (serviceType == typeof(DS4Windows.DI.IKeyButtonControllerRegistry)) return registry;
                if (serviceType == typeof(DS4Windows.DI.IMapping)) return mapping;
                return null;
            }
        }

        private class DisconnectOnlyMapping : DS4Windows.DI.IMapping
        {
            private readonly DS4Windows.DI.IKeyButtonControllerRegistry registry;
            public DisconnectOnlyMapping(DS4Windows.DI.IKeyButtonControllerRegistry registry) { this.registry = registry; }
            public void HandleDeviceDisconnect(int device) { registry.ClearForDevice(device); }
            public DS4State SetCurveAndDeadzone(int device, DS4State cState, DS4State dState, DS4Windows.DI.IProfileSettingsService settings) { throw new NotSupportedException(); }
            public void MapCustom(int device, DS4State cState, DS4State MappedState, DS4StateExposed eState, Mouse tp, ControlService ctrl) { throw new NotSupportedException(); }
            public void TempMouseJoystick(int device, DS4State MappedState) { throw new NotSupportedException(); }
            public void Commit(int device, DS4Windows.DI.IProfileSettingsService settings) { throw new NotSupportedException(); }
            public bool GetBoolMappingExternal(int device, DS4Controls control, DS4State cState, DS4StateExposed eState, Mouse tp) { throw new NotSupportedException(); }
            public void PlayMacroDirect(int device, SpecialAction action) { throw new NotSupportedException(); }
            public void EndMacroDirect(int device) { throw new NotSupportedException(); }
        }

        private static System.Collections.IDictionary ControllersOf(DS4Windows.Services.KeyButtonControllerRegistry registry)
        {
            var field = typeof(DS4Windows.Services.KeyButtonControllerRegistry).GetField("_controllers", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Could not find _controllers field via reflection");
            return field.GetValue(registry) as System.Collections.IDictionary;
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
            var provider = new TestServiceProvider(factory);
            DS4Windows.DI.ServiceProviderHolder.SetProvider(provider);

            try
            {
                // create controllers via the registry
                provider.Registry.GetOrCreate(device, KeyButtonActionController.Mode.Press, "t1");
                provider.Registry.GetOrCreate(device, KeyButtonActionController.Mode.Toggle, "t2");

                // capture instances
                var dict = ControllersOf(provider.Registry);
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

            var provider = new TestServiceProvider(factory);
            DS4Windows.DI.ServiceProviderHolder.SetProvider(provider);
            try
            {
                provider.Registry.GetOrCreate(device, KeyButtonActionController.Mode.Press, "bad");
                provider.Registry.GetOrCreate(device, KeyButtonActionController.Mode.Toggle, "good");

                var dict = ControllersOf(provider.Registry);

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
