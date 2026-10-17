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
    [XmlRoot("Programs")]
    public class AutoProfilesDTO : IDTO<AutoProfileHolder>
    {
        [XmlElement("Program")] // Use XmlElement here to skip container element
        public List<AutoProfileEntrySerializer> programEntries;

        public AutoProfilesDTO()
        {
            programEntries = new List<AutoProfileEntrySerializer>();
        }

        public void MapFrom(AutoProfileHolder source)
        {
            foreach(AutoProfileEntity entity in source.AutoProfileColl)
            {
                AutoProfileEntrySerializer temp = new AutoProfileEntrySerializer()
                {
                    Path = entity.Path,
                    Title = entity.Title,
                    Controller1 = entity.ProfileNames[0],
                    Controller2 = entity.ProfileNames[1],
                    Controller3 = entity.ProfileNames[2],
                    TurnOff = entity.Turnoff,
                };

                if (source.UsingMaxControllers)
                {
                    temp.Controller4 = entity.ProfileNames[3];
                    temp.Controller5 = entity.ProfileNames[4];
                    temp.Controller6 = entity.ProfileNames[5];
                    temp.Controller7 = entity.ProfileNames[6];
                    temp.Controller8 = entity.ProfileNames[7];
                }

                programEntries.Add(temp);
            }
        }

        public void MapTo(AutoProfileHolder destination)
        {
            foreach(AutoProfileEntrySerializer serializier in programEntries)
            {
                AutoProfileEntity autoprof = new AutoProfileEntity(serializier.Path, serializier.Title);

                autoprof.ProfileNames[0] = serializier.Controller1;
                autoprof.ProfileNames[1] = serializier.Controller2;
                autoprof.ProfileNames[2] = serializier.Controller3;
                autoprof.ProfileNames[3] = serializier.Controller4;

                if (ControlService.CURRENT_DS4_CONTROLLER_LIMIT >=
                    ControlService.MAX_DS4_CONTROLLER_COUNT)
                {
                    autoprof.ProfileNames[4] = serializier.Controller5;
                    autoprof.ProfileNames[5] = serializier.Controller6;
                    autoprof.ProfileNames[6] = serializier.Controller7;
                    autoprof.ProfileNames[7] = serializier.Controller8;
                }

                autoprof.Turnoff = serializier.TurnOff;
                destination.AutoProfileColl.Add(autoprof);
            }
        }
    }
}
