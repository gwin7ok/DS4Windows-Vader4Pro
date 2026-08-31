using System;

namespace DS4Windows.DI
{
    public interface IPathService
    {
        string AppDataPath { get; set; }
        string ExecutableDirectory { get; }
        string ProfilesPath { get; }
        string ActionsPath { get; }

        string GetProfilePath(string profileName);
        string GetAutoProfilesPath();
    }
}
