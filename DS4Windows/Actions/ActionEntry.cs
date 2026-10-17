using DS4Windows.Services;
using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using DS4Windows.DS4Control;
using DS4Windows.Actions;

namespace DS4Windows
{

    internal class ActionEntry
    {
        public SpecialAction ActionDef;
        public Actions.Action ActionImpl;
        public ActionInstanceState[] States;

        public ActionEntry(SpecialAction action, IActionFactory factory = null)
        {
            ActionDef = action;
            ActionImpl = factory != null ? factory.CreateFrom(action, -1) : ActionFactory.CreateFrom(action, -1);
            States = new ActionInstanceState[ControllerLimits.MaxControllerCount];
            for (int i = 0; i < States.Length; ++i) States[i] = new ActionInstanceState();
            try
            {
                AppLogger.LogTrace($"ActionEntry created: action={(action?.name ?? "(null)")} - initialized ActionInstanceState (IsToggledOn cleared)");
            }
            catch { }
        }
    }
}