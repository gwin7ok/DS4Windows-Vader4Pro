using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step3-5: `Mapping.ApplyStickCalibration`（ドリフト補正 8件）と `Mapping.Commit`（出力確定 24件）が、
    /// `using static DS4Windows.Global;` 経由の非修飾参照（または `Global.` 修飾参照）へ戻っていないことを固定する回帰ガード。
    /// `Mapping.cs` は Step3-7 まで `using static DS4Windows.Global;` を残すため、非修飾の Global メンバ呼び出しが
    /// 誤って再導入されてもコンパイルは通る。本テストはその混入を検出する。
    /// `Commit` は出力先の `IVirtualKBM` を実 DI ホストから解決しており、テストから実行すると実際に入力を送出してしまうため、
    /// 挙動テストの代わりに本ガード（ソース走査）と実機確認で担保する。
    /// ソースファイルが見つからない環境（ソースなしで実行される場合）では、検査を行わず合格とする。
    /// </summary>
    public class MappingCommitAndCalibrationGlobalReferenceGuardTests
    {
        private const string CalibrationStart = "public static DS4State ApplyStickCalibration(";
        private const string CalibrationEnd = "private static bool ShiftTrigger(";
        private const string CommitStart = "public static void Commit(";
        private const string CommitEnd = "public enum Click";

        /// <summary>Step3-5 で `IProfileSettingsService` 経由へ置換済みの `Global` メンバ名（ドリフト補正）。</summary>
        private static readonly string[] MigratedCalibrationMembers =
        {
            "RightStickDriftXAxis", "RightStickDriftYAxis",
            "LeftStickDriftXAxis", "LeftStickDriftYAxis",
        };

        /// <summary>Step3-5 で `IProfileSettingsService.OutputKBMMapping` 経由へ置換済みの `Global` メンバ名（出力確定）。</summary>
        private static readonly string[] MigratedCommitMembers =
        {
            "outputKBMMapping",
        };

        private readonly ITestOutputHelper _output;

        public MappingCommitAndCalibrationGlobalReferenceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>コメント（ブロック・行）を除いた、指定範囲のソーステキストを返す。ファイルが見つからなければ null。</summary>
        private string ReadRangeWithoutComments(string startMarker, string endMarker)
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/Mapping.cs");
            if (path == null)
            {
                _output.WriteLine("Mapping.cs が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            int start = text.IndexOf(startMarker, StringComparison.Ordinal);
            Assert.True(start >= 0, $"{startMarker} が見つからない（メソッド名・シグネチャの変更？）");
            int end = text.IndexOf(endMarker, start, StringComparison.Ordinal);
            Assert.True(end > start, $"{endMarker} が {startMarker} の後ろに見つからない（並び順の変更？）");

            string body = text.Substring(start, end - start);
            body = Regex.Replace(body, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            body = Regex.Replace(body, @"//[^\r\n]*", string.Empty);
            return body;
        }

        private static void AssertNoGlobalReference(string body, string[] members, string methodName)
        {
            foreach (string member in members)
            {
                // 非修飾（using static 経由）と Global. 修飾の両方を検出する。`settings.Xxx` のような他オブジェクト経由は対象外。
                var bare = new Regex(@"(?<![\w.])" + Regex.Escape(member) + @"\b");
                var qualified = new Regex(@"\bGlobal\." + Regex.Escape(member) + @"\b");

                Assert.False(bare.IsMatch(body), $"{member} への非修飾参照（Global）が {methodName} に残っている");
                Assert.False(qualified.IsMatch(body), $"Global.{member} への修飾参照が {methodName} に残っている");
            }
        }

        [Fact]
        public void ApplyStickCalibration_TakesProfileSettingsAsParameter()
        {
            // 検査範囲はシグネチャ行から始まるため、引数リストも含まれる。
            string body = ReadRangeWithoutComments(CalibrationStart, CalibrationEnd);
            if (body == null) return;

            Assert.Contains("IProfileSettingsService settings", body);
        }

        [Fact]
        public void ApplyStickCalibration_HasNoReferenceToMigratedGlobalMembers()
        {
            string body = ReadRangeWithoutComments(CalibrationStart, CalibrationEnd);
            if (body == null) return;

            AssertNoGlobalReference(body, MigratedCalibrationMembers, "ApplyStickCalibration");
        }

        [Fact]
        public void Commit_TakesProfileSettingsAsParameter()
        {
            string body = ReadRangeWithoutComments(CommitStart, CommitEnd);
            if (body == null) return;

            Assert.Contains("IProfileSettingsService settings", body);
        }

        [Fact]
        public void Commit_HasNoReferenceToMigratedGlobalMembers()
        {
            string body = ReadRangeWithoutComments(CommitStart, CommitEnd);
            if (body == null) return;

            AssertNoGlobalReference(body, MigratedCommitMembers, "Commit");
        }

        [Fact]
        public void Commit_ReadsOutputKbmMappingFromPassedSettings()
        {
            // 置換漏れの検出（Global 参照が残っていないことの裏返しとして、引数経由の参照が存在することも確認する）。
            string body = ReadRangeWithoutComments(CommitStart, CommitEnd);
            if (body == null) return;

            Assert.Contains("settings.OutputKBMMapping.", body);
        }
    }
}