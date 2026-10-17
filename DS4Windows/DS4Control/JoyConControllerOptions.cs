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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using DS4Windows.InputDevices;
using DS4WinWPF.DS4Control.DTOXml;

namespace DS4Windows
{

    public class JoyConControllerOptions : ControllerOptionsStore
    {
        public const string XML_ELEMENT_NAME = "JoyConSupportSettings";

        private bool enableHomeLED = true;
        public bool EnableHomeLED
        {
            get => enableHomeLED;
            set
            {
                if (enableHomeLED == value) return;
                enableHomeLED = value;
                EnableHomeLEDChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler EnableHomeLEDChanged;

        public JoyConControllerOptions(InputDeviceType deviceType) :
            base(deviceType)
        {
        }

        public override void PersistSettings(XmlDocument xmlDoc, XmlNode node)
        {
            string testStr = string.Empty;
            XmlSerializer serializer = new XmlSerializer(typeof(JoyConControllerOptsDTO));

            using (Utf8StringWriter strWriter = new Utf8StringWriter())
            {
                using XmlWriter xmlWriter = XmlWriter.Create(strWriter,
                    new XmlWriterSettings()
                    {
                        Encoding = Encoding.UTF8,
                        Indent = false,
                        OmitXmlDeclaration = true, // only partial XML with no declaration
                    });

                // Write root element and children
                JoyConControllerOptsDTO dto = new JoyConControllerOptsDTO();
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

            XmlNode tempJoyConNode = xmlDoc.CreateDocumentFragment();
            tempJoyConNode.InnerXml = testStr;

            XmlNode tempOptsNode = node.SelectSingleNode(XML_ELEMENT_NAME);
            if (tempOptsNode != null)
            {
                node.RemoveChild(tempOptsNode);
            }

            tempOptsNode = tempJoyConNode;
            node.AppendChild(tempOptsNode);
        }

        public override void LoadSettings(XmlDocument xmlDoc, XmlNode node)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(JoyConControllerOptsDTO));
            XmlNode baseNode = node.SelectSingleNode(XML_ELEMENT_NAME);
            if (baseNode == null)
                return;

            try
            {
                using var stringReader = new StringReader(baseNode.OuterXml);
                using var xmlReader = XmlReader.Create(stringReader);
                JoyConControllerOptsDTO dto = serializer.Deserialize(xmlReader) as JoyConControllerOptsDTO;
                dto.MapTo(this);
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
