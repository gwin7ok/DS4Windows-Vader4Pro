using System;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    public class AppSettingsServiceTests
    {
        public AppSettingsServiceTests()
        {
            var pathService = new PathService();
            if (string.IsNullOrEmpty(Global.appdatapath))
            {
                Global.appdatapath = pathService.AppDataPath;
            }
        }

        private class MockProfileXmlStore : IProfileXmlStore
        {
            public bool LoadAppSettingsResult { get; set; } = true;
            public bool SaveAppSettingsResult { get; set; } = true;
            public int LoadCallCount { get; private set; }
            public int SaveCallCount { get; private set; }

            public bool LoadProfileXml(int deviceIndex, bool launchProgram, ControlService control,
                string overridePath = "", bool xinputChange = true, bool postLoad = true) => true;

            public bool SaveProfileXml(int deviceIndex, string profileName) => true;

            public bool LoadAppSettingsXml()
            {
                LoadCallCount++;
                return LoadAppSettingsResult;
            }

            public bool SaveAppSettingsXml()
            {
                SaveCallCount++;
                return SaveAppSettingsResult;
            }
        }

        [Fact]
        public void Save_DelegatesToXmlStore_ReturnsTrueOnSuccess()
        {
            var mockXmlStore = new MockProfileXmlStore { SaveAppSettingsResult = true };
            var service = new AppSettingsService(mockXmlStore);

            bool result = service.Save();

            Assert.True(result);
            Assert.Equal(1, mockXmlStore.SaveCallCount);
        }

        [Fact]
        public void Save_DelegatesToXmlStore_ReturnsFalseOnFailure()
        {
            var mockXmlStore = new MockProfileXmlStore { SaveAppSettingsResult = false };
            var service = new AppSettingsService(mockXmlStore);

            bool result = service.Save();

            Assert.False(result);
            Assert.Equal(1, mockXmlStore.SaveCallCount);
        }

        [Fact]
        public void Load_DelegatesToXmlStore_ReturnsTrueOnSuccess()
        {
            var mockXmlStore = new MockProfileXmlStore { LoadAppSettingsResult = true };
            var service = new AppSettingsService(mockXmlStore);

            bool result = service.Load();

            Assert.True(result);
            Assert.Equal(1, mockXmlStore.LoadCallCount);
        }

        [Fact]
        public void Properties_UpdateGlobalAndFiresSettingChangedEvent()
        {
            var mockXmlStore = new MockProfileXmlStore();
            var service = new AppSettingsService(mockXmlStore);
            string changedProperty = null;
            service.SettingChanged += (s, prop) => changedProperty = prop;

            // StartMinimized
            bool originalMin = service.StartMinimized;
            service.StartMinimized = !originalMin;
            Assert.Equal(nameof(service.StartMinimized), changedProperty);
            Assert.Equal(!originalMin, Global.startMinimized);
            service.StartMinimized = originalMin; // 復元

            // CloseMinimizes
            changedProperty = null;
            bool originalClose = service.CloseMinimizes;
            service.CloseMinimizes = !originalClose;
            Assert.Equal(nameof(service.CloseMinimizes), changedProperty);
            Assert.Equal(!originalClose, Global.closeMinimizes);
            service.CloseMinimizes = originalClose; // 復元

            // CheckWhen
            changedProperty = null;
            int originalCheck = service.CheckWhen;
            service.CheckWhen = originalCheck + 1;
            Assert.Equal(nameof(service.CheckWhen), changedProperty);
            Assert.Equal(originalCheck + 1, Global.CheckWhen);
            service.CheckWhen = originalCheck; // 復元
        }

        [Fact]
        public void GlobalShim_SaveAndLoad_ShouldSynchronizeWithService()
        {
            DS4WinWPF.AppHost.CreateHost();
            var service = DS4WinWPF.AppHost.GetService<IAppSettingsService>();

            Assert.NotNull(service);

            var saveEx = Record.Exception(() => Global.Save());
            Assert.Null(saveEx);

            var loadEx = Record.Exception(() => Global.Load());
            Assert.Null(loadEx);
        }
    }
}
