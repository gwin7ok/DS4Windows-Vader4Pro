using System;
using System.IO;
using System.Linq;

namespace DS4WinWPF
{
    public static class LogRotator
    {
        // Perform deterministic startup rotation: move ds4windows_log.txt -> ds4windows_log_YYYYMMDD_N.txt
        public static void PerformStartupRotation(string appDataPath, int maxArchiveFiles)
        {
            try
            {
                if (string.IsNullOrEmpty(appDataPath)) return;
                string logsDir = Path.Combine(appDataPath, "Logs");
                if (!Directory.Exists(logsDir)) Directory.CreateDirectory(logsDir);

                string src = Path.Combine(logsDir, "ds4windows_log.txt");
                if (!File.Exists(src)) return;

                string date = DateTime.Now.ToString("yyyyMMdd");
                // Find next sequence number
                var existing = Directory.GetFiles(logsDir, $"ds4windows_log_{date}_*.txt");
                int seq = 1;
                if (existing.Length > 0)
                {
                    var nums = existing.Select(p => Path.GetFileNameWithoutExtension(p))
                        .Select(n => n.Split('_').Last())
                        .Select(s => { int v; return int.TryParse(s, out v) ? v : 0; })
                        .Where(v => v >= 0);
                    if (nums.Any()) seq = nums.Max() + 1;
                }

                string dst;
                do
                {
                    dst = Path.Combine(logsDir, $"ds4windows_log_{date}_{seq}.txt");
                    seq++;
                } while (File.Exists(dst));

                // Attempt atomic move
                File.Move(src, dst);

                // Prune archives by most recent modification time (keep newest maxArchiveFiles)
                try
                {
                    var archives = Directory.GetFiles(logsDir, "ds4windows_log_*.txt")
                        .Where(f => !f.EndsWith("ds4windows_log.txt", StringComparison.OrdinalIgnoreCase))
                        .Select(f => new FileInfo(f))
                        .OrderByDescending(fi => fi.LastWriteTimeUtc)
                        .ToList();

                    if (maxArchiveFiles < 0) maxArchiveFiles = 0;
                    for (int i = maxArchiveFiles; i < archives.Count; i++)
                    {
                        try { archives[i].Delete(); } catch { }
                    }
                }
                catch { }
            }
            catch { }
        }
    }
}
