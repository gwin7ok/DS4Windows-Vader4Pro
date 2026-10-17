using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step5 W2: <c>Program.rootHub</c> を削除した結果の回帰ガード。
    /// - <c>Program</c> クラス（<c>Program.cs</c>）が再び作られない。本体のどのファイルも <c>Program.rootHub</c> を参照しない。
    /// - <c>App</c> が <c>ControlService</c> を volatile なフィールドで持ち、<c>ServiceRegistration</c> は常に新規生成する。
    /// - <c>Global.SaveControllerConfigs</c>／<c>LoadControllerConfigs</c> は再宣言されない。
    /// ソースが見つからない環境では、検査を行わず合格とする。判断の根拠は <c>Phase7-Step5-Plan.md</c> の Q1・Q2・Q8。
    /// </summary>
    public class Phase7Step5RootHubGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step5RootHubGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static string StripComments(string text)
        {
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        private string Read(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した: " + relativePath);
                return null;
            }

            return StripComments(File.ReadAllText(path));
        }

        [Fact]
        public void NoSourceFile_ReferencesProgramRootHub_AndProgramCsIsGone()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/DS4Control/Global.cs");
            if (anchor == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(anchor), "..", ".."));
            string sourceDir = Path.Combine(root, "DS4Windows");
            Assert.False(File.Exists(Path.Combine(sourceDir, "DS4Control", "Program.cs")), "Program.cs が再び作られている。");

            var offenders = new List<string>();
            foreach (string file in Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories))
            {
                string normalized = file.Replace('\\', '/');
                if (normalized.Contains("/obj/") || normalized.Contains("/bin/")) continue;

                string text = StripComments(File.ReadAllText(file));
                if (Regex.IsMatch(text, @"\bProgram\s*\.\s*rootHub\b") || Regex.IsMatch(text, @"\bclass\s+Program\b"))
                    offenders.Add(Path.GetRelativePath(sourceDir, file));
            }

            Assert.True(offenders.Count == 0, "Program.rootHub／Program クラスが残っている: " + string.Join(", ", offenders));
        }

        [Fact]
        public void App_HoldsTheControlServiceInAVolatileField()
        {
            string text = Read("DS4Windows/App.xaml.cs");
            if (text == null) return;

            Assert.Matches(@"private\s+volatile\s+DS4Windows\.ControlService\s+_controlService\s*;", text);
            Assert.Matches(@"_controlService\s*=\s*ResolvePostHostService<DS4Windows\.ControlService>\(\)", text);
        }

        [Fact]
        public void ServiceRegistration_AlwaysCreatesTheControlService()
        {
            string text = Read("DS4Windows/DI/ServiceRegistration.cs");
            if (text == null) return;

            Assert.DoesNotContain("rootHub", text);
            Assert.Matches(@"return\s+new\s+ControlService\(", text);
        }

        [Fact]
        public void GlobalControllerConfigWrappers_AreNotRedeclared()
        {
            string text = Read("DS4Windows/DS4Control/Global.cs");
            if (text == null) return;

            Assert.DoesNotMatch(@"static\s+bool\s+(Save|Load)ControllerConfigs\s*\(", text);
        }
    }
}
