using DS4Windows.Services;
using System;
using DS4Windows;

namespace DS4Windows.Actions
{
    public class DefaultActionFactory : IActionFactory
    {
        // Phase6-Step10-4-4: アクションが必要とする部品（マクロ再生役・プログラム起動役・プロファイル切替役）を、
        // 本工場が生成時に受け取り、各アクションへ渡す（アクション側の AppHost 参照＝Service Locator を廃止）。
        private readonly IMacroPlayer _macroPlayer;
        private readonly IProcessLauncher _processLauncher;
        private readonly IProfileSwitcher _profileSwitcher;

        public DefaultActionFactory(IMacroPlayer macroPlayer, IProcessLauncher processLauncher,
            IProfileSwitcher profileSwitcher)
        {
            _macroPlayer = macroPlayer ?? throw new ArgumentNullException(nameof(macroPlayer));
            _processLauncher = processLauncher ?? throw new ArgumentNullException(nameof(processLauncher));
            _profileSwitcher = profileSwitcher ?? throw new ArgumentNullException(nameof(profileSwitcher));
        }

        public Action CreateFrom(SpecialAction sa, int index)
        {
            if (sa == null) return null;

            switch (sa.typeID)
            {
                case SpecialAction.ActionTypeId.Key:
                    return new KeyActionAdapter(sa, index);
                case SpecialAction.ActionTypeId.Program:
                    return new LaunchProcessActionAdapter(sa, index, _processLauncher);
                case SpecialAction.ActionTypeId.Macro:
                    return new MacroActionAdapter(sa, index, _macroPlayer);
                case SpecialAction.ActionTypeId.Profile:
                    return new ProfileSwitchActionAdapter(sa, index, _profileSwitcher);
                // TODO: add Disconnect etc.
                default:
                    return null;
            }
        }
    }
}
