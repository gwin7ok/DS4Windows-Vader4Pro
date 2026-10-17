using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10b W1（Q1＝A）: 既定のボタン割り当て・名前の表・<c>getX360ControlString</c>（旧 <c>Global</c> と <c>BackingStore</c>）を
    /// <c>X360ControlTables</c>／<c>DS4ControlNames</c> へ移したことの回帰ガード。表の内容は従来と同じでなければならない。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    public class Phase7Step10bW1ControlTablesGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step10bW1ControlTablesGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void DefaultButtonMapping_HasOneEntryPerDS4Control_WithTheKnownAnchors()
        {
            var map = X360ControlTables.defaultButtonMapping;
            Assert.Equal(X360Controls.None, map[(int)DS4Controls.None]);
            Assert.Equal(X360Controls.LB, map[(int)DS4Controls.L1]);
            Assert.Equal(X360Controls.RT, map[(int)DS4Controls.R2]);
            Assert.Equal(X360Controls.A, map[(int)DS4Controls.Cross]);
            Assert.Equal(X360Controls.DpadUp, map[(int)DS4Controls.DpadUp]);
        }

        [Fact]
        public void ReverseMapping_IsTheInverseOfTheDefaultMapping_ForEveryMappedButton()
        {
            var forward = X360ControlTables.defaultButtonMapping;
            var reverse = X360ControlTables.reverseX360ButtonMapping;
            Assert.Equal(forward.Length, reverse.Length);
            for (int i = 0; i < forward.Length; i++)
            {
                X360Controls mapped = forward[i];
                // 複数の DS4 入力が同じ X360 ボタンへ割り当たる場合は、後の方が逆引きに残る。どちらにしても往復で元のボタンに戻ること
                if (mapped != X360Controls.None)
                    Assert.Equal(mapped, forward[(int)reverse[(int)mapped]]);
            }
        }

        [Fact]
        public void GetX360ControlString_ByControllerType_UsesTheMatchingNameTable()
        {
            Assert.Equal("A Button", X360ControlTables.xboxDefaultNames[X360Controls.A]);
            Assert.Equal(X360ControlTables.xboxDefaultNames[X360Controls.A],
                X360ControlTables.getX360ControlString(X360Controls.A, OutContType.X360));
            Assert.Equal(X360ControlTables.ds4DefaultNames[X360Controls.A],
                X360ControlTables.getX360ControlString(X360Controls.A, OutContType.DS4));
            Assert.Equal(string.Empty, X360ControlTables.getX360ControlString(X360Controls.A, OutContType.None));
        }

        [Theory]
        [InlineData(X360Controls.Back, "Back")]
        [InlineData(X360Controls.LS, "Left Stick")]
        [InlineData(X360Controls.LB, "Left Bumper")]
        [InlineData(X360Controls.LXNeg, "Left X-Axis-")]
        [InlineData(X360Controls.LYPos, "Left Y-Axis+")]
        [InlineData(X360Controls.TouchpadClick, "Touchpad Click")]
        [InlineData(X360Controls.FifthMouse, "5th Mouse Button")]
        [InlineData(X360Controls.WUP, "Mouse Wheel Up")]
        [InlineData(X360Controls.Unbound, "Unbound")]
        public void GetX360ControlString_ForTheXmlName_IsUnchanged(X360Controls key, string expected)
        {
            Assert.Equal(expected, X360ControlTables.getX360ControlString(key));
        }

        [Fact]
        public void GetX360ControlString_ForTheXmlName_FallsBackToUnbound()
        {
            Assert.Equal("Unbound", X360ControlTables.getX360ControlString((X360Controls)255));
        }

        [Fact]
        public void DS4ControlNames_ContainTheKnownEntries()
        {
            Assert.NotEmpty(DS4ControlNames.ds4inputNames);
            Assert.True(DS4ControlNames.ds4inputNames.ContainsKey(DS4Controls.Cross));
            Assert.Equal(286, DS4ControlNames.macroDS4Values[DS4Controls.TouchMulti]);
            Assert.Equal(287, DS4ControlNames.macroDS4Values[DS4Controls.TouchStarted]);
            Assert.Equal(288, DS4ControlNames.macroDS4Values[DS4Controls.TouchEnded]);
        }

        [Fact]
        public void TheSource_NoLongerReferencesTheMovedMembersOnGlobal_AndBackingStoreHasNoCopy()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/DS4Control/Global.cs");
            if (anchor == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(anchor), "..", ".."));
            var pattern = new Regex(@"(?<![\w])(?:DS4Windows\.)?Global\.(defaultButtonMapping|reverseX360ButtonMapping|xboxDefaultNames|ds4DefaultNames|ds4inputNames|macroDS4Values|getX360ControlString)\b");
            string Strip(string t) => Regex.Replace(Regex.Replace(t, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline), @"//[^\r\n]*", string.Empty);
            var offenders = Directory.GetFiles(Path.Combine(root, "DS4Windows"), "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(@"\obj\") && !f.Contains(@"\bin\"))
                .Where(f => pattern.IsMatch(Strip(File.ReadAllText(f))))
                .Select(f => Path.GetFileName(f))
                .ToArray();
            Assert.True(offenders.Length == 0, "Global の移動済みメンバーへの参照が残っている: " + string.Join(", ", offenders));

            string backing = Strip(File.ReadAllText(Path.Combine(root, "DS4Windows", "DS4Control", "BackingStore.cs")));
            Assert.False(Regex.IsMatch(backing, @"\bstring\s+getX360ControlString\s*\("), "BackingStore に getX360ControlString の宣言が残っている");
        }
    }
}
