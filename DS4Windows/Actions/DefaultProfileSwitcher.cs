using System;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    /// <summary>
    /// IProfileSwitcher の標準実装。
    /// プロファイル切り替えと、切り替え直後の連鎖発火（カスケードループ）防止ガードを提供します。
    /// プロファイル適用の実体は IProfileApplicationService に委譲し、Halt保護および一元管理を行います。
    /// </summary>
    public class DefaultProfileSwitcher : IProfileSwitcher
    {
        // 直近にプロファイル切替を実行したタイムスタンプ（デバウンス用）
        private readonly long[] _lastSwitchTicks = new long[4];
        private readonly string[] _previousProfiles = new string[4];
        private readonly bool[] _temporaryProfiles = new bool[4];
        private readonly IProfileApplicationService _profileAppService;

        public DefaultProfileSwitcher(IProfileApplicationService profileAppService = null)
        {
            _profileAppService = profileAppService;
        }

        private IProfileApplicationService ResolveAppService()
        {
            return _profileAppService ?? DS4WinWPF.AppHost.GetService<IProfileApplicationService>();
        }

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
                bool isTemporaryProfile = action.IsTemporaryProfileAction;
                _temporaryProfiles[deviceIndex] = isTemporaryProfile;

                // プロファイル適用: IProfileApplicationService へ一本化（Halt保護内包、Program.rootHub 直参照排除）
                var appService = ResolveAppService();
                if (appService != null)
                {
                    appService.ApplyProfile(deviceIndex, targetProfile, isTemporaryProfile, false,
                        ProfileChangeSource.MappingAction);
                }
                else
                {
                    // 極限フォールバック: DI未初期化時（§2.1 原則）
                    Global.ApplyProfile(deviceIndex, targetProfile, isTemporaryProfile, false,
                        Program.rootHub, ProfileChangeSource.MappingAction);
                }

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
                if (_temporaryProfiles[deviceIndex])
                {
                    _temporaryProfiles[deviceIndex] = false;
                }

                var appService = ResolveAppService();
                if (appService != null && appService.RestoreFromAction(deviceIndex))
                {
                    return;
                }

                string prevProfile = _previousProfiles[deviceIndex];
                if (!string.IsNullOrWhiteSpace(prevProfile))
                {
                    if (appService != null)
                    {
                        appService.ApplyProfile(deviceIndex, prevProfile, false, false,
                            ProfileChangeSource.MappingAction);
                    }
                    else
                    {
                        Global.ApplyProfile(deviceIndex, prevProfile, false, false,
                            Program.rootHub, ProfileChangeSource.MappingAction);
                    }

                    try { AppLogger.LogToGui($"Profile restored to '{prevProfile}' on controller {deviceIndex + 1}", false); } catch { }
                }
            }
            catch (Exception ex)
            {
                try { AppLogger.LogTrace($"DefaultProfileSwitcher.RestoreProfile failed: {ex}"); } catch { }
            }
        }

        public void ApplyManualProfile(int deviceIndex, string profileName, bool launchProgram,
            bool xinputChange, ControlService control, ProfileChangeSource source,
            string prolog, bool showNotification)
        {
            var appService = ResolveAppService();
            if (appService != null)
            {
                appService.ApplyProfile(deviceIndex, profileName, false, launchProgram,
                    source, prolog, showNotification);
            }
            else
            {
                Global.ApplyProfile(deviceIndex, profileName, launchProgram, xinputChange,
                    control, source, prolog, showNotification);
            }
        }

        /// <summary>
        /// 切断時等に指定スロットの内部状態をクリアします（§5.6 ガードレール）。
        /// </summary>
        public void ClearState(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= 4) return;

            _previousProfiles[deviceIndex] = null;
            _temporaryProfiles[deviceIndex] = false;
            _lastSwitchTicks[deviceIndex] = 0;

            var appService = ResolveAppService();
            appService?.ClearPendingRestore(deviceIndex);
        }
    }
}
