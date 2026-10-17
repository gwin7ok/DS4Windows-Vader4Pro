using System;
using System.Threading;
using DS4Windows.Actions;
using Xunit;

namespace DS4Windows.Actions.Tests
{

    public class ActionControllerTests
    {
        [Fact]
        public void Start_Should_Invoke_RepeaterStart_And_Stop_Should_Invoke_RepeaterStop()
        {
            var mock = new MockRepeater();
            var controller = new TestActionController(1, mock);

            // Create minimal fake binding and trigger (nulls acceptable for this test)
            IActionBinding binding = null;
            ITriggerContext trigger = null;

            controller.Start(binding, trigger);
            Assert.True(mock.Started);
            Assert.False(mock.Stopped);

            controller.Stop(binding, trigger);
            Assert.True(mock.Stopped);
            Assert.False(mock.Started);
        }
    }
}
