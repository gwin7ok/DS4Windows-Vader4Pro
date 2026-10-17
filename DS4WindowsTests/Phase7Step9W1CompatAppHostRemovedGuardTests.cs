using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step9 W1（Q1＝A）: 互換 <c>DS4Windows.AppHost</c>（<c>DI/Compat/AppHost.cs</c>）を削除したことの回帰ガード。
    /// 正本は <c>DS4WinWPF.AppHost</c> だけ。ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    public class Phase7Step9W1CompatAppHostRemovedGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step9W1CompatAppHostRemovedGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void NoAppHostType_ExistsInTheDS4WindowsNamespace()
        {
            var type = typeof(DS4Windows.Global).Assembly.GetType("DS4Windows.AppHost");
            Assert.Null(type);
            Assert.NotNull(typeof(DS4Windows.Global).Assembly.GetType("DS4WinWPF.AppHost"));
        }

        [Fact]
        public void TheCompatFile_AndFolder_DoNotExist_AndNoSourceDeclaresIt()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/DS4Control/Global.cs");
            if (anchor == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string root = Path.GetDirectoryName(Path.GetDirectoryName(anchor)); // DS4Windows プロジェクト
            Assert.False(File.Exists(Path.Combine(root, "DI", "Compat", "AppHost.cs")));

            var declaration = new Regex(@"\bclass\s+AppHost\b");
            var offenders = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(@"\obj\") && !f.Contains(@"\bin\"))
                .Where(f => declaration.IsMatch(File.ReadAllText(f)))
                .Select(f => f.Substring(root.Length).TrimStart('\\'))
                .ToArray();
            Assert.True(offenders.SequenceEqual(new[] { @"DI\AppHost.cs" }),
                "AppHost の宣言は DI/AppHost.cs の 1 つだけであること: " + string.Join(", ", offenders));
        }
    }
}
