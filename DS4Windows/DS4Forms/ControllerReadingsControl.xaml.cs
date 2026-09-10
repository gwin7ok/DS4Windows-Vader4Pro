using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WinWPF.DS4Forms
{
    /// <summary>
    /// Interaction logic for ControllerReadingsControl.xaml
    /// </summary>
    public partial class ControllerReadingsControl : UserControl
    {
        private const int CANVAS_WIDTH = 280;
        private const int CANVAS_HEIGHT = 280;

        private const int AXIS_WIDTH = 256;
        private const int AXIS_HEIGHT = 256;

        private const int LS_AXIS_HALF_WIDTH = AXIS_WIDTH / 2;
        private const int LS_AXIS_HALF_HEIGHT = AXIS_HEIGHT / 2;

        private const int RS_AXIS_HALF_WIDTH = AXIS_WIDTH / 2;
        private const int RS_AXIS_HALF_HEIGHT = AXIS_HEIGHT / 2;

        private DS4Device dev;
        private ControllerReadingsViewModel readingsVM;
        private Line lsXLine;
        private Line lsYLine;
        private Line rsXLine;
        private Line rsYLine;
        private Line sixaxisRecenterLine;
        private NonFormTimer readingTimer;
        private bool useTimer;
        private int _isDrawingActive = 0;

        private enum LatencyWarnMode : uint
        {
            None,
            Warn1,
            Warn2,
        }

        private LatencyWarnMode warnMode = LatencyWarnMode.None;

        public ControllerReadingsControl()
        {
            InitializeComponent();

            lsXLine = new Line()
            {
                X1 = 0,
                X2 = CANVAS_WIDTH,
                Y1 = LS_AXIS_HALF_HEIGHT,
                Y2 = LS_AXIS_HALF_HEIGHT,
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 1.0,
            };

            lsYLine = new Line()
            {
                X1 = LS_AXIS_HALF_WIDTH,
                X2 = LS_AXIS_HALF_WIDTH,
                Y1 = 0,
                Y2 = CANVAS_HEIGHT,
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 1.0,
            };

            rsXLine = new Line()
            {
                X1 = 0,
                X2 = CANVAS_WIDTH,
                Y1 = RS_AXIS_HALF_HEIGHT,
                Y2 = RS_AXIS_HALF_HEIGHT,
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 1.0,
            };

            rsYLine = new Line()
            {
                X1 = RS_AXIS_HALF_WIDTH,
                X2 = RS_AXIS_HALF_WIDTH,
                Y1 = 0,
                Y2 = CANVAS_HEIGHT,
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 1.0,
            };

            sixaxisRecenterLine = new Line()
            {
                X1 = 0,
                X2 = 0,
                Y1 = 0,
                Y2 = gyroRecenterCanvas.Height,
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 2.0,
            };

            lsInputCanvas.Children.Add(lsXLine);
            lsInputCanvas.Children.Add(lsYLine);
            Canvas.SetLeft(lsXLine, 0);
            Canvas.SetTop(lsXLine, 0);
            Canvas.SetLeft(lsYLine, 0);
            Canvas.SetTop(lsYLine, 0);

            rsInputCanvas.Children.Add(rsXLine);
            rsInputCanvas.Children.Add(rsYLine);
            Canvas.SetLeft(rsXLine, 0);
            Canvas.SetTop(rsXLine, 0);
            Canvas.SetLeft(rsYLine, 0);
            Canvas.SetTop(rsYLine, 0);

            gyroRecenterCanvas.Children.Add(sixaxisRecenterLine);
            Canvas.SetLeft(sixaxisRecenterLine, 0);
            Canvas.SetTop(sixaxisRecenterLine, 0);

            InitLate();
        }

        private void InitLate()
        {
            readingsVM = new ControllerReadingsViewModel();
            DataContext = readingsVM;
        }

        public void ChangeDevice(int devIndex)
        {
            if (devIndex < 0 || devIndex >= ControlService.CURRENT_MAX_NUM_DEVICES)
                return;

            DisableControl();
            DS4Device tempDev = Program.rootHub.DS4Controllers[devIndex];
            if (tempDev != null)
            {
                UseDevice(tempDev);
            }
        }

        public void UseDevice(DS4Device tempDev)
        {
            dev = tempDev;
            EnableControl();
        }

        public void EnableControl()
        {
            if (dev != null)
            {
                // 既存タイマーが動いている場合は多重起動を防ぐため確実に停止・破棄
                if (useTimer && readingTimer != null)
                {
                    readingTimer.Stop();
                    readingTimer.Dispose();
                    readingTimer = null;
                    useTimer = false;
                }

                readingTimer = new NonFormTimer();
                // 20fps (50ms) に設定してUIキュー滞留を完全防止
                readingTimer.Interval = 50.0;
                readingTimer.Elapsed += ControllerReadingTimer_Elapsed;
                readingTimer.Start();
                useTimer = true;
            }
        }

        public void DisableControl()
        {
            if (dev != null)
            {
                if (useTimer)
                {
                    readingTimer.Stop();
                    readingTimer.Dispose();
                    readingTimer = null;
                    useTimer = false;
                }

                dev = null;
            }
        }

        private void ControllerReadingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (dev == null)
                return;

            // 前回のUI描画タスクがまだDispatcherで実行中の場合はスキップ（未処理キュー蓄積による遅延フリーズを防止）
            if (Interlocked.CompareExchange(ref _isDrawingActive, 1, 0) != 0)
                return;

            DS4State cState = dev.GetCurrentStateRef();
            DS4StateExposed eState = dev.getExposedState();

            double lsX = cState.LX;
            double lsY = cState.LY;
            double rsX = cState.RX;
            double rsY = cState.RY;

            double gyroX = eState.GyroX;
            double gyroY = eState.GyroY;
            double gyroZ = eState.GyroZ;

            double accelX = eState.AccelX;
            double accelY = eState.AccelY;
            double accelZ = eState.AccelZ;

            int touch0X = 0, touch0Y = 0, touch1X = 0, touch1Y = 0;
            bool touch0Active = false, touch1Active = false;

            if (cState.TrackPadTouch0.IsActive)
            {
                touch0X = cState.TrackPadTouch0.X;
                touch0Y = cState.TrackPadTouch0.Y;
                touch0Active = true;
            }

            if (cState.TrackPadTouch1.IsActive)
            {
                touch1X = cState.TrackPadTouch1.X;
                touch1Y = cState.TrackPadTouch1.Y;
                touch1Active = true;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    Canvas.SetLeft(lsXLine, 0);
                    Canvas.SetTop(lsXLine, lsY);
                    Canvas.SetLeft(lsYLine, lsX);
                    Canvas.SetTop(lsYLine, 0);

                    Canvas.SetLeft(rsXLine, 0);
                    Canvas.SetTop(rsXLine, rsY);
                    Canvas.SetLeft(rsYLine, rsX);
                    Canvas.SetTop(rsYLine, 0);

                    readingsVM.LeftStickX = (int)lsX;
                    readingsVM.LeftStickY = (int)lsY;
                    readingsVM.RightStickX = (int)rsX;
                    readingsVM.RightStickY = (int)rsY;

                    readingsVM.L2 = cState.L2;
                    readingsVM.R2 = cState.R2;

                    readingsVM.GyroX = (int)gyroX;
                    readingsVM.GyroY = (int)gyroY;
                    readingsVM.GyroZ = (int)gyroZ;

                    readingsVM.AccelX = (int)accelX;
                    readingsVM.AccelY = (int)accelY;
                    readingsVM.AccelZ = (int)accelZ;

                    readingsVM.Touch0X = touch0X;
                    readingsVM.Touch0Y = touch0Y;
                    readingsVM.Touch0Active = touch0Active;

                    readingsVM.Touch1X = touch1X;
                    readingsVM.Touch1Y = touch1Y;
                    readingsVM.Touch1Active = touch1Active;

                    readingsVM.Cross = cState.Cross;
                    readingsVM.Circle = cState.Circle;
                    readingsVM.Square = cState.Square;
                    readingsVM.Triangle = cState.Triangle;

                    readingsVM.DpadUp = cState.DpadUp;
                    readingsVM.DpadRight = cState.DpadRight;
                    readingsVM.DpadDown = cState.DpadDown;
                    readingsVM.DpadLeft = cState.DpadLeft;

                    readingsVM.L1 = cState.L1;
                    readingsVM.R1 = cState.R1;
                    readingsVM.L3 = cState.L3;
                    readingsVM.R3 = cState.R3;

                    readingsVM.Share = cState.Share;
                    readingsVM.Options = cState.Options;
                    readingsVM.PS = cState.PS;
                    readingsVM.TouchButton = cState.TouchButton;

                    readingsVM.InputDelay = dev.Latency;
                    if (readingsVM.InputDelay > 10.0)
                    {
                        if (warnMode != LatencyWarnMode.Warn2)
                        {
                            warnMode = LatencyWarnMode.Warn2;
                            inputDelayLb.Foreground = new SolidColorBrush(Colors.Red);
                        }
                    }
                    else if (readingsVM.InputDelay > 5.0)
                    {
                        if (warnMode != LatencyWarnMode.Warn1)
                        {
                            warnMode = LatencyWarnMode.Warn1;
                            inputDelayLb.Foreground = new SolidColorBrush(Colors.Yellow);
                        }
                    }
                    else
                    {
                        if (warnMode != LatencyWarnMode.None)
                        {
                            warnMode = LatencyWarnMode.None;
                            inputDelayLb.Foreground = new SolidColorBrush(Colors.White);
                        }
                    }

                    UpdateCoordLabels();
                }
                finally
                {
                    Interlocked.Exchange(ref _isDrawingActive, 0);
                }
            }));
        }

        private void UpdateCoordLabels()
        {
            sixaxisRecenterLine.X1 = sixaxisRecenterCanvas.ActualWidth / 2.0;
            sixaxisRecenterLine.X2 = sixaxisRecenterLine.X1;
        }

        private void SixaxisRecenterBtn_Click(object sender, RoutedEventArgs e)
        {
            if (dev != null)
            {
                dev.Recenter();
            }
        }
    }
}