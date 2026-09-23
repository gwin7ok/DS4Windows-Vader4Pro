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
    /// Step3-5（`SetCurveAndDeadzone` 残り・`ApplyStickCalibration`・`Commit`）、Step3-6 の対象も、
    /// このクラスへ追加していく。
    /// </summary>
    public class MappingHotPathAllocationTests
    {
        private const int Slot = 0;
        private const int Iterations = 20000;

        private readonly ITestOutputHelper _output;

        public MappingHotPathAllocationTests(ITestOutputHelper output)
        {
            _output = output;
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

            for (int i = 0; i < 1000; i++) RunOnce(); // ウォームアップ（型初期化・JIT を計測から除く）

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++) RunOnce();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            _output.WriteLine($"割り当て: {allocated} bytes / {Iterations} 回（sink={sink}）");
            Assert.Equal(0L, allocated);
        }
    }
}