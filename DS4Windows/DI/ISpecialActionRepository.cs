using System;
using System.Collections.Generic;

namespace DS4Windows.DI
{
    public interface ISpecialActionRepository
    {
        string ActionsPath { get; }
        IReadOnlyList<SpecialAction> Actions { get; }
        List<SpecialAction> ActionList { get; }

        bool LoadActions();
        bool SaveActions();

        SpecialAction GetAction(string actionName);
        int GetActionIndex(string actionName);
        bool ActionExists(string actionName);

        bool AddAction(SpecialAction action);
        bool RemoveAction(string actionName);
        bool ReplaceAction(string oldActionName, SpecialAction newAction);

        event EventHandler ActionsChanged;
    }
}
