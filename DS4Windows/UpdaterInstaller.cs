using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using NLog;

namespace DS4WinWPF
{
    // Installer helper invoked by UI flow. Attempts non-elevated move first;
    // on access denied it will relaunch the current EXE elevated with
    // --complete-install and --complete-install-target arguments and wait for result.
    public static class UpdaterInstaller
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // Try to install; returns true on success.
        // If allowElevation is false, do not attempt to relaunch elevated and simply return false on permission errors.
        public static bool TryInstall(string sourcePath, string targetPath, bool allowElevation = true)
        {
            try
            {
                if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(targetPath))
                {
                    logger.Error("TryInstall called with empty paths");
                    return false;
                }

                var targetDir = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                // Attempt non-elevated copy/move first
                try
                {
                    File.Copy(sourcePath, targetPath, true);
                    try { File.Delete(sourcePath); } catch { }
                    logger.Info($"Installed without elevation: {targetPath}");
                    return true;
                }
                catch (UnauthorizedAccessException)
                {
                    logger.Warn("Non-elevated install failed due to permissions");
                    if (!allowElevation)
                    {
                        // Caller requested no elevation attempt
                        return false;
                    }
                    // fallthrough to elevation attempt
                }

                // Build elevated start info
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                string args = $"--complete-install \"{sourcePath}\" --complete-install-target \"{targetPath}\"";
                var psi = new ProcessStartInfo(exePath)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    Arguments = args
                };

                try
                {
                    var proc = Process.Start(psi);
                    if (proc == null)
                    {
                        logger.Error("Failed to start elevated process (null)");
                        return false;
                    }

                    proc.WaitForExit();
                    int code = proc.ExitCode;
                    logger.Info($"Elevated installer exited with code {code}");
                    return code == 0;
                }
                catch (Win32Exception ex)
                {
                    // This is thrown when user cancels UAC or process cannot be started
                    logger.Warn(ex, "Elevation cancelled or failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "TryInstall failed");
                return false;
            }
        }
    }
}
