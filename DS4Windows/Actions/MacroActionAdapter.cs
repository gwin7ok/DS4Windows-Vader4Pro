using System;
using DS4Windows;

namespace DS4Windows.Actions
{
    /// <summary>
    /// マクロアクションのアダプター
    /// Mapping.cs のトリガー判定から MacroAction へのディスパッチを中継
    /// </summary>
    public class MacroActionAdapter
    {
        private readonly SpecialAction sa;
        private readonly MacroAction action;

        public MacroActionAdapter(SpecialAction sa)
        {
            this.sa = sa;
            this.action = new MacroAction(sa);
        }

        public string ActionType => "Macro";
        public SpecialAction SpecialAction => sa;
        public MacroAction OutputAction => action;

        public bool OnTrigger(int device, MappingContext context)
        {
            if (sa == null) return false;
            action.Execute(new OutputContext(device, context));
            return true;
        }

        public void OnRelease(int device, MappingContext context)
        {
            action.Stop(new OutputContext(device, context));
        }
    }
}