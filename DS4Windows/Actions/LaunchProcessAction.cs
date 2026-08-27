using System;
using System.Diagnostics;
using System.IO;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    public class LaunchProcessAction : IOutputAction
    {
        private readonly SpecialAction sa;
        private readonly IProcessLauncher _launcher;

        public LaunchProcessAction(SpecialAction sa, IProcessLauncher launcher = null)
        {
            this.sa = sa;
            this._launcher = launcher ?? new DefaultProcessLauncher();
        }

        public string Id => sa?.name ?? "LaunchProcess";

        public void Execute(IOutputContext ctx)
        {
            try
            {
                if (sa == null) return;

                string path = !string.IsNullOrEmpty(sa.details) ? sa.details : sa.customAction;
                if (string.IsNullOrWhiteSpace(path)) return;

                string ext = Path.GetExtension(path).ToLowerInvariant();
                string targetPath = path;
                string arguments = sa.arguments ?? string.Empty;

                if (ext == ".bat" || ext == ".cmd")
                {
                    targetPath = "cmd.exe";
                    arguments = $"/c \"{path}\" {arguments}".Trim();
                }

                _launcher.Launch(targetPath, arguments);
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
