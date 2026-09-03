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
        private readonly BackingStore _config;
        private IPathService _pathService;

        public event EventHandler ActionsChanged;

        public SpecialActionRepository(BackingStore config = null, IPathService pathService = null)
        {
            _config = config ?? Global.store;
            _pathService = pathService;
        }

        private IPathService PathSvc => _pathService ??= Global.PathServiceInstance;

        public string ActionsPath
        {
            get
            {
                string baseDir = PathSvc != null && !string.IsNullOrEmpty(PathSvc.AppDataPath)
                    ? PathSvc.AppDataPath
                    : (!string.IsNullOrEmpty(Global.appdatapath) ? Global.appdatapath : AppContext.BaseDirectory);
                return Path.Combine(baseDir, "Actions.xml");
            }
        }

        public IReadOnlyList<SpecialAction> Actions
        {
            get
            {
                lock (_actionLock)
                {
                    return _config != null && _config.actions != null
                        ? _config.actions.ToList().AsReadOnly()
                        : Array.Empty<SpecialAction>();
                }
            }
        }

        public List<SpecialAction> ActionList
        {
            get => _config != null ? _config.actions : null;
        }

        public bool LoadActions()
        {
            lock (_actionLock)
            {
                try
                {
                    if (!File.Exists(ActionsPath))
                        return false;

                    bool result = _config != null ? _config.LoadActions() : Global.LoadActions();
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] SpecialActionRepository.LoadActions: Actions.xml loaded via DI (result={result})");
                    OnActionsChanged();
                    return result;
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
                    if (_config != null)
                    {
                        _config.SaveActions();
                    }
                    else
                    {
                        Global.SaveActions();
                    }

                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace("[DI] SpecialActionRepository.SaveActions: Actions.xml saved via DI");

                    return true;
                }
                catch (Exception ex)
                {
                    AppLogger.LogToGui($"Failed to save Actions.xml: {ex.Message}", true);
                    return false;
                }
            }
        }

        public SpecialAction GetAction(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName) || _config == null || _config.actions == null)
                return null;

            lock (_actionLock)
            {
                return _config.actions.FirstOrDefault(a => string.Equals(a.name, actionName, StringComparison.OrdinalIgnoreCase));
            }
        }

        public int GetActionIndex(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName) || _config == null || _config.actions == null)
                return -1;

            lock (_actionLock)
            {
                return _config.actions.FindIndex(a => string.Equals(a.name, actionName, StringComparison.OrdinalIgnoreCase));
            }
        }

        public bool ActionExists(string actionName)
        {
            return GetAction(actionName) != null;
        }

        public bool AddAction(SpecialAction action)
        {
            if (action == null || string.IsNullOrWhiteSpace(action.name) || _config == null || _config.actions == null)
                return false;

            lock (_actionLock)
            {
                int index = GetActionIndex(action.name);
                if (index >= 0)
                {
                    _config.actions[index] = action;
                }
                else
                {
                    _config.actions.Add(action);
                }

                if (AppLogger.IsTraceEnabled)
                    AppLogger.LogTrace($"[DI] SpecialActionRepository.AddAction: Action '{action.name}' added via DI");
                OnActionsChanged();
                return true;
            }
        }

        public bool RemoveAction(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName) || _config == null || _config.actions == null)
                return false;

            lock (_actionLock)
            {
                int index = GetActionIndex(actionName);
                if (index >= 0)
                {
                    _config.actions.RemoveAt(index);
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
            if (string.IsNullOrWhiteSpace(oldActionName) || newAction == null || _config == null || _config.actions == null)
                return false;

            lock (_actionLock)
            {
                int index = GetActionIndex(oldActionName);
                if (index >= 0)
                {
                    _config.actions[index] = newAction;
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
