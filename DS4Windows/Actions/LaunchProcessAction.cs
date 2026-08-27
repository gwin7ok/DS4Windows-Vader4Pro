using System;
using System.IO;
using DS4Windows.Services;
using DS4WinWPF;

namespace DS4Windows.Actions
{
    public class LaunchProcessAction : IOutputAction
    {
        private readonly SpecialAction sa;
        private readonly IProcessLauncher _launcher;

        public LaunchProcessAction(SpecialAction sa, IProcessLauncher launcher = null)
        {
            this.sa = sa;
            this._launcher = launcher ?? AppHost.GetService<IProcessLauncher>() ?? new DefaultProcessLauncher();
        }

        public string Id => sa?.name ?? "LaunchProcess";

        public void Execute(IOutputContext ctx)
        {
            try
            {
                if (sa == null || string.IsNullOrWhiteSpace(sa.details)) return;

                string path = sa.details;
                string ext = Path.GetExtension(path).ToLowerInvariant();
                string targetPath = path;
                string arguments = sa.extra ?? string.Empty;

                if (ext == ".bat" || ext == ".cmd")
                {
                    targetPath = "cmd.exe";
                    arguments = $"/c \"{path}\" {arguments}".Trim();
                }

                _launcher.Launch(targetPath, arguments, false, true);
                try { AppLogger.LogTrace($"LaunchProcessAction.Execute: id={Id} target={targetPath}"); } catch { }
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
