/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS4WinWPF.DS4Control
{
    public abstract class PresetOption
    {
        public enum OutputContChoice : ushort
        {
            None,
            Xbox360,
            DualShock4,
        }

        protected string name;
        protected string description;
        protected bool outputControllerChoice;
        protected OutputContChoice outputCont;
        // Phase5-Step13-7: 全サブクラス共通のControlService注入(Program.rootHubフォールバック)。
        // これらのPresetクラスはPresetOptionViewModelで new() 経由でパラメータレスに生成されるため、
        // サブクラス側の変更は不要(既定のフォールバック付きコンストラクタが暗黙的に呼ばれる)。
        protected readonly DS4Windows.ControlService controlService;

        public string Name { get => name; }
        public string Description { get => description; }
        public bool OutputControllerChoice { get => outputControllerChoice; }
        public OutputContChoice OutputCont
        {
            get => outputCont;
            set => outputCont = value;
        }

        protected PresetOption(DS4Windows.ControlService controlService = null)
        {
            this.controlService = controlService ?? DS4Windows.Program.rootHub;
        }

        public abstract void ApplyPreset(int idx);
    }

    public class GamepadPreset : PresetOption
    {
        public GamepadPreset()
        {
            name = Translations.Strings.GamepadPresetName;
            description = Translations.Strings.GamepadPresetDescription;
            outputControllerChoice = true;
            outputCont = OutputContChoice.Xbox360;
        }

        public override void ApplyPreset(int idx)
        {
            if (outputCont == OutputContChoice.Xbox360)
            {
                DS4Windows.Global.LoadBlankDevProfile(idx, false, controlService, false);
            }
            else if (outputCont == OutputContChoice.DualShock4)
            {
                DS4Windows.Global.LoadBlankDS4Profile(idx, false, controlService, false);
            }
        }
    }

    public class GamepadGyroCamera : PresetOption
    {
        public GamepadGyroCamera()
        {
            name = Translations.Strings.GamepadGyroCameraName;
            description = Translations.Strings.GamepadGyroCameraDescription;
            outputControllerChoice = true;
            outputCont = OutputContChoice.Xbox360;
        }

        public override void ApplyPreset(int idx)
        {
            if (outputCont == OutputContChoice.Xbox360)
            {
                DS4Windows.Global.LoadDefaultGamepadGyroProfile(idx, false, controlService, false);
            }
            else if (outputCont == OutputContChoice.DualShock4)
            {
                DS4Windows.Global.LoadDefaultDS4GamepadGyroProfile(idx, false, controlService, false);
            }
        }
    }

    public class MixedPreset : PresetOption
    {
        public MixedPreset()
        {
            name = Translations.Strings.MixedPresetName;
            description = Translations.Strings.MixedPresetDescription;
            outputControllerChoice = true;
            outputCont = OutputContChoice.Xbox360;
        }

        public override void ApplyPreset(int idx)
        {
            if (outputCont == OutputContChoice.Xbox360)
            {
                DS4Windows.Global.LoadDefaultMixedControlsProfile(idx, false, controlService, false);
            }
            else if (outputCont == OutputContChoice.DualShock4)
            {
                DS4Windows.Global.LoadDefaultDS4MixedControlsProfile(idx, false, controlService, false);
            }
        }
    }

    public class MixedGyroMousePreset : PresetOption
    {
        public MixedGyroMousePreset()
        {
            name = Translations.Strings.MixedGyroMousePresetName;
            description = Translations.Strings.MixedGyroMousePresetDescription;
            outputControllerChoice = true;
            outputCont = OutputContChoice.Xbox360;
        }

        public override void ApplyPreset(int idx)
        {
            if (outputCont == OutputContChoice.Xbox360)
            {
                DS4Windows.Global.LoadDefaultMixedGyroMouseProfile(idx, false, controlService, false);
            }
            else if (outputCont == OutputContChoice.DualShock4)
            {
                DS4Windows.Global.LoadDefaultDS4MixedGyroMouseProfile(idx, false, controlService, false);
            }
        }
    }

    public class KBMPreset : PresetOption
    {
        public KBMPreset()
        {
            name = Translations.Strings.KBMPresetName;
            description = Translations.Strings.KBMPresetDescription;
        }

        public override void ApplyPreset(int idx)
        {
            DS4Windows.Global.LoadDefaultKBMProfile(idx, false, controlService, false);
        }
    }

    public class KBMGyroMouse : PresetOption
    {
        public KBMGyroMouse()
        {
            name = Translations.Strings.KBMGyroMouseName;
            description = Translations.Strings.KBMGyroMouseDescription;
        }

        public override void ApplyPreset(int idx)
        {
            DS4Windows.Global.LoadDefaultKBMGyroMouseProfile(idx, false, controlService, false);
        }
    }
}