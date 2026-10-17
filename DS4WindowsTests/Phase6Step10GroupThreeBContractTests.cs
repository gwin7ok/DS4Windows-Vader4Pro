using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-3b-1: アクションの名前変更・削除を全プロファイルへ反映する契約
    /// （<see cref="IProfileRepository.ReplaceActionReference"/>／<see cref="IProfileRepository.RemoveActionReference"/>）の検証。
    /// - すべてのプロファイル XML の &lt;ProfileActions&gt; が対象であること
    /// - 項目単位の完全一致であること（部分一致では書き換えない）
    /// - 一致したファイルだけが書き換わり、書き換えたプロファイル名が返ること
    /// </summary>
    public class Phase6Step10GroupThreeBContractTests : IDisposable
    {
        private readonly string _dir;
        private readonly string _profiles;
        private readonly ProfileRepository _repo;

        public Phase6Step10GroupThreeBContractTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "Phase6Step10_3b_" + Guid.NewGuid().ToString("N"));
            _profiles = Path.Combine(_dir, "Profiles");
            Directory.CreateDirectory(_profiles);
            _repo = TestRepositoryFactory.CreateProfileRepository(TestRepositoryFactory.CreateProfileSettingsService(), TestRepositoryFactory.CreateProfileXmlStore(), new PathService(_dir));
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, true); } catch { }
        }

        private void WriteProfile(string name, string profileActions)
        {
            string body = profileActions == null
                ? "<DS4Windows><Other>1</Other></DS4Windows>"
                : $"<DS4Windows><ProfileActions>{profileActions}</ProfileActions></DS4Windows>";
            File.WriteAllText(Path.Combine(_profiles, name + ".xml"), "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + body);
        }

        private string ReadActions(string name)
        {
            var doc = new XmlDocument();
            doc.Load(Path.Combine(_profiles, name + ".xml"));
            return doc.SelectSingleNode("DS4Windows/ProfileActions")?.InnerText;
        }

        [Fact]
        public void ReplaceActionReference_RewritesAllMatchingProfiles_OnlyOnExactItemMatch()
        {
            WriteProfile("A", "Old/Keep");
            WriteProfile("B", "Keep/Old");
            WriteProfile("C", "Old10/Other");   // 部分一致では書き換えない
            WriteProfile("D", "Keep");
            WriteProfile("E", null);            // ProfileActions 自体が無い

            IReadOnlyList<string> changed = _repo.ReplaceActionReference("Old", "New");

            Assert.Equal(new[] { "A", "B" }, changed.OrderBy(x => x).ToArray());
            Assert.Equal("New/Keep", ReadActions("A"));
            Assert.Equal("Keep/New", ReadActions("B"));
            Assert.Equal("Old10/Other", ReadActions("C"));
            Assert.Equal("Keep", ReadActions("D"));
            Assert.Null(ReadActions("E"));
        }

        [Fact]
        public void ReplaceActionReference_IsCaseSensitive()
        {
            WriteProfile("A", "old");

            Assert.Empty(_repo.ReplaceActionReference("Old", "New"));
            Assert.Equal("old", ReadActions("A"));
        }

        [Fact]
        public void ReplaceActionReference_DoesNotDuplicateExistingNewName()
        {
            WriteProfile("A", "Old/New/Keep");

            _repo.ReplaceActionReference("Old", "New");

            Assert.Equal("New/Keep", ReadActions("A"));
        }

        [Fact]
        public void ReplaceActionReference_IgnoresBlankOrUnchangedNames_AndLeavesFilesUntouched()
        {
            WriteProfile("A", "Old");
            DateTime before = File.GetLastWriteTimeUtc(Path.Combine(_profiles, "A.xml"));

            Assert.Empty(_repo.ReplaceActionReference("Old", "Old"));
            Assert.Empty(_repo.ReplaceActionReference("", "New"));
            Assert.Empty(_repo.ReplaceActionReference("Old", " "));

            Assert.Equal("Old", ReadActions("A"));
            Assert.Equal(before, File.GetLastWriteTimeUtc(Path.Combine(_profiles, "A.xml")));
        }

        [Fact]
        public void RemoveActionReference_RemovesFromAllMatchingProfiles()
        {
            WriteProfile("A", "Gone/Keep");
            WriteProfile("B", "Gone");
            WriteProfile("C", "Keep");
            WriteProfile("D", "Gone10");

            IReadOnlyList<string> changed = _repo.RemoveActionReference("Gone");

            Assert.Equal(new[] { "A", "B" }, changed.OrderBy(x => x).ToArray());
            Assert.Equal("Keep", ReadActions("A"));
            Assert.Equal(string.Empty, ReadActions("B"));
            Assert.Equal("Keep", ReadActions("C"));
            Assert.Equal("Gone10", ReadActions("D"));
        }

        [Fact]
        public void RemoveActionReference_ReturnsEmptyWhenNothingMatches_OrFolderIsMissing()
        {
            WriteProfile("A", "Keep");
            Assert.Empty(_repo.RemoveActionReference("Missing"));

            Directory.Delete(_profiles, true);
            Assert.Empty(_repo.RemoveActionReference("Missing"));
            Assert.Empty(_repo.ReplaceActionReference("Old", "New"));
        }

        [Fact]
        public void ActionReferenceUpdate_SkipsUnreadableProfile_AndContinues()
        {
            File.WriteAllText(Path.Combine(_profiles, "Broken.xml"), "<DS4Windows><ProfileActions>Old");
            WriteProfile("A", "Old");

            IReadOnlyList<string> changed = _repo.ReplaceActionReference("Old", "New");

            Assert.Equal(new[] { "A" }, changed.ToArray());
            Assert.Equal("New", ReadActions("A"));
        }

        [Fact]
        public void GetAppliedProfilesUsingAction_ReturnsProfilesOfSlotsThatUseTheAction()
        {
            // Global の実行時状態（スロット別のプロファイル名とアクション一覧）を退避し、必ず戻す
            string[] paths = Global.store.profilePath;
            List<string>[] actions = Global.store.profileActions;
            string path0 = paths[0], path1 = paths[1], path2 = paths[2];
            List<string> actions0 = actions[0], actions1 = actions[1], actions2 = actions[2];
            try
            {
                paths[0] = "P0"; actions[0] = new List<string> { "ActA", "ActB" };
                paths[1] = "P1"; actions[1] = new List<string> { "ActB" };
                paths[2] = "P0"; actions[2] = new List<string> { "ActA" };   // 同じプロファイルは 1 回だけ

                Assert.Equal(new[] { "P0" }, _repo.GetAppliedProfilesUsingAction("ActA").ToArray());
                Assert.Equal(new[] { "P0", "P1" }, _repo.GetAppliedProfilesUsingAction("ActB").ToArray());
                Assert.Empty(_repo.GetAppliedProfilesUsingAction("Act"));      // 部分一致はしない
                Assert.Empty(_repo.GetAppliedProfilesUsingAction(""));
            }
            finally
            {
                paths[0] = path0; paths[1] = path1; paths[2] = path2;
                actions[0] = actions0; actions[1] = actions1; actions[2] = actions2;
            }
        }
    }
}
