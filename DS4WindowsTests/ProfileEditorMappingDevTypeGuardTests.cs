using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step6-3: `ProfileEditor.xaml.cs` の `Reload` と `RefreshEditorBindings`（プリセット適用後）が、
    /// プロファイル読み込み後の出力種別でマッピング一覧のボタン名表記を明示的に揃えている
    /// （`mappingListVM.UpdateMappingDevType(profileSettingsVM.ContType)` を呼ぶ）ことを固定する回帰ガード。
    /// `mappingListVM` はプロファイル読み込み前の出力種別で生成されるため、この呼び出しがないと、
    /// 出力種別コンボボックスの SelectionChanged が発生しない組み合わせでボタン名が食い違う（Phase6-Step6-Plan.md §0.1）。
    /// `ProfileEditor` は WPF の UserControl で単体駆動が困難なため、ソース走査で担保する。
    /// ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class ProfileEditorMappingDevTypeGuardTests
    {
        private static readonly string[] MethodMarkers =
        {
            "public void Reload(",
            "private void RefreshEditorBindings(",
        };

        private const string RequiredCall = "mappingListVM.UpdateMappingDevType(profileSettingsVM.ContType)";

        private readonly ITestOutputHelper _output;

        public ProfileEditorMappingDevTypeGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string ReadSourceWithoutComments()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (path == null)
            {
                _output.WriteLine("ProfileEditor.xaml.cs が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        /// <summary>宣言の先頭から、対応する閉じ波括弧までのメソッド全体を返す。</summary>
        private static string ExtractMethod(string text, string marker)
        {
            int start = text.IndexOf(marker, StringComparison.Ordinal);
            Assert.True(start >= 0, $"{marker} が見つからない（メソッド名・修飾子の変更？ガードの見直しが必要）");

            int open = text.IndexOf('{', start);
            Assert.True(open >= 0, $"{marker} の本体が見つからない");

            int depth = 0;
            for (int i = open; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}')
                {
                    depth--;
                    if (depth == 0) return text.Substring(start, i - start + 1);
                }
            }

            Assert.True(false, $"{marker} の閉じ波括弧が見つからない");
            return null;
        }

        [Fact]
        public void ReloadAndRefreshEditorBindings_UpdateMappingDevTypeAfterLoading()
        {
            string text = ReadSourceWithoutComments();
            if (text == null) return;

            foreach (string marker in MethodMarkers)
            {
                string body = ExtractMethod(text, marker);
                int updateLate = body.IndexOf("profileSettingsVM.UpdateLateProperties()", StringComparison.Ordinal);
                int devType = body.IndexOf(RequiredCall, StringComparison.Ordinal);

                Assert.True(devType >= 0, $"{marker} が {RequiredCall} を呼んでいない");
                Assert.True(updateLate >= 0 && devType > updateLate,
                    $"{marker} で {RequiredCall} が UpdateLateProperties() の後に呼ばれていない（読み込み後の出力種別を使うため）");
            }
        }
    }
}
