using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step1 W1: <c>BackingStore</c> の呼び出し元 0 件のメソッドを削除した結果の回帰ガード。
    /// (1) 削除済みのメソッドが <c>BackingStore.cs</c> に再び現れないこと、
    /// (2) 削除せずに残すもの（<c>PostProcessLoad</c>。W3 で DTO 読込の経路へ接続する）が残っていること、を固定する。
    /// 削除の経緯と根拠は <c>Phase7-0-Survey.md</c> §1.1、<c>Phase7-Step1-Plan.md</c> §3 W1。
    /// ソースが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Phase7Step1DeadMethodGuardTests
    {
        private static readonly string[] RemovedFromBackingStore =
        {
            "SaveOld", "UsedSavedProfileString", "RebuildProfileActionDicts",
            "GetDS4Action", "GetDS4Extra", "GetDS4KeyType", "GetDS4STrigger",
            "GetGyroOutModeString", "GetGyroOutModeType", "UpdateDS4CLightbarMacro",
        };

        private static readonly string[] KeptInBackingStore =
        {
            "PostProcessLoad", "GetDS4CSetting", "UpdateDS4CSetting",
        };

        private readonly ITestOutputHelper _output;

        public Phase7Step1DeadMethodGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static string StripComments(string text)
        {
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        private static bool DeclaresMethod(string strippedSource, string name)
        {
            return Regex.IsMatch(strippedSource,
                @"\b(?:public|private|internal|protected)\s+(?:static\s+)?[\w<>\[\],.? ]+?\s+" + Regex.Escape(name) + @"\s*\(");
        }

        [Fact]
        public void RemovedMethods_DoNotReappearInBackingStore()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/BackingStore.cs");
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string text = StripComments(File.ReadAllText(path));
            List<string> reappeared = RemovedFromBackingStore.Where(n => DeclaresMethod(text, n)).ToList();
            Assert.True(reappeared.Count == 0,
                "削除済みのメソッドが BackingStore.cs に再び宣言されている: " + string.Join(", ", reappeared));
        }

        [Fact]
        public void KeptMethods_StillExistInBackingStore()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/BackingStore.cs");
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string text = StripComments(File.ReadAllText(path));
            List<string> missing = KeptInBackingStore.Where(n => !DeclaresMethod(text, n)).ToList();
            Assert.True(missing.Count == 0,
                "残すべきメソッドが BackingStore.cs から失われている: " + string.Join(", ", missing));
        }
    }
}
