using System;

namespace DS4Windows.DI
{
    public interface IPathService
    {
        string AppDataPath { get; }
        string ExecutableDirectory { get; }
        string ExecutablePath { get; }
        string ProfilesPath { get; }

        string GetProfilePath(string profileName);

        // ---- Phase6-Step7-1: App.xaml.cs（Post-Host）の Global 直接参照解消（Global への薄い委譲）----
        /// <summary>
        /// ローミング AppData 配下の既定の保存先（<c>%APPDATA%\DS4Windows</c>）。<c>PathService.RoamingAppDataPathState</c> と同じ値。
        /// 現在の保存先 <see cref="AppDataPath"/> とは別物（初回起動時のコピー先・候補として使う）。
        /// </summary>
        string RoamingAppDataPath { get; }

        /// <summary>
        /// プログラムフォルダと AppData の両方に設定がある状態か（<c>PathService.MultipleSaveLocationsState</c>）。
        /// 起動時の <c>PathService.FindConfigLocation</c> が設定する。
        /// </summary>
        bool HasMultipleSaveLocations { get; }

        // ---- Phase6-Step9-1: ViewModel の Global.exedirpath 参照の置換先（Global への薄い委譲）----
        /// <summary>
        /// プログラムフォルダ（<c>Global.exedirpath</c> と同じ値。ジャンクション解決済みの実行ファイルの親フォルダ。末尾の区切りなし）。
        /// <see cref="ExecutableDirectory"/>（<c>AppContext.BaseDirectory</c>。末尾に区切りが付き、ジャンクションは解決されない）とは
        /// 値が異なりうるため、<c>exedirpath</c> の置換には本メンバーを使う。
        /// TODO(技術的負債): <see cref="ExecutableDirectory"/> との統合は Phase6-Step12 で判断する。
        /// </summary>
        string ProgramDirectory { get; }

        // ---- Phase6-Step10-1-1: SaveWhere 画面の Global 直接参照の置換先（Global への薄い委譲）----
        /// <summary>
        /// 設定ファイルの保存先を決める（<c>PathService.SaveWhere</c> と同じ動作）。
        /// 保存先のフォルダに加え、Profiles／Actions／LinkedProfiles／ControllerConfigs の各ファイルのパスも更新する。
        /// 保存先の決定には必ず本メソッドを使うこと（<see cref="AppDataPath"/> は読み取り専用。Phase6-Step12 Wave 5 でセッターを削除）。
        /// </summary>
        void SetSaveLocation(string path);

        /// <summary>
        /// 空のプロファイル一覧ファイルを作る（旧 <c>Global.SaveDefault</c> と同じ動作）。作成に失敗したら false。
        /// </summary>
        bool CreateEmptyProfilesFile(string path);
    }
}