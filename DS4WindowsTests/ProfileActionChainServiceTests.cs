using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    public class ProfileActionChainServiceTests
    {
        private class MockMappingActionDispatcher : IMappingActionDispatcher
        {
            public class DispatchCall
            {
                public SpecialAction Action { get; set; }
                public int DeviceIndex { get; set; }
                public bool Start { get; set; }
            }

            public List<DispatchCall> Calls { get; } = new List<DispatchCall>();

            public void DispatchProfileActionEdge(SpecialAction action, int deviceIndex, bool start)
            {
                Calls.Add(new DispatchCall
                {
                    Action = action,
                    DeviceIndex = deviceIndex,
                    Start = start
                });
            }
        }

        private class MockProfileActionProvider : IProfileActionProvider
        {
            private readonly Dictionary<string, SpecialAction> _actions = new Dictionary<string, SpecialAction>();

            public void AddAction(string name, SpecialAction action)
            {
                _actions[name] = action;
            }

            public IReadOnlyList<string> GetProfileActionNames(int deviceIndex)
            {
                return _actions.Keys.ToList();
            }

            public SpecialAction GetProfileAction(int deviceIndex, string actionName)
            {
                _actions.TryGetValue(actionName, out var action);
                return action;
            }
        }

        [Fact]
        public void DispatchNextActions_MatchingControls_DispatchesAction()
        {
            var mockProvider = new MockProfileActionProvider();
            var mockDispatcher = new MockMappingActionDispatcher();
            var service = new ProfileActionChainService(mockProvider, mockDispatcher);

            var sourceAction = new SpecialAction("SourceAction", "Cross", "Profile", "Profile", 0);
            var nextAction = new SpecialAction("NextAction", "Cross", "Key", "Key", 0);
            mockProvider.AddAction("NextAction", nextAction);

            service.DispatchNextActions(0, sourceAction);

            Assert.Single(mockDispatcher.Calls);
            Assert.Same(nextAction, mockDispatcher.Calls[0].Action);
            Assert.Equal(0, mockDispatcher.Calls[0].DeviceIndex);
            Assert.True(mockDispatcher.Calls[0].Start);
        }

        [Fact]
        public void DispatchNextActions_NonMatchingControls_DoesNotDispatch()
        {
            var mockProvider = new MockProfileActionProvider();
            var mockDispatcher = new MockMappingActionDispatcher();
            var service = new ProfileActionChainService(mockProvider, mockDispatcher);

            var sourceAction = new SpecialAction("SourceAction", "Cross", "Profile", "Profile", 0);
            var nextAction = new SpecialAction("NextAction", "Circle", "Key", "Key", 0);
            mockProvider.AddAction("NextAction", nextAction);

            service.DispatchNextActions(0, sourceAction);

            Assert.Empty(mockDispatcher.Calls);
        }

        [Fact]
        public void DispatchNextActions_SourceHasUTrigger_DoesNotDispatch()
        {
            var mockProvider = new MockProfileActionProvider();
            var mockDispatcher = new MockMappingActionDispatcher();
            var service = new ProfileActionChainService(mockProvider, mockDispatcher);

            var sourceAction = new SpecialAction("SourceAction", "Cross", "Profile", "Profile", 0);
            sourceAction.uTrigger.Add("Square");
            var nextAction = new SpecialAction("NextAction", "Cross", "Key", "Key", 0);
            mockProvider.AddAction("NextAction", nextAction);

            service.DispatchNextActions(0, sourceAction);

            Assert.Empty(mockDispatcher.Calls);
        }

        [Fact]
        public void DispatchNextActions_SourceAutomaticUntrigger_DoesNotDispatch()
        {
            var mockProvider = new MockProfileActionProvider();
            var mockDispatcher = new MockMappingActionDispatcher();
            var service = new ProfileActionChainService(mockProvider, mockDispatcher);

            var sourceAction = new SpecialAction("SourceAction", "Cross", "Profile", "Profile", 0);
            sourceAction.automaticUntrigger = true;
            var nextAction = new SpecialAction("NextAction", "Cross", "Key", "Key", 0);
            mockProvider.AddAction("NextAction", nextAction);

            service.DispatchNextActions(0, sourceAction);

            Assert.Empty(mockDispatcher.Calls);
        }

        [Fact]
        public void DispatchNextActions_NullOrOutOfBounds_HandledSafely()
        {
            var mockProvider = new MockProfileActionProvider();
            var mockDispatcher = new MockMappingActionDispatcher();
            var service = new ProfileActionChainService(mockProvider, mockDispatcher);

            var validAction = new SpecialAction("SourceAction", "Cross", "Profile", "Profile", 0);

            // Null sourceAction
            service.DispatchNextActions(0, null);
            Assert.Empty(mockDispatcher.Calls);

            // Out of bounds slots
            service.DispatchNextActions(-1, validAction);
            service.DispatchNextActions(4, validAction);
            Assert.Empty(mockDispatcher.Calls);
        }

        [Fact]
        public void ProfileActionProvider_DirectBackingStore_ReturnsCorrectActions()
        {
            var backingStore = new BackingStore();
            var provider = new ProfileActionProvider(backingStore);

            var action = new SpecialAction("ProviderTest", "Triangle", "Key", "Key", 0);
            backingStore.profileActions[0].Add("ProviderTest");
            backingStore.profileActionDict[0]["ProviderTest"] = action;

            var names = provider.GetProfileActionNames(0);
            Assert.Contains("ProviderTest", names);

            var retrieved = provider.GetProfileAction(0, "ProviderTest");
            Assert.NotNull(retrieved);
            Assert.Same(action, retrieved);
        }
    }
}
