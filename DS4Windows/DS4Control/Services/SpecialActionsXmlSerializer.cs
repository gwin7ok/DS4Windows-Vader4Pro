using DS4Windows.DS4Control;
using DS4Windows.InputDevices;
using DS4Windows.StickModifiers;
using DS4WinWPF.DS4Control.DTOXml;
using DS4WinWPF.DS4Forms.ViewModels;
using Sensorit.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;
using DS4WinWPF;
using DS4WinWPF.ApiDTO;
using WpfScreenHelper;
using static DS4Windows.Mouse;
using static DS4Windows.Util;
using LightbarMacro = DS4WinWPF.DS4Forms.ViewModels.LightbarMacro;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase7-Step10c W2（Q1＝A）: スペシャルアクション（<c>Actions.xml</c>）の保存・読込。
    /// 旧: <c>BackingStore</c> の <c>CreateAction</c>／<c>PrepareActionsXml</c>／<c>SaveActions</c>／<c>SaveActionNew</c>／<c>SaveAction</c>／<c>RemoveAction</c>／<c>LoadActions</c>
    /// （移動のみ。ロジック・ログ・要素の順序は変えない。<c>BackingStore</c> の公開状態を <c>store.</c> で読み書きする）。
    /// <c>LoadActions</c> は、読み込みの後に <c>BackingStore.RaiseActionsLoaded</c>（<c>ActionsLoaded</c> イベント）を発行する。
    /// </summary>
    public class SpecialActionsXmlSerializer
    {
        private readonly BackingStore store;
        private readonly XmlDocument m_Xdoc = new XmlDocument(); // 旧 BackingStore.m_Xdoc と同じ使い方（排他は呼び出し側のロック）

        public SpecialActionsXmlSerializer(BackingStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        private void CreateAction()
        {
            XmlDocument m_Xdoc = new XmlDocument();
            PrepareActionsXml(m_Xdoc);
            m_Xdoc.Save(store.m_Actions);
        }

        private void PrepareActionsXml(XmlDocument xmlDoc)
        {
            XmlNode Node;

            Node = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", String.Empty);
            xmlDoc.AppendChild(Node);

            Node = xmlDoc.CreateComment(String.Format(" Special Actions Configuration Data. {0} ", DateTime.Now));
            xmlDoc.AppendChild(Node);

            Node = xmlDoc.CreateWhitespace("\r\n");
            xmlDoc.AppendChild(Node);

            Node = xmlDoc.CreateNode(XmlNodeType.Element, "Actions", "");
            xmlDoc.AppendChild(Node);
        }

        public bool SaveActions()
        {
            bool saved = true;

            string output_path = store.m_Actions;
            string testStr = string.Empty;
            XmlSerializer serializer = new XmlSerializer(typeof(ActionsDTO));
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
                xmlWriter.WriteComment(String.Format(" Special Actions Configuration Data. {0} ", DateTime.Now));
                xmlWriter.WriteWhitespace("\r\n");
                xmlWriter.WriteWhitespace("\r\n");

                // Write root element and children
                ActionsDTO dto = new ActionsDTO();
                dto.MapFrom(store);
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

        public void SaveActionNew(string name, string controls, int mode, string details, bool edit, double delayTime = 0.0, string extras = "")
        {
            SpecialAction tempAction = null;

            switch (mode)
            {
                case 1:
                    tempAction = new SpecialAction(name, controls, "Macro", details, extras: extras);
                    break;
                case 2:
                    string[] tempDetails = details.Split("?");
                    //double doub = 0.0;
                    //double.TryParse(tempDetails[1], out doub);
                    tempAction = new SpecialAction(name, controls, "Program", tempDetails[0],
                        delay: delayTime, extras: extras);
                    break;
                case 3:
                    tempAction = new SpecialAction(name, controls, "Profile", details, extras: extras);
                    break;
                case 4:
                    tempAction = new SpecialAction(name, controls, "Key", details, extras: extras);
                    break;
                case 10:
                    // Button mapping for SpecialAction
                    tempAction = new SpecialAction(name, controls, "Button", details, extras: extras);
                    break;
                case 5:
                    tempAction = new SpecialAction(name, controls, "DisconnectBT", details, delayTime);
                    break;
                case 6:
                    tempAction = new SpecialAction(name, controls, "BatteryCheck", details, delayTime);
                    break;
                case 7:
                    tempAction = new SpecialAction(name, controls, "MultiAction", details);
                    break;
                case 8:
                    tempAction = new SpecialAction(name, controls, "SASteeringWheelEmulationCalibrate",
                        details, delayTime);
                    break;
                case 9:
                    tempAction = new SpecialAction(name, controls, "GyroCalibration", details, delayTime);
                    break;
                default:
                    break;
            }

            if (edit)
            {
                int tempIndex = store.actions.FindIndex(item => item.name == name);
                if (tempIndex != -1 && tempAction != null)
                {
                    store.actions[tempIndex] = tempAction;
                }
            }
            else if (tempAction != null)
            {
                store.actions.Add(tempAction);
            }

            SaveActions();
        }

        public bool SaveAction(string name, string controls, int mode, string details, bool edit, string extras = "")
        {
            bool saved = true;
            if (!File.Exists(store.m_Actions))
                CreateAction();

            try
            {
                m_Xdoc.Load(store.m_Actions);
            }
            catch (XmlException)
            {
                // XML file has become corrupt. Start from scratch
                AppLogger.LogToGui(DS4WinWPF.Properties.Resources.XMLActionsCorrupt, true);
                m_Xdoc.RemoveAll();
                PrepareActionsXml(m_Xdoc);
            }

            XmlNode Node;

            Node = m_Xdoc.CreateComment(String.Format(" Special Actions Configuration Data. {0} ", DateTime.Now));
            foreach (XmlNode node in m_Xdoc.SelectNodes("//comment()"))
                node.ParentNode.ReplaceChild(Node, node);

            Node = m_Xdoc.SelectSingleNode("Actions");
            XmlElement el = m_Xdoc.CreateElement("Action");
            el.SetAttribute("Name", name);
            el.AppendChild(m_Xdoc.CreateElement("Trigger")).InnerText = controls;
            switch (mode)
            {
                case 1:
                    el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "Macro";
                    el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details;
                    if (extras != string.Empty)
                        el.AppendChild(m_Xdoc.CreateElement("Extras")).InnerText = extras;
                    break;
                case 2:
                    el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "Program";
                    el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details.Split('?')[0];
                    el.AppendChild(m_Xdoc.CreateElement("Arguements")).InnerText = extras;
                    el.AppendChild(m_Xdoc.CreateElement("Delay")).InnerText = details.Split('?')[1];
                    break;
                case 3:
                    el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "Profile";
                    el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details;
                    el.AppendChild(m_Xdoc.CreateElement("UnloadTrigger")).InnerText = extras;
                    break;
                case 4:
                    el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "Key";
                    el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details;
                    if (!string.IsNullOrEmpty(extras))
                    {
                        string[] exts = extras.Split('\n');
                        el.AppendChild(m_Xdoc.CreateElement("UnloadTrigger")).InnerText = exts.Length > 1 ? exts[1] : string.Empty;
                        // New element name: SwitchMode. Values: "Press" or "Toggle". Keep compatibility with older UnloadStyle.
                        el.AppendChild(m_Xdoc.CreateElement("SwitchMode")).InnerText = exts.Length > 0 ? exts[0] : string.Empty;
                    }
                    break;
                case 10:
                    // Button mapping
                    el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "Button";
                    el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details;
                    break;
                case 5:
                    el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "DisconnectBT";
                    el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details;
                    break;
                case 6:
                    el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "BatteryCheck";
                    el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details;
                    break;
                case 7:
                    el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "MultiAction";
                    el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details;
                    break;
                case 8:
                    el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "SASteeringWheelEmulationCalibrate";
                    el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details;
                    break;
            }

            if (edit)
            {
                XmlNode oldxmlprocess = m_Xdoc.SelectSingleNode("/Actions/Action[@Name=\"" + name + "\"]");
                Node.ReplaceChild(el, oldxmlprocess);
            }
            else { Node.AppendChild(el); }

            m_Xdoc.AppendChild(Node);
            try { m_Xdoc.Save(store.m_Actions); }
            catch { saved = false; }
            LoadActions();
            return saved;
        }

        public void RemoveAction(string name)
        {
            int tempIndex = store.actions.FindIndex(item => item.name == name);
            if (tempIndex != -1)
            {
                store.actions.RemoveAt(tempIndex);
            }

            SaveActions();

            //m_Xdoc.Load(m_Actions);
            //XmlNode Node = m_Xdoc.SelectSingleNode("Actions");
            //XmlNode Item = m_Xdoc.SelectSingleNode("/Actions/Action[@Name=\"" + name + "\"]");
            //if (Item != null)
            //    Node.RemoveChild(Item);

            //m_Xdoc.AppendChild(Node);
            //m_Xdoc.Save(m_Actions);
            //LoadActions();
        }

        public bool LoadActions()
        {
            bool loaded = true;

            store.actions.Clear();
            try { ActionManager.ClearAllEntries(); } catch { }

            //string configFile = Path.Combine(Global.appdatapath, "Actions.xml");
            if (!File.Exists(store.m_Actions))
            {
                store.actions.Add(new SpecialAction("Disconnect Controller", "PS/Options", "DisconnectBT", "0"));
                loaded = SaveActions();

                // Reset runtime synthetic state and ActionManager entries after creating defaults
                if (loaded)
                {
                    try
                    {
                        try { ActionManager.ClearAllEntries(); } catch { }
                        // Phase7-Step7 W2（Q4＝A）: 合成入力・マクロの実行時状態のリセット（旧: Mapping の静的な状態を直接書き換え）は、
                        // ActionsLoaded の購読者（ServiceRegistration が登録する ActionsLoadedRuntimeReset）が行う。
                        store.RaiseActionsLoaded();
                    }
                    catch { }
                }

                return loaded;
            }

            XmlSerializer serializer = new XmlSerializer(typeof(ActionsDTO));
            using StreamReader sr = new StreamReader(store.m_Actions);
            try
            {
                ActionsDTO dto = serializer.Deserialize(sr) as ActionsDTO;
                dto.MapTo(store);
            }
            catch (InvalidOperationException e)
            {
                AppLogger.LogToGui($"Actions.xml contains invalid data. Could not be read. {e.InnerException.Message}", false);
                loaded = false;
            }
            catch (XmlException e)
            {
                AppLogger.LogToGui($"Actions.xml could not be read. Invalid XML syntax. {e.InnerException.Message}", false);
                loaded = false;
            }

            // After loading actions, clear ActionManager entries and reset runtime state
            if (loaded)
            {
                try
                {
                    try { ActionManager.ClearAllEntries(); } catch { }
                    store.RaiseActionsLoaded();
                }
                catch { }
            }

            return loaded;

            //bool saved = true;
            //if (!File.Exists(Global.appdatapath + "\\Actions.xml"))
            //{
            //    SaveAction("Disconnect Controller", "PS/Options", 5, "0", false);
            //    saved = false;
            //}

            //try
            //{
            //    actions.Clear();
            //    XmlDocument doc = new XmlDocument();
            //    doc.Load(Global.appdatapath + "\\Actions.xml");
            //    XmlNodeList actionslist = doc.SelectNodes("Actions/Action");
            //    string name, controls, type, details, extras, extras2;
            //    Mapping.actionDone.Clear();
            //    foreach (XmlNode x in actionslist)
            //    {
            //        name = x.Attributes["Name"].Value;
            //        controls = x.ChildNodes[0].InnerText;
            //        type = x.ChildNodes[1].InnerText;
            //        details = x.ChildNodes[2].InnerText;
            //        Mapping.actionDone.Add(new Mapping.ActionState());
            //        if (type == "Profile")
            //        {
            //            extras = x.ChildNodes[3].InnerText;
            //            actions.Add(new SpecialAction(name, controls, type, details, 0, extras));
            //        }
            //        else if (type == "Macro")
            //        {
            //            if (x.ChildNodes[3] != null) extras = x.ChildNodes[3].InnerText;
            //            else extras = string.Empty;
            //            actions.Add(new SpecialAction(name, controls, type, details, 0, extras));
            //        }
            //        else if (type == "Key")
            //        {
            //            if (x.ChildNodes[3] != null)
            //            {
            //                extras = x.ChildNodes[3].InnerText;
            //                extras2 = x.ChildNodes[4].InnerText;
            //            }
            //            else
            //            {
            //                extras = string.Empty;
            //                extras2 = string.Empty;
            //            }
            //            if (!string.IsNullOrEmpty(extras))
            //                actions.Add(new SpecialAction(name, controls, type, details, 0, extras2 + '\n' + extras));
            //            else
            //                actions.Add(new SpecialAction(name, controls, type, details));
            //        }
            //        else if (type == "DisconnectBT")
            //        {
            //            double doub;
            //            if (double.TryParse(details, System.Globalization.NumberStyles.Float, Global.configFileDecimalCulture, out doub))
            //                actions.Add(new SpecialAction(name, controls, type, "", doub));
            //            else
            //                actions.Add(new SpecialAction(name, controls, type, ""));
            //        }
            //        else if (type == "BatteryCheck")
            //        {
            //            double doub;
            //            if (double.TryParse(details.Split('|')[0], System.Globalization.NumberStyles.Float, Global.configFileDecimalCulture, out doub))
            //                actions.Add(new SpecialAction(name, controls, type, details, doub));
            //            else if (double.TryParse(details.Split(',')[0], System.Globalization.NumberStyles.Float, Global.configFileDecimalCulture, out doub))
            //                actions.Add(new SpecialAction(name, controls, type, details, doub));
            //            else
            //                actions.Add(new SpecialAction(name, controls, type, details));
            //        }
            //        else if (type == "Program")
            //        {
            //            double doub;
            //            if (x.ChildNodes[3] != null)
            //            {
            //                extras = x.ChildNodes[3].InnerText;
            //                if (double.TryParse(x.ChildNodes[4].InnerText, System.Globalization.NumberStyles.Float, Global.configFileDecimalCulture, out doub))
            //                    actions.Add(new SpecialAction(name, controls, type, details, doub, extras));
            //                else
            //                    actions.Add(new SpecialAction(name, controls, type, details, 0, extras));
            //            }
            //            else
            //            {
            //                actions.Add(new SpecialAction(name, controls, type, details));
            //            }
            //        }
            //        else if (type == "XboxGameDVR" || type == "MultiAction")
            //        {
            //            actions.Add(new SpecialAction(name, controls, type, details));
            //        }
            //        else if (type == "SASteeringWheelEmulationCalibrate")
            //        {
            //            double doub;
            //            if (double.TryParse(details, System.Globalization.NumberStyles.Float, Global.configFileDecimalCulture, out doub))
            //                actions.Add(new SpecialAction(name, controls, type, "", doub));
            //            else
            //                actions.Add(new SpecialAction(name, controls, type, ""));
            //        }
            //    }
            //}
            //catch { saved = false; }
            //return saved;
        }
    }
}
