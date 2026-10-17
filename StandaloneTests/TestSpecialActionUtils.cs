using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using DS4Windows;

namespace StandaloneTests
{
    [TestClass]
    public class TestSpecialActionUtils
    {
        [TestMethod]
        public void NormalizeActionName_TrimsWhitespace()
        {
            string input = "  My Action Name  ";
            string expected = "My Action Name";
            string actual = Util.NormalizeActionName(input);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void NormalizeActionName_NullReturnsEmpty()
        {
            string input = null;
            string expected = string.Empty;
            string actual = Util.NormalizeActionName(input);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Util_NormalizeActionName_FixedExpectations()
        {
            // Phase6-Step12 Wave 1: 重複実装だった Global.NormalizeActionName を削除したため、Util 側の期待値を固定する。
            Assert.AreEqual("Action", Util.NormalizeActionName("  Action\t\n"));
            Assert.AreEqual(string.Empty, Util.NormalizeActionName(null));
            Assert.AreEqual("My Action Name", Util.NormalizeActionName("  My Action Name  "));
            Assert.AreEqual(string.Empty, Util.NormalizeActionName("   "));
        }
    }
}
