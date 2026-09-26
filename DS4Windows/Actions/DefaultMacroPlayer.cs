using System;
using System.Threading;
using DS4Windows;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    /// <summary>
    /// IMacroPlayer の標準実装
    /// IVirtualKBM をコンストラクタ注入で受領し、
    /// Mapping.PlayMacroDirect / Mapping.EndMacroDirect へ安全に委譲し、
    /// 機能・エッジケース処理を 100% 維持したまま DI 境界を提供します。
    /// </summary>
    public class DefaultMacroPlayer : IMacroPlayer
    {
        private readonly bool[] _isPlaying = new bool[4];
        private readonly object _lock = new object();
        private readonly IVirtualKBM _virtualKBM;

        public DefaultMacroPlayer(IVirtualKBM virtualKBM = null)
        {
            _virtualKBM = virtualKBM ?? DS4WinWPF.AppHost.GetService<IVirtualKBM>();
        }

        public bool IsPlaying(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= 4) return false;
            lock (_lock)
            {
                return _isPlaying[deviceIndex];
            }
        }

        public void Play(int deviceIndex, SpecialAction action, CancellationToken cancellationToken = default)
        {
            if (deviceIndex < 0 || deviceIndex >= 4 || action == null) return;

            lock (_lock)
            {
                _isPlaying[deviceIndex] = true;
            }

            Mapping.PlayMacroDirect(deviceIndex, action);
        }

        public void Stop(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= 4) return;

            lock (_lock)
            {
                _isPlaying[deviceIndex] = false;
            }

            Mapping.EndMacroDirect(deviceIndex);
        }
    }
}
