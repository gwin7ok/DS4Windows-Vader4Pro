using System.Text;
using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10c W1（Q2＝A）: 設定ファイルの保存・読込のゴールデンテスト。
    /// 切り出し（W2 以降）の前に、現行のコードで期待値を生成して保存し、切り出しの後も同じ入力から同じ出力・同じ状態になることを照合する。
    /// ゴールデンが無いときは、生成して失敗する（内容を確認してコミットし、再実行する）。置き場所は <c>DS4WindowsTests/Golden/Phase7Step10c/</c>。
    /// 保存した XML は全文を照合し、読込は「読込前の既定の状態」との差（追加行 +、消えた行 -）を照合する。
    /// 対象: プロファイル（既定／変化させた状態）、アプリ設定、スペシャルアクション、リンクプロファイル、プリセット 10 種。
    /// 対象外: コントローラー固有設定（実機のデバイス実体が要る。移動の規模が小さい）。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase7Step10cGoldenPersistenceTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step10cGoldenPersistenceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static string State(PersistenceGoldenContext ctx, BackingStore store)
            => ctx.Normalize(BackingStoreStateDumper.Dump(store));

        private static string StateDiffFromDefault(PersistenceGoldenContext ctx, BackingStore store)
            => BackingStoreStateDumper.Diff(State(ctx, PersistenceHarness.NewStore()), State(ctx, store));

        // ---- プロファイル ----

        [Fact]
        public void Profile_DefaultState_SavedXml_MatchesGolden()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                string xml = PersistenceHarness.SaveProfile(ctx, PersistenceHarness.NewStore(), "GoldenDefault");
                GoldenFile.AssertMatches("profile-default.xml", ctx.Normalize(xml), _output);
            }
        }

        [Fact]
        public void Profile_VariedState_SavedXml_MatchesGolden()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                BackingStore store = PersistenceHarness.NewStore();
                BackingStoreStateFiller.Fill(store, PersistenceHarness.Slot);

                string xml = PersistenceHarness.SaveProfile(ctx, store, "GoldenVaried");
                GoldenFile.AssertMatches("profile-varied.xml", ctx.Normalize(xml), _output);
            }
        }

        [Fact]
        public void Profile_VariedXml_LoadedState_MatchesGolden()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                // 入力は、ゴールデンの保存結果（なければ、いま変化させた状態を保存した結果）。保存側の変化に影響されず、読込だけを固定する
                string xml = GoldenFile.ReadOrNull("profile-varied.xml");
                if (xml == null)
                {
                    BackingStore source = PersistenceHarness.NewStore();
                    BackingStoreStateFiller.Fill(source, PersistenceHarness.Slot);
                    xml = PersistenceHarness.SaveProfile(ctx, source, "GoldenVariedInput");
                }

                BackingStore loaded = PersistenceHarness.LoadProfile(ctx, xml, "GoldenVariedLoad");
                GoldenFile.AssertMatches("profile-varied.state.txt", StateDiffFromDefault(ctx, loaded), _output);
            }
        }

        // ---- アプリ設定 ----

        [Fact]
        public void AppSettings_DefaultState_SavedXml_MatchesGolden()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                string xml = PersistenceHarness.SaveAppSettings(ctx, PersistenceHarness.NewStore());
                GoldenFile.AssertMatches("appsettings-default.xml", ctx.Normalize(xml), _output);
            }
        }

        [Fact]
        public void AppSettings_VariedState_SavedXml_MatchesGolden()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                BackingStore store = PersistenceHarness.NewStore();
                BackingStoreStateFiller.Fill(store, 0);

                string xml = PersistenceHarness.SaveAppSettings(ctx, store);
                GoldenFile.AssertMatches("appsettings-varied.xml", ctx.Normalize(xml), _output);
            }
        }

        [Fact]
        public void AppSettings_VariedXml_LoadedState_MatchesGolden()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                string xml = GoldenFile.ReadOrNull("appsettings-varied.xml");
                if (xml == null)
                {
                    BackingStore source = PersistenceHarness.NewStore();
                    BackingStoreStateFiller.Fill(source, 0);
                    xml = PersistenceHarness.SaveAppSettings(ctx, source);
                }

                BackingStore loaded = PersistenceHarness.LoadAppSettings(ctx, xml);
                GoldenFile.AssertMatches("appsettings-varied.state.txt", StateDiffFromDefault(ctx, loaded), _output);
            }
        }

        // ---- スペシャルアクション ----

        [Fact]
        public void Actions_SavedXml_MatchesGolden()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                string xml = PersistenceHarness.SaveActions(ctx, PersistenceHarness.NewStore());
                GoldenFile.AssertMatches("actions.xml", ctx.Normalize(xml), _output);
            }
        }

        [Fact]
        public void Actions_LoadedState_MatchesGolden()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                string xml = GoldenFile.ReadOrNull("actions.xml") ?? PersistenceHarness.SaveActions(ctx, PersistenceHarness.NewStore());
                BackingStore loaded = PersistenceHarness.LoadActions(ctx, xml);
                GoldenFile.AssertMatches("actions.state.txt", ctx.Normalize(BackingStoreStateDumper.Dump(loaded.actions)), _output);
            }
        }

        // ---- リンクプロファイル ----

        [Fact]
        public void LinkedProfiles_SavedXml_MatchesGolden()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                string xml = PersistenceHarness.SaveLinkedProfiles(ctx, PersistenceHarness.NewStore());
                GoldenFile.AssertMatches("linkedprofiles.xml", ctx.Normalize(xml), _output);
            }
        }

        [Fact]
        public void LinkedProfiles_LoadedState_MatchesGolden()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                string xml = GoldenFile.ReadOrNull("linkedprofiles.xml") ?? PersistenceHarness.SaveLinkedProfiles(ctx, PersistenceHarness.NewStore());
                BackingStore loaded = PersistenceHarness.LoadLinkedProfiles(ctx, xml);
                GoldenFile.AssertMatches("linkedprofiles.state.txt", ctx.Normalize(BackingStoreStateDumper.Dump(loaded.linkedProfiles)), _output);
            }
        }

        // ---- プリセット（10 種。既定の状態との差を 1 ファイルにまとめる）----

        [Fact]
        public void Presets_LoadedState_MatchesGolden()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                var sb = new StringBuilder();
                foreach (string name in PersistenceHarness.PresetNames)
                {
                    sb.Append("## ").Append(name).Append('\n');
                    BackingStore loaded = PersistenceHarness.LoadPreset(ctx, name);
                    sb.Append(StateDiffFromDefault(ctx, loaded)).Append('\n');
                }

                GoldenFile.AssertMatches("presets.state.txt", sb.ToString(), _output);
            }
        }
    }
}
