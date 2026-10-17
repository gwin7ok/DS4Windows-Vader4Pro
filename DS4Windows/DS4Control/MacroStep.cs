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
using System.Windows.Input;

namespace DS4Windows
{

    public class MacroStep
    {
        public const int WAIT_OFFSET = 300;

        public enum StepType : uint
        {
            ActDown,
            ActUp,
            Wait,
        }

        public enum StepOutput : uint
        {
            None,
            Key,
            Button,
            Rumble,
            Lightbar,
        }

        private string name;
        private int value;
        private StepType actType;
        private StepOutput outputType;

        public string Name
        {
            get => name;
            set
            {
                name = value;
                NameChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler NameChanged;
        public int Value
        {
            get => value;
            set
            {
                this.value = value;
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler ValueChanged;
        public StepType ActType { get => actType; }
        public StepOutput OutputType { get => outputType; }

        public MacroStep(int value, string name, StepType act, StepOutput output)
        {
            this.value = value;
            this.name = name;
            actType = act;
            outputType = output;

            ValueChanged += MacroStep_ValueChanged;
        }

        private void MacroStep_ValueChanged(object sender, EventArgs e)
        {
            if (actType == StepType.Wait)
            {
                Name = $"Wait {value - 300}ms";
            }
            else if (outputType == StepOutput.Rumble)
            {
                int result = value;
                result -= 1000000;
                int curHeavy = result / 1000;
                int curLight = result - (curHeavy * 1000);
                Name = $"Rumble {curHeavy},{curLight}";
            }
            else if (outputType == StepOutput.Lightbar)
            {
                int temp = value - 1000000000;
                int r = temp / 1000000;
                temp -= (r * 1000000);
                int g = temp / 1000;
                temp -= (g * 1000);
                int b = temp;
                Name = $"Lightbar Color: {r},{g},{b}";
            }
        }
    }
}
