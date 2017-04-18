using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace zeus.HelperClass
{
    [XmlRootAttribute("response")]
    public class RFRSettings
    {
        [XmlRoot("frs")]
        public class FRS
        {
            [XmlElement("remoteFR")]
            public SafeBool RemoteFR { get; set; }
            [XmlElement("checkWidth")]
            public int checkWidth { get; set; }
            [XmlElement("remoteFRtimeout")]
            public int Timeout { get; set; }
            [XmlElement("remoteFRurl")]
            public string URL { get; set; }
            [XmlElement("headertext")]
            public string HeaderText { get; set; }
        }
        [XmlIgnore]
        private string FileName = "config\\frssettings.xml";
        [XmlElement("blocked")]
        public bool Blocked { get; set; }
        [XmlElement("configuration-id")]
        public int ConfigurationID { get; set; }
        [XmlElement("speed")]
        public string Speed { get; set; }
        [XmlElement("frs")]
        public FRS frs { get; set; }

        public RFRSettings Load()
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

        private RFRSettings Deserialize()
        {
            try
            {
                string xml = File.ReadAllText(FileName);

                XmlSerializer serializer = new XmlSerializer(typeof(RFRSettings));
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml));
                RFRSettings obj = (RFRSettings)serializer.Deserialize(ms);
                return obj;
            }
            catch (Exception ex)
            {
                IPayBox.AddToLog(IPayBox.Logs.Main, ex.Message);
            }

            return null;
        }
    }
}
