using System;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    public class NotificationServiceTests
    {
        [Fact]
        public void Defaults_ShouldSynchronizeWithGlobal()
        {
            var origNotifications = Global.Notifications;
            var origFlash = Global.FlashWhenLate;
            try
            {
                Global.Notifications = 1;
                Global.FlashWhenLate = false;

                var service = new AppNotificationService();

                Assert.True(service.NotificationsEnabled);
                Assert.False(service.FlashTaskbar);
            }
            finally
            {
                Global.Notifications = origNotifications;
                Global.FlashWhenLate = origFlash;
            }
        }

        [Fact]
        public void SendNotification_ShouldFireNotificationTriggered_WhenEnabled()
        {
            var origNotifications = Global.Notifications;
            try
            {
                var service = new AppNotificationService();
                service.NotificationsEnabled = true;

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
                var service = new AppNotificationService();
                service.NotificationsEnabled = true;

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
                var service = new AppNotificationService();
                service.NotificationsEnabled = true;

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
                var service = new AppNotificationService();
                bool eventFired = false;
                service.NotificationTriggered += (s, e) => eventFired = true;

                service.NotificationsEnabled = false;
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

        [Fact]
        public void GlobalShim_ShouldSynchronizeWithService()
        {
            var origFlash = Global.FlashWhenLate;
            try
            {
                var service = new AppNotificationService();
                Global.NotificationServiceInstance = service;

                Assert.NotNull(Global.NotificationServiceInstance);
                Global.NotificationServiceInstance.FlashTaskbar = true;
                Assert.True(service.FlashTaskbar);
                Assert.True(Global.FlashWhenLate);
            }
            finally
            {
                Global.FlashWhenLate = origFlash;
            }
        }

        [Fact]
        public void Service_ShouldDirectlyReflectGlobalChanges()
        {
            var origNotifications = Global.Notifications;
            var origFlash = Global.FlashWhenLate;
            try
            {
                var service = new AppNotificationService();

                Global.Notifications = 0;
                Assert.False(service.NotificationsEnabled);

                Global.Notifications = 1;
                Assert.True(service.NotificationsEnabled);

                Global.FlashWhenLate = true;
                Assert.True(service.FlashTaskbar);

                Global.FlashWhenLate = false;
                Assert.False(service.FlashTaskbar);
            }
            finally
            {
                Global.Notifications = origNotifications;
                Global.FlashWhenLate = origFlash;
            }
        }
    }
}