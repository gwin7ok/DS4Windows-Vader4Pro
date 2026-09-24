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

        // ---- Phase6-Step7-1: App.xaml.cs（Post-Host）の Global 直接参照解消（Global への薄い委譲）----
        /// <summary>
        /// Actions.xml を読み込めなかったときの復旧処理（<c>Global.CreateStdActions</c>）。
        /// 各プロファイルの ProfileActions に「Disconnect Controller」を追加し、アクションを読み込み直す。
        /// </summary>
        void CreateStandardActions();

        event EventHandler ActionsChanged;
    }
}
