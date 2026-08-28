using System;
using DS4Windows;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    /// <summary>
    /// IProfileSwitcher の標準実装。
    /// プロファイル切り替えと、切り替え直後の連鎖発火（カスケードループ）防止ガードを提供します。
    /// </summary>
    public class DefaultProfileSwitcher : IProfileSwitcher
    {
        // 直近にプロファイル切替を実行したタイムスタンプ（デバウンス用）
        private readonly long[] _lastSwitchTicks = new long[4];
        private readonly string[] _previousProfiles = new string[4];

        public void SwitchProfile(int deviceIndex, SpecialAction action)
        {
            if (deviceIndex < 0 || deviceIndex >= 4 || action == null) return;

            long now = DateTime.UtcNow.Ticks;
            // 短時間（250ms以内）の連続切り替えを防止（同一トリガー押し込み中のカスケードループ遮断）
            if (now - _lastSwitchTicks[deviceIndex] < TimeSpan.FromMilliseconds(250).Ticks)
            {
                return;
            }

            _lastSwitchTicks[deviceIndex] = now;

            try
            {
                string targetProfile = action.details;
                if (string.IsNullOrWhiteSpace(targetProfile)) return;

                // 現在のプロファイルをバックアップ
                _previousProfiles[deviceIndex] = Global.ProfilePath[deviceIndex];

                // プロファイル適用: Global.ApplyProfile 経由で ProfilePath 更新 + LoadProfile を一括実行
                // (source=MappingAction は SpecialAction 経由の切替であることを示す)
                Global.ApplyProfile(deviceIndex, targetProfile, false, false,
                    Program.rootHub, ProfileChangeSource.MappingAction);

                try { AppLogger.LogToGui($"Profile switched to '{targetProfile}' on controller {deviceIndex + 1}", false); } catch { }
            }
            catch (Exception ex)
            {
                try { AppLogger.LogTrace($"DefaultProfileSwitcher.SwitchProfile failed: {ex}"); } catch { }
            }
        }

        public void RestoreProfile(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= 4) return;

            try
            {
                string prevProfile = _previousProfiles[deviceIndex];
                if (!string.IsNullOrWhiteSpace(prevProfile))
                {
                    Global.ApplyProfile(deviceIndex, prevProfile, false, false,
                        Program.rootHub, ProfileChangeSource.MappingAction);
                    try { AppLogger.LogToGui($"Profile restored to '{prevProfile}' on controller {deviceIndex + 1}", false); } catch { }
                }
            }
            catch (Exception ex)
            {
                try { AppLogger.LogTrace($"DefaultProfileSwitcher.RestoreProfile failed: {ex}"); } catch { }
            }
        }
    }
}
