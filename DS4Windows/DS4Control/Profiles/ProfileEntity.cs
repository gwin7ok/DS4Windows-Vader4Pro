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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS4WinWPF
{
    public class ProfileEntity
    {
        private string name;
        public string Name
        {
            get => name;
            set
            {
                if (name == value) return;
                name = value;
                NameChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler NameChanged;
        // Phase6-Step10-4-1: Global.appdatapath の直接参照を廃止し、IPathService を ProfileList から受け取る
        private readonly DS4Windows.DI.IPathService pathService;

        public ProfileEntity(DS4Windows.DI.IPathService pathService)
        {
            this.pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        }

        public event EventHandler ProfileDeleted;

        public void DeleteFile()
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                string filepath = pathService.AppDataPath + @"\Profiles\" + name + ".xml";
                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                    ProfileDeleted?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void RenameProfile(string newProfileName)
        {
            string oldFilePath = Path.Combine(pathService.AppDataPath,
                "Profiles", $"{name}.xml");

            string newFilePath = Path.Combine(pathService.AppDataPath,
                "Profiles", $"{newProfileName}.xml");

            if (File.Exists(oldFilePath) && !File.Exists(newFilePath))
            {
                File.Move(oldFilePath, newFilePath);
                // Send NameChanged event so controls get updated with new name
                Name = newProfileName;
            }
        }
    }
}
