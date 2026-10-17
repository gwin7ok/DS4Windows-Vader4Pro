using System;
using System.IO;
using System.Text.RegularExpressions;
using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step9 W2（Q2＝A）: <c>Mapping</c> と状態の保持クラス 3 つが <c>Global</c> を参照しないことと、
    /// 新設した <c>ControllerLimits</c>／<c>NumericUtil</c> が旧 <c>Global</c> の値・計算と同一であることの回帰ガード。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    public class Phase7Step9W2NoGlobalInMappingGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step9W2NoGlobalInMappingGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ControllerLimits_HaveTheSameValuesAsBefore()
        {
            Assert.Equal(8, ControllerLimits.MaxControllerCount);
            Assert.Equal(9, ControllerLimits.TestProfileItemCount);
            Assert.Equal(8, ControllerLimits.TestProfileIndex);
            Assert.Equal(4, ControllerLimits.OldXInputControllerCount);
            Assert.Equal((byte)128, ControllerLimits.StickAxisMidpoint);
        }

        // Phase7-Step10a W4: Global の別名（定数 5 つ・Clamp）は削除した。再導入の禁止は Step12DeadShimRemovalGuardTests が固定する。

        [Theory]
        [InlineData(0.0, -5.0, 10.0)]
        [InlineData(0.0, 15.0, 10.0)]
        [InlineData(0.0, 5.0, 10.0)]
        [InlineData(0.0, 0.0, 10.0)]
        [InlineData(0.0, 10.0, 10.0)]
        [InlineData(-3.5, -3.5, 2.5)]
        [InlineData(5.0, 7.0, 3.0)] // min > max: 旧実装と同じく、value < min の判定が先に効く
        public void Clamp_IsIdenticalToTheOldImplementation(double min, double value, double max)
        {
            double expected = (value < min) ? min : (value > max) ? max : value;
            Assert.Equal(expected, NumericUtil.Clamp(min, value, max));
        }

        [Fact]
        public void Clamp_NaN_IsReturnedAsBefore()
        {
            // 旧実装: NaN は両方の比較が false になり、そのまま返る
            Assert.True(double.IsNaN(NumericUtil.Clamp(0, double.NaN, 10)));
        }

        [Theory]
        [InlineData("DS4Windows/DS4Control/Mapping.cs")]
        [InlineData("DS4Windows/DS4Control/SyntheticInputState.cs")]
        [InlineData("DS4Windows/DS4Control/MacroRuntimeState.cs")]
        [InlineData("DS4Windows/DS4Control/StickMotionState.cs")]
        public void TheFile_DoesNotReferenceGlobal(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string text = Regex.Replace(Regex.Replace(File.ReadAllText(path), @"/\*.*?\*/", string.Empty, RegexOptions.Singleline), @"//[^\r\n]*", string.Empty);
            var matches = Regex.Matches(text, @"(?<![\w.])Global\.\w+");
            Assert.True(matches.Count == 0, relativePath + " に Global への参照が残っている: " + (matches.Count > 0 ? matches[0].Value : string.Empty));
        }
    }
}
