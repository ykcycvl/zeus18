using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace zeus.HelperClass
{
    [XmlRootAttribute("terminal")]
    public class TerminalInfo
    {
        [XmlIgnore]
        private string FileName = "config\\terminal.xml";
        [XmlElement("terminal_id")]
        public string TerminalID { get; set; }
        [XmlElement("password")]
        public string Password { get; set; }
        [XmlElement("pin")]
        public string PIN { get; set; }
        [XmlElement("secret_number")]
        public string SecretNumber { get; set; }
        [XmlElement("agent_inn")]
        public string AgentINN { get; set; }
        [XmlElement("agent_support_phone")]
        public string AgentSupportPhone { get; set; }
        [XmlElement("bank")]
        public string Bank { get; set; }
        [XmlElement("terms_number")]
        public string TermsNumber { get; set; }
        [XmlElement("agent_adress")]
        public string AgentAddress { get; set; }
        [XmlElement("configuration-id")]
        public string ConfigurationID { get; set; }
        [XmlElement("interface")]
        public string Interface { get; set; }
        [XmlElement("terminal_adress")]
        public string TerminalAddress { get; set; }

        public TerminalInfo Load()
        {
            return Deserialize();
        }

        private string Serialize()
        {
            XmlWriterSettings settings = new XmlWriterSettings()
            {
                Indent = false,
                OmitXmlDeclaration = true
            };

            var stream = new MemoryStream();
            using (XmlWriter xw = XmlWriter.Create(stream, settings))
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
                XmlSerializer x = new XmlSerializer(GetType(), "");
                x.Serialize(xw, this, ns);
            }

            string s = Encoding.UTF8.GetString(stream.ToArray());
            return s;
        }

        private TerminalInfo Deserialize()
        {
            string xml = File.ReadAllText(FileName).ToLower();

            XmlSerializer serializer = new XmlSerializer(typeof(TerminalInfo));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            TerminalInfo obj = (TerminalInfo)serializer.Deserialize(ms);
            return obj;
        }
    }
}
