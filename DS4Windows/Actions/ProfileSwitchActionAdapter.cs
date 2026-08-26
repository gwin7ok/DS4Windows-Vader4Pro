using System;
using DS4Windows;

namespace DS4Windows.Actions
{
    /// <summary>
    /// プロファイル切り替えアクションのアダプター（Action 派生）
    /// Mapping.cs のトリガー判定から ProfileSwitchAction へのディスパッチを中継
    /// </summary>
    public class ProfileSwitchActionAdapter : Action
    {
        private readonly SpecialAction sa;
        private readonly ProfileSwitchAction action;
        private readonly int deviceIndex;

        public ProfileSwitchActionAdapter(SpecialAction sa, int deviceIndex = 0)
        {
            this.sa = sa;
            this.deviceIndex = deviceIndex;
            this.action = new ProfileSwitchAction(sa, deviceIndex);
        }

        public string ActionType => "Profile";
        public SpecialAction SpecialAction => sa;
        public ProfileSwitchAction OutputAction => action;

        public override void OnTrigger(int device, MappingContext context)
        {
            if (sa == null) return;
            action.Execute(null);
        }

        public override void OnRelease(int device, MappingContext context)
        {
            action.Stop(null);
        }
    }
}