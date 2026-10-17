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
    [XmlRoot("Actions")]
    public class ActionsDTO : IDTO<BackingStore>
    {
        [XmlElement("Action")] // Use XmlElement here to skip container element
        public List<SpecialActionSerializer> actionSerializers;

        public ActionsDTO()
        {
            actionSerializers = new List<SpecialActionSerializer>();
        }

        public void MapFrom(BackingStore source)
        {
            foreach(SpecialAction action in source.actions)
            {
                SpecialActionSerializer actionSerializer = new SpecialActionSerializer();
                switch(action.typeID)
                {
                    case SpecialAction.ActionTypeId.Macro:
                        actionSerializer.Name = action.name;
                        actionSerializer.Trigger = action.controls;
                        actionSerializer.TypeString = action.type;
                        actionSerializer.Details = action.details;
                        actionSerializer.Extras = action.extra;
                        break;
                    case SpecialAction.ActionTypeId.Program:
                        actionSerializer.Name = action.name;
                        actionSerializer.Trigger = action.controls;
                        actionSerializer.TypeString = action.type;
                        actionSerializer.Details = action.details;
                        actionSerializer.Arguments = action.extra;
                        actionSerializer.Delay = action.delayTime;
                        break;
                    case SpecialAction.ActionTypeId.Profile:
                        actionSerializer.Name = action.name;
                        actionSerializer.Trigger = action.controls;
                        actionSerializer.TypeString = action.type;
                        actionSerializer.Details = action.details;
                        actionSerializer.UnloadTrigger = action.extra;
                        break;

                    case SpecialAction.ActionTypeId.Key:
                        actionSerializer.Name = action.name;
                        actionSerializer.Trigger = action.controls;
                        actionSerializer.TypeString = action.type;
                        actionSerializer.Details = action.details;
                        if (!string.IsNullOrEmpty(action.extra))
                        {
                            string[] exts = action.extra.Split('\n');
                            // New name: SwitchMode (values: Press/Toggle). Keep compatibility by storing in SwitchMode.
                            actionSerializer.SwitchMode = exts.Length > 0 ? exts[0] : string.Empty;
                            actionSerializer.UnloadTrigger = exts.Length > 1 ? exts[1] : string.Empty;
                        }

                        break;

                    case SpecialAction.ActionTypeId.Button:
                        actionSerializer.Name = action.name;
                        actionSerializer.Trigger = action.controls;
                        actionSerializer.TypeString = "Button";
                        actionSerializer.Details = action.details;
                        // Phase6-Step10-3c: SwitchMode（Press／Toggle）を保存する（Key と同じ。従来は書き出していなかった）
                        if (!string.IsNullOrEmpty(action.extra))
                        {
                            string[] btnExts = action.extra.Split('\n');
                            actionSerializer.SwitchMode = btnExts.Length > 0 ? btnExts[0] : string.Empty;
                        }
                        break;

                    case SpecialAction.ActionTypeId.DisconnectBT:
                    case SpecialAction.ActionTypeId.BatteryCheck:
                    case SpecialAction.ActionTypeId.MultiAction:
                    case SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate:
                    case SpecialAction.ActionTypeId.GyroCalibrate:
                        actionSerializer.Name = action.name;
                        actionSerializer.Trigger = action.controls;
                        actionSerializer.TypeString = action.type;
                        if (action.typeID == SpecialAction.ActionTypeId.BatteryCheck)
                        {
                            actionSerializer.Details = string.Join("|", action.details.Split(","));
                        }
                        else
                        {
                            actionSerializer.Details = action.details;
                        }

                        break;
                    case SpecialAction.ActionTypeId.None:
                    default:
                        actionSerializer = null;
                        break;
                }

                if (actionSerializer != null)
                {
                    actionSerializers.Add(actionSerializer);
                }
            }
        }

        public void MapTo(BackingStore destination)
        {
            foreach(SpecialActionSerializer actionSerializer in actionSerializers)
            {
                SpecialAction tempAction = null;
                switch(actionSerializer.TypeString)
                {
                    case "Macro":
                        tempAction = new SpecialAction(actionSerializer.Name,
                            actionSerializer.Trigger, actionSerializer.TypeString,
                            actionSerializer.Details, 0, actionSerializer.Extras);
                        break;
                    case "Program":
                        {
                            string extras = actionSerializer.Arguments;
                            tempAction = new SpecialAction(actionSerializer.Name,
                                actionSerializer.Trigger, actionSerializer.TypeString,
                                actionSerializer.Details, actionSerializer.Delay,
                                extras
                            );
                        }

                        break;
                    case "Profile":
                        {
                            string extras = actionSerializer.UnloadTrigger;
                            tempAction = new SpecialAction(actionSerializer.Name,
                                actionSerializer.Trigger, actionSerializer.TypeString,
                                actionSerializer.Details, 0, extras);
                        }

                        break;
                    case "Key":
                        {
                            string tempExtras = string.Empty;
                            // Prefer new SwitchMode if present.
                            // For legacy files where only UnloadStyle exists, treat that as the old toggle-style marker
                            // (map legacy UnloadStyle -> "Toggle" to preserve previous behavior).
                            string mode;
                            if (!string.IsNullOrEmpty(actionSerializer.SwitchMode))
                                mode = actionSerializer.SwitchMode;
                            else if (!string.IsNullOrEmpty(actionSerializer.UnloadStyle))
                                mode = "Toggle"; // legacy UnloadStyle indicates toggle behavior
                            else
                                mode = "Press"; // default to Press when neither SwitchMode nor legacy UnloadStyle present
                            if (!string.IsNullOrEmpty(mode) || !string.IsNullOrEmpty(actionSerializer.UnloadTrigger))
                            {
                                tempExtras = $"{mode}\n{actionSerializer.UnloadTrigger}";
                            }

                            tempAction = new SpecialAction(actionSerializer.Name,
                                actionSerializer.Trigger, actionSerializer.TypeString,
                                actionSerializer.Details, 0, tempExtras);
                        }

                        break;
                        case "Button":
                            // Phase6-Step10-3c: SwitchMode（Press／Toggle）を読み込む。無い場合は従来どおり Press
                            tempAction = new SpecialAction(actionSerializer.Name,
                                actionSerializer.Trigger, actionSerializer.TypeString,
                                actionSerializer.Details, 0,
                                string.IsNullOrEmpty(actionSerializer.SwitchMode) ? string.Empty : $"{actionSerializer.SwitchMode}\n");
                            break;
                    case "DisconnectBT":
                        {
                            double delayTime = actionSerializer.Delay;
                            string details = actionSerializer.Details;
                            double.TryParse(details, System.Globalization.NumberStyles.Float, Global.configFileDecimalCulture, out delayTime);

                            tempAction = new SpecialAction(actionSerializer.Name,
                                actionSerializer.Trigger, actionSerializer.TypeString,
                                details, delayTime);
                        }

                        break;
                    case "BatteryCheck":
                        {
                            double delayTime = actionSerializer.Delay;
                            string details = actionSerializer.Details;
                            if (double.TryParse(details.Split('|')[0], System.Globalization.NumberStyles.Float, Global.configFileDecimalCulture, out delayTime))
                            {
                            }
                            else if (double.TryParse(details.Split(',')[0], System.Globalization.NumberStyles.Float, Global.configFileDecimalCulture, out delayTime))
                            {
                            }

                            tempAction = new SpecialAction(actionSerializer.Name,
                                actionSerializer.Trigger, actionSerializer.TypeString,
                                details, delayTime);
                        }

                        break;

                    case "MultiAction":
                        tempAction = new SpecialAction(actionSerializer.Name,
                            actionSerializer.Trigger, actionSerializer.TypeString,
                            actionSerializer.Details, actionSerializer.Delay,
                            actionSerializer.Extras);
                        break;
                    case "SASteeringWheelEmulationCalibrate":
                        tempAction = new SpecialAction(actionSerializer.Name,
                            actionSerializer.Trigger, actionSerializer.TypeString,
                            "", actionSerializer.Delay);
                        break;
                    case "GyroCalibration":
                        double.TryParse(actionSerializer.Details, System.Globalization.NumberStyles.Float, Global.configFileDecimalCulture, out var delay);
                        tempAction = new SpecialAction(actionSerializer.Name, actionSerializer.Trigger,
                            actionSerializer.TypeString, actionSerializer.Details, delay);
                        break;
                    default:
                        break;
                }

                if (tempAction != null)
                {
                    destination.actions.Add(tempAction);
                }
            }
        }
    }
}
