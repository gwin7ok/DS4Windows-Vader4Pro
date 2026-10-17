﻿using System.Diagnostics;

namespace DS4Windows.Services
{
    /// <summary>
    /// IElevatedProcessLauncher の既定実装。
    /// 既存の ControlService.DS4Devices_RequestElevation にあった直接 Process.Start
    /// 実装をそのまま移設したもの（ロジック変更なし）。Phase 3 Step 3-5.
    /// </summary>
    public class DefaultElevatedProcessLauncher : IElevatedProcessLauncher
    {
        public int? RelaunchElevated(string arguments, int timeoutMs = 30000)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(Global.exelocation);
            startInfo.Verb = "runas";
            startInfo.Arguments = arguments;
            startInfo.UseShellExecute = true;

            try
            {
                Process child = Process.Start(startInfo);
                int? result = null;
                if (!child.WaitForExit(timeoutMs))
                {
                    child.Kill();
                }
                else
                {
                    result = child.ExitCode;
                }
                child.Dispose();
                return result;
            }
            catch
            {
                return null;
            }
        }
    }
}
