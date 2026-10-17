/*
DS4Windows
Copyright (C) 2023 Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Actions;

namespace DS4WinWPF.DS4Forms.ViewModels
{

    public class SpecialActionItem : INotifyPropertyChanged
    {
        private bool active;
        private string actionName;
        private int index;
        private string typeName;
        private string controls;
        private SpecialAction specialAction;

        public bool IsMissing { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public SpecialActionItem(SpecialAction action, string actionName, int index)
        {
            this.specialAction = action;
            this.actionName = actionName;
            this.index = index;
            if (action != null)
            {
                this.typeName = action.type.ToString();
                this.controls = action.controls;
            }
        }

        public bool Active
        {
            get => active;
            set
            {
                if (active != value)
                {
                    active = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ActionName
        {
            get => actionName;
            set
            {
                if (actionName != value)
                {
                    actionName = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Index
        {
            get => index;
            set
            {
                if (index != value)
                {
                    index = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TypeName
        {
            get => typeName;
            set
            {
                if (typeName != value)
                {
                    typeName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Controls
        {
            get => controls;
            set
            {
                if (controls != value)
                {
                    controls = value;
                    OnPropertyChanged();
                }
            }
        }

        public SpecialAction SpecialAction
        {
            get => specialAction;
            set
            {
                specialAction = value;
                OnPropertyChanged();
            }
        }

        public void Refresh()
        {
            OnPropertyChanged(string.Empty);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}