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
    /// Phase7-Step8 W4: 状態の保持クラス 3 つ（<c>SyntheticInputState</c>／<c>MacroRuntimeState</c>／<c>StickMotionState</c>）の
    /// 暫定の静的な窓口 <c>Shared</c> を削除したことと、コンテナの Singleton を利用者が共有していることの回帰ガード。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。判断の根拠は Phase7-Step8-Plan.md（Q5）。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase7Step8W4NoSharedGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step8W4NoSharedGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(typeof(SyntheticInputState))]
        [InlineData(typeof(MacroRuntimeState))]
        [InlineData(typeof(StickMotionState))]
        public void TheHolders_HaveNoStaticMembers(Type holder)
        {
            var statics = holder
                .GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(m => !(m is ConstructorInfo) && !(m is FieldInfo f && f.IsLiteral)) // const を除く
                .Select(m => m.Name)
                .ToArray();

            Assert.Empty(statics);
        }

        [Fact]
        public void TheHost_SharesOneInstanceOfEachHolder_WithMappingAndTheControlService()
        {
            DS4WinWPF.AppHost.CreateHost();
            var synthetic = DS4WinWPF.AppHost.GetService<SyntheticInputState>();
            var macro = DS4WinWPF.AppHost.GetService<MacroRuntimeState>();
            var stick = DS4WinWPF.AppHost.GetService<StickMotionState>();
            Assert.Same(synthetic, DS4WinWPF.AppHost.GetService<SyntheticInputState>());
            Assert.Same(macro, DS4WinWPF.AppHost.GetService<MacroRuntimeState>());
            Assert.Same(stick, DS4WinWPF.AppHost.GetService<StickMotionState>());

            var mapping = DS4WinWPF.AppHost.GetService<IMapping>();
            object Field(Type type, object target, string name)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(field);
                return field.GetValue(target);
            }

            Assert.Same(synthetic, Field(typeof(Mapping), mapping, "_syntheticInput"));
            Assert.Same(macro, Field(typeof(Mapping), mapping, "_macroRuntime"));
            Assert.Same(stick, Field(typeof(Mapping), mapping, "_stickMotion"));
            Assert.Same(synthetic.DeviceState, Field(typeof(Mapping), mapping, "deviceState"));

            var control = DS4WinWPF.AppHost.GetService<ControlService>();
            Assert.Same(stick, Field(typeof(ControlService), control, "_stickMotion"));
            Assert.Same(synthetic, Field(typeof(ControlService), control, "_syntheticInput"));
        }

        [Fact]
        public void NoSourceFile_ReferencesTheRemovedSharedWindows()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/DS4Control/Global.cs");
            if (anchor == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string root = Path.GetDirectoryName(Path.GetDirectoryName(anchor)); // DS4Windows プロジェクト
            var shared = new Regex(@"\b(SyntheticInputState|MacroRuntimeState|StickMotionState)\.Shared\b");
            var offenders = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(@"\obj\") && !f.Contains(@"\bin\"))
                .Where(f => shared.IsMatch(Regex.Replace(Regex.Replace(File.ReadAllText(f), @"/\*.*?\*/", string.Empty, RegexOptions.Singleline), @"//[^\r\n]*", string.Empty)))
                .ToArray();
            Assert.True(offenders.Length == 0, "削除済みの Shared への参照が残っている: " + string.Join(", ", offenders));
        }
    }
}
