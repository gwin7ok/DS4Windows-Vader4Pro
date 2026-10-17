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

    public class JoyConControllerOptionsWrapper
    {
        private JoyConControllerOptions options;
        public JoyConControllerOptions Options { get => options; }

        private JoyConDeviceOptions parentOptions;
        public JoyConDeviceOptions ParentOptions { get => parentOptions; }

        public bool Visible { get => parentOptions.Enabled; }
        public event EventHandler VisibleChanged;

        private List<EnumChoiceSelection<LinkMode>> linkModes = new List<EnumChoiceSelection<LinkMode>>()
        {
            new EnumChoiceSelection<LinkMode>("Split", LinkMode.Split),
            new EnumChoiceSelection<LinkMode>("Joined", LinkMode.Joined),
        };
        public List<EnumChoiceSelection<LinkMode>> LinkModes { get => linkModes; }

        private List<EnumChoiceSelection<JoinedGyroProvider>> joinGyroOptions = new List<EnumChoiceSelection<JoinedGyroProvider>>()
        {
            new EnumChoiceSelection<JoinedGyroProvider>("Left", JoinedGyroProvider.JoyConL),
            new EnumChoiceSelection<JoinedGyroProvider>("Right", JoinedGyroProvider.JoyConR),
        };
        public List<EnumChoiceSelection<JoinedGyroProvider>> JoinGyroOptions { get => joinGyroOptions; }

        public JoyConControllerOptionsWrapper(JoyConControllerOptions options,
            JoyConDeviceOptions parentOpts)
        {
            this.options = options;
            this.parentOptions = parentOpts;
            parentOptions.EnabledChanged += (sender, e) => { VisibleChanged?.Invoke(this, EventArgs.Empty); };
        }
    }

}
