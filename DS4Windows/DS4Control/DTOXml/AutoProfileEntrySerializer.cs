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
using System.Xml;
using System.Xml.Serialization;
using DS4Windows;

namespace DS4WinWPF.DS4Control.DTOXml
{

    public class AutoProfileEntrySerializer
    {
        [XmlAttribute("path")]
        public string Path
        {
            get; set;
        } = string.Empty;

        [XmlAttribute("title")]
        public string Title
        {
            get; set;
        } = string.Empty;

        [XmlElement("Controller1")]
        public string Controller1
        {
            get; set;
        } = string.Empty;

        [XmlElement("Controller2")]
        public string Controller2
        {
            get; set;
        } = string.Empty;

        [XmlElement("Controller3")]
        public string Controller3
        {
            get; set;
        } = string.Empty;

        [XmlElement("Controller4")]
        public string Controller4
        {
            get; set;
        } = string.Empty;

        [XmlElement("Controller5")]
        public string Controller5
        {
            get; set;
        } = string.Empty;
        public bool ShouldSerializeController5()
        {
            return ControllerLimits.MaxControllerCount >= 5;
        }

        [XmlElement("Controller6")]
        public string Controller6
        {
            get; set;
        } = string.Empty;
        public bool ShouldSerializeController6()
        {
            return ControllerLimits.MaxControllerCount >= 6;
        }

        [XmlElement("Controller7")]
        public string Controller7
        {
            get; set;
        } = string.Empty;
        public bool ShouldSerializeController7()
        {
            return ControllerLimits.MaxControllerCount >= 7;
        }

        [XmlElement("Controller8")]
        public string Controller8
        {
            get; set;
        } = string.Empty;
        public bool ShouldSerializeController8()
        {
            return ControllerLimits.MaxControllerCount >= 8;
        }

        [XmlElement("TurnOff")]
        public string TurnOffString
        {
            get => TurnOff.ToString();
            set
            {
                TurnOff = XmlDataUtilities.StrToBool(value);
            }
        }

        [XmlIgnore]
        public bool TurnOff
        {
            get; set;
        }

        public AutoProfileEntrySerializer()
        {
        }
    }
}
