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
    public class SpecialActionsListViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<SpecialActionItem> actionCol = new ObservableCollection<SpecialActionItem>();
        private int specialActionIndex = -1;
        private SpecialActionItem currentSAItem;
        private int deviceNum;
        private readonly ISpecialActionRepository specialActionRepo;
        private readonly IProfileRepository profileRepo;
        private readonly IManagedActionManager actionManager;
        // TODO(技術的負債・削除要否は別途判断): Issue7是正（タスク3）により、本フィールドを
        // 参照していたボタン名表示ロジックは profileSettings.OutContType 経由に置き換えられ、
        // 本ファイル内では現在未使用となっている。Fix-Plan.md タスク3の方針に従い、本タスクでは
        // 参照差し替えのみを行い、フィールド自体の削除要否は別途（Phase5-Step14タスク7の全体確認、
        // またはPhase6）で判断する。
        private readonly IOutputSlotService outputSlotService;
        // Issue7是正（Phase5-Step14-Issue7-Fix-Plan.md タスク3）:
        // ボタン名表示の参照先を outputSlotService.GetOutputDeviceType から
        // 正しい永続化実体である profileSettings.OutContType へ切り替えるために新規注入。
        private readonly IProfileSettingsService profileSettings;

        private string _profileActions = string.Empty;
        private bool _isInternalSync = false;

        public event EventHandler SpecialActionIndexChanged;
        public event EventHandler ItemSelectedChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// プロファイル XML に保存されるスラッシュ区切りの Special Actions 文字列 (例: "Action1/Action2")
        /// 各アイテムの Active 状態とリアルタイムに自動同期されます。
        /// </summary>
        public string ProfileActions
        {
            get => _profileActions;
            private set
            {
                if (_profileActions != value)
                {
                    _profileActions = value;
                    OnPropertyChanged();
                }
            }
        }

        public SpecialActionsListViewModel(int deviceNum,
            ISpecialActionRepository specialActionRepo = null,
            IProfileRepository profileRepo = null,
            IManagedActionManager actionManager = null,
            IOutputSlotService outputSlotService = null,
            IProfileSettingsService profileSettings = null)
        {
            this.deviceNum = deviceNum;
            this.specialActionRepo = specialActionRepo ?? DS4WinWPF.AppHost.GetService<ISpecialActionRepository>() ?? Global.SpecialActionRepositoryInstance;
            this.profileRepo = profileRepo ?? DS4WinWPF.AppHost.GetService<IProfileRepository>() ?? Global.ProfileRepositoryInstance;
            this.actionManager = actionManager ?? DS4WinWPF.AppHost.GetService<IManagedActionManager>();
            this.outputSlotService = outputSlotService ?? DS4WinWPF.AppHost.GetService<IOutputSlotService>() ?? Global.OutputSlotServiceInstance;
            this.profileSettings = profileSettings ?? DS4WinWPF.AppHost.GetService<IProfileSettingsService>() ?? Global.ProfileSettingsServiceInstance;

            actionCol.CollectionChanged += ActionCol_CollectionChanged;
        }

        public ObservableCollection<SpecialActionItem> ActionCol => actionCol;

        public int SpecialActionIndex
        {
            get => specialActionIndex;
            set
            {
                if (specialActionIndex == value) return;
                specialActionIndex = value;
                SpecialActionIndexChanged?.Invoke(this, EventArgs.Empty);
                SpecialActionsListViewModel_SpecialActionIndexChanged(this, EventArgs.Empty);
                OnPropertyChanged();
            }
        }

        public SpecialActionItem CurrentSpecialActionItem
        {
            get => currentSAItem;
            set
            {
                currentSAItem = value;
                OnPropertyChanged();
            }
        }

        public bool ItemSelected => specialActionIndex >= 0;

        public void SortActions(string propertyName, ListSortDirection direction)
        {
            List<SpecialActionItem> items = actionCol.ToList();
            actionCol.Clear();

            switch (propertyName)
            {
                case "Name":
                    if (direction == ListSortDirection.Ascending)
                        items.Sort((x, y) => string.Compare(x.ActionName, y.ActionName, StringComparison.CurrentCultureIgnoreCase));
                    else
                        items.Sort((x, y) => string.Compare(y.ActionName, x.ActionName, StringComparison.CurrentCultureIgnoreCase));
                    break;
                case "Active":
                    if (direction == ListSortDirection.Ascending)
                        items.Sort((x, y) => x.Active.CompareTo(y.Active));
                    else
                        items.Sort((x, y) => y.Active.CompareTo(x.Active));
                    break;
                case "TypeName":
                    if (direction == ListSortDirection.Ascending)
                        items.Sort((x, y) => string.Compare(x.TypeName, y.TypeName, StringComparison.CurrentCultureIgnoreCase));
                    else
                        items.Sort((x, y) => string.Compare(y.TypeName, x.TypeName, StringComparison.CurrentCultureIgnoreCase));
                    break;
                case "Controls":
                    if (direction == ListSortDirection.Ascending)
                        items.Sort((x, y) => string.Compare(x.Controls, y.Controls, StringComparison.CurrentCultureIgnoreCase));
                    else
                        items.Sort((x, y) => string.Compare(y.Controls, x.Controls, StringComparison.CurrentCultureIgnoreCase));
                    break;
            }

            foreach (var item in items)
            {
                actionCol.Add(item);
            }
        }

        public SpecialActionItem CreateActionItem(SpecialAction action)
        {
            SpecialActionItem item = new SpecialActionItem(action, GetActionDisplayName(action), actionCol.Count);
            return item;
        }

        public SpecialActionItem CreateActionItem(SpecialAction action, string actionName)
        {
            SpecialActionItem item = new SpecialActionItem(action, actionName, actionCol.Count);
            return item;
        }

        private void ActionCol_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (SpecialActionItem item in e.OldItems)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (SpecialActionItem item in e.NewItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }
            }

            SyncProfileActionsString();
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SpecialActionItem.Active) && !_isInternalSync)
            {
                SyncProfileActionsString();
            }
        }

        private void SpecialActionsListViewModel_SpecialActionIndexChanged(object sender, EventArgs e)
        {
            CurrentSpecialActionItem = specialActionIndex >= 0 ? actionCol[specialActionIndex] : null;
            ItemSelectedChanged?.Invoke(this, EventArgs.Empty);
            OnPropertyChanged(nameof(ItemSelected));
        }

        public void LoadActions(bool direct = false, string profileActionsString = null)
        {
            _isInternalSync = true;
            try
            {
                // 既存購読解除
                foreach (var item in actionCol)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
                actionCol.Clear();

                var actions = specialActionRepo != null ? specialActionRepo.ActionList : Global.GetActions();
                if (actions != null)
                {
                    int index = 0;
                    foreach (SpecialAction action in actions)
                    {
                        SpecialActionItem item = new SpecialActionItem(action, GetActionDisplayName(action), index);
                        actionCol.Add(item);
                        item.PropertyChanged += Item_PropertyChanged;
                        index++;
                    }
                }

                if (!string.IsNullOrEmpty(profileActionsString))
                {
                    LoadProfileActionsString(profileActionsString);
                }
                else
                {
                    SyncProfileActionsString();
                }
            }
            finally
            {
                _isInternalSync = false;
            }
        }

        public string GetActionDisplayName(SpecialAction action)
        {
            string displayName = action.name;
            return displayName;
        }

        public List<string> GetEnabledActionNames()
        {
            List<string> list = new List<string>();
            foreach (SpecialActionItem item in actionCol)
            {
                if (item.Active)
                {
                    list.Add(item.ActionName);
                }
            }
            return list;
        }

        /// <summary>
        /// 現在チェックされているアクションから ProfileActions スラッシュ区切り文字列を再生成します。
        /// </summary>
        public void SyncProfileActionsString()
        {
            var enabledNames = actionCol.Where(a => a.Active && !a.IsMissing).Select(a => a.ActionName);
            ProfileActions = string.Join("/", enabledNames);
        }

        /// <summary>
        /// プロファイルから読み込んだ ProfileActions 文字列（例: "Act1/Act2"）をリストのチェック状態に反映します。
        /// </summary>
        public void LoadProfileActionsString(string profileActionsString)
        {
            _isInternalSync = true;
            try
            {
                HashSet<string> activeSet = new HashSet<string>(
                    (profileActionsString ?? string.Empty).Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var item in actionCol)
                {
                    item.Active = activeSet.Contains(item.ActionName);
                }

                ProfileActions = profileActionsString ?? string.Empty;
            }
            finally
            {
                _isInternalSync = false;
            }
        }

        public void RemoveAction(SpecialActionItem item)
        {
            if (item == null) return;

            if (specialActionRepo != null)
            {
                specialActionRepo.RemoveAction(item.ActionName);
            }
            else
            {
                Global.RemoveAction(item.ActionName);
            }

            actionCol.Remove(item);

            // インデックス再割り振り
            for (int i = 0; i < actionCol.Count; i++)
            {
                actionCol[i].Index = i;
            }

            SyncProfileActionsString();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

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