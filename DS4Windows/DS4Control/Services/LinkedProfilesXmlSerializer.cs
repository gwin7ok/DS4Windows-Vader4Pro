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
    /// Phase7-Step10c W2（Q1＝A）: リンクプロファイル（<c>LinkedProfiles.xml</c>）の保存・読込。
    /// 旧: <c>BackingStore</c> の <c>createLinkedProfiles</c>／<c>LoadLinkedProfiles</c>／<c>SaveLinkedProfiles</c>（移動のみ）。
    /// </summary>
    public class LinkedProfilesXmlSerializer
    {
        private readonly BackingStore store;

        public LinkedProfilesXmlSerializer(BackingStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public bool createLinkedProfiles()
        {
            bool saved = true;

            string output_path = store.m_linkedProfiles;
            string testStr = string.Empty;
            XmlSerializer serializer = new XmlSerializer(typeof(LinkedProfilesDTO));
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
                xmlWriter.WriteComment(string.Format(" Mac Address and Profile Linking Data. {0} ", DateTime.Now));
                xmlWriter.WriteWhitespace("\r\n");
                xmlWriter.WriteWhitespace("\r\n");

                // Write root element and children
                LinkedProfilesDTO dto = new LinkedProfilesDTO();
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
                AppLogger.LogToGui("Unauthorized Access - Save failed to path: " + store.m_linkedProfiles, false);
                saved = false;
            }

            return saved;

            //bool saved = true;
            //XmlDocument m_Xdoc = new XmlDocument();
            //XmlNode Node;

            //Node = m_Xdoc.CreateXmlDeclaration("1.0", "utf-8", string.Empty);
            //m_Xdoc.AppendChild(Node);

            //Node = m_Xdoc.CreateComment(string.Format(" Mac Address and Profile Linking Data. {0} ", DateTime.Now));
            //m_Xdoc.AppendChild(Node);

            //Node = m_Xdoc.CreateWhitespace("\r\n");
            //m_Xdoc.AppendChild(Node);

            //Node = m_Xdoc.CreateNode(XmlNodeType.Element, "LinkedControllers", "");
            //m_Xdoc.AppendChild(Node);

            //try { m_Xdoc.Save(m_linkedProfiles); }
            //catch (UnauthorizedAccessException) { AppLogger.LogToGui("Unauthorized Access - Save failed to path: " + m_linkedProfiles, false); saved = false; }

            //return saved;
        }

        public bool LoadLinkedProfiles()
        {
            bool loaded = true;
            if (File.Exists(store.m_linkedProfiles))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(LinkedProfilesDTO));
                using StreamReader sr = new StreamReader(store.m_linkedProfiles);
                try
                {
                    LinkedProfilesDTO dto = serializer.Deserialize(sr) as LinkedProfilesDTO;
                    dto.MapTo(store);
                }
                catch (InvalidOperationException e)
                {
                    AppLogger.LogToGui($"LinkedProfiles.xml contains invalid data. Could not be read. {e.InnerException.Message}", false);
                }
                catch (XmlException e)
                {
                    AppLogger.LogToGui($"LinkedProfiles.xml could not be read. Invalid XML syntax. {e.InnerException.Message}", false);
                }
            }
            else
            {
                AppLogger.LogToGui("LinkedProfiles.xml can't be found.", false);
                loaded = false;
            }

            return loaded;

            //bool loaded = true;
            //if (File.Exists(m_linkedProfiles))
            //{
            //    XmlDocument linkedXdoc = new XmlDocument();
            //    XmlNode Node;
            //    linkedXdoc.Load(m_linkedProfiles);
            //    linkedProfiles.Clear();

            //    try
            //    {
            //        Node = linkedXdoc.SelectSingleNode("/LinkedControllers");
            //        XmlNodeList links = Node.ChildNodes;
            //        for (int i = 0, listLen = links.Count; i < listLen; i++)
            //        {
            //            XmlNode current = links[i];
            //            string serial = current.Name.Replace("MAC", string.Empty);
            //            string profile = current.InnerText;
            //            linkedProfiles[serial] = profile;
            //        }
            //    }
            //    catch { loaded = false; }
            //}
            //else
            //{
            //    AppLogger.LogToGui("LinkedProfiles.xml can't be found.", false);
            //    loaded = false;
            //}

            //return loaded;
        }

        public bool SaveLinkedProfiles()
        {
            bool saved = true;

            string output_path = store.m_linkedProfiles;
            if (File.Exists(store.m_linkedProfiles))
            {
                string testStr = string.Empty;
                XmlSerializer serializer = new XmlSerializer(typeof(LinkedProfilesDTO));
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
                    //xmlWriter.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\"");
                    xmlWriter.WriteComment(string.Format(" Mac Address and Profile Linking Data. {0} ", DateTime.Now));
                    xmlWriter.WriteWhitespace("\r\n");
                    xmlWriter.WriteWhitespace("\r\n");

                    // Write root element and children
                    LinkedProfilesDTO dto = new LinkedProfilesDTO();
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
                    AppLogger.LogToGui("Unauthorized Access - Save failed to path: " + store.m_linkedProfiles, false);
                    saved = false;
                }
            }
            else
            {
                saved = createLinkedProfiles();
            }

            return saved;

            //bool saved = true;
            //if (File.Exists(m_linkedProfiles))
            //{
            //    XmlDocument linkedXdoc = new XmlDocument();
            //    XmlNode Node;

            //    Node = linkedXdoc.CreateXmlDeclaration("1.0", "utf-8", string.Empty);
            //    linkedXdoc.AppendChild(Node);

            //    Node = linkedXdoc.CreateComment(string.Format(" Mac Address and Profile Linking Data. {0} ", DateTime.Now));
            //    linkedXdoc.AppendChild(Node);

            //    Node = linkedXdoc.CreateWhitespace("\r\n");
            //    linkedXdoc.AppendChild(Node);

            //    Node = linkedXdoc.CreateNode(XmlNodeType.Element, "LinkedControllers", "");
            //    linkedXdoc.AppendChild(Node);

            //    Dictionary<string, string>.KeyCollection serials = linkedProfiles.Keys;
            //    //for (int i = 0, itemCount = linkedProfiles.Count; i < itemCount; i++)
            //    for (var serialEnum = serials.GetEnumerator(); serialEnum.MoveNext();)
            //    {
            //        //string serial = serials.ElementAt(i);
            //        string serial = serialEnum.Current;
            //        string profile = linkedProfiles[serial];
            //        XmlElement link = linkedXdoc.CreateElement("MAC" + serial);
            //        link.InnerText = profile;
            //        Node.AppendChild(link);
            //    }

            //    try { linkedXdoc.Save(m_linkedProfiles); }
            //    catch (UnauthorizedAccessException) { AppLogger.LogToGui("Unauthorized Access - Save failed to path: " + m_linkedProfiles, false); saved = false; }
            //}
            //else
            //{
            //    saved = createLinkedProfiles();
            //    saved = saved && SaveLinkedProfiles();
            //}

            //return saved;
        }
    }
}
