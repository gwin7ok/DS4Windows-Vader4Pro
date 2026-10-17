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
using System.Xml;
using System.Xml.Serialization;
using DS4Windows;

namespace DS4WinWPF.DS4Control.DTOXml
{

    public class SpecialActionSerializer
    {
        [XmlAttribute("Name")]
        public string Name
        {
            get; set;
        } = string.Empty;

        [XmlElement("Trigger")]
        public string Trigger
        {
            get; set;
        } = string.Empty;

        [XmlElement("Type")]
        public string TypeString
        {
            get; set;
        } = string.Empty;

        [XmlElement("Details")]
        public string Details
        {
            get; set;
        } = string.Empty;

        [XmlElement("Extras")]
        public string Extras
        {
            get; set;
        } = string.Empty;
        public bool ShouldSerializeExtras()
        {
            return TypeString == "Macro" && !string.IsNullOrEmpty(Extras);
        }

        [XmlElement("Arguements")]
        public string Arguments
        {
            get; set;
        } = string.Empty;
        public bool ShouldSerializeArguments()
        {
            return TypeString == "Program" && !string.IsNullOrEmpty(Arguments);
        }

        [XmlIgnore]
        public double Delay
        {
            get; set;
        }
        //public bool ShouldSerializeDelay()
        //{
        //    return TypeString == "Program";
        //}

        [XmlElement("Delay")]
        public string DelayString
        {
            get
            {
                return Delay.ToString(Global.configFileDecimalCulture);
            }
            set
            {
                double delayTime = 0.0;
                if (double.TryParse(value, System.Globalization.NumberStyles.Float, Global.configFileDecimalCulture, out delayTime))
                {
                    Delay = delayTime;
                }
            }
        }
        public bool ShouldSerializeDelayString()
        {
            return TypeString == "Program";
        }

        [XmlElement("UnloadTrigger")]
        public string UnloadTrigger
        {
            get; set;
        } = string.Empty;
        public bool ShouldSerializeUnloadTrigger()
        {
            HashSet<string> knownUseTypes = new HashSet<string>()
            {
                "Key", "Profile",
            };
            return knownUseTypes.Contains(TypeString) && !string.IsNullOrEmpty(UnloadTrigger);
        }

        [XmlElement("UnloadStyle")]
        public string UnloadStyle
        {
            get; set;
        } = string.Empty;
        public bool ShouldSerializeUnloadStyle()
        {
            // Legacy element; serialize only if SwitchMode is not present to preserve existing files when necessary.
            return TypeString == "Key" && string.IsNullOrEmpty(SwitchMode) && !string.IsNullOrEmpty(UnloadStyle);
        }

        [XmlElement("SwitchMode")]
        public string SwitchMode { get; set; } = string.Empty;
        public bool ShouldSerializeSwitchMode()
        {
            // Phase6-Step10-3c: Button（コントローラーのボタンの割り当て）も、Press／Toggle を保存する
            return (TypeString == "Key" || TypeString == "Button") && !string.IsNullOrEmpty(SwitchMode);
        }
    }
}
