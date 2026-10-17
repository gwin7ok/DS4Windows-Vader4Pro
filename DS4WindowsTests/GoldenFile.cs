using System;
using System.IO;
using System.Text;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10c W1（Q2＝A）: ゴールデンファイルの照合。ファイルが無いときは、現在の出力から生成して失敗する
    /// （ユーザーが切り出し前のコードで実行し、内容を確認してコミットする）。ファイルがあれば、改行の違いを除いて全文を比較する。
    /// ゴールデンの置き場所は <c>DS4WindowsTests/Golden/Phase7Step10c/</c>。ソースが見つからない環境では照合を省略する。
    /// </summary>
    internal static class GoldenFile
    {
        private const string Anchor = "DS4WindowsTests/Golden/Phase7Step10c/README.txt";

        public static string Normalize(string text)
        {
            if (text == null) return string.Empty;
            return text.Replace("\r\n", "\n").TrimEnd('\n', ' ', '\t') + "\n";
        }

        private static string FolderOrNull()
        {
            string anchor = SourceFileLocator.Find(Anchor);
            return anchor == null ? null : Path.GetDirectoryName(anchor);
        }

        /// <summary>ゴールデンの内容を返す。ファイルまたはソースが無ければ null。</summary>
        public static string ReadOrNull(string fileName)
        {
            string folder = FolderOrNull();
            if (folder == null) return null;
            string path = Path.Combine(folder, fileName);
            return File.Exists(path) ? Normalize(File.ReadAllText(path)) : null;
        }

        public static void AssertMatches(string fileName, string actual, ITestOutputHelper output)
        {
            string folder = FolderOrNull();
            if (folder == null)
            {
                output.WriteLine("ソースが見つからないため、ゴールデン照合を省略した: " + fileName);
                return;
            }

            string path = Path.Combine(folder, fileName);
            string normalized = Normalize(actual);
            if (!File.Exists(path))
            {
                File.WriteAllText(path, normalized, new UTF8Encoding(false));
                throw new XunitException("ゴールデンファイルを生成した: " + path +
                    " 。内容を確認してコミットし、再実行すること（切り出し前のコードで生成すること）。");
            }

            string expected = Normalize(File.ReadAllText(path));
            if (!string.Equals(expected, normalized, StringComparison.Ordinal))
            {
                string actualPath = path + ".actual";
                File.WriteAllText(actualPath, normalized, new UTF8Encoding(false));
                throw new XunitException("ゴールデンと一致しない: " + fileName + " 。実際の出力を " + actualPath +
                    " に書き出した（差分を確認すること。意図した変更でなければ、移動で挙動が変わっている）。" + FirstDifference(expected, normalized));
            }

            // 前回の失敗で残った .actual を片付ける
            string leftover = path + ".actual";
            if (File.Exists(leftover)) { try { File.Delete(leftover); } catch { } }
        }

        private static string FirstDifference(string expected, string actual)
        {
            string[] a = expected.Split('\n');
            string[] b = actual.Split('\n');
            int n = Math.Min(a.Length, b.Length);
            for (int i = 0; i < n; i++)
            {
                if (a[i] != b[i])
                    return $" 最初の差（{i + 1} 行目）: 期待「{Trunc(a[i])}」／実際「{Trunc(b[i])}」";
            }
            return $" 行数が違う（期待 {a.Length}／実際 {b.Length}）";
        }

        private static string Trunc(string s) => s.Length <= 160 ? s : s.Substring(0, 160) + "…";
    }
}
