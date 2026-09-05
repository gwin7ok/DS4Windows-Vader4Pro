using System;
using System.Collections.Generic;
using DS4Windows;

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

        // ---- Phase5-Step13-2: デバイススロット別・実行時プロファイル状態 (m_Config委譲、BackingStore共有参照) ----
        string[] ProfilePath { get; }
        string[] OlderProfilePath { get; }
        string[] SelectedProfile { get; }
        string[] LinkedProfileUI { get; }

        event EventHandler<SelectedProfileChangedEventArgs> SelectedProfileChanged;
        void RaiseSelectedProfileChanged(int deviceIndex, string profileName);

        // ---- Phase5-Step13-2: LinkedProfile（コントローラーMAC単位のプロファイル紐付け）管理 ----
        void ChangeLinkedProfile(string serial, string profile);
        void RemoveLinkedProfile(string serial);
        bool SaveLinkedProfiles();
    }
}