/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using NonFormTimer = System.Timers.Timer;
using DS4Windows;

namespace DS4WinWPF.DS4Forms
{
    /// <summary>
    /// Interaction logic for ControllerReadingsControl.xaml
    /// </summary>
    public partial class ControllerReadingsControl : UserControl
    {
        private enum LatencyWarnMode : uint
        {
            None,
            Caution,
            Warn,
        }

        private int deviceNum;
        private int profileDeviceNum;
        private event EventHandler DeviceNumChanged;
        private NonFormTimer readingTimer;
        private bool useTimer;
        private double lsDeadX;
        private double lsDeadY;
        private double rsDeadX;
        private double rsDeadY;

        private double sixAxisXDead;
        private double sixAxisZDead;
        private double l2Dead;
        private double r2Dead;

        private sbyte lsDriftX;
        private sbyte lsDriftY;
        private sbyte rsDriftX;
        private sbyte rsDriftY;

        public double LsDeadX
        {
            get => lsDeadX;
            set
            {
                lsDeadX = value;
                LsDeadXChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler LsDeadXChanged;

        public double LsDeadY
        {
            get => lsDeadY;
            set
            {
                lsDeadY = value;
                LsDeadYChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler LsDeadYChanged;

        public double RsDeadX
        {
            get => rsDeadX;
            set
            {
                rsDeadX = value;
                RsDeadXChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler RsDeadXChanged;

        public double RsDeadY
        {
            get => rsDeadY;
            set
            {
                rsDeadY = value;
                RsDeadYChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler RsDeadYChanged;

        public double SixAxisXDead
        {
            get => sixAxisXDead;
            set
            {
                sixAxisXDead = value;
                SixAxisDeadXChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler SixAxisDeadXChanged;

        public double SixAxisZDead
        {
            get => sixAxisZDead;
            set
            {
                sixAxisZDead = value;
                SixAxisDeadZChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler SixAxisDeadZChanged;

        public double L2Dead
        {
            get => l2Dead;
            set
            {
                l2Dead = value;
                L2DeadChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler L2DeadChanged;

        public double R2Dead
        {
            get => r2Dead;
            set
            {
                r2Dead = value;
                R2DeadChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler R2DeadChanged;


        public sbyte LsDriftX
        {
            get => lsDriftX;
            set
            {
                lsDriftX = value;
                LsDriftXChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler LsDriftXChanged;

        public sbyte LsDriftY
        {
            get => lsDriftY;
            set
            {
                lsDriftY = value;
                LsDriftYChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler LsDriftYChanged;

        public sbyte RsDriftX
        {
            get => rsDriftX;
            set
            {
                rsDriftX = value;
                RsDriftXChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler RsDriftXChanged;

        public sbyte RsDriftY
        {
            get => rsDriftY;
            set
            {
                rsDriftY = value;
                RsDriftYChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler RsDriftYChanged;


        private DS4State baseState = new DS4State();
        private DS4State interState = new DS4State();
        private DS4StateExposed exposeState;
        private Label outputDelayLabel; // XAMLコントロール参照キャッシュ
        private const int CANVAS_WIDTH = 130;
        private const int CANVAS_MIDPOINT = CANVAS_WIDTH / 2;
        private const double TRIG_LB_TRANSFORM_OFFSETY = 66.0;

        // UI過剰描画・キュー滞留防止用のキャッシュフィールド
        private bool isUiUpdating = false;
        private int lastInLX = -1, lastInLY = -1, lastOutLX = -1, lastOutLY = -1;
        private int lastInRX = -1, lastInRY = -1, lastOutRX = -1, lastOutRY = -1;
        private int lastInL2 = -1, lastOutL2 = -1, lastInR2 = -1, lastOutR2 = -1;
        private int lastAccelX = -999, lastAccelY = -999, lastAccelZ = -999;
        private double lastOutAccelX = -999, lastOutAccelZ = -999;
        private int lastGyroYaw = -99999, lastGyroPitch = -99999, lastGyroRoll = -99999;
        private int lastTouchX = -1, lastTouchY = -1;
        private int lastBattery = -1;
        private int lastLatencyInt = -1;
        private int lastProcDelayInt = -1;
        private long lastCalibrating = -1;

        // Phase5-Step15-2-b: Program.rootHub直接参照を廃止し、他View/ViewModelと同じDIフォールバックパターンを導入する。
        // 動作は完全に同一（フォールバック先が同じProgram.rootHubのため）で、実行時の挙動に変化はない。
        private readonly DS4Windows.ControlService controlService;

        public ControllerReadingsControl()
        {
            controlService = DS4WinWPF.AppHost.GetService<DS4Windows.ControlService>() ?? Program.rootHub;
            InitializeComponent();
            inputContNum.Content = $"#{deviceNum + 1}";
            exposeState = new DS4StateExposed(baseState);

            readingTimer = new NonFormTimer();
            readingTimer.Interval = 66.0; // 約15fps (66ms): 診断用として十分滑らかかつCPU負荷を最小化

            LsDeadXChanged += ChangeLsDeadControls;
            LsDeadYChanged += ChangeLsDeadControls;
            LsDeadXChanged += ChangeLsDriftControls;
            LsDeadYChanged += ChangeLsDriftControls;

            RsDeadXChanged += ChangeRsDeadControls;
            RsDeadYChanged += ChangeRsDeadControls;
            RsDeadXChanged += ChangeRsDriftControls;
            RsDeadYChanged += ChangeRsDriftControls;

            LsDriftXChanged += ChangeLsDriftControls;
            LsDriftYChanged += ChangeLsDriftControls;
            RsDriftXChanged += ChangeRsDriftControls;
            RsDriftYChanged += ChangeRsDriftControls;

            SixAxisDeadXChanged += ChangeSixAxisDeadControls;
            SixAxisDeadZChanged += ChangeSixAxisDeadControls;

            DeviceNumChanged += ControllerReadingsControl_DeviceNumChanged;
        }

        private void ControllerReadingsControl_DeviceNumChanged(object sender, EventArgs e)
        {
            inputContNum.Content = $"#{deviceNum + 1}";
        }

        private void ChangeSixAxisDeadControls(object sender, EventArgs e)
        {
            sixAxisDeadEllipse.Width = sixAxisXDead * CANVAS_WIDTH;
            sixAxisDeadEllipse.Height = sixAxisZDead * CANVAS_WIDTH;
            Canvas.SetLeft(sixAxisDeadEllipse, CANVAS_MIDPOINT - (sixAxisXDead * CANVAS_WIDTH / 2.0));
            Canvas.SetTop(sixAxisDeadEllipse, CANVAS_MIDPOINT - (sixAxisZDead * CANVAS_WIDTH / 2.0));
        }

        private void ChangeRsDriftControls(object sender, EventArgs e)
        {
            rsDriftEllipse.Width = rsDeadX * CANVAS_WIDTH;
            rsDriftEllipse.Height = rsDeadY * CANVAS_WIDTH;
            Canvas.SetLeft(rsDriftEllipse, (1 + (RsDriftX / 127.0) - rsDeadX) * CANVAS_MIDPOINT);
            Canvas.SetTop(rsDriftEllipse, (1 + (RsDriftY / 127.0) - rsDeadY) * CANVAS_MIDPOINT);
        }

        private void ChangeLsDriftControls(object sender, EventArgs e)
        {
            lsDriftEllipse.Width = lsDeadX * CANVAS_WIDTH;
            lsDriftEllipse.Height = lsDeadY * CANVAS_WIDTH;
            Canvas.SetLeft(lsDriftEllipse, (1 + (LsDriftX / 127.0) - lsDeadX) * CANVAS_MIDPOINT);
            Canvas.SetTop(lsDriftEllipse, (1 + (LsDriftY / 127.0) - lsDeadY) * CANVAS_MIDPOINT);
        }

        private void ChangeRsDeadControls(object sender, EventArgs e)
        {
            rsDeadEllipse.Width = rsDeadX * CANVAS_WIDTH;
            rsDeadEllipse.Height = rsDeadY * CANVAS_WIDTH;
            Canvas.SetLeft(rsDeadEllipse, CANVAS_MIDPOINT - (rsDeadX * CANVAS_WIDTH / 2.0));
            Canvas.SetTop(rsDeadEllipse, CANVAS_MIDPOINT - (rsDeadX * CANVAS_WIDTH / 2.0));
        }

        private void ChangeLsDeadControls(object sender, EventArgs e)
        {
            lsDeadEllipse.Width = lsDeadX * CANVAS_WIDTH;
            lsDeadEllipse.Height = lsDeadY * CANVAS_WIDTH;
            Canvas.SetLeft(lsDeadEllipse, CANVAS_MIDPOINT - (lsDeadX * CANVAS_WIDTH / 2.0));
            Canvas.SetTop(lsDeadEllipse, CANVAS_MIDPOINT - (lsDeadX * CANVAS_WIDTH / 2.0));
        }

        public void UseDevice(int index, int profileDevIdx)
        {
            deviceNum = index;
            profileDeviceNum = profileDevIdx;
            DeviceNumChanged?.Invoke(this, EventArgs.Empty);
        }

        public void EnableControl(bool state)
        {
            // 多重登録防止のため、一旦イベントを解除してから制御
            readingTimer.Elapsed -= ControllerReadingTimer_Elapsed;

            if (state)
            {
                IsEnabled = true;
                useTimer = true;
                if (controlService != null)
                {
                    controlService.IsMeasuringProcessingDelay = true;
                }
                readingTimer.Elapsed += ControllerReadingTimer_Elapsed;
                readingTimer.Start();
            }
            else
            {
                IsEnabled = false;
                useTimer = false;
                if (controlService != null)
                {
                    controlService.IsMeasuringProcessingDelay = false;
                }
                readingTimer.Stop();
                isUiUpdating = false;
            }
        }

        private void ControllerReadingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            readingTimer.Stop();

            // 前回のUI更新処理がまだ完了していない場合はスキップし、キューの滞留を防止
            if (isUiUpdating)
            {
                if (useTimer) readingTimer.Start();
                return;
            }

            DS4Device ds = controlService?.DS4Controllers != null && deviceNum < controlService.DS4Controllers.Length
                ? controlService.DS4Controllers[deviceNum]
                : null;

            if (ds != null && ds.IsAlive())
            {
                DS4State tmpbaseState = controlService.getDS4State(deviceNum);
                DS4State tmpinterState = controlService.getDS4StateTemp(deviceNum);
                long cntCalibrating = ds.SixAxis.CntCalibrating;

                // タイムアウト付き待機 (最大10ms) でブロッキングを防止
                if (ds.ReadWaitEv.Wait(10))
                {
                    try
                    {
                        ds.ReadWaitEv.Reset();

                        // 内部状態の高速メモリコピー
                        tmpbaseState.CopyTo(baseState);
                        tmpinterState.CopyTo(interState);

                        if (deviceNum != profileDeviceNum)
                            Mapping.SetCurveAndDeadzone(profileDeviceNum, baseState, interState);
                    }
                    finally
                    {
                        ds.ReadWaitEv.Set();
                    }

                    isUiUpdating = true;

                    // UIスレッドへのディスパッチを非同期・Background優先度にすることで、
                    // マウスやWindowsの描画メッセージを最優先させ、OS全体の重さを解消
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        try
                        {
                            UpdateUiControls(ds, cntCalibrating);
                        }
                        finally
                        {
                            isUiUpdating = false;
                        }
                    }));
                }
            }

            if (useTimer)
            {
                readingTimer.Start();
            }
        }

        /// <summary>
        /// 値が変化したコントロールのみをピンポイントで更新する差分描画処理
        /// </summary>
        private void UpdateUiControls(DS4Device ds, long cntCalibrating)
        {
            int inLx = baseState.LX;
            int inLy = baseState.LY;
            int outLx = interState.LX;
            int outLy = interState.LY;

            // 左スティック Canvas & ラベル差分更新
            if (inLx != lastInLX || inLy != lastInLY || outLx != lastOutLX || outLy != lastOutLY)
            {
                if (inLx != lastInLX || inLy != lastInLY)
                {
                    Canvas.SetLeft(lsValRec, inLx / 255.0 * CANVAS_WIDTH - 3);
                    Canvas.SetTop(lsValRec, inLy / 255.0 * CANVAS_WIDTH - 3);
                    lxInValLb.Content = inLx;
                    lyInValLb.Content = inLy;
                    lastInLX = inLx;
                    lastInLY = inLy;
                }
                if (outLx != lastOutLX || outLy != lastOutLY)
                {
                    Canvas.SetLeft(lsMapValRec, outLx / 255.0 * CANVAS_WIDTH - 3);
                    Canvas.SetTop(lsMapValRec, outLy / 255.0 * CANVAS_WIDTH - 3);
                    lxOutValLb.Content = outLx;
                    lyOutValLb.Content = outLy;
                    lastOutLX = outLx;
                    lastOutLY = outLy;
                }
            }

            int inRx = baseState.RX;
            int inRy = baseState.RY;
            int outRx = interState.RX;
            int outRy = interState.RY;

            // 右スティック Canvas & ラベル差分更新
            if (inRx != lastInRX || inRy != lastInRY || outRx != lastOutRX || outRy != lastOutRY)
            {
                if (inRx != lastInRX || inRy != lastInRY)
                {
                    Canvas.SetLeft(rsValRec, inRx / 255.0 * CANVAS_WIDTH - 3);
                    Canvas.SetTop(rsValRec, inRy / 255.0 * CANVAS_WIDTH - 3);
                    rxInValLb.Content = inRx;
                    ryInValLb.Content = inRy;
                    lastInRX = inRx;
                    lastInRY = inRy;
                }
                if (outRx != lastOutRX || outRy != lastOutRY)
                {
                    Canvas.SetLeft(rsMapValRec, outRx / 255.0 * CANVAS_WIDTH - 3);
                    Canvas.SetTop(rsMapValRec, outRy / 255.0 * CANVAS_WIDTH - 3);
                    rxOutValLb.Content = outRx;
                    ryOutValLb.Content = outRy;
                    lastOutRX = outRx;
                    lastOutLY = outRy;
                }
            }

            // SixAxis / ジャイロ差分更新
            int accelX = exposeState.getAccelX();
            int accelY = exposeState.getAccelY();
            int accelZ = exposeState.getAccelZ();
            double outAccelX = interState.Motion.outputAccelX;
            double outAccelZ = interState.Motion.outputAccelZ;

            if (accelX != lastAccelX || accelZ != lastAccelZ || Math.Abs(outAccelX - lastOutAccelX) > 0.01 || Math.Abs(outAccelZ - lastOutAccelZ) > 0.01)
            {
                int posX = accelX + 127;
                int posZ = accelZ + 127;
                Canvas.SetLeft(sixAxisValRec, posX / 255.0 * CANVAS_WIDTH - 3);
                Canvas.SetTop(sixAxisValRec, posZ / 255.0 * CANVAS_WIDTH - 3);
                Canvas.SetLeft(sixAxisMapValRec, Math.Min(Math.Max(outAccelX + 127.0, 0), 255.0) / 255.0 * CANVAS_WIDTH - 3);
                Canvas.SetTop(sixAxisMapValRec, Math.Min(Math.Max(outAccelZ + 127.0, 0), 255.0) / 255.0 * CANVAS_WIDTH - 3);

                sixAxisXInValLb.Content = exposeState.AccelX;
                sixAxisXOutValLb.Content = (int)outAccelX;
                sixAxisZInValLb.Content = exposeState.AccelZ;
                sixAxisZOutValLb.Content = (int)outAccelZ;

                accelXSlider.Value = accelX;
                accelYSlider.Value = accelY;
                accelZSlider.Value = accelZ;

                lastAccelX = accelX;
                lastAccelY = accelY;
                lastAccelZ = accelZ;
                lastOutAccelX = outAccelX;
                lastOutAccelZ = outAccelZ;
            }

            // L2 トリガー差分更新
            int inL2 = baseState.L2;
            int outL2 = interState.L2;
            if (inL2 != lastInL2 || outL2 != lastOutL2)
            {
                l2Slider.Value = inL2;
                l2ValLbTrans.Y = Math.Min(outL2, Math.Max(0, 255)) / 255.0 * -70.0 + TRIG_LB_TRANSFORM_OFFSETY;
                l2ValLbBrush.Color = outL2 >= 255 ? Colors.Green : (outL2 == 0 ? Colors.Red : Colors.Black);
                l2InValLb.Content = inL2;
                l2OutValLb.Content = outL2;
                lastInL2 = inL2;
                lastOutL2 = outL2;
            }

            // R2 トリガー差分更新
            int inR2 = baseState.R2;
            int outR2 = interState.R2;
            if (inR2 != lastInR2 || outR2 != lastOutR2)
            {
                r2Slider.Value = inR2;
                r2ValLbTrans.Y = Math.Min(outR2, Math.Max(0, 255)) / 255.0 * -70.0 + TRIG_LB_TRANSFORM_OFFSETY;
                r2ValLbBrush.Color = outR2 >= 255 ? Colors.Green : (outR2 == 0 ? Colors.Red : Colors.Black);
                r2InValLb.Content = inR2;
                r2OutValLb.Content = outR2;
                lastInR2 = inR2;
                lastOutR2 = outR2;
            }

            // ジャイロスライダー差分更新
            int yaw = baseState.Motion.gyroYawFull;
            int pitch = baseState.Motion.gyroPitchFull;
            int roll = baseState.Motion.gyroRollFull;
            if (yaw != lastGyroYaw || pitch != lastGyroPitch || roll != lastGyroRoll)
            {
                gyroYawSlider.Value = yaw;
                gyroPitchSlider.Value = pitch;
                gyroRollSlider.Value = roll;
                lastGyroYaw = yaw;
                lastGyroPitch = pitch;
                lastGyroRoll = roll;
            }

            // タッチパッド差分更新
            int touchX = baseState.TrackPadTouch0.X;
            int touchY = baseState.TrackPadTouch0.Y;
            if (touchX != lastTouchX || touchY != lastTouchY)
            {
                touchXValLb.Content = touchX;
                touchYValLb.Content = touchY;
                lastTouchX = touchX;
                lastTouchY = touchY;
            }

            // 入力遅延 (0.1ms単位で変化があった場合のみテキスト再構築)
            double latency = ds.Latency;
            int latencyInt = (int)(latency * 10);
            if (latencyInt != lastLatencyInt)
            {
                int warnInterval = ds.getWarnInterval();
                inputDelayLb.Content = string.Format(Properties.Resources.InputDelay, latency.ToString("0.0"));

                if (latency > warnInterval)
                {
                    inpuDelayBackBrush.Color = Colors.Red;
                    inpuDelayForeBrush.Color = Colors.White;
                }
                else if (latency > (warnInterval * 0.5))
                {
                    inpuDelayBackBrush.Color = Colors.Yellow;
                    inpuDelayForeBrush.Color = Colors.Black;
                }
                else
                {
                    inpuDelayBackBrush.Color = Colors.Transparent;
                    inpuDelayForeBrush.Color = SystemColors.WindowTextColor;
                }
                lastLatencyInt = latencyInt;
            }

            // バッテリー残量差分更新
            int battery = baseState.Battery;
            if (battery != lastBattery)
            {
                batteryLvlLb.Content = $"{Translations.Strings.Battery}: {battery}%";
                lastBattery = battery;
            }

            // キャリブレーションインジケータ
            if (cntCalibrating != lastCalibrating)
            {
                gyroCalEllipse.Visibility = cntCalibrating > 0 && ((cntCalibrating / 250) % 2 == 1) ? Visibility.Visible : Visibility.Hidden;
                lastCalibrating = cntCalibrating;
            }

            // 出力遅延差分更新 (0.01ms単位で変化があった場合のみテキスト再構築)
            double procDelay = controlService != null ? controlService.GetProcessingDelay(deviceNum) : 0.0;
            int procDelayInt = (int)(procDelay * 100);
            if (procDelayInt != lastProcDelayInt)
            {
                outputDelayLabel ??= FindName("outputDelayLb") as Label;
                if (outputDelayLabel != null)
                {
                    outputDelayLabel.Content = procDelay > 0.0 ? $"出力遅延: {procDelay:0.00} ms" : "出力遅延: -- ms";
                }
                lastProcDelayInt = procDelayInt;
            }
        }
    }
}