using System;
using DS4Windows;

namespace DS4Windows.Actions
{
    /// <summary>
    /// マクロアクションのアダプター
    /// Mapping.cs のトリガー判定から MacroAction へのディスパッチを中継
    /// </summary>
    public class MacroActionAdapter : Action
    {
        private readonly SpecialAction sa;
        private readonly MacroAction action;
        private readonly int deviceIndex;

        public MacroActionAdapter(SpecialAction sa, int deviceIndex = 0)
        {
            this.sa = sa;
            this.deviceIndex = deviceIndex;
            this.action = new MacroAction(sa, deviceIndex);
        }

        public string ActionType => "Macro";
        public SpecialAction SpecialAction => sa;
        public MacroAction OutputAction => action;

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