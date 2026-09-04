using System;
using System.IO;
using DS4Windows.DI;

namespace DS4Windows
{
    public class PathService : IPathService
    {
        private string _appDataPath;

        public PathService(string appDataPath = null)
        {
            _appDataPath = appDataPath;
        }

        /// <summary>
        /// アプリケーションデータパスを取得または設定します。
        /// Global.appdatapath との相互循環再帰を防ぎ、安全に On-Demand 解決します（§5.4 ガードレール）。
        /// </summary>
        public string AppDataPath
        {
            get
            {
                return !string.IsNullOrEmpty(_appDataPath)
                    ? _appDataPath
                    : AppContext.BaseDirectory;
            }
            set => _appDataPath = value;
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
