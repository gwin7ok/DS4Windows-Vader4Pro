using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using NLog;
using DS4Windows;

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
                    AppLogger.LogDebug("[UpdaterInstall] TryInstall called with empty source/target");
                    return false;
                }

                var targetDir = Path.GetDirectoryName(targetPath);

                // Attempt non-elevated copy/move first. Ensure directory creation is part of
                // the non-elevated attempt so permission errors can fallthrough to elevation.
                try
                {
                    AppLogger.LogDebug($"[UpdaterInstall] Attempting non-elevated file install: {sourcePath} -> {targetPath}");
                    if (!Directory.Exists(targetDir))
                    {
                        AppLogger.LogDebug($"[UpdaterInstall] Creating target directory: {targetDir}");
                        Directory.CreateDirectory(targetDir);
                    }
                    File.Copy(sourcePath, targetPath, true);
                    try { File.Delete(sourcePath); } catch { }
                    logger.Info($"Installed without elevation: {targetPath}");
                    AppLogger.LogDebug($"[UpdaterInstall] Installed without elevation: {targetPath}");
                    return true;
                }
                catch (UnauthorizedAccessException ex)
                {
                    AppLogger.LogDebug($"[UpdaterInstall] Non-elevated install failed due to permissions: {ex.Message}");
                    logger.Warn("Non-elevated install failed due to permissions");
                    if (!allowElevation)
                    {
                        // Caller requested no elevation attempt
                        AppLogger.LogDebug("[UpdaterInstall] Caller requested no elevation; returning false");
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
                    Arguments = args,
                    WorkingDirectory = Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory
                };

                AppLogger.LogDebug($"[UpdaterInstall] Launching elevated install helper: {exePath} {args}");
                AppLogger.LogDebug($"[UpdaterInstall] ProcessStartInfo: FileName={psi.FileName} Arguments={psi.Arguments} UseShellExecute={psi.UseShellExecute} Verb={psi.Verb} WorkingDirectory={psi.WorkingDirectory}");

                try
                {
                    var proc = Process.Start(psi);
                    if (proc == null)
                    {
                        logger.Error("Failed to start elevated process (null)");
                        AppLogger.LogDebug("[UpdaterInstall] Failed to start elevated process: null returned");
                        return false;
                    }

                    proc.WaitForExit();
                    int code = proc.ExitCode;
                    logger.Info($"Elevated installer exited with code {code}");
                    AppLogger.LogDebug($"[UpdaterInstall] Elevated installer exited with code {code}");
                    return code == 0;
                }
                catch (Win32Exception ex)
                {
                    // This is thrown when user cancels UAC or process cannot be started
                    logger.Warn(ex, "Elevation cancelled or failed");
                    AppLogger.LogError($"[UpdaterInstall] Elevation start failure: {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[UpdaterInstall] TryInstall failed: {ex.Message}");
                logger.Error(ex, "TryInstall failed");
                return false;
            }
        }

        // Try to install a directory (copy all files/subdirs under sourceDir into targetDir)
        public static bool TryInstallDirectory(string sourceDir, string targetDir, bool allowElevation = true)
        {
            try
            {
                if (string.IsNullOrEmpty(sourceDir) || string.IsNullOrEmpty(targetDir))
                {
                    logger.Error("TryInstallDirectory called with empty paths");
                    AppLogger.LogDebug("[UpdaterInstall] TryInstallDirectory called with empty source/target");
                    return false;
                }

                if (!Directory.Exists(sourceDir))
                {
                    logger.Error($"Source directory not found: {sourceDir}");
                    AppLogger.LogDebug($"[UpdaterInstall] Source directory missing: {sourceDir}");
                    return false;
                }

                // Attempt non-elevated directory install first. Directory creation is part of
                // the non-elevated attempt so permission errors allow falling through to elevation.
                try
                {
                    AppLogger.LogDebug($"[UpdaterInstall] Attempting non-elevated directory install: {sourceDir} -> {targetDir}");
                    if (!Directory.Exists(targetDir))
                    {
                        AppLogger.LogDebug($"[UpdaterInstall] Creating target directory: {targetDir}");
                        Directory.CreateDirectory(targetDir);
                    }
                    // Copy recursively
                    CopyDirectoryRecursive(sourceDir, targetDir);
                    logger.Info($"Installed directory without elevation: {targetDir}");
                    AppLogger.LogDebug($"[UpdaterInstall] Installed directory without elevation: {targetDir}");
                    return true;
                }
                catch (UnauthorizedAccessException ex)
                {
                    AppLogger.LogDebug($"[UpdaterInstall] Non-elevated directory install failed due to permissions: {ex.Message}");
                    logger.Warn("Non-elevated directory install failed due to permissions");
                    if (!allowElevation)
                    {
                        AppLogger.LogDebug("[UpdaterInstall] Caller requested no elevation; returning false from TryInstallDirectory");
                        return false;
                    }
                    // else fallthrough to elevation attempt
                }

                // Elevation: start elevated process to perform complete install
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                // Pass source directory and target directory as arguments
                string args = $"--complete-install \"{sourceDir}\" --complete-install-target \"{targetDir}\"";
                var psi = new ProcessStartInfo(exePath)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    Arguments = args,
                    WorkingDirectory = Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory
                };

                AppLogger.LogDebug($"[UpdaterInstall] Launching elevated directory installer: {exePath} {args}");
                AppLogger.LogDebug($"[UpdaterInstall] ProcessStartInfo: FileName={psi.FileName} Arguments={psi.Arguments} UseShellExecute={psi.UseShellExecute} Verb={psi.Verb} WorkingDirectory={psi.WorkingDirectory}");

                try
                {
                    var proc = Process.Start(psi);
                    if (proc == null)
                    {
                        logger.Error("Failed to start elevated process (null)");
                        AppLogger.LogDebug("[UpdaterInstall] Failed to start elevated process (null)");
                        return false;
                    }

                    proc.WaitForExit();
                    int code = proc.ExitCode;
                    logger.Info($"Elevated installer exited with code {code}");
                    AppLogger.LogDebug($"[UpdaterInstall] Elevated installer exited with code {code}");
                    return code == 0;
                }
                catch (Win32Exception ex)
                {
                    AppLogger.LogError($"[UpdaterInstall] Elevation cancelled or failed: {ex.Message}");
                    logger.Warn(ex, "Elevation cancelled or failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[UpdaterInstall] TryInstallDirectory failed: {ex.Message}");
                logger.Error(ex, "TryInstallDirectory failed");
                return false;
            }
        }

        private static void CopyDirectoryRecursive(string sourceDir, string destinationDir)
        {
            // Create all directories
            foreach (var dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourceDir, destinationDir));
            }

            // Copy all files
            foreach (var newPath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourceDir, destinationDir), true);
            }
        }
    }
}
