﻿using System.Diagnostics;

namespace DS4Windows.Services
{
    /// <summary>
    /// IProcessInspector の既定実装。
    /// 既存の Global.LoadProfile（ScpUtil.cs）にあった procFound 判定ループを
    /// そのまま移設したもの（ロジック変更なし）。Phase 3 Step 3-6.
    /// </summary>
    public class DefaultProcessInspector : IProcessInspector
    {
        public bool IsProcessRunning(string exePath)
        {
            Process[] localAll = Process.GetProcesses();
            bool procFound = false;
            for (int procInd = 0, procsLen = localAll.Length; !procFound && procInd < procsLen; procInd++)
            {
                try
                {
                    string temp = localAll[procInd].MainModule.FileName;
                    if (temp == exePath)
                    {
                        procFound = true;
                    }
                }
                // Ignore any process for which this information
                // is not exposed
                catch { }
            }
            return procFound;
        }
    }
}
