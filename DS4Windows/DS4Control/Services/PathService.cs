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
                        if (AppLogger.IsTraceEnabled)
                            AppLogger.LogTrace($"[DI] PathService: AppDataPath resolved to '{_appDataPath}'");
                    }
                    return _appDataPath;
                }
            }
            set
            {
                lock (_syncLock)
                {
                    _appDataPath = value;
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] PathService: AppDataPath explicitly set to '{value}'");
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

            string resolvedPath = Path.Combine(ProfilesPath, profileName);
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] PathService.GetProfilePath: Profile '{profileName}' -> '{resolvedPath}'");
            return resolvedPath;
        }

        public string GetAutoProfilesPath()
        {
            string path = Path.Combine(AppDataPath, "Auto Profiles.xml");
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] PathService.GetAutoProfilesPath: '{path}'");
            return path;
        }
    }
}
