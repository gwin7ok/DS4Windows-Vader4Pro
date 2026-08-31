using System;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    public class NotificationServiceTests
    {
        [Fact]
        public void Defaults_ShouldHaveNotificationsEnabled()
        {
            var service = new AppNotificationService();

            Assert.True(service.NotificationsEnabled);
            Assert.False(service.FlashTaskbar);
        }

        [Fact]
        public void SendNotification_ShouldFireNotificationTriggered_WhenEnabled()
        {
            var service = new AppNotificationService();
            NotificationEventArgs eventArgs = null;
            service.NotificationTriggered += (s, e) => eventArgs = e;

            service.SendNotification("TitleTest", "MessageTest", true);

            Assert.NotNull(eventArgs);
            Assert.Equal("TitleTest", eventArgs.Title);
            Assert.Equal("MessageTest", eventArgs.Message);
            Assert.True(eventArgs.IsToast);
        }

        [Fact]
        public void SendNotification_ShouldNotFire_WhenDisabled()
        {
            var service = new AppNotificationService();
            bool eventFired = false;
            service.NotificationTriggered += (s, e) => eventFired = true;

            service.NotificationsEnabled = false;
            service.SendNotification("TitleTest", "MessageTest");

            Assert.False(eventFired);
        }

        [Fact]
        public void GlobalShim_ShouldSynchronizeWithService()
        {
            var service = new AppNotificationService();
            Global.NotificationServiceInstance = service;

            Assert.NotNull(Global.NotificationServiceInstance);
            Global.NotificationServiceInstance.FlashTaskbar = true;
            Assert.True(service.FlashTaskbar);
        }
    }
}
