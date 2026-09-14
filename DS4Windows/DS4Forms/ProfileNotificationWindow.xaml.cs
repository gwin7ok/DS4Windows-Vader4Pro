using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace DS4WinWPF.DS4Forms
{
    public partial class ProfileNotificationWindow : Window
    {
        // 画面上に表示中の単一通知ウィンドウを保持（重複生成・アニメーション衝突を防止）
        private static ProfileNotificationWindow currentWindow = null;
        private static readonly object lockObject = new object();
        private static CancellationTokenSource closeCts = null;

        /// <summary>
        /// 通知ウィンドウが維持される最小時間（ミリ秒）。
        /// </summary>
        public const int MIN_NOTIFICATION_HOLD_DURATION_MS = 1000;

        // Windows API for system sound
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
            // 位置設定はShowNotificationで行うため何もしない
        }

        public void PositionWindow()
        {
            var workingArea = SystemParameters.WorkArea;

            lock (lockObject)
            {
                // 画面右上に配置
                this.Left = workingArea.Right - this.Width - 20;
                this.Top = workingArea.Top + 20;
            }
        }

        /// <summary>
        /// 独自ウィンドウによるプロファイル適用通知を表示します。
        /// 既に通知が表示中の場合は、新しいウィンドウを重ねずにテキストを最新化してタイマーを延長します。
        /// </summary>
        public static void ShowNotification(string message)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                lock (lockObject)
                {
                    // 1. 既に表示中であれば、新しいウィンドウを重ねずにテキストを書き換えてタイマー延長
                    if (currentWindow != null && currentWindow.IsLoaded)
                    {
                        currentWindow.MessageTextBlock.Text = message;

                        closeCts?.Cancel();
                        closeCts = new CancellationTokenSource();
                        var token = closeCts.Token;

                        Task.Delay(3000, token).ContinueWith(t =>
                        {
                            if (!t.IsCanceled)
                            {
                                Application.Current?.Dispatcher.Invoke(() =>
                                {
                                    currentWindow?.CloseNotification();
                                });
                            }
                        }, token);

                        MessageBeep(MB_ICONINFORMATION);
                        return;
                    }

                    // 2. 表示中のウィンドウがなければ新規生成して表示
                    currentWindow = new ProfileNotificationWindow();
                    currentWindow.MessageTextBlock.Text = message;
                    currentWindow.PositionWindow();
                    currentWindow.Show();

                    MessageBeep(MB_ICONINFORMATION);

                    closeCts?.Cancel();
                    closeCts = new CancellationTokenSource();
                    var tokenNew = closeCts.Token;

                    Task.Delay(3000, tokenNew).ContinueWith(t =>
                    {
                        if (!t.IsCanceled)
                        {
                            Application.Current?.Dispatcher.Invoke(() =>
                            {
                                currentWindow?.CloseNotification();
                            });
                        }
                    }, tokenNew);
                }
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
                        if (currentWindow == this)
                        {
                            currentWindow = null;
                        }
                    }
                    this.Close();
                };
                fadeOut.Begin(this);
            }
            else
            {
                lock (lockObject)
                {
                    if (currentWindow == this)
                    {
                        currentWindow = null;
                    }
                }
                this.Close();
            }
        }
    }
}