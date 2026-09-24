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

        // Phase6-Step7-1（決定2＝L2）: Phase4-Step3（c06a1c9b）で作られたが App 側が未接続だった契約。
        // 従来は冒頭に「Actions.xml がなければ何もせず false を返す」判定があり、Global.LoadActions() と動作が違った
        // （BackingStore.LoadActions はファイルがなければ既定アクション「Disconnect Controller」を作って保存し true を返す）。
        // そのまま App から使うと、初回起動で CreateStandardActions が走り、全プロファイルの XML が書き換わってしまうため、
        // この判定を削除して Global と同じ動作にした（ファイルの有無の扱いは BackingStore.LoadActions に任せる）。
        // 残る差は、想定外の例外（XML・データ不正以外）も false にする点のみ（Global 側は例外を外へ投げる）。
        public bool LoadActions()
        {
            lock (_actionLock)
            {
                try
                {
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

        // ---- Phase6-Step7-1: App.xaml.cs（Post-Host）の Global 直接参照解消 ----
        // Global.CreateStdActions への薄い委譲。プロファイル XML の書き換えと読み込み直しは Global 側が行う。
        public void CreateStandardActions()
        {
            Global.CreateStdActions();
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace("[DI] SpecialActionRepository.CreateStandardActions: standard actions created via DI");
        }

        protected virtual void OnActionsChanged()
        {
            ActionsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
