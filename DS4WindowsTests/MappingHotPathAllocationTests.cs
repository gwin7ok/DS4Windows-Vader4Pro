using System;
using Xunit;
using Xunit.Abstractions;
using DS4Windows;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step3-4: 毎入力レポートで実行される `Mapping` のホットパスが、`Global` 直接参照から
    /// 引数で渡された `IProfileSettingsService` の参照に変わっても、ヒープ割り当て（GC Alloc）を
    /// 発生させないこと（ゼロアロケーション方針、Phase6-Step3-Plan.md §6）を検証する。
    /// 処理時間の厳密な比較は実機（On_Report 全体の計測）で行う。
    /// Step3-5 で `SetCurveAndDeadzone` のジャイロ系と `ApplyStickCalibration` の検証を追加した。
    /// `Commit` は実際の入力送出を伴うため割り当てテストの対象外（`MappingArgumentPassThroughTests` の冒頭コメント参照）。
    /// Step3-6 の対象も、このクラスへ追加していく。
    /// </summary>
    public class MappingHotPathAllocationTests
    {
        private const int Slot = 0;
        private const int Iterations = 20000;
        private const int MaxRounds = 3;

        private readonly ITestOutputHelper _output;

        public MappingHotPathAllocationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// ウォームアップの後、<see cref="Iterations"/> 回の呼び出しを最大 <see cref="MaxRounds"/> 回計測し、
        /// 割り当てバイト数の最小値を返す（0 になった時点で打ち切る）。
        /// 1 回の呼び出しごとに割り当てがあれば、どの計測回でも <see cref="Iterations"/> バイト以上になるため検出できる。
        /// 一方、ランタイムが計測区間の途中で一度だけ行う割り当て（階層型 JIT・OSR による再コンパイルなど。
        /// 2026-09-24 に ApplyStickCalibration で 784 バイト／20000 回として観測）は、次の計測回では再発しないため除外される。
        /// </summary>
        private long MeasureMinAllocatedBytes(Action runOnce)
        {
            for (int i = 0; i < 1000; i++) runOnce(); // ウォームアップ（型初期化・JIT を計測から除く）

            long min = long.MaxValue;
            for (int round = 0; round < MaxRounds && min != 0; round++)
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < Iterations; i++) runOnce();
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                _output.WriteLine($"  計測 {round + 1} 回目: {allocated} bytes");
                min = Math.Min(min, allocated);
            }

            return min;
        }

        [Fact]
        public void SetCurveAndDeadzone_DoesNotAllocate()
        {
            var settings = new ProfileSettingsService(new BackingStore());
            var cState = new DS4State();
            var dState = new DS4State();

            int sink = 0;

            void RunOnce()
            {
                // 実際の入力ループと同様、毎回入力値を書き直してから呼び出す（メソッドは cState も書き換えるため）。
                cState.LX = 200; cState.LY = 140; cState.RX = 60; cState.RY = 180;
                cState.L2 = 200; cState.R2 = 50;
                sink += Mapping.SetCurveAndDeadzone(Slot, cState, dState, settings).LX;
            }

            long allocated = MeasureMinAllocatedBytes(RunOnce);

            _output.WriteLine($"割り当て: {allocated} bytes / {Iterations} 回（最小の計測回、sink={sink}）");
            Assert.Equal(0L, allocated);
        }

        [Fact]
        public void SetCurveAndDeadzone_WithGyroControlsActive_DoesNotAllocate()
        {
            // ジャイロ→コントロール変換（Step3-5 で引数渡しへ移した SX/SZ 系）の分岐を実際に通す。
            var store = new BackingStore();
            store.gyroOutMode[Slot] = GyroOutMode.Controls;
            store.setSXOutCurveMode(Slot, 2); // 出力カーブの分岐も通す
            store.setSZOutCurveMode(Slot, 2);
            var settings = new ProfileSettingsService(store);

            var cState = new DS4State();
            var motion = new SixAxis(0, 0, 0, 0, 0, 0, 0.0);
            motion.accelX = 64;
            motion.accelZ = -64;
            motion.outputGyroControls = true;
            cState.Motion = motion;
            var dState = new DS4State();

            int sink = 0;

            void RunOnce()
            {
                cState.LX = 200; cState.LY = 140; cState.RX = 60; cState.RY = 180;
                cState.L2 = 200; cState.R2 = 50;
                sink += Mapping.SetCurveAndDeadzone(Slot, cState, dState, settings).LX;
            }

            long allocated = MeasureMinAllocatedBytes(RunOnce);

            _output.WriteLine($"割り当て: {allocated} bytes / {Iterations} 回（最小の計測回、sink={sink}）");
            Assert.Equal(0L, allocated);
        }

        [Fact]
        public void ApplyStickCalibration_DoesNotAllocate()
        {
            var store = new BackingStore();
            // 4軸すべてで補正の分岐（減算とクランプ）を通す。
            store.rightStickDriftXAxis[Slot] = 3;
            store.rightStickDriftYAxis[Slot] = -3;
            store.leftStickDriftXAxis[Slot] = 5;
            store.leftStickDriftYAxis[Slot] = -5;
            var settings = new ProfileSettingsService(store);
            var state = new DS4State();

            int sink = 0;

            void RunOnce()
            {
                state.LX = 128; state.LY = 128; state.RX = 128; state.RY = 128;
                sink += Mapping.ApplyStickCalibration(Slot, state, settings).LX;
            }

            long allocated = MeasureMinAllocatedBytes(RunOnce);

            _output.WriteLine($"割り当て: {allocated} bytes / {Iterations} 回（最小の計測回、sink={sink}）");
            Assert.Equal(0L, allocated);
        }

    }
}