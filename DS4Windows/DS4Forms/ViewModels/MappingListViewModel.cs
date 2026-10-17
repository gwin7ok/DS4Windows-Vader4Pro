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

using DS4Windows;
using DS4Windows.DI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    public class MappingListViewModel
    {
        //private int devIndex;
        private ObservableCollection<MappedControl> mappings = new ObservableCollection<MappedControl>();
        public ObservableCollection<MappedControl> Mappings { get => mappings; }

        private int selectedIndex = -1;
        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                if (selectedIndex == value) return;
                selectedIndex = value;
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler SelectedIndexChanged;

        private Dictionary<DS4Controls, MappedControl> controlMap =
            new Dictionary<DS4Controls, MappedControl>();
        public Dictionary<DS4Controls, MappedControl> ControlMap { get => controlMap; }

        /// <summary>
        /// DS4Controls -> Int index map. Store approriate list index for a stored MappedControl instance
        /// </summary>
        private Dictionary<DS4Controls, int> controlIndexMap =
            new Dictionary<DS4Controls, int>();
        public Dictionary<DS4Controls, int> ControlIndexMap { get => controlIndexMap; }

        private MappedControl l2FullPullControl;
        public MappedControl L2FullPullControl { get => l2FullPullControl; }

        private MappedControl r2FullPullControl;
        public MappedControl R2FullPullControl { get => r2FullPullControl; }

        private MappedControl lsOuterBindControl;
        public MappedControl LsOuterBindControl { get => lsOuterBindControl; }

        private MappedControl rsOuterBindControl;
        public MappedControl RsOuterBindControl { get => rsOuterBindControl; }

        private MappedControl gyroSwipeLeftControl;
        private MappedControl gyroSwipeRightControl;
        private MappedControl gyroSwipeUpControl;
        private MappedControl gyroSwipeDownControl;

        private List<MappedControl> extraControls = new List<MappedControl>();

        public MappingListViewModel(int devIndex, OutContType devType, IProfileSettingsService profileSettingsService)
        {
            if (profileSettingsService == null) throw new ArgumentNullException(nameof(profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.Cross, "Cross", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.Circle, "Circle", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.Square, "Square", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.Triangle, "Triangle", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.Options, "Options", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.Share, "Share", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.DpadUp, "Up", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.DpadDown, "Down", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.DpadLeft, "Left", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.DpadRight, "Right", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.PS, "PS", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.Mute, "Mute", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.L1, "L1", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.R1, "R1", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.L2, "L2", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.R2, "R2", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.L3, "L3", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.R3, "R3", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.Capture, "Capture", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.SideL, "Side L", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.SideR, "Side R", devType, profileSettingsService));

            mappings.Add(new MappedControl(devIndex, DS4Controls.TouchLeft, "Left Touch", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.TouchRight, "Right Touch", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.TouchMulti, "Multitouch", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.TouchUpper, "Upper Touch", devType, profileSettingsService));

            mappings.Add(new MappedControl(devIndex, DS4Controls.LYNeg, "LS Up", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.LYPos, "LS Down", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.LXNeg, "LS Left", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.LXPos, "LS Right", devType, profileSettingsService));

            mappings.Add(new MappedControl(devIndex, DS4Controls.RYNeg, "RS Up", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.RYPos, "RS Down", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.RXNeg, "RS Left", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.RXPos, "RS Right", devType, profileSettingsService));

            mappings.Add(new MappedControl(devIndex, DS4Controls.GyroZNeg, "Tilt Up", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.GyroZPos, "Tilt Down", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.GyroXPos, "Tilt Left", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.GyroXNeg, "Tilt Right", devType, profileSettingsService));

            mappings.Add(new MappedControl(devIndex, DS4Controls.SwipeUp, "Swipe Up", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.SwipeDown, "Swipe Down", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.SwipeLeft, "Swipe Left", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.SwipeRight, "Swipe Right", devType, profileSettingsService));

            mappings.Add(new MappedControl(devIndex, DS4Controls.FnL, "Function Left", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.FnR, "Function Right", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.BLP, "Bottom Left Paddle", devType, profileSettingsService));
            mappings.Add(new MappedControl(devIndex, DS4Controls.BRP, "Bottom Right Paddle", devType, profileSettingsService));

            int controlIndex = 0;
            foreach (MappedControl mapped in mappings)
            {
                controlMap.Add(mapped.Control, mapped);
                controlIndexMap.Add(mapped.Control, controlIndex);
                controlIndex++;
            }

            /*
             * Establish data binding data for virtual button DS4ControlSettings instances
             */
            lsOuterBindControl = new MappedControl(devIndex, DS4Controls.LSOuter, "LS Outer", devType, profileSettingsService);
            rsOuterBindControl = new MappedControl(devIndex, DS4Controls.RSOuter, "RS Outer", devType, profileSettingsService);

            l2FullPullControl = new MappedControl(devIndex, DS4Controls.L2FullPull, "L2 Full Pull", devType, profileSettingsService);
            r2FullPullControl = new MappedControl(devIndex, DS4Controls.R2FullPull, "R2 Full Pull", devType, profileSettingsService);

            gyroSwipeLeftControl = new MappedControl(devIndex, DS4Controls.GyroSwipeLeft, "Gyro Swipe Left", devType, profileSettingsService);
            gyroSwipeRightControl = new MappedControl(devIndex, DS4Controls.GyroSwipeRight, "Gyro Swipe Right", devType, profileSettingsService);
            gyroSwipeUpControl = new MappedControl(devIndex, DS4Controls.GyroSwipeUp, "Gyro Swipe Up", devType, profileSettingsService);
            gyroSwipeDownControl = new MappedControl(devIndex, DS4Controls.GyroSwipeDown, "Gyro Swipe Down", devType, profileSettingsService);

            extraControls.Add(lsOuterBindControl);
            extraControls.Add(rsOuterBindControl);
            extraControls.Add(l2FullPullControl);
            extraControls.Add(r2FullPullControl);
            extraControls.Add(gyroSwipeLeftControl);
            extraControls.Add(gyroSwipeRightControl);
            extraControls.Add(gyroSwipeUpControl);
            extraControls.Add(gyroSwipeDownControl);

            controlMap.Add(DS4Controls.LSOuter, lsOuterBindControl);
            controlMap.Add(DS4Controls.RSOuter, rsOuterBindControl);
            controlMap.Add(DS4Controls.L2FullPull, l2FullPullControl);
            controlMap.Add(DS4Controls.R2FullPull, r2FullPullControl);
            controlMap.Add(DS4Controls.GyroSwipeLeft, gyroSwipeLeftControl);
            controlMap.Add(DS4Controls.GyroSwipeRight, gyroSwipeRightControl);
            controlMap.Add(DS4Controls.GyroSwipeUp, gyroSwipeUpControl);
            controlMap.Add(DS4Controls.GyroSwipeDown, gyroSwipeDownControl);
        }

        public void UpdateMappingDevType(OutContType devType)
        {
            foreach (MappedControl mapped in mappings)
            {
                mapped.DevType = devType;
            }

            foreach (MappedControl mapped in extraControls)
            {
                mapped.DevType = devType;
            }
        }

        public void UpdateMappings()
        {
            foreach (MappedControl mapped in mappings)
            {
                mapped.UpdateMappingName();
            }

            foreach (MappedControl mapped in extraControls)
            {
                mapped.UpdateMappingName();
            }
        }
    }
}
