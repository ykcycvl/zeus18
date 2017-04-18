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
    public class TPIN
    {
        [XmlIgnore]
        private string FileName = "config\\tpin.xml";
        [XmlRoot("pins")]
        public class Person
        {
            [XmlAttribute("pid")]
            public int PID { get; set; }
            [XmlAttribute("name")]
            public string Name { get; set; }
            [XmlAttribute("pin")]
            public string PIN { get; set; }
        }
        [XmlArray("pins"), XmlArrayItem("person")]
        public List<Person> Persons { get; set; }
        [XmlElement("blocked")]
        public bool Blocked { get; set; }
        [XmlElement("configuration-id")]
        public int ConfigurationID { get; set; }
        [XmlElement("speed")]
        public string Speed { get; set; }

        public TPIN Load()
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

        private TPIN Deserialize()
        {
            try
            {
                string xml = File.ReadAllText(FileName);

                XmlSerializer serializer = new XmlSerializer(typeof(TPIN));
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml));
                TPIN obj = (TPIN)serializer.Deserialize(ms);
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
