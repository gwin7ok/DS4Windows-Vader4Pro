using DS4Windows.DI;
using DS4Windows.DS4Control;

namespace DS4Windows.Services
{
    /// <summary>
    /// <see cref="IVirtualKBMLifecycle"/> の既定実装。現行の <c>Global</c> 側処理へ薄く委譲し、
    /// 挙動・ログ（<c>Global.InitOutputKBMHandler</c> 内の <c>AppLogger.LogDebug</c>）を従来と完全に同一に保つ。
    /// </summary>
    public class OutputKBMHandlerLifecycle : IVirtualKBMLifecycle
    {
        private readonly IEnvironmentService _environmentService;

        public OutputKBMHandlerLifecycle(IEnvironmentService environmentService)
        {
            _environmentService = environmentService ?? throw new System.ArgumentNullException(nameof(environmentService));
        }

        // TODO(技術的負債): ハンドラの実体が Global の static フィールド（Global.outputKBMHandler）に残っているため、
        // 本クラスから直接読み書きしている。実体をサービス内へ移すのは Global 撤去フェーズ（Phase7）で行う。
        public void ReleaseHandler()
        {
            if (Global.outputKBMHandler != null)
            {
                Global.outputKBMHandler.Disconnect();
                Global.outputKBMHandler = null;
            }
        }

        public void DetermineHandler(string identifier) => Global.InitOutputKBMHandler(identifier);

        public void SwitchToFallbackHandler() => Global.outputKBMHandler = VirtualKBMFactory.GetFallbackHandler();

        public void ApplyFakerInputVersion()
        {
            if (Global.outputKBMHandler != null)
            {
                Global.outputKBMHandler.Version = _environmentService.FakerInputVersion;
            }
        }

        public void InitializeMapping(string identifier) => Global.InitOutputKBMMapping(identifier);
    }
}