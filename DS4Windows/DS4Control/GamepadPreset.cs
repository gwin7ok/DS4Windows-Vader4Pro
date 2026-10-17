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

    public class GamepadPreset : PresetOption
    {
        public GamepadPreset(DS4Windows.DI.IProfilePresetLoader presetLoader, DS4Windows.ControlService controlService)
            : base(presetLoader, controlService)
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
                presetLoader.LoadBlank(idx, false, controlService, false);
            }
            else if (outputCont == OutputContChoice.DualShock4)
            {
                presetLoader.LoadBlankDS4(idx, false, controlService, false);
            }
        }
    }
}