using System;
using System.Threading;
using DS4Windows;

namespace DS4Windows.Actions
{
    /// <summary>
    /// IMacroPlayer の標準実装
    /// Mapping.PlayMacroDirect / Mapping.EndMacroDirect へ安全に委譲し、
    /// 機能・エッジケース処理を 100% 維持したまま DI 境界を提供します。
    /// </summary>
    public class DefaultMacroPlayer : IMacroPlayer
    {
        public bool IsPlaying(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= 4) return false;
            // デバイスごとの再生状態を取得
            return Mapping.macroPlaying != null && Mapping.macroPlaying[deviceIndex];
        }

        public void Play(int deviceIndex, SpecialAction action, CancellationToken cancellationToken = default)
        {
            if (deviceIndex < 0 || deviceIndex >= 4 || action == null) return;

            Mapping.PlayMacroDirect(deviceIndex, action);
        }

        public void Stop(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= 4) return;

            Mapping.EndMacroDirect(deviceIndex);
        }
    }
}