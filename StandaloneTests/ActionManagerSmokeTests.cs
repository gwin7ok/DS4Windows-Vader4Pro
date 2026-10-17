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
            // Phase6-Step10-4-3: DI ホストの生成が Global の静的初期化の副作用に依存していた（K7-1）。
            // 1 回目と 2 回目の GetStateFor が同じ管理先を使うよう、ホストを先に明示的に構築する。
            DS4WinWPF.AppHost.CreateHost();

            // Arrange: create a lightweight SpecialAction
            var sa = TestHelpers.CreateKeyAction("smoke_test", "L1", "32");

            // Act: get state and flip IsToggledOn
            var st = ActionManager.GetStateFor(sa, 0);
            Assert.IsNotNull(st, "Action state should not be null");
            st.IsToggledOn = true;

            // Assert: value persisted
            var st2 = ActionManager.GetStateFor(sa, 0);
            Assert.IsNotNull(st2);
            Assert.IsTrue(st2.IsToggledOn, "IsToggledOn should be true after set");

            // Clear entries and ensure new state resets flag
            ActionManager.ClearAllEntries();
            var st3 = ActionManager.GetStateFor(sa, 0);
            Assert.IsNotNull(st3);
            Assert.IsFalse(st3.IsToggledOn, "IsToggledOn should be false after ClearAllEntries");
        }
    }
}