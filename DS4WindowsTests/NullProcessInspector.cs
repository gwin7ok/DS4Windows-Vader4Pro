using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-4-2: クラスタ 1（プロファイル系サービス）の Global／AppHost／Program.rootHub 直接参照解消の回帰ガード
    /// （<c>Phase6-Step10-4-Plan.md</c> §13）。10-4-2b／2c で、対象のファイルを追加していく。
    /// </summary>
    internal class NullProcessInspector : IProcessInspector
    {
        public bool IsProcessRunning(string exePath) => false;
        public bool GetForegroundProcessInfo(out string processPath, out string windowTitle)
        {
            processPath = string.Empty;
            windowTitle = string.Empty;
            return false;
        }
    }
}
