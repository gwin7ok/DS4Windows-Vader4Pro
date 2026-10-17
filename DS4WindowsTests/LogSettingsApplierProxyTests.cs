using DS4Windows.DI;
using DS4Windows.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-1-5（決定D7＝A）: ログ設定の反映の窓口（ILogSettingsApplier）の遅延接続プロキシ。
    /// 置換前の <c>App.logHolder?.…</c> と同じ動作（接続前は何もしない、接続後は転送する）を固定する。
    /// </summary>
    public class LogSettingsApplierProxyTests
    {
        private sealed class RecordingApplier : ILogSettingsApplier
        {
            public int UpdateLogSettingsCalls;
            public int RestoreArchiveSettingCalls;
            public int? LastMaxArchiveFiles;

            public void UpdateLogSettings() => UpdateLogSettingsCalls++;
            public void UpdateNLogConfig(int maxArchiveFiles) => LastMaxArchiveFiles = maxArchiveFiles;
            public void RestoreArchiveSetting() => RestoreArchiveSettingCalls++;
        }

        [Fact]
        public void BeforeAttach_AllCallsAreNoOps()
        {
            var proxy = new LogSettingsApplierProxy();

            var ex = Record.Exception(() =>
            {
                proxy.UpdateLogSettings();
                proxy.UpdateNLogConfig(5);
                proxy.RestoreArchiveSetting();
            });

            Assert.Null(ex);
        }

        [Fact]
        public void AfterAttach_ForwardsToTarget()
        {
            var proxy = new LogSettingsApplierProxy();
            var target = new RecordingApplier();
            proxy.Attach(target);

            proxy.UpdateLogSettings();
            proxy.UpdateNLogConfig(7);
            proxy.RestoreArchiveSetting();

            Assert.Equal(1, target.UpdateLogSettingsCalls);
            Assert.Equal(7, target.LastMaxArchiveFiles);
            Assert.Equal(1, target.RestoreArchiveSettingCalls);
        }

        [Fact]
        public void LoggerHolder_ImplementsContract()
        {
            Assert.True(typeof(ILogSettingsApplier).IsAssignableFrom(typeof(DS4WinWPF.LoggerHolder)));
        }

        [Fact]
        public void Container_ResolvesSameProxyInstanceForContractAndConcreteType()
        {
            DS4WinWPF.AppHost.CreateHost();

            var contract = DS4WinWPF.AppHost.GetService<ILogSettingsApplier>();
            var concrete = DS4WinWPF.AppHost.GetService<LogSettingsApplierProxy>();

            Assert.NotNull(contract);
            Assert.Same(concrete, contract);
        }
    }
}
