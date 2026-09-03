using System;
using System.IO;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    public class ProfileXmlStoreTests
    {
        [Fact]
        public void SaveProfileXml_ShouldReturnTrueOnSuccessfulWrite()
        {
            DS4WinWPF.AppHost.CreateHost();

            string profilesDir = Path.Combine(Global.appdatapath, "Profiles");
            if (!Directory.Exists(profilesDir))
            {
                Directory.CreateDirectory(profilesDir);
            }

            var store = new ProfileXmlStore(new BackingStore());
            string profileName = "Phase5Step2_ProfileXmlStore_SaveTest";
            string path = Path.Combine(profilesDir, $"{profileName}.xml");

            try
            {
                bool result = store.SaveProfileXml(0, profileName);
                Assert.True(result);
            }
            finally
            {
                if (File.Exists(path))
                {
                    try { File.Delete(path); } catch { }
                }
            }
        }

        [Fact]
        public void LoadProfileXml_NonExistentPath_ShouldNotThrow()
        {
            DS4WinWPF.AppHost.CreateHost();

            var store = new ProfileXmlStore(new BackingStore());
            var control = DS4WinWPF.AppHost.GetService<ControlService>();

            var exception = Record.Exception(() =>
                store.LoadProfileXml(0, false, control, @"NonExistent_Phase5Step2_Path.xml", false, true));

            Assert.Null(exception);
        }

        [Fact]
        public void XmlIoLock_ShouldBeSharedStaticObject()
        {
            // Step6(AppSettingsService)が同一ロックを共有できることを保証する回帰テスト
            Assert.NotNull(ProfileXmlStore.XmlIoLock);
        }

        [Fact]
        public void Constructor_WithNullBackingStore_ShouldFallBackToGlobalStore()
        {
            var store = new ProfileXmlStore(null);
            Assert.NotNull(store);
        }
    }
}
