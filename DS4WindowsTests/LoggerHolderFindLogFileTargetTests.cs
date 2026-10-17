using DS4WinWPF;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step9b-1（F04）: NLog.config の async="false" では logfile が直接の FileTarget になる。
    /// UpdateLogSettings／RestoreArchiveSetting が使う検索が、ラッパーの有無の両方で FileTarget を返すことを固定する。
    /// （旧実装はラッパー前提で NullReferenceException になり、設定の保存も止めていた）
    /// </summary>
    public class LoggerHolderFindLogFileTargetTests
    {
        [Fact]
        public void FindLogFileTarget_DirectFileTarget_ReturnsIt()
        {
            var config = new LoggingConfiguration();
            var file = new FileTarget("logfile") { FileName = "direct.txt" };
            config.AddTarget(file);

            var found = LoggerHolder.FindLogFileTarget(config);

            Assert.Same(file, found);
        }

        [Fact]
        public void FindLogFileTarget_AsyncWrappedFileTarget_ReturnsInnerFileTarget()
        {
            var config = new LoggingConfiguration();
            var file = new FileTarget("logfile") { FileName = "wrapped.txt" };
            var wrapper = new AsyncTargetWrapper("logfile", file);
            config.AddTarget(wrapper);

            var found = LoggerHolder.FindLogFileTarget(config);

            Assert.Same(file, found);
        }

        [Fact]
        public void FindLogFileTarget_NoLogfileTarget_ReturnsNull()
        {
            var config = new LoggingConfiguration();
            config.AddTarget(new FileTarget("other") { FileName = "other.txt" });

            Assert.Null(LoggerHolder.FindLogFileTarget(config));
        }

        [Fact]
        public void FindLogFileTarget_NullConfiguration_ReturnsNull()
        {
            Assert.Null(LoggerHolder.FindLogFileTarget(null));
        }
    }
}
