using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step5 W3 以降: Service Locator の解消（Q4〜Q9）の回帰ガード。Wave ごとに追記する。
    /// - W3: 出力スロットの契約（<c>LoadOutputSlots</c>／<c>SaveOutputSlots</c>）の接続、<c>Global.ProfileXmlStoreInstance</c> と
    ///   <c>Global.NotificationServiceInstance</c> の削除（<c>AppLogger.NotificationService</c> への置き換え）。
    /// ソースが見つからない環境では、検査を行わず合格とする。判断の根拠は <c>Phase7-Step5-Plan.md</c>。
    /// </summary>
    public class Phase7Step5ServiceLocatorGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step5ServiceLocatorGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string Read(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した: " + relativePath);
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            return Regex.Replace(text, @"//[^\r\n]*", string.Empty);
        }

        [Fact]
        public void OutputSlotPersistence_GoesThroughTheContract()
        {
            string app = Read("DS4Windows/App.xaml.cs");
            string control = Read("DS4Windows/DS4Control/ControlService.cs");
            if (app == null || control == null) return;

            Assert.Contains("_outputSlotService.LoadOutputSlots()", app);
            Assert.DoesNotContain("LoadPermanentSlotsConfig", app);
            Assert.DoesNotContain("LoadPermanentSlotsConfig", control);
            Assert.Contains("_outputSlotServiceFactory().SaveOutputSlots()", control);
            Assert.DoesNotContain("OutputSlotPersist.", control);
        }

        [Fact]
        public void GlobalInstances_AreRemoved_ExceptTheProfileSettingsOne()
        {
            string global = Read("DS4Windows/DS4Control/Global.cs");
            if (global == null) return;

            Assert.DoesNotContain("ProfileXmlStoreInstance", global);
            Assert.DoesNotContain("NotificationServiceInstance", global);
            Assert.Contains("ProfileSettingsServiceInstance", global);
        }

        [Fact]
        public void ControlServiceAndBackingStore_ReceiveTheirCollaborators()
        {
            string control = Read("DS4Windows/DS4Control/ControlService.cs");
            string store = Read("DS4Windows/DS4Control/BackingStore.cs");
            string registration = Read("DS4Windows/DI/ServiceRegistration.cs");
            if (control == null || store == null || registration == null) return;

            Assert.DoesNotContain("AppHost", control);
            Assert.DoesNotContain("AppHost", store);
            Assert.Contains("Services.IElevatedProcessLauncher elevatedProcessLauncher", control);
            Assert.Contains("sp.GetRequiredService<IElevatedProcessLauncher>()", registration);
            Assert.Contains("store.ProcessInspector = sp.GetRequiredService<IProcessInspector>();", registration);
        }

        [Fact]
        public void App_ResolvesServicesOnlyThroughTheSingleEntryPoint()
        {
            string app = Read("DS4Windows/App.xaml.cs");
            if (app == null) return;

            // AppHost.GetService は ResolvePostHostService の 1 件だけ。ServiceProviderHolder も使わない
            Assert.Single(Regex.Matches(app, @"AppHost\.GetService"));
            Assert.DoesNotContain("ServiceProviderHolder", app);
            Assert.Contains("_logSettingsApplierProxy.Attach(logHolder)", app);
        }

        [Fact]
        public void MainWindowAndProfileEditor_DoNotUseTheLocator_AndAreCreatedThroughFactories()
        {
            string main = Read("DS4Windows/DS4Forms/MainWindow.xaml.cs");
            string editor = Read("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            string app = Read("DS4Windows/App.xaml.cs");
            string registration = Read("DS4Windows/DI/ServiceRegistration.cs");
            if (main == null || editor == null || app == null || registration == null) return;

            Assert.DoesNotContain("AppHost", main);
            Assert.DoesNotContain("AppHost", editor);
            Assert.DoesNotContain("new MainWindow(", app);
            Assert.DoesNotContain("new DS4Forms.MainWindow(", app);
            Assert.Contains("ResolvePostHostService<IMainWindowFactory>().Create(parser)", app);
            Assert.Contains("services.AddSingleton<IMainWindowFactory, MainWindowFactory>();", registration);
        }

        [Fact]
        public void AppLogger_UsesItsOwnNotificationService_SetByTheApp()
        {
            string logger = Read("DS4Windows/DS4Control/AppLogger.cs");
            string app = Read("DS4Windows/App.xaml.cs");
            if (logger == null || app == null) return;

            Assert.DoesNotContain("NotificationServiceInstance", logger);
            Assert.Matches(@"public\s+static\s+DS4Windows\.DI\.INotificationService\s+NotificationService", logger);
            Assert.Matches(@"AppLogger\.NotificationService\s*=\s*ResolvePostHostService<INotificationService>\(\)", app);
        }
    }
}
