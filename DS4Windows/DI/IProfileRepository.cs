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

        // Phase7-Step2 W5: LoadDefaultProfile／LoadProfileToSlot／GetProfileNames／ProfileExists は削除した（本体の呼び出しが 0 件。
        // 読み込みは LoadProfile、プリセットは ProfilePresetLoader、一覧は UI の ProfileList が担う重複）。
        // 参照: docs-forDIMG/MadeByAgent/Phase7-Step2-Plan.md §2.4

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

        /// <summary>
        /// Phase6-Step10-4-5b: プロファイルの名前変更に伴い、リンクプロファイル（コントローラーとプロファイルの紐づけ）と、
        /// 画面表示用の <see cref="LinkedProfileUI"/> に残っている旧名を、新名へ置き換える。置き換えた件数を返す。
        /// 1 件以上置き換えた場合は、LinkedProfiles.xml へ保存する（接続中でないコントローラーの紐づけも対象）。
        /// </summary>
        int ReplaceLinkedProfileName(string oldName, string newName);

        /// <summary>
        /// Phase6-Step10-4-5d: プロファイルの名前変更に伴い、スペシャルアクション（プロファイル切替）の切替先として
        /// 保存されている旧名を、新名へ置き換える。置き換えた件数を返す。1 件以上置き換えた場合は、Actions.xml へ保存する。
        /// </summary>
        int ReplaceProfileSwitchTarget(string oldName, string newName);
        // ---- Phase5-Step13-5: デバイス別・割当済みSpecialAction名リスト ----
        List<string>[] ProfileActions { get; }
        void CacheExtraProfileInfo(int deviceIndex);

        // ---- Phase5-Step13-6: プロファイルのカスタムフラグキャッシュ更新 ----
        void CacheProfileCustomsFlags(int deviceIndex);

        // ---- Phase5-Step13-7: 一時プロファイル読込（スワイプ操作等での即時切替用） ----
        bool LoadTempProfile(int deviceIndex, string profileName, bool launchProgram, ControlService control, bool xinputChange = true);

        // ---- Phase5-Step13-7 Tier4: プロファイル未定義アクションの警告ログ出力 ----
        void EmitMissingActionLogsForDevice(int deviceIndex, bool forceEmit = false, string overrideProfileName = null);


        // ---- Phase6-Step2-2 (PR-2): リンクプロファイル解決（Global への薄い委譲）----
        bool ContainsLinkedProfile(string serial);
        string GetLinkedProfile(string serial);

        // ---- Phase6-Step7-1: App.xaml.cs（Post-Host）の Global 直接参照解消（Global への薄い委譲）----
        /// <summary>
        /// スロットの設定を初期値に戻したうえで、プロファイル名 <paramref name="profileName"/> として保存する
        /// （初回起動時の既定プロファイル作成に使う）。保存の成否を返す。
        /// </summary>
        bool SaveAsProfile(int deviceIndex, string profileName);

        /// <summary>LinkedProfiles.xml（コントローラーとプロファイルの紐付け）を読み込む。</summary>
        bool LoadLinkedProfiles();

        // Phase7-Step3c W2: LoadBlankProfile は削除した（IProfilePresetLoader.LoadBlank と同じ処理の重複。呼び出し元の
        // ProfileSettingsViewModel は IProfilePresetLoader を使う）。
        // 参照: docs-forDIMG/MadeByAgent/Phase7-Step3c-Plan.md Q4

        // ---- Phase6-Step10-3b-1: アクションの名前変更・削除の、全プロファイルへの反映（機能追加）----
        // TODO(Step10-3b-3/3b-4 で接続予定): 画面（ProfileEditor の「除去」ボタン、アクションの名前変更）からの呼び出しは
        // 未接続。Phase6-Step10-3b-Plan.md §7 のとおり、書き換えたプロファイルの一覧を MainWindow へ通知し、
        // 既存の SyncProfileListAndControllers（保存時と同じ再適用）を 1 回呼ぶ。

        /// <summary>
        /// すべてのプロファイル XML の <c>&lt;ProfileActions&gt;</c> について、アクション名 <paramref name="oldName"/> を
        /// <paramref name="newName"/> へ置換する。<c>/</c> で区切った項目と完全一致（大文字小文字を区別）で比較する。
        /// 一致したファイルだけを保存する。ファイル 1 つの失敗は、他のファイルの処理を止めない。
        /// 新名が同じプロファイルに既にある場合は、重複させない。
        /// </summary>
        /// <returns>書き換えたプロファイル名（拡張子なし）の一覧</returns>
        IReadOnlyList<string> ReplaceActionReference(string oldName, string newName);

        /// <summary>
        /// すべてのプロファイル XML の <c>&lt;ProfileActions&gt;</c> から、アクション名 <paramref name="name"/> を取り除く。
        /// 比較・保存・失敗時の扱いは <see cref="ReplaceActionReference"/> と同じ。
        /// </summary>
        /// <returns>書き換えたプロファイル名（拡張子なし）の一覧</returns>
        IReadOnlyList<string> RemoveActionReference(string name);

        /// <summary>
        /// 接続中のコントローラーのスロットに適用されているプロファイルのうち、アクション <paramref name="actionName"/> を
        /// 使っているもの（スロットのメモリ上の <c>ProfileActions</c> に含まれるもの）の名前の一覧を返す。
        /// アクションの内容だけが変わったとき（XML の書き換えは不要）に、実行時のキャッシュを更新するための再適用の対象を求める。
        /// ファイルは読まない。比較は完全一致（大文字小文字を区別）。
        /// </summary>
        IReadOnlyList<string> GetAppliedProfilesUsingAction(string actionName);
    }
}