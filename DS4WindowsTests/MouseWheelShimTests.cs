using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.DS4Control;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step4-1: `MouseWheel` が `Global`（静的な状態）ではなく、コンストラクタで受け取った
    /// `IProfileSettingsService`（`ScrollSensitivity`、`OutputKBMMapping.WHEEL_TICK_*`）と
    /// `IVirtualKBM`（`PerformMouseWheelEvent`）を使うことを検証する。
    /// `MouseWheel` と `Touch` のコンストラクタは internal のため、リフレクションで生成する
    /// （`MappingGyroControlsHelperTests` と同じ手法）。
    /// 出力先は記録用スタブの `IVirtualKBM` で、実際の入力は送出しない。
    /// </summary>
    public class MouseWheelShimTests
    {
        private const int Slot = 0;

        private static readonly Type WheelType = typeof(Mapping).Assembly.GetType("DS4Windows.MouseWheel");

        private static object CreateWheel(int deviceNum, IProfileSettingsService settings, IVirtualKBM kbm)
        {
            Assert.NotNull(WheelType); // 型名変更等で失われた場合に検出する
            try
            {
                return Activator.CreateInstance(WheelType, deviceNum, settings, kbm);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        private static Touch CreateTouch(int x, int y, Touch previous)
        {
            ConstructorInfo ctor = typeof(Touch).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(int), typeof(int), typeof(byte), typeof(Touch) }, null);
            Assert.NotNull(ctor);
            return (Touch)ctor.Invoke(new object[] { x, y, (byte)0, previous });
        }

        /// <summary>2本指が、左右の間隔960px（係数1.0）のまま、縦方向に指定量だけ動いたイベントを作る。</summary>
        private static TouchpadEventArgs TwoFingerVerticalMove(int fromY, int toY)
        {
            Touch prev0 = CreateTouch(0, fromY, null);
            Touch prev1 = CreateTouch(960, fromY, null);
            Touch cur0 = CreateTouch(0, toY, prev0);
            Touch cur1 = CreateTouch(960, toY, prev1);
            return new TouchpadEventArgs(DateTime.UtcNow, false, true, cur0, cur1);
        }

        private static void InvokeTouchesMoved(object wheel, TouchpadEventArgs args)
        {
            MethodInfo method = WheelType.GetMethod("touchesMoved");
            Assert.NotNull(method);
            method.Invoke(wheel, new object[] { args, false });
        }

        private static ProfileSettingsService CreateSettings(int scrollSensitivity, int tickUp, int tickDown)
        {
            var store = new BackingStore();
            store.scrollSensitivity[Slot] = scrollSensitivity;
            var settings = new ProfileSettingsService(store);
            var mapping = new SendInputMapping { WHEEL_TICK_UP = tickUp, WHEEL_TICK_DOWN = tickDown };
            settings.OutputKBMMapping = mapping;
            return settings;
        }

        [Fact]
        public void Constructor_NullProfileSettings_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => CreateWheel(Slot, null, new RecordingVirtualKbm()));
        }

        [Fact]
        public void Constructor_NullVirtualKbm_Throws()
        {
            var settings = CreateSettings(100, 120, -120);
            Assert.Throws<ArgumentNullException>(() => CreateWheel(Slot, settings, null));
        }

        [Fact]
        public void TouchesMoved_ScrollUp_SendsWheelEventScaledByTickUp()
        {
            var settings = CreateSettings(100, 240, -240);
            var kbm = new RecordingVirtualKbm();
            object wheel = CreateWheel(Slot, settings, kbm);

            // 指が上へ100px動く（lastMidY - currentMidY = +100）→ yAction = 100 → 100 * WHEEL_TICK_UP
            InvokeTouchesMoved(wheel, TwoFingerVerticalMove(fromY: 200, toY: 100));

            Assert.Single(kbm.WheelEvents);
            Assert.Equal((100 * 240, 0), kbm.WheelEvents[0]);
        }

        [Fact]
        public void TouchesMoved_ScrollDown_SendsWheelEventScaledByTickDown()
        {
            var settings = CreateSettings(100, 240, -240);
            var kbm = new RecordingVirtualKbm();
            object wheel = CreateWheel(Slot, settings, kbm);

            // 指が下へ100px動く → yAction = -100 → (-100) * -1 * WHEEL_TICK_DOWN
            InvokeTouchesMoved(wheel, TwoFingerVerticalMove(fromY: 100, toY: 200));

            Assert.Single(kbm.WheelEvents);
            Assert.Equal((100 * -240, 0), kbm.WheelEvents[0]);
        }

        [Fact]
        public void TouchesMoved_ReadsTickValuesAtCallTime()
        {
            var settings = CreateSettings(100, 120, -120);
            var kbm = new RecordingVirtualKbm();
            object wheel = CreateWheel(Slot, settings, kbm);

            // 生成後に設定を差し替えても、呼び出しごとに現在の値を読む（値をキャッシュしない）
            settings.OutputKBMMapping = new SendInputMapping { WHEEL_TICK_UP = 360, WHEEL_TICK_DOWN = -360 };
            InvokeTouchesMoved(wheel, TwoFingerVerticalMove(fromY: 200, toY: 100));

            Assert.Equal((100 * 360, 0), kbm.WheelEvents[0]);
        }

        [Fact]
        public void TouchesMoved_ZeroScrollSensitivity_SendsNothing()
        {
            var settings = CreateSettings(0, 120, -120);
            var kbm = new RecordingVirtualKbm();
            object wheel = CreateWheel(Slot, settings, kbm);

            InvokeTouchesMoved(wheel, TwoFingerVerticalMove(fromY: 200, toY: 100));

            Assert.Empty(kbm.WheelEvents);
        }
    }
}
