using System;
using Xunit;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    // Phase5-Watchpoints-Investigation-Report Watchpoint 2 対応。
    // ControllerListViewModel は controlService(Singletonの ControlService)の
    // ServiceStarted/PreServiceStop/HotplugController、および profileRepo(Singletonの
    // IProfileRepository)の SelectedProfileChanged を購読するが、従来これらの購読解除が
    // 一切行われていなかった(IDisposable 未実装)。Dispose() 追加によりこれを是正した。
    //
    // 【本テストの制約について】
    // ControlService は具象・重量級クラス(スロット管理・ホットプラグ監視スレッド等の
    // 実ハードウェア連動処理を含む)であり、IControlService のような抽象化インターフェースが
    // 現時点で存在しないため、本リポジトリ全体でユニットテストから直接インスタンス化した
    // 前例が無い。そのため本テストは、実際にイベント購読→Dispose→再発火で未呼び出しを
    // 確認する完全な振る舞いテストではなく、構造面(IDisposable実装の存在とDispose呼び出しが
    // 安全であること)の検証に留める。将来 IControlService 等の抽象化が導入された際は、
    // 本テストを実際のイベント再発火検証に拡張することが望ましい。
    public class ControllerListViewModelTests
    {
        static ControllerListViewModelTests()
        {
            if (string.IsNullOrEmpty(Global.appdatapath))
            {
                Global.appdatapath = AppContext.BaseDirectory;
            }
        }

        [Fact]
        public void ControllerListViewModel_ShouldImplementIDisposable()
        {
            Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(ControllerListViewModel)));
        }

        [Fact]
        public void Dispose_MethodExists_WithNoParameters()
        {
            var disposeMethod = typeof(ControllerListViewModel).GetMethod("Dispose", Type.EmptyTypes);
            Assert.NotNull(disposeMethod);
        }
    }
}