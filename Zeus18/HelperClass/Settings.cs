using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace zeus.HelperClass
{
    [XmlRootAttribute("settings")]
    public class Settings
    {
        [XmlIgnore]
        private string FileName = "config\\settings.xml";
        [XmlIgnore]
        public string Args;
        [XmlIgnore]
        public bool AtolDriver;
        [XmlIgnore]
        public bool FiscalRegister;
        [XmlIgnore]
        public int Inches; // Сколько дюймов использовать.
        [XmlIgnore]
        private DateTime _Last_Report;
        [XmlIgnore]
        private DateTime _LastReport;
        [XmlElement("auto-zreport")]
        public string AutoZReport { get; set; }
        [XmlElement("blocked")]
        public SafeBool Blocked { get; set; }
        [XmlElement("can-flash-initialize")]
        public SafeBool CanFlashInitialize { get; set; }
        [XmlElement("fiscal-mode")]
        public SafeBool FiscalMode { get; set; }
        [XmlElement("last_report")]
        public string Last_Report 
        {
            get
            {
                return _Last_Report.ToString("dd.MM.yyyy HH:mm:ss");
            }
            set
            {
                _Last_Report = DateTime.Parse(value);
            }
        }
        [XmlElement("last-report")]
        public string LastReport 
        {
            get
            {
                return _LastReport.ToString("dd.MM.yyyy HH:mm:ss");
            }
            set
            {
                _LastReport = DateTime.Parse(value);
            }
        }
        [XmlIgnore]
        public bool MasterPinActive = false; // Сколько дюймов использовать.
        [XmlIgnore]
        public string modemPort;
        [XmlIgnore]
        public string NetModemName;
        [XmlIgnore]
        public int NetOption; // 0 - Локалка; 1- Модем
        [XmlIgnore]
        public float RequestTimeout; // Устанавливаем таймаут обращения к серваку (секунды)
        [XmlIgnore]
        public Size Resolution; // Сколько вешать в пискселях.
        [XmlIgnore]
        public string[] ServiceUrl;
        [XmlIgnore]
        public int ServiceUrlIndex;
        [XmlIgnore]
        public string StartupPath = "";
        [XmlIgnore]
        public string[] UpdateUrl;
        [XmlIgnore]
        public int UpdateUrlIndex;
        [XmlIgnore]
        public bool WindowsPrinter;

        public Settings Load()
        {
            return Deserialize();
        }

        public void SaveSettings()
        {
            string xml = Serialize();
            File.WriteAllText(FileName, xml);
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

        private Settings Deserialize()
        {
            string xml = File.ReadAllText(FileName).ToLower();

            XmlSerializer serializer = new XmlSerializer(typeof(Settings));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            Settings obj = (Settings)serializer.Deserialize(ms);
            return obj;
        }
    }
}
