using System;
using System.IO;
using DS4Windows.DI;

namespace DS4Windows
{
    public class PathService : IPathService
    {
        private readonly object _syncLock = new object();
        private string _appDataPath;

        public string AppDataPath
        {
            get
            {
                lock (_syncLock)
                {
                    if (string.IsNullOrWhiteSpace(_appDataPath))
                    {
                        _appDataPath = !string.IsNullOrEmpty(Global.appdatapath)
                            ? Global.appdatapath
                            : AppContext.BaseDirectory;
                    }
                    return _appDataPath;
                }
            }
            set
            {
                lock (_syncLock)
                {
                    _appDataPath = value;
                }
            }
        }

        public string ExecutableDirectory => AppDomain.CurrentDomain.BaseDirectory;

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

        public string GetProfilePath(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
                return string.Empty;

            if (!profileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                profileName += ".xml";

            return Path.Combine(ProfilesPath, profileName);
        }

        public string GetAutoProfilesPath() => Path.Combine(AppDataPath, "Auto Profiles.xml");
    }
}
