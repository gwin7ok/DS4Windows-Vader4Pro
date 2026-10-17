using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DS4WinWPF.DS4Forms;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10a W3（Q1＝A）: 資源パスの定数（旧 <c>Global.RESOURCES_PREFIX</c>／<c>ASSEMBLY_RESOURCE_PREFIX</c>）を
    /// 画面側の <c>ResourcePaths</c> へ移したことの回帰ガード。値は従来と同じ文字列でなければならない。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    public class Phase7Step10aW3ResourcePrefixGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step10aW3ResourcePrefixGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TheConstants_HaveTheSameValuesAsBefore()
        {
            Assert.Equal("/DS4Windows;component/Resources", ResourcePaths.ResourcesPrefix);
            Assert.Equal("pack://application:,,,/DS4Windows;", ResourcePaths.AssemblyResourcePrefix);
        }

        [Fact]
        public void TheInstanceProperties_BuildTheSameUris()
        {
            var paths = new ResourcePaths();
            Assert.Equal("/DS4Windows;component/Resources/size.png", paths.SizePNG);
            Assert.Equal("/DS4Windows;component/Resources/DS4 Config.png", paths.DS4ConfigPNG);
            Assert.Equal("/DS4Windows;component/Resources/DS4 lightbar.png", paths.DS4LightbarPNG);
            Assert.Equal("/DS4Windows;component/Resources/DS4-Config_RS.png", paths.DS4ConfigRSPNG);
            Assert.Equal("/DS4Windows;component/Resources/rainbow.png", paths.RainbowPNG);
        }

        [Fact]
        public void TheIconTable_UsesTheSamePrefix()
        {
            Assert.Equal("/DS4Windows;component/Resources/DS4W.ico", DS4WinWPF.DS4Forms.TrayIconResources.iconChoiceResources[DS4Windows.TrayIconChoice.Default]);
            Assert.Equal("/DS4Windows;component/Resources/DS4W - White.ico", DS4WinWPF.DS4Forms.TrayIconResources.iconChoiceResources[DS4Windows.TrayIconChoice.White]);
        }

        [Fact]
        public void TheFormsFiles_DoNotReferenceTheGlobalPrefixes()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/DS4Control/Global.cs");
            if (anchor == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(anchor), "..", ".."));
            var pattern = new Regex(@"(?<![\w])(?:DS4Windows\.)?Global\.(RESOURCES_PREFIX|ASSEMBLY_RESOURCE_PREFIX)\b");
            var offenders = Directory.GetFiles(Path.Combine(root, "DS4Windows", "DS4Forms"), "*.cs", SearchOption.AllDirectories)
                .Concat(new[] { Path.Combine(root, "DS4Windows", "DS4Control", "Global.cs") })
                .Where(f => pattern.IsMatch(Regex.Replace(Regex.Replace(File.ReadAllText(f), @"/\*.*?\*/", string.Empty, RegexOptions.Singleline), @"//[^\r\n]*", string.Empty)))
                .Select(f => Path.GetFileName(f))
                .ToArray();
            Assert.True(offenders.Length == 0, "Global の資源パスの定数への参照が残っている: " + string.Join(", ", offenders));
        }
    }
}
