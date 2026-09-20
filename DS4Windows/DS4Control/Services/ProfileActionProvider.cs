using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows.DI;

namespace DS4Windows
{
    public class ProfileActionProvider : IProfileActionProvider
    {
        private readonly BackingStore _config;

        public ProfileActionProvider(BackingStore config = null)
        {
            _config = config ?? Global.store;
        }

        public IReadOnlyList<string> GetProfileActionNames(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= ProfileSettingsService.TEST_PROFILE_ITEM_COUNT || _config == null)
                return Array.Empty<string>();

            var actionNames = _config.profileActions[deviceIndex];
            var result = actionNames == null ? Array.Empty<string>() : actionNames.ToArray();
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ProfileActionProvider.GetProfileActionNames: Slot {deviceIndex}, Count {result.Length}");
            return result;
        }

        public SpecialAction GetProfileAction(int deviceIndex, string actionName)
        {
            if (deviceIndex < 0 || deviceIndex >= ProfileSettingsService.TEST_PROFILE_ITEM_COUNT ||
                string.IsNullOrEmpty(actionName) || _config == null)
                return null;

            SpecialAction action = null;
            if (_config.profileActionDict[deviceIndex].TryGetValue(actionName, out var act))
            {
                action = act;
            }

            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ProfileActionProvider.GetProfileAction: Slot {deviceIndex}, Action '{actionName}'");
            return action;
        }

        // ---- Phase6-Step2-5 (PR-5): スペシャルアクション数 ----
        // 毎レポート評価される経路から呼ばれるため、BackingStore の値を直接返す（従来の Global.getProfileActionCount と同一）。
        public int GetProfileActionCount(int deviceIndex) => _config.profileActionCount[deviceIndex];
    }
}