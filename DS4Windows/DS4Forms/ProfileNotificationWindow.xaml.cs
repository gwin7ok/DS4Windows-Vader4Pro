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
                var notification = new ProfileNotificationWindow();
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