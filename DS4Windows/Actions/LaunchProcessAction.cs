using System;
using System.Diagnostics;
using System.IO;

namespace DS4Windows.Actions
{
    /// <summary>
    /// LaunchProcessAction（C5 — Phase 1）
    /// specActionLaunchProc（外部プログラム起動）の抽象化
    /// §2.1修正版準拠: 古い方式（直接 Process.Start）をフォールバックとして残す
    /// ログ出力（AppLogger.LogTrace / LogDebug）は維持（削除・新設しない）
    ///
    /// 元実装（Mapping.cs の SpecialAction.ActionTypeId.Program ブロック）が持っていた
    /// 3つの起動経路をすべて再現する（§2.2 No Feature Drop 準拠）:
    ///   1) action.extra に "$hidden" 修飾子がある場合:
    ///      - details の拡張子が .bat/.cmd なら COMSPEC 経由（/C "details" cmdArgs）で起動
    ///      - それ以外は details を直接 FileName にし、cmdArgs を Arguments に設定
    ///      - いずれも WindowStyle=Hidden, CreateNoWindow=true, UseShellExecute=true
    ///   2) action.extra があるが "$hidden" 修飾子なし: details を FileName, extra を Arguments に設定
    ///   3) action.extra が空: details のみを FileName に設定（Arguments なし）
    /// いずれも UseShellExecute=true。
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

                string details = sa.details;
                string extra = sa.extra;

                try { AppLogger.LogTrace($"LaunchProcessAction.Execute: id={Id} device={ctx?.Device} details={details} extra={extra}"); } catch { }

                // 元の specActionLaunchProc ブロック（Mapping.cs）と同一のロジックで
                // FileName / Arguments / hidden を確定する（振る舞い変更なし）。
                string fileName;
                string arguments = null;
                bool hidden = false;

                if (!string.IsNullOrEmpty(extra))
                {
                    int pos = extra.IndexOf("$hidden", StringComparison.OrdinalIgnoreCase);
                    if (pos >= 0)
                    {
                        // $hidden 修飾子を除去（先頭から見つかった最初の1箇所、7文字分）
                        string cmdArgs = extra.Remove(pos, 7);
                        string cmdExt = Path.GetExtension(details)?.ToLower();

                        if (cmdExt == ".bat" || cmdExt == ".cmd")
                        {
                            // バッチスクリプトは既定のコマンドシェル（COMSPEC）経由で起動
                            fileName = Environment.GetEnvironmentVariable("COMSPEC");
                            arguments = "/C \"" + details + "\" " + cmdArgs;
                        }
                        else
                        {
                            // 通常の実行ファイル（details）+ 任意のコマンドライン引数（cmdArgs）
                            fileName = details;
                            arguments = cmdArgs;
                        }

                        hidden = true;
                    }
                    else
                    {
                        // $hidden 修飾子なし。extra をそのまま Arguments とする
                        fileName = details;
                        arguments = extra;
                    }
                }
                else
                {
                    // 引数修飾子なし。既定の Windows 設定で子プロセスを起動
                    fileName = details;
                }

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
                            launcher.Launch(fileName, arguments, true, hidden);
                            launchedViaDI = true;
                        }
                    }
                }
                catch { launchedViaDI = false; }

                // フォールバック（古い方式を残す — §2.1修正版）。
                // 元の specActionLaunchProc / temp Process 呼び出しと完全に等価な StartInfo を構築する。
                if (!launchedViaDI)
                {
                    try { AppLogger.LogTrace($"LaunchProcessAction.Execute (fallback): id={Id} device={ctx?.Device} fileName={fileName} arguments={arguments} hidden={hidden}"); } catch { }

                    using (Process proc = new Process())
                    {
                        proc.StartInfo.FileName = fileName;
                        if (arguments != null) proc.StartInfo.Arguments = arguments;
                        proc.StartInfo.UseShellExecute = true;
                        if (hidden)
                        {
                            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                            proc.StartInfo.CreateNoWindow = true;
                        }
                        proc.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                try { AppLogger.LogDebug($"LaunchProcessAction.Execute error: id={Id} error={ex.Message}"); } catch { }
            }
        }

        public void Stop(IOutputContext ctx)
        {
            // プロセス起動は一方向操作のため Stop は空（ログのみ維持）。
            // 元の Mapping.cs でも SpecialAction.ActionTypeId.Program は release/untrigger を処理していない
            // （§2.2 No Feature Drop: 元コードに存在しない挙動を新設しない）。
            try { AppLogger.LogTrace($"LaunchProcessAction.Stop: id={Id} device={ctx?.Device}"); } catch { }
        }
    }
}
