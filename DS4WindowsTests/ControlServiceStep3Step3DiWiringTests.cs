using System.Reflection;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step3-3: 当初は `Mapping.cs` の `MapCustom`（画面座標変換・絶対マウス出力）が使用していた
    /// `ButtonAbsMouseInfos` への直接参照を、`ctrl.ProfileSettingsService`（ControlService の既存フィールドを
    /// 公開する読み取り専用 internal プロパティ）経由の引数渡しへ切り替えるために新設したプロパティである。
    /// その後、ボタン割当のAbs Mouse機能自体（`ButtonAbsMouseInfos` を含む）とタッチパッドのAbsolute Mouse
    /// モード、共有基盤の `IDisplayCoordinateService` は使用頻度が極めて低いと判断され、ユーザー承認のもと
    /// 別途削除された（`copilot-instructions.md` §2.2 例外規定）。`ProfileSettingsService` プロパティ自体は
    /// Step3-6（`GetControlSettingsGroup` 等）で再利用する汎用アクセサとして存置している。
    /// ここでは、このプロパティが Composition Root から受け取った実体をそのまま返すことを検証する。
    /// パターンは <see cref="ControlServiceStep3Step2DiWiringTests"/> に倣う。
    /// </summary>
    public class ControlServiceStep3Step3DiWiringTests
    {
        private static object GetInternalProperty(ControlService controlService, string propertyName)
        {
            PropertyInfo prop = typeof(ControlService).GetProperty(
                propertyName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(prop); // プロパティ名変更等でシグネチャが失われた場合に検出する
            return prop.GetValue(controlService);
        }

        [Fact]
        public void ProfileSettingsService_ReturnsSameInstanceAsAppHost()
        {
            DS4WinWPF.AppHost.CreateHost();
            var controlService = DS4WinWPF.AppHost.GetService<ControlService>();

            var expected = DS4WinWPF.AppHost.GetService<IProfileSettingsService>();
            var actual = GetInternalProperty(controlService, "ProfileSettingsService");

            Assert.NotNull(expected);
            Assert.Same(expected, actual);
        }
    }
}