using System;
using System.IO;
using System.Linq;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    public class SpecialActionRepositoryTests
    {
        [Fact]
        public void ActionsPath_ShouldReturnValidPath()
        {
            var repo = TestRepositoryFactory.CreateSpecialActionRepository(new BackingStore());
            var path = repo.ActionsPath;
            Assert.False(string.IsNullOrWhiteSpace(path));
            Assert.EndsWith("Actions.xml", path, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetAction_ShouldReturnItemInList()
        {
            // Phase7-Step2 W5: AddAction（メモリ上の並行実装）を削除したため、一覧への追加は ActionList で行う。
            var repo = TestRepositoryFactory.CreateSpecialActionRepository(new BackingStore());
            var action = new SpecialAction("TestAction", "Touchpad", "Key", "10", 0);

            repo.ActionList.Add(action);

            var retrieved = repo.GetAction("TestAction");
            Assert.NotNull(retrieved);
            Assert.Equal("TestAction", retrieved.name);

            Assert.True(repo.ActionExists("TestAction"));
            Assert.False(repo.ActionExists("NonExistentAction"));
        }

        [Fact]
        public void SpecialActionRepository_Modifications_ShouldReflectInBackingStore()
        {
            var backingStore = new BackingStore();
            var repo = TestRepositoryFactory.CreateSpecialActionRepository(backingStore);

            var action = new SpecialAction("BackingStoreTest", "Circle", "Key", "30", 0);
            repo.ActionList.Add(action);

            // BackingStore.actions に直接反映されていることを検証（二重管理解消の証明）
            Assert.Contains(backingStore.actions, a => a.name == "BackingStoreTest");
            Assert.Same(action, repo.ActionList.First(a => a.name == "BackingStoreTest"));
        }
    }
}
