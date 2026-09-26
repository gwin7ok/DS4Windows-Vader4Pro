using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step3-6a: `Mapping.cs` のうち、`ControlService ctrl`（または呼び出し元から渡された `IProfileSettingsService`）を
    /// 経由して設定を読む「グループ A」のメソッド群（Step3-6a の対象24件）が、`using static DS4Windows.Global;` 経由の
    /// 非修飾参照（または `Global.` 修飾参照）へ戻っていないことを固定する回帰ガード。
    /// `Mapping.cs` は Step3-7 まで `using static DS4Windows.Global;` を残すため、非修飾の Global メンバ呼び出しが
    /// 誤って再導入されてもコンパイルは通る。本テストはその混入を検出する。
    /// 対象外（このガードは検査しない）: グループ B（判定補助メソッド群）・グループ C（非同期マクロ経路）は Step3-6b／3-6c で扱う。
    /// `MapCustomAction` 内の `Global.ApplyProfile`（DI 未登録時のフォールバック、Step3-7 で温存判断）は検査対象のメンバーに含めない。
    /// ソースファイルが見つからない環境（ソースなしで実行される場合）では、検査を行わず合格とする。
    /// </summary>
    public class MappingControlPathGlobalReferenceGuardTests
    {
        /// <summary>検査対象メソッドの宣言（一意に特定できる先頭部分）。</summary>
        private static readonly string[] MethodMarkers =
        {
            "public static void MapCustom(",
            "private static void ProcessControlSettingAction(",
            "private static bool IfAxisIsNotModified(",
            "private static async void MapCustomAction(",
            "private static void ReleaseActionKeys(",
            "private static double getMouseMapping(",
            "protected static Int32 Scale360degreeGyroAxis(",
        };

        /// <summary>Step3-6a で `IProfileSettingsService`／`IProfileActionProvider` 経由へ置換済みの `Global` メンバ名。</summary>
        private static readonly string[] MigratedGlobalMembers =
        {
            "getProfileActionCount", "GetSASteeringWheelEmulationAxis",
            "ButtonMouseInfos", "outputKBMMapping", "reverseX360ButtonMapping",
            "GetDS4CSetting",
            "getLSDeadzone", "getRSDeadzone",
            "SAWheelFuzzValues", "getSXDeadzone", "WheelSmoothInfo", "getSXAntiDeadzone",
        };

        private readonly ITestOutputHelper _output;

        public MappingControlPathGlobalReferenceGuardTests(ITestOutputHelper output)
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
        public void ControlPathMethods_HaveNoReferenceToMigratedGlobalMembers()
        {
            string text = ReadSourceWithoutComments();
            if (text == null) return;

            foreach (string marker in MethodMarkers)
            {
                string body = ExtractMethod(text, marker);

                foreach (string member in MigratedGlobalMembers)
                {
                    // 非修飾（using static 経由）と Global. 修飾の両方を検出する。`ctrl.ProfileSettingsService.Xxx` のような他オブジェクト経由は対象外。
                    var bare = new Regex(@"(?<![\w.])" + Regex.Escape(member) + @"\b");
                    var qualified = new Regex(@"\bGlobal\." + Regex.Escape(member) + @"\b");

                    Assert.False(bare.IsMatch(body), $"{member} への非修飾参照（Global）が {marker} に残っている");
                    Assert.False(qualified.IsMatch(body), $"Global.{member} への修飾参照が {marker} に残っている");
                }
            }
        }

        [Theory]
        [InlineData("private static bool IfAxisIsNotModified(")]
        [InlineData("private static void ReleaseActionKeys(")]
        public void MethodsWithoutCtrl_TakeProfileSettingsAsParameter(string marker)
        {
            string text = ReadSourceWithoutComments();
            if (text == null) return;

            Assert.Contains("IProfileSettingsService settings", ExtractMethod(text, marker));
        }
    }
}