using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace DS4WinWPF.DS4Forms
{
    public partial class ProfileNotificationWindow : Window
    {
        private static List<ProfileNotificationWindow> activeNotifications = new List<ProfileNotificationWindow>();
        private static readonly object lockObject = new object();

        public const int MIN_NOTIFICATION_HOLD_DURATION_MS = 1000;
        private DateTime shownTime = DateTime.MinValue;

        [DllImport("user32.dll")]
        private static extern bool MessageBeep(uint uType);
        private const uint MB_ICONINFORMATION = 0x00000040;

        public ProfileNotificationWindow()
        {
            InitializeComponent();
            this.Loaded += ProfileNotificationWindow_Loaded;
        }

        private void ProfileNotificationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 位置設定はShowNotificationで行うため、ここでは何もしない
        }

        public void PositionWindow()
        {
            var workingArea = SystemParameters.WorkArea;

            lock (lockObject)
            {
                this.Left = workingArea.Right - this.Width - 20;
                this.Top = workingArea.Top + 20;
            }
        }

        public static void ShowNotification(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                DateTime now = DateTime.UtcNow;

                lock (lockObject)
                {
                    // 既存の古い通知があればクローズ（1秒維持判定）
                    foreach (var oldWin in activeNotifications.ToList())
                    {
                        double elapsedMs = (now - oldWin.shownTime).TotalMilliseconds;
                        int remainingMs = (int)(MIN_NOTIFICATION_HOLD_DURATION_MS - elapsedMs);

                        if (remainingMs <= 0)
                        {
                            oldWin.CloseNotification();
                        }
                        else
                        {
                            Task.Delay(remainingMs).ContinueWith(_ =>
                            {
                                Application.Current?.Dispatcher.Invoke(() =>
                                {
                                    oldWin.CloseNotification();
                                });
                            });
                        }
                    }
                }

                var notification = new ProfileNotificationWindow();
                notification.shownTime = DateTime.UtcNow;
                notification.MessageTextBlock.Text = message;

                lock (lockObject)
                {
                    activeNotifications.Add(notification);
                    notification.PositionWindow();
                }

                notification.Show();

                // システム音を再生
                MessageBeep(MB_ICONINFORMATION);

                // ※注意: XAML側で Loaded 時の FadeInStoryboard が自動実行されるため、
                // ここでの fadeIn?.Begin 二重呼び出し（チラつき・消えかけの原因）は削除済み。

                // 3秒後に自動フェードアウト
                Task.Delay(3000).ContinueWith(t =>
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        notification.CloseNotification();
                    });
                });
            });
        }

        private void CloseNotification()
        {
            var fadeOut = this.FindResource("FadeOutStoryboard") as Storyboard;
            if (fadeOut != null)
            {
                fadeOut.Completed += (s, e) =>
                {
                    lock (lockObject)
                    {
                        activeNotifications.Remove(this);
                    }

                    this.Close();
                };
                fadeOut.Begin(this);
            }
            else
            {
                lock (lockObject)
                {
                    activeNotifications.Remove(this);
                }
                this.Close();
            }
        }
    }
}