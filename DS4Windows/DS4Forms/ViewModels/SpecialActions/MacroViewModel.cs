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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels.Util;

namespace DS4WinWPF.DS4Forms.ViewModels.SpecialActions
{
    public class MacroViewModel : NotifyDataErrorBase, System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
        private bool useScanCode;
        private bool runTriggerRelease;
        private bool syncRun;
        private bool keepKeyState;
        private bool repeatHeld;
        private List<int> macro = new List<int>(1);
        private string macrostring;

        public bool UseScanCode { get => useScanCode; set { useScanCode = value; OnPropertyChanged(nameof(UseScanCode)); } }
        public bool RunTriggerRelease { get => runTriggerRelease; set { runTriggerRelease = value; OnPropertyChanged(nameof(RunTriggerRelease)); } }
        public bool SyncRun { get => syncRun; set { syncRun = value; OnPropertyChanged(nameof(SyncRun)); } }
        public bool KeepKeyState { get => keepKeyState; set { keepKeyState = value; OnPropertyChanged(nameof(KeepKeyState)); } }
        public bool RepeatHeld { get => repeatHeld; set { repeatHeld = value; OnPropertyChanged(nameof(RepeatHeld)); } }
        public List<int> Macro { get => macro; set { macro = value; OnPropertyChanged(nameof(Macro)); } }
        public string Macrostring { get => macrostring;
            set
            {
                macrostring = value;
                MacrostringChanged?.Invoke(this, EventArgs.Empty);
                OnPropertyChanged(nameof(Macrostring));
            }
        }

        public event EventHandler MacrostringChanged;

        public void LoadAction(SpecialAction action)
        {
            macro = action.macro;
            if (action.macro.Count > 0)
            {
                MacroParser macroParser = new MacroParser(action.macro.ToArray());
                macroParser.LoadMacro();
                macrostring = string.Join(", ", macroParser.GetMacroStrings());
            }

            useScanCode = action.keyType.HasFlag(DS4KeyType.ScanCode);
            runTriggerRelease = action.pressRelease;
            syncRun = action.synchronized;
            keepKeyState = action.keepKeyState;
            // Treat either RepeatMacro or HoldMacro as "repeat while held" for compatibility
            repeatHeld = action.keyType.HasFlag(DS4KeyType.RepeatMacro) || action.keyType.HasFlag(DS4KeyType.HoldMacro);
        }

        public DS4ControlSettings PrepareSettings()
        {
            DS4ControlSettings settings = new DS4ControlSettings(DS4Controls.None);
            settings.action.actionMacro = macro.ToArray();
            settings.actionType = DS4ControlSettings.ActionType.Macro;
            settings.keyType = DS4KeyType.Macro;
            if (repeatHeld)
            {
                // Keep both RepeatMacro and HoldMacro flags in settings so
                // macro editor (RecordBox) and special-action editor stay in sync.
                settings.keyType |= DS4KeyType.RepeatMacro;
                settings.keyType |= DS4KeyType.HoldMacro;
            }
            if (useScanCode)
            {
                settings.keyType |= DS4KeyType.ScanCode;
            }

            return settings;
        }

        public void SaveAction(SpecialAction action, bool edit = false)
        {
            List<string> extrasList = new List<string>();
            extrasList.Add(useScanCode ? "Scan Code" : null);
            extrasList.Add(runTriggerRelease ? "RunOnRelease" : null);
            extrasList.Add(syncRun ? "Sync" : null);
            extrasList.Add(keepKeyState ? "KeepKeyState" : null);
            extrasList.Add(repeatHeld ? "Repeat" : null);
            Global.SaveAction(action.name, action.controls, 1, string.Join("/", macro), edit,
                extras: string.Join("/", extrasList.Where(s => !string.IsNullOrEmpty(s))));
        }

        public void UpdateMacroString()
        {
            string temp = "";
            if (macro.Count > 0)
            {
                MacroParser macroParser = new MacroParser(macro.ToArray());
                macroParser.LoadMacro();
                temp = string.Join(", ", macroParser.GetMacroStrings());
            }

            Macrostring = temp;
        }

        public override bool IsValid(SpecialAction action)
        {
            ClearOldErrors();

            bool valid = true;
            List<string> macroErrors = new List<string>();

            if (macro.Count == 0)
            {
                valid = false;
                macroErrors.Add("No macro defined");
                errors["Macro"] = macroErrors;
                RaiseErrorsChanged("Macro");
            }

            return valid;
        }

        public override void ClearOldErrors()
        {
            if (errors.Count > 0)
            {
                errors.Clear();
                RaiseErrorsChanged("Macro");
            }
        }
    }
}
