using System;
using System.Collections.Generic;
using DS4Windows.DI;

namespace DS4Windows
{
    public class ProfileActionProvider : IProfileActionProvider
    {
        public IReadOnlyList<string> GetProfileActionNames(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= ProfileSettingsService.TEST_PROFILE_ITEM_COUNT)
                return Array.Empty<string>();

            var actionNames = Global.getProfileActions(deviceIndex);
            var result = actionNames == null ? Array.Empty<string>() : actionNames.ToArray();
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ProfileActionProvider.GetProfileActionNames: Slot {deviceIndex}, Count {result.Length}");
            return result;
        }

        public SpecialAction GetProfileAction(int deviceIndex, string actionName)
        {
            if (deviceIndex < 0 || deviceIndex >= ProfileSettingsService.TEST_PROFILE_ITEM_COUNT ||
                string.IsNullOrEmpty(actionName))
                return null;

            var action = Global.GetProfileAction(deviceIndex, actionName);
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ProfileActionProvider.GetProfileAction: Slot {deviceIndex}, Action '{actionName}'");
            return action;
        }
    }
}
