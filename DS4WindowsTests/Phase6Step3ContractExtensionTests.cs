using System.Linq;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step3-1: `Mapping.cs` の段階的引数渡し（Step3-2以降）に先立って新設・拡張した
    /// 3つの契約（
    /// <see cref="IProfileXmlStore.SaveControllerConfigsForDevice"/>、
    /// <see cref="IProfileActionProvider.GetProfileActionIndexOf"/>／<c>GetProfileActionsRaw</c>、
    /// <see cref="IProfileSettingsService.GetControlSettingsGroup"/>）が、
    /// 従来の <see cref="Global"/> 側の実体と同一の値・同一の参照を返すことを検証する。
    /// 各テストは変更した Global 状態を必ず元に戻す（Phase6-Step2 の教訓）。
    /// </summary>
    public class Phase6Step3ContractExtensionTests
    {
        private readonly ProfileActionProvider _actionProvider = TestRepositoryFactory.CreateProfileActionProvider();
        private readonly ProfileSettingsService _profileSettings = TestRepositoryFactory.CreateProfileSettingsService();

        // ---- IProfileActionProvider.GetProfileActionIndexOf / GetProfileActionsRaw ----

        [Fact]
        public void GetProfileActionIndexOf_SharesStateWithGlobal_AndReturnsMinusOneWhenMissing()
        {
            const int slot = 0;
            var original = Global.store.profileActionIndexDict[slot].ToDictionary(kv => kv.Key, kv => kv.Value);
            try
            {
                Global.store.profileActionIndexDict[slot].Clear();
                Global.store.profileActionIndexDict[slot]["TestAction"] = 3;
                Global.store.profileActionIndexDict[slot]["FirstAction"] = 0;

                Assert.Equal(3, _actionProvider.GetProfileActionIndexOf(slot, "TestAction"));
                Assert.Equal(0, _actionProvider.GetProfileActionIndexOf(slot, "FirstAction"));

                // Phase7-Step1 W2（K2 の是正）: Dictionary<TKey,TValue>.TryGetValue は未検出時に out 引数を
                // default(int)(=0) で上書きするため、移植元の Global.GetProfileActionIndexOf（Phase6-Step12 Wave 4 で削除）は
                // 未検出でも 0 を返していた。現在は戻り値で判定し、未検出は -1 を返す（0 は定義済みの先頭のアクションと区別できる）。
                Assert.Equal(-1, _actionProvider.GetProfileActionIndexOf(slot, "NoSuchAction"));
                Assert.Equal(-1, _actionProvider.GetProfileActionIndexOf(slot, null));
            }
            finally
            {
                Global.store.profileActionIndexDict[slot].Clear();
                foreach (var kv in original)
                    Global.store.profileActionIndexDict[slot][kv.Key] = kv.Value;
            }
        }

        [Fact]
        public void GetProfileActionsRaw_ReturnsSameInstanceAsGlobal()
        {
            const int slot = 1;
            var original = Global.store.profileActions[slot];
            try
            {
                var list = new System.Collections.Generic.List<string> { "Alpha", "Beta" };
                Global.store.profileActions[slot] = list;

                // Global.getProfileActions と同じ実体参照を返す（ToArray() 等でコピーしない）ことを検証する。
                Assert.Same(list, _actionProvider.GetProfileActionsRaw(slot));
                Assert.Same(Global.store.profileActions[slot], _actionProvider.GetProfileActionsRaw(slot));
            }
            finally
            {
                Global.store.profileActions[slot] = original;
            }
        }

        // ---- IProfileSettingsService.GetControlSettingsGroup ----

        [Fact]
        public void GetControlSettingsGroup_ReturnsSameInstanceAsGlobal()
        {
            const int slot = 0;
            var expected = Global.store.ds4controlSettings[slot];

            var actual = _profileSettings.GetControlSettingsGroup(slot);

            Assert.Same(expected, actual);
        }
    }
}