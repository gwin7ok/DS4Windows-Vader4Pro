using System;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// <see cref="IProfilePresetLoader"/> の実装。プリセットを読み込み、既定のスペシャルアクションの作成、エクストラ情報のキャッシュ更新、
    /// 一時プロファイルの状態（名前・使用・距離）のクリアまでを行う。
    /// Phase7-Step3c W2: 旧 <c>Global.Load…Profile</c>（10 名）の処理をここへ移した。<c>Global</c> 経由をやめ、
    /// 注入された <see cref="BackingStore"/> と <see cref="IProfileSettingsService"/> を使う。
    /// </summary>
    public class ProfilePresetLoader : IProfilePresetLoader
    {
        private readonly BackingStore _config;
        private readonly IProfileSettingsService _profileSettings;
        // Phase7-Step10c W6: プリセットの読込（旧: BackingStore.LoadBlank*／LoadDefault*）
        private readonly ProfilePresets _presets;

        public ProfilePresetLoader(BackingStore config, IProfileSettingsService profileSettings, ProfilePresets presets)
        {
            _presets = presets ?? throw new ArgumentNullException(nameof(presets));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
        }

        // どのプリセットでも、読み込みの後に同じ後処理を行う（旧 Global.Load…Profile の共通部分）。
        private void CompleteLoad(int deviceIndex)
        {
            _config.EstablishDefaultSpecialActions(deviceIndex);
            _config.CacheExtraProfileInfo(deviceIndex);

            _profileSettings.SetTempProfileName(deviceIndex, string.Empty);
            _profileSettings.SetUseTempProfile(deviceIndex, false);
            _profileSettings.SetTempProfileDistance(deviceIndex, false);
        }

        public void LoadBlank(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true)
        {
            _presets.LoadBlankProfile(deviceIndex, launchProgram, control, "", xinputChange, postLoad);
            CompleteLoad(deviceIndex);
        }

        public void LoadBlankDS4(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true)
        {
            _presets.LoadBlankDS4Profile(deviceIndex, launchProgram, control, "", xinputChange, postLoad);
            CompleteLoad(deviceIndex);
        }

        public void LoadDefaultGamepadGyro(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true)
        {
            _presets.LoadDefaultGamepadGyroProfile(deviceIndex, launchProgram, control, "", xinputChange, postLoad);
            CompleteLoad(deviceIndex);
        }

        public void LoadDefaultDS4GamepadGyro(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true)
        {
            _presets.LoadDefaultDS4GamepadGyroProfile(deviceIndex, launchProgram, control, "", xinputChange, postLoad);
            CompleteLoad(deviceIndex);
        }

        public void LoadDefaultMixedControls(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true)
        {
            _presets.LoadDefaultMixedControlsProfile(deviceIndex, launchProgram, control, "", xinputChange, postLoad);
            CompleteLoad(deviceIndex);
        }

        public void LoadDefaultDS4MixedControls(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true)
        {
            _presets.LoadDefaultDS4MixedControlsProfile(deviceIndex, launchProgram, control, "", xinputChange, postLoad);
            CompleteLoad(deviceIndex);
        }

        public void LoadDefaultMixedGyroMouse(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true)
        {
            _presets.LoadDefaultMixedGyroMouseProfile(deviceIndex, launchProgram, control, "", xinputChange, postLoad);
            CompleteLoad(deviceIndex);
        }

        public void LoadDefaultDS4MixedGyroMouse(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true)
        {
            _presets.LoadDefaultDS4MixedGyroMouseProfile(deviceIndex, launchProgram, control, "", xinputChange, postLoad);
            CompleteLoad(deviceIndex);
        }

        public void LoadDefaultKBM(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true)
        {
            _presets.LoadDefaultKBMProfile(deviceIndex, launchProgram, control, "", xinputChange, postLoad);
            CompleteLoad(deviceIndex);
        }

        public void LoadDefaultKBMGyroMouse(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true)
        {
            _presets.LoadDefaultKBMGyroMouseProfile(deviceIndex, launchProgram, control, "", xinputChange, postLoad);
            CompleteLoad(deviceIndex);
        }
    }
}
