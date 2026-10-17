using Xunit;
using DS4Windows;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase5-Step14 通知経路統合: AppLogger.LogToTray が
    /// AppLogger.NotificationService.SendNotification へ正しく委譲することを検証する。
    /// </summary>
    public class LogToTrayNotificationBridgeTests
    {
        [Fact]
        public void LogToTray_ShouldDelegateToNotificationService()
        {
            var original = AppLogger.NotificationService;
            var mock = new MockNotificationService();
            try
            {
                AppLogger.NotificationService = mock;

                AppLogger.LogToTray("hello world", warning: false);

                Assert.Single(mock.SendCalls);
                Assert.Equal("hello world", mock.SendCalls[0].Message);
                Assert.False(mock.SendCalls[0].Warning);
                Assert.False(mock.SendCalls[0].Temporary);
            }
            finally
            {
                AppLogger.NotificationService = original;
            }
        }

        [Fact]
        public void LogToTray_ShouldPropagateWarningFlag()
        {
            var original = AppLogger.NotificationService;
            var mock = new MockNotificationService();
            try
            {
                AppLogger.NotificationService = mock;

                AppLogger.LogToTray("warn message", warning: true);

                Assert.Single(mock.SendCalls);
                Assert.True(mock.SendCalls[0].Warning);
            }
            finally
            {
                AppLogger.NotificationService = original;
            }
        }

        [Fact]
        public void LogToTray_ShouldAlwaysPassTemporaryFalse()
        {
            // LogToTray自体にtemporaryパラメータが存在しないため、常にfalseで渡ることを固定する回帰テスト。
            var original = AppLogger.NotificationService;
            var mock = new MockNotificationService();
            try
            {
                AppLogger.NotificationService = mock;

                AppLogger.LogToTray("temp check");

                Assert.Single(mock.SendCalls);
                Assert.False(mock.SendCalls[0].Temporary);
            }
            finally
            {
                AppLogger.NotificationService = original;
            }
        }

        [Fact]
        public void LogToTray_ShouldPassEmptyTitle()
        {
            // タイトルはUI層(MainWindow)側で解決する仕様のため、LogToTrayからは常に空文字を渡す。
            var original = AppLogger.NotificationService;
            var mock = new MockNotificationService();
            try
            {
                AppLogger.NotificationService = mock;

                AppLogger.LogToTray("title check");

                Assert.Single(mock.SendCalls);
                Assert.Equal(string.Empty, mock.SendCalls[0].Title);
            }
            finally
            {
                AppLogger.NotificationService = original;
            }
        }
    }
}