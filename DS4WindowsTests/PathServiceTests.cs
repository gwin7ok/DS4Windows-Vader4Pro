using System;
using System.IO;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    public class PathServiceTests
    {
        [Fact]
        public void AppDataPath_ShouldResolveValidDirectory()
        {
            var service = new PathService();
            var appData = service.AppDataPath;
            Assert.False(string.IsNullOrWhiteSpace(appData));
        }

        [Fact]
        public void ProfilesPath_ShouldCombineWithAppDataPath()
        {
            var service = new PathService();
            var profiles = service.ProfilesPath;
            Assert.EndsWith("Profiles", profiles, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetProfilePath_ShouldNormalizeXmlExtension()
        {
            var service = new PathService();

            var p1 = service.GetProfilePath("Default");
            var p2 = service.GetProfilePath("Default.xml");

            Assert.EndsWith("Default.xml", p1, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(p1, p2);
            Assert.Equal(string.Empty, service.GetProfilePath(""));
        }

        [Fact]
        public void GlobalShim_ShouldSynchronizeWithService()
        {
            var service = new PathService();
            Global.PathServiceInstance = service;

            Assert.NotNull(Global.PathServiceInstance);
            Assert.Equal(service.ProfilesPath, Global.PathServiceInstance.ProfilesPath);
        }
    }
}
