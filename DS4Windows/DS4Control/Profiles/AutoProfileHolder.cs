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
using System.Windows.Data;
using System.Xml;
using System.Xml.Serialization;
using DS4Windows;
using DS4WinWPF.DS4Control.DTOXml;

namespace DS4WinWPF
{
    public class AutoProfileHolder
    {
        private object _colLockobj = new object();
        // Phase6-Step10-4-2b: Global.appdatapath の直接参照を廃止し、IPathService を必須の依存として受け取る
        private readonly DS4Windows.DI.IPathService pathService;
        private ObservableCollection<AutoProfileEntity> autoProfileColl;
        public ObservableCollection<AutoProfileEntity> AutoProfileColl { get => autoProfileColl; }
        //public Dictionary<string, AutoProfileEntity> AutoProfileDict { get => autoProfileDict; }

        //private Dictionary<string, AutoProfileEntity> autoProfileDict;

        // Phase7-Step10b W4: 旧 ControlService.USING_MAX_CONTROLLERS（静的シム）の置換。保存時（AutoProfilesDTO.MapFrom）に、
        // 4〜8 番目のコントローラーのプロファイルを書くかの判定に使う。
        private readonly DS4Windows.DI.IEnvironmentService environmentService;
        public bool UsingMaxControllers => environmentService.UsingMaxControllers;

        public AutoProfileHolder(DS4Windows.DI.IPathService pathService, DS4Windows.DI.IEnvironmentService environmentService)
        {
            this.pathService = pathService ?? throw new System.ArgumentNullException(nameof(pathService));
            this.environmentService = environmentService ?? throw new System.ArgumentNullException(nameof(environmentService));
            autoProfileColl = new ObservableCollection<AutoProfileEntity>();
            //autoProfileDict = new Dictionary<string, AutoProfileEntity>();
            Load();

            BindingOperations.EnableCollectionSynchronization(autoProfileColl, _colLockobj);
        }

        private void Load()
        {
            string configFile = Path.Combine(pathService.AppDataPath, "Auto Profiles.xml");
            if (!File.Exists(configFile))
                return;

            XmlSerializer serializer = new XmlSerializer(typeof(AutoProfilesDTO));
            using StreamReader sr = new StreamReader(configFile);
            try
            {
                AutoProfilesDTO dto = serializer.Deserialize(sr) as AutoProfilesDTO;
                dto.MapTo(this);
            }
            catch (InvalidOperationException) {}
            catch (XmlException) {}
        }

        public bool Save(string m_Profile)
        {
            bool saved = true;

            string output_path = m_Profile;
            string testStr = string.Empty;
            XmlSerializer serializer = new XmlSerializer(typeof(AutoProfilesDTO));
            using (Utf8StringWriter strWriter = new Utf8StringWriter())
            {
                using XmlWriter xmlWriter = XmlWriter.Create(strWriter,
                    new XmlWriterSettings()
                    {
                        Encoding = Encoding.UTF8,
                        Indent = true,
                    });

                // Write header explicitly
                //xmlWriter.WriteStartDocument();
                xmlWriter.WriteComment(string.Format(" Auto-Profile Configuration Data. {0} ", DateTime.Now));
                xmlWriter.WriteWhitespace("\r\n");
                xmlWriter.WriteWhitespace("\r\n");

                // Write root element and children
                AutoProfilesDTO dto = new AutoProfilesDTO();
                dto.MapFrom(this);
                // Omit xmlns:xsi and xmlns:xsd from output
                serializer.Serialize(xmlWriter, dto,
                    new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty }));
                xmlWriter.Flush();
                xmlWriter.Close();

                testStr = strWriter.ToString();
                //Trace.WriteLine("TEST OUTPUT");
                //Trace.WriteLine(testStr);
            }

            try
            {
                using (StreamWriter sw = new StreamWriter(output_path, false))
                {
                    sw.Write(testStr);
                }
            }
            catch (UnauthorizedAccessException)
            {
                saved = false;
            }

            return saved;
        }

        /// <summary>
        /// Phase6-Step10-4-5c: プロファイルの名前変更に伴い、各ルールが参照している旧名（コントローラーごとのプロファイル名）を
        /// 新名へ置き換える。置き換えた件数を返す。1 件以上置き換えた場合は、Auto Profiles.xml へ保存する。
        /// </summary>
        public int ReplaceProfileName(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return 0;

            int replaced = 0;
            lock (_colLockobj)
            {
                foreach (AutoProfileEntity entity in autoProfileColl)
                {
                    string[] names = entity.ProfileNames;
                    if (names == null) continue;

                    for (int i = 0; i < names.Length; i++)
                    {
                        if (string.Equals(names[i], oldName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            names[i] = newName;
                            replaced++;
                        }
                    }
                }
            }

            if (replaced > 0)
            {
                Save(Path.Combine(pathService.AppDataPath, "Auto Profiles.xml"));
            }

            return replaced;
        }

        public void Remove(AutoProfileEntity item)
        {
            //autoProfileDict.Remove(item.Path);
            autoProfileColl.Remove(item);
        }
    }
}
