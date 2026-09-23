using System.Reflection;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step3-3: `Mapping.cs` の `MapCustom`（画面座標変換・絶対マウス出力）で使用していた
    /// `Global.absUseAllMonitors` / `Global.TranslateCoorToAbsDisplay` / `ButtonAbsMouseInfos` への
    /// 直接参照を、`ctrl.DisplayCoordinateService` / `ctrl.ProfileSettingsService`（ともに ControlService の
    /// 既存フィールドを公開する読み取り専用 internal プロパティ）経由の引数渡しへ切り替えたことに伴い、
    /// 新設した `ProfileSettingsService` プロパティが Composition Root から受け取った実体をそのまま返すことを検証する。
    /// `DisplayCoordinateService` は Step3-1 で既に追加済みのため対象外（<see cref="ControlServiceStep3Step2DiWiringTests"/> で検証済み）。
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