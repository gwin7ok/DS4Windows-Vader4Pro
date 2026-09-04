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
            Assert.False(string.IsNullOrWhiteSpace(service.AppDataPath));
        }

        [Fact]
        public void AppDataPath_OnDemandEvaluation_ReflectsGlobalChangesDynamically()
        {
            var service = new PathService();
            var originalInstance = Global.PathServiceInstance;
            Global.PathServiceInstance = service;

            try
            {
                Global.appdatapath = @"C:\TestDynamicPath1";
                Assert.Equal(@"C:\TestDynamicPath1", service.AppDataPath);

                Global.appdatapath = @"C:\TestDynamicPath2";
                Assert.Equal(@"C:\TestDynamicPath2", service.AppDataPath);
            }
            finally
            {
                Global.PathServiceInstance = originalInstance;
            }
        }

        [Fact]
        public void ProfilesPath_ShouldCombineWithAppDataPath()
        {
            var service = new PathService(@"C:\TestAppPath");
            Assert.Equal(@"C:\TestAppPath\Profiles", service.ProfilesPath);
        }

        [Fact]
        public void GetProfilePath_ShouldNormalizeXmlExtension()
        {
            var service = new PathService(@"C:\TestAppPath");
            Assert.Equal(@"C:\TestAppPath\Profiles\Test.xml", service.GetProfilePath("Test"));
            Assert.Equal(@"C:\TestAppPath\Profiles\Test.xml", service.GetProfilePath("Test.xml"));
            Assert.Equal(string.Empty, service.GetProfilePath(null));
            Assert.Equal(string.Empty, service.GetProfilePath(""));
        }

        [Fact]
        public void GlobalShim_ShouldSynchronizeWithService()
        {
            var service = new PathService(@"C:\TestAppPath");
            Global.PathServiceInstance = service;

            Assert.NotNull(Global.PathServiceInstance);
            Assert.Equal(@"C:\TestAppPath\Profiles", Global.PathServiceInstance.ProfilesPath);
        }
    }
}
