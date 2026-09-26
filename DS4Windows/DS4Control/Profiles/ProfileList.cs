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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DS4WinWPF
{
    public class ProfileList
    {
        private object _proLockobj = new object();
        private ObservableCollection<ProfileEntity> profileListCol =
            new ObservableCollection<ProfileEntity>();
        // Phase5-Step15-2-a: Global.appdatapath直接参照を廃止し、IPathServiceをDI経由で受け取る。
        // 引数省略時はGlobal.PathServiceInstanceにフォールバックし、既存呼び出し元(new ProfileList())を変更不要にする。
        private readonly DS4Windows.DI.IPathService pathService;

        public ObservableCollection<ProfileEntity> ProfileListCol { get => profileListCol; set => profileListCol = value; }

        public ProfileList(DS4Windows.DI.IPathService pathService = null)
        {
            this.pathService = pathService ?? DS4Windows.Global.PathServiceInstance;
            BindingOperations.EnableCollectionSynchronization(profileListCol, _proLockobj);
        }

        /// <summary>
        /// ディスク上の全プロファイルXMLファイルを正（SSOT）としてコレクションを同期します。
        /// ※ ComboBoxのバインディング破壊・TargetExceptionクラッシュを防ぐため、
        ///   Clear() は行わず、新規追加分と削除分のみを差分同期（マージ）します。
        /// </summary>
        public void Refresh()
        {
            string profilesDir = pathService.AppDataPath + @"\Profiles\";
            if (!Directory.Exists(profilesDir))
                return;

            string[] files = Directory.GetFiles(profilesDir, "*.xml");
            var diskProfileNames = new HashSet<string>(
                files.Select(f => Path.GetFileNameWithoutExtension(f)),
                StringComparer.CurrentCultureIgnoreCase);

            // 1. ディスクから削除されたプロファイルのみリストから除去
            for (int i = profileListCol.Count - 1; i >= 0; i--)
            {
                if (!diskProfileNames.Contains(profileListCol[i].Name))
                {
                    profileListCol.RemoveAt(i);
                }
            }

            // 2. ディスク上に新設されたプロファイルのみをソート順で挿入（既存アイテムはそのまま維持）
            foreach (string name in diskProfileNames.OrderBy(n => n))
            {
                if (!profileListCol.Any(x => string.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase)))
                {
                    AddProfileSort(name);
                }
            }
        }

        public void AddProfileSort(string profilename)
        {
            // 重複チェック: 既に同名のプロファイルが存在する場合はスキップ
            if (profileListCol.Any(x => string.Equals(x.Name, profilename, StringComparison.CurrentCultureIgnoreCase)))
            {
                return;
            }

            int idx = 0;
            bool inserted = false;
            foreach (ProfileEntity entry in profileListCol)
            {
                if (entry.Name.CompareTo(profilename) > 0)
                {
                    profileListCol.Insert(idx, new ProfileEntity() { Name = profilename });
                    inserted = true;
                    break;
                }
                idx++;
            }

            if (!inserted)
            {
                profileListCol.Add(new ProfileEntity() { Name = profilename });
            }
        }

        public void RemoveProfile(string profile)
        {
            var selectedEntity = profileListCol.SingleOrDefault(x => string.Equals(x.Name, profile, StringComparison.CurrentCultureIgnoreCase));
            if (selectedEntity != null)
            {
                int selectedIndex = profileListCol.IndexOf(selectedEntity);
                profileListCol.RemoveAt(selectedIndex);
            }
        }
    }
}