using System.ComponentModel;
using System.Linq;
using Xunit;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// SpecialActionsListViewModel.SortActions の是正（チェックボックス列クリック時に
    /// 「直前のソート列・方向」が第2ソートキーとして使われず、Active 列の昇順の向きも
    /// 逆になっていたバグの修正。さらに、XAML 側のヘッダー Tag="Trigger"/"Action" と
    /// GetColumnComparison のキー名（旧: "TypeName"/"Controls"）が不一致で、
    /// これら2列をクリックしてもヘッダーの矢印だけ動いて実際には並び替わらなかった
    /// バグの修正）を検証する。
    /// </summary>
    public class SpecialActionsListViewModelSortTests
    {
        private static SpecialActionsListViewModel CreateVm(params (string name, bool active)[] items)
        {
            // 実サービスに依存しないコンストラクタ引数（すべて null）で軽量に生成する。
            var vm = new SpecialActionsListViewModel(0, null, null, null, null, null);
            int index = 0;
            foreach (var (name, active) in items)
            {
                var item = new SpecialActionItem(null, name, index++) { Active = active };
                vm.ActionCol.Add(item);
            }
            return vm;
        }

        [Fact]
        public void SortActions_ActiveAscending_ChecksSortToTop()
        {
            var vm = CreateVm(("Bravo", false), ("Alpha", true), ("Charlie", false), ("Delta", true));

            vm.SortActions("Active", ListSortDirection.Ascending);

            // チェック有り(true)が先頭2件、以降チェック無し(false)。
            Assert.Equal(new[] { true, true, false, false }, vm.ActionCol.Select(i => i.Active));
        }

        [Fact]
        public void SortActions_ActiveAscending_SecondaryKeyIsPreviousSortColumn_NameAscending()
        {
            // 既定状態（コンストラクタ直後）は Name 昇順が「直前のソート」として扱われる。
            var vm = CreateVm(("Bravo", true), ("Alpha", true), ("Delta", false), ("Charlie", false));

            vm.SortActions("Active", ListSortDirection.Ascending);

            // Active(true) 同士は名前昇順（Alpha, Bravo）、Active(false) 同士も名前昇順（Charlie, Delta）。
            Assert.Equal(new[] { "Alpha", "Bravo", "Charlie", "Delta" }, vm.ActionCol.Select(i => i.ActionName));
        }

        [Fact]
        public void SortActions_ActiveDescending_UnchecksSortToTop()
        {
            var vm = CreateVm(("Alpha", true), ("Bravo", false));

            vm.SortActions("Active", ListSortDirection.Descending);

            Assert.Equal(new[] { false, true }, vm.ActionCol.Select(i => i.Active));
        }

        [Fact]
        public void SortActions_SameColumnTwice_NoSecondaryKey_TieBreaksByName()
        {
            var vm = CreateVm(("Charlie", true), ("Alpha", true), ("Bravo", false));

            vm.SortActions("Active", ListSortDirection.Ascending);
            // 直前も Active（同一列）のため第2キーは適用されず、Active 同値の場合は
            // 最終タイブレーク（アクション名昇順）のみで並ぶ。
            vm.SortActions("Active", ListSortDirection.Ascending);

            Assert.Equal(new[] { "Alpha", "Charlie", "Bravo" }, vm.ActionCol.Select(i => i.ActionName));
        }

        [Fact]
        public void SortActions_UpdatesCurrentSortColumnAndDirection()
        {
            var vm = CreateVm(("Alpha", true));

            Assert.Equal("Name", vm.CurrentSortColumn);
            Assert.True(vm.CurrentSortAscending);

            vm.SortActions("Active", ListSortDirection.Descending);

            Assert.Equal("Active", vm.CurrentSortColumn);
            Assert.False(vm.CurrentSortAscending);
            Assert.Equal(ListSortDirection.Descending, vm.CurrentSortDirection);
        }

        [Fact]
        public void SortActions_PreservesPreviousSortDirection_AsSecondaryKey()
        {
            var vm = CreateVm(("Bravo", true), ("Alpha", true), ("Charlie", false), ("Delta", false));

            // まず名前を降順にしておく（「直前のソート」を降順にする）。
            vm.SortActions("Name", ListSortDirection.Descending);
            // 続けて Active 昇順でソート: 第2キーは「名前・降順」を維持する。
            vm.SortActions("Active", ListSortDirection.Ascending);

            Assert.Equal(new[] { "Bravo", "Alpha", "Delta", "Charlie" }, vm.ActionCol.Select(i => i.ActionName));
        }

        // ---- Trigger/Action 列（XAML 側の Tag="Trigger"/"Action" に対応。
        //      実データは Controls/TypeName プロパティ。過去に Tag 値と
        //      GetColumnComparison のキー名が不一致で、クリックしてもヘッダーの
        //      矢印だけ動いて実際には並び替わらないバグがあったため、回帰防止に追加）----

        private static SpecialActionsListViewModel CreateVmWithControlsAndType(
            params (string name, string controls, string typeName)[] items)
        {
            var vm = new SpecialActionsListViewModel(0, null, null, null, null, null);
            int index = 0;
            foreach (var (name, controls, typeName) in items)
            {
                var item = new SpecialActionItem(null, name, index++)
                {
                    Controls = controls,
                    TypeName = typeName
                };
                vm.ActionCol.Add(item);
            }
            return vm;
        }

        [Fact]
        public void SortActions_TriggerColumn_SortsByControlsProperty()
        {
            var vm = CreateVmWithControlsAndType(
                ("Alpha", "R1", "Key"),
                ("Bravo", "L1", "Macro"),
                ("Charlie", "PS+R1", "Key"));

            vm.SortActions("Trigger", ListSortDirection.Ascending);

            Assert.Equal(new[] { "L1", "PS+R1", "R1" }, vm.ActionCol.Select(i => i.Controls));
        }

        [Fact]
        public void SortActions_ActionColumn_SortsByTypeNameProperty()
        {
            var vm = CreateVmWithControlsAndType(
                ("Alpha", "R1", "Macro"),
                ("Bravo", "L1", "Key"),
                ("Charlie", "PS+R1", "ProfileSwitch"));

            vm.SortActions("Action", ListSortDirection.Ascending);

            Assert.Equal(new[] { "Key", "Macro", "ProfileSwitch" }, vm.ActionCol.Select(i => i.TypeName));
        }
    }
}