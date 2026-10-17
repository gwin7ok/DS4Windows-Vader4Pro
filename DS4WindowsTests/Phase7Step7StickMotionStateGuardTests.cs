using System.IO;
using System.Text.RegularExpressions;
using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step7 W4: スティック・ジャイロ・トリガーの処理状態を <c>StickMotionState</c> へ切り出したことと、
    /// 外から使われる入れ子の型（<c>PostMapStickData</c> ほか）をトップレベルへ出したことの回帰ガード。
    /// ソースが見つからない環境では、ソース検査を行わず合格とする。判断の根拠は Phase7-Step7-Plan.md（Q2・Q3・Q5）。
    /// </summary>
    public class Phase7Step7StickMotionStateGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step7StickMotionStateGuardTests(ITestOutputHelper output)
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
        public void NewInstance_HasTheSameInitialValuesAsTheOldStaticFields()
        {
            var state = new StickMotionState();
            int count = DS4Windows.ControllerLimits.MaxControllerCount;

            Assert.Equal(count, state.GyroStickX.Length);
            Assert.Equal(count, state.GyroStickY.Length);
            Assert.Equal(count, state.WheelFilters.Length);
            Assert.All(state.GyroStickX, v => Assert.Equal(128, v));
            Assert.All(state.GyroStickY, v => Assert.Equal(128, v));

            Assert.Equal(count, state.MapStickActionData.Length);
            Assert.All(state.MapStickActionData, d => Assert.NotNull(d));
            Assert.Equal(count, state.FlickMappingData.Length);
            Assert.All(state.FlickMappingData, d => Assert.NotNull(d));
            Assert.Equal(count, state.L2TwoStageMappingData.Length);
            Assert.Equal(count, state.R2TwoStageMappingData.Length);
            Assert.All(state.L2TwoStageMappingData, d => Assert.NotNull(d));
            Assert.All(state.R2TwoStageMappingData, d => Assert.NotNull(d));
            Assert.Equal(count, state.DeltaAccelProcessors.Length);
            Assert.All(state.DeltaAccelProcessors, g => { Assert.NotNull(g.LSProcessor); Assert.NotNull(g.RSProcessor); });

            // L2 と R2 は別の instance（旧: 別々の配列）
            Assert.NotSame(state.L2TwoStageMappingData[0], state.R2TwoStageMappingData[0]);
        }

        // Phase7-Step8 W2: 転送プロパティは削除した。Mapping のコンストラクターが、受け取った保持クラスの配列を、同じ参照のまま readonly で持つこと
        [Fact]
        public void MappingCopies_ReferenceTheSameArraysAsTheHolder()
        {
            var state = new StickMotionState();
            var mapper = TestMappingFactory.Create(stickMotion: state);

            object Field(string name)
            {
                var field = typeof(Mapping).GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.NotNull(field);
                return field.GetValue(mapper);
            }

            Assert.Same(state.GyroStickX, Field("gyroStickX"));
            Assert.Same(state.GyroStickY, Field("gyroStickY"));
            Assert.Same(state.MapStickActionData, Field("mapStickActionData"));
            Assert.Same(state.FlickMappingData, Field("flickMappingData"));
            Assert.Same(state.L2TwoStageMappingData, Field("l2TwoStageMappingData"));
            Assert.Same(state.R2TwoStageMappingData, Field("r2TwoStageMappingData"));
            Assert.Same(state.DeltaAccelProcessors, Field("deltaAccelProcessors"));
            Assert.Same(state.WheelFilters, Field("wheelFilters"));
        }

        [Fact]
        public void Mapping_HoldsNoStickMotionStateOfItsOwn_AndNoNestedStateTypes()
        {
            string mapping = Read("DS4Windows/DS4Control/Mapping.cs");
            if (mapping == null) return;

            // Phase7-Step8 W2: 転送プロパティは削除し、コンストラクターで受けた保持クラスの配列を readonly で複製して持つ
            Assert.Contains("private readonly StickMotionState _stickMotion;", mapping);
            Assert.Contains("private readonly byte[] gyroStickX;", mapping);
            Assert.Contains("private readonly PostMapStickData[] mapStickActionData;", mapping);
            Assert.Contains("private readonly FlickStickMappingData[] flickMappingData;", mapping);
            Assert.Contains("private readonly DeltaSettingsProcessorGroup[] deltaAccelProcessors;", mapping);
            Assert.Contains("gyroStickX = stickMotion.GyroStickX;", mapping);
            Assert.DoesNotContain("StickMotionState.Shared", mapping);

            foreach (string nested in new[]
            {
                "public class PostMapStickData", "public class FlickStickMappingData", "public class TwoStageTriggerMappingData",
                "public class DeltaSettingsProcessor", "public class DeltaSettingsProcessorGroup", "public enum Click",
            })
            {
                Assert.DoesNotContain(nested, mapping);
            }
        }

        [Fact]
        public void MouseAndControlService_UseTheHolderAndTheTopLevelTypes()
        {
            string mouse = Read("DS4Windows/DS4Control/Mouse.cs");
            string control = Read("DS4Windows/DS4Control/ControlService.cs");
            if (mouse == null || control == null) return;

            Assert.DoesNotContain("Mapping.gyroStick", mouse);
            Assert.DoesNotContain("Mapping.mapStickActionData", mouse);
            Assert.DoesNotContain("Mapping.PostMapStickData", mouse);
            Assert.DoesNotContain("Mapping.Click", mouse);
            Assert.Contains("_stickMotion.GyroStickX", mouse); // Phase7-Step8 W1: コンストラクター注入
            Assert.DoesNotContain("StickMotionState.Shared", mouse);

            foreach (string name in new[] { "l2TwoStageMappingData", "r2TwoStageMappingData", "wheelFilters", "flickMappingData", "deltaAccelProcessors" })
            {
                Assert.DoesNotContain("Mapping." + name, control);
            }
            Assert.Contains("_stickMotion.FlickMappingData", control); // Phase7-Step8 W1: コンストラクター注入
            Assert.DoesNotContain("StickMotionState.Shared", control);
        }

        [Fact]
        public void Registration_RegistersTheSharedState()
        {
            string registration = Read("DS4Windows/DI/ServiceRegistration.cs");
            if (registration == null) return;

            Assert.Contains("services.AddSingleton<StickMotionState>();", registration); // Phase7-Step8 W4: Shared は削除した
        }
    }
}
