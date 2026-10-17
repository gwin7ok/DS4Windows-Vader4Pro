using System.Collections.Generic;
using System.Linq;
using Xunit;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-3b-2: Actions.xml に無く、プロファイルの XML にだけ残っているアクション名が、
    /// スペシャルアクション一覧に「トリガー・アクション欄が空のグレーの項目」として表示されること
    /// （36d124a6 のリファクタで失われていた 40011c12 の動作の復活）。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class SpecialActionsListViewModelMissingActionTests
    {
        private static SpecialActionsListViewModel CreateVm() =>
            new SpecialActionsListViewModel(0, TestRepositoryFactory.CreateSpecialActionRepository(), TestRepositoryFactory.CreateProfileRepository());

        /// <summary>Global.store のアクション一覧を、テスト用の内容へ差し替え、終了時に戻す</summary>
        private sealed class ActionsScope : System.IDisposable
        {
            private readonly List<SpecialAction> _original;
            private readonly string _originalPath;
            private readonly string _dir;

            public ActionsScope(params string[] names)
            {
                // Actions.xml の保存先を、一時ディレクトリへ向ける（削除の保存が、実際の設定を書き換えないようにする）
                _dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Phase6Step10_3b_" + System.Guid.NewGuid().ToString("N"));
                System.IO.Directory.CreateDirectory(_dir);
                _originalPath = Global.store.m_Actions;
                Global.store.m_Actions = System.IO.Path.Combine(_dir, "Actions.xml");

                var list = Global.store.actions;
                _original = new List<SpecialAction>(list);
                list.Clear();
                foreach (string name in names)
                {
                    list.Add(new SpecialAction(name, "Cross", "Program", "C:/a.exe"));
                }
                TestRepositoryFactory.CreateActionsXml().SaveActions();
            }

            public void Dispose()
            {
                Global.store.m_Actions = _originalPath;
                var list = Global.store.actions;
                list.Clear();
                list.AddRange(_original);
                try { System.IO.Directory.Delete(_dir, true); } catch { }
            }
        }

        [Fact]
        public void LoadActions_ShowsProfileOnlyNames_AsMissingGrayItems()
        {
            using var scope = new ActionsScope("ActA", "ActB");
            var vm = CreateVm();

            vm.LoadActions(false, "ActA/Gone");

            Assert.Equal(3, vm.ActionCol.Count);
            var actA = vm.ActionCol.Single(i => i.ActionName == "ActA");
            var actB = vm.ActionCol.Single(i => i.ActionName == "ActB");
            var gone = vm.ActionCol.Single(i => i.ActionName == "Gone");

            Assert.False(actA.IsMissing);
            Assert.True(actA.Active);
            Assert.False(actB.IsMissing);
            Assert.False(actB.Active);

            Assert.True(gone.IsMissing);
            Assert.True(gone.Active);
            Assert.NotNull(gone.SpecialAction);
            Assert.Equal(string.Empty, gone.SpecialAction.controls);
        }

        [Fact]
        public void LoadActions_DoesNotDuplicateExistingNames_IgnoringCase()
        {
            using var scope = new ActionsScope("ActA");
            var vm = CreateVm();

            vm.LoadActions(false, "acta");

            Assert.Single(vm.ActionCol);
            Assert.False(vm.ActionCol[0].IsMissing);
            Assert.True(vm.ActionCol[0].Active);
        }

        [Fact]
        public void LoadActions_MissingItems_CanBeUncheckedToRemoveFromProfileActions()
        {
            using var scope = new ActionsScope("ActA");
            var vm = CreateVm();

            vm.LoadActions(false, "ActA/Gone");

            // 読み込み直後は、プロファイルの内容をそのまま保持する（チェックしたままなら、保存してもプロファイルに残る）
            Assert.Equal("ActA/Gone", vm.ProfileActions);
            Assert.Equal(new[] { "ActA", "Gone" }, vm.GetEnabledActionNames().ToArray());

            // チェックを外せば、保存対象（有効なアクション名）と同期文字列から外れる
            vm.ActionCol.Single(i => i.ActionName == "Gone").Active = false;
            Assert.Equal(new[] { "ActA" }, vm.GetEnabledActionNames().ToArray());
            Assert.Equal("ActA", vm.ProfileActions);
        }

        [Fact]
        public void RemoveAction_ForExistingAction_RemovesFromActionsXmlAndList()
        {
            using var scope = new ActionsScope("ActA", "ActB");
            var vm = CreateVm();
            vm.LoadActions(false, "ActA/ActB");

            vm.RemoveAction(vm.ActionCol.Single(i => i.ActionName == "ActA"));

            Assert.DoesNotContain(vm.ActionCol, i => i.ActionName == "ActA");
            Assert.DoesNotContain(Global.store.actions, a => a.name == "ActA");
            Assert.Contains(Global.store.actions, a => a.name == "ActB");
            string xml = System.IO.File.ReadAllText(Global.store.m_Actions);
            Assert.DoesNotContain("ActA", xml);
            Assert.Contains("ActB", xml);
        }

        [Fact]
        public void RemoveAction_ForMissingItem_OnlyRemovesFromList()
        {
            using var scope = new ActionsScope("ActA");
            var vm = CreateVm();
            vm.LoadActions(false, "ActA/Gone");
            string before = System.IO.File.ReadAllText(Global.store.m_Actions);

            vm.RemoveAction(vm.ActionCol.Single(i => i.ActionName == "Gone"));

            Assert.DoesNotContain(vm.ActionCol, i => i.ActionName == "Gone");
            Assert.Contains(Global.store.actions, a => a.name == "ActA");
            Assert.Equal(before, System.IO.File.ReadAllText(Global.store.m_Actions));
        }

    }
}
