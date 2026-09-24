using System;

namespace DS4Windows.DI
{
    public interface IPathService
    {
        string AppDataPath { get; set; }
        string ExecutableDirectory { get; }
        string ExecutablePath { get; }
        string ProfilesPath { get; }
        string ActionsPath { get; }

        string GetProfilePath(string profileName);
        string GetAutoProfilesPath();

        // ---- Phase6-Step7-1: App.xaml.cs（Post-Host）の Global 直接参照解消（Global への薄い委譲）----
        /// <summary>
        /// ローミング AppData 配下の既定の保存先（<c>%APPDATA%\DS4Windows</c>）。<c>Global.appDataPpath</c> と同じ値。
        /// 現在の保存先 <see cref="AppDataPath"/> とは別物（初回起動時のコピー先・候補として使う）。
        /// </summary>
        string RoamingAppDataPath { get; }

        /// <summary>
        /// プログラムフォルダと AppData の両方に設定がある状態か（<c>Global.multisavespots</c>）。
        /// 起動時の <c>Global.FindConfigLocation</c> が設定する。
        /// </summary>
        bool HasMultipleSaveLocations { get; }
    }
}