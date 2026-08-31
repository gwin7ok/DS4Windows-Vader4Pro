using System;
using System.IO;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    public class ProfileRepositoryTests
    {
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

            // Apply temporary profile
            repository.ApplyProfileDirect(1, "TemporaryTest");

            Assert.True(settings.GetUseTempProfile(1));
            Assert.Equal("TemporaryTest", settings.GetTempProfileName(1));

            // Restore profile
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
    }
}
