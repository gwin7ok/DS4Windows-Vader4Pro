using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DS4Windows.DI;

namespace DS4Windows
{
    public class ProfileRepository : IProfileRepository
    {
        private readonly object _fileLock = new object();
        // Phase7-Step3 W1: Global 経由の参照をやめ、Composition Root から注入された BackingStore（設定の唯一の実体）を使う。
        private readonly BackingStore _config;
        private readonly IProfileSettingsService _profileSettings;
        private readonly IProfileXmlStore _profileXmlStore;
        private readonly IPathService _pathService;

        // Phase6-Step10-4-2b: 全ての依存を必須化（Global.*Instance／AppHost のフォールバックを廃止）。
        // ControlService は ControlService → IProfileRepository の依存循環を避けるため、遅延解決（Func）で受け取る。
        private readonly Func<ControlService> _controlProvider;
        // Phase7-Step10c W2: リンクプロファイル・Actions.xml の保存・読込（旧: BackingStore のメソッド）
        private readonly Services.LinkedProfilesXmlSerializer _linkedProfilesXml;
        private readonly Services.SpecialActionsXmlSerializer _actionsXml;
        // Phase7-Step10c W4: プロファイル（旧形式の XML）の読込（旧: BackingStore.LoadProfile）
        private readonly Services.LegacyProfileXmlSerializer _legacyProfileXml;

        public ProfileRepository(BackingStore config, IProfileSettingsService profileSettings,
            IProfileXmlStore profileXmlStore, IPathService pathService,
            Func<ControlService> controlProvider,
            Services.LinkedProfilesXmlSerializer linkedProfilesXml, Services.SpecialActionsXmlSerializer actionsXml,
            Services.LegacyProfileXmlSerializer legacyProfileXml)
        {
            _legacyProfileXml = legacyProfileXml ?? throw new ArgumentNullException(nameof(legacyProfileXml));
            _linkedProfilesXml = linkedProfilesXml ?? throw new ArgumentNullException(nameof(linkedProfilesXml));
            _actionsXml = actionsXml ?? throw new ArgumentNullException(nameof(actionsXml));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _profileXmlStore = profileXmlStore ?? throw new ArgumentNullException(nameof(profileXmlStore));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _controlProvider = controlProvider ?? throw new ArgumentNullException(nameof(controlProvider));
        }

        private IPathService PathSvc => _pathService;

        public string ProfilesPath => PathSvc.ProfilesPath;

        public string GetProfilePath(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
                return string.Empty;

            if (!profileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                profileName += ".xml";

            return Path.Combine(ProfilesPath, profileName);
        }

        public bool LoadProfile(int deviceIndex, string profileName)
        {
            lock (_fileLock)
            {
                try
                {
                    string path = GetProfilePath(profileName);
                    if (!File.Exists(path))
                        return false;

                    string normalizedName = Path.GetFileNameWithoutExtension(profileName);
                    ProfilePath[deviceIndex] = normalizedName;
                    ControlService control = _controlProvider();
                    if (control == null)
                        return false;

                    // Phase5-Step2: Global.LoadProfileへの委譲をやめ、XML I/O(IProfileXmlStore)と
                    // 状態調整ロジック(旧Global.LoadProfile内で行っていたもの)をここに直接内包する。
                    // 挙動はGlobal.LoadProfileと完全に同一に維持する。
                    _config.loggedInvalidActions.Clear();
                    _profileXmlStore.LoadProfileXml(deviceIndex, false, control, "", false, true);
                    _profileSettings?.SetTempProfileName(deviceIndex, string.Empty);
                    _profileSettings?.SetUseTempProfile(deviceIndex, false);
                    _profileSettings?.SetTempProfileDistance(deviceIndex, false);

                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] ProfileRepository.LoadProfile: Slot {deviceIndex}, Profile '{profileName}' loaded via DI");
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool SaveProfile(int deviceIndex, string profileName)
        {
            lock (_fileLock)
            {
                try
                {
                    string path = GetProfilePath(profileName);
                    if (string.IsNullOrEmpty(path))
                    {
                        AppLogger.LogToGui($"Failed to save profile: invalid path for '{profileName}'", true);
                        return false;
                    }

                    // Phase5-Step2/Step4: IProfileXmlStore.SaveProfileXmlの成否(bool)を伝播し、成否に応じた通知・ログを出力
                    bool saveSuccess = _profileXmlStore.SaveProfileXml(deviceIndex, profileName);
                    if (saveSuccess)
                    {
                        if (AppLogger.IsTraceEnabled)
                            AppLogger.LogTrace($"[DI] SaveProfile succeeded: Slot {deviceIndex}, Profile '{profileName}'");
                    }
                    else
                    {
                        AppLogger.LogToGui($"Failed to save profile '{profileName}' for device {deviceIndex + 1}", true);
                        if (AppLogger.IsTraceEnabled)
                            AppLogger.LogTrace($"[DI] SaveProfile failed: Slot {deviceIndex}, Profile '{profileName}'");
                    }

                    return saveSuccess;
                }
                catch (Exception ex)
                {
                    AppLogger.LogToGui($"Failed to save profile '{profileName}' for device {deviceIndex + 1}: {ex.Message}", true);
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] SaveProfile failed with exception: Slot {deviceIndex}, Profile '{profileName}', error={ex}");
                    return false;
                }
            }
        }

        public bool ApplyProfileDirect(int deviceIndex, string profileName)
        {
            if (deviceIndex < 0 || deviceIndex >= ProfileSettingsService.MAX_DS4_CONTROLLER_COUNT)
                return false;

            _profileSettings?.SetUseTempProfile(deviceIndex, true);
            _profileSettings?.SetTempProfileName(deviceIndex, profileName);
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ProfileRepository.ApplyProfileDirect: Slot {deviceIndex}, Temp Profile '{profileName}' applied via DI");
            return LoadProfile(deviceIndex, profileName);
        }

        public bool RestoreProfileDirect(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= ProfileSettingsService.MAX_DS4_CONTROLLER_COUNT)
                return false;

            _profileSettings?.SetUseTempProfile(deviceIndex, false);
            _profileSettings?.SetTempProfileName(deviceIndex, string.Empty);
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ProfileRepository.RestoreProfileDirect: Slot {deviceIndex} restored via DI");
            return LoadProfile(deviceIndex, string.Empty);
        }

        // ---- Phase5-Step13-2: デバイススロット別・実行時プロファイル状態 ----
        // Global.ProfilePath 等は m_Config(BackingStore) への薄い公開アクセサであり、
        // 本クラスの LoadProfile 内でも同一配列(Global.ProfilePath[deviceIndex])へ直接書き込んでいる
        // 既存の単一の実体であるため、状態を複製せずそのまま公開する(Phase4-Step10の知見に準拠)。
        public string[] ProfilePath => _config.profilePath;
        public string[] OlderProfilePath => _config.olderProfilePath;
        public string[] SelectedProfile => _config.selectedProfile;
        public string[] LinkedProfileUI => _config.linkedProfileUI;

        // Phase7-Step3c W4: Global の静的イベントから、このリポジトリ（DI のシングルトン）のインスタンスイベントにした。
        // 発火元は ProfileApplicationService（RaiseSelectedProfileChanged 経由）。購読者は ControllerListViewModel のみ。
        public event EventHandler<SelectedProfileChangedEventArgs> SelectedProfileChanged;

        public void RaiseSelectedProfileChanged(int deviceIndex, string profileName)
            => SelectedProfileChanged?.Invoke(this, new SelectedProfileChangedEventArgs(deviceIndex, profileName));

        // ---- Phase5-Step13-2: LinkedProfile（コントローラーMAC単位のプロファイル紐付け）管理 ----
        // Phase7-Step3 W5: 旧 Global.changeLinkedProfile／removeLinkedProfile／containsLinkedProfile／getLinkedProfile の処理
        // （シリアルからコロンを除いて正規化する）をここへ移した。
        public void ChangeLinkedProfile(string serial, string profile)
        {
            string tempSerial = serial.Replace(":", string.Empty);
            _config.linkedProfiles[tempSerial] = profile;
        }

        public void RemoveLinkedProfile(string serial)
        {
            string tempSerial = serial.Replace(":", string.Empty);
            if (_config.linkedProfiles.ContainsKey(tempSerial))
            {
                _config.linkedProfiles.Remove(tempSerial);
            }
        }

        public bool SaveLinkedProfiles()
            => _linkedProfilesXml.SaveLinkedProfiles();

        // Phase6-Step10-4-5b: 名前変更後に、リンクプロファイルの旧名が残って「Selected Profile が空欄になる」不具合の是正
        public int ReplaceLinkedProfileName(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return 0;

            int replaced = 0;
            var linked = _config.linkedProfiles;
            if (linked != null)
            {
                foreach (string serial in linked.Keys.ToList())
                {
                    if (string.Equals(linked[serial], oldName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        linked[serial] = newName;
                        replaced++;
                    }
                }
            }

            string[] ui = _config.linkedProfileUI;
            if (ui != null)
            {
                for (int i = 0; i < ui.Length; i++)
                {
                    if (string.Equals(ui[i], oldName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        ui[i] = newName;
                        replaced++;
                    }
                }
            }

            if (replaced > 0)
            {
                _linkedProfilesXml.SaveLinkedProfiles();
            }

            return replaced;
        }

        // Phase6-Step10-4-5d: 名前変更後に、プロファイル切替のスペシャルアクションの切替先が、旧名のまま（なし扱い）になる不具合の是正
        public int ReplaceProfileSwitchTarget(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return 0;

            int replaced = 0;
            var actions = _config.actions;
            if (actions != null)
            {
                lock (actions)
                {
                    foreach (SpecialAction action in actions)
                    {
                        if (action != null && action.typeID == SpecialAction.ActionTypeId.Profile &&
                            string.Equals(action.details, oldName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            // 実行時は action.details を参照する（読み込み済みの定義へ、そのまま反映される）
                            action.details = newName;
                            replaced++;
                        }
                    }
                }
            }

            if (replaced > 0)
            {
                _actionsXml.SaveActions();
            }

            return replaced;
        }

        // ---- Phase5-Step13-5: デバイス別・割当済みSpecialAction名リスト ----
        public List<string>[] ProfileActions => _config.profileActions;
        public void CacheExtraProfileInfo(int deviceIndex) => _config.CacheExtraProfileInfo(deviceIndex);

        // ---- Phase5-Step13-6: プロファイルのカスタムフラグキャッシュ更新 ----
        public void CacheProfileCustomsFlags(int deviceIndex) => _config.CacheProfileCustomsFlags(deviceIndex);

        // ---- Phase5-Step13-7: 一時プロファイル読込（スワイプ操作等での即時切替用） ----
        // Phase7-Step3c W3: 旧 Global.LoadTempProfile の処理をここへ移した（xinputChange は従来どおり読み込みへ渡さない）。
        public bool LoadTempProfile(int deviceIndex, string profileName, bool launchProgram, ControlService control, bool xinputChange = true)
        {
            _config.loggedInvalidActions.Clear();
            bool result = _legacyProfileXml.LoadProfile(deviceIndex, launchProgram, control, Path.Combine(ProfilesPath, $"{profileName}.xml"));
            if (result)
            {
                _profileSettings.SetTempProfileName(deviceIndex, profileName);
                _profileSettings.SetUseTempProfile(deviceIndex, true);
                _profileSettings.SetTempProfileDistance(deviceIndex, profileName.ToLower().Contains("distance"));
            }

            return result;
        }

        // ---- Phase5-Step13-7 Tier4: プロファイル未定義アクションの警告ログ出力 ----
        public void EmitMissingActionLogsForDevice(int deviceIndex, bool forceEmit = false, string overrideProfileName = null)
            => _config.EmitMissingActionLogsForDevice(deviceIndex, forceEmit, overrideProfileName);

        // ---- Phase6-Step7-1: App.xaml.cs（Post-Host）の Global 直接参照解消 ----
        // Global.SaveAsProfile は戻り値を捨てる void のため、同じ実体（BackingStore.SaveAsProfile）を直接呼んで成否を返す。
        public bool SaveAsProfile(int deviceIndex, string profileName)
            => _legacyProfileXml.SaveAsProfile(deviceIndex, profileName);

        public bool LoadLinkedProfiles()
            => _linkedProfilesXml.LoadLinkedProfiles();

        // ---- Phase6-Step10-3b-1: アクションの名前変更・削除の、全プロファイルへの反映 ----
        public IReadOnlyList<string> ReplaceActionReference(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName) ||
                string.Equals(oldName, newName, StringComparison.Ordinal))
                return Array.Empty<string>();

            return UpdateActionReferences(oldName, newName);
        }

        public IReadOnlyList<string> RemoveActionReference(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Array.Empty<string>();

            return UpdateActionReferences(name, null);
        }

        public IReadOnlyList<string> GetAppliedProfilesUsingAction(string actionName)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(actionName))
                return result;

            List<string>[] actions = ProfileActions;
            string[] paths = ProfilePath;
            if (actions == null || paths == null)
                return result;

            int slotCount = Math.Min(ControlService.CURRENT_DS4_CONTROLLER_LIMIT, Math.Min(actions.Length, paths.Length));
            for (int i = 0; i < slotCount; i++)
            {
                if (actions[i] != null && !string.IsNullOrEmpty(paths[i]) &&
                    actions[i].Contains(actionName, StringComparer.Ordinal) &&
                    !result.Contains(paths[i], StringComparer.OrdinalIgnoreCase))
                {
                    result.Add(paths[i]);
                }
            }

            return result;
        }

        // newName が null のときは、oldName を取り除く。
        // XmlIoLock（同一 XML 排他ロック）の下で、プロファイルごとに読み込み → 置換 → 保存を行う。
        private IReadOnlyList<string> UpdateActionReferences(string oldName, string newName)
        {
            var changed = new List<string>();
            string[] files;
            try
            {
                if (!Directory.Exists(ProfilesPath))
                    return changed;
                files = Directory.GetFiles(ProfilesPath, "*.xml");
            }
            catch (Exception ex)
            {
                AppLogger.LogToGui($"[DI] ProfileRepository: cannot enumerate profiles for action reference update: {ex.Message}", true);
                return changed;
            }

            foreach (string file in files)
            {
                try
                {
                    lock (ProfileXmlStore.XmlIoLock)
                    {
                        var doc = new System.Xml.XmlDocument { PreserveWhitespace = true };
                        doc.Load(file);
                        System.Xml.XmlNode node = doc.SelectSingleNode("DS4Windows/ProfileActions");
                        if (node == null || string.IsNullOrEmpty(node.InnerText))
                            continue;

                        string[] names = node.InnerText.Split('/');
                        if (!names.Contains(oldName, StringComparer.Ordinal))
                            continue;

                        var result = new List<string>();
                        foreach (string n in names)
                        {
                            string value = string.Equals(n, oldName, StringComparison.Ordinal) ? newName : n;
                            if (value == null || result.Contains(value, StringComparer.Ordinal))
                                continue;
                            result.Add(value);
                        }

                        node.InnerText = string.Join("/", result);
                        doc.Save(file);
                        changed.Add(Path.GetFileNameWithoutExtension(file));
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.LogToGui($"[DI] ProfileRepository: failed to update action reference in '{Path.GetFileName(file)}': {ex.Message}", true);
                }
            }

            if (changed.Count > 0)
            {
                string action = newName == null ? $"removed '{oldName}'" : $"renamed '{oldName}' to '{newName}'";
                AppLogger.LogToGui($"[DI] Action reference {action} in {changed.Count} profile(s): {string.Join(", ", changed)}", false);
            }

            return changed;
        }

        // Phase7-Step3c W2: LoadBlankProfile は削除した（IProfilePresetLoader.LoadBlank に統合）。


        // ---- Phase6-Step2-2 (PR-2): リンクプロファイル解決 ----
        public bool ContainsLinkedProfile(string serial)
        {
            string tempSerial = serial.Replace(":", string.Empty);
            return _config.linkedProfiles.ContainsKey(tempSerial);
        }

        public string GetLinkedProfile(string serial)
        {
            string temp = string.Empty;
            string tempSerial = serial.Replace(":", string.Empty);
            if (_config.linkedProfiles.ContainsKey(tempSerial))
            {
                temp = _config.linkedProfiles[tempSerial];
            }

            return temp;
        }
    }
}