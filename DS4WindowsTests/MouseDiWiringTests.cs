using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step4-5: `Mouse` が、コンストラクタで受け取った `IProfileSettingsService`／`IVirtualKBM` を
    /// 内部の `MouseCursor`／`MouseWheel` へ同一インスタンスのまま伝搬していることを検証する。
    /// `DS4Device` は実機・HID が必要なため、コンストラクタを実行せずに生成した未初期化オブジェクトを渡す
    /// （`Mouse` のコンストラクタは `GyroMouseSensSettings`（null でよい）しか参照しない）。
    /// </summary>
    public class MouseDiWiringTests
    {
        private const int Slot = 0;

        private static DS4Device CreateUninitializedDevice()
        {
            return (DS4Device)RuntimeHelpers.GetUninitializedObject(typeof(DS4Device));
        }

        private static object GetField(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field); // フィールド名変更等で失われた場合に検出する
            return field.GetValue(target);
        }

        [Fact]
        public void Constructor_NullProfileSettings_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new Mouse(Slot, CreateUninitializedDevice(), null, new RecordingVirtualKbm()));
        }

        [Fact]
        public void Constructor_NullVirtualKbm_Throws()
        {
            var settings = new ProfileSettingsService(new BackingStore());
            Assert.Throws<ArgumentNullException>(() =>
                new Mouse(Slot, CreateUninitializedDevice(), settings, null));
        }

        [Fact]
        public void Constructor_PropagatesServicesToCursorAndWheel()
        {
            var settings = new ProfileSettingsService(new BackingStore());
            var kbm = new RecordingVirtualKbm();

            var mouse = new Mouse(Slot, CreateUninitializedDevice(), settings, kbm);

            Assert.Same(settings, GetField(mouse, "_profileSettings"));
            Assert.Same(kbm, GetField(mouse, "_virtualKBM"));

            object cursor = GetField(mouse, "cursor");
            Assert.Same(settings, GetField(cursor, "_profileSettings"));
            Assert.Same(kbm, GetField(cursor, "_virtualKBM"));

            object wheel = GetField(mouse, "wheel");
            Assert.Same(settings, GetField(wheel, "_profileSettings"));
            Assert.Same(kbm, GetField(wheel, "_virtualKBM"));
        }
    }
}
