using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF;
using DS4WinWPF.DS4Control.DTOXml;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10b W4（Q4＝A・Q6＝A）: <c>ControlService.USING_MAX_CONTROLLERS</c>（静的シム）を削除し、
    /// <c>IEnvironmentService.UsingMaxControllers</c> の注入に切り替えたことと、孤立定数の整理の回帰ガード。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    public class Phase7Step10bW4UsingMaxControllersGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step10bW4UsingMaxControllersGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TheStaticShim_IsGone_ButTheSlotLimitShimRemains()
        {
            Assert.Null(typeof(ControlService).GetProperty("USING_MAX_CONTROLLERS", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.NotNull(typeof(ControlService).GetProperty("CURRENT_DS4_CONTROLLER_LIMIT", BindingFlags.Static | BindingFlags.Public));
        }

        [Fact]
        public void TheHolder_FollowsTheEnvironment()
        {
            var env = new EnvironmentService();
            var holder = new AutoProfileHolder(new PathService(), env);
            Assert.Equal(env.UsingMaxControllers, holder.UsingMaxControllers);
            Assert.Throws<System.ArgumentNullException>(() => new AutoProfileHolder(new PathService(), null));
        }

        [Fact]
        public void TheDto_WritesControllers4To8_OnlyWhenTheEnvironmentUsesMaxControllers()
        {
            var env = new EnvironmentService();
            var holder = new AutoProfileHolder(new PathService(), env);
            holder.AutoProfileColl.Clear();
            var entity = new AutoProfileEntity("C:\\test\\a.exe", "t");
            for (int i = 0; i < entity.ProfileNames.Length; i++) entity.ProfileNames[i] = "P" + (i + 1);
            holder.AutoProfileColl.Add(entity);

            var dto = new AutoProfilesDTO();
            dto.MapFrom(holder);

            var written = dto.programEntries.Single();
            Assert.Equal("P1", written.Controller1);
            Assert.Equal("P3", written.Controller3);
            if (env.UsingMaxControllers)
            {
                Assert.Equal("P4", written.Controller4);
                Assert.Equal("P8", written.Controller8);
            }
            else
            {
                Assert.Equal(string.Empty, written.Controller8);
            }
        }

        [Fact]
        public void TheViewModel_FollowsTheEnvironment()
        {
            DS4WinWPF.AppHost.CreateHost();
            var factory = DS4WinWPF.AppHost.GetService<IViewModelFactory>();
            var env = DS4WinWPF.AppHost.GetService<IEnvironmentService>();
            var vm = factory.CreateAutoProfilesViewModel(TestRepositoryFactory.CreateAutoProfileHolder(),
                new ProfileList(DS4WinWPF.AppHost.GetService<IPathService>()));
            Assert.Equal(env.UsingMaxControllers, vm.UsingExpandedControllers);
        }

        [Fact]
        public void ProgramItem_ShowsTheExpandedControllerRows_OnlyWhenTheCreatorSaysSo()
        {
            // 存在しないパスなら、アイコンの読み込みは行われない
            var expanded = new ProgramItem(@"C:\no\such\file.exe", true);
            var normal = new ProgramItem(@"C:\no\such\file.exe", false);
            Assert.Equal(System.Windows.Visibility.Visible, expanded.ExpandedControllersVisible);
            Assert.Equal(System.Windows.Visibility.Collapsed, normal.ExpandedControllersVisible);
        }

        [Fact]
        public void TheOrphanCurveConstants_AreGone_AndTheCheckEveryDefaultsAreConnected()
        {
            Assert.Equal(1, BackingStore.DEFAULT_CHECK_EVERY_VALUE);
            Assert.Equal(1, BackingStore.DEFAULT_CHECK_EVERY_UNIT);
            var fresh = new BackingStore();
            Assert.Equal(BackingStore.DEFAULT_CHECK_EVERY_VALUE, fresh.checkEveryValue);
            Assert.Equal(BackingStore.DEFAULT_CHECK_EVERY_UNIT, fresh.checkEveryUnit);

            foreach (string name in new[] { "DEFAULT_STICK_OUTPUT_CURVE", "DEFAULT_STICK_OUTPUT_CURVE_ID", "DEFAULT_SA_OUTPUT_CURVE", "DEFAULT_SA_OUTPUT_CURVE_ID" })
                Assert.Null(typeof(BackingStore).GetField(name));
        }

        [Fact]
        public void NoSource_ReferencesTheRemovedShim()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/DS4Control/Global.cs");
            if (anchor == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(anchor), "..", ".."));
            string Strip(string t) => Regex.Replace(Regex.Replace(t, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline), @"//[^\r\n]*", string.Empty);
            var offenders = Directory.GetFiles(Path.Combine(root, "DS4Windows"), "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(@"\obj\") && !f.Contains(@"\bin\"))
                .Where(f => Strip(File.ReadAllText(f)).Contains("USING_MAX_CONTROLLERS"))
                .Select(f => Path.GetFileName(f))
                .ToArray();
            Assert.True(offenders.Length == 0, "削除済みの USING_MAX_CONTROLLERS への参照が残っている: " + string.Join(", ", offenders));
        }
    }
}
