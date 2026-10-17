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

            // Phase6-Step2-2 (PR-2) / Phase6-Step3-1: IProfileXmlStore の拡張に追随（本テストでは使用しない）
            public bool LoadControllerConfigsForDevice(DS4Device device) => true;
            public bool SaveControllerConfigsForDevice(DS4Device device) => true;
        }

        [Fact]
        public void ProfilesPath_ShouldReturnValidPath()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var repository = TestRepositoryFactory.CreateProfileRepository(settings);

            var path = repository.ProfilesPath;
            Assert.False(string.IsNullOrWhiteSpace(path));
            Assert.EndsWith("Profiles", path, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetProfilePath_ShouldAppendXmlExtension()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var repository = TestRepositoryFactory.CreateProfileRepository(settings);

            var path1 = repository.GetProfilePath("Default");
            var path2 = repository.GetProfilePath("Default.xml");

            Assert.EndsWith("Default.xml", path1, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith("Default.xml", path2, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(path1, path2);

            Assert.Equal(string.Empty, repository.GetProfilePath(""));
            Assert.Equal(string.Empty, repository.GetProfilePath(null));
        }

        [Fact]
        public void ApplyAndRestoreProfileDirect_ShouldUpdateSettingsService()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var repository = TestRepositoryFactory.CreateProfileRepository(settings);

            repository.ApplyProfileDirect(1, "TemporaryTest");

            Assert.True(settings.GetUseTempProfile(1));
            Assert.Equal("TemporaryTest", settings.GetTempProfileName(1));

            repository.RestoreProfileDirect(1);

            Assert.False(settings.GetUseTempProfile(1));
            Assert.Equal(string.Empty, settings.GetTempProfileName(1));
        }

        [Fact]
        public void ApplyAndRestoreProfileDirect_ShouldUpdateTempProfileState()
        {
            // Phase6-Step12 Wave 3: Global.ProfileRepositoryInstance（旧シム）を削除したため、リポジトリを直接使う。
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var repository = TestRepositoryFactory.CreateProfileRepository(settings);

            Assert.NotNull(repository.ProfilesPath);

            repository.ApplyProfileDirect(2, "ShimDirectTest");
            Assert.True(settings.GetUseTempProfile(2));
            Assert.Equal("ShimDirectTest", settings.GetTempProfileName(2));

            repository.RestoreProfileDirect(2);
            Assert.False(settings.GetUseTempProfile(2));
        }

        [Fact]
        public void LoadProfile_ExistingFile_ShouldDelegateToXmlStoreAndResetTempProfileState()
        {
            DS4WinWPF.AppHost.CreateHost();

            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var fakeXmlStore = new FakeProfileXmlStore();
            var repository = TestRepositoryFactory.CreateProfileRepository(settings, fakeXmlStore);

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
                Assert.False(settings.TempProfileDistanceArray[4]);
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
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var fakeXmlStore = new FakeProfileXmlStore();
            var repository = TestRepositoryFactory.CreateProfileRepository(settings, fakeXmlStore);

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
            var repository = TestRepositoryFactory.CreateProfileRepository(TestRepositoryFactory.CreateProfileSettingsService());
            Assert.Same(Global.store.profilePath, repository.ProfilePath);
            Assert.Same(Global.store.olderProfilePath, repository.OlderProfilePath);
            Assert.Same(Global.store.selectedProfile, repository.SelectedProfile);
            Assert.Same(Global.store.linkedProfileUI, repository.LinkedProfileUI);
        }

        [Fact]
        public void SelectedProfileChanged_ShouldFireOnRaise()
        {
            var repository = TestRepositoryFactory.CreateProfileRepository(TestRepositoryFactory.CreateProfileSettingsService());
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
            var repository = TestRepositoryFactory.CreateProfileRepository(TestRepositoryFactory.CreateProfileSettingsService());
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
            var repository = TestRepositoryFactory.CreateProfileRepository(TestRepositoryFactory.CreateProfileSettingsService());
            Assert.Same(Global.store.profileActions, repository.ProfileActions);
        }

        [Fact]
        public void CacheExtraProfileInfo_And_CacheProfileCustomsFlags_ShouldNotThrow()
        {
            var repository = TestRepositoryFactory.CreateProfileRepository(TestRepositoryFactory.CreateProfileSettingsService());

            var ex1 = Record.Exception(() => repository.CacheExtraProfileInfo(7));
            Assert.Null(ex1);

            var ex2 = Record.Exception(() => repository.CacheProfileCustomsFlags(7));
            Assert.Null(ex2);
        }

        [Fact]
        public void EmitMissingActionLogsForDevice_ShouldNotThrow()
        {
            var repository = TestRepositoryFactory.CreateProfileRepository(TestRepositoryFactory.CreateProfileSettingsService());

            var ex = Record.Exception(() => repository.EmitMissingActionLogsForDevice(7, forceEmit: true, overrideProfileName: "Phase5Step13-9_Test"));
            Assert.Null(ex);
        }
    }
}