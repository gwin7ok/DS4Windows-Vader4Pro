using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    // Phase5-Step14 FormSettings統一: 本テストクラスは以前、EnvironmentServiceが独自に
    // 孤立保持していた RunAtStartup/StartMinimized/CloseMinimizes/UseLang/FormWidth/FormHeight/
    // FormLocationX/FormLocationY（Global/m_Configと非連動）を前提としたアサートを含んでいた。
    // これらのプロパティはIAppSettingsServiceとの重複であったため撤去され、IEnvironmentServiceは
    // 実行時環境プローブ（IsAdministrator/ApplicationVersion/RefreshHidHideInfo/RefreshFakerInputInfo）
    // 専用インターフェースへ純化された。本テストもそれに合わせて書き直す。
    public class EnvironmentServiceTests
    {
        [Fact]
        public void ApplicationVersion_ShouldDelegateToGlobal()
        {
            var service = new EnvironmentService();

            Assert.Equal(Global.exeversion, service.ApplicationVersion);
        }

        [Fact]
        public void IsAdministrator_ShouldNotThrow()
        {
            var service = new EnvironmentService();

            // 実行環境(管理者/非管理者)に依存するため値そのものは検証せず、
            // Globalへの委譲呼び出しが例外なく完了することのみ確認する。
            var _ = service.IsAdministrator();
        }

        [Fact]
        public void GlobalShim_ShouldReturnAssignedInstance()
        {
            var service = new EnvironmentService();
            IEnvironmentService original = Global.EnvironmentServiceInstance;
            try
            {
                Global.EnvironmentServiceInstance = service;

                Assert.Same(service, Global.EnvironmentServiceInstance);
            }
            finally
            {
                // 他テストへの影響を避けるため、シムの状態を元に戻す。
                Global.EnvironmentServiceInstance = original;
            }
        }
    }
}