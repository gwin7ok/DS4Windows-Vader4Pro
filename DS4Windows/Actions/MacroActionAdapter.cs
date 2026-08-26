using System;
using DS4Windows;

namespace DS4Windows.Actions
{
    /// <summary>
    /// マクロアクションのアダプター（IActionAdapter 実装）
    /// Mapping.cs のトリガー判定から MacroAction へのディスパッチを中継
    /// </summary>
    public class MacroActionAdapter : IActionAdapter
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
        public IOutputAction OutputAction => action;

        public bool OnTrigger(int device, MappingContext context)
        {
            if (sa == null) return false;
            action.Execute(new ActionContext(device, context));
            return true;
        }
    }
}