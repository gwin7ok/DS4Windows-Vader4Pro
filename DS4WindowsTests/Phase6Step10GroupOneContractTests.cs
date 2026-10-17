using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-1-1: グループ1（起動・環境系）の View／ViewModel の Global 直接参照解消に先立って追加した契約が、
    /// いずれも <c>Global</c> と同じ値・同じ動作であること（薄い委譲）を検証する。
    /// - IEnvironmentService: IsWin10OrGreater／IsWin8OrGreater／IsFakerInputInstalled／RefreshViGEmBusInfo
    /// - IPathService: SetSaveLocation（Global.SaveWhere。AppDataPath の setter とは別物）／CreateEmptyProfilesFile（Global.SaveDefault）
    /// - IAppSettingsService: LastVersionChecked／SetCulture
    /// Global 状態を変更するテストは、変更を必ず元に戻す（Phase6-Status.md §6.4-1）。
    /// </summary>
    public class Phase6Step10GroupOneContractTests
    {
        // ---- IEnvironmentService ----

        [Fact]
        public void EnvironmentService_WindowsVersionChecks_MatchLegacyFormula()
        {
            // Phase7-Step4 W1: 旧 Global.IsWin10OrGreater／IsWin8OrGreater は EnvironmentService へ移した。旧実装と同じ式で照合する
            IEnvironmentService service = new EnvironmentService();
            var os = Environment.OSVersion.Version;
            Assert.Equal(os.Major >= 10, service.IsWin10OrGreater());
            Assert.Equal(os.Major > 6 || (os.Major == 6 && os.Minor >= 2), service.IsWin8OrGreater());
        }

        [Fact]
        public void EnvironmentService_IsFakerInputInstalled_IsStableAcrossInstances()
        {
            // Phase7-Step4 W1: 調べ物は EnvironmentService が持つ。どのインスタンスでも同じ結果を返す
            IEnvironmentService service = new EnvironmentService();
            Assert.Equal(new EnvironmentService().IsFakerInputInstalled(), service.IsFakerInputInstalled());
        }

        [Fact]
        public void EnvironmentService_IsHidHideInstalled_DoesNotChangeCachedState()
        {
            IEnvironmentService service = new EnvironmentService();
            bool cachedBefore = EnvironmentService.HidHideInstalledState;
            Assert.Equal(new EnvironmentService().IsHidHideInstalled(), service.IsHidHideInstalled());
            Assert.Equal(cachedBefore, EnvironmentService.HidHideInstalledState);
        }

        [Fact]
        public void EnvironmentService_RefreshViGEmBusInfo_SharesStateAcrossInstances()
        {
            // Phase7-Step4 W1: 状態は static。更新はどのインスタンスからでも、ほかのインスタンスに見える
            IEnvironmentService service = new EnvironmentService();
            service.RefreshViGEmBusInfo();
            Assert.Equal(new EnvironmentService().IsViGEmBusInstalled(), service.IsViGEmBusInstalled());
            Assert.Equal(EnvironmentService.ViGEmBusVersionState, service.ViGEmBusVersion);
        }

        // ---- IPathService ----

        [Fact]
        public void PathService_SetSaveLocation_UpdatesGlobalAppDataPath_AndAppDataPathReflectsIt()
        {
            IPathService service = new PathService();
            string original = Global.appdatapath;
            // Global.SaveWhere は appdatapath のほかに、設定ファイル 4 つのパス（BackingStore）も書き換える。
            // 他のテスト（ProfileRepositoryTests など）へ影響を残さないよう、4 つとも元の値へ戻す。
            var store = Global.store;
            string originalProfile = store.m_Profile;
            string originalActions = store.m_Actions;
            string originalLinkedProfiles = store.m_linkedProfiles;
            string originalControllerConfigs = store.m_controllerConfigs;
            string target = Path.Combine(Path.GetTempPath(), "Phase6Step10_SaveWhere_" + Guid.NewGuid().ToString("N"));
            try
            {
                service.SetSaveLocation(target);
                Assert.Equal(target, Global.appdatapath);
                Assert.Equal(target, service.AppDataPath);

                // 設定ファイルのパスも更新されること（AppDataPath の setter では更新されない）
                Assert.Equal(Path.Combine(target, "Profiles.xml"), store.m_Profile);
                Assert.Equal(Path.Combine(target, "Actions.xml"), store.m_Actions);
                Assert.Equal(Path.Combine(target, "LinkedProfiles.xml"), store.m_linkedProfiles);
                Assert.Equal(Path.Combine(target, "ControllerConfigs.xml"), store.m_controllerConfigs);
            }
            finally
            {
                Global.appdatapath = original;
                store.m_Profile = originalProfile;
                store.m_Actions = originalActions;
                store.m_linkedProfiles = originalLinkedProfiles;
                store.m_controllerConfigs = originalControllerConfigs;
            }
        }

        [Fact]
        public void PathService_CreateEmptyProfilesFile_CreatesSameXmlAsGlobalSaveDefault()
        {
            IPathService service = new PathService();
            string dir = Path.Combine(Path.GetTempPath(), "Phase6Step10_SaveDefault_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string viaService = Path.Combine(dir, "service.xml");
                string viaGlobal = Path.Combine(dir, "second.xml");

                // Phase7-Step4 W3: 旧 Global.SaveDefault は PathService.CreateEmptyProfilesFile へ移した。2 つのインスタンスが同じ XML を作る
                Assert.True(service.CreateEmptyProfilesFile(viaService));
                Assert.True(new PathService().CreateEmptyProfilesFile(viaGlobal));

                var a = new XmlDocument();
                var b = new XmlDocument();
                a.Load(viaService);
                b.Load(viaGlobal);
                Assert.Equal(b.DocumentElement.Name, a.DocumentElement.Name);
                Assert.Equal("Profile", a.DocumentElement.Name);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void PathService_CreateEmptyProfilesFile_ReturnsFalse_WhenDirectoryDoesNotExist()
        {
            IPathService service = new PathService();
            string path = Path.Combine(Path.GetTempPath(), "Phase6Step10_NoDir_" + Guid.NewGuid().ToString("N"), "Profiles.xml");
            Assert.False(service.CreateEmptyProfilesFile(path));
            Assert.False(new PathService().CreateEmptyProfilesFile(path));
        }

        // ---- IAppSettingsService ----

        [Fact]
        public void LastVersionChecked_SharesStateWithGlobal_UpdatesNum_AndNotifiesOnChange()
        {
            var service = TestRepositoryFactory.CreateAppSettingsService();
            string original = Global.LastVersionChecked ?? string.Empty;
            var changed = new System.Collections.Generic.List<string>();
            service.SettingChanged += (s, name) => changed.Add(name);
            try
            {
                string newValue = original == "9.8.7" ? "9.8.6" : "9.8.7";
                service.LastVersionChecked = newValue;

                Assert.Equal(newValue, Global.LastVersionChecked);
                Assert.Equal(newValue, service.LastVersionChecked);
                Assert.Equal(Global.store.lastVersionCheckedNum, service.LastVersionCheckedNum);
                Assert.Contains(nameof(IAppSettingsService.LastVersionChecked), changed);

                // 同じ値の再設定では通知しない
                changed.Clear();
                service.LastVersionChecked = newValue;
                Assert.Empty(changed);
            }
            finally
            {
                Global.store.lastVersionChecked = original;
            }
        }

        [Fact]
        public void SetCulture_AppliesToCurrentUiCulture_LikeGlobalSetCulture_AndDoesNotChangeUseLang()
        {
            IAppSettingsService service = TestRepositoryFactory.CreateAppSettingsService();
            CultureInfo originalThreadUi = Thread.CurrentThread.CurrentUICulture;
            CultureInfo originalDefaultUi = CultureInfo.DefaultThreadCurrentUICulture;
            string originalUseLang = Global.UseLang;
            try
            {
                service.SetCulture("ja");
                Assert.Equal("ja", Thread.CurrentThread.CurrentUICulture.Name);
                Assert.Equal("ja", CultureInfo.DefaultThreadCurrentUICulture.Name);
                Assert.Equal(originalUseLang, Global.UseLang);

                // 不正なカルチャ名でも例外にしない（Global.SetCulture と同じ。try/catch で握りつぶす）。
                // 注意: .NET は "xx-..." 形式の未知の名前を、例外にせず別のカルチャとして受け入れることがある。
                // そのため、設定後のカルチャ名の値は検証せず、例外が出ないことだけを確認する。
                var ex = Record.Exception(() => service.SetCulture("!invalid culture name!"));
                Assert.Null(ex);
            }
            finally
            {
                Thread.CurrentThread.CurrentUICulture = originalThreadUi;
                CultureInfo.DefaultThreadCurrentUICulture = originalDefaultUi;
            }
        }
    }
}
