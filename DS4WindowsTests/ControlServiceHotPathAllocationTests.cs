using System;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step2-5 (PR-5): 入力ループ（On_Report）で毎レポート評価される取得系のシムが、
    /// ヒープ割り当て（GC Alloc）を行わないこと（ゼロアロケーション方針）、および1回あたりの呼び出しコストが
    /// 想定外に大きくないことを検証する。
    /// 処理時間の厳密な比較（置換前後 ±5%）は、実機での On_Report 全体の計測（Phase6-Step2-Plan.md §7.2）で行う。
    /// ここでの時間の検証は、Legacy 経路の再解決やログ出力の混入といった異常を検出するための緩い上限のみとする。
    /// </summary>
    public class ControlServiceHotPathAllocationTests
    {
        private const int Iterations = 200000;
        private const int Slot = 0;

        private readonly ITestOutputHelper _output;
        private readonly AppSettingsService _appSettings = new AppSettingsService();
        private readonly AppearanceSettingsService _appearance = new AppearanceSettingsService();
        private readonly ProfileSettingsService _profileSettings = new ProfileSettingsService();
        private readonly ProfileActionProvider _actionProvider = new ProfileActionProvider();

        public ControlServiceHotPathAllocationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>On_Report 相当の取得をまとめて1回実行する（結果は捨てずに集約し、最適化で消されないようにする）。</summary>
        private int ReadHotPathOnce()
        {
            int acc = 0;
            if (_appSettings.UseUdpServerSmoothing) acc++;
            acc += _appSettings.FlashWhenLateAt;
            if (_appSettings.FlashWhenLate) acc++;
            if (_appSettings.UseOscSender) acc++;
            if (_appSettings.UseOscServer) acc++;
            if (_appearance.UseIconChoice == TrayIconChoice.Battery) acc++;
            if (_profileSettings.GetEnableTouchToggle(Slot)) acc++;
            if (_profileSettings.ContainsCustomAction(Slot)) acc++;
            if (_profileSettings.ContainsCustomExtras(Slot)) acc++;
            if (_profileSettings.TouchOutMode[Slot] != TouchpadOutMode.Controls) acc++;
            if (_profileSettings.TouchpadActiveArray[Slot]) acc++;
            if (_profileSettings.UseDInputOnlyArray[Slot]) acc++;
            acc += (int)_profileSettings.OutputDS4TriggerMode[Slot];
            acc += (int)_profileSettings.GetGyroOutMode(Slot);
            acc += _profileSettings.GetRumbleBoost(Slot);
            acc += _actionProvider.GetProfileActionCount(Slot);
            return acc;
        }

        [Fact]
        public void HotPathGetters_DoNotAllocate()
        {
            int sink = 0;
            for (int i = 0; i < 1000; i++) sink += ReadHotPathOnce(); // ウォームアップ

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++) sink += ReadHotPathOnce();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            _output.WriteLine($"割り当て: {allocated} bytes / {Iterations} 回（sink={sink}）");
            Assert.Equal(0L, allocated);
        }

        [Fact]
        public void HotPathGetters_PerCallCost_IsBelowLooseUpperBound()
        {
            int sink = 0;
            for (int i = 0; i < 1000; i++) sink += ReadHotPathOnce(); // ウォームアップ

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < Iterations; i++) sink += ReadHotPathOnce();
            stopwatch.Stop();

            // 1回の ReadHotPathOnce は 16 個の取得を含む。
            double nanosecondsPerRead = stopwatch.Elapsed.TotalMilliseconds * 1000000.0 / Iterations;
            _output.WriteLine($"ReadHotPathOnce: {nanosecondsPerRead:F1} ns / 回（16 取得、sink={sink}）");

            // 緩い上限（1回あたり 20 µs）。Legacy 経路の再解決・ログ出力・ロックなどの混入を検出する。
            Assert.True(nanosecondsPerRead < 20000.0, $"1回あたり {nanosecondsPerRead:F1} ns は想定外に大きい");
        }
    }
}