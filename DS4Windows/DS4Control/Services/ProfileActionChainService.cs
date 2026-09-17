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

            // プロファイル切替アクション自体は別プロファイルへ移行した時点で単体完結するため、
            // 新プロファイル側の同一トリガーを持つプロファイル切替アクションを連鎖実行（カスケードループ）させない
            if (string.Equals(sourceAction.type, "Profile", StringComparison.OrdinalIgnoreCase))
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
                    // 次のアクションがプロファイル切替の場合は連鎖をスキップ
                    if (string.Equals(nextAction.type, "Profile", StringComparison.OrdinalIgnoreCase))
                        continue;

                    _actionDispatcher.DispatchProfileActionEdge(nextAction, deviceIndex, true);
                }
            }

            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ProfileActionChainService.DispatchNextActions: Slot {deviceIndex}, Source='{sourceAction.name}'");
        }
    }
}