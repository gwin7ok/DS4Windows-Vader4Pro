using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DS4Windows.DI;

namespace DS4Windows
{
    public class SpecialActionRepository : ISpecialActionRepository
    {
        private readonly object _actionLock = new object();
        private readonly List<SpecialAction> _actions = new List<SpecialAction>();

        public event EventHandler ActionsChanged;

        public string ActionsPath
        {
            get
            {
                string baseDir = !string.IsNullOrEmpty(Global.appdatapath)
                    ? Global.appdatapath
                    : AppContext.BaseDirectory;
                return Path.Combine(baseDir, "Actions.xml");
            }
        }

        public IReadOnlyList<SpecialAction> Actions
        {
            get
            {
                lock (_actionLock)
                {
                    return _actions.ToList().AsReadOnly();
                }
            }
        }

        public List<SpecialAction> ActionList
        {
            get => _actions;
        }

        public bool LoadActions()
        {
            lock (_actionLock)
            {
                try
                {
                    if (!File.Exists(ActionsPath))
                        return false;

                    Global.LoadActions();
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace("[DI] SpecialActionRepository.LoadActions: Actions.xml loaded via DI");
                    OnActionsChanged();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool SaveActions()
        {
            lock (_actionLock)
            {
                try
                {
                    Global.SaveActions();
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace("[DI] SpecialActionRepository.SaveActions: Actions.xml saved via DI");
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public SpecialAction GetAction(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName))
                return null;

            lock (_actionLock)
            {
                return _actions.FirstOrDefault(a => string.Equals(a.name, actionName, StringComparison.OrdinalIgnoreCase));
            }
        }

        public int GetActionIndex(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName))
                return -1;

            lock (_actionLock)
            {
                return _actions.FindIndex(a => string.Equals(a.name, actionName, StringComparison.OrdinalIgnoreCase));
            }
        }

        public bool ActionExists(string actionName)
        {
            return GetAction(actionName) != null;
        }

        public bool AddAction(SpecialAction action)
        {
            if (action == null || string.IsNullOrWhiteSpace(action.name))
                return false;

            lock (_actionLock)
            {
                int index = GetActionIndex(action.name);
                if (index >= 0)
                {
                    _actions[index] = action;
                }
                else
                {
                    _actions.Add(action);
                }
                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace($"[DI] SpecialActionRepository.AddAction: Action '{action.name}' added via DI");
                OnActionsChanged();
                return true;
            }
        }

        public bool RemoveAction(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName))
                return false;

            lock (_actionLock)
            {
                int index = GetActionIndex(actionName);
                if (index >= 0)
                {
                    _actions.RemoveAt(index);
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] SpecialActionRepository.RemoveAction: Action '{actionName}' removed via DI");
                    OnActionsChanged();
                    return true;
                }
                return false;
            }
        }

        public bool ReplaceAction(string oldActionName, SpecialAction newAction)
        {
            if (string.IsNullOrWhiteSpace(oldActionName) || newAction == null)
                return false;

            lock (_actionLock)
            {
                int index = GetActionIndex(oldActionName);
                if (index >= 0)
                {
                    _actions[index] = newAction;
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] SpecialActionRepository.ReplaceAction: Action '{oldActionName}' replaced via DI");
                    OnActionsChanged();
                    return true;
                }
                return false;
            }
        }

        protected virtual void OnActionsChanged()
        {
            ActionsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
