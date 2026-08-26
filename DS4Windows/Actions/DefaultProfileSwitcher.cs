using System;
using DS4Windows;

namespace DS4Windows.Actions
{
    /// <summary>
    /// IProfileSwitcher の標準実装
    /// Mapping.ApplyProfileDirect / Mapping.RestoreProfileDirect へ安全に委譲し、
    /// プロファイル切り替え・排他制御・トースト通知を完全維持したまま DI 境界を提供します。
    /// </summary>
    public class DefaultProfileSwitcher : IProfileSwitcher
    {
        public void SwitchProfile(int deviceIndex, SpecialAction action)
        {
            if (deviceIndex < 0 || deviceIndex >= 4 || action == null) return;

            Mapping.ApplyProfileDirect(deviceIndex, action);
        }

        public void RestoreProfile(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= 4) return;

            Mapping.RestoreProfileDirect(deviceIndex);
        }
    }
}