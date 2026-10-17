using System;
using DS4Windows.DI;
using DS4Windows.DS4Control;

namespace DS4Windows.Services
{
    /// <summary>
    /// <see cref="IVirtualKBMLifecycle"/> の既定実装。ハンドラの実体を持つ <see cref="OutputKBMHandlerAdapter"/> へ書き込み、
    /// KBM マッピングは <see cref="IProfileSettingsService.OutputKBMMapping"/> へ設定する。
    /// Phase7-Step8 W3（Q6＝A）: 旧 <c>Global.outputKBMHandler</c>／<c>Global.outputKBMMapping</c>／<c>Global.InitOutputKBMHandler</c>／
    /// <c>Global.InitOutputKBMMapping</c> を削除し、ここへ移した。挙動・ログ（<c>AppLogger.LogDebug</c>）は従来と同一。
    /// </summary>
    public class OutputKBMHandlerLifecycle : IVirtualKBMLifecycle
    {
        private readonly IEnvironmentService _environmentService;
        private readonly OutputKBMHandlerAdapter _handlerOwner;
        private readonly IProfileSettingsService _profileSettings;

        public OutputKBMHandlerLifecycle(IEnvironmentService environmentService, OutputKBMHandlerAdapter handlerOwner,
            IProfileSettingsService profileSettings)
        {
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _handlerOwner = handlerOwner ?? throw new ArgumentNullException(nameof(handlerOwner));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
        }

        public void ReleaseHandler()
        {
            if (_handlerOwner.Handler != null)
            {
                _handlerOwner.Handler.Disconnect();
                _handlerOwner.Handler = null;
            }
        }

        public void DetermineHandler(string identifier)
        {
            VirtualKBMBase handler = VirtualKBMFactory.DetermineHandler(identifier);
            _handlerOwner.Handler = handler;
            // For testing: disable fake key repeat to isolate whether repeated synthetic sends
            // are due to fakeKeyRepeat logic. Re-enable or remove this for normal builds.
            if (handler != null)
            {
                try
                {
                    AppLogger.LogDebug($"Output KBM handler fakeKeyRepeat={handler.fakeKeyRepeat} for {handler.GetIdentifier()}");
                }
                catch (Exception)
                {
                    // ignore - defensive in case handler implementation restricts setting
                }
            }
        }

        public void SwitchToFallbackHandler() => _handlerOwner.Handler = VirtualKBMFactory.GetFallbackHandler();

        public void ApplyFakerInputVersion()
        {
            if (_handlerOwner.Handler != null)
            {
                _handlerOwner.Handler.Version = _environmentService.FakerInputVersion;
            }
        }

        public void InitializeMapping(string identifier) =>
            _profileSettings.OutputKBMMapping = VirtualKBMFactory.GetMappingInstance(identifier);
    }
}
