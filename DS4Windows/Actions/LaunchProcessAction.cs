using System;
using System.Diagnostics;

namespace DS4Windows.Actions
{
    /// <summary>
    /// LaunchProcessAction（C5 — Phase 1）
    /// specActionLaunchProc（外部プログラム起動）の抽象化
    /// §2.1修正版準拠: 古い方式（直接 Process.Start）をフォールバックとして残す
    /// ログ出力（AppLogger.LogTrace / LogDebug）は維持（削除・新設しない）
    /// </summary>
    public class LaunchProcessAction : IOutputAction
    {
        private readonly SpecialAction sa;

        public LaunchProcessAction(SpecialAction sa)
        {
            this.sa = sa;
        }

        public string Id => sa?.name ?? "LaunchProcess";

        public void Execute(IOutputContext ctx)
        {
            try
            {
                if (sa == null) return;

                string processPath = sa.details; // SpecialAction.details に起動パスを格納

                try { AppLogger.LogTrace($"LaunchProcessAction.Execute: id={Id} device={ctx?.Device} path={processPath}"); } catch { }

                // 新経路（DI経由の IProcessLauncher）を優先
                bool launchedViaDI = false;
                try
                {
                    var sp = DS4Windows.DI.ServiceProviderHolder.Provider;
                    if (sp != null)
                    {
                        var launcher = sp.GetService(typeof(IProcessLauncher)) as IProcessLauncher;
                        if (launcher != null)
                        {
                            launcher.Launch(processPath);
                            launchedViaDI = true;
                        }
                    }
                }
                catch { launchedViaDI = false; }

                // フォールバック（古い方式を残す — §2.1修正版）
                if (!launchedViaDI)
                {
                    try { AppLogger.LogTrace($"LaunchProcessAction.Execute (fallback): id={Id} device={ctx?.Device} path={processPath}"); } catch { }
                    Process.Start(processPath);
                }
            }
            catch (Exception ex)
            {
                try { AppLogger.LogDebug($"LaunchProcessAction.Execute error: id={Id} error={ex.Message}"); } catch { }
            }
        }

        public void Stop(IOutputContext ctx)
        {
            // プロセス起動は一方向操作のため Stop は空（ログのみ維持）
            try { AppLogger.LogTrace($"LaunchProcessAction.Stop: id={Id} device={ctx?.Device}"); } catch { }
        }
    }
}
