using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DS4Windows;
using DS4Windows.DI;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10b W3（Q3＝A・Q5＝A）: 持ち越しの契約の整理の回帰ガード。
    /// (1) <c>ProfileSubSettingBase.OnSubPropertyChanged</c>（購読者 0 件の孤立イベント）を削除したこと（<c>PropertyChanged</c> は残る）、
    /// (2) <c>ProfileApplicationService</c> が未使用の依存 <c>IDeviceStateService</c> を持たないこと、
    /// (3) <c>IOutputSlotService.OutputSlotChanged</c> は温存し、TODO が更新されていること。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    public class Phase7Step10bW3ContractCleanupGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step10bW3ContractCleanupGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private class Dummy : ProfileSubSettingBase
        {
            public void Raise() => RaisePropertyChanged("X");
            public void RaiseAll() => RaiseAllPropertiesChanged();
        }

        [Fact]
        public void ProfileSubSettingBase_HasNoOnSubPropertyChanged_ButStillRaisesPropertyChanged()
        {
            Assert.Null(typeof(ProfileSubSettingBase).GetEvent("OnSubPropertyChanged",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));

            var d = new Dummy();
            string last = null;
            d.PropertyChanged += (s, e) => last = e.PropertyName;
            d.Raise();
            Assert.Equal("X", last);
            d.RaiseAll();
            Assert.Equal(string.Empty, last);
        }

        [Fact]
        public void ProfileApplicationService_DoesNotTakeIDeviceStateService()
        {
            var ctor = typeof(ProfileApplicationService).GetConstructors().Single();
            Assert.DoesNotContain(ctor.GetParameters(), p => p.ParameterType == typeof(IDeviceStateService));
            Assert.Equal(8, ctor.GetParameters().Length);
        }

        [Fact]
        public void TheHost_StillResolvesTheProfileApplicationService()
        {
            DS4WinWPF.AppHost.CreateHost();
            Assert.NotNull(DS4WinWPF.AppHost.GetService<IProfileApplicationService>());
        }

        [Fact]
        public void OutputSlotChanged_IsKept_WithAnUpdatedTodo()
        {
            Assert.NotNull(typeof(IOutputSlotService).GetEvent("OutputSlotChanged"));

            string path = SourceFileLocator.Find("DS4Windows/DI/IOutputSlotService.cs");
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string raw = File.ReadAllText(path);
            Assert.Contains("TODO(Phase7-Step2 W6、温存。Phase7-Step6 Q5＝A・Phase7-Step10b Q5＝A", raw);
        }
    }
}
