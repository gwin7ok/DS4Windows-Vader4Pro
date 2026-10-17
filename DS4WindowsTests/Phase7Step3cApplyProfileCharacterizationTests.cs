using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step3c: プロファイル適用（旧 <c>Global.ApplyProfile</c>／<c>ApplyProfileToSlot</c>／<c>LoadTempProfile</c>／プリセット）の結果を固定する特性化テスト。
    /// W1 では旧 <c>Global</c> に対して書き、W3 で移管先と両方に流して同じ結果であることを確かめ、W4 で旧 <c>Global</c> の削除とともに移管先だけにした
    /// （期待値は W1 から変えていない）。
    /// 固定するもの: 適用後の状態（<c>ProfilePath</c>／<c>SelectedProfile</c>／<c>OlderProfilePath</c>）、一時プロファイルの状態（名前・使用・距離）、
    /// <c>SelectedProfileChanged</c> の発火（回数と引数）、失敗時に何も変えないこと、スロット・名前の検査。
    /// 静的な状態（<c>Global.appdatapath</c>、スロット 0 の状態、<c>Program.rootHub</c>）は退避して復元する。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase7Step3cApplyProfileCharacterizationTests : IDisposable
    {
        private const int Slot = 0;
        private const string Normal = "Phase7Step3c_Normal";
        private const string Temp = "Phase7Step3c_Temp";
        private const string Distance = "Phase7Step3c_distance_Temp";
        private const string Missing = "Phase7Step3c_Missing";

        private readonly string _dir;
        private readonly string _originalAppData;
        private readonly ControlService _control;
        private readonly IProfileSettingsService _settings;
        private readonly ProfileRepository _repo;
        private readonly ProfileApplicationService _service;
        private readonly ProfileSlotApplier _slotApplier;
        private readonly string _origProfilePath;
        private readonly string _origOlder;
        private readonly string _origSelected;
        private readonly bool _origUseTemp;
        private readonly string _origTempName;
        private readonly bool _origDistance;
        private readonly List<SelectedProfileChangedEventArgs> _events = new List<SelectedProfileChangedEventArgs>();
        private readonly EventHandler<SelectedProfileChangedEventArgs> _handler;

        public Phase7Step3cApplyProfileCharacterizationTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "Phase7Step3c_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_dir, "Profiles"));
            _originalAppData = Global.appdatapath;
            Global.appdatapath = _dir;

            DS4WinWPF.AppHost.CreateHost();
            _control = DS4WinWPF.AppHost.GetService<ControlService>();
            _settings = Global.ProfileSettingsServiceInstance;

            _repo = TestRepositoryFactory.CreateProfileRepository(_settings);
            _service = TestRepositoryFactory.CreateProfileApplicationService(_settings,
                () => _control, _repo, new EnvironmentService());
            _slotApplier = new ProfileSlotApplier(_service);

            _origProfilePath = _repo.ProfilePath[Slot];
            _origOlder = _repo.OlderProfilePath[Slot];
            _origSelected = _repo.SelectedProfile[Slot];
            _origUseTemp = _settings.GetUseTempProfile(Slot);
            _origTempName = _settings.GetTempProfileName(Slot);
            _origDistance = Global.tempprofileDistance[Slot];

            // 読み込める有効なプロファイルを、保存の経路で作る（保存先は退避した appdatapath 配下）
            Assert.True(TestRepositoryFactory.CreateLegacyProfileXml().SaveProfile(Slot, Normal));
            Assert.True(TestRepositoryFactory.CreateLegacyProfileXml().SaveProfile(Slot, Temp));
            Assert.True(TestRepositoryFactory.CreateLegacyProfileXml().SaveProfile(Slot, Distance));

            _handler = (s, e) => _events.Add(e);
            _repo.SelectedProfileChanged += _handler;
        }

        public void Dispose()
        {
            _repo.SelectedProfileChanged -= _handler;
            _repo.ProfilePath[Slot] = _origProfilePath;
            _repo.OlderProfilePath[Slot] = _origOlder;
            _repo.SelectedProfile[Slot] = _origSelected;
            _settings.SetUseTempProfile(Slot, _origUseTemp);
            _settings.SetTempProfileName(Slot, _origTempName);
            Global.tempprofileDistance[Slot] = _origDistance;
            Global.appdatapath = _originalAppData;
            try { Directory.Delete(_dir, true); } catch { }
        }

        // 入口: false＝ApplyProfileWithoutHalt、true＝ApplyProfile（Halt あり。この環境では Slot 0 に実機が無いため、Halt せずに適用する）
        private bool Apply(bool withHalt, string name, bool isTemp, ProfileChangeSource source)
            => withHalt
                ? _service.ApplyProfile(Slot, name, isTemp, false, source, "prolog")
                : _service.ApplyProfileWithoutHalt(Slot, name, isTemp, false, source, "prolog");

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ApplyProfile_Normal_UpdatesSelectionAndRaisesEventOnce(bool withHalt)
        {
            _repo.OlderProfilePath[Slot] = "OlderSentinel";
            _repo.SelectedProfile[Slot] = "SelectedSentinel";

            bool result = Apply(withHalt, Normal, false, ProfileChangeSource.Manual);

            Assert.True(result);
            Assert.Equal(Normal, _repo.ProfilePath[Slot]);
            Assert.Equal(Normal, _repo.SelectedProfile[Slot]);
            Assert.Equal(Normal, _repo.OlderProfilePath[Slot]);
            Assert.False(_settings.GetUseTempProfile(Slot));
            Assert.Equal(string.Empty, _settings.GetTempProfileName(Slot));
            Assert.False(Global.tempprofileDistance[Slot]);

            Assert.Single(_events);
            Assert.Equal(Slot, _events[0].DeviceIndex);
            Assert.Equal(Normal, _events[0].ProfileName);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ApplyProfile_Temp_KeepsOlderProfilePath_AndMarksTempState(bool withHalt)
        {
            _repo.OlderProfilePath[Slot] = "OlderSentinel";
            _repo.ProfilePath[Slot] = "ProfilePathSentinel";

            bool result = Apply(withHalt, Temp, true, ProfileChangeSource.MappingAction);

            Assert.True(result);
            // 一時プロファイルでは、復帰先の OlderProfilePath と通常の ProfilePath は更新されない
            Assert.Equal("OlderSentinel", _repo.OlderProfilePath[Slot]);
            Assert.Equal("ProfilePathSentinel", _repo.ProfilePath[Slot]);
            Assert.Equal(Temp, _repo.SelectedProfile[Slot]);
            Assert.True(_settings.GetUseTempProfile(Slot));
            Assert.Equal(Temp, _settings.GetTempProfileName(Slot));
            Assert.False(Global.tempprofileDistance[Slot]);

            Assert.Single(_events);
            Assert.Equal(Temp, _events[0].ProfileName);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ApplyProfile_TempWithDistanceInName_SetsDistanceFlag(bool withHalt)
        {
            bool result = Apply(withHalt, Distance, true, ProfileChangeSource.MappingAction);

            Assert.True(result);
            Assert.True(Global.tempprofileDistance[Slot]);

            // 通常プロファイルを適用すると、一時プロファイルの状態と距離フラグが戻る
            Assert.True(Apply(withHalt, Normal, false, ProfileChangeSource.Manual));
            Assert.False(_settings.GetUseTempProfile(Slot));
            Assert.Equal(string.Empty, _settings.GetTempProfileName(Slot));
            Assert.False(Global.tempprofileDistance[Slot]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ApplyProfile_MissingProfile_ReturnsFalse_AndKeepsSelectionAndRaisesNothing(bool withHalt)
        {
            _repo.SelectedProfile[Slot] = "SelectedSentinel";
            _repo.OlderProfilePath[Slot] = "OlderSentinel";

            bool result = Apply(withHalt, Missing, false, ProfileChangeSource.Manual);

            Assert.False(result);
            Assert.Equal("SelectedSentinel", _repo.SelectedProfile[Slot]);
            Assert.Equal("OlderSentinel", _repo.OlderProfilePath[Slot]);
            Assert.Empty(_events);
            // 通常プロファイルでは、読み込みの前に ProfilePath が更新される（失敗しても戻さない。従来の動作）
            Assert.Equal(Missing, _repo.ProfilePath[Slot]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ApplyProfile_MissingTempProfile_ReturnsFalse_AndKeepsTempState(bool withHalt)
        {
            _settings.SetUseTempProfile(Slot, false);
            _settings.SetTempProfileName(Slot, string.Empty);
            _repo.SelectedProfile[Slot] = "SelectedSentinel";

            bool result = Apply(withHalt, Missing, true, ProfileChangeSource.MappingAction);

            Assert.False(result);
            Assert.False(_settings.GetUseTempProfile(Slot));
            Assert.Equal(string.Empty, _settings.GetTempProfileName(Slot));
            Assert.Equal("SelectedSentinel", _repo.SelectedProfile[Slot]);
            Assert.Empty(_events);
        }

        [Fact]
        public void LoadTempProfile_Success_SetsTempState_AndDoesNotTouchSelection()
        {
            _repo.SelectedProfile[Slot] = "SelectedSentinel";

            bool result = _repo.LoadTempProfile(Slot, Temp, false, _control);

            Assert.True(result);
            Assert.True(_settings.GetUseTempProfile(Slot));
            Assert.Equal(Temp, _settings.GetTempProfileName(Slot));
            Assert.Equal("SelectedSentinel", _repo.SelectedProfile[Slot]);
            Assert.Empty(_events);
        }

        [Fact]
        public void ApplyProfileWithoutHalt_RejectsInvalidSlotOrName_AndDoesNotChangeState()
        {
            _repo.SelectedProfile[Slot] = "SelectedSentinel";

            Assert.False(_service.ApplyProfileWithoutHalt(-1, Normal));
            Assert.False(_service.ApplyProfileWithoutHalt(Slot, ""));
            Assert.False(_service.ApplyProfileWithoutHalt(Slot, "   "));
            Assert.False(_service.ApplyProfileWithoutHalt(Slot, null));

            Assert.Equal("SelectedSentinel", _repo.SelectedProfile[Slot]);
            Assert.Empty(_events);
        }

        [Fact]
        public void ProfileSlotApplier_RejectsInvalidSlotOrName_AndDoesNotChangeState()
        {
            _repo.SelectedProfile[Slot] = "SelectedSentinel";

            Assert.False(_slotApplier.ApplyToSlot(-1, Normal, ProfileChangeSource.Manual));
            Assert.False(_slotApplier.ApplyToSlot(ControlService.CURRENT_DS4_CONTROLLER_LIMIT, Normal, ProfileChangeSource.Manual));
            Assert.False(_slotApplier.ApplyToSlot(Slot, "", ProfileChangeSource.Manual));
            Assert.False(_slotApplier.ApplyToSlot(Slot, "   ", ProfileChangeSource.Manual));
            Assert.False(_slotApplier.ApplyToSlot(Slot, null, ProfileChangeSource.Manual));

            Assert.Equal("SelectedSentinel", _repo.SelectedProfile[Slot]);
            Assert.Empty(_events);
        }

        [Fact]
        public void ProfileSlotApplier_Valid_AppliesNormalProfile_AndRaisesEventOnce()
        {
            bool result = _slotApplier.ApplyToSlot(Slot, Normal, ProfileChangeSource.ControlService);

            Assert.True(result);
            Assert.Equal(Normal, _repo.ProfilePath[Slot]);
            Assert.Equal(Normal, _repo.SelectedProfile[Slot]);
            Assert.Equal(Normal, _repo.OlderProfilePath[Slot]);
            Assert.False(_settings.GetUseTempProfile(Slot));
            Assert.Single(_events);
            Assert.Equal(Normal, _events[0].ProfileName);
        }

        [Fact]
        public void PresetLoadBlank_ResetsTempProfileState()
        {
            _settings.SetUseTempProfile(Slot, true);
            _settings.SetTempProfileName(Slot, "TempSentinel");
            Global.tempprofileDistance[Slot] = true;

            // 旧 Global.LoadBlankDevProfile の処理は、Phase7-Step3c W2 で ProfilePresetLoader に移した
            TestRepositoryFactory.CreateProfilePresetLoader().LoadBlank(Slot, false, _control, false, false);

            Assert.False(_settings.GetUseTempProfile(Slot));
            Assert.Equal(string.Empty, _settings.GetTempProfileName(Slot));
            Assert.False(Global.tempprofileDistance[Slot]);
        }
    }
}
