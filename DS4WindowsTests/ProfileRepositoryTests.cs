using System;
using System.IO;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    public class ProfileRepositoryTests
    {
        private class FakeProfileXmlStore : IProfileXmlStore
        {
            public bool LoadReturnValue = true;
            public bool SaveReturnValue = true;
            public int LoadCallCount;
            public int SaveCallCount;

            public bool LoadProfileXml(int deviceIndex, bool launchProgram, ControlService control,
                string overridePath = "", bool xinputChange = true, bool postLoad = true)
            {
                LoadCallCount++;
                return LoadReturnValue;
            }

            public bool SaveProfileXml(int deviceIndex, string profileName)
            {
                SaveCallCount++;
                return SaveReturnValue;
            }

            public bool LoadAppSettingsXml() => true;
            public bool SaveAppSettingsXml() => true;
        }

        [Fact]
        public void ProfilesPath_ShouldReturnValidPath()
        {
            var settings = new ProfileSettingsService();
            var repository = new ProfileRepository(settings);

            var path = repository.ProfilesPath;
            Assert.False(string.IsNullOrWhiteSpace(path));
            Assert.EndsWith("Profiles", path, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetProfilePath_ShouldAppendXmlExtension()
        {
            var settings = new ProfileSettingsService();
            var repository = new ProfileRepository(settings);

            var path1 = repository.GetProfilePath("Default");
            var path2 = repository.GetProfilePath("Default.xml");

            Assert.EndsWith("Default.xml", path1, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith("Default.xml", path2, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(path1, path2);

            Assert.Equal(string.Empty, repository.GetProfilePath(""));
            Assert.Equal(string.Empty, repository.GetProfilePath(null));
        }

        [Fact]
        public void LoadDefaultProfile_ShouldResetSlotSettings()
        {
            var settings = new ProfileSettingsService();
            var repository = new ProfileRepository(settings);

            settings.SetTouchpadActive(0, false);
            settings.SetUseTempProfile(0, true);

            var result = repository.LoadDefaultProfile(0);
            Assert.True(result);

            Assert.True(settings.GetTouchpadActive(0));
            Assert.False(settings.GetUseTempProfile(0));
        }

        [Fact]
        public void ApplyAndRestoreProfileDirect_ShouldUpdateSettingsService()
        {
            var settings = new ProfileSettingsService();
            var repository = new ProfileRepository(settings);

            repository.ApplyProfileDirect(1, "TemporaryTest");

            Assert.True(settings.GetUseTempProfile(1));
            Assert.Equal("TemporaryTest", settings.GetTempProfileName(1));

            repository.RestoreProfileDirect(1);

            Assert.False(settings.GetUseTempProfile(1));
            Assert.Equal(string.Empty, settings.GetTempProfileName(1));
        }

        [Fact]
        public void GlobalShim_ShouldSynchronizeWithRepository()
        {
            var settings = new ProfileSettingsService();
            var repository = new ProfileRepository(settings);
            Global.ProfileRepositoryInstance = repository;
            Global.ProfileSettingsServiceInstance = settings;

            Assert.NotNull(Global.ProfileRepositoryInstance);
            Assert.Equal(repository.ProfilesPath, Global.ProfileRepositoryInstance.ProfilesPath);

            Global.ProfileRepositoryInstance.ApplyProfileDirect(2, "ShimDirectTest");
            Assert.True(settings.GetUseTempProfile(2));
            Assert.Equal("ShimDirectTest", settings.GetTempProfileName(2));

            Global.ProfileRepositoryInstance.RestoreProfileDirect(2);
            Assert.False(settings.GetUseTempProfile(2));
        }

        [Fact]
        public void LoadProfile_ExistingFile_ShouldDelegateToXmlStoreAndResetTempProfileState()
        {
            DS4WinWPF.AppHost.CreateHost();

            var settings = new ProfileSettingsService();
            var fakeXmlStore = new FakeProfileXmlStore();
            var repository = new ProfileRepository(settings, fakeXmlStore);

            string profileName = "Phase5Step2_LoadProfileTest";
            string path = repository.GetProfilePath(profileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, string.Empty);

            try
            {
                settings.SetUseTempProfile(4, true);
                settings.SetTempProfileName(4, "SomeOldTempProfile");
                settings.SetTempProfileDistance(4, true);

                bool result = repository.LoadProfile(4, profileName);

                Assert.True(result);
                Assert.Equal(1, fakeXmlStore.LoadCallCount);
                Assert.False(settings.GetUseTempProfile(4));
                Assert.Equal(string.Empty, settings.GetTempProfileName(4));
                Assert.False(settings.GetTempProfileDistance(4));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void SaveProfile_ShouldPropagateXmlStoreSuccessResult()
        {
            var settings = new ProfileSettingsService();
            var fakeXmlStore = new FakeProfileXmlStore();
            var repository = new ProfileRepository(settings, fakeXmlStore);

            bool result = repository.SaveProfile(0, "Phase5Step2_SaveProfileTest");
            Assert.True(result);
            Assert.Equal(1, fakeXmlStore.SaveCallCount);

            fakeXmlStore.SaveReturnValue = false;
            bool result2 = repository.SaveProfile(0, "Phase5Step2_SaveProfileTest");
            Assert.False(result2);
        }

        // Phase5-Step13-2で追加: ProfilePath等はGlobal(m_Config)への薄い公開アクセサであり、
        // 同一配列の参照であることを検証する(状態複製がないことの確認)。
        [Fact]
        public void ProfilePath_ShouldReferenceSameArrayAsGlobal()
        {
            var repository = new ProfileRepository(new ProfileSettingsService());
            Assert.Same(Global.ProfilePath, repository.ProfilePath);
            Assert.Same(Global.OlderProfilePath, repository.OlderProfilePath);
            Assert.Same(Global.SelectedProfile, repository.SelectedProfile);
            Assert.Same(Global.LinkedProfileUI, repository.LinkedProfileUI);
        }

        [Fact]
        public void SelectedProfileChanged_ShouldFireOnRaise()
        {
            var repository = new ProfileRepository(new ProfileSettingsService());
            int? receivedDevice = null;
            string receivedProfile = null;
            EventHandler<SelectedProfileChangedEventArgs> handler = (s, e) =>
            {
                receivedDevice = e.DeviceIndex;
                receivedProfile = e.ProfileName;
            };

            repository.SelectedProfileChanged += handler;
            try
            {
                repository.RaiseSelectedProfileChanged(7, "Phase5Step13-9_Test");
                Assert.Equal(7, receivedDevice);
                Assert.Equal("Phase5Step13-9_Test", receivedProfile);
            }
            finally
            {
                repository.SelectedProfileChanged -= handler;
            }
        }

        [Fact]
        public void LinkedProfile_ChangeAndRemove_ShouldNotThrowAndShouldPersist()
        {
            var repository = new ProfileRepository(new ProfileSettingsService());
            const string testSerial = "00:11:22:33:44:99"; // テスト専用の架空シリアル

            try
            {
                var changeEx = Record.Exception(() => repository.ChangeLinkedProfile(testSerial, "Phase5Step13-9_LinkedProfile"));
                Assert.Null(changeEx);

                var saveEx = Record.Exception(() => repository.SaveLinkedProfiles());
                Assert.Null(saveEx);
            }
            finally
            {
                // テスト用シリアルの紐付けを確実に除去してテスト環境を汚さない
                Record.Exception(() => repository.RemoveLinkedProfile(testSerial));
                Record.Exception(() => repository.SaveLinkedProfiles());
            }
        }

        [Fact]
        public void ProfileActions_ShouldReferenceSameArrayAsGlobal()
        {
            var repository = new ProfileRepository(new ProfileSettingsService());
            Assert.Same(Global.ProfileActions, repository.ProfileActions);
        }

        [Fact]
        public void CacheExtraProfileInfo_And_CacheProfileCustomsFlags_ShouldNotThrow()
        {
            var repository = new ProfileRepository(new ProfileSettingsService());

            var ex1 = Record.Exception(() => repository.CacheExtraProfileInfo(7));
            Assert.Null(ex1);

            var ex2 = Record.Exception(() => repository.CacheProfileCustomsFlags(7));
            Assert.Null(ex2);
        }

        [Fact]
        public void EmitMissingActionLogsForDevice_ShouldNotThrow()
        {
            var repository = new ProfileRepository(new ProfileSettingsService());

            var ex = Record.Exception(() => repository.EmitMissingActionLogsForDevice(7, forceEmit: true, overrideProfileName: "Phase5Step13-9_Test"));
            Assert.Null(ex);
        }
    }
}