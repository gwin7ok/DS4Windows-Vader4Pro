using System;
using DS4Windows.DI;

namespace DS4Windows
{
    public class ProfileActionChainService : IProfileActionChainService
    {
        private readonly IProfileActionProvider _actionProvider;
        private readonly IMappingActionDispatcher _actionDispatcher;

        public ProfileActionChainService(IProfileActionProvider actionProvider,
            IMappingActionDispatcher actionDispatcher = null)
        {
            _actionProvider = actionProvider;
            _actionDispatcher = actionDispatcher ?? DS4WinWPF.AppHost.GetService<IMappingActionDispatcher>() ?? new MappingActionDispatcher();
        }

        public void DispatchNextActions(int deviceIndex, SpecialAction sourceAction)
        {
            if (deviceIndex < 0 || deviceIndex >= 4 || sourceAction == null ||
                sourceAction.uTrigger.Count != 0 || sourceAction.automaticUntrigger)
                return;

            if (_actionProvider == null)
                return;

            var actionNames = _actionProvider.GetProfileActionNames(deviceIndex);
            if (actionNames == null)
                return;

            for (int index = 0; index < actionNames.Count; index++)
            {
                string actionName = actionNames[index];
                SpecialAction nextAction = _actionProvider.GetProfileAction(deviceIndex, actionName);
                if (nextAction != null && nextAction.controls == sourceAction.controls)
                {
                    _actionDispatcher.DispatchProfileActionEdge(nextAction, deviceIndex, true);
                }
            }

            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ProfileActionChainService.DispatchNextActions: Slot {deviceIndex}, Source='{sourceAction.name}'");
        }
    }
}
