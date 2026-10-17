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
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels.Util;
using LEDBarMode = DS4Windows.DualSenseControllerOptions.LEDBarMode;
using MuteLEDMode = DS4Windows.DualSenseControllerOptions.MuteLEDMode;
using LinkMode = DS4Windows.JoyConDeviceOptions.LinkMode;
using JoinedGyroProvider = DS4Windows.JoyConDeviceOptions.JoinedGyroProvider;

namespace DS4WinWPF.DS4Forms.ViewModels
{

    public class DualSenseControllerOptionsWrapper
    {
        private DualSenseControllerOptions options;
        public DualSenseControllerOptions Options { get => options; }

        private DualSenseDeviceOptions parentOptions;
        public bool Visible { get => parentOptions.Enabled; }
        public event EventHandler VisibleChanged;

        private List<EnumChoiceSelection<LEDBarMode>> dsLEDModeOptions = new List<EnumChoiceSelection<LEDBarMode>>()
        {
            new EnumChoiceSelection<LEDBarMode>("Off", LEDBarMode.Off),
            new EnumChoiceSelection<LEDBarMode>("Only for multiple controllers", LEDBarMode.MultipleControllers),
            new EnumChoiceSelection<LEDBarMode>("Battery Percentage", LEDBarMode.BatteryPercentage),
            new EnumChoiceSelection<LEDBarMode>("On", LEDBarMode.On),
        };
        public List<EnumChoiceSelection<LEDBarMode>> DsLEDModes { get => dsLEDModeOptions; }

        private List<EnumChoiceSelection<MuteLEDMode>> dsMuteLEDModes = new List<EnumChoiceSelection<MuteLEDMode>>()
        {
            new EnumChoiceSelection<MuteLEDMode>("Off", MuteLEDMode.Off),
            new EnumChoiceSelection<MuteLEDMode>("On", MuteLEDMode.On),
            new EnumChoiceSelection<MuteLEDMode>("Pulse", MuteLEDMode.Pulse),
        };
        public List<EnumChoiceSelection<MuteLEDMode>> DsMuteLEDModes { get => dsMuteLEDModes; }

        public DualSenseControllerOptionsWrapper(DualSenseControllerOptions options,
            DualSenseDeviceOptions parentOpts)
        {
            this.options = options;
            this.parentOptions = parentOpts;
            parentOptions.EnabledChanged += (sender, e) => { VisibleChanged?.Invoke(this, EventArgs.Empty); };
        }
    }

}
