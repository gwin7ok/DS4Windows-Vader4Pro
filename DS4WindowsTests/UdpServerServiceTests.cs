using System;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    public class UdpServerServiceTests
    {
        [Fact]
        public void InitialState_IsNotRunning()
        {
            var service = new UdpServerService();
            Assert.False(service.IsRunning);
            Assert.Equal(26760, service.Port);
            Assert.Equal("127.0.0.1", service.ListenAddress);
        }

        [Fact]
        public void Start_WithNullControl_ReturnsFalse()
        {
            var service = new UdpServerService();
            bool result = service.Start(null, 26760, "127.0.0.1");

            Assert.False(result);
            Assert.False(service.IsRunning);
        }

        [Fact]
        public void Stop_WhenNotRunning_DoesNotThrow()
        {
            var service = new UdpServerService();

            var ex = Record.Exception(() => service.Stop());

            Assert.Null(ex);
            Assert.False(service.IsRunning);
        }
    }
}
