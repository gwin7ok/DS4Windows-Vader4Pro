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
        [TestMethod]
        public void ClearDeviceState_RemovesKeyButtonControllersForDevice()
        {
            // Arrange: ensure no controllers for device 5
            int device = 5;

            var mappingType = typeof(Mapping);
            var dictField = mappingType.GetField("keyButtonControllers", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(dictField, "Could not find keyButtonControllers field via reflection");
            var dict = dictField.GetValue(null) as System.Collections.IDictionary;
            Assert.IsNotNull(dict);

            // Create one or two controllers via private factory method
            var getOrCreate = mappingType.GetMethod("GetOrCreateKeyButtonController", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(int), typeof(KeyButtonActionController.Mode), typeof(string) }, null);
            Assert.IsNotNull(getOrCreate, "Could not find GetOrCreateKeyButtonController(int,Mode,string)");

            // Create entries
            getOrCreate.Invoke(null, new object[] { device, KeyButtonActionController.Mode.Press, "test-action-1" });
            getOrCreate.Invoke(null, new object[] { device, KeyButtonActionController.Mode.Toggle, "test-action-2" });

            // Verify entries exist for device
            var keysBefore = dict.Keys.Cast<object>().Select(k => k.ToString()).Where(s => s.StartsWith(device + ":")).ToArray();
            Assert.IsTrue(keysBefore.Length >= 1, "Expected at least one KeyButtonActionController entry for device before clear");

            // Act: clear device state via ActionManager (which calls Mapping.ClearKeyButtonControllersForDevice)
            ActionManager.ClearDeviceState(device);

            // Assert: no entries remain for that device
            var keysAfter = dict.Keys.Cast<object>().Select(k => k.ToString()).Where(s => s.StartsWith(device + ":")).ToArray();
            Assert.IsTrue(keysAfter.Length == 0, "Expected no KeyButtonActionController entries for device after ClearDeviceState");
        }
    }
}
