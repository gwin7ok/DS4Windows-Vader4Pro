using System.Reflection;
using Xunit;
using DS4Windows;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step3-2: `Mapping.LogActionDoneCountOnTrigger`（private static、診断ログ用ヘルパー）に
    /// `ControlService ctrl` 引数を追加した変更（`GetActions()?.Count` を
    /// `ctrl?.SpecialActionRepository?.ActionList?.Count` へ置換）の回帰防止テスト。
    /// ログ出力自体（診断情報）は検証対象とせず、シグネチャ変更後も例外を発生させずに
    /// 呼び出せること（null 安全性を含む）のみを検証する。
    /// </summary>
    public class MappingLogActionDoneCountOnTriggerTests
    {
        private static MethodInfo Method => typeof(Mapping).GetMethod(
            "LogActionDoneCountOnTrigger", BindingFlags.Static | BindingFlags.NonPublic);

        [Fact]
        public void MethodSignature_HasControlServiceParameter()
        {
            Assert.NotNull(Method); // メソッド名変更等でシグネチャが失われた場合に検出する

            ParameterInfo[] parameters = Method.GetParameters();
            Assert.Contains(parameters, p => p.Name == "ctrl" && p.ParameterType == typeof(ControlService));
        }

        [Fact]
        public void Invoke_WithNullControlService_DoesNotThrow()
        {
            // ctrl が null でも ctrl?.SpecialActionRepository?.ActionList?.Count ?? 0 により安全に 0 件扱いになる。
            var ex = Record.Exception(() =>
                Method.Invoke(null, new object[] { 0, null, 0, null, "UnitTest" }));

            Assert.Null(ex);
        }

        [Fact]
        public void Invoke_WithRealControlService_DoesNotThrow()
        {
            DS4WinWPF.AppHost.CreateHost();
            var controlService = DS4WinWPF.AppHost.GetService<ControlService>();

            var ex = Record.Exception(() =>
                Method.Invoke(null, new object[] { 0, null, 0, controlService, "UnitTest" }));

            Assert.Null(ex);
        }
    }
}