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
        public void Properties_SetAndGet_FiresSettingChangedEvent()
        {
            var mockXmlStore = new MockProfileXmlStore();
            var service = new AppSettingsService(mockXmlStore);
            string changedProperty = null;
            service.SettingChanged += (s, prop) => changedProperty = prop;

            // StartMinimized
            service.StartMinimized = true;
            Assert.Equal(nameof(service.StartMinimized), changedProperty);
            Assert.True(service.StartMinimized);

            // CloseMinimizes
            changedProperty = null;
            service.CloseMinimizes = true;
            Assert.Equal(nameof(service.CloseMinimizes), changedProperty);
            Assert.True(service.CloseMinimizes);

            // CheckWhen
            changedProperty = null;
            service.CheckWhen = 24;
            Assert.Equal(nameof(service.CheckWhen), changedProperty);
            Assert.Equal(24, service.CheckWhen);

            // UdpServerPort
            changedProperty = null;
            service.UdpServerPort = 26761;
            Assert.Equal(nameof(service.UdpServerPort), changedProperty);
            Assert.Equal(26761, service.UdpServerPort);
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

        // Phase5-Step13-9 (回帰テスト): StartMinimized/MinimizeToTaskbar/CloseMinimizes/
        // UseUdpServer/UdpServerPort/UdpServerListenAddress/UseExclusiveModeは、かつて独立した
        // privateフィールドを持ち、Global側の実体と一切連動しない「孤立重複状態」バグがあった
        // （Step13-7調査時に発覚、根本修正済み）。このテストは、サービス経由の書き込みが
        // 実際にGlobal側の同一実体に反映されることを検証し、同種の回帰を防ぐ。
        [Fact]
        public void StartMinimized_ShouldReadWriteSameEntityAsGlobal()
        {
            var service = new AppSettingsService(new MockProfileXmlStore());
            bool original = Global.StartMinimized;
            try
            {
                service.StartMinimized = !original;
                Assert.Equal(!original, Global.StartMinimized);

                Global.StartMinimized = original;
                Assert.Equal(original, service.StartMinimized);
            }
            finally
            {
                Global.StartMinimized = original;
            }
        }

        [Fact]
        public void MinimizeToTaskbar_ShouldReadWriteSameEntityAsGlobal()
        {
            var service = new AppSettingsService(new MockProfileXmlStore());
            bool original = Global.MinToTaskbar;
            try
            {
                service.MinimizeToTaskbar = !original;
                Assert.Equal(!original, Global.MinToTaskbar);
            }
            finally
            {
                Global.MinToTaskbar = original;
            }
        }

        [Fact]
        public void CloseMinimizes_ShouldReadWriteSameEntityAsGlobal()
        {
            var service = new AppSettingsService(new MockProfileXmlStore());
            bool original = Global.CloseMini;
            try
            {
                service.CloseMinimizes = !original;
                Assert.Equal(!original, Global.CloseMini);
            }
            finally
            {
                Global.CloseMini = original;
            }
        }

        [Fact]
        public void UseExclusiveMode_ShouldReadWriteSameEntityAsGlobal()
        {
            var service = new AppSettingsService(new MockProfileXmlStore());
            bool original = Global.UseExclusiveMode;
            try
            {
                service.UseExclusiveMode = !original;
                Assert.Equal(!original, Global.UseExclusiveMode);
            }
            finally
            {
                Global.UseExclusiveMode = original;
            }
        }

        [Fact]
        public void UdpServerSettings_ShouldReadWriteSameEntityAsGlobal()
        {
            var service = new AppSettingsService(new MockProfileXmlStore());
            bool originalUse = Global.isUsingUDPServer();
            int originalPort = Global.getUDPServerPortNum();
            string originalAddr = Global.getUDPServerListenAddress();
            try
            {
                service.UseUdpServer = !originalUse;
                Assert.Equal(!originalUse, Global.isUsingUDPServer());

                service.UdpServerPort = 27000;
                Assert.Equal(27000, Global.getUDPServerPortNum());

                service.UdpServerListenAddress = "192.168.1.1";
                Assert.Equal("192.168.1.1", Global.getUDPServerListenAddress());
            }
            finally
            {
                Global.setUsingUDPServer(originalUse);
                Global.setUDPServerPort(originalPort);
                Global.setUDPServerListenAddress(originalAddr);
            }
        }

        // Phase5-Step13-7 で追加されたウィンドウ位置・サイズ・列幅の永続化プロパティ。
        // 実体はGlobal.FormWidth等(m_Configへの薄い公開アクセサ)であり、全10種のColWidthは
        // 同一パターンのため代表として3種のみ検証する。
        [Fact]
        public void FormGeometry_SetAndGet_FiresSettingChangedEvent()
        {
            var service = new AppSettingsService(new MockProfileXmlStore());
            string changedProperty = null;
            service.SettingChanged += (s, prop) => changedProperty = prop;

            int originalWidth = Global.FormWidth;
            int originalHeight = Global.FormHeight;
            int originalX = Global.FormLocationX;
            int originalY = Global.FormLocationY;
            try
            {
                service.FormWidth = 1024;
                Assert.Equal(nameof(service.FormWidth), changedProperty);
                Assert.Equal(1024, Global.FormWidth);

                changedProperty = null;
                service.FormHeight = 768;
                Assert.Equal(nameof(service.FormHeight), changedProperty);
                Assert.Equal(768, Global.FormHeight);

                changedProperty = null;
                service.FormLocationX = 100;
                Assert.Equal(nameof(service.FormLocationX), changedProperty);
                Assert.Equal(100, Global.FormLocationX);

                changedProperty = null;
                service.FormLocationY = 200;
                Assert.Equal(nameof(service.FormLocationY), changedProperty);
                Assert.Equal(200, Global.FormLocationY);
            }
            finally
            {
                Global.FormWidth = originalWidth;
                Global.FormHeight = originalHeight;
                Global.FormLocationX = originalX;
                Global.FormLocationY = originalY;
            }
        }

        [Fact]
        public void ControllerListColumnWidths_ShouldReadWriteSameEntityAsGlobal()
        {
            var service = new AppSettingsService(new MockProfileXmlStore());
            int originalIndex = Global.ControllerIndexColWidth;
            int originalStatus = Global.ControllerStatusColWidth;
            int originalBattery = Global.ControllerBatteryColWidth;
            try
            {
                service.ControllerIndexColWidth = 55;
                Assert.Equal(55, Global.ControllerIndexColWidth);

                service.ControllerStatusColWidth = 66;
                Assert.Equal(66, Global.ControllerStatusColWidth);

                service.ControllerBatteryColWidth = 77;
                Assert.Equal(77, Global.ControllerBatteryColWidth);
            }
            finally
            {
                Global.ControllerIndexColWidth = originalIndex;
                Global.ControllerStatusColWidth = originalStatus;
                Global.ControllerBatteryColWidth = originalBattery;
            }
        }

        [Fact]
        public void Notifications_SwipeProfiles_ShouldReadWriteSameEntityAsGlobal()
        {
            var service = new AppSettingsService(new MockProfileXmlStore());
            string changedProperty = null;
            service.SettingChanged += (s, prop) => changedProperty = prop;

            int originalNotif = Global.Notifications;
            bool originalSwipe = Global.SwipeProfiles;
            try
            {
                service.Notifications = 2;
                Assert.Equal(nameof(service.Notifications), changedProperty);
                Assert.Equal(2, Global.Notifications);

                changedProperty = null;
                service.SwipeProfiles = !originalSwipe;
                Assert.Equal(nameof(service.SwipeProfiles), changedProperty);
                Assert.Equal(!originalSwipe, Global.SwipeProfiles);
            }
            finally
            {
                Global.Notifications = originalNotif;
                Global.SwipeProfiles = originalSwipe;
            }
        }
    }
}