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

        /// <summary>
        /// 通知ウィンドウが次の通知によって消去されずに画面に維持される最小時間（ミリ秒）。
        /// とりあえず1秒（1000ms）に設定。必要に応じてこの定数値を変更してください。
        /// </summary>
        public const int MIN_NOTIFICATION_HOLD_DURATION_MS = 2000;

        private DateTime shownTime = DateTime.MinValue;

        // Windows API for system sound
        [DllImport("user32.dll")]
        private static extern bool MessageBeep(uint uType);

        // Windows API for system sound
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
                // すべての通知を同じ位置（右上）に重ねて表示
                this.Left = workingArea.Right - this.Width - 20;
                this.Top = workingArea.Top + 20;
            }
        }

        public static void ShowNotification(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                DateTime now = DateTime.UtcNow;

                // 新しい通知が来た際、既存の通知が最低1秒（MIN_NOTIFICATION_HOLD_DURATION_MS）は維持されるよう制御
                lock (lockObject)
                {
                    foreach (var oldWin in activeNotifications.ToList())
                    {
                        double elapsedMs = (now - oldWin.shownTime).TotalMilliseconds;
                        int remainingMs = (int)(MIN_NOTIFICATION_HOLD_DURATION_MS - elapsedMs);

                        if (remainingMs <= 0)
                        {
                            // すでに1秒以上経過していれば即座に閉じる
                            oldWin.CloseNotification();
                        }
                        else
                        {
                            // まだ1秒経過していなければ、残り時間を待機してからフェードアウト（最低1秒の表示を保証）
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
                    // リストに追加後に位置を再設定
                    notification.PositionWindow();
                }

                notification.Show();

                // システム音を再生
                MessageBeep(MB_ICONINFORMATION);

                // フェードイン アニメーション
                var fadeIn = notification.FindResource("FadeInStoryboard") as Storyboard;
                fadeIn?.Begin(notification);

                // 3秒後にフェードアウト（次の通知が来なくても3秒で自然消去）
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

                        // すべての通知は同じ位置に重なっているため、位置調整は不要
                        // 残った通知は既に正しい位置にある
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