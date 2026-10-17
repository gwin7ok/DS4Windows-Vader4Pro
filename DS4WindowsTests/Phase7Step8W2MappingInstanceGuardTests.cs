using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step8 W2: <c>Mapping</c> を DI の Singleton（<c>IMapping</c>）の instance にしたこと、サービスへの静的な束縛
    /// （<c>AppHost</c>／<c>ServiceProviderHolder</c>／<c>Global</c> のフォールバック）をコンストラクター注入に替えたこと、
    /// キー／ボタンのコントローラー登録簿を <c>KeyButtonControllerRegistry</c> に切り出したことの回帰ガード。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。判断の根拠は Phase7-Step8-Plan.md（Q1〜Q4・Q8）。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase7Step8W2MappingInstanceGuardTests
    {
        // 状態を持たない表（読み取り専用の運用）。Q2＝A により static のまま残すもの
        private static readonly string[] AllowedStaticFields =
        {
            "shiftTriggerMapping", "ds4ControlMapping", "BoolDS4Controls", "macroEndIndex",
            "calibrationColor_0", "calibrationColor_1", "calibrationColor_2", "calibrationColor_3",
        };

        private readonly ITestOutputHelper _output;

        public Phase7Step8W2MappingInstanceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string Read(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した: " + relativePath);
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            return Regex.Replace(text, @"//[^\r\n]*", string.Empty);
        }

        [Fact]
        public void Mapping_ImplementsIMapping_AndHasNoStaticMutableStateOtherThanTheAllowedTables()
        {
            Assert.True(typeof(IMapping).IsAssignableFrom(typeof(Mapping)));

            var statics = typeof(Mapping)
                .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(f => !f.IsLiteral) // const を除く
                .Select(f => f.Name)
                .OrderBy(n => n)
                .ToArray();

            Assert.Equal(AllowedStaticFields.OrderBy(n => n).ToArray(), statics);
        }

        [Fact]
        public void IMapping_HasExactlyTheEightWindowMembers()
        {
            string[] names = typeof(IMapping).GetMethods().Select(m => m.Name).OrderBy(n => n).ToArray();
            string[] expected =
            {
                "Commit", "EndMacroDirect", "GetBoolMappingExternal", "HandleDeviceDisconnect",
                "MapCustom", "PlayMacroDirect", "SetCurveAndDeadzone", "TempMouseJoystick",
            };
            Assert.Equal(expected, names);
        }

        [Fact]
        public void Constructor_RequiresEveryDependency()
        {
            ConstructorInfo ctor = typeof(Mapping).GetConstructors().Single();
            ParameterInfo[] parameters = ctor.GetParameters();
            Assert.Equal(11, parameters.Length); // Phase7-Step8 W3: ISyntheticDispatcher を追加

            // 実 DI ホストの部品で 1 つずつ null にして、ArgumentNullException になることを確認する
            var valid = new object[]
            {
                new SyntheticInputState(), new MacroRuntimeState(), new StickMotionState(),
                DS4WinWPF.AppHost.GetService<IProfileSettingsService>(),
                DS4WinWPF.AppHost.GetService<IProfileApplicationService>(),
                DS4WinWPF.AppHost.GetService<IVirtualKBM>(),
                DS4WinWPF.AppHost.GetService<IKeyButtonControllerRegistry>(),
                DS4WinWPF.AppHost.GetService<IProcessLauncher>(),
                DS4WinWPF.AppHost.GetService<IProfileSwitcher>(),
                DS4WinWPF.AppHost.GetService<ISyntheticDispatcher>(),
                new Func<IDeviceStateAccessor>(() => null),
            };

            for (int i = 0; i < valid.Length; i++)
            {
                object[] args = (object[])valid.Clone();
                args[i] = null;
                var ex = Assert.Throws<TargetInvocationException>(() => ctor.Invoke(args));
                Assert.IsType<ArgumentNullException>(ex.InnerException);
            }
        }

        [Fact]
        public void Host_ResolvesTheSameMappingInstance_ForControlServiceAndTheContainer()
        {
            DS4WinWPF.AppHost.CreateHost();
            var mapping = DS4WinWPF.AppHost.GetService<IMapping>();
            Assert.NotNull(mapping);
            Assert.Same(mapping, DS4WinWPF.AppHost.GetService<IMapping>());
            Assert.Same(mapping, DS4WinWPF.AppHost.GetService<ControlService>().MappingService);
            Assert.Same(mapping, DS4WinWPF.AppHost.GetService<Func<IMapping>>()());
        }

        [Fact]
        public void DefaultActionManagerAndMacroPlayer_ReceiveALazyMapping_NotTheMappingItself()
        {
            // ホスト構築時（PreallocateActionEntries）に Mapping とその依存を作らないため、Func<IMapping> で受ける（Phase5-Plan §5.4）
            Assert.Contains(typeof(DefaultActionManager).GetConstructors().Single().GetParameters(), p => p.ParameterType == typeof(Func<IMapping>));
            Assert.Contains(typeof(DefaultMacroPlayer).GetConstructors().Single().GetParameters(), p => p.ParameterType == typeof(Func<IMapping>));
            Assert.DoesNotContain(typeof(DefaultActionManager).GetConstructors().Single().GetParameters(), p => p.ParameterType == typeof(IMapping));
            Assert.DoesNotContain(typeof(DefaultMacroPlayer).GetConstructors().Single().GetParameters(), p => p.ParameterType == typeof(IMapping));
        }

        [Fact]
        public void Mapping_HasNoServiceLocatorOrGlobalFallbacks()
        {
            string mapping = Read("DS4Windows/DS4Control/Mapping.cs");
            if (mapping == null) return;

            Assert.DoesNotContain("AppHost", mapping);
            Assert.DoesNotContain("ServiceProviderHolder", mapping);
            Assert.DoesNotContain("Global.outputKBMHandler", mapping);
            Assert.DoesNotContain("ProfileSettingsServiceInstance", mapping);
            Assert.DoesNotContain("MouseOutputAction", mapping);
            Assert.DoesNotContain("keyButtonControllerLock", mapping);
        }

        [Fact]
        public void CallersUseTheInstance_NotStaticMappingCalls()
        {
            var staticCall = new Regex(@"\bMapping\.(Commit|MapCustom|SetCurveAndDeadzone|TempMouseJoystick|HandleDeviceDisconnect|GetBoolMappingExternal|PlayMacroDirect|EndMacroDirect|ClearKeyButtonControllersForDevice|GetOrCreateKeyButtonControllerForAction)\b");
            foreach (string file in new[]
            {
                "DS4Windows/DS4Control/ControlService.cs", "DS4Windows/Actions/ActionManager.cs", "DS4Windows/Actions/DefaultActionManager.cs",
                "DS4Windows/Actions/DefaultMacroPlayer.cs", "DS4Windows/DS4Control/Services/ProfileApplicationService.cs",
                "DS4Windows/DS4Forms/ControllerReadingsControl.xaml.cs", "DS4Windows/DS4Forms/ViewModels/RecordBoxViewModel.cs",
            })
            {
                string text = Read(file);
                if (text == null) continue;
                Assert.False(staticCall.IsMatch(text), file + " に Mapping の静的呼び出しが残っている");
            }
        }

        [Fact]
        public void Registry_ReusesTheControllerPerDeviceAndName_AndClearsOnlyTheGivenDevice()
        {
            var registry = new KeyButtonControllerRegistry(new DefaultKeyButtonActionControllerFactory(), new DefaultControllerRegistry());
            const int deviceA = 6;
            const int deviceB = 7;

            var a1 = registry.GetOrCreate(deviceA, KeyButtonActionController.Mode.Press, "step8-a1");
            var a1again = registry.GetOrCreate(deviceA, KeyButtonActionController.Mode.Press, "step8-a1");
            var a2 = registry.GetOrCreate(deviceA, KeyButtonActionController.Mode.Toggle, "step8-a2");
            var b1 = registry.GetOrCreate(deviceB, KeyButtonActionController.Mode.Press, "step8-b1");

            Assert.NotNull(a1);
            Assert.Same(a1, a1again);
            Assert.NotSame(a1, a2);

            registry.ClearForDevice(deviceA);

            // 破棄されたデバイスのコントローラーは、再度要求すると新しく作られる。他のデバイスは残る
            Assert.NotSame(a1, registry.GetOrCreate(deviceA, KeyButtonActionController.Mode.Press, "step8-a1"));
            Assert.Same(b1, registry.GetOrCreate(deviceB, KeyButtonActionController.Mode.Press, "step8-b1"));

            registry.ClearForDevice(deviceA);
            registry.ClearForDevice(deviceB);
        }

        [Fact]
        public void Registry_RequiresItsDependencies()
        {
            Assert.Throws<ArgumentNullException>(() => new KeyButtonControllerRegistry(null, new DefaultControllerRegistry()));
            Assert.Throws<ArgumentNullException>(() => new KeyButtonControllerRegistry(new DefaultKeyButtonActionControllerFactory(), null));
            Assert.Contains(typeof(ProfileApplicationService).GetConstructors().Single().GetParameters(), p => p.ParameterType == typeof(IKeyButtonControllerRegistry));
        }
    }
}
