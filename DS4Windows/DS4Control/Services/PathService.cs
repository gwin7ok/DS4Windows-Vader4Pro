using System;
using System.IO;
using DS4Windows.DI;

namespace DS4Windows
{
    public class PathService : IPathService
    {
        // カスタム設定されたパス（単体テスト等で明示的に上書きされた場合のみ保持）
        private string _customAppDataPath;

        public PathService(string appDataPath = null)
        {
            if (!string.IsNullOrEmpty(appDataPath))
            {
                _customAppDataPath = appDataPath;
            }
        }

        /// <summary>
        /// アプリケーションデータパスを取得または設定します。
        /// 固定キャッシュを行わず、常に Global.appdatapath の最新値を On-Demand 評価します（§5.4 ガードレール）。
        /// これにより、起動順序逆転によるパス固定化ハザードを完全に防止します。
        /// </summary>
        public string AppDataPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_customAppDataPath))
                    return _customAppDataPath;

                return !string.IsNullOrEmpty(Global.appdatapath)
                    ? Global.appdatapath
                    : AppContext.BaseDirectory;
            }
            set => _customAppDataPath = value;
        }

        public string ExecutableDirectory => AppContext.BaseDirectory;

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

        public string ActionsPath => Path.Combine(AppDataPath, "Actions.xml");
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

        public string GetAutoProfilesPath() => AutoProfilesPath;
    }
}
