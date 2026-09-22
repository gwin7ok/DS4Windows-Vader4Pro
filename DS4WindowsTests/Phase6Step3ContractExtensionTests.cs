using System.Linq;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step3-1: `Mapping.cs` の段階的引数渡し（Step3-2以降）に先立って新設・拡張した
    /// 4つの契約（<see cref="IDisplayCoordinateService"/> 新設、
    /// <see cref="IProfileXmlStore.SaveControllerConfigsForDevice"/>、
    /// <see cref="IProfileActionProvider.GetProfileActionIndexOf"/>／<c>GetProfileActionsRaw</c>、
    /// <see cref="IProfileSettingsService.GetControlSettingsGroup"/>）が、
    /// 従来の <see cref="Global"/> 側の実体と同一の値・同一の参照を返すことを検証する。
    /// 各テストは変更した Global 状態を必ず元に戻す（Phase6-Step2 の教訓）。
    /// </summary>
    public class Phase6Step3ContractExtensionTests
    {
        private readonly DisplayCoordinateService _displayCoordinates = new DisplayCoordinateService();
        private readonly ProfileActionProvider _actionProvider = new ProfileActionProvider();
        private readonly ProfileSettingsService _profileSettings = new ProfileSettingsService();

        // ---- IDisplayCoordinateService ----

        [Fact]
        public void UseAllMonitors_SharesStateWithGlobal()
        {
            bool original = Global.absUseAllMonitors;
            try
            {
                Global.absUseAllMonitors = true;
                Assert.True(_displayCoordinates.UseAllMonitors);

                Global.absUseAllMonitors = false;
                Assert.False(_displayCoordinates.UseAllMonitors);
            }
            finally
            {
                Global.absUseAllMonitors = original;
            }
        }

        [Fact]
        public void TranslateCoorToAbsDisplay_MatchesGlobalComputation()
        {
            var originalDisplay = Global.absDisplayBounds;
            var originalDesktop = Global.fullDesktopBounds;
            try
            {
                // 恒等変換になる境界（フル解像度＝表示解像度）で、Global と同一の計算結果になることを検証する。
                Global.fullDesktopBounds = new System.Windows.Rect(0, 0, 1920, 1080);
                Global.absDisplayBounds = new System.Windows.Rect(0, 0, 1920, 1080);

                _displayCoordinates.TranslateCoorToAbsDisplay(0.25, 0.75, out double svcX, out double svcY);
                Global.TranslateCoorToAbsDisplay(0.25, 0.75, out double globalX, out double globalY);

                Assert.Equal(globalX, svcX);
                Assert.Equal(globalY, svcY);
            }
            finally
            {
                Global.absDisplayBounds = originalDisplay;
                Global.fullDesktopBounds = originalDesktop;
            }
        }

        [Fact]
        public void PrepareAbsMonitorBounds_EmptyEdid_FallsBackToFullDesktop()
        {
            // ControlService の呼び出し（SystemEvents_DisplaySettingsChanged）と同じ引数（空文字）で、
            // 「全モニター使用」にフォールバックする既存の Global の分岐（else節）に入ることを検証する。
            bool originalUseAll = Global.absUseAllMonitors;
            try
            {
                Global.absUseAllMonitors = false;

                _displayCoordinates.PrepareAbsMonitorBounds(string.Empty);

                Assert.True(_displayCoordinates.UseAllMonitors);
            }
            finally
            {
                Global.absUseAllMonitors = originalUseAll;
            }
        }

        // ---- IProfileXmlStore.SaveControllerConfigsForDevice ----

        [Fact]
        public void ProfileXmlStore_ImplementsSaveControllerConfigsForDevice()
        {
            // 実デバイス（DS4Device、実HID接続が前提）を要する処理のため、Step2-2の
            // LoadControllerConfigsForDevice と同様に、値の等価性検証は実機確認に委ねる。
            // ここでは Global への委譲経路が構成されていること（契約実装漏れがないこと）のみを保証する。
            IProfileXmlStore store = new ProfileXmlStore(new BackingStore());
            Assert.NotNull(store);

            var method = typeof(ProfileXmlStore).GetMethod(nameof(IProfileXmlStore.SaveControllerConfigsForDevice));
            Assert.NotNull(method);
        }

        // ---- IProfileActionProvider.GetProfileActionIndexOf / GetProfileActionsRaw ----

        [Fact]
        public void GetProfileActionIndexOf_SharesStateWithGlobal()
        {
            const int slot = 0;
            var original = Global.store.profileActionIndexDict[slot].ToDictionary(kv => kv.Key, kv => kv.Value);
            try
            {
                Global.store.profileActionIndexDict[slot].Clear();
                Global.store.profileActionIndexDict[slot]["TestAction"] = 3;

                Assert.Equal(3, _actionProvider.GetProfileActionIndexOf(slot, "TestAction"));
                Assert.Equal(-1, _actionProvider.GetProfileActionIndexOf(slot, "NoSuchAction"));
                Assert.Equal(
                    Global.GetProfileActionIndexOf(slot, "TestAction"),
                    _actionProvider.GetProfileActionIndexOf(slot, "TestAction"));
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
                Assert.Same(Global.getProfileActions(slot), _actionProvider.GetProfileActionsRaw(slot));
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
            Assert.Same(Global.GetControlSettingsGroup(slot), actual);
        }
    }

    /// <summary>
    /// Phase6-Step3-1: ControlService のコンストラクタ注入拡張（IDisplayCoordinateService）が
    /// Composition Root から正しく配線されていることと、必須引数が null で受け付けられないことを検証する。
    /// パターンは ControlServicePr5DiWiringTests（Phase6-Step2-5）に倣う。
    /// </summary>
    public class ControlServiceStep3DiWiringTests
    {
        [Fact]
        public void AppHost_ResolvesControlService_WithDisplayCoordinateService()
        {
            DS4WinWPF.AppHost.CreateHost();

            ControlService controlService = null;
            var ex = Record.Exception(() => controlService = DS4WinWPF.AppHost.GetService<ControlService>());

            Assert.Null(ex);
            Assert.NotNull(controlService);
        }

        [Fact]
        public void AppHost_ResolvesIDisplayCoordinateService()
        {
            DS4WinWPF.AppHost.CreateHost();

            Assert.NotNull(DS4WinWPF.AppHost.GetService<IDisplayCoordinateService>());
        }

        [Fact]
        public void Constructor_NullDisplayCoordinateService_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<System.ArgumentNullException>(
                () => ControlServiceTestFactory.Create("displayCoordinateService"));

            Assert.Equal("displayCoordinateService", ex.ParamName);
        }
    }
}