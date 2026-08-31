using System;
using System.Collections.Generic;

namespace DS4Windows.DI
{
    public interface IProfileRepository
    {
        string ProfilesPath { get; }
        string GetProfilePath(string profileName);

        bool LoadProfile(int deviceIndex, string profileName);
        bool SaveProfile(int deviceIndex, string profileName);

        bool LoadDefaultProfile(int deviceIndex);
        bool LoadProfileToSlot(int deviceIndex, string profileName);

        IReadOnlyList<string> GetProfileNames();
        bool ProfileExists(string profileName);

        bool ApplyProfileDirect(int deviceIndex, string profileName);
        bool RestoreProfileDirect(int deviceIndex);
    }
}
