using System;
using Xunit;
using DS4Windows;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step4-5: `MouseCursor.TouchMoveCursor`（タッチパッドのマウス移動。毎レポート呼ばれるホットパス）が、
    /// 置換後もヒープ割り当て0バイトであることを固定する（`MappingHotPathAllocationTests` と同じ手法）。
    /// 出力先のスタブは記録を止めて（`Recording = false`）、記録用リストの拡張を計測から除外する。
    /// </summary>
    public class MouseCursorHotPathAllocationTests
    {
        private const int Slot = 0;

        [Fact]
        public void TouchMoveCursor_AllocatesZeroBytes()
        {
            var store = new BackingStore();
            store.touchSensitivity[Slot] = 100;
            var settings = new ProfileSettingsService(store);
            var kbm = new RecordingVirtualKbm { Recording = false };
            var cursor = new MouseCursor(Slot, null, settings, kbm);

            // JIT・初回初期化を計測から外す
            for (int i = 0; i < 200; i++) cursor.TouchMoveCursor(10, 3);

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) cursor.TouchMoveCursor(10, 3);
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.Equal(0, after - before);
        }
    }
}
