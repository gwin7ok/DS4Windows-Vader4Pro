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
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.StickModifiers;
using DS4WinWPF.DS4Forms.ViewModels.Util;
using DS4Windows.InputDevices;

namespace DS4WinWPF.DS4Forms.ViewModels
{

    public class TriggerEffectChoice
    {
        private string displayName;
        public string DisplayName { get => displayName; set => displayName = value; }


        private DS4Windows.InputDevices.TriggerEffects mode;
        public DS4Windows.InputDevices.TriggerEffects Mode { get => mode; set => mode = value; }

        public TriggerEffectChoice(string name, DS4Windows.InputDevices.TriggerEffects mode)
        {
            this.displayName = name;
            this.mode = mode;
        }
    }
}