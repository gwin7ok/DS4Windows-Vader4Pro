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
    }
}
