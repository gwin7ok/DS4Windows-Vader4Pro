using System;
using System.IO;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    public class LaunchProcessAction : IOutputAction
    {
        private readonly SpecialAction sa;
        private readonly IProcessLauncher _launcher;

        public LaunchProcessAction(SpecialAction sa, IProcessLauncher launcher)
        {
            this.sa = sa;
            this._launcher = launcher ?? throw new ArgumentNullException(nameof(launcher)); // Phase6-Step10-4-4: 必須の依存
        }

        public string Id => sa?.name ?? "LaunchProcess";

        public void Execute(IOutputContext ctx)
        {
            try
            {
                if (sa == null || string.IsNullOrWhiteSpace(sa.details)) return;

                // Phase6-Step9b-3（F06）: DI 移行前（Mapping.cs の specActionLaunchProc、79170f73 の直前）の仕様へ戻した。
                // - extra に "$hidden"（大文字小文字不問）を含むときだけ、その語を 1 回取り除き、ウィンドウを非表示にして起動する。
                //   .bat/.cmd のときは %COMSPEC% /C "<details>" <引数> で起動する。
                // - それ以外は、OS の既定動作（UseShellExecute = true）で details を起動し、extra をそのまま引数に渡す
                //   （ショートカット・ドキュメント・関連付けによる起動を維持するため。非表示にしない）。
                string path = sa.details;
                string extra = sa.extra ?? string.Empty;
                string targetPath = path;
                string arguments = extra;
                bool hidden = false;

                int pos = extra.Length > 0 ? extra.IndexOf("$hidden", StringComparison.OrdinalIgnoreCase) : -1;
                if (pos >= 0)
                {
                    hidden = true;
                    string cmdArgs = extra.Remove(pos, "$hidden".Length);
                    string ext = Path.GetExtension(path).ToLowerInvariant();
                    if (ext == ".bat" || ext == ".cmd")
                    {
                        targetPath = Environment.GetEnvironmentVariable("COMSPEC");
                        if (string.IsNullOrWhiteSpace(targetPath)) targetPath = "cmd.exe";
                        arguments = "/C \"" + path + "\" " + cmdArgs;
                    }
                    else
                    {
                        arguments = cmdArgs;
                    }
                }

                _launcher.Launch(targetPath, arguments, true, hidden);
                try { AppLogger.LogTrace($"LaunchProcessAction.Execute: id={Id} target={targetPath} hidden={hidden}"); } catch { }
            }
            catch (Exception ex)
            {
                try { AppLogger.LogTrace($"LaunchProcessAction.Execute failed: {ex}"); } catch { }
            }
        }

        public void Stop(IOutputContext ctx)
        {
        }
    }
}
