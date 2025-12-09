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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using DS4Windows;

namespace DS4WinWPF.DS4Forms.ViewModels
{
public class SpecialActionsListViewModel
{
    private ObservableCollection<SpecialActionItem> actionCol = new ObservableCollection<SpecialActionItem>();
    private int specialActionIndex = -1;
    private SpecialActionItem currentSAItem;
    private int deviceNum;
    public event EventHandler SpecialActionIndexChanged;
    public event EventHandler ItemSelectedChanged;

    public SpecialActionsListViewModel(int deviceNum)
    {
        this.deviceNum = deviceNum;
        SpecialActionIndexChanged += SpecialActionsListViewModel_SpecialActionIndexChanged;
        actionCol.CollectionChanged += ActionCol_CollectionChanged;
    }

    public ObservableCollection<SpecialActionItem> ActionCol => actionCol;
    public int SpecialActionIndex
    {
        get => specialActionIndex;
        set
        {
            if (specialActionIndex != value)
            {
                specialActionIndex = value;
                SpecialActionIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public SpecialActionItem CurrentSpecialActionItem
    {
        get => currentSAItem;
        set => currentSAItem = value;
    }

    public bool ItemSelected => specialActionIndex >= 0;

    public void SortActions(string column, bool ascending)
    {
        bool isUiThread = false;
        try {
            isUiThread = System.Windows.Application.Current?.Dispatcher?.CheckAccess() ?? false;
        } catch { }
    AppLogger.LogDebug($"[SortActions] UI thread: {isUiThread}");
    if (!isUiThread) AppLogger.LogDebug("[SortActions] WARNING: Running off the UI thread. Operations on ObservableCollection may fail.");

        AppLogger.LogDebug($"[SortActions] column={column}, ascending={ascending}, actionCol.Count(before)={actionCol.Count}");
        IEnumerable<SpecialActionItem> sorted = null;
        switch (column)
        {
            case "Active":
                // Put active (checked) items first when ascending==true
                sorted = ascending ? actionCol.OrderByDescending(x => x.Active)
                                   : actionCol.OrderBy(x => x.Active);
                break;
            case "Name":
                sorted = ascending ? actionCol.OrderBy(x => x.ActionName, StringComparer.CurrentCultureIgnoreCase)
                                   : actionCol.OrderByDescending(x => x.ActionName, StringComparer.CurrentCultureIgnoreCase);
                break;
            case "Trigger":
                sorted = ascending ? actionCol.OrderBy(x => x.Controls, StringComparer.CurrentCultureIgnoreCase)
                                   : actionCol.OrderByDescending(x => x.Controls, StringComparer.CurrentCultureIgnoreCase);
                break;
            case "Action":
                sorted = ascending ? actionCol.OrderBy(x => x.TypeName, StringComparer.CurrentCultureIgnoreCase)
                                   : actionCol.OrderByDescending(x => x.TypeName, StringComparer.CurrentCultureIgnoreCase);
                break;
            default:
                AppLogger.LogDebug($"[SortActions] Invalid column value: {column}");
                return;
        }
        var sortedList = sorted.ToList(); // Clear前に評価
        AppLogger.LogDebug($"[SortActions] sorted.Count={sortedList.Count}");
        if (sortedList.Count > 0)
        {
            AppLogger.LogDebug($"[SortActions] sorted items: {string.Join(",", sortedList.Select(x => x.ActionName))}");
        }
        var oldRef = actionCol;
        actionCol.Clear();
        AppLogger.LogDebug($"[SortActions] actionCol.Count(after Clear)={actionCol.Count}");
        int idx = 0;
        foreach (var item in sortedList)
        {
            try
            {
                AppLogger.LogDebug($"[SortActions] Add: {item.ActionName}, Index={idx}");
                item.Index = idx++;
                actionCol.Add(item);
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[SortActions] Exception adding item: {item?.ActionName} - {ex}");
                throw;
            }
        }
        if (!object.ReferenceEquals(actionCol, oldRef))
        {
            AppLogger.LogDebug($"[SortActions] actionCol reference changed!");
        }
        AppLogger.LogDebug($"[SortActions] actionCol.Count(after)={actionCol.Count}");
    }

    // 1引数オーバーロード（ProfileEditor.xaml.cs用）
    public SpecialActionItem CreateActionItem(SpecialAction action)
    {
        string displayName = GetActionDisplayName(action);
        int index = actionCol.Count;
        return CreateActionItem(action, displayName, index);
    }

    public SpecialActionItem CreateActionItem(SpecialAction action, string displayName, int index)
    {
        var item = new SpecialActionItem(action, displayName, index);
        item.IsMissing = (action == null);
        return item;
    }

    private void ActionCol_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
        {
            for (int i = e.OldStartingIndex; i < actionCol.Count; i++)
            {
                // Replace old index with updated index
                actionCol[i].Index = i;
            }
        }
    }

    private void SpecialActionsListViewModel_SpecialActionIndexChanged(object sender, EventArgs e)
    {
        ItemSelectedChanged?.Invoke(this, EventArgs.Empty);
    }

    public void LoadActions(bool newProfile = false)
    {
        actionCol.Clear();

        // Actions.xmlに存在するスペシャルアクション名一覧
        var xmlActionNames = Global.GetActions().Select(a => a.name).ToList();
        // プロフィール設定ファイルに残っているスペシャルアクション名一覧
        List<string> pactions = Global.ProfileActions[deviceNum];

        // 両方を統合し、重複除去して名前順でソート
        var allActionNames = xmlActionNames.Union(pactions).Distinct().OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase).ToList();

        int idx = 0;
        foreach (var actionName in allActionNames)
        {
            SpecialAction action = Global.GetActions().FirstOrDefault(a => string.Equals(a.name, actionName, StringComparison.CurrentCultureIgnoreCase));
            bool isMissing = action == null;
            if (isMissing)
            {
                // Actions.xmlに存在しない場合はダミーのSpecialActionを作成
                action = new SpecialAction(actionName, "", "null", "");
            }
            string displayName = GetActionDisplayName(action);
            string typeName = isMissing ? $"({Properties.Resources.InvalidSpecialAction})" : displayName;
            SpecialActionItem item = new SpecialActionItem(action, typeName, idx);
            item.IsMissing = isMissing;

            // プロファイルで有効なものはチェックON
            if (pactions.Contains(actionName))
            {
                item.Active = true;
            }
            // Note: newProfile時の自動チェック処理は削除。
            // EstablishDefaultSpecialActions()でprofileActionsに追加されたアクションのみがチェックされる。

            actionCol.Add(item);
            idx++;
        }
    }

    public string GetActionDisplayName(SpecialAction action)
    {
        string displayName = string.Empty;
        switch (action.typeID)
        {
            case SpecialAction.ActionTypeId.DisconnectBT:
                displayName = Properties.Resources.DisconnectBT; break;
            case SpecialAction.ActionTypeId.Macro:
                displayName = Properties.Resources.Macro + (action.keyType.HasFlag(DS4KeyType.ScanCode) ? " (" + Properties.Resources.ScanCode + ")" : "");
                break;
            case SpecialAction.ActionTypeId.Program:
                displayName = Properties.Resources.LaunchProgram.Replace("*program*", Path.GetFileNameWithoutExtension(action.details));
                break;
            case SpecialAction.ActionTypeId.Profile:
                displayName = Properties.Resources.LoadProfile.Replace("*profile*", action.details);
                break;
            case SpecialAction.ActionTypeId.Key:
                displayName = KeyInterop.KeyFromVirtualKey(int.Parse(action.details)).ToString() +
                     (action.keyType.HasFlag(DS4KeyType.Toggle) ? " (Toggle)" : "");
                break;
            case SpecialAction.ActionTypeId.BatteryCheck:
                displayName = Properties.Resources.CheckBattery;
                break;
            case SpecialAction.ActionTypeId.XboxGameDVR:
                displayName = "Xbox Game DVR";
                break;
            case SpecialAction.ActionTypeId.MultiAction:
                displayName = Properties.Resources.MultiAction;
                break;
            case SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate:
                displayName = Properties.Resources.SASteeringWheelEmulationCalibrate;
                break;
            case SpecialAction.ActionTypeId.GyroCalibrate:
                displayName = Translations.Strings.SpecialActionEdit_CalibrateGyro;
                break;
            default: break;
        }

        return displayName;
    }

    // Returns the list of currently enabled action names (does not mutate global state).
    public List<string> GetEnabledActionNames()
    {
        List<string> pactions = new List<string>();
        foreach (SpecialActionItem item in actionCol)
        {
            if (item.Active)
            {
                pactions.Add(item.ActionName);
            }
        }

        return pactions;
    }

    public void RemoveAction(SpecialActionItem item)
    {
        // Remove from Actions.xml (global actions list)
        try
        {
            Global.RemoveAction(item.SpecialAction.name);
        }
        catch { }

        // Also remove any references to this action from the current profile's
        // ProfileActions for this viewmodel's device, updating cached profile info
        // immediately so the UI/logic stays consistent.
        try
        {
            var pa = Global.ProfileActions; // List<string>[]
            if (pa != null && pa.Length > deviceNum && pa[deviceNum] != null)
            {
                // Remove entries that match by normalized name (trim + case-insensitive)
                var list = pa[deviceNum];
                string target = Global.NormalizeActionName(item.SpecialAction.name);
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        if (string.Equals(Global.NormalizeActionName(list[i]), target, StringComparison.OrdinalIgnoreCase))
                        {
                            list.RemoveAt(i);
                        }
                    }
                    catch { }
                }
            }
            Global.CacheExtraProfileInfo(deviceNum);
        }
        catch { }

        int itemIndex = item.Index;
        actionCol.RemoveAt(itemIndex);
    }
}

    public class SpecialActionItem : System.ComponentModel.INotifyPropertyChanged
    {
        private SpecialAction specialAction;
        public bool IsMissing { get; set; } // Actions.xmlに存在しない場合true
        private bool active;
        private string typeName;
        private int index = 0;

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public SpecialActionItem(SpecialAction specialAction, string displayName, int index)
        {
            this.specialAction = specialAction;
            this.typeName = displayName;
            this.index = index;
        }

        public bool Active
        {
            get => active;
            set
            {
                if (active != value)
                {
                    active = value;
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Active)));
                }
            }
        }

        public string ActionName
        {
            get => specialAction.name;
            set => specialAction.name = value;
        }

        public int Index
        {
            get => index;
            set => index = value;
        }

        public string TypeName => typeName;

        public string Controls => specialAction.controls.Replace("/", ", ");

        public SpecialAction SpecialAction => specialAction;

        public void Refresh()
        {
            // イベントは未実装
        }
    }
}
