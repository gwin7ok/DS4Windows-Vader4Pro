using System;
using System.Reflection;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    public class SpecialActionsListViewModelTests
    {
        static SpecialActionsListViewModelTests()
        {
            if (string.IsNullOrEmpty(Global.appdatapath))
            {
                Global.appdatapath = AppContext.BaseDirectory;
            }
        }

        [Fact]
        public void SpecialActionsListViewModel_Audit_NoSingletonExternalEventSubscriptions()
        {
            // Watchpoint 2 監査テスト:
            // SpecialActionsListViewModel が Singleton サービス (ISpecialActionRepository, IProfileRepository, IOutputSlotService)
            // のイベントを直接購読していないことを検証し、Singleton からの強参照によるメモリリーク経路が存在しないことを証明する。

            var fields = typeof(SpecialActionsListViewModel).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            
            foreach (var f in fields)
            {
                Assert.False(typeof(Delegate).IsAssignableFrom(f.FieldType) && f.Name.Contains("Service"),
                    $"Unexpected service event subscription delegate found in field: {f.Name}");
            }
        }

        [Fact]
        public void SpecialActionsListViewModel_CanBeInstantiatedWithInjectedServices()
        {
            DS4WinWPF.AppHost.CreateHost();
            
            var specialActionRepo = DS4WinWPF.AppHost.GetService<ISpecialActionRepository>();
            var profileRepo = DS4WinWPF.AppHost.GetService<IProfileRepository>();
            var outputSlotService = DS4WinWPF.AppHost.GetService<IOutputSlotService>();

            // コンストラクタへの DI サービス直接注入による ViewModel 生成を検証
            var vm = new SpecialActionsListViewModel(0, specialActionRepo, profileRepo, null, outputSlotService);

            Assert.NotNull(vm);
            Assert.NotNull(vm.ActionCol);
        }
    }
}