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
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using DS4Windows;
using DS4Windows.InputDevices;
using DS4Windows.StickModifiers;
using DS4WinWPF.DS4Forms.ViewModels;
using static DS4Windows.Mouse;

namespace DS4WinWPF.DS4Control.DTOXml
{

    public class FlickStickSettings
    {
        [XmlElement("RealWorldCalibration")]
        public double RealWorldCalibration
        {
            get; set;
        }

        [XmlElement("FlickThreshold")]
        public double FlickThreshold
        {
            get; set;
        }

        [XmlElement("FlickTime")]
        public double FlickTime
        {
            get; set;
        }

        [XmlElement("MinAngleThreshold")]
        public double MinAngleThreshold
        {
            get; set;
        }
    }
}