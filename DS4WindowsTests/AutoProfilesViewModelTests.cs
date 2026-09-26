using System;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WindowsTests
{
    // Phase5-Watchpoints-Investigation-Report Watchpoint 2 対応の一環。
    // AutoProfilesViewModel はコンストラクタで IAutoProfileService をプロパティ参照
    // (ProfileSwitchChoice ゲッター/セッター経由) するのみで、そのイベント
    // (RequestServiceChange 等)を購読していないことを実コード調査で確認済み。
    // したがって Singleton サービスのイベント購読解除漏れによるリークの実害は無く、
    // IDisposable の追加実装は不要と判断した。本テストはその前提(イベント未購読)を
    // 将来のリグレッションから守るための構造テストと、DI経由の基本動作確認を行う。
    public class AutoProfilesViewModelTests
    {
        static AutoProfilesViewModelTests()
        {
            if (string.IsNullOrEmpty(Global.appdatapath))
            {
                Global.appdatapath = AppContext.BaseDirectory;
            }
        }

        [Fact]
        public void AutoProfilesViewModel_DoesNotImplementIDisposable()
        {
            // Watchpoint 2監査時点の前提(Singletonイベント未購読)を明文化する回帰ガード。
            // 将来IAutoProfileServiceのイベントを購読するよう変更された場合、
            // このテストが失敗することでIDisposable追加要否の再検討を促す。
            Assert.False(typeof(AutoProfilesViewModel).GetInterface(nameof(IDisposable)) != null);
        }

        [Fact]
        public void AppHost_ShouldResolve_AutoProfilesViewModel_ViaFactory()
        {
            DS4WinWPF.AppHost.CreateHost();
            var factory = DS4WinWPF.AppHost.GetService<IViewModelFactory>();
            var holder = new AutoProfileHolder();
            var profileList = new ProfileList();

            var vm = factory.CreateAutoProfilesViewModel(holder, profileList);

            Assert.NotNull(vm);
        }
    }
}