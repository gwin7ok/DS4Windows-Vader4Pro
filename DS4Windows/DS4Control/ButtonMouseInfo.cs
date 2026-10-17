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
using DS4Windows.StickModifiers;
using DS4WinWPF.DS4Control;
using Sensorit.Base;
using DS4Windows.InputDevices;

namespace DS4Windows
{

    public class ButtonMouseInfo
    {
        public const double MOUSESTICKANTIOFFSET = 0.008;
        public const int DEFAULT_BUTTON_SENS = 25;
        public const double DEFAULT_BUTTON_VERTICAL_SCALE = 1.0;
        public const int DEFAULT_TEMP_SENS = -1;

        public int buttonSensitivity = DEFAULT_BUTTON_SENS;
        public int ButtonSensitivity
        {
            get => buttonSensitivity;
            set
            {
                if (buttonSensitivity == value) return;
                buttonSensitivity = value;
                ButtonMouseInfoChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler ButtonMouseInfoChanged;

        public bool mouseAccel;
        public int activeButtonSensitivity = DEFAULT_BUTTON_SENS;
        public int tempButtonSensitivity = DEFAULT_TEMP_SENS;
        public double mouseVelocityOffset = MOUSESTICKANTIOFFSET;
        public double buttonVerticalScale = DEFAULT_BUTTON_VERTICAL_SCALE;

        public ButtonMouseInfo()
        {
            ButtonMouseInfoChanged += ButtonMouseInfo_ButtonMouseInfoChanged;
        }

        private void ButtonMouseInfo_ButtonMouseInfoChanged(object sender, EventArgs e)
        {
            if (tempButtonSensitivity == DEFAULT_TEMP_SENS)
            {
                activeButtonSensitivity = buttonSensitivity;
            }
        }

        public void SetActiveButtonSensitivity(int sens)
        {
            activeButtonSensitivity = sens;
        }

        public void Reset()
        {
            buttonSensitivity = DEFAULT_BUTTON_SENS;
            mouseAccel = false;
            activeButtonSensitivity = DEFAULT_BUTTON_SENS;
            tempButtonSensitivity = DEFAULT_TEMP_SENS;
            mouseVelocityOffset = MOUSESTICKANTIOFFSET;
            buttonVerticalScale = DEFAULT_BUTTON_VERTICAL_SCALE;
        }
    }
}