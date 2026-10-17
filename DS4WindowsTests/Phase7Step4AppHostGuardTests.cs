using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step4 W2: <c>AppHost</c> の整理と、起動・終了の手順（Q2・Q3・Q5）の回帰ガード。
    /// ホストは全テストが共有する static のため、実際に破棄はせず、ソースの構造で確かめる。
    /// - <c>CreateHost(string[])</c> と、未使用だった引数は削除済みで、再び宣言されない。
    /// - <c>-driverinstall</c> 分岐は、設定の読込の前に <c>CreateHost(config, parser)</c> を明示的に呼ぶ（<c>Global.Load</c> は削除済み）。
    /// - <c>CleanShutdown</c> は、コントローラーの停止の後・ログのフラッシュの前に <c>AppHost.Dispose()</c> を呼ぶ。
    /// - <c>Dispose</c> の後は、<c>GetService</c> が暗黙にホストを作り直さない。
    /// ソースが見つからない環境では、検査を行わず合格とする。判断の根拠は <c>Phase7-Step4-Plan.md</c>。
    /// </summary>
    public class Phase7Step4AppHostGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step4AppHostGuardTests(ITestOutputHelper output)
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

        private static string ExtractMethodBody(string text, string signaturePattern)
        {
            Match m = Regex.Match(text, signaturePattern);
            Assert.True(m.Success, "メソッドが見つからない: " + signaturePattern);
            int open = text.IndexOf('{', m.Index);
            int depth = 0;
            for (int i = open; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                        return text.Substring(open, i - open + 1);
                }
            }

            Assert.True(false, "メソッドの終わりが見つからない: " + signaturePattern);
            return null;
        }

        [Fact]
        public void AppHost_DoesNotDeclareRemovedOverloads()
        {
            string text = Read("DS4Windows/DI/AppHost.cs");
            if (text == null) return;

            Assert.DoesNotMatch(@"CreateHost\s*\(\s*string\[\]", text);
            Assert.DoesNotMatch(@"CreateHost\s*\(\s*IConfiguration\s+configuration\s*=\s*null\s*\)", text);
            Assert.Matches(@"public\s+static\s+IHost\s+CreateHost\s*\(\s*\)", text);
            Assert.Matches(@"public\s+static\s+IHost\s+CreateHost\s*\(\s*IConfiguration\s+configuration\s*,\s*DS4WinWPF\.ArgumentParser\s+parser\s*,", text);
        }

        [Fact]
        public void AppHost_DoesNotRecreateTheHostAfterDispose()
        {
            string text = Read("DS4Windows/DI/AppHost.cs");
            if (text == null) return;

            Assert.Contains("_disposed = true;", text);
            Assert.Equal(2, Regex.Matches(text, @"if\s*\(\s*_disposed\s*\)").Count);
            Assert.Equal(2, Regex.Matches(text, @"_disposed\s*=\s*false;").Count);
        }

        [Fact]
        public void DriverInstallBranch_CreatesTheHostExplicitlyBeforeLoadingSettings()
        {
            string text = Read("DS4Windows/App.xaml.cs");
            if (text == null) return;

            string body = ExtractMethodBody(text, @"void\s+CheckOptions\s*\(");
            int create = body.IndexOf("AppHost.CreateHost(", System.StringComparison.Ordinal);
            int load = body.IndexOf(".Load()", System.StringComparison.Ordinal);
            Assert.True(create >= 0, "-driverinstall 分岐に AppHost.CreateHost の明示的な呼び出しがない。");
            Assert.True(load > create, "設定の読込が、ホストの明示的な構築より前にある。");
            Assert.DoesNotContain("Global.Load", body);
        }

        [Fact]
        public void GlobalLoad_IsRemoved()
        {
            string text = Read("DS4Windows/DS4Control/Global.cs");
            if (text == null) return;

            Assert.DoesNotMatch(@"public\s+static\s+bool\s+Load\s*\(", text);
        }

        [Fact]
        public void CleanShutdown_DisposesTheHostAfterStoppingControllersAndBeforeFlushingLogs()
        {
            string text = Read("DS4Windows/App.xaml.cs");
            if (text == null) return;

            string body = ExtractMethodBody(text, @"void\s+CleanShutdown\s*\(");
            int stop = body.IndexOf("_controlService.ShutDown()", System.StringComparison.Ordinal);
            int clearRegistry = body.IndexOf("ClearControllersForDevice", System.StringComparison.Ordinal);
            int dispose = body.IndexOf("AppHost.Dispose()", System.StringComparison.Ordinal);
            int flush = body.IndexOf("LogManager.Flush()", System.StringComparison.Ordinal);

            Assert.True(stop >= 0 && clearRegistry >= 0 && dispose >= 0 && flush >= 0, "CleanShutdown の構造が想定と違う。");
            Assert.True(dispose > stop, "AppHost.Dispose が、コントローラーの停止より前にある（防壁 5 に反する）。");
            Assert.True(dispose > clearRegistry, "AppHost.Dispose が、レジストリの破棄より前にある。");
            Assert.True(dispose < flush, "AppHost.Dispose が、ログのフラッシュより後にある。");
        }

        [Fact]
        public void WelcomeDialog_NoLongerLoadsConfigItself()
        {
            string text = Read("DS4Windows/DS4Forms/WelcomeDialog.xaml.cs");
            if (text == null) return;

            Assert.DoesNotContain("loadConfig", text);
            Assert.DoesNotContain("Global.Load", text);
            Assert.DoesNotContain("FindConfigLocation", text);
        }
    }
}
