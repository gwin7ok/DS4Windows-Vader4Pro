using System;
using Xunit;
using DS4Windows;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step4-2: `MouseCursor` が `Global`（静的な状態）ではなく、コンストラクタで受け取った
    /// `IProfileSettingsService`（`TouchSensitivity`、`TouchpadInvert`、`TouchRelMouse` 等）と
    /// `IVirtualKBM`（`MoveRelativeMouse`）を使うことを検証する。
    /// 互いに独立した BackingStore を持つ設定を用意し、片方だけ値を変えて出力を相対比較する
    /// （`MappingArgumentPassThroughTests` と同じ手法。期待値の絶対値は固定しない）。
    /// 出力先は記録用スタブで、実際の入力は送出しない。
    /// </summary>
    public class MouseCursorMovementTests
    {
        private const int Slot = 0;

        private static ProfileSettingsService CreateSettings(out BackingStore store, byte touchSensitivity = 100, int touchpadInvert = 0)
        {
            store = new BackingStore();
            store.touchSensitivity[Slot] = touchSensitivity;
            store.touchpadInvert[Slot] = touchpadInvert;
            return new ProfileSettingsService(store);
        }

        private static MouseCursor CreateCursor(ProfileSettingsService settings, RecordingVirtualKbm kbm)
        {
            return new MouseCursor(Slot, null, settings, kbm);
        }

        [Fact]
        public void Constructor_NullProfileSettings_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new MouseCursor(Slot, null, null, new RecordingVirtualKbm()));
        }

        [Fact]
        public void Constructor_NullVirtualKbm_Throws()
        {
            var settings = CreateSettings(out _);
            Assert.Throws<ArgumentNullException>(() => new MouseCursor(Slot, null, settings, null));
        }

        [Fact]
        public void TouchMoveCursor_PositiveDeltaX_SendsRelativeMove()
        {
            var settings = CreateSettings(out _);
            var kbm = new RecordingVirtualKbm();

            CreateCursor(settings, kbm).TouchMoveCursor(10, 0);

            Assert.Single(kbm.MoveEvents);
            Assert.True(kbm.MoveEvents[0].X > 0, $"X={kbm.MoveEvents[0].X}");
            Assert.Equal(0, kbm.MoveEvents[0].Y);
        }

        [Fact]
        public void TouchMoveCursor_UsesTouchSensitivityFromPassedSettings()
        {
            var low = CreateSettings(out _, touchSensitivity: 100);
            var high = CreateSettings(out _, touchSensitivity: 200);
            var lowKbm = new RecordingVirtualKbm();
            var highKbm = new RecordingVirtualKbm();

            CreateCursor(low, lowKbm).TouchMoveCursor(10, 0);
            CreateCursor(high, highKbm).TouchMoveCursor(10, 0);

            Assert.Single(lowKbm.MoveEvents);
            Assert.Single(highKbm.MoveEvents);
            Assert.True(highKbm.MoveEvents[0].X > lowKbm.MoveEvents[0].X,
                $"感度が反映されていない: low={lowKbm.MoveEvents[0].X} high={highKbm.MoveEvents[0].X}");
        }

        [Fact]
        public void TouchMoveCursor_TouchpadInvertHorizontal_FlipsX()
        {
            var settings = CreateSettings(out _, touchpadInvert: 0x02);
            var kbm = new RecordingVirtualKbm();

            CreateCursor(settings, kbm).TouchMoveCursor(10, 0);

            Assert.Single(kbm.MoveEvents);
            Assert.True(kbm.MoveEvents[0].X < 0, $"X={kbm.MoveEvents[0].X}");
        }

        [Fact]
        public void TouchMoveCursor_DisableInvert_IgnoresTouchpadInvert()
        {
            var settings = CreateSettings(out _, touchpadInvert: 0x02);
            var kbm = new RecordingVirtualKbm();

            CreateCursor(settings, kbm).TouchMoveCursor(10, 0, disableInvert: true);

            Assert.Single(kbm.MoveEvents);
            Assert.True(kbm.MoveEvents[0].X > 0, $"X={kbm.MoveEvents[0].X}");
        }

        [Fact]
        public void TouchMoveCursor_ZeroDelta_SendsNothing()
        {
            var settings = CreateSettings(out _);
            var kbm = new RecordingVirtualKbm();

            CreateCursor(settings, kbm).TouchMoveCursor(0, 0);

            Assert.Empty(kbm.MoveEvents);
        }
    }
}
