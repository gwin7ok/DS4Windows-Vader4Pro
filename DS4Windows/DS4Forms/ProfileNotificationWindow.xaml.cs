using System;
using System.Collections.Generic;
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
            // 画面右下に配置
            PositionWindow();
        }

        private void PositionWindow()
        {
            var workingArea = SystemParameters.WorkArea;
            
            lock (lockObject)
            {
                // 既存の通知ウィンドウの数に基づいて位置を計算
                int notificationIndex = activeNotifications.Count;
                
                this.Left = workingArea.Right - this.Width - 20;
                this.Top = workingArea.Bottom - (this.Height + 10) * (notificationIndex + 1) - 20;
            }
        }

        public static void ShowNotification(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var notification = new ProfileNotificationWindow();
                notification.MessageTextBlock.Text = message;
                
                lock (lockObject)
                {
                    activeNotifications.Add(notification);
                }
                
                notification.Show();
                
                // システム音を再生
                MessageBeep(MB_ICONINFORMATION);
                
                // フェードイン アニメーション
                var fadeIn = notification.FindResource("FadeInStoryboard") as Storyboard;
                fadeIn?.Begin(notification);
                
                // 3秒後にフェードアウト
                Task.Delay(3000).ContinueWith(t =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
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
                        
                        // 残りの通知ウィンドウの位置を再調整
                        for (int i = 0; i < activeNotifications.Count; i++)
                        {
                            var workingArea = SystemParameters.WorkArea;
                            activeNotifications[i].Left = workingArea.Right - activeNotifications[i].Width - 20;
                            activeNotifications[i].Top = workingArea.Bottom - (activeNotifications[i].Height + 10) * (i + 1) - 20;
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
                    activeNotifications.Remove(this);
                }
                this.Close();
            }
        }
    }
}