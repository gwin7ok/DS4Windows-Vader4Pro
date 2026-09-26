using Xunit;
using DS4Windows;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase5-Step14 通知経路統合: AppLogger.LogToTray が
    /// Global.NotificationServiceInstance.SendNotification へ正しく委譲することを検証する。
    /// </summary>
    public class LogToTrayNotificationBridgeTests
    {
        [Fact]
        public void LogToTray_ShouldDelegateToNotificationServiceInstance()
        {
            var original = Global.NotificationServiceInstance;
            var mock = new MockNotificationService();
            try
            {
                Global.NotificationServiceInstance = mock;

                AppLogger.LogToTray("hello world", warning: false);

                Assert.Single(mock.SendCalls);
                Assert.Equal("hello world", mock.SendCalls[0].Message);
                Assert.False(mock.SendCalls[0].Warning);
                Assert.False(mock.SendCalls[0].Temporary);
            }
            finally
            {
                Global.NotificationServiceInstance = original;
            }
        }

        [Fact]
        public void LogToTray_ShouldPropagateWarningFlag()
        {
            var original = Global.NotificationServiceInstance;
            var mock = new MockNotificationService();
            try
            {
                Global.NotificationServiceInstance = mock;

                AppLogger.LogToTray("warn message", warning: true);

                Assert.Single(mock.SendCalls);
                Assert.True(mock.SendCalls[0].Warning);
            }
            finally
            {
                Global.NotificationServiceInstance = original;
            }
        }

        [Fact]
        public void LogToTray_ShouldAlwaysPassTemporaryFalse()
        {
            // LogToTray自体にtemporaryパラメータが存在しないため、常にfalseで渡ることを固定する回帰テスト。
            var original = Global.NotificationServiceInstance;
            var mock = new MockNotificationService();
            try
            {
                Global.NotificationServiceInstance = mock;

                AppLogger.LogToTray("temp check");

                Assert.Single(mock.SendCalls);
                Assert.False(mock.SendCalls[0].Temporary);
            }
            finally
            {
                Global.NotificationServiceInstance = original;
            }
        }

        [Fact]
        public void LogToTray_ShouldPassEmptyTitle()
        {
            // タイトルはUI層(MainWindow)側で解決する仕様のため、LogToTrayからは常に空文字を渡す。
            var original = Global.NotificationServiceInstance;
            var mock = new MockNotificationService();
            try
            {
                Global.NotificationServiceInstance = mock;

                AppLogger.LogToTray("title check");

                Assert.Single(mock.SendCalls);
                Assert.Equal(string.Empty, mock.SendCalls[0].Title);
            }
            finally
            {
                Global.NotificationServiceInstance = original;
            }
        }
    }
}