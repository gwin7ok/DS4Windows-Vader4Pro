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

using DS4Windows;
using DS4WinWPF.DS4Forms;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Interop;

namespace DS4WinWPF.DS4Control
{
    [SuppressUnmanagedCodeSecurity]
    internal static class WindowPlacementHelper
    {
        #region Interop

        /// <summary>
        /// The RECT structure defines the coordinates of the upper-left and lower-right corners of a rectangle.
        /// See: https://www.pinvoke.net/default.aspx/Structures/RECT.html
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public override readonly string ToString()
            {
                return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
            }
        }

        /// <summary>
        /// The POINT structure defines the x- and y-coordinates of a point.
        /// See: https://www.pinvoke.net/default.aspx/Structures/POINT.html
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }

            public override readonly string ToString()
            {
                return $"X: {X}, Y: {Y}";
            }
        }

        /// <summary>
        /// Contains information about the placement of a window on the screen.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        internal struct WINDOWPLACEMENT
        {
            /// <summary>
            /// The length of the structure, in bytes. Before calling the SetWindowPlacement, set this member to sizeof(WINDOWPLACEMENT).
            /// </summary>
            public int Length;

            /// <summary>
            /// Specifies flags that control the position of the minimized window and the method by which the window is restored.
            /// </summary>
            public int Flags;

            /// <summary>
            /// The current show state of the window.
            /// </summary>
            public int ShowCmd;

            /// <summary>
            /// The coordinates of the window's upper-left corner when the window is minimized.
            /// </summary>
            public POINT MinPosition;

            /// <summary>
            /// The coordinates of the window's upper-left corner when the window is maximized.
            /// </summary>
            public POINT MaxPosition;

            /// <summary>
            /// The window's coordinates when the window is in the restored position.
            /// </summary>
            public RECT NormalPosition;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);

        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;

        #endregion

        private static IntPtr mainWindowHandle = IntPtr.Zero;

        internal static void ApplyPlacement(MainWindow mainWindow, bool startMinimized)
        {
            if(mainWindowHandle == IntPtr.Zero)
            {
                mainWindowHandle = new WindowInteropHelper(mainWindow).Handle;
            }

            // SetWindowPlacement は常に物理ピクセルを期待する
            // しかし、保存値は論理ピクセルなので変換が必要
            // 重要: ウィンドウが配置される先のモニタのDPIで変換する必要があるが、
            // この時点ではまだウィンドウが表示されていないため、保存された位置から
            // 対象モニタのDPIを取得する必要がある
            
            AppLogger.LogDebug($"ApplyPlacement: Saved Logical Position ({Global.FormLocationX}, {Global.FormLocationY}), Size ({Global.FormWidth}x{Global.FormHeight})");

            // 保存された矩形（論理ピクセル）を用意して、各スクリーンとの包含／重なりを出力
            try
            {
                var savedRect = new System.Windows.Rect(Global.FormLocationX, Global.FormLocationY, Global.FormWidth, Global.FormHeight);
                AppLogger.LogTrace($"ApplyPlacement: Saved Logical Rect = {savedRect}");
                AppLogger.LogTrace("ApplyPlacement: Enumerating screens:");
                int si = 0;
                foreach (var s in WpfScreenHelper.Screen.AllScreens)
                {
                    var contains = s.Bounds.Contains(new System.Windows.Point(Global.FormLocationX, Global.FormLocationY));
                    var inter = System.Windows.Rect.Intersect(savedRect, s.Bounds);
                    AppLogger.LogTrace($"ApplyPlacement: Screen[{si}] Bounds={s.Bounds} ScaleFactor={s.ScaleFactor:F3} WorkingArea={s.WorkingArea}");
                    AppLogger.LogTrace($"ApplyPlacement: Screen[{si}] Contains SavedPoint={contains} SavedRectIntersects={!inter.IsEmpty}");
                    si++;
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogToGui($"ApplyPlacement: Failed while enumerating screens for debug: {ex.Message}", true);
            }

            // 保存された論理座標から、配置先モニタのDPIを取得
            double targetDpiX = 1.0;
            double targetDpiY = 1.0;
            
            // WpfScreenHelper を使用して、論理座標に対応するモニタを特定
            try
            {
                var targetScreen = WpfScreenHelper.Screen.AllScreens
                    .FirstOrDefault(s => s.Bounds.Contains(new System.Windows.Point(Global.FormLocationX, Global.FormLocationY)));
                
                if (targetScreen != null)
                {
                    targetDpiX = targetScreen.ScaleFactor;
                    targetDpiY = targetScreen.ScaleFactor;
                    AppLogger.LogDebug($"ApplyPlacement: Target Monitor DPI Scale = {targetDpiX:F3}");
                }
                else
                {
                    // モニタが見つからない場合はプライマリモニタのDPIを使用
                    var primaryScreen = WpfScreenHelper.Screen.PrimaryScreen;
                    if (primaryScreen != null)
                    {
                        targetDpiX = primaryScreen.ScaleFactor;
                        targetDpiY = primaryScreen.ScaleFactor;
                        AppLogger.LogToGui($"ApplyPlacement: Using Primary Monitor DPI Scale = {targetDpiX:F3}", false);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogToGui($"ApplyPlacement: Failed to get target monitor DPI: {ex.Message}", true);
            }

            // 論理ピクセル → 物理ピクセルに変換
            // 注: SetWindowPlacement を呼ぶ時点でウィンドウが別 DPI コンテキストにある場合、
            // Windows は内部でサイズをスケーリングするため、期待した最終サイズを得るには
            // 現在のウィンドウ DPI コンテキストに合わせた物理座標を渡す必要がある。
            // つまり、渡す物理値 = 論理値 * (現在のウィンドウ DPI / 96)。
            // 計算方針:
            // - left/top (物理) はターゲットモニタのスケールで計算する (これが最終的な物理左上となるため、論理位置を保持する)
            // - width/height の差分 (right-left, bottom-top) は SetWindowPlacement 呼び出し前のウィンドウ DPI スケールで計算する。
            //   Windows は SetWindowPlacement の後で right/bottom にスケーリングを適用するため、差分を before-DPI スケールで渡すと
            //   最終的にターゲットモニタの DPI で期待した幅/高さになる。

            double windowDpiScale = 1.0;
            try
            {
                var dpiForWindow = GetDpiForWindow(mainWindowHandle);
                if (dpiForWindow > 0)
                {
                    windowDpiScale = dpiForWindow / 96.0;
                    AppLogger.LogTrace($"ApplyPlacement: Current window DPI scale = {windowDpiScale:F3} (dpi={dpiForWindow})");
                }
            }
            catch { }

            double targetScale = targetDpiX; // targetScreen.ScaleFactor を earlier にセット済み
            try
            {
                AppLogger.LogTrace($"ApplyPlacement: Target monitor scale = {targetScale:F3}");
            }
            catch { }

            // 左上はターゲットスケールで計算
            int physicalX = (int)Math.Round(Global.FormLocationX * targetScale);
            int physicalY = (int)Math.Round(Global.FormLocationY * targetScale);

            // 幅/高さ差分は現在のウィンドウ DPI スケールで計算（SetWindowPlacement 後にスケーリングされる想定）
            int physicalWidth = (int)Math.Round(Global.FormWidth * windowDpiScale);
            int physicalHeight = (int)Math.Round(Global.FormHeight * windowDpiScale);

            AppLogger.LogTrace($"ApplyPlacement: Computed physical LeftTop=({physicalX},{physicalY}) Width/HeightDiff=({physicalWidth}x{physicalHeight}) -- targetScale={targetScale:F3}, windowScale={windowDpiScale:F3}");

            var placement = new WINDOWPLACEMENT()
            {
                Flags = 0,
                Length = Marshal.SizeOf(typeof(WINDOWPLACEMENT)),
                ShowCmd = (startMinimized ? SW_SHOWMINIMIZED : SW_SHOWNORMAL),
                MaxPosition = new POINT(-1, -1),
                MinPosition = new POINT(-1, -1),
                NormalPosition = new RECT(physicalX, physicalY, 
                    physicalX + physicalWidth, physicalY + physicalHeight)
            };

            try
            {
                try
                {
                    var dpiB = GetDpiForWindow(mainWindowHandle);
                    AppLogger.LogTrace($"ApplyPlacement: GetDpiForWindow before SetWindowPlacement = {dpiB}");
                }
                catch { }

                var setRes = SetWindowPlacement(mainWindowHandle, ref placement);

                // Query resulting placement and DPI
                var resulting = GetPlacement(mainWindow);
                AppLogger.LogTrace($"ApplyPlacement: After SetWindowPlacement, GetPlacement physicalRect={resulting}");

                try
                {
                    var dpiA = GetDpiForWindow(mainWindowHandle);
                    AppLogger.LogTrace($"ApplyPlacement: GetDpiForWindow after SetWindowPlacement = {dpiA}");
                }
                catch { }

                // Determine which screen contains the center of resulting rect
                try
                {
                    int resWidth = resulting.Right - resulting.Left;
                    int resHeight = resulting.Bottom - resulting.Top;
                    double centerX = resulting.Left + resWidth / 2.0;
                    double centerY = resulting.Top + resHeight / 2.0;
                    int sidx = 0;
                    foreach (var s in WpfScreenHelper.Screen.AllScreens)
                    {
                        if (s.Bounds.Contains(new System.Windows.Point(centerX, centerY)))
                        {
                            AppLogger.LogTrace($"ApplyPlacement: Resulting window center is on Screen[{sidx}] Bounds={s.Bounds} ScaleFactor={s.ScaleFactor}");
                            break;
                        }
                        sidx++;
                    }
                }
                catch { }

                if (!setRes)
                {
                    var err = Marshal.GetLastWin32Error();
                    AppLogger.LogToGui($"ApplyPlacement: SetWindowPlacement returned false, Win32Error={err}", true);
                }
                else
                {
                    AppLogger.LogDebug($"ApplyPlacement: SetWindowPlacement succeeded");
                }
            }
            catch(Exception ex)
            {
                AppLogger.LogToGui($"Failed to apply window placement: {ex.Message}", true);
            }
        }

        internal static RECT GetPlacement(MainWindow mainWindow)
        {
            if (mainWindowHandle == IntPtr.Zero)
            {
                mainWindowHandle = new WindowInteropHelper(mainWindow).Handle;
            }

            GetWindowPlacement(mainWindowHandle, out var placement);
            return placement.NormalPosition;
        }

        /// <summary>
        /// 物理ピクセル座標を論理ピクセルに変換して返す
        /// GetWindowPlacementは物理ピクセルを返すため、保存時は論理ピクセルに変換する必要がある
        /// </summary>
        internal static (int LogicalWidth, int LogicalHeight, int LogicalX, int LogicalY) GetLogicalPlacement(MainWindow mainWindow)
        {
            var physicalRect = GetPlacement(mainWindow);
            
            // DPI取得
            var source = System.Windows.PresentationSource.FromVisual(mainWindow);
            double dpiScaleX = 1.0;
            double dpiScaleY = 1.0;
            if (source?.CompositionTarget != null)
            {
                var m = source.CompositionTarget.TransformToDevice;
                dpiScaleX = m.M11;
                dpiScaleY = m.M22;
            }

            // 物理ピクセル → 論理ピクセルに変換
            int logicalWidth = (int)Math.Round((physicalRect.Right - physicalRect.Left) / dpiScaleX);
            int logicalHeight = (int)Math.Round((physicalRect.Bottom - physicalRect.Top) / dpiScaleY);
            int logicalX = (int)Math.Round(physicalRect.Left / dpiScaleX);
            int logicalY = (int)Math.Round(physicalRect.Top / dpiScaleY);

            try
            {
                AppLogger.LogTrace($"GetLogicalPlacement: PhysicalRect={physicalRect} DPI=({dpiScaleX:F3},{dpiScaleY:F3}) => Logical=({logicalX},{logicalY}) Size=({logicalWidth}x{logicalHeight})");
            }
            catch { }

            return (logicalWidth, logicalHeight, logicalX, logicalY);
        }
    }
}