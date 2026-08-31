using System;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    public class EnvironmentServiceTests
    {
        [Fact]
        public void Defaults_ShouldMatchExpected()
        {
            var service = new EnvironmentService();

            Assert.False(service.RunAtStartup);
            Assert.False(service.StartMinimized);
            Assert.False(service.CloseMinimizes);
            Assert.Equal(string.Empty, service.UseLang);
            Assert.Equal(782, service.FormWidth);
            Assert.Equal(550, service.FormHeight);
        }

        [Fact]
        public void MutatingProperty_ShouldFireEnvironmentSettingChanged()
        {
            var service = new EnvironmentService();
            bool eventFired = false;
            service.EnvironmentSettingChanged += (s, e) => eventFired = true;

            service.RunAtStartup = true;
            Assert.True(eventFired);
            Assert.True(service.RunAtStartup);
        }

        [Fact]
        public void GlobalShim_ShouldSynchronizeWithService()
        {
            var service = new EnvironmentService();
            Global.EnvironmentServiceInstance = service;

            Assert.NotNull(Global.EnvironmentServiceInstance);
            service.StartMinimized = true;
            Assert.True(Global.EnvironmentServiceInstance.StartMinimized);
        }
    }
}
