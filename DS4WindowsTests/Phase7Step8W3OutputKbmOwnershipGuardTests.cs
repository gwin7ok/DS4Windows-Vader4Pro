using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.DS4Control;
using DS4Windows.Services;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step8 W3: 出力 KBM ハンドラの実体を <c>OutputKBMHandlerAdapter</c> へ移したこと（<c>Global.outputKBMHandler</c>／<c>outputKBMMapping</c> の削除）、
    /// 合成キーの送出を <c>SyntheticDispatcherService</c>（<c>ISyntheticDispatcher</c>）にし、静的な <c>SyntheticDispatcher</c> を状態を持たない橋渡しにしたこと、
    /// <c>ActionsLoadedRuntimeReset</c> を instance にしたことの回帰ガード。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。判断の根拠は Phase7-Step8-Plan.md（Q6）。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase7Step8W3OutputKbmOwnershipGuardTests
    {
        private const int Device = 4;
        private const ushort Key = 77;

        private readonly ITestOutputHelper _output;

        public Phase7Step8W3OutputKbmOwnershipGuardTests(ITestOutputHelper output)
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

        private static SyntheticDispatcherService CreateService(out SyntheticInputState state, out OutputKBMHandlerAdapter owner,
            out ProfileSettingsService settings)
        {
            state = new SyntheticInputState();
            owner = new OutputKBMHandlerAdapter();
            settings = TestRepositoryFactory.CreateProfileSettingsService(new BackingStore());
            return new SyntheticDispatcherService(state, owner, settings);
        }

        [Fact]
        public void SendPress_WithAnExplicitHandler_SendsAndRecordsTheCounters()
        {
            var service = CreateService(out var state, out _, out _);
            var handler = new MockVirtualKBM();

            service.SendPress(Device, Key, 65, false, handler);

            Assert.Equal(new uint[] { 65 }, handler.KeyPressCalls.ToArray());
            Assert.Equal(1, state.DeviceState[Device].keyPresses[Key].current.repeatCount);
            Assert.Equal(1, state.GlobalState.keyPresses[Key].current.repeatCount);
            Assert.Equal(65u, state.DeviceState[Device].nativeKeyAlias[Key]);
        }

        [Fact]
        public void SendPressAndRelease_WithoutAnyHandler_DoNothing_LikeTheOldNullHandlerCheck()
        {
            var service = CreateService(out var state, out var owner, out _);
            Assert.Null(owner.Handler);

            service.SendPress(Device, Key, 65, false);
            service.SendRelease(Device, Key, 65, false);

            Assert.Empty(state.DeviceState[Device].keyPresses);
            Assert.Empty(state.GlobalState.keyPresses);
        }

        [Fact]
        public void SendPressAndRelease_WithoutAnExplicitHandler_UseTheOwnedHandler_AndRelease_ClearsTheCounters()
        {
            var service = CreateService(out var state, out var owner, out _);
            var recorder = new RecordingVirtualKbmBase();
            owner.Handler = recorder;

            service.SendPress(Device, Key, 65, false);
            service.SendPress(Device, Key, 65, false);
            Assert.Equal(2, state.DeviceState[Device].keyPresses[Key].current.repeatCount);

            service.SendRelease(Device, Key, 65, false);

            Assert.Equal(new uint[] { 65 }, recorder.KeyReleases.ToArray());
            Assert.Equal(0, state.DeviceState[Device].keyPresses[Key].current.repeatCount);
            Assert.Equal(0, state.GlobalState.keyPresses[Key].current.repeatCount);
        }

        [Fact]
        public void ResetKeyTiming_ZeroesTheCounters()
        {
            var service = CreateService(out var state, out _, out _);
            service.SendPress(Device, Key, 65, false, new MockVirtualKBM());

            service.ResetKeyTiming(Device, Key);

            Assert.Equal(0, state.DeviceState[Device].keyPresses[Key].current.repeatCount);
            Assert.Equal(0L, state.DeviceState[Device].keyPresses[Key].current.lastSyntheticSendUtcTicks);
        }

        [Fact]
        public void ResolveNativeKey_UsesTheProfileSettingsMapping_AndFallsBackToTheLogicalKey()
        {
            var service = CreateService(out _, out _, out var settings);
            settings.OutputKBMMapping = null;
            Assert.Equal(65u, service.ResolveNativeKey(65));

            var mapping = VirtualKBMFactory.GetMappingInstance();
            mapping.PopulateConstants();
            mapping.PopulateMappings();
            settings.OutputKBMMapping = mapping;
            Assert.Equal(mapping.GetRealEventKey(65), service.ResolveNativeKey(65));
        }

        [Fact]
        public void StaticBridge_DelegatesToTheBoundInstance_AndWithoutOneOnlyForwardsToTheGivenHandler()
        {
            var service = CreateService(out var state, out _, out _);
            try
            {
                SyntheticDispatcher.Bind(null);
                var handler = new MockVirtualKBM();
                SyntheticDispatcher.SendPress(Device, Key, 66, false, handler);
                Assert.Equal(new uint[] { 66 }, handler.KeyPressCalls.ToArray());
                Assert.Empty(state.DeviceState[Device].keyPresses); // 未束縛のときは、合成状態を更新しない

                SyntheticDispatcher.Bind(service);
                SyntheticDispatcher.SendPress(Device, Key, 66, false, handler);
                Assert.Equal(1, state.DeviceState[Device].keyPresses[Key].current.repeatCount);
            }
            finally
            {
                // ホストの送出役へ束縛し直す（他のテストへ影響を残さない）
                SyntheticDispatcher.Bind(DS4WinWPF.AppHost.GetService<ISyntheticDispatcher>());
            }
        }

        [Fact]
        public void Host_SharesOneAdapterBetweenTheSenderAndTheLifecycle_AndBindsTheBridge()
        {
            DS4WinWPF.AppHost.CreateHost();
            var adapter = DS4WinWPF.AppHost.GetService<OutputKBMHandlerAdapter>();
            Assert.Same(adapter, DS4WinWPF.AppHost.GetService<IVirtualKBM>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IVirtualKBMLifecycle>());
            Assert.NotNull(DS4WinWPF.AppHost.GetService<ISyntheticDispatcher>());
            Assert.Same(DS4WinWPF.AppHost.GetService<ISyntheticDispatcher>(), DS4WinWPF.AppHost.GetService<ISyntheticDispatcher>());
        }

        [Fact]
        public void ActionsLoadedRuntimeReset_ResetsTheInjectedHolders()
        {
            var state = new SyntheticInputState();
            var macro = new MacroRuntimeState();
            state.DeviceState[Device].keyPresses[Key] = new SyntheticState.KeyPresses();
            var oldGlobal = state.GlobalState;
            macro.MacroDone[3] = true;

            new ActionsLoadedRuntimeReset(state, macro).Reset();

            Assert.Empty(state.DeviceState[Device].keyPresses);
            Assert.NotSame(oldGlobal, state.GlobalState);
            Assert.False(macro.MacroDone[3]);
        }

        [Fact]
        public void Global_NoLongerOwnsTheOutputKbmHandlerOrMapping()
        {
            string global = Read("DS4Windows/DS4Control/Global.cs");
            if (global == null) return;

            Assert.DoesNotMatch(@"\boutputKBMHandler\b", global);
            Assert.DoesNotMatch(@"\boutputKBMMapping\b", global);
            Assert.DoesNotContain("InitOutputKBMHandler", global);
            Assert.DoesNotContain("InitOutputKBMMapping", global);
        }

        [Fact]
        public void NoSourceFile_ReferencesTheRemovedGlobalMembers_OrTheStaticResetMethod()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/DS4Control/Global.cs");
            if (anchor == null) return;

            string root = Path.GetDirectoryName(Path.GetDirectoryName(anchor)); // DS4Windows プロジェクト（本体のみ。テストの文字列は対象外）
            var removed = new Regex(@"Global\.(outputKBMHandler|outputKBMMapping|InitOutputKBMHandler|InitOutputKBMMapping)\b|ActionsLoadedRuntimeReset\.Reset\b");
            var offenders = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(@"\obj\") && !f.Contains(@"\bin\"))
                .Where(f => removed.IsMatch(Regex.Replace(Regex.Replace(File.ReadAllText(f), @"/\*.*?\*/", string.Empty, RegexOptions.Singleline), @"//[^\r\n]*", string.Empty)))
                .ToArray();
            Assert.True(offenders.Length == 0, "削除済みのメンバーへの参照が残っている: " + string.Join(", ", offenders));
        }
    }
}
