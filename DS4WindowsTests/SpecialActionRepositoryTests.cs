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
            var repo = new SpecialActionRepository(new BackingStore());
            var path = repo.ActionsPath;
            Assert.False(string.IsNullOrWhiteSpace(path));
            Assert.EndsWith("Actions.xml", path, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AddAndGetAction_ShouldWorkCorrectly()
        {
            var repo = new SpecialActionRepository(new BackingStore());
            var action = new SpecialAction("TestAction", "Touchpad", "Key", "10", 0);

            var added = repo.AddAction(action);
            Assert.True(added);

            var retrieved = repo.GetAction("TestAction");
            Assert.NotNull(retrieved);
            Assert.Equal("TestAction", retrieved.name);

            Assert.True(repo.ActionExists("TestAction"));
            Assert.False(repo.ActionExists("NonExistentAction"));
        }

        [Fact]
        public void RemoveAction_ShouldRemoveItem()
        {
            var repo = new SpecialActionRepository(new BackingStore());
            var action = new SpecialAction("RemoveTestAction", "Touchpad", "Key", "10", 0);

            repo.AddAction(action);
            Assert.True(repo.ActionExists("RemoveTestAction"));

            var removed = repo.RemoveAction("RemoveTestAction");
            Assert.True(removed);
            Assert.False(repo.ActionExists("RemoveTestAction"));
        }

        [Fact]
        public void ReplaceAction_ShouldReplaceExistingItem()
        {
            var repo = new SpecialActionRepository(new BackingStore());
            var action1 = new SpecialAction("ReplaceTest", "Touchpad", "Key", "10", 0);
            var action2 = new SpecialAction("ReplaceTest", "Cross", "Macro", "20", 0);

            repo.AddAction(action1);
            Assert.Equal("Touchpad", repo.GetAction("ReplaceTest").controls);

            bool replaced = repo.ReplaceAction("ReplaceTest", action2);
            Assert.True(replaced);
            Assert.Equal("Cross", repo.GetAction("ReplaceTest").controls);
        }

        [Fact]
        public void SpecialActionRepository_Modifications_ShouldReflectInBackingStore()
        {
            var backingStore = new BackingStore();
            var repo = new SpecialActionRepository(backingStore);

            var action = new SpecialAction("BackingStoreTest", "Circle", "Key", "30", 0);
            repo.AddAction(action);

            // BackingStore.actions に直接反映されていることを検証（二重管理解消の証明）
            Assert.Contains(backingStore.actions, a => a.name == "BackingStoreTest");
            Assert.Same(action, repo.ActionList.First(a => a.name == "BackingStoreTest"));

            repo.RemoveAction("BackingStoreTest");
            Assert.DoesNotContain(backingStore.actions, a => a.name == "BackingStoreTest");
        }

        [Fact]
        public void ActionsChangedEvent_ShouldFireOnMutation()
        {
            var repo = new SpecialActionRepository(new BackingStore());
            bool eventFired = false;
            repo.ActionsChanged += (s, e) => eventFired = true;

            var action = new SpecialAction("EventTestAction", "Touchpad", "Key", "10", 0);
            repo.AddAction(action);

            Assert.True(eventFired);
        }

        [Fact]
        public void GlobalShim_ShouldSynchronizeWithRepository()
        {
            var backingStore = new BackingStore();
            var repo = new SpecialActionRepository(backingStore);
            Global.SpecialActionRepositoryInstance = repo;

            var action = new SpecialAction("ShimActionTest", "Touchpad", "Key", "10", 0);
            repo.AddAction(action);

            Assert.True(Global.SpecialActionRepositoryInstance.ActionExists("ShimActionTest"));
            Assert.NotNull(Global.SpecialActionRepositoryInstance.GetAction("ShimActionTest"));
        }
    }
}
