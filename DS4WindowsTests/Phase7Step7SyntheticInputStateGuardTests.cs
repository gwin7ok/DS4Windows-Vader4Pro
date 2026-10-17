using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step7 W2: 合成入力の状態（globalState／deviceState／syncStateLock）を <c>SyntheticInputState</c> へ切り出したことと、
    /// <c>BackingStore</c> から <c>Mapping</c> への逆参照を <c>ActionsLoaded</c> イベントへ置き換えたことの回帰ガード。
    /// ソースが見つからない環境では、検査を行わず合格とする。判断の根拠は Phase7-Step7-Plan.md（Q2〜Q5）。
    /// </summary>
    public class Phase7Step7SyntheticInputStateGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step7SyntheticInputStateGuardTests(ITestOutputHelper output)
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
        public void Mapping_HoldsNoSyntheticStateOfItsOwn_OnlyForwarders()
        {
            string mapping = Read("DS4Windows/DS4Control/Mapping.cs");
            if (mapping == null) return;

            Assert.DoesNotContain("public class SyntheticState", mapping);
            Assert.DoesNotContain("new ReaderWriterLockSlim()", mapping);
            Assert.DoesNotContain("new SyntheticState()", mapping);
            // Phase7-Step8 W2: 転送プロパティは削除し、コンストラクターで受けた保持クラスの配列を readonly で複製して持つ
            Assert.Contains("private readonly SyntheticInputState _syntheticInput;", mapping);
            Assert.Contains("private readonly ReaderWriterLockSlim syncStateLock;", mapping);
            Assert.Contains("private readonly SyntheticState[] deviceState;", mapping);
            Assert.Contains("syncStateLock = syntheticInput.SyncStateLock;", mapping);
            Assert.Contains("deviceState = syntheticInput.DeviceState;", mapping);
            Assert.DoesNotContain("SyntheticInputState.Shared", mapping);
        }

        [Fact]
        public void BackingStore_DoesNotReferenceMapping_AndRaisesActionsLoadedInBothPaths()
        {
            string store = Read("DS4Windows/DS4Control/BackingStore.cs");
            if (store == null) return;

            Assert.False(Regex.IsMatch(store, @"\bMapping\."), "BackingStore が Mapping を参照している");
            Assert.Contains("public event Action ActionsLoaded;", store);
            // Phase7-Step10c W2: Actions.xml の読込（2 経路）は SpecialActionsXmlSerializer へ移した。発行元（イベントとメソッド）は BackingStore のまま
            Assert.Contains("public void RaiseActionsLoaded()", store);
            string actionsXml = Read("DS4Windows/DS4Control/Services/SpecialActionsXmlSerializer.cs");
            if (actionsXml == null) return;
            Assert.Equal(2, Regex.Matches(actionsXml, @"store\.RaiseActionsLoaded\(\);").Count);
        }

        [Fact]
        public void Registration_SubscribesTheResetOnce_AndRegistersTheSharedState()
        {
            string registration = Read("DS4Windows/DI/ServiceRegistration.cs");
            if (registration == null) return;

            // Phase7-Step8 W3: 購読者は instance（保持クラスをコンストラクターで受ける）。前回の購読者を外してから登録する
            Assert.Contains("store.ActionsLoaded -= _subscribedReset.Reset;", registration);
            Assert.Contains("store.ActionsLoaded += reset.Reset;", registration);
            Assert.Contains("services.AddSingleton<SyntheticInputState>();", registration); // Phase7-Step8 W4: Shared は削除した
        }

        [Fact]
        public void ResetHandler_KeepsTheOrderAndTheLogMessage()
        {
            string reset = Read("DS4Windows/DS4Control/Services/ActionsLoadedRuntimeReset.cs");
            if (reset == null) return;

            int state = reset.IndexOf("_syntheticInput.ResetAll();");
            int toggled = reset.IndexOf("ActionManager.ClearAllToggledOn();");
            int macro = reset.IndexOf("_macroRuntime.ClearMacroDone();");
            int log = reset.IndexOf("Runtime synthetic state reset after Actions load");
            Assert.True(state >= 0 && state < toggled && toggled < macro && macro < log);
        }

        [Fact]
        public void SyntheticDispatcherService_UsesTheInjectedHolder_AndTheStaticBridgeHoldsNoState()
        {
            string service = Read("DS4Windows/DS4Control/Services/SyntheticDispatcherService.cs");
            string bridge = Read("DS4Windows/DS4Control/SyntheticDispatcher.cs");
            if (service == null || bridge == null) return;

            Assert.DoesNotContain("Mapping.deviceState", service);
            Assert.DoesNotContain("Mapping.globalState", service);
            Assert.DoesNotContain("Shared", service);
            Assert.Contains("_syntheticInput.DeviceState", service);

            // Phase7-Step8 W3（Q6＝A）: 暫定の静的な橋渡しは、状態もフォールバックも持たない
            Assert.DoesNotContain("SyntheticInputState", bridge);
            Assert.DoesNotContain("Global.", bridge);
        }
    }
}
