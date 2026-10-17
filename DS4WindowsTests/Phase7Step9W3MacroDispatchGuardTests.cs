using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step9 W3（K3、Q3＝A）: PlayMacro の二重実行防止の暫定フラグを <c>MacroRuntimeState.TryBeginDispatch</c>／<c>EndDispatch</c> に
    /// 包んだ（挙動は同一）ことの回帰ガード。恒久の再設計は Phase8。ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    public class Phase7Step9W3MacroDispatchGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step9W3MacroDispatchGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TryBeginDispatch_SucceedsOnce_ThenFailsUntilEnded()
        {
            var state = new MacroRuntimeState();

            Assert.True(state.TryBeginDispatch(5));
            Assert.True(state.DispatchInFlight[5]);

            // 実行中の 2 回目は失敗し、フラグは変わらない
            Assert.False(state.TryBeginDispatch(5));
            Assert.True(state.DispatchInFlight[5]);

            // 別の control には影響しない
            Assert.True(state.TryBeginDispatch(6));

            state.EndDispatch(5);
            Assert.False(state.DispatchInFlight[5]);
            Assert.True(state.DispatchInFlight[6]);
            Assert.True(state.TryBeginDispatch(5));
        }

        [Fact]
        public void EndDispatch_OnAnIdleControl_StaysFalse()
        {
            var state = new MacroRuntimeState();
            state.EndDispatch(3);
            Assert.False(state.DispatchInFlight[3]);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(MacroRuntimeState.ControlArrayLength)]
        public void OutOfRangeIndex_ThrowsToTheCaller_AsTheOldCodeDid(int index)
        {
            var state = new MacroRuntimeState();
            Assert.Throws<IndexOutOfRangeException>(() => state.TryBeginDispatch(index));
            Assert.Throws<IndexOutOfRangeException>(() => state.EndDispatch(index));
        }

        [Fact]
        public void TheDispatchLock_IsNotExposed()
        {
            Assert.Null(typeof(MacroRuntimeState).GetProperty("DispatchLock", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Fact]
        public void Mapping_UsesTheWrappers_AndNoLongerTouchesTheLockOrTheArray()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/Mapping.cs");
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string text = Regex.Replace(Regex.Replace(File.ReadAllText(path), @"/\*.*?\*/", string.Empty, RegexOptions.Singleline), @"//[^\r\n]*", string.Empty);
            Assert.Contains("_macroRuntime.TryBeginDispatch(", text);
            Assert.Contains("_macroRuntime.EndDispatch(", text);
            Assert.DoesNotContain("macroDispatchLock", text);
            Assert.DoesNotContain("macroDispatchInFlight[", text);
            Assert.DoesNotContain("DispatchLock", text);
        }
    }
}
