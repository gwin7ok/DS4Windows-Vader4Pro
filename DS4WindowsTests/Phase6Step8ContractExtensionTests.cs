using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step8-1: <c>ProfileEditor.xaml.cs</c> の Global 直接参照解消に先立って追加した契約を検証する。
    /// - <see cref="ISpecialActionRepository.GetActionOrPlaceholder"/>: <c>Global.GetAction</c> と同じ契約
    ///   （前後の空白と大文字小文字を無視、未発見は <c>null</c> ではなく placeholder）。
    ///   既存の <see cref="ISpecialActionRepository.GetAction"/> は <c>null</c> を返す別契約のため、置換できない。
    /// - （<c>IProfileRepository.LoadBlankProfile</c> の確認は、Phase7-Step3c W2 で契約ごと削除した。<c>IProfilePresetLoader.LoadBlank</c> に統合）
    /// Global 状態を変更するテストは、変更を必ず元に戻す（Phase6-Step2 の教訓）。
    /// </summary>
    public class Phase6Step8ContractExtensionTests
    {
        private readonly ITestOutputHelper _output;

        public Phase6Step8ContractExtensionTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static BackingStore CreateStoreWithAction(string name)
        {
            var store = new BackingStore();
            store.actions.Clear();
            store.actions.Add(new SpecialAction(name, "PS", "DisconnectBT", "0"));
            return store;
        }

        [Fact]
        public void GetActionOrPlaceholder_FoundByExactName_ReturnsTheAction()
        {
            var store = CreateStoreWithAction("MyAction");
            var repo = TestRepositoryFactory.CreateSpecialActionRepository(store);

            SpecialAction result = repo.GetActionOrPlaceholder("MyAction");

            Assert.Same(store.actions[0], result);
        }

        [Theory]
        [InlineData("  MyAction ")]
        [InlineData("myaction")]
        [InlineData("MYACTION")]
        public void GetActionOrPlaceholder_IgnoresSurroundingWhitespaceAndCase(string requested)
        {
            var store = CreateStoreWithAction("MyAction");
            var repo = TestRepositoryFactory.CreateSpecialActionRepository(store);

            SpecialAction result = repo.GetActionOrPlaceholder(requested);

            Assert.Same(store.actions[0], result);
        }

        [Theory]
        [InlineData("NoSuchAction")]
        [InlineData("")]
        [InlineData(null)]
        public void GetActionOrPlaceholder_WhenNotFound_ReturnsPlaceholderNotNull(string requested)
        {
            var store = CreateStoreWithAction("MyAction");
            var repo = TestRepositoryFactory.CreateSpecialActionRepository(store);

            SpecialAction result = repo.GetActionOrPlaceholder(requested);

            Assert.NotNull(result);
            Assert.Equal("null", result.name);
            Assert.Equal("null", result.controls);
            Assert.Equal("null", result.type);
            Assert.NotSame(store.actions[0], result);
        }

        [Fact]
        public void GetAction_WhenNotFound_StillReturnsNull_ContractDiffersFromPlaceholder()
        {
            // 契約差の記録: 既存の GetAction は未発見で null を返す。ProfileEditor は placeholder 契約を必要とする。
            var store = CreateStoreWithAction("MyAction");
            var repo = TestRepositoryFactory.CreateSpecialActionRepository(store);

            Assert.Null(repo.GetAction("NoSuchAction"));
            Assert.NotNull(repo.GetActionOrPlaceholder("NoSuchAction"));
        }

        [Fact]
        public void GetActionOrPlaceholder_MatchesGlobalGetAction_ForGlobalStore()
        {
            // 既定の SpecialActionRepository は Global.store（= Global.GetAction の実体）を使う。
            var repo = TestRepositoryFactory.CreateSpecialActionRepository();
            SpecialAction viaGlobal = Global.GetAction("DefinitelyMissingAction_Step8");
            SpecialAction viaRepo = repo.GetActionOrPlaceholder("DefinitelyMissingAction_Step8");

            Assert.Equal(viaGlobal.name, viaRepo.name);
            Assert.Equal(viaGlobal.controls, viaRepo.controls);
            Assert.Equal(viaGlobal.type, viaRepo.type);
            Assert.Equal(viaGlobal.typeID, viaRepo.typeID);
        }
    }
}
