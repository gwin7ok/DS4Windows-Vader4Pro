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
        // Phase6-Step10-3-3: 未使用だった依存（actionManager、outputSlotService、profileSettings）を削除した。
        // ボタン名表示は Issue7 の是正で、取得元がこのクラスの外へ移っている（Phase5-Step14-Issue7-Fix-Plan.md タスク3）。

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
            ISpecialActionRepository specialActionRepo,
            IProfileRepository profileRepo)
        {
            this.deviceNum = deviceNum;
            this.specialActionRepo = specialActionRepo ?? throw new ArgumentNullException(nameof(specialActionRepo));
            this.profileRepo = profileRepo ?? throw new ArgumentNullException(nameof(profileRepo));

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

        // Phase6系の他の是正と同様、直前にソートした列・方向を「唯一の実体」として本 ViewModel が保持する。
        // View（ProfileEditor.xaml.cs）側で同じ情報を別途保持すると二重状態になるため、
        // View はヘッダー表示や再ソート時に本プロパティを参照する（CurrentSortColumn/CurrentSortAscending）。
        private string lastSortedPropertyName = "Name";
        private ListSortDirection lastSortedDirection = ListSortDirection.Ascending;

        /// <summary>直近に <see cref="SortActions"/> で適用したソート列名。</summary>
        public string CurrentSortColumn => lastSortedPropertyName;

        /// <summary>直近に <see cref="SortActions"/> で適用したソート方向。</summary>
        public ListSortDirection CurrentSortDirection => lastSortedDirection;

        /// <summary><see cref="CurrentSortDirection"/> が昇順かどうか（View 側の bool 引数用の簡便プロパティ）。</summary>
        public bool CurrentSortAscending => lastSortedDirection == ListSortDirection.Ascending;

        public void SortActions(string propertyName, ListSortDirection direction)
        {
            List<SpecialActionItem> items = actionCol.ToList();
            actionCol.Clear();

            Comparison<SpecialActionItem> primary = GetColumnComparison(propertyName, direction);

            // 直前にソートしていた列が今回と異なる場合、それを第2ソートキーとして使う
            // （直前の方向をそのまま維持する）。例: 名前昇順の状態から Active 列をクリックすると、
            // Active（クリックした方向）→ 名前昇順 の順で並ぶ。
            Comparison<SpecialActionItem> secondary =
                (lastSortedPropertyName != propertyName)
                    ? GetColumnComparison(lastSortedPropertyName, lastSortedDirection)
                    : null;

            items.Sort((x, y) =>
            {
                int cmp = primary?.Invoke(x, y) ?? 0;
                if (cmp != 0) return cmp;

                if (secondary != null)
                {
                    cmp = secondary(x, y);
                    if (cmp != 0) return cmp;
                }

                // 最終タイブレーク: アクション名（プロファイル内で一意）で確定的な順序にする。
                // List<T>.Sort は不安定ソートのため、これがないと主・第2キーとも同値の要素の
                // 順序が実行のたびに変わり得る。
                return string.Compare(x.ActionName, y.ActionName, StringComparison.CurrentCultureIgnoreCase);
            });

            foreach (var item in items)
            {
                actionCol.Add(item);
            }

            lastSortedPropertyName = propertyName;
            lastSortedDirection = direction;
            OnPropertyChanged(nameof(CurrentSortColumn));
            OnPropertyChanged(nameof(CurrentSortDirection));
            OnPropertyChanged(nameof(CurrentSortAscending));
        }

        /// <summary>
        /// 指定した列・方向に対応する比較関数を返す。未知の列名の場合は null（比較なし＝同値扱い）。
        /// </summary>
        private static Comparison<SpecialActionItem> GetColumnComparison(string propertyName, ListSortDirection direction)
        {
            bool asc = direction == ListSortDirection.Ascending;
            switch (propertyName)
            {
                case "Name":
                    if (asc) return (x, y) => string.Compare(x.ActionName, y.ActionName, StringComparison.CurrentCultureIgnoreCase);
                    return (x, y) => string.Compare(y.ActionName, x.ActionName, StringComparison.CurrentCultureIgnoreCase);
                case "Active":
                    // 昇順ではチェック有り(true)を上に、降順ではチェック無し(false)を上にする。
                    if (asc) return (x, y) => y.Active.CompareTo(x.Active);
                    return (x, y) => x.Active.CompareTo(y.Active);
                case "Trigger":
                    // XAML側のヘッダーボタン Tag="Trigger" に対応。実データは Controls プロパティ。
                    if (asc) return (x, y) => string.Compare(x.Controls, y.Controls, StringComparison.CurrentCultureIgnoreCase);
                    return (x, y) => string.Compare(y.Controls, x.Controls, StringComparison.CurrentCultureIgnoreCase);
                case "Action":
                    // XAML側のヘッダーボタン Tag="Action" に対応。実データは TypeName プロパティ。
                    if (asc) return (x, y) => string.Compare(x.TypeName, y.TypeName, StringComparison.CurrentCultureIgnoreCase);
                    return (x, y) => string.Compare(y.TypeName, x.TypeName, StringComparison.CurrentCultureIgnoreCase);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Phase6-Step8-3: 名前からアクションを取得する（前後の空白・大文字小文字を無視、未発見は placeholder）。
        /// ProfileEditor の <c>Global.GetAction</c> の置換（<see cref="ISpecialActionRepository.GetActionOrPlaceholder"/>）。
        /// </summary>
        public SpecialAction GetActionOrPlaceholder(string actionName)
        {
            return specialActionRepo.GetActionOrPlaceholder(actionName);
        }

        /// <summary>
        /// Phase6-Step8-3: Actions.xml のファイルパス（エクスポートのコピー元。ProfileEditor の <c>Global.appdatapath</c> の置換）。
        /// </summary>
        public string ActionsFilePath => specialActionRepo.ActionsPath;

        /// <summary>
        /// Phase6-Step8-4: 有効（チェック済み）なアクション名を、編集スロットの <c>ProfileActions</c> へ反映する。
        /// 保存時とチェック切り替え時の共通処理（ProfileEditor の同じ 2 行の重複を集約）。
        /// </summary>
        public void CommitEnabledActionsToProfile()
        {
            SyncProfileActionsString();
            profileRepo.ProfileActions[deviceNum] = GetEnabledActionNames();
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

                var actions = specialActionRepo.ActionList;
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

                // Phase6-Step10-3b-2: Actions.xml に無く、プロファイルの XML にだけ残っている名前を、トリガー・アクション欄が空の
                // グレーの項目として表示する（チェックを外して保存する、または「除去」で取り除ける）。
                // 40011c12 で実装され、36d124a6 のリファクタ（Actions.xml の一覧だけから一覧を作る形）で失われていた。
                // チェックしたままのものは、保存してもプロファイルに残る（GetEnabledActionNames）。
                IEnumerable<string> profileNames = !string.IsNullOrEmpty(profileActionsString)
                    ? profileActionsString.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                    : (profileRepo.ProfileActions != null && deviceNum >= 0 && deviceNum < profileRepo.ProfileActions.Length
                        ? profileRepo.ProfileActions[deviceNum] : null);
                if (profileNames != null)
                {
                    int missingIndex = actionCol.Count;
                    foreach (string profileActionName in profileNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList())
                    {
                        if (string.IsNullOrWhiteSpace(profileActionName) ||
                            actionCol.Any(a => string.Equals(a.ActionName, profileActionName, StringComparison.OrdinalIgnoreCase)))
                            continue;

                        var placeholder = new SpecialAction(profileActionName, "", "null", "");
                        SpecialActionItem missingItem = new SpecialActionItem(placeholder, profileActionName, missingIndex++)
                        {
                            IsMissing = true,
                            Active = true,
                        };
                        actionCol.Add(missingItem);
                        missingItem.PropertyChanged += Item_PropertyChanged;
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

        /// <summary>
        /// 一覧からアクションを除去する。Actions.xml に存在するアクションは、Actions.xml からも削除して保存する
        /// （Phase6-Step10-3b-3: <see cref="ISpecialActionRepository.RemoveActionAndSave"/>。従来はメモリ上のリストから外すだけだった）。
        /// Actions.xml に無い名前（グレーの項目）は、一覧から外すだけ。プロファイルの XML からの参照の削除は呼び出し側が行う。
        /// </summary>
        public void RemoveAction(SpecialActionItem item)
        {
            if (item == null) return;

            if (!item.IsMissing)
            {
                specialActionRepo.RemoveActionAndSave(item.ActionName);
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
}