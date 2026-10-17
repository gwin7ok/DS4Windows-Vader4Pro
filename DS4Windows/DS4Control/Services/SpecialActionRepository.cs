using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using DS4Windows.DI;

namespace DS4Windows
{
    public class SpecialActionRepository : ISpecialActionRepository
    {
        private readonly object _actionLock = new object();
        private readonly BackingStore _config;
        private readonly IPathService _pathService;
        // Phase7-Step8 W1: キー型スペシャルアクションの保存後に、合成入力の状態をリセットするために使う（旧: Global.SaveAction → Mapping）
        private readonly SyntheticInputState _syntheticInput;
        // Phase7-Step10c W2: Actions.xml の保存・読込（旧: BackingStore のメソッド）
        private readonly Services.SpecialActionsXmlSerializer _actionsXml;

        public event EventHandler ActionsChanged;

        public SpecialActionRepository(BackingStore config, IPathService pathService, SyntheticInputState syntheticInput,
            Services.SpecialActionsXmlSerializer actionsXml)
        {
            _actionsXml = actionsXml ?? throw new ArgumentNullException(nameof(actionsXml));
            _syntheticInput = syntheticInput ?? throw new ArgumentNullException(nameof(syntheticInput));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        }

        private IPathService PathSvc => _pathService;

        public string ActionsPath
        {
            get
            {
                string baseDir = !string.IsNullOrEmpty(PathSvc.AppDataPath)
                    ? PathSvc.AppDataPath
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
                    bool result = _actionsXml.LoadActions();
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

        public SpecialAction GetAction(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName) || _config == null || _config.actions == null)
                return null;

            lock (_actionLock)
            {
                return _config.actions.FirstOrDefault(a => string.Equals(a.name, actionName, StringComparison.OrdinalIgnoreCase));
            }
        }

        public bool ActionExists(string actionName)
        {
            return GetAction(actionName) != null;
        }

        // ---- Phase6-Step7-1: App.xaml.cs（Post-Host）の Global 直接参照解消 ----
        // Global.CreateStdActions への薄い委譲。プロファイル XML の書き換えと読み込み直しは Global 側が行う。
        public void CreateStandardActions()
        {
            CreateStdActions();
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace("[DI] SpecialActionRepository.CreateStandardActions: standard actions created via DI");
        }

        // ---- Phase6-Step8-1: ProfileEditor.xaml.cs の Global 直接参照解消 ----
        // Global.GetAction（m_Config.GetAction）と同じ実体 BackingStore.GetAction への薄い委譲。
        // 本リポジトリの _config は通常 Global.store（= m_Config）なので結果は Global.GetAction と一致する。
        public SpecialAction GetActionOrPlaceholder(string actionName)
        {
            if (_config == null)
                return new SpecialAction("null", "null", "null", "null");

            return _config.GetAction(actionName);
        }

        // ---- Phase6-Step10-3-1: スペシャルアクション編集画面の Global 直接参照解消 ----
        // Phase7-Step10c W2: 旧 Global.SaveAction / Global.RemoveAction の処理（本クラスのプライベートメソッド）。これらは、実行中のアクション状態のクリア、
        // Actions.xml への書き込み、読み直しと検証（SaveAction ではキー種別のランタイム状態のリセットも）を行う。
        // （メモリ上のリストだけを変える旧 AddAction／RemoveAction は Phase7-Step2 で削除した。）
        // 対象は Global.store（Global.m_Config と同じ実体）。本リポジトリの _config が Global.store 以外の場合でも、
        // Global と同じ実体に対して動作する。
        public void SaveAction(string name, string controls, int mode, string details, bool edit,
            double delayTime = 0.0, string extras = "")
        {
            SaveActionAndReload(name, controls, mode, details, edit, delayTime, extras);

            // Phase7-Step8 W1: 旧 Global.SaveAction の末尾の処理。キー型（mode==4）を保存したときだけ、当該キーの実行時状態をリセットする
            try
            {
                if (mode == 4 && ushort.TryParse(details, out ushort savedKey))
                {
                    _syntheticInput.ResetRuntimeStateForKey(savedKey);
                }
            }
            catch { }
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] SpecialActionRepository.SaveAction: Action '{name}' saved via DI (mode={mode}, edit={edit})");
            OnActionsChanged();
        }

        public bool RemoveActionAndSave(string actionName)
        {
            bool existed = ActionExists(actionName);
            RemoveActionAndReload(actionName);
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] SpecialActionRepository.RemoveActionAndSave: Action '{actionName}' removed and saved via DI (existed={existed})");
            OnActionsChanged();
            return existed;
        }


        // ---- Phase7-Step10c W2: 旧 Global.SaveAction／RemoveAction／ReloadActionsAndVerify（Actions.xml の保存・読込のシム）の移動先 ----
        // 実行中のアクション状態のクリア → Actions.xml への書き込み（SpecialActionsXmlSerializer）→ 読み直しと件数の検証、の順序・ログは従来のまま。
        private static void ClearAllActionEntries()
        {
            try
            {
                try { ActionManager.ClearAllEntries(); } catch { }
            }
            catch { }
        }

        private void SaveActionAndReload(string name, string controls, int mode, string details, bool edit, double delayTime, string extras)
        {
            // Mark actionDone as uninitialized before mutating actions to reduce
            // the window where MapCustomAction can observe an inconsistent size.
            ClearAllActionEntries();

            _actionsXml.SaveActionNew(name, controls, mode, details, edit, delayTime, extras);

            // Reload actions from file and verify counts to avoid duplicated in-memory entries.
            ReloadActionsAndVerify("SaveAction");
        }

        private void RemoveActionAndReload(string name)
        {
            ClearAllActionEntries();

            _actionsXml.RemoveAction(name);

            // After removal, reload to ensure consistency between file and memory.
            ReloadActionsAndVerify("RemoveAction");
        }

        // Reload Actions.xml into the backing store and verify that the in-memory
        // `actions` count matches the number of <Action> elements in the file.
        private void ReloadActionsAndVerify(string caller)
        {
            try
            {
                // Attempt to reload from the canonical file
                bool loaded = _actionsXml.LoadActions();

                int memCount = _config.actions?.Count ?? 0;
                int fileCount = -1;
                try
                {
                    XmlDocument xd = new XmlDocument();
                    xd.Load(_config.m_Actions);
                    XmlNodeList nodes = xd.SelectNodes("/Actions/Action");
                    fileCount = nodes?.Count ?? 0;
                }
                catch (Exception ex)
                {
                    AppLogger.LogToGui($"Could not count Actions.xml entries: {ex.Message}", false);
                }

                if (fileCount >= 0)
                {
                    if (memCount != fileCount)
                    {
                        AppLogger.LogToGui($"Warning: action count mismatch after {caller}: memory={memCount} file={fileCount}", true);
                    }
                    else
                    {
                        AppLogger.LogToGui($"Action list reloaded ({memCount} entries) after {caller}.", false);
                    }
                }
                else
                {
                    AppLogger.LogToGui($"Action list reloaded ({memCount} entries) after {caller}. (file count unknown)", false);
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogToGui($"Error reloading Actions.xml after {caller}: {ex.Message}", true);
            }
        }

        // Phase7-Step10c W2: 旧 Global.CreateStdActions（唯一の呼び出し元はこのクラス）。プロファイル XML へ既定のアクションを書き足し、Actions.xml を読み直す。
        private void CreateStdActions()
        {
            XmlDocument xDoc = new XmlDocument();
            try
            {
                string[] profiles = Directory.GetFiles(_pathService.AppDataPath + @"\Profiles\");
                string s = string.Empty;
                //foreach (string s in profiles)
                for (int i = 0, proflen = profiles.Length; i < proflen; i++)
                {
                    s = profiles[i];
                    if (Path.GetExtension(s) == ".xml")
                    {
                        xDoc.Load(s);
                        XmlNode el = xDoc.SelectSingleNode("DS4Windows/ProfileActions");
                        if (el != null)
                        {
                            if (string.IsNullOrEmpty(el.InnerText))
                                el.InnerText = "Disconnect Controller";
                            else
                                el.InnerText += "/Disconnect Controller";
                        }
                        else
                        {
                            XmlNode Node = xDoc.SelectSingleNode("DS4Windows");
                            el = xDoc.CreateElement("ProfileActions");
                            el.InnerText = "Disconnect Controller";
                            Node.AppendChild(el);
                        }

                        xDoc.Save(s);
                        _actionsXml.LoadActions();
                    }
                }
            }
            catch { }
        }

        protected virtual void OnActionsChanged()
        {
            ActionsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
