using System;
using System.IO;
using NLog;

namespace DS4WinWPF
{
    // Helper used when process is started elevated to complete file move/install
    public static class UpdaterElevatedHelper
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        // Return 0 on success, non-zero error codes otherwise
        public static int CompleteInstall(string sourcePath, string targetPath)
        {
            try
            {
                if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(targetPath))
                {
                    logger.Error("CompleteInstall called with empty paths");
                    return 4; // cannot_save_download / invalid args
                }

                if (!File.Exists(sourcePath))
                {
                    logger.Error($"Source file not found: {sourcePath}");
                    return 2; // download_failed / missing source
                }

                var targetDir = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                // Use copy then delete to allow overwrite semantics
                File.Copy(sourcePath, targetPath, true);
                try
                {
                    File.Delete(sourcePath);
                }
                catch (Exception ex)
                {
                    // not fatal; log and continue
                    logger.Warn(ex, $"Failed to delete temp file {sourcePath}");
                }

                logger.Info($"CompleteInstall succeeded: {sourcePath} -> {targetPath}");
                return 0;
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Error(ex, $"Unauthorized while completing install {sourcePath} -> {targetPath}");
                return 3; // admin_required
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to complete install {sourcePath} -> {targetPath}");
                return 5; // replace_failed / general failure
            }
        }
    }
}
