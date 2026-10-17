using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;
using DS4Windows;

namespace StandaloneTests
{
    [TestClass]
    public class MappingControllerLifecycleTests
    {
        // Phase7-Step8 W2: 登録簿は Mapping から KeyButtonControllerRegistry へ移った。
        // ActionManager.ClearDeviceState（静的ファサード）は、コンテナから IMapping を解決して HandleDeviceDisconnect を呼ぶ。
        // ここでは IMapping の代わりに、登録簿を破棄するだけの偽物を差す。
        private class TestServiceProvider : IServiceProvider
        {
            private readonly object registry;
            private readonly object mapping;
            public TestServiceProvider(object registry, object mapping) { this.registry = registry; this.mapping = mapping; }
            public object GetService(Type serviceType)
            {
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

        [TestMethod]
        public void ClearDeviceState_RemovesKeyButtonControllersForDevice()
        {
            // Arrange: ensure no controllers for device 5
            int device = 5;

            var registry = new DS4Windows.Services.KeyButtonControllerRegistry(
                new DS4Windows.Actions.DefaultKeyButtonActionControllerFactory(), new DS4Windows.Actions.DefaultControllerRegistry());
            var dictField = typeof(DS4Windows.Services.KeyButtonControllerRegistry).GetField("_controllers", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(dictField, "Could not find _controllers field via reflection");
            var dict = dictField.GetValue(registry) as System.Collections.IDictionary;
            Assert.IsNotNull(dict);

            DS4Windows.DI.ServiceProviderHolder.SetProvider(new TestServiceProvider(registry, new DisconnectOnlyMapping(registry)));
            try
            {
                // Create entries
                registry.GetOrCreate(device, KeyButtonActionController.Mode.Press, "test-action-1");
                registry.GetOrCreate(device, KeyButtonActionController.Mode.Toggle, "test-action-2");

                // Verify entries exist for device
                var keysBefore = dict.Keys.Cast<object>().Select(k => k.ToString()).Where(s => s.StartsWith(device + ":")).ToArray();
                Assert.IsTrue(keysBefore.Length >= 1, "Expected at least one KeyButtonActionController entry for device before clear");

                // Act: clear device state via ActionManager (which calls IMapping.HandleDeviceDisconnect → registry.ClearForDevice)
                ActionManager.ClearDeviceState(device);

                // Assert: no entries remain for that device
                var keysAfter = dict.Keys.Cast<object>().Select(k => k.ToString()).Where(s => s.StartsWith(device + ":")).ToArray();
                Assert.IsTrue(keysAfter.Length == 0, "Expected no KeyButtonActionController entries for device after ClearDeviceState");
            }
            finally
            {
                DS4Windows.DI.ServiceProviderHolder.SetProvider(null);
            }
        }
    }
}
