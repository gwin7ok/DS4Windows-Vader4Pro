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

            // Phase6-Step2-2 (PR-2) / Phase6-Step3-1: IProfileXmlStore の拡張に追随（本テストでは使用しない）
            public bool LoadControllerConfigsForDevice(DS4Device device) => true;
            public bool SaveControllerConfigsForDevice(DS4Device device) => true;

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
            var service = TestRepositoryFactory.CreateAppSettingsService(mockXmlStore);

            bool result = service.Save();

            Assert.True(result);
            Assert.Equal(1, mockXmlStore.SaveCallCount);
        }

        [Fact]
        public void Save_DelegatesToXmlStore_ReturnsFalseOnFailure()
        {
            var mockXmlStore = new MockProfileXmlStore { SaveAppSettingsResult = false };
            var service = TestRepositoryFactory.CreateAppSettingsService(mockXmlStore);

            bool result = service.Save();

            Assert.False(result);
            Assert.Equal(1, mockXmlStore.SaveCallCount);
        }

        [Fact]
        public void Load_DelegatesToXmlStore_ReturnsTrueOnSuccess()
        {
            var mockXmlStore = new MockProfileXmlStore { LoadAppSettingsResult = true };
            var service = TestRepositoryFactory.CreateAppSettingsService(mockXmlStore);

            bool result = service.Load();

            Assert.True(result);
            Assert.Equal(1, mockXmlStore.LoadCallCount);
        }

        [Fact]
        public void Properties_SetAndGet_FiresSettingChangedEvent()
        {
            var mockXmlStore = new MockProfileXmlStore();
            var service = TestRepositoryFactory.CreateAppSettingsService(mockXmlStore);
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

            var saveEx = Record.Exception(() => service.Save());
            Assert.Null(saveEx);

            // Phase7-Step4 W2: Global.Load は削除した。サービスの Load が同じ処理（旧 Global.Load の中身）
            var loadEx = Record.Exception(() => service.Load());
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
            var service = TestRepositoryFactory.CreateAppSettingsService(new MockProfileXmlStore());
            bool original = Global.store.startMinimized;
            try
            {
                service.StartMinimized = !original;
                Assert.Equal(!original, Global.store.startMinimized);

                Global.store.startMinimized = original;
                Assert.Equal(original, service.StartMinimized);
            }
            finally
            {
                Global.store.startMinimized = original;
            }
        }

        [Fact]
        public void MinimizeToTaskbar_ShouldReadWriteSameEntityAsGlobal()
        {
            var service = TestRepositoryFactory.CreateAppSettingsService(new MockProfileXmlStore());
            bool original = Global.store.minToTaskbar;
            try
            {
                service.MinimizeToTaskbar = !original;
                Assert.Equal(!original, Global.store.minToTaskbar);
            }
            finally
            {
                Global.store.minToTaskbar = original;
            }
        }

        [Fact]
        public void CloseMinimizes_ShouldReadWriteSameEntityAsGlobal()
        {
            var service = TestRepositoryFactory.CreateAppSettingsService(new MockProfileXmlStore());
            bool original = Global.store.closeMini;
            try
            {
                service.CloseMinimizes = !original;
                Assert.Equal(!original, Global.store.closeMini);
            }
            finally
            {
                Global.store.closeMini = original;
            }
        }

        [Fact]
        public void UseExclusiveMode_ShouldReadWriteSameEntityAsGlobal()
        {
            var service = TestRepositoryFactory.CreateAppSettingsService(new MockProfileXmlStore());
            bool original = Global.store.useExclusiveMode;
            try
            {
                service.UseExclusiveMode = !original;
                Assert.Equal(!original, Global.store.useExclusiveMode);
            }
            finally
            {
                Global.store.useExclusiveMode = original;
            }
        }

        [Fact]
        public void UdpServerSettings_ShouldReadWriteSameEntityAsGlobal()
        {
            var service = TestRepositoryFactory.CreateAppSettingsService(new MockProfileXmlStore());
            bool originalUse = Global.store.useUDPServ;
            int originalPort = Global.store.udpServPort;
            string originalAddr = Global.store.udpServListenAddress;
            try
            {
                service.UseUdpServer = !originalUse;
                Assert.Equal(!originalUse, Global.store.useUDPServ);

                service.UdpServerPort = 27000;
                Assert.Equal(27000, Global.store.udpServPort);

                service.UdpServerListenAddress = "192.168.1.1";
                Assert.Equal("192.168.1.1", Global.store.udpServListenAddress);
            }
            finally
            {
                Global.store.useUDPServ = originalUse;
                Global.store.udpServPort = originalPort;
                Global.store.udpServListenAddress = originalAddr;
            }
        }

        // Phase5-Step13-7 で追加されたウィンドウ位置・サイズ・列幅の永続化プロパティ。
        // 実体はGlobal.FormWidth等(m_Configへの薄い公開アクセサ)であり、全10種のColWidthは
        // 同一パターンのため代表として3種のみ検証する。
        [Fact]
        public void FormGeometry_SetAndGet_FiresSettingChangedEvent()
        {
            var service = TestRepositoryFactory.CreateAppSettingsService(new MockProfileXmlStore());
            string changedProperty = null;
            service.SettingChanged += (s, prop) => changedProperty = prop;

            int originalWidth = Global.store.formWidth;
            int originalHeight = Global.store.formHeight;
            int originalX = Global.store.formLocationX;
            int originalY = Global.store.formLocationY;
            try
            {
                service.FormWidth = 1024;
                Assert.Equal(nameof(service.FormWidth), changedProperty);
                Assert.Equal(1024, Global.store.formWidth);

                changedProperty = null;
                service.FormHeight = 768;
                Assert.Equal(nameof(service.FormHeight), changedProperty);
                Assert.Equal(768, Global.store.formHeight);

                changedProperty = null;
                service.FormLocationX = 100;
                Assert.Equal(nameof(service.FormLocationX), changedProperty);
                Assert.Equal(100, Global.store.formLocationX);

                changedProperty = null;
                service.FormLocationY = 200;
                Assert.Equal(nameof(service.FormLocationY), changedProperty);
                Assert.Equal(200, Global.store.formLocationY);
            }
            finally
            {
                Global.store.formWidth = originalWidth;
                Global.store.formHeight = originalHeight;
                Global.store.formLocationX = originalX;
                Global.store.formLocationY = originalY;
            }
        }

        [Fact]
        public void ControllerListColumnWidths_ShouldReadWriteSameEntityAsGlobal()
        {
            var service = TestRepositoryFactory.CreateAppSettingsService(new MockProfileXmlStore());
            int originalIndex = Global.store.controllerIndexColWidth;
            int originalStatus = Global.store.controllerStatusColWidth;
            int originalBattery = Global.store.controllerBatteryColWidth;
            try
            {
                service.ControllerIndexColWidth = 55;
                Assert.Equal(55, Global.store.controllerIndexColWidth);

                service.ControllerStatusColWidth = 66;
                Assert.Equal(66, Global.store.controllerStatusColWidth);

                service.ControllerBatteryColWidth = 77;
                Assert.Equal(77, Global.store.controllerBatteryColWidth);
            }
            finally
            {
                Global.store.controllerIndexColWidth = originalIndex;
                Global.store.controllerStatusColWidth = originalStatus;
                Global.store.controllerBatteryColWidth = originalBattery;
            }
        }

        [Fact]
        public void Notifications_SwipeProfiles_ShouldReadWriteSameEntityAsGlobal()
        {
            var service = TestRepositoryFactory.CreateAppSettingsService(new MockProfileXmlStore());
            string changedProperty = null;
            service.SettingChanged += (s, prop) => changedProperty = prop;

            int originalNotif = Global.Notifications;
            bool originalSwipe = Global.store.swipeProfiles;
            try
            {
                // デフォルト値(2)と衝突しないよう、元の値と異なる値を明示的に選ぶ
                int newNotif = originalNotif == 1 ? 0 : 1;
                service.Notifications = newNotif;
                Assert.Equal(nameof(service.Notifications), changedProperty);
                Assert.Equal(newNotif, Global.Notifications);

                changedProperty = null;
                service.SwipeProfiles = !originalSwipe;
                Assert.Equal(nameof(service.SwipeProfiles), changedProperty);
                Assert.Equal(!originalSwipe, Global.store.swipeProfiles);
            }
            finally
            {
                Global.Notifications = originalNotif;
                Global.store.swipeProfiles = originalSwipe;
            }
        }
    }
}