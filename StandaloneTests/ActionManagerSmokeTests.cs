using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using DS4Windows;

namespace StandaloneTests
{
    [TestClass]
    public class ActionManagerSmokeTests
    {
        [TestMethod]
        public void ActionDoneFlagRoundtrip()
        {
            // Arrange: create a lightweight SpecialAction
            var sa = TestHelpers.CreateKeyAction("smoke_test", "L1", "32");

            // Act: get state and flip ActionDone
            var st = ActionManager.GetStateFor(sa, 0);
            Assert.IsNotNull(st, "Action state should not be null");
            st.ActionDone = true;

            // Assert: value persisted
            var st2 = ActionManager.GetStateFor(sa, 0);
            Assert.IsNotNull(st2);
            Assert.IsTrue(st2.ActionDone, "ActionDone should be true after set");

            // Clear entries and ensure new state resets flag
            ActionManager.ClearAllEntries();
            var st3 = ActionManager.GetStateFor(sa, 0);
            Assert.IsNotNull(st3);
            Assert.IsFalse(st3.ActionDone, "ActionDone should be false after ClearAllEntries");
        }
    }
}
