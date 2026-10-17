using System;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    public class NotificationServiceTests
    {
        [Fact]
        public void SendNotification_ShouldFireNotificationTriggered_WhenEnabled()
        {
            var origNotifications = Global.Notifications;
            try
            {
                var service = TestRepositoryFactory.CreateAppNotificationService();
                Global.Notifications = 1;

                NotificationEventArgs eventArgs = null;
                service.NotificationTriggered += (s, e) => eventArgs = e;

                service.SendNotification("TitleTest", "MessageTest", isToast: true);

                Assert.NotNull(eventArgs);
                Assert.Equal("TitleTest", eventArgs.Title);
                Assert.Equal("MessageTest", eventArgs.Message);
                Assert.False(eventArgs.Warning);
                Assert.False(eventArgs.Temporary);
                Assert.True(eventArgs.IsToast);
            }
            finally
            {
                Global.Notifications = origNotifications;
            }
        }

        [Fact]
        public void SendNotification_ShouldPropagateWarningFlag()
        {
            var origNotifications = Global.Notifications;
            try
            {
                var service = TestRepositoryFactory.CreateAppNotificationService();
                Global.Notifications = 1;

                NotificationEventArgs eventArgs = null;
                service.NotificationTriggered += (s, e) => eventArgs = e;

                service.SendNotification("TitleTest", "MessageTest", warning: true);

                Assert.NotNull(eventArgs);
                Assert.True(eventArgs.Warning);
                Assert.False(eventArgs.Temporary);
            }
            finally
            {
                Global.Notifications = origNotifications;
            }
        }

        [Fact]
        public void SendNotification_ShouldPropagateTemporaryFlag()
        {
            var origNotifications = Global.Notifications;
            try
            {
                var service = TestRepositoryFactory.CreateAppNotificationService();
                Global.Notifications = 1;

                NotificationEventArgs eventArgs = null;
                service.NotificationTriggered += (s, e) => eventArgs = e;

                service.SendNotification("TitleTest", "MessageTest", temporary: true);

                Assert.NotNull(eventArgs);
                Assert.False(eventArgs.Warning);
                Assert.True(eventArgs.Temporary);
            }
            finally
            {
                Global.Notifications = origNotifications;
            }
        }

        [Fact]
        public void SendNotification_ShouldNotFire_WhenDisabled()
        {
            var origNotifications = Global.Notifications;
            try
            {
                var service = TestRepositoryFactory.CreateAppNotificationService();
                bool eventFired = false;
                service.NotificationTriggered += (s, e) => eventFired = true;

                Global.Notifications = 0;
                // warning/temporary はデフォルト値(false)のまま。シグネチャ拡張後も本テストの意図（無効時は発火しない）に影響しない。
                service.SendNotification("TitleTest", "MessageTest");

                Assert.False(eventFired);
                Assert.Equal(0, Global.Notifications);
            }
            finally
            {
                Global.Notifications = origNotifications;
            }
        }
    }
}