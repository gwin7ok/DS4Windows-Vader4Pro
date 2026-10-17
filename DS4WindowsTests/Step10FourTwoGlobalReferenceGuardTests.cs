using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4WindowsTests
{

    public class Step10FourTwoGlobalReferenceGuardTests
    {
        // ファイルごとに、使ってよい Global メンバー（薄い委譲・本体が Global にあるもの）
        private static readonly Dictionary<string, string[]> Allowed = new Dictionary<string, string[]>
        {
            // 10-4-2a
            // ApplyProfile: 実装本体が Global にある（Phase7 で Mapping の instance 化とあわせて移す。TODO 付き）
            ["DS4Windows/DS4Control/Services/ProfileApplicationService.cs"] = new[] { "ApplyProfile" },
            // 実行中アクション状態のクリアを含む Global 側の実装への薄い委譲
            ["DS4Windows/DS4Control/Services/SpecialActionRepository.cs"] =
                new[] { "CreateStdActions", "SaveAction", "RemoveAction", "SaveActions" },
            // 10-4-2b
            // 薄い委譲（IProfileRepository の契約の実体。Global 側に実装がある）と、読み込み前のクリア対象
            ["DS4Windows/DS4Control/Services/ProfileRepository.cs"] = new[]
            {
                "ProfilePath", "OlderProfilePath", "SelectedProfile", "LinkedProfileUI", "SelectedProfileChanged",
                "RaiseSelectedProfileChanged", "changeLinkedProfile", "removeLinkedProfile", "SaveLinkedProfiles",
                "ProfileActions", "CacheExtraProfileInfo", "CacheProfileCustomsFlags", "LoadTempProfile", "store",
                "LoadLinkedProfiles", "LoadBlankDevProfile", "containsLinkedProfile", "getLinkedProfile", "loggedInvalidActions",
            },
            ["DS4Windows/DS4Control/Services/ProfileActionProvider.cs"] = new string[0],
            ["DS4Windows/DS4Control/Profiles/AutoProfileHolder.cs"] = new[] { "MAX_DS4_CONTROLLER_COUNT" },
            ["DS4Windows/DS4Control/Services/AutoProfileService.cs"] = new[] { "autoProfileSwitchNotifyChoice", "CreateAutoProfiles" },
            // 10-4-2c: 薄い委譲（実装本体が Global にあるもの）と、不変の定数表
            // 10-4-3: 実行時配列の実体は Phase7-Step6 W1 で OutputSlotService へ移した。残るのは配列の長さの定数だけ
            ["DS4Windows/DS4Control/Services/OutputSlotService.cs"] = new[] { "TEST_PROFILE_ITEM_COUNT" },
            ["DS4Windows/DS4Control/Services/OutputSlotStore.cs"] = new string[0],
            ["DS4Windows/DS4Control/OutputSlotManager.cs"] = new string[0],
            ["DS4Windows/DS4Control/OutputSlotPersist.cs"] = new[] { "exeversion" },
            // 10-4-4: Actions/*（ServiceProviderHolder は D4-4 により Phase7/Phase8 へ温存。MAX_DS4_CONTROLLER_COUNT は定数）
            ["DS4Windows/Actions/MacroAction.cs"] = new string[0],
            ["DS4Windows/Actions/ProfileSwitchAction.cs"] = new string[0],
            ["DS4Windows/Actions/LaunchProcessAction.cs"] = new string[0],
            ["DS4Windows/Actions/MouseOutputAction.cs"] = new string[0],
            ["DS4Windows/Actions/DefaultActionFactory.cs"] = new string[0],
            ["DS4Windows/Actions/ActionFactory.cs"] = new string[0],
            ["DS4Windows/Actions/DefaultMacroPlayer.cs"] = new[] { "MAX_DS4_CONTROLLER_COUNT" },
            ["DS4Windows/Actions/DefaultActionManager.cs"] = new[] { "MAX_DS4_CONTROLLER_COUNT" },
            ["DS4Windows/Actions/ActionManager.cs"] = new[] { "MAX_DS4_CONTROLLER_COUNT" },
            ["DS4Windows/DS4Control/Services/ProfileSettingsService.cs"] =
                new[] { "RefreshActionAlias", "RefreshExtrasButtons", "defaultButtonMapping", "reverseX360ButtonMapping" },
        };

        private static string ReadWithoutComments(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null) return null;
            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        [Fact]
        public void ReplacedFiles_DoNotReferenceGlobalExceptRetainedMembers()
        {
            foreach (var entry in Allowed)
            {
                string text = ReadWithoutComments(entry.Key);
                if (text == null) continue;

                var unexpected = Regex.Matches(text, @"\bGlobal\.(\w+)")
                    .Cast<Match>().Select(m => m.Groups[1].Value)
                    .Where(n => !entry.Value.Contains(n)).Distinct().ToList();
                Assert.True(unexpected.Count == 0,
                    $"{entry.Key} に温存項目以外の Global 参照がある: {string.Join(", ", unexpected)}");

                Assert.DoesNotContain("AppHost", text);
                Assert.DoesNotContain("Program.rootHub", text);
            }
        }

        [Fact]
        public void SpecialActionRepository_RequiresStoreAndPathService()
        {
            Assert.Throws<ArgumentNullException>(() => new SpecialActionRepository(null, new PathService(), new SyntheticInputState(), TestRepositoryFactory.CreateActionsXml()));
            Assert.Throws<ArgumentNullException>(() => new SpecialActionRepository(new BackingStore(), null, new SyntheticInputState(), TestRepositoryFactory.CreateActionsXml()));
            Assert.Throws<ArgumentNullException>(() => new SpecialActionRepository(new BackingStore(), new PathService(), new SyntheticInputState(), null));
        }

        [Fact]
        public void ProfileRepository_ProfileActionProvider_AutoProfileHolder_RequireDependencies()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var config = new BackingStore();
            var store = TestRepositoryFactory.CreateProfileXmlStore(config);
            var linkedXml = TestRepositoryFactory.CreateLinkedProfilesXml(config);
            var actionsXml = TestRepositoryFactory.CreateActionsXml(config);
            var legacyXml = TestRepositoryFactory.CreateLegacyProfileXml(config);
            var path = new PathService();
            Func<ControlService> control = () => null;

            Assert.Throws<ArgumentNullException>(() => new ProfileRepository(null, settings, store, path, control, linkedXml, actionsXml, legacyXml));
            Assert.Throws<ArgumentNullException>(() => new ProfileXmlStore(null, new DS4Windows.Services.ControllerConfigsXmlSerializer(config), TestRepositoryFactory.CreateAppSettingsXml(config), TestRepositoryFactory.CreateLegacyProfileXml(config)));
            Assert.Throws<ArgumentNullException>(() => new ProfileXmlStore(config, null, TestRepositoryFactory.CreateAppSettingsXml(config), TestRepositoryFactory.CreateLegacyProfileXml(config)));
            Assert.Throws<ArgumentNullException>(() => new ProfileXmlStore(config, new DS4Windows.Services.ControllerConfigsXmlSerializer(config), null, TestRepositoryFactory.CreateLegacyProfileXml(config)));
            Assert.Throws<ArgumentNullException>(() => new ProfileXmlStore(config, new DS4Windows.Services.ControllerConfigsXmlSerializer(config), TestRepositoryFactory.CreateAppSettingsXml(config), null));
            Assert.Throws<ArgumentNullException>(() => new ProfileRepository(config, settings, store, path, null, linkedXml, actionsXml, legacyXml));
            Assert.Throws<ArgumentNullException>(() => new ProfileRepository(config, null, store, path, control, linkedXml, actionsXml, legacyXml));
            Assert.Throws<ArgumentNullException>(() => new ProfileRepository(config, settings, null, path, control, linkedXml, actionsXml, legacyXml));
            Assert.Throws<ArgumentNullException>(() => new ProfileRepository(config, settings, store, null, control, linkedXml, actionsXml, legacyXml));
            Assert.Throws<ArgumentNullException>(() => new ProfileRepository(config, settings, store, path, control, null, actionsXml, legacyXml));
            Assert.Throws<ArgumentNullException>(() => new ProfileRepository(config, settings, store, path, control, linkedXml, null, legacyXml));
            Assert.Throws<ArgumentNullException>(() => new ProfileRepository(config, settings, store, path, control, linkedXml, actionsXml, null));
            Assert.Throws<ArgumentNullException>(() => new ProfileActionProvider(null));
            Assert.Throws<ArgumentNullException>(() => new DS4WinWPF.AutoProfileHolder(null, new EnvironmentService()));
        }

        [Fact]
        public void ProfileSettingsService_RequiresAllDependencies()
        {
            var store = new BackingStore();
            var env = new EnvironmentService();
            Func<ControlService> control = () => null;

            Assert.Throws<ArgumentNullException>(() => new ProfileSettingsService(null, env, control));
            Assert.Throws<ArgumentNullException>(() => new ProfileSettingsService(store, null, control));
            Assert.Throws<ArgumentNullException>(() => new ProfileSettingsService(store, env, null));
        }

        [Fact]
        public void ProfileSettingsService_GetRumbleBoost_UsesInjectedControlProvider()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService(new BackingStore(), null, () => null);
            // コントローラーが無い場合も、注入された関数だけを参照して例外なく値を返す（Program.rootHub を参照しない）
            var ex = Record.Exception(() => settings.GetRumbleBoost(0));
            Assert.Null(ex);
        }

        [Fact]
        public void AutoProfileService_RequiresAllDependencies()
        {
            var path = new PathService();
            var holder = new DS4WinWPF.AutoProfileHolder(path, new EnvironmentService());
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var app = TestRepositoryFactory.CreateProfileApplicationService(settings,
                () => null,
                TestRepositoryFactory.CreateProfileRepository(settings), new EnvironmentService());
            var repo = TestRepositoryFactory.CreateProfileRepository(settings);
            var env = new EnvironmentService();
            var appSettings = TestRepositoryFactory.CreateAppSettingsService();
            Func<ControlService> control = () => null;
            IProcessInspector inspector = new NullProcessInspector();

            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(null, app, settings, inspector, env, appSettings, repo, control, Global.store));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, null, settings, inspector, env, appSettings, repo, control, Global.store));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, app, null, inspector, env, appSettings, repo, control, Global.store));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, app, settings, null, env, appSettings, repo, control, Global.store));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, app, settings, inspector, null, appSettings, repo, control, Global.store));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, app, settings, inspector, env, null, repo, control, Global.store));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, app, settings, inspector, env, appSettings, null, control, Global.store));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, app, settings, inspector, env, appSettings, repo, null, Global.store));
            Assert.Throws<ArgumentNullException>(() => new AutoProfileService(holder, app, settings, inspector, env, appSettings, repo, control, null));
        }

        [Fact]
        public void AutoProfileHolder_LoadsFromInjectedPath()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ds4w-step10-4-2b-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(dir);
            try
            {
                // 設定ファイルが無い場合は空で生成される（注入されたパスを参照し、Global.appdatapath を参照しない）
                var holder = new DS4WinWPF.AutoProfileHolder(new PathService(dir), new EnvironmentService());
                Assert.Empty(holder.AutoProfileColl);
            }
            finally
            {
                try { System.IO.Directory.Delete(dir, true); } catch { }
            }
        }

        [Fact]
        public void Mapping_FreshPressGuard_CoversAllSpecialActionTypes()
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/Mapping.cs");
            if (text == null) return;

            // 10-4-2e で追加した 6 型（既存の Program/Profile/GyroCalibrate 以外）が、全てガードを参照する
            int start = text.IndexOf("freshPressBlockedOtherTypes = false;", StringComparison.Ordinal);
            Assert.True(start >= 0, "その他の型のガードが見つからない");
            string block = text.Substring(start, 900);
            foreach (string type in new[] { "Macro", "Button", "Key", "DisconnectBT", "BatteryCheck", "SASteeringWheelEmulationCalibrate" })
            {
                Assert.Contains("ActionTypeId." + type, block);
            }
            Assert.Contains("RequiresFreshPressAfterReset", block);
        }

        [Fact]
        public void OutputSlot_Services_RequireAllDependencies()
        {
            var store = new OutputSlotStore(new PathService());
            Func<OutputSlotManager> manager = () => null;
            Func<ControlService> control = () => null;

            Assert.Throws<ArgumentNullException>(() => new OutputSlotStore(null));
            Assert.Throws<ArgumentNullException>(() => new OutputSlotManager(null));
            Assert.Throws<ArgumentNullException>(() => new OutputSlotService(null, store, control));
            Assert.Throws<ArgumentNullException>(() => new OutputSlotService(manager, null, control));
            Assert.Throws<ArgumentNullException>(() => new OutputSlotService(manager, store, null));
        }

        [Fact]
        public void OutputSlotStore_Save_UsesInjectedPath()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ds4w-step10-4-3-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(dir);
            try
            {
                var store = new OutputSlotStore(new PathService(dir));
                var manager = TestRepositoryFactory.CreateOutputSlotManager();

                Assert.True(store.Save(manager)); // 書き込み成功時は true を返す（Phase6-Step10-4-Plan.md §26 で是正）
                Assert.True(System.IO.File.Exists(System.IO.Path.Combine(dir, "OutputSlots.xml")));
            }
            finally
            {
                try { System.IO.Directory.Delete(dir, true); } catch { }
            }
        }

        [Fact]
        public void OutputSlotManager_ReadsViGEmBusVersionFromProvider()
        {
            int calls = 0;
            var manager = new OutputSlotManager(() => { calls++; return new Version(1, 0, 0, 0); });
            Assert.NotNull(manager);
            Assert.Equal(0, calls); // 生成時には読まない（割り当て時に都度読み取る）
        }

        [Fact]
        public void Actions_RequireAllDependencies()
        {
            var sa = new SpecialAction("a", "Cross", "Key", "1", 0);
            var player = new DefaultMacroPlayer(new MockVirtualKBM(), TestRepositoryFactory.MappingProvider);
            var launcher = new DefaultProcessLauncher();
            var switcher = new DefaultProfileSwitcher();

            Assert.Throws<ArgumentNullException>(() => new MacroAction(sa, (IMacroPlayer)null));
            Assert.Throws<ArgumentNullException>(() => new ProfileSwitchAction(sa, (IProfileSwitcher)null));
            Assert.Throws<ArgumentNullException>(() => new LaunchProcessAction(sa, null));
            Assert.Throws<ArgumentNullException>(() => new MouseOutputAction(sa, null));
            Assert.Throws<ArgumentNullException>(() => new DefaultMacroPlayer(null, TestRepositoryFactory.MappingProvider));
            Assert.Throws<ArgumentNullException>(() => new DefaultMacroPlayer(new MockVirtualKBM(), null));
            Assert.Throws<ArgumentNullException>(() => new DefaultActionManager(null, TestRepositoryFactory.MappingProvider));
            Assert.Throws<ArgumentNullException>(() => new DefaultActionManager(TestRepositoryFactory.CreateDefaultActionFactory(), null));
            Assert.Throws<ArgumentNullException>(() => new DefaultActionFactory(null, launcher, switcher));
            Assert.Throws<ArgumentNullException>(() => new DefaultActionFactory(player, null, switcher));
            Assert.Throws<ArgumentNullException>(() => new DefaultActionFactory(player, launcher, null));
        }

        [Fact]
        public void DefaultActionFactory_PassesInjectedDependencies_ToActions()
        {
            var factory = TestRepositoryFactory.CreateDefaultActionFactory();
            Assert.NotNull(factory.CreateFrom(new SpecialAction("k", "Cross", "Key", "1", 0) { typeID = SpecialAction.ActionTypeId.Key }, 0));
            Assert.IsType<MacroActionAdapter>(factory.CreateFrom(new SpecialAction("m", "Cross", "Macro", "1", 0) { typeID = SpecialAction.ActionTypeId.Macro }, 0));
            Assert.IsType<ProfileSwitchActionAdapter>(factory.CreateFrom(new SpecialAction("p", "Cross", "Profile", "x", 0) { typeID = SpecialAction.ActionTypeId.Profile }, 0));
            Assert.IsType<LaunchProcessActionAdapter>(factory.CreateFrom(new SpecialAction("l", "Cross", "Program", "x", 0) { typeID = SpecialAction.ActionTypeId.Program }, 0));
        }

        [Fact]
        public void MainWindow_And_ControlService_UseOnlyConstGlobalMembers()
        {
            var allowed = new Dictionary<string, string[]>
            {
                // 定数のみ（RESOURCES_PREFIX/TEST_PROFILE_INDEX/MAX_DS4_CONTROLLER_COUNT）
                ["DS4Windows/DS4Forms/MainWindow.xaml.cs"] = new[] { "RESOURCES_PREFIX", "TEST_PROFILE_INDEX" },
                ["DS4Windows/DS4Control/ControlService.cs"] = new[] { "MAX_DS4_CONTROLLER_COUNT", "TEST_PROFILE_INDEX" },
            };

            foreach (var entry in allowed)
            {
                string text = ReadWithoutComments(entry.Key);
                if (text == null) continue;

                var unexpected = Regex.Matches(text, @"Global.(w+)")
                    .Cast<Match>().Select(m => m.Groups[1].Value)
                    .Where(n => !entry.Value.Contains(n)).Distinct().ToList();
                Assert.True(unexpected.Count == 0,
                    $"{entry.Key} に温存項目以外の Global 参照がある: {string.Join(", ", unexpected)}");

                Assert.DoesNotContain("Program.rootHub", text);
            }
        }

        [Fact]
        public void ReplaceLinkedProfileName_ReturnsZero_ForBlankOrUnmatchedNames()
        {
            var repo = TestRepositoryFactory.CreateProfileRepository(TestRepositoryFactory.CreateProfileSettingsService());

            // 一致が無い場合は保存（ファイルへの書き込み）をしない
            Assert.Equal(0, repo.ReplaceLinkedProfileName(null, "b"));
            Assert.Equal(0, repo.ReplaceLinkedProfileName("a", " "));
            Assert.Equal(0, repo.ReplaceLinkedProfileName("no-such-profile-" + Guid.NewGuid().ToString("N"), "b"));
        }

        [Fact]
        public void MainWindow_Rename_UsesCommonSyncPath_AndReplacesLinkedProfiles()
        {
            string text = ReadWithoutComments("DS4Windows/DS4Forms/MainWindow.xaml.cs");
            if (text == null) return;

            // プロファイル一覧からの名前変更も、全スロットの名前更新・再適用の共通経路を通る
            Assert.Contains("SyncProfileListAndControllers(oldProfile: oldProfileName, newProfile: newProfileName)", text);
            // 共通経路の名前変更では、リンクプロファイルの旧名も置き換える
            Assert.Contains("profileRepo.ReplaceLinkedProfileName(oldProfile, newProfile)", text);
            // 自動プロファイルの旧名も置き換える
            Assert.Contains("Holder?.ReplaceProfileName(oldProfile, newProfile)", text);
        }

        [Fact]
        public void AutoProfileHolder_ReplaceProfileName_ReplacesAndPersists()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ds4w-step10-4-5c-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(dir);
            try
            {
                var holder = new DS4WinWPF.AutoProfileHolder(new PathService(dir), new EnvironmentService());
                var rule = new DS4WinWPF.AutoProfileEntity(@"C:\game.exe", "");
                rule.ProfileNames[0] = "OldName";
                rule.ProfileNames[1] = "Other";
                holder.AutoProfileColl.Add(rule);

                Assert.Equal(0, holder.ReplaceProfileName(" ", "x"));
                Assert.Equal(1, holder.ReplaceProfileName("oldname", "NewName"));
                Assert.Equal("NewName", rule.ProfileNames[0]);
                Assert.Equal("Other", rule.ProfileNames[1]);

                // 保存先は注入されたパス。読み直しても新名が残る
                var reloaded = new DS4WinWPF.AutoProfileHolder(new PathService(dir), new EnvironmentService());
                Assert.Single(reloaded.AutoProfileColl);
                Assert.Equal("NewName", reloaded.AutoProfileColl[0].ProfileNames[0]);
            }
            finally
            {
                try { System.IO.Directory.Delete(dir, true); } catch { }
            }
        }

        [Fact]
        public void OutputSlotService_PluginAndUnplug_AreRejectedWithoutControlService_AndRaiseNoEvent()
        {
            // ControlService が無い（ホストが無い）場合は、何もできないため false を返し、変更の通知も出さない
            var service = new OutputSlotService(() => TestRepositoryFactory.CreateOutputSlotManager(),
                new OutputSlotStore(new PathService()), () => null);
            int fired = 0;
            service.OutputSlotChanged += (s, e) => fired++;

            Assert.False(service.PluginSlot(0, OutContType.X360));
            Assert.False(service.UnplugSlot(0));
            Assert.False(service.PluginSlot(-1, OutContType.DS4));
            Assert.False(service.UnplugSlot(OutputSlotService.MAX_SLOTS));
            Assert.Equal(0, fired);
        }

        [Fact]
        public void OutputSlotOperations_GoThroughOutputSlotService_NotControlServiceDirectly()
        {
            foreach (string file in new[]
            {
                "DS4Windows/DS4Forms/ViewModels/CurrentOutDeviceViewModel.cs",
                "DS4Windows/DS4Forms/OutputSlotManagerControl.xaml.cs",
                "DS4Windows/DS4Forms/MainWindow.xaml.cs",
            })
            {
                string text = ReadWithoutComments(file);
                if (text == null) continue;
                Assert.DoesNotContain("AttachUnboundOutDev(", text);
                Assert.DoesNotContain("DetachUnboundOutDev(", text);
            }

            string vm = ReadWithoutComments("DS4Windows/DS4Forms/ViewModels/CurrentOutDeviceViewModel.cs");
            if (vm != null)
            {
                Assert.Contains("outputSlotService.PluginSlot(", vm);
                Assert.Contains("outputSlotService.UnplugSlot(", vm);
            }
        }

        [Fact]
        public void ProfileApplicationService_RequiresAllDependencies()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            Func<ControlService> control = () => null;
            var repo = TestRepositoryFactory.CreateProfileRepository(settings, null, null);
            var env = new EnvironmentService();

            Assert.Throws<ArgumentNullException>(() => TestRepositoryFactory.CreateProfileApplicationService(null, control, repo, env));
            // Phase7-Step10b W3: IDeviceStateService は ProfileApplicationService の依存ではなくなった
            Assert.Throws<ArgumentNullException>(() => TestRepositoryFactory.CreateProfileApplicationService(settings, null, repo, env));
            Assert.Throws<ArgumentNullException>(() => TestRepositoryFactory.CreateProfileApplicationService(settings, control, null, env));
            Assert.Throws<ArgumentNullException>(() => TestRepositoryFactory.CreateProfileApplicationService(settings, control, repo, null));
        }

        [Fact]
        public void ProfileApplicationService_SlotLimit_FollowsEnvironment_NotFixedFour()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var repo = TestRepositoryFactory.CreateProfileRepository(settings);
            var env = new EnvironmentService();
            var service = TestRepositoryFactory.CreateProfileApplicationService(settings, () => null, repo, env);
            int limit = env.ControllerSlotLimit;
            string[] older = repo.OlderProfilePath;
            string[] backup = (string[])older.Clone();
            try
            {
                // 上限の内側（最後のスロット）は受け入れられ、保留中の復帰先がクリアされる
                older[limit - 1] = "pending";
                service.ClearPendingRestore(limit - 1);
                Assert.Equal(string.Empty, older[limit - 1]);

                // 上限（範囲外）は拒否され、何も変更されない
                older[limit] = "pending";
                service.ClearPendingRestore(limit);
                Assert.Equal("pending", older[limit]);

                Assert.False(service.ApplyProfile(limit, "NoSuchProfile"));
                Assert.False(service.ApplyProfile(-1, "NoSuchProfile"));
            }
            finally
            {
                Array.Copy(backup, older, backup.Length);
            }
        }
    }
}
