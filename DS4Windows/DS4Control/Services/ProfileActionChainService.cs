using System;
using DS4Windows.DI;

namespace DS4Windows
{
    public class ProfileActionChainService : IProfileActionChainService
    {
        private readonly IProfileActionProvider _actionProvider;

        public ProfileActionChainService(IProfileActionProvider actionProvider)
        {
            _actionProvider = actionProvider;
        }

        public void DispatchNextActions(int deviceIndex, SpecialAction sourceAction)
        {
            if (sourceAction == null || sourceAction.uTrigger.Count != 0 || sourceAction.automaticUntrigger)
                return;

            var actionNames = _actionProvider.GetProfileActionNames(deviceIndex);
            for (int index = 0; index < actionNames.Count; index++)
            {
                string actionName = actionNames[index];
                SpecialAction nextAction = _actionProvider.GetProfileAction(deviceIndex, actionName);
                if (nextAction != null && nextAction.controls == sourceAction.controls)
                    Mapping.DispatchProfileActionEdge(nextAction, deviceIndex, true);
            }

            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ProfileActionChainService.DispatchNextActions: Slot {deviceIndex}");
        }
    }
}
