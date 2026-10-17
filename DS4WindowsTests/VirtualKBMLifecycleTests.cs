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
    /// Phase7-Step8 W3（Q6＝A）: ハンドラの実体は <see cref="OutputKBMHandlerAdapter.Handler"/>（旧 Global.outputKBMHandler）、
    /// KBM マッピングは <c>IProfileSettingsService.OutputKBMMapping</c>（旧 Global.outputKBMMapping）へ移った。
    /// 各テストは、独立の adapter・設定サービスを使うため、静的な状態を共有せず、元へ戻す必要もない。
    /// </summary>
    public class VirtualKBMLifecycleTests
    {
        private readonly OutputKBMHandlerAdapter _adapter = new OutputKBMHandlerAdapter();
        private readonly ProfileSettingsService _settings = TestRepositoryFactory.CreateProfileSettingsService(new BackingStore());
        private readonly OutputKBMHandlerLifecycle _lifecycle;

        public VirtualKBMLifecycleTests()
        {
            _lifecycle = new OutputKBMHandlerLifecycle(new EnvironmentService(), _adapter, _settings);
        }

        [Fact]
        public void Constructor_NullDependencies_ThrowArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new OutputKBMHandlerLifecycle(null, _adapter, _settings));
            Assert.Equal("environmentService", ex.ParamName);

            ex = Assert.Throws<ArgumentNullException>(() => new OutputKBMHandlerLifecycle(new EnvironmentService(), null, _settings));
            Assert.Equal("handlerOwner", ex.ParamName);

            ex = Assert.Throws<ArgumentNullException>(() => new OutputKBMHandlerLifecycle(new EnvironmentService(), _adapter, null));
            Assert.Equal("profileSettings", ex.ParamName);
        }

        [Fact]
        public void ReleaseHandler_WithExistingHandler_ClearsHandler()
        {
            _adapter.Handler = VirtualKBMFactory.GetFallbackHandler();

            _lifecycle.ReleaseHandler();

            Assert.Null(_adapter.Handler);
        }

        [Fact]
        public void ReleaseHandler_WithoutHandler_DoesNotThrow()
        {
            _adapter.Handler = null;

            var ex = Record.Exception(() => _lifecycle.ReleaseHandler());

            Assert.Null(ex);
            Assert.Null(_adapter.Handler);
        }

        [Fact]
        public void DetermineHandler_WithFallbackIdentifier_SetsFallbackHandler()
        {
            _adapter.Handler = null;

            _lifecycle.DetermineHandler(VirtualKBMFactory.GetFallbackHandlerIdentifier());

            Assert.NotNull(_adapter.Handler);
            Assert.Equal(VirtualKBMFactory.GetFallbackHandlerIdentifier(), _adapter.Handler.GetIdentifier());
        }

        [Fact]
        public void SwitchToFallbackHandler_ReplacesHandlerWithFallback()
        {
            _adapter.Handler = null;

            _lifecycle.SwitchToFallbackHandler();

            Assert.NotNull(_adapter.Handler);
            Assert.Equal(VirtualKBMFactory.GetFallbackHandlerIdentifier(), _adapter.Handler.GetIdentifier());
        }

        [Fact]
        public void ApplyFakerInputVersion_SetsHandlerVersionFromEnvironmentService()
        {
            string originalVersion = EnvironmentService.FakerInputVersionState;
            try
            {
                EnvironmentService.FakerInputVersionState = "1.2.3.4";
                _adapter.Handler = VirtualKBMFactory.GetFallbackHandler();

                _lifecycle.ApplyFakerInputVersion();

                Assert.Equal("1.2.3.4", _adapter.Handler.Version);
            }
            finally
            {
                EnvironmentService.FakerInputVersionState = originalVersion;
            }
        }

        [Fact]
        public void ApplyFakerInputVersion_WithoutHandler_DoesNotThrow()
        {
            _adapter.Handler = null;

            var ex = Record.Exception(() => _lifecycle.ApplyFakerInputVersion());

            Assert.Null(ex);
        }

        [Fact]
        public void InitializeMapping_WithFallbackIdentifier_SetsTheMappingOnTheProfileSettings()
        {
            _settings.OutputKBMMapping = null;

            _lifecycle.InitializeMapping(VirtualKBMFactory.GetFallbackHandlerIdentifier());

            Assert.NotNull(_settings.OutputKBMMapping);
        }

        [Fact]
        public void Adapter_DelegatesToTheOwnedHandler_AndIsInertWithoutOne()
        {
            var adapter = new OutputKBMHandlerAdapter();
            Assert.Equal(string.Empty, adapter.GetIdentifier());
            Assert.False(adapter.Connect());

            adapter.Handler = VirtualKBMFactory.GetFallbackHandler();
            Assert.Equal(VirtualKBMFactory.GetFallbackHandlerIdentifier(), adapter.GetIdentifier());

            adapter.Handler = null;
            Assert.Equal(string.Empty, adapter.GetIdentifier());
        }

        [Fact]
        public void EnvironmentService_FakerInputVersion_ReflectsStaticState()
        {
            string original = EnvironmentService.FakerInputVersionState;
            try
            {
                EnvironmentService.FakerInputVersionState = "9.8.7.6";

                Assert.Equal("9.8.7.6", new EnvironmentService().FakerInputVersion);
            }
            finally
            {
                EnvironmentService.FakerInputVersionState = original;
            }
        }
    }
}
