using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step3-4: `Mapping.SetCurveAndDeadzone` のスティック・トリガー系（Step3-4 の対象27件）が、
    /// `using static DS4Windows.Global;` 経由の非修飾参照（または `Global.` 修飾参照）へ戻っていないことを固定する回帰ガード。
    /// `Mapping.cs` は Step3-7 まで `using static DS4Windows.Global;` を残すため、非修飾の Global メンバ呼び出しが
    /// 誤って再導入されてもコンパイルは通る。本テストはその混入を検出する。
    /// ソースファイルが見つからない環境（ソースなしで実行される場合）では、検査を行わず合格とする。
    /// Step3-5 で残りのジャイロ系（SX/SZ）を引数渡しへ移す際に、対象メンバを本ガードへ追加する。
    /// </summary>
    public class MappingSetCurveAndDeadzoneGlobalReferenceGuardTests
    {
        private const string StartMarker = "public static DS4State SetCurveAndDeadzone(";
        private const string EndMarker = "public static DS4State ApplyStickCalibration(";

        /// <summary>Step3-4 で `IProfileSettingsService` 経由へ置換済みの `Global` メンバ名。</summary>
        private static readonly string[] MigratedGlobalMembers =
        {
            "getLSRotation", "getRSRotation",
            "GetLSAntiSnapbackInfo", "GetRSAntiSnapbackInfo",
            "GetLSDeadInfo", "GetRSDeadInfo",
            "GetL2ModInfo", "GetR2ModInfo",
            "getLSSens", "getRSSens", "getL2Sens", "getR2Sens",
            "GetSquareStickInfo",
            "getLsOutCurveMode", "getRsOutCurveMode", "getL2OutCurveMode", "getR2OutCurveMode",
            "lsOutBezierCurveObj", "rsOutBezierCurveObj", "l2OutBezierCurveObj", "r2OutBezierCurveObj",
        };

        private readonly ITestOutputHelper _output;

        public MappingSetCurveAndDeadzoneGlobalReferenceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>コメント（ブロック・行）を除いた `SetCurveAndDeadzone` 本体のテキストを返す。見つからなければ null。</summary>
        private string ReadMethodSourceWithoutComments()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/Mapping.cs");
            if (path == null)
            {
                _output.WriteLine("Mapping.cs が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            int start = text.IndexOf(StartMarker, StringComparison.Ordinal);
            int end = text.IndexOf(EndMarker, StringComparison.Ordinal);
            Assert.True(start >= 0, "SetCurveAndDeadzone が見つからない（メソッド名の変更？）");
            Assert.True(end > start, "ApplyStickCalibration が SetCurveAndDeadzone の後ろに見つからない（並び順の変更？）");

            string body = text.Substring(start, end - start);
            body = Regex.Replace(body, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            body = Regex.Replace(body, @"//[^\r\n]*", string.Empty);
            return body;
        }

        [Fact]
        public void SetCurveAndDeadzone_TakesProfileSettingsAsParameter()
        {
            string body = ReadMethodSourceWithoutComments();
            if (body == null) return;

            Assert.Contains("IProfileSettingsService settings", body);
        }

        [Fact]
        public void SetCurveAndDeadzone_HasNoReferenceToMigratedGlobalMembers()
        {
            string body = ReadMethodSourceWithoutComments();
            if (body == null) return;

            foreach (string member in MigratedGlobalMembers)
            {
                // 非修飾（using static 経由）と Global. 修飾の両方を検出する。`settings.Xxx` のような他オブジェクト経由は対象外。
                var bare = new Regex(@"(?<![\w.])" + Regex.Escape(member) + @"\b");
                var qualified = new Regex(@"\bGlobal\." + Regex.Escape(member) + @"\b");

                Assert.False(bare.IsMatch(body), $"{member} への非修飾参照（Global）が SetCurveAndDeadzone に残っている");
                Assert.False(qualified.IsMatch(body), $"Global.{member} への修飾参照が SetCurveAndDeadzone に残っている");
            }
        }
    }
}