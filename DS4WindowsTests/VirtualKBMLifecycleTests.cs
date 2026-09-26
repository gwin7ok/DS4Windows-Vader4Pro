using System;
using Xunit;
using DS4Windows;
using DS4Windows.DS4Control;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step2-3 (PR-3, 決定D3): OutputKBMHandlerLifecycle が、旧 ControlService の
    /// RefreshOutputKBMHandler / InitOutputKBMHandler が行っていたハンドラ操作と等価であることを検証する。
    /// Global.outputKBMHandler / Global.outputKBMMapping は static のため、各テストは変更した状態を必ず元に戻す。
    /// </summary>
    public class VirtualKBMLifecycleTests
    {
        private readonly OutputKBMHandlerLifecycle _lifecycle = new OutputKBMHandlerLifecycle(new EnvironmentService());

        [Fact]
        public void Constructor_NullEnvironmentService_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new OutputKBMHandlerLifecycle(null));

            Assert.Equal("environmentService", ex.ParamName);
        }

        [Fact]
        public void ReleaseHandler_WithExistingHandler_ClearsHandler()
        {
            var original = Global.outputKBMHandler;
            try
            {
                Global.outputKBMHandler = VirtualKBMFactory.GetFallbackHandler();

                _lifecycle.ReleaseHandler();

                Assert.Null(Global.outputKBMHandler);
            }
            finally
            {
                Global.outputKBMHandler = original;
            }
        }

        [Fact]
        public void ReleaseHandler_WithoutHandler_DoesNotThrow()
        {
            var original = Global.outputKBMHandler;
            try
            {
                Global.outputKBMHandler = null;

                var ex = Record.Exception(() => _lifecycle.ReleaseHandler());

                Assert.Null(ex);
                Assert.Null(Global.outputKBMHandler);
            }
            finally
            {
                Global.outputKBMHandler = original;
            }
        }

        [Fact]
        public void DetermineHandler_WithFallbackIdentifier_SetsFallbackHandler()
        {
            var original = Global.outputKBMHandler;
            try
            {
                Global.outputKBMHandler = null;

                _lifecycle.DetermineHandler(VirtualKBMFactory.GetFallbackHandlerIdentifier());

                Assert.NotNull(Global.outputKBMHandler);
                Assert.Equal(VirtualKBMFactory.GetFallbackHandlerIdentifier(), Global.outputKBMHandler.GetIdentifier());
            }
            finally
            {
                Global.outputKBMHandler = original;
            }
        }

        [Fact]
        public void SwitchToFallbackHandler_ReplacesHandlerWithFallback()
        {
            var original = Global.outputKBMHandler;
            try
            {
                Global.outputKBMHandler = null;

                _lifecycle.SwitchToFallbackHandler();

                Assert.NotNull(Global.outputKBMHandler);
                Assert.Equal(VirtualKBMFactory.GetFallbackHandlerIdentifier(), Global.outputKBMHandler.GetIdentifier());
            }
            finally
            {
                Global.outputKBMHandler = original;
            }
        }

        [Fact]
        public void ApplyFakerInputVersion_SetsHandlerVersionFromEnvironmentService()
        {
            var originalHandler = Global.outputKBMHandler;
            string originalVersion = Global.fakerInputVersion;
            try
            {
                Global.fakerInputVersion = "1.2.3.4";
                Global.outputKBMHandler = VirtualKBMFactory.GetFallbackHandler();

                _lifecycle.ApplyFakerInputVersion();

                Assert.Equal("1.2.3.4", Global.outputKBMHandler.Version);
            }
            finally
            {
                Global.fakerInputVersion = originalVersion;
                Global.outputKBMHandler = originalHandler;
            }
        }

        [Fact]
        public void ApplyFakerInputVersion_WithoutHandler_DoesNotThrow()
        {
            var original = Global.outputKBMHandler;
            try
            {
                Global.outputKBMHandler = null;

                var ex = Record.Exception(() => _lifecycle.ApplyFakerInputVersion());

                Assert.Null(ex);
            }
            finally
            {
                Global.outputKBMHandler = original;
            }
        }

        [Fact]
        public void InitializeMapping_WithFallbackIdentifier_SetsMappingLikeGlobal()
        {
            var original = Global.outputKBMMapping;
            try
            {
                Global.outputKBMMapping = null;

                _lifecycle.InitializeMapping(VirtualKBMFactory.GetFallbackHandlerIdentifier());

                Assert.NotNull(Global.outputKBMMapping);
            }
            finally
            {
                Global.outputKBMMapping = original;
            }
        }

        [Fact]
        public void EnvironmentService_FakerInputVersion_ReflectsGlobalValue()
        {
            string original = Global.fakerInputVersion;
            try
            {
                Global.fakerInputVersion = "9.8.7.6";

                Assert.Equal("9.8.7.6", new EnvironmentService().FakerInputVersion);
            }
            finally
            {
                Global.fakerInputVersion = original;
            }
        }
    }
}