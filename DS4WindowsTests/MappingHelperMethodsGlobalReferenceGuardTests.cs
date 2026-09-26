using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step3-6b（決定 P2）: `Mapping.cs` のうち、`ControlService ctrl` を持たない「判定補助メソッド群」（グループ B、12件）が、
    /// `using static DS4Windows.Global;` 経由の非修飾参照（または `Global.` 修飾参照）へ戻っていないことを固定する回帰ガード。
    /// グループ B は呼び出し元が約50箇所と多く `ctrl` を持たないため、引数渡しは行わず、`Mapping` の静的 `profileSettings` から読む
    /// （Phase7 の instance 化で解消する。`Phase6-Step3-Plan.md` §3.4.1）。
    /// `Mapping.cs` は Step3-7 まで `using static DS4Windows.Global;` を残すため、Global メンバ呼び出しが誤って再導入されても
    /// コンパイルは通る。本テストはその混入を検出する。
    /// ソースファイルが見つからない環境（ソースなしで実行される場合）では、検査を行わず合格とする。
    /// </summary>
    public class MappingHelperMethodsGlobalReferenceGuardTests
    {
        /// <summary>検査対象メソッドの宣言（一意に特定できる先頭部分）。</summary>
        private static readonly string[] MethodMarkers =
        {
            "private static byte GetByteMapping(",
            "public static bool GetBoolMappingExternal(",
            "private static bool GetBoolMapping(",
            "private static bool getBoolSpecialActionMapping(",
            "private static bool GetBoolActionMapping(",
            "private static byte GetXYAxisMapping(",
            "private static void GetMouseWheelMapping(",
        };

        /// <summary>Step3-6b で `profileSettings` 経由へ置換済みの `Global` メンバ名。</summary>
        private static readonly string[] MigratedGlobalMembers =
        {
            "IsUsingSAForControls", "SXSens", "SZSens", "outputKBMMapping",
        };

        private readonly ITestOutputHelper _output;

        public MappingHelperMethodsGlobalReferenceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>コメント（ブロック・行）を除いた Mapping.cs 全体のテキスト。見つからなければ null。</summary>
        private string ReadSourceWithoutComments()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/Mapping.cs");
            if (path == null)
            {
                _output.WriteLine("Mapping.cs が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        /// <summary>宣言の先頭から、対応する閉じ波括弧までのメソッド全体（シグネチャを含む）を返す。</summary>
        private static string ExtractMethod(string text, string marker)
        {
            int start = text.IndexOf(marker, StringComparison.Ordinal);
            Assert.True(start >= 0, $"{marker} が見つからない（メソッド名・修飾子の変更？）");
            Assert.True(text.IndexOf(marker, start + marker.Length, StringComparison.Ordinal) < 0,
                $"{marker} が複数見つかった（オーバーロードの追加？ガードの見直しが必要）");

            int open = text.IndexOf('{', start);
            Assert.True(open > start, $"{marker} の本体が見つからない");

            int depth = 0;
            for (int i = open; i < text.Length; i++)
            {
                if (text[i] == '{')
                {
                    depth++;
                }
                else if (text[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return text.Substring(start, i - start + 1);
                    }
                }
            }

            Assert.True(false, $"{marker} の閉じ波括弧が見つからない");
            return null;
        }

        [Fact]
        public void HelperMethods_HaveNoReferenceToMigratedGlobalMembers()
        {
            string text = ReadSourceWithoutComments();
            if (text == null) return;

            foreach (string marker in MethodMarkers)
            {
                string body = ExtractMethod(text, marker);

                foreach (string member in MigratedGlobalMembers)
                {
                    // 非修飾（using static 経由）と Global. 修飾の両方を検出する。`profileSettings.Xxx` のような他オブジェクト経由は対象外。
                    var bare = new Regex(@"(?<![\w.])" + Regex.Escape(member) + @"\b");
                    var qualified = new Regex(@"\bGlobal\." + Regex.Escape(member) + @"\b");

                    Assert.False(bare.IsMatch(body), $"{member} への非修飾参照（Global）が {marker} に残っている");
                    Assert.False(qualified.IsMatch(body), $"Global.{member} への修飾参照が {marker} に残っている");
                }
            }
        }

        [Fact]
        public void IsUsingGyroForControls_ReadsGyroOutputModeFromStaticProfileSettings()
        {
            string text = ReadSourceWithoutComments();
            if (text == null) return;

            string body = ExtractMethod(text, "private static bool IsUsingGyroForControls(");

            Assert.Contains("profileSettings.GyroOutputMode[device] == GyroOutMode.Controls", body);
        }
    }
}