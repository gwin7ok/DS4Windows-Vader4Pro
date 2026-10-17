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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WinWPF.DS4Forms.ViewModels
{

    public class MacroStepItem
    {
        private static string[] imageSources = new string[]
        {
            $"{ResourcePaths.ResourcesPrefix}/{(string)App.Current.FindResource("KeyDownImg")}",
            $"{ResourcePaths.ResourcesPrefix}/{(string)App.Current.FindResource("KeyUpImg")}",
            $"{ResourcePaths.ResourcesPrefix}/{(string)App.Current.FindResource("ClockImg")}",
        };

        public static void CacheImgLocations()
        {
            imageSources = new string[]
            {
                $"{ResourcePaths.ResourcesPrefix}/{(string)App.Current.FindResource("KeyDownImg")}",
                $"{ResourcePaths.ResourcesPrefix}/{(string)App.Current.FindResource("KeyUpImg")}",
                $"{ResourcePaths.ResourcesPrefix}/{(string)App.Current.FindResource("ClockImg")}",
            };
        }

        private MacroStep step;
        private string image;

        public string Image { get => image; }
        public MacroStep Step { get => step; }
        public int DisplayValue
        {
            get
            {
                int result = step.Value;
                if (step.ActType == MacroStep.StepType.Wait)
                {
                    result -= 300;
                }

                return result;
            }
            set
            {
                int result = value;
                if (step.ActType == MacroStep.StepType.Wait)
                {
                    result += 300;
                }

                step.Value = result;
            }
        }

        public int RumbleHeavy
        {
            get
            {
                int result = step.Value;
                result -= 1000000;
                string temp = result.ToString();
                result = int.Parse(temp.Substring(0, 3));
                return result;
            }
            set
            {
                int result = step.Value;
                result -= 1000000;
                int curHeavy = result / 1000;
                int curLight = result - (curHeavy * 1000);
                result = curLight + (value * 1000) + 1000000;
                step.Value = result;
            }
        }

        public int RumbleLight
        {
            get
            {
                int result = step.Value;
                result -= 1000000;
                string temp = result.ToString();
                result = int.Parse(temp.Substring(3, 3));
                return result;
            }
            set
            {
                int result = step.Value;
                result -= 1000000;
                int curHeavy = result / 1000;
                result = value + (curHeavy * 1000) + 1000000;
                step.Value = result;
            }
        }

        public MacroStepItem(MacroStep step)
        {
            this.step = step;
            image = imageSources[(int)step.ActType];
        }

        public void UpdateLightbarValue(Color color)
        {
            step.Value = 1000000000 + (color.R * 1000000) + (color.G * 1000) + color.B;
        }

        public Color LightbarColorValue()
        {
            int temp = step.Value - 1000000000;
            int r = temp / 1000000;
            temp -= (r * 1000000);
            int g = temp / 1000;
            temp -= (g * 1000);
            int b = temp;
            return new Color() { A = 255, R = (byte)r, G = (byte)g, B = (byte)b };
        }
    }
}