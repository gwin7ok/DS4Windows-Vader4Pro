using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DS4Windows;

[TestClass]
public class ScpUtilIntegrationTests
{
    [TestMethod]
    public void LoadSave_Roundtrip_PreservesWidthsAndLastChecked()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".xml");
        try
        {
            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<Profile app_version=\"3.11.3\" config_version=\"2\">\n" +
                "  <profileEditorLeftWidth>760</profileEditorLeftWidth>\n" +
                "  <profileEditorRightWidth>600</profileEditorRightWidth>\n" +
                "  <specialActionNameColWidth>310</specialActionNameColWidth>\n" +
                "  <specialActionTriggerColWidth>150</specialActionTriggerColWidth>\n" +
                "  <specialActionDetailColWidth>220</specialActionDetailColWidth>\n" +
                "  <LastChecked>11/11/2025 13:14:15</LastChecked>\n" +
                "</Profile>";

            File.WriteAllText(tempPath, xml);

            var bs = new BackingStore();
            bs.m_Profile = tempPath;
            bool loaded = bs.Load();
            Assert.IsTrue(loaded, "Initial Load failed");

            Assert.AreEqual(760, bs.profileEditorLeftWidth);
            Assert.AreEqual(600, bs.profileEditorRightWidth);
            Assert.AreEqual(310, bs.specialActionNameColWidth);
            Assert.AreEqual(150, bs.specialActionTriggerColWidth);
            Assert.AreEqual(220, bs.specialActionDetailColWidth);
            Assert.AreNotEqual(default(DateTime), bs.lastChecked, "LastChecked was not parsed");

            // Modify and save
            bs.profileEditorLeftWidth = 123;
            bs.profileEditorRightWidth = 456;
            bs.specialActionNameColWidth = 11;
            bs.specialActionTriggerColWidth = 22;
            bs.specialActionDetailColWidth = 33;

            bool saved = bs.Save();
            Assert.IsTrue(saved, "Save failed");

            // Load into a new instance
            var bs2 = new BackingStore();
            bs2.m_Profile = tempPath;
            bool loaded2 = bs2.Load();
            Assert.IsTrue(loaded2, "Reload failed");

            Assert.AreEqual(123, bs2.profileEditorLeftWidth);
            Assert.AreEqual(456, bs2.profileEditorRightWidth);
            Assert.AreEqual(11, bs2.specialActionNameColWidth);
            Assert.AreEqual(22, bs2.specialActionTriggerColWidth);
            Assert.AreEqual(33, bs2.specialActionDetailColWidth);

            // LastChecked should remain parseable and equal (string->DateTime roundtrip)
            Assert.AreNotEqual(default(DateTime), bs2.lastChecked);
            // Allow exact equality since serializer uses a fixed format
            Assert.AreEqual(bs.lastChecked.ToString("MM/dd/yyyy HH:mm:ss"), bs2.lastChecked.ToString("MM/dd/yyyy HH:mm:ss"));
        }
        finally
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
        }
    }
}
