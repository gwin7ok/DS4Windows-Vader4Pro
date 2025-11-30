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
        public void ScpUtilAndUtil_NormalizeConsistent()
        {
            string input = "  Action\t\n";
            string a = Util.NormalizeActionName(input);
            string b = DS4Windows.ScpUtil.NormalizeActionName(input);
            Assert.AreEqual(a, b);
        }
    }
}
