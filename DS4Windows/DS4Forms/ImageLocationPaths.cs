using DS4Windows.DI;
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System.Windows.Interop;
using System.Diagnostics;
using System.IO;
using System.Management;
using NonFormTimer = System.Timers.Timer;
using System.Runtime.InteropServices;
using System.ComponentModel;
using HttpProgress;

using DS4WinWPF.DS4Forms.ViewModels;
using DS4Windows;
using DS4WinWPF.DS4Control;
using DS4WinWPF.Translations;


namespace DS4WinWPF.DS4Forms
{

    public class ImageLocationPaths
    {
        public string NewProfile { get => $"{ResourcePaths.ResourcesPrefix}/{App.Current.FindResource("NewProfileImg")}"; }
        public event EventHandler NewProfileChanged;

        public string EditProfile { get => $"{ResourcePaths.ResourcesPrefix}/{App.Current.FindResource("EditImg")}"; }
        public event EventHandler EditProfileChanged;

        public string DeleteProfile { get => $"{ResourcePaths.ResourcesPrefix}/{App.Current.FindResource("DeleteImg")}"; }
        public event EventHandler DeleteProfileChanged;

        public string DuplicateProfile { get => $"{ResourcePaths.ResourcesPrefix}/{App.Current.FindResource("CopyImg")}"; }
        public event EventHandler DuplicateProfileChanged;

        public string ExportProfile { get => $"{ResourcePaths.ResourcesPrefix}/{App.Current.FindResource("ExportImg")}"; }
        public event EventHandler ExportProfileChanged;

        public string ImportProfile { get => $"{ResourcePaths.ResourcesPrefix}/{App.Current.FindResource("ImportImg")}"; }
        public event EventHandler ImportProfileChanged;

        public ImageLocationPaths()
        {
            App current = App.Current as App;
            if (current != null)
            {
                current.ThemeChanged += Current_ThemeChanged;
            }
        }

        private void Current_ThemeChanged(object sender, EventArgs e)
        {
            NewProfileChanged?.Invoke(this, EventArgs.Empty);
            EditProfileChanged?.Invoke(this, EventArgs.Empty);
            DeleteProfileChanged?.Invoke(this, EventArgs.Empty);
            DuplicateProfileChanged?.Invoke(this, EventArgs.Empty);
            ExportProfileChanged?.Invoke(this, EventArgs.Empty);
            ImportProfileChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}