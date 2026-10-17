using System.IO;
using System.Text.RegularExpressions;
using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step8 W1: <c>Mouse</c>／<c>ControlService</c> が保持クラスをコンストラクターで受けること、
    /// <c>MapClick</c>／<c>ResetRuntimeStateForKey</c> を <c>SyntheticInputState</c> へ移したこと、
    /// 本体の呼び出しが 0 件だった <c>Mapping</c> の 4 メンバーを削除したことの回帰ガード。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。判断の根拠は Phase7-Step8-Plan.md（Q5・Q7・Q9）。
    /// </summary>
    public class Phase7Step8W1StateHolderInjectionGuardTests
    {
        private const ushort Key = 0x4B;

        private readonly ITestOutputHelper _output;

        public Phase7Step8W1StateHolderInjectionGuardTests(ITestOutputHelper output)
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
        public void CountClick_CountsEachKindOnTheGivenDevice_AndIgnoresNone()
        {
            var state = new SyntheticInputState();
            state.CountClick(2, Click.Left);
            state.CountClick(2, Click.Left);
            state.CountClick(2, Click.Right);
            state.CountClick(2, Click.WUP);
            state.CountClick(2, Click.None);

            Assert.Equal(2, state.DeviceState[2].currentClicks.leftCount);
            Assert.Equal(1, state.DeviceState[2].currentClicks.rightCount);
            Assert.Equal(1, state.DeviceState[2].currentClicks.wUpCount);
            Assert.Equal(0, state.DeviceState[2].currentClicks.middleCount);
            Assert.Equal(0, state.DeviceState[3].currentClicks.leftCount);
        }

        [Fact]
        public void ResetRuntimeStateForKey_RemovesOnlyThatKeyFromEveryDevice()
        {
            var state = new SyntheticInputState();
            for (int d = 0; d < state.DeviceState.Length; d++)
            {
                state.DeviceState[d].keyPresses[Key] = new SyntheticState.KeyPresses();
                state.DeviceState[d].keyPresses[0x4C] = new SyntheticState.KeyPresses();
                state.DeviceState[d].nativeKeyAlias[Key] = 1;
            }

            state.ResetRuntimeStateForKey(Key);

            for (int d = 0; d < state.DeviceState.Length; d++)
            {
                Assert.False(state.DeviceState[d].keyPresses.ContainsKey(Key));
                Assert.False(state.DeviceState[d].nativeKeyAlias.ContainsKey(Key));
                Assert.True(state.DeviceState[d].keyPresses.ContainsKey(0x4C));
            }
        }

        [Fact]
        public void Mapping_NoLongerHasTheMovedOrDeadMembers()
        {
            string mapping = Read("DS4Windows/DS4Control/Mapping.cs");
            if (mapping == null) return;

            foreach (string name in new[]
            {
                "MapClick", "ResetRuntimeStateForKey", "DispatchProfileActionEdge",
                "TakePendingRestoreProfileName", "ApplyProfileDirect", "RestoreProfileDirect",
            })
            {
                Assert.DoesNotMatch(@"\b" + name + @"\b", mapping);
            }
        }

        [Fact]
        public void MouseAndControlService_ReceiveTheHoldersByConstructor()
        {
            string mouse = Read("DS4Windows/DS4Control/Mouse.cs");
            string control = Read("DS4Windows/DS4Control/ControlService.cs");
            string registration = Read("DS4Windows/DI/ServiceRegistration.cs");
            if (mouse == null || control == null || registration == null) return;

            Assert.DoesNotContain("StickMotionState.Shared", mouse);
            Assert.DoesNotContain("SyntheticInputState.Shared", mouse);
            Assert.DoesNotMatch(@"\bMapping\.", mouse);
            Assert.DoesNotContain("StickMotionState.Shared", control);
            Assert.DoesNotContain("SyntheticInputState.Shared", control);
            Assert.Contains("sp.GetRequiredService<StickMotionState>()", registration);
            Assert.Contains("sp.GetRequiredService<SyntheticInputState>()", registration);
        }

        [Fact]
        public void SpecialActionRepository_ResetsTheKeyStateAfterGlobalSaveAction_AndGlobalHasNoMappingReference()
        {
            string repository = Read("DS4Windows/DS4Control/Services/SpecialActionRepository.cs");
            string global = Read("DS4Windows/DS4Control/Global.cs");
            if (repository == null || global == null) return;

            int save = repository.IndexOf("SaveActionAndReload(", System.StringComparison.Ordinal);
            int reset = repository.IndexOf("_syntheticInput.ResetRuntimeStateForKey(", System.StringComparison.Ordinal);
            Assert.True(save >= 0 && reset > save, "SaveActionAndReload（旧 Global.SaveAction）の後に ResetRuntimeStateForKey を呼ぶこと");
            Assert.Contains("mode == 4", repository);

            Assert.DoesNotMatch(@"\bMapping\.", global);
        }
    }
}
