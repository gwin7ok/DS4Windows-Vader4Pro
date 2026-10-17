using System;
using System.IO;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.DS4Control;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step9-1: 主要4大 ViewModel の Global 直接参照解消に先立って追加した契約が、
    /// いずれも <c>Global</c> と同じ値・同じ動作であること（薄い委譲）を検証する。
    /// - IAppSettingsService: UseCustomSteamFolder／CustomSteamFolder／FakeExeName／LogMaxArchiveFiles
    /// - IEnvironmentService: IsViGEmBusInstalled／IsRunningSupportedViGEmBus／IsUsingMinViGEm117333／AdminNeeded
    /// - IPathService: ProgramDirectory（Global.exedirpath。ExecutableDirectory とは別物）
    /// - IAppearanceSettingsService: BatteryChanged（Global.BatteryChanged への転送）
    /// - IProfileSettingsService: RefreshActionAlias（Global.RefreshActionAlias と同じ結果）
    /// Global 状態を変更するテストは、変更を必ず元に戻す（Phase6-Status.md §6.4-1）。
    /// </summary>
    public class Phase6Step9ContractExtensionTests
    {
        // ---- IAppSettingsService ----

        [Fact]
        public void UseCustomSteamFolder_SharesStateWithGlobal_AndNotifiesOnChange()
        {
            var service = TestRepositoryFactory.CreateAppSettingsService();
            bool original = Global.store.useCustomSteamFolder;
            var changed = new System.Collections.Generic.List<string>();
            service.SettingChanged += (s, name) => changed.Add(name);
            try
            {
                service.UseCustomSteamFolder = !original;
                Assert.Equal(!original, Global.store.useCustomSteamFolder);
                Assert.Equal(!original, service.UseCustomSteamFolder);
                Assert.Contains(nameof(IAppSettingsService.UseCustomSteamFolder), changed);

                // 同じ値の再設定では通知しない
                changed.Clear();
                service.UseCustomSteamFolder = !original;
                Assert.Empty(changed);
            }
            finally
            {
                Global.store.useCustomSteamFolder = original;
            }
        }

        [Fact]
        public void CustomSteamFolder_SharesStateWithGlobal()
        {
            var service = TestRepositoryFactory.CreateAppSettingsService();
            string original = Global.store.customSteamFolder;
            try
            {
                service.CustomSteamFolder = @"C:\Phase6Step9\Steam";
                Assert.Equal(@"C:\Phase6Step9\Steam", Global.store.customSteamFolder);
                Assert.Equal(Global.store.customSteamFolder, service.CustomSteamFolder);
            }
            finally
            {
                Global.store.customSteamFolder = original;
            }
        }

        [Fact]
        public void FakeExeName_ValidName_IsAppliedAndNotified()
        {
            var service = TestRepositoryFactory.CreateAppSettingsService();
            string original = Global.store.fakeExeFileName;
            var changed = new System.Collections.Generic.List<string>();
            service.SettingChanged += (s, name) => changed.Add(name);
            try
            {
                service.FakeExeName = "Phase6Step9Fake";
                Assert.Equal("Phase6Step9Fake", Global.store.fakeExeFileName);
                Assert.Contains(nameof(IAppSettingsService.FakeExeName), changed);
            }
            finally
            {
                Global.store.fakeExeFileName = original;
            }
        }

        [Fact]
        public void FakeExeName_InvalidFileNameChars_AreIgnoredLikeGlobal_AndNotNotified()
        {
            var service = TestRepositoryFactory.CreateAppSettingsService();
            string original = Global.store.fakeExeFileName;
            var changed = new System.Collections.Generic.List<string>();
            service.SettingChanged += (s, name) => changed.Add(name);
            try
            {
                service.FakeExeName = "Phase6Step9Valid";
                changed.Clear();

                // Global.store.fakeExeFileName の setter は、ファイル名に使えない文字を含む値を黙って無視する
                service.FakeExeName = "bad|name?";
                Assert.Equal("Phase6Step9Valid", Global.store.fakeExeFileName);
                Assert.Equal("Phase6Step9Valid", service.FakeExeName);
                Assert.Empty(changed);
            }
            finally
            {
                Global.store.fakeExeFileName = original;
            }
        }

        [Fact]
        public void LogMaxArchiveFiles_SharesStateWithGlobal()
        {
            var service = TestRepositoryFactory.CreateAppSettingsService();
            int original = Global.LogMaxArchiveFiles;
            try
            {
                int value = original + 7;
                service.LogMaxArchiveFiles = value;
                Assert.Equal(value, Global.LogMaxArchiveFiles);
                Assert.Equal(value, service.LogMaxArchiveFiles);
            }
            finally
            {
                Global.LogMaxArchiveFiles = original;
            }
        }

        // ---- IEnvironmentService ----

        [Fact]
        public void ViGEmProbes_AreConsistentWithTheCachedVersion()
        {
            // Phase7-Step4 W1: 旧 Global.IsViGEmBusInstalled／IsRunningSupportedViGEmBus／IsUsingMinViGEm117333 は EnvironmentService へ移した。
            // 旧実装と同じ式（最小バージョンとの比較）で照合する
            var service = new EnvironmentService();
            var version = EnvironmentService.ViGEmBusVersionState;

            Assert.Equal(service.IsViGEmBusInstalled() && new Version("1.16.112.0").CompareTo(version) <= 0,
                service.IsRunningSupportedViGEmBus());
            Assert.Equal(version.CompareTo(new Version("1.17.333.0")) >= 0, service.IsUsingMinViGEm117333());
        }

        [Fact]
        public void AdminNeeded_MatchesGlobal()
        {
            var service = new EnvironmentService();

            Assert.Equal(Global.AdminNeeded(), service.AdminNeeded());
        }

        // ---- IPathService ----

        [Fact]
        public void ProgramDirectory_IsGlobalExedirpath_WithoutTrailingSeparator()
        {
            var service = new PathService();

            Assert.Equal(Global.exedirpath, service.ProgramDirectory);
            Assert.False(service.ProgramDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()),
                "ProgramDirectory は Global.exedirpath と同じく末尾に区切りを持たない（AppContext.BaseDirectory とは別物）");
            Assert.Equal(Path.GetDirectoryName(service.ExecutablePath), service.ProgramDirectory);
        }

        // ---- IAppearanceSettingsService ----

        [Fact]
        public void BatteryChanged_Subscription_ReceivesGlobalInvokeWithSameSenderAndValue()
        {
            IAppearanceSettingsService service = TestRepositoryFactory.CreateAppearanceSettingsService();
            object receivedSender = null;
            byte receivedValue = 0;
            int fired = 0;
            EventHandler<byte> handler = (sender, value) =>
            {
                fired++;
                receivedSender = sender;
                receivedValue = value;
            };

            service.BatteryChanged += handler;
            try
            {
                service.InvokeBatteryChanged(37);

                Assert.Equal(1, fired);
                Assert.Same(typeof(Global), receivedSender);
                Assert.Equal((byte)37, receivedValue);
            }
            finally
            {
                service.BatteryChanged -= handler;
            }

            // 購読解除後は通知されない
            service.InvokeBatteryChanged(38);
            Assert.Equal(1, fired);
        }

        [Fact]
        public void BatteryChanged_Subscription_ReceivesGlobalInvokeDirectly()
        {
            IAppearanceSettingsService service = TestRepositoryFactory.CreateAppearanceSettingsService();
            int fired = 0;
            EventHandler<byte> handler = (sender, value) => fired++;

            service.BatteryChanged += handler;
            try
            {
                Global.InvokeBatteryChanged(50);
                Assert.Equal(1, fired);
            }
            finally
            {
                service.BatteryChanged -= handler;
            }
        }

        // ---- IProfileSettingsService ----

        [Fact]
        public void RefreshActionAlias_NonKeyAction_ResetsAliasToZero_LikeGlobal()
        {
            var service = TestRepositoryFactory.CreateProfileSettingsService();
            var viaService = new DS4ControlSettings(DS4Controls.Cross);
            var viaGlobal = new DS4ControlSettings(DS4Controls.Cross);
            viaService.action.actionAlias = 99;
            viaGlobal.action.actionAlias = 99;
            viaService.shiftAction.actionAlias = 77;
            viaGlobal.shiftAction.actionAlias = 77;

            service.RefreshActionAlias(viaService, false);
            service.RefreshActionAlias(viaService, true);
            Global.RefreshActionAlias(viaGlobal, false);
            Global.RefreshActionAlias(viaGlobal, true);

            Assert.Equal(0u, viaService.action.actionAlias);
            Assert.Equal(0u, viaService.shiftAction.actionAlias);
            Assert.Equal(viaGlobal.action.actionAlias, viaService.action.actionAlias);
            Assert.Equal(viaGlobal.shiftAction.actionAlias, viaService.shiftAction.actionAlias);
        }

        [Fact]
        public void RefreshActionAlias_KeyAction_UsesOutputKbmMapping_LikeGlobal()
        {
            var service = TestRepositoryFactory.CreateProfileSettingsService();
            // Phase7-Step8 W3: 旧 Global.outputKBMMapping は、Global.ProfileSettingsServiceInstance.OutputKBMMapping への転送プロパティだった
            var globalSettings = Global.ProfileSettingsServiceInstance;
            VirtualKBMMapping originalMapping = globalSettings.OutputKBMMapping;
            try
            {
                VirtualKBMMapping mapping = VirtualKBMFactory.GetMappingInstance();
                mapping.PopulateConstants();
                mapping.PopulateMappings();
                globalSettings.OutputKBMMapping = mapping;
                service.OutputKBMMapping = mapping;

                const int virtualKey = 0x41; // 'A'
                var viaService = new DS4ControlSettings(DS4Controls.Cross);
                viaService.actionType = DS4ControlSettings.ActionType.Key;
                viaService.action.actionKey = virtualKey;
                var viaGlobal = new DS4ControlSettings(DS4Controls.Cross);
                viaGlobal.actionType = DS4ControlSettings.ActionType.Key;
                viaGlobal.action.actionKey = virtualKey;

                service.RefreshActionAlias(viaService, false);
                Global.RefreshActionAlias(viaGlobal, false);

                Assert.Equal(mapping.GetRealEventKey((uint)virtualKey), viaService.action.actionAlias);
                Assert.Equal(viaGlobal.action.actionAlias, viaService.action.actionAlias);
            }
            finally
            {
                globalSettings.OutputKBMMapping = originalMapping;
            }
        }
    }
}
