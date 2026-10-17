using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using DS4Windows.DI;

namespace DS4Windows
{
    /// <summary>
    /// <see cref="IPathService"/> の実装。
    ///
    /// 【Phase7-Step4 W3】
    /// 実行ファイルの情報（場所・フォルダ・バージョン）、設定の保存場所（appdatapath）、初回起動・保存場所が 2 か所の判定と、
    /// それらを決める <see cref="FindConfigLocation()"/>／<see cref="SaveWhere"/>／空のプロファイル一覧の作成を、
    /// <c>Global</c> から本クラスへ移した。これらはプロセス全体で 1 つの状態（起動前の App、LoggerHolder、BackingStore、各サービスが
    /// 同じ値を見る）のため、<see cref="EnvironmentService"/> と同じく static な状態として持つ。インスタンスはどれを使っても
    /// 同じ状態を読み書きする。下位層（BackingStore／LoggerHolder など）が静的に読む <c>Global.appdatapath</c>／<c>exedirpath</c>／
    /// <c>exelocation</c>／<c>exeversion</c> は、本クラスの状態を指す読み書きの窓口として <c>Global</c> に残す
    /// （TODO(Phase7-Step10): BackingStore の永続化を切り出すときに、下位層へ <see cref="IPathService"/> を注入して窓口を廃止する）。
    /// 静的初期化の循環を作らないため、本クラスの static 初期化は <c>Global</c> を参照しない（回帰ガード: Phase7Step4PathServiceGuardTests）。
    /// </summary>
    public class PathService : IPathService
    {
        // ---- Phase7-Step4 W3: 旧 Global の static 状態 ----

        // Need to perform extra steps to check if DS4Windows is installed in a junction
        // directory (done with Scoop). Use real path when available
        private static readonly string s_executableLocation = ResolveExecutableLocation();
        private static readonly FileVersionInfo s_fileVersion = FileVersionInfo.GetVersionInfo(s_executableLocation);
        private static readonly string s_executableVersion = s_fileVersion.FileVersion;
        private static readonly string s_roamingAppDataPath =
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\DS4Windows";

        /// <summary>設定の保存先フォルダ（未決定のときは null。<see cref="SaveWhere"/> が決める）。</summary>
        public static string AppDataPathState { get; set; }

        /// <summary>実行ファイルのフルパス（ジャンクション解決済み）。</summary>
        public static string ExecutableLocationState => s_executableLocation;

        /// <summary>実行ファイルの親フォルダ（ジャンクション解決済み。末尾の区切りなし）。テストが一時フォルダへ向けることがある。</summary>
        public static string ExecutableDirectoryState { get; set; } = Directory.GetParent(s_executableLocation).FullName;

        /// <summary>実行ファイルのバージョン文字列。</summary>
        public static string ExecutableVersionState => s_executableVersion;

        /// <summary>ローミング AppData 配下の既定の保存先（%APPDATA%\DS4Windows）。</summary>
        public static string RoamingAppDataPathState => s_roamingAppDataPath;

        /// <summary>初回起動か（<see cref="FindConfigLocation()"/> が設定する）。</summary>
        public static bool FirstRunState { get; set; }

        /// <summary>プログラムフォルダと AppData の両方に設定がある状態か（<see cref="FindConfigLocation()"/> が設定する）。</summary>
        public static bool MultipleSaveLocationsState { get; set; }

        private static string ResolveExecutableLocation()
        {
            string filePath = Process.GetCurrentProcess().MainModule.FileName;
            DirectoryInfo dirInfo = new DirectoryInfo(Path.GetDirectoryName(filePath));
            // Check if exe is placed in a junction symlink directory (done with Scoop).
            // Good enough
            if (dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint) &&
                dirInfo.LinkTarget != null)
            {
                // App directory is a junction. Find real directory and get proper path
                // for inserting into HidHide
                filePath = Path.Combine(dirInfo.LinkTarget, Path.GetFileName(filePath));
            }

            return filePath;
        }

        // ---- インスタンス ----

        private string _customAppDataPath;
        private readonly BackingStore _config;

        // config: SaveWhere が設定ファイルのパス（m_Profile ほか）を更新する先。省略時は Global.store（本番の BackingStore と同じ実体）。
        // TODO(Phase7-Step10): 省略時の Global.store をやめ、BackingStore を必須の依存にする（BackingStore の永続化の切り出しと同時）。
        public PathService(string appDataPath = null, BackingStore config = null)
        {
            _customAppDataPath = appDataPath;
            _config = config ?? Global.store;
        }

        /// <summary>
        /// アプリケーションデータパスを取得します（コンストラクタ引数で指定した場合は、その値を返します）。
        /// 固定キャッシュを行わず、常に <see cref="AppDataPathState"/> の最新値を On-Demand 評価します（§5.4 ガードレール）。
        /// これにより、起動順序逆転によるパス固定化ハザードを完全に防止します。
        /// </summary>
        public string AppDataPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_customAppDataPath))
                    return _customAppDataPath;

                return !string.IsNullOrEmpty(AppDataPathState)
                    ? AppDataPathState
                    : AppContext.BaseDirectory;
            }
        }

        public string ExecutableDirectory => AppContext.BaseDirectory;

        // Phase6-Step9-1／Phase7-Step4 W3: 旧 Global.exedirpath（ExecutableDirectory とは値が異なりうる。IPathService の注記を参照）
        public string ProgramDirectory => ExecutableDirectoryState;

        /// <summary>
        /// 実行ファイルのフルパスを取得します。Scoop等のジャンクションシンボリックリンク解決ロジックを含みます
        /// （Phase7-Step4 W3: 旧 Global.exelocation の実体をここへ移した）。
        /// </summary>
        public string ExecutablePath => ExecutableLocationState;

        public string ProfilesPath
        {
            get
            {
                string path = Path.Combine(AppDataPath, "Profiles");
                if (!Directory.Exists(path))
                {
                    try { Directory.CreateDirectory(path); } catch { }
                }
                return path;
            }
        }

        public string AutoProfilesPath => Path.Combine(AppDataPath, "Auto Profiles.xml");
        public string LinkedProfilesPath => Path.Combine(AppDataPath, "LinkedProfiles.xml");
        public string ControllerConfigsPath => Path.Combine(AppDataPath, "ControllerConfigs.xml");

        public string GetProfilePath(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
                return string.Empty;

            if (!profileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                profileName += ".xml";

            return Path.Combine(ProfilesPath, profileName);
        }

        // ---- Phase6-Step7-1: App.xaml.cs（Post-Host）の Global 直接参照解消 ----
        public string RoamingAppDataPath => RoamingAppDataPathState;

        public bool HasMultipleSaveLocations => MultipleSaveLocationsState;

        // ---- Phase6-Step10-1-1／Phase7-Step4 W3: 保存場所の決定（AppDataPath の setter とは別物。IPathService の注記を参照）----
        public void SetSaveLocation(string path) => SaveWhere(_config, path);

        /// <summary>
        /// 保存場所を決める。<see cref="AppDataPathState"/> のほか、設定ファイル 4 つのパス（BackingStore）も更新する
        /// （旧 <c>Global.SaveWhere</c> と同じ動作）。
        /// </summary>
        public static void SaveWhere(BackingStore config, string path)
        {
            AppDataPathState = path;
            config.m_Profile = path + "\\Profiles.xml";
            config.m_Actions = path + "\\Actions.xml";
            config.m_linkedProfiles = path + "\\LinkedProfiles.xml";
            config.m_controllerConfigs = path + "\\ControllerConfigs.xml";
        }

        /// <summary>
        /// 設定の保存場所を決める（旧 <c>Global.FindConfigLocation</c>）。プログラムフォルダと AppData の
        /// "Auto Profiles.xml" の有無で、初回起動・保存場所が 2 か所・保存先を決める。起動時に 1 度呼ぶ。
        /// </summary>
        public void FindConfigLocation() =>
            FindConfigLocation(_config, ExecutableDirectoryState, RoamingAppDataPathState, File.Exists);

        /// <summary>
        /// <see cref="FindConfigLocation()"/> の本体。実行ファイルのフォルダ、ローミングのフォルダ、ファイルの有無の判定を
        /// 引数で受け取るため、4 通りの分岐をテストで固定できる。分岐の内容は旧 <c>Global.FindConfigLocation</c> のまま:
        /// - 両方にある（同じフォルダでない）: 初回起動＋保存場所が 2 か所。保存先は決めない。
        /// - プログラムフォルダだけ、または両方が同じフォルダ: 保存先はプログラムフォルダ。
        /// - AppData だけ: 保存先は AppData。
        /// - どちらにもない: 初回起動（保存場所は 1 か所）。保存先は決めない。
        /// どの分岐も、該当しないフラグは変更しない（false に戻さない）。
        /// </summary>
        public static void FindConfigLocation(BackingStore config, string executableDirectory,
            string roamingAppDataPath, Func<string, bool> fileExists)
        {
            bool programFolderAutoProfilesExists = fileExists(Path.Combine(executableDirectory, "Auto Profiles.xml"));
            bool appDataAutoProfilesExists = fileExists(Path.Combine(roamingAppDataPath, "Auto Profiles.xml"));
            bool isSameFolder = appDataAutoProfilesExists && executableDirectory == roamingAppDataPath;

            if (programFolderAutoProfilesExists && appDataAutoProfilesExists &&
                !isSameFolder)
            {
                FirstRunState = true;
                MultipleSaveLocationsState = true;
            }
            else if (programFolderAutoProfilesExists)
            {
                SaveWhere(config, executableDirectory);
            }
            else if (appDataAutoProfilesExists)
            {
                SaveWhere(config, roamingAppDataPath);
            }
            else if (!programFolderAutoProfilesExists && !appDataAutoProfilesExists)
            {
                FirstRunState = true;
                MultipleSaveLocationsState = false;
            }
        }

        /// <summary>空のプロファイル一覧ファイルを作る（旧 <c>Global.SaveDefault</c>）。作成に失敗したら false。</summary>
        public bool CreateEmptyProfilesFile(string path)
        {
            Boolean Saved = true;
            XmlDocument m_Xdoc = new XmlDocument();
            try
            {
                XmlNode Node;

                m_Xdoc.RemoveAll();

                Node = m_Xdoc.CreateXmlDeclaration("1.0", "utf-8", String.Empty);
                m_Xdoc.AppendChild(Node);

                Node = m_Xdoc.CreateComment(string.Format(" Profile Configuration Data. {0} ", DateTime.Now));
                m_Xdoc.AppendChild(Node);

                Node = m_Xdoc.CreateWhitespace("\r\n");
                m_Xdoc.AppendChild(Node);

                Node = m_Xdoc.CreateNode(XmlNodeType.Element, "Profile", null);

                m_Xdoc.AppendChild(Node);

                m_Xdoc.Save(path);
            }
            catch { Saved = false; }

            return Saved;
        }
    }
}
