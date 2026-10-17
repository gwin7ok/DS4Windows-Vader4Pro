using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace DS4WindowsTests
{
    /// <summary>
    /// ソースコードの静的検査（回帰ガード）を行うテストが、リポジトリ内のソースファイルを探すための共通ヘルパー。
    /// ソースが見つからない環境（ソースなしで実行される場合）では null を返す。
    /// </summary>
    internal static class SourceFileLocator
    {
        /// <param name="relativePath">リポジトリのルートからの相対パス（例: DS4Windows/DS4Control/ControlService.cs）。</param>
        public static string Find(string relativePath, [CallerFilePath] string callerFile = "")
        {
            var starts = new List<string>();
            if (!string.IsNullOrEmpty(callerFile) && Path.IsPathRooted(callerFile))
            {
                starts.Add(Path.GetDirectoryName(callerFile));
            }
            starts.Add(AppContext.BaseDirectory);

            foreach (string start in starts)
            {
                string dir = start;
                while (!string.IsNullOrEmpty(dir))
                {
                    string candidate = Path.Combine(dir, relativePath.Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                    dir = Path.GetDirectoryName(dir);
                }
            }

            return null;
        }
    }
}