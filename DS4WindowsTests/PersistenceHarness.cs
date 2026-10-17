using System;
using System.IO;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10c W1: 設定ファイルの保存・読込を呼ぶ窓口（ゴールデンテスト用）。
    /// 切り出し（W2 以降）で永続化の実装が <c>BackingStore</c> から新しい型へ移るたびに、ここだけを付け替える。
    /// ゴールデンテストの本体（入力・期待値・照合）は変えない。
    /// </summary>
    internal static class PersistenceHarness
    {
        /// <summary>プロファイルの保存・読込・プリセットに使うスロット（編集用の作業スロット。ControlService の状態に触れない）。</summary>
        public const int Slot = ControllerLimits.TestProfileIndex;

        public static BackingStore NewStore() => new BackingStore();

        // ---- プロファイル（旧形式の XML）----

        public static string SaveProfile(PersistenceGoldenContext ctx, BackingStore store, string name)
        {
            Assert.True(TestRepositoryFactory.CreateLegacyProfileXml(store).SaveProfile(Slot, name), "プロファイルの保存に失敗した");
            return File.ReadAllText(Path.Combine(ctx.AppData, "Profiles", name + ".xml"));
        }

        public static BackingStore LoadProfile(PersistenceGoldenContext ctx, string xml, string name)
        {
            string path = Path.Combine(ctx.AppData, "Profiles", name + ".xml");
            File.WriteAllText(path, xml);
            BackingStore store = NewStore();
            Assert.True(TestRepositoryFactory.CreateLegacyProfileXml(store).LoadProfile(Slot, false, ctx.Control, path, false, false), "プロファイルの読込に失敗した");
            return store;
        }

        // ---- アプリ設定（Profiles.xml）----

        public static string SaveAppSettings(PersistenceGoldenContext ctx, BackingStore store)
        {
            string path = Path.Combine(ctx.AppData, "Profiles.xml");
            store.m_Profile = path;
            Assert.True(TestRepositoryFactory.CreateAppSettingsXml(store).Save(), "アプリ設定の保存に失敗した");
            return File.ReadAllText(path);
        }

        public static BackingStore LoadAppSettings(PersistenceGoldenContext ctx, string xml)
        {
            string path = Path.Combine(ctx.AppData, "Profiles.xml");
            File.WriteAllText(path, xml);
            BackingStore store = NewStore();
            store.m_Profile = path;
            Assert.True(TestRepositoryFactory.CreateAppSettingsXml(store).Load(), "アプリ設定の読込に失敗した");
            return store;
        }

        // ---- スペシャルアクション（Actions.xml）----

        public static string SaveActions(PersistenceGoldenContext ctx, BackingStore store)
        {
            string path = Path.Combine(ctx.AppData, "Actions.xml");
            store.m_Actions = path;
            var xml = new DS4Windows.Services.SpecialActionsXmlSerializer(store);
            // モード: 1＝マクロ、2＝プログラム、3＝プロファイル、4＝キー、5＝切断、6＝バッテリー確認、7＝複数アクション、8＝ホイール較正、10＝ボタン
            xml.SaveAction("Golden_Macro", "Cross", 1, "65/66/67", false, "Hold");
            xml.SaveAction("Golden_Program", "Circle", 2, "C:/golden/a.exe?3", false, "-arg");
            xml.SaveAction("Golden_Profile", "Square", 3, "Golden_ProfileName", false, "Triangle");
            xml.SaveAction("Golden_Key", "L1", 4, "65", false, "Toggle\nR1");
            xml.SaveAction("Golden_Disconnect", "L2", 5, "5", false);
            xml.SaveAction("Golden_Battery", "R2", 6, "50", false);
            xml.SaveAction("Golden_Multi", "Cross/Circle", 7, "Golden_Macro/Golden_Program", false);
            xml.SaveAction("Golden_Edit", "Triangle", 2, "C:/golden/b.exe?0", false);
            xml.SaveAction("Golden_Edit", "Square", 2, "C:/golden/c.exe?1", true);
            return File.ReadAllText(path);
        }

        public static BackingStore LoadActions(PersistenceGoldenContext ctx, string xml)
        {
            string path = Path.Combine(ctx.AppData, "Actions.xml");
            File.WriteAllText(path, xml);
            BackingStore store = NewStore();
            store.m_Actions = path;
            Assert.True(new DS4Windows.Services.SpecialActionsXmlSerializer(store).LoadActions(), "アクションの読込に失敗した");
            return store;
        }

        // ---- リンクプロファイル（LinkedProfiles.xml）----

        public static string SaveLinkedProfiles(PersistenceGoldenContext ctx, BackingStore store)
        {
            string path = Path.Combine(ctx.AppData, "LinkedProfiles.xml");
            store.m_linkedProfiles = path;
            var linked = new DS4Windows.Services.LinkedProfilesXmlSerializer(store);
            Assert.True(linked.createLinkedProfiles(), "リンクプロファイルの新規作成に失敗した");
            store.linkedProfiles["0123456789AB"] = "Golden_ProfileA";
            store.linkedProfiles["BA9876543210"] = "Golden_ProfileB";
            Assert.True(linked.SaveLinkedProfiles(), "リンクプロファイルの保存に失敗した");
            return File.ReadAllText(path);
        }

        public static BackingStore LoadLinkedProfiles(PersistenceGoldenContext ctx, string xml)
        {
            string path = Path.Combine(ctx.AppData, "LinkedProfiles.xml");
            File.WriteAllText(path, xml);
            BackingStore store = NewStore();
            store.m_linkedProfiles = path;
            Assert.True(new DS4Windows.Services.LinkedProfilesXmlSerializer(store).LoadLinkedProfiles(), "リンクプロファイルの読込に失敗した");
            return store;
        }

        // ---- プリセット ----

        public static readonly string[] PresetNames =
        {
            "LoadBlankDS4Profile", "LoadBlankProfile", "LoadDefaultGamepadGyroProfile", "LoadDefaultDS4GamepadGyroProfile",
            "LoadDefaultMixedGyroMouseProfile", "LoadDefaultDS4MixedGyroMouseProfile", "LoadDefaultDS4MixedControlsProfile",
            "LoadDefaultMixedControlsProfile", "LoadDefaultKBMProfile", "LoadDefaultKBMGyroMouseProfile",
        };

        public static BackingStore LoadPreset(PersistenceGoldenContext ctx, string presetName)
        {
            BackingStore store = NewStore();
            var s = new DS4Windows.Services.ProfilePresets(store);
            ControlService c = ctx.Control;
            switch (presetName)
            {
                case "LoadBlankDS4Profile": s.LoadBlankDS4Profile(Slot, false, c, "", false, false); break;
                case "LoadBlankProfile": s.LoadBlankProfile(Slot, false, c, "", false, false); break;
                case "LoadDefaultGamepadGyroProfile": s.LoadDefaultGamepadGyroProfile(Slot, false, c, "", false, false); break;
                case "LoadDefaultDS4GamepadGyroProfile": s.LoadDefaultDS4GamepadGyroProfile(Slot, false, c, "", false, false); break;
                case "LoadDefaultMixedGyroMouseProfile": s.LoadDefaultMixedGyroMouseProfile(Slot, false, c, "", false, false); break;
                case "LoadDefaultDS4MixedGyroMouseProfile": s.LoadDefaultDS4MixedGyroMouseProfile(Slot, false, c, "", false, false); break;
                case "LoadDefaultDS4MixedControlsProfile": s.LoadDefaultDS4MixedControlsProfile(Slot, false, c, "", false, false); break;
                case "LoadDefaultMixedControlsProfile": s.LoadDefaultMixedControlsProfile(Slot, false, c, "", false, false); break;
                case "LoadDefaultKBMProfile": s.LoadDefaultKBMProfile(Slot, false, c, "", false, false); break;
                case "LoadDefaultKBMGyroMouseProfile": s.LoadDefaultKBMGyroMouseProfile(Slot, false, c, "", false, false); break;
                default: throw new ArgumentException(presetName);
            }

            return store;
        }
    }
}
