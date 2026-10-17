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
    // ==========================================
    // パターン A: ProfileActions および Sensitivity
    // ==========================================

    public class ProfileActions
    {
        public const int MAX_ACTIONS = 6;
        public string[] actions = new string[MAX_ACTIONS];
        public string[] actionExtras = new string[MAX_ACTIONS];

        public event EventHandler ActionsChanged;

        public string GetAction(int index) => index >= 0 && index < MAX_ACTIONS ? actions[index] : string.Empty;

        public void SetAction(int index, string value)
        {
            if (index < 0 || index >= MAX_ACTIONS) return;
            if (actions[index] == value) return;
            actions[index] = value;
            ActionsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Reset()
        {
            Array.Clear(actions, 0, actions.Length);
            Array.Clear(actionExtras, 0, actionExtras.Length);
            ActionsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}