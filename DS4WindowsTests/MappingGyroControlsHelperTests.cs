using System.Reflection;
using Xunit;
using DS4Windows;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step3-6b（決定 P2）: `Mapping.IsUsingGyroForControls`（`Global.IsUsingSAForControls` の置き換え。
    /// `Mapping` の静的 `profileSettings` の `GyroOutputMode` を読む）が、置き換え前の `Global.IsUsingSAForControls` と
    /// 同じ結果を返すことを検証する。両者とも同じ BackingStore（`Global.store`）の同じ配列を見るため、
    /// 出力モードを変えたとき常に一致するはずである。
    /// 検証後は元の値へ戻す（Global の static 状態を残さない。Phase6-Status.md §6.4 の教訓1）。
    /// </summary>
    public class MappingGyroControlsHelperTests
    {
        private const int Slot = 0;

        private static readonly MethodInfo HelperMethod = typeof(Mapping).GetMethod(
            "IsUsingGyroForControls", BindingFlags.Static | BindingFlags.NonPublic);

        [Theory]
        [InlineData(GyroOutMode.Controls)]
        [InlineData(GyroOutMode.None)]
        [InlineData(GyroOutMode.Mouse)]
        public void IsUsingGyroForControls_MatchesGlobalIsUsingSAForControls(GyroOutMode mode)
        {
            Assert.NotNull(HelperMethod); // メソッド名変更等でシグネチャが失われた場合に検出する

            GyroOutMode original = Global.store.gyroOutMode[Slot];
            try
            {
                Global.store.gyroOutMode[Slot] = mode;

                bool expected = Global.IsUsingSAForControls(Slot);
                bool actual = (bool)HelperMethod.Invoke(null, new object[] { Slot });

                Assert.Equal(expected, actual);
                Assert.Equal(mode == GyroOutMode.Controls, actual);
            }
            finally
            {
                Global.store.gyroOutMode[Slot] = original;
            }
        }
    }
}