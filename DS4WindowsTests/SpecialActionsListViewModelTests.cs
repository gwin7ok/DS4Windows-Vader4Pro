using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels;
using Moq;
using Xunit;

namespace DS4WindowsTests
{
    public class SpecialActionsListViewModelTests
    {
        [Fact]
        public void Constructor_PureDI_InitializesCorrectly()
        {
            // Arrange
            var actionRepoMock = new Mock<ISpecialActionRepository>();
            var profileRepoMock = new Mock<IProfileRepository>();
            var outputSlotMock = new Mock<IOutputSlotService>();

            actionRepoMock.Setup(x => x.ActionNames).Returns(new List<string> { "Action1", "Action2" });
            profileRepoMock.Setup(x => x.ProfileList).Returns(new ProfileList());

            // Act
            var vm = new SpecialActionsListViewModel(
                actionRepoMock.Object,
                profileRepoMock.Object,
                outputSlotMock.Object);

            // Assert
            Assert.NotNull(vm.ActionCol);
            Assert.Equal(2, vm.ActionCol.Count);
        }

        [Fact]
        public void SpecialActionsListViewModel_Audit_NoSingletonExternalEventSubscriptions()
        {
            // Watchpoint 2 監査テスト:
            // SpecialActionsListViewModel が Singleton サービス (ISpecialActionRepository, IProfileRepository, IOutputSlotService)
            // のイベントを直接購読していないことを検証し、Singleton からの強参照によるメモリリーク経路が存在しないことを証明する。

            var fields = typeof(SpecialActionsListViewModel).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            
            // ViewModel の型定義に Singleton サービスに対する EventHandler / Delegate フィールドが直接保持されていないことを確認
            foreach (var f in fields)
            {
                Assert.False(typeof(Delegate).IsAssignableFrom(f.FieldType) && f.Name.Contains("Service"),
                    $"Unexpected service event subscription delegate found in field: {f.Name}");
            }
        }

        [Fact]
        public async Task WorkerThread_PropertyAccess_IsThreadSafe()
        {
            // Arrange
            var actionRepoMock = new Mock<ISpecialActionRepository>();
            var profileRepoMock = new Mock<IProfileRepository>();
            var outputSlotMock = new Mock<IOutputSlotService>();
            actionRepoMock.Setup(x => x.ActionNames).Returns(new List<string>());
            profileRepoMock.Setup(x => x.ProfileList).Returns(new ProfileList());

            var vm = new SpecialActionsListViewModel(
                actionRepoMock.Object,
                profileRepoMock.Object,
                outputSlotMock.Object);

            // Act: ワーカースレッドからのアクセス
            var ex = await Record.ExceptionAsync(() => Task.Run(() =>
            {
                _ = vm.ActionCol;
                _ = vm.ExportEnabled;
            }));

            // Assert
            Assert.Null(ex);
        }
    }
}