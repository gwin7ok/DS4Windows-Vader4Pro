using System;
using System.IO;
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
            var repo = new SpecialActionRepository();
            var path = repo.ActionsPath;
            Assert.False(string.IsNullOrWhiteSpace(path));
            Assert.EndsWith("Actions.xml", path, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AddAndGetAction_ShouldWorkCorrectly()
        {
            var repo = new SpecialActionRepository();
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
            var repo = new SpecialActionRepository();
            var action = new SpecialAction("RemoveTestAction", "Touchpad", "Key", "10", 0);

            repo.AddAction(action);
            Assert.True(repo.ActionExists("RemoveTestAction"));

            var removed = repo.RemoveAction("RemoveTestAction");
            Assert.True(removed);
            Assert.False(repo.ActionExists("RemoveTestAction"));
        }

        [Fact]
        public void ActionsChangedEvent_ShouldFireOnMutation()
        {
            var repo = new SpecialActionRepository();
            bool eventFired = false;
            repo.ActionsChanged += (s, e) => eventFired = true;

            var action = new SpecialAction("EventTestAction", "Touchpad", "Key", "10", 0);
            repo.AddAction(action);

            Assert.True(eventFired);
        }

        [Fact]
        public void GlobalShim_ShouldSynchronizeWithRepository()
        {
            var repo = new SpecialActionRepository();
            Global.SpecialActionRepositoryInstance = repo;

            var action = new SpecialAction("ShimActionTest", "Touchpad", "Key", "10", 0);
            repo.AddAction(action);

            Assert.True(Global.SpecialActionRepositoryInstance.ActionExists("ShimActionTest"));
            Assert.NotNull(Global.SpecialActionRepositoryInstance.GetAction("ShimActionTest"));
        }
    }
}
