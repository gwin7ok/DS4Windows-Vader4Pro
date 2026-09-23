using System;
using Xunit;
using Xunit.Abstractions;
using DS4Windows;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step3-6a（決定 R1）: <c>IProfileSettingsService.ReverseX360ButtonMapping</c> が、
    /// 起動時に1回作られて以後変化しない定数表 <c>Global.reverseX360ButtonMapping</c> をコピーせずに返すこと、
    /// および従来のコピー版 <c>GetReverseX360ButtonMapping()</c> が引き続き独立したコピーを返すことを検証する。
    /// この表は毎入力レポートの経路（<c>Mapping.ProcessControlSettingAction</c>）から引かれるため、
    /// 読み取りがヒープ割り当てを発生させないこと（ゼロアロケーション方針）も固定する。
    /// </summary>
    public class ProfileSettingsServiceReverseX360ButtonMappingTests
    {
        private const int Iterations = 20000;

        private readonly ITestOutputHelper _output;

        public ProfileSettingsServiceReverseX360ButtonMappingTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static DS4Windows.DI.IProfileSettingsService CreateService()
        {
            return new ProfileSettingsService(new BackingStore());
        }

        [Fact]
        public void ReverseX360ButtonMapping_ReturnsGlobalTableItself()
        {
            var service = CreateService();

            Assert.Same(Global.reverseX360ButtonMapping, service.ReverseX360ButtonMapping);
        }

        [Fact]
        public void GetReverseX360ButtonMapping_StillReturnsIndependentCopy()
        {
            var service = CreateService();

            DS4Controls[] copy = service.GetReverseX360ButtonMapping();

            Assert.NotSame(service.ReverseX360ButtonMapping, copy);
            Assert.Equal(service.ReverseX360ButtonMapping, copy);
        }

        [Fact]
        public void ReverseX360ButtonMapping_DoesNotAllocate()
        {
            var service = CreateService();
            int sink = 0;

            void RunOnce()
            {
                sink += (int)service.ReverseX360ButtonMapping[(int)X360Controls.A];
            }

            for (int i = 0; i < 1000; i++) RunOnce(); // ウォームアップ

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++) RunOnce();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            _output.WriteLine($"割り当て: {allocated} bytes / {Iterations} 回（sink={sink}）");
            Assert.Equal(0L, allocated);
        }
    }
}