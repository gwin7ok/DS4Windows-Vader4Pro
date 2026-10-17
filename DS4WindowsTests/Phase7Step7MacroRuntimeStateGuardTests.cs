using System.IO;
using System.Text.RegularExpressions;
using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step7 W3: マクロの実行時状態（macrodone／macroDispatchInFlight／macroDispatchLock／macroControl／macroCount／macroTaskQueue）を
    /// <c>MacroRuntimeState</c> へ切り出したことの回帰ガード。
    /// ソースが見つからない環境では、ソース検査を行わず合格とする。判断の根拠は Phase7-Step7-Plan.md（Q2・Q3）。
    /// </summary>
    public class Phase7Step7MacroRuntimeStateGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step7MacroRuntimeStateGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string Read(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した: " + relativePath);
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            return Regex.Replace(text, @"//[^\r\n]*", string.Empty);
        }

        [Fact]
        public void NewInstance_HasTheSameShapeAsTheOldStaticFields()
        {
            var state = new MacroRuntimeState();

            Assert.Equal(52, MacroRuntimeState.ControlArrayLength);
            Assert.Equal(52, state.MacroDone.Length);
            Assert.Equal(52, state.DispatchInFlight.Length);
            Assert.Equal(26, state.MacroControl.Length);
            Assert.Equal(DS4Windows.ControllerLimits.MaxControllerCount, state.TaskQueue.Length);
            Assert.All(state.TaskQueue, q => Assert.NotNull(q));

            // 参照は安定（再代入されない）
            var macroDone = state.MacroDone;
            var taskQueue = state.TaskQueue;
            state.ClearMacroDone();
            Assert.Same(macroDone, state.MacroDone);
            Assert.Same(taskQueue, state.TaskQueue);
        }

        [Fact]
        public void ClearMacroDone_ResetsEveryFlag()
        {
            var state = new MacroRuntimeState();
            state.MacroDone[0] = true;
            state.MacroDone[51] = true;

            state.ClearMacroDone();

            Assert.All(state.MacroDone, f => Assert.False(f));
        }

        [Fact]
        public void Mapping_HoldsNoMacroStateOfItsOwn_OnlyForwarders()
        {
            string mapping = Read("DS4Windows/DS4Control/Mapping.cs");
            if (mapping == null) return;

            // Phase7-Step8 W2: 転送プロパティは削除し、コンストラクターで受けた保持クラスの配列を readonly で複製して持つ
            Assert.Contains("private readonly MacroRuntimeState _macroRuntime;", mapping);
            Assert.Contains("private readonly bool[] macrodone;", mapping);
            // Phase7-Step9 W3（K3）: 二重実行防止のフラグとロックは MacroRuntimeState.TryBeginDispatch／EndDispatch に包んだ（Mapping は持たない）
            Assert.DoesNotContain("private readonly bool[] macroDispatchInFlight;", mapping);
            Assert.DoesNotContain("private readonly object macroDispatchLock;", mapping);
            Assert.Contains("private readonly bool[] macroControl;", mapping);
            Assert.Contains("private readonly Dictionary<string, Task>[] macroTaskQueue;", mapping);
            Assert.Contains("macroTaskQueue = macroRuntime.TaskQueue;", mapping);
            Assert.DoesNotContain("MacroRuntimeState.Shared", mapping);
            Assert.Contains("DS4_CONTROL_MACRO_ARRAY_LEN = MacroRuntimeState.ControlArrayLength", mapping);
        }

        [Fact]
        public void Registration_RegistersTheSharedState()
        {
            string registration = Read("DS4Windows/DI/ServiceRegistration.cs");
            if (registration == null) return;

            Assert.Contains("services.AddSingleton<MacroRuntimeState>();", registration); // Phase7-Step8 W4: Shared は削除した
        }
    }
}
