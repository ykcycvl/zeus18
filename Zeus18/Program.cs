using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.Drawing;
using System.IO;
using Microsoft.Win32;
using System.Media;
using zeus.HelperClass;
using System.Xml;

namespace zeus
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            IPayBox.Settings = new zeus.HelperClass.Settings().Load();
            IPayBox.Settings.StartupPath = Application.StartupPath;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new main());
        }
    }

    public static class _cursor
    {
        public static bool Visible = true;

        public static void Hide()
        {
            if (Visible && !IPayBox.Debug)
            {
                Cursor.Hide();
                Visible = false;
            }
        }

        public static void Show()
        {
            if (!Visible)
            {
                Cursor.Show();
                Visible = true;
            }
        }
    }

    public static class IPayBox
    {
        public enum Logs { Main, Update }
        public static class Devices
        {
            public static Acceptors.Acceptor Acceptor;
            public static FR.FiscalRegisters FRegister;
            public static Printers.Printer Printer;
            public static WD.Watchdog Watchdog;
            public static modem Modem;
        }
        public static class Info
        {
            public static string OpsosName = string.Empty;
            public static string IMSI = string.Empty;
            public static string CoreVersion;
        }
        public static class State
        {
            public static bool IsBlocked = false;
            public static bool NeedToSendPays = false;
            public static bool NeedToUpdateConfiguration = false;
            public static bool NeedUpdateCore = false;
            public static bool NeedToRestart = false;
            public static bool NeedToReboot = false;
            public static bool NeedShutdown = false;
            public static bool Working = true; // Работать ли автомату
        }

        public static class Forms
        {
            public static int Count;   // Всего форм
            public static int Index;  // текущая форма обработки
        }

        public static bool Debug = false;
        public static HelperClass.RFRSettings RFRSettings;
        public static Statistic Incass;
        public static HelperClass.Incass Incassation;           // Объект инкассации
        public static bool IncassCheck = false;
        public static bool Internet;
        public static HelperClass.Import Import;
        public static Settings Settings;
        public static main StartForm;
        public static uint userID = 1;
        public static PrintDocument doc;
        public static string PrintString;
        public static string SELECTED_PRV_ID;
        public static PAY curPay;
        public static TERMINAL Terminal;
        public static HelperClass.TerminalInfo TerminalInfo;
        public static HelperClass.TPIN TPIN;
        public static XmlDocument XML_Statistic;        // XML структура статистики

        public struct PAY
        {
            public string prv_id;
            public string prv_name;
            public string prv_img;
            public string account;
            public Decimal from_amount;
            public Decimal to_amount;
            public string extra;
            public bool IsAccountConfirmed; // Подтверждены ли введенные данные
            public bool IsMoney;            // Всунуты ли деньги
            public bool IsRecieptPrinted;   //Напечатан ли чек
            public bool IsOnlyOnline;       // онлайновый ли это провайдер?
            public string Options;
            public DateTime Date;
            public string txn_id;
            public bool IsPaySended;
            public List<HelperClass.AccountInfo> ViewText;
        }
        public struct Statistic
        {
            public int count; // кол-во бумажек всунуто
            public int countchecks; // кол-во чеков распечатано
            public int bytesSend; // байт послано
            public int bytesRead; // байт принято
            public int incass_amount; // сумма инкасации
            public DateTime incass_date_start; // Дата начала инкасации
            public int CountR10;
            public int CountR50;
            public int CountR100;
            public int CountR500;
            public int CountR1000;
            public int CountR5000;
        }
        public struct TERMINAL
        {
            public string jur_name;
            public string jur_inn;
            public string jur_adress;
            public string bank;
            public string trm_adress;
            public string terms_number;
            public string support_phone;
            public string terminal_id;
            public string terminal_pass;
            public string pincode;
            public string secret_number;
            public string configuration_id;
            public string Interface;
            public string InterfaceVersion;
            public bool FiscalMode;
        }
        public struct FRS
        {
            public static string RemoteFiscalRegisterURL = "http://localhost:5881/buy/";
            public static string headertext = "";
            public static bool RemoteFR = false;
            public static int remoteFRtimeout = 9000;
            public static int checkWidth = 38;
        }

        public static void AddToLog(Logs l, string message)
        {
            if (IPayBox.Debug)
                System.Diagnostics.Debug.WriteLine(message);
            FileInfo fi = null;
            DirectoryInfo di = new DirectoryInfo(IPayBox.Settings.StartupPath + "\\logs");
            if (!di.Exists)
                di.Create();
            switch (l)
            {
                case Logs.Main: fi = new FileInfo(IPayBox.Settings.StartupPath + "\\logs\\" + DateTime.Now.ToString("yyyy-MM-dd") + ".log"); break;
                case Logs.Update: fi = new FileInfo(IPayBox.Settings.StartupPath + "\\logs\\update.log"); break;
                default:
                    fi = new FileInfo(IPayBox.Settings.StartupPath + "\\logs\\default.log"); break;
            }
            StreamWriter sw = null;
            try
            {
                if (fi.Exists)
                    sw = fi.AppendText();
                else
                    sw = fi.CreateText();

                string append = DateTime.Now.ToString("HH:mm:ss") + " " + message;

                sw.WriteLine(append);

            }
            catch
            { }
            finally
            {
                if (sw != null)
                    sw.Close();
            }


        }
        public static void FlushStatistic()
        {
            XmlDocument doc = new XmlDocument();

            doc.LoadXml("<statistic></statistic>");

            // кол-во купюр
            XmlElement el = doc.CreateElement("count_bill");
            el.InnerText = IPayBox.Incass.count.ToString();

            XmlNode root = doc.DocumentElement;
            root.InsertAfter(el, root.FirstChild);

            // кол-во 10
            el = doc.CreateElement("count10");
            el.InnerText = IPayBox.Incass.CountR10.ToString();
            root.InsertAfter(el, root.FirstChild);

            // кол-во 50
            el = doc.CreateElement("count50");
            el.InnerText = IPayBox.Incass.CountR50.ToString();
            root.InsertAfter(el, root.FirstChild);

            // кол-во 100
            el = doc.CreateElement("count100");
            el.InnerText = IPayBox.Incass.CountR100.ToString();
            root.InsertAfter(el, root.FirstChild);
            // кол-во 500
            el = doc.CreateElement("count500");
            el.InnerText = IPayBox.Incass.CountR500.ToString();
            root.InsertAfter(el, root.FirstChild);
            // кол-во 1000
            el = doc.CreateElement("count1000");
            el.InnerText = IPayBox.Incass.CountR1000.ToString();
            root.InsertAfter(el, root.FirstChild);

            // кол-во 5000
            el = doc.CreateElement("count5000");
            el.InnerText = IPayBox.Incass.CountR5000.ToString();
            root.InsertAfter(el, root.FirstChild);

            // кол-во чеков
            el = doc.CreateElement("count_check");
            el.InnerText = IPayBox.Incass.countchecks.ToString();
            root.InsertAfter(el, root.FirstChild);

            // количество принятых байт
            el = doc.CreateElement("bytes_read");
            el.InnerText = IPayBox.Incass.bytesRead.ToString();
            root.InsertAfter(el, root.FirstChild);

            // количество переданных байт
            el = doc.CreateElement("bytes_send");
            el.InnerText = IPayBox.Incass.bytesSend.ToString();
            root.InsertAfter(el, root.FirstChild);

            // сумма инкасации
            el = doc.CreateElement("amount");
            el.InnerText = IPayBox.Incass.incass_amount.ToString();
            root.InsertAfter(el, root.FirstChild);
            // сумма инкасации
            el = doc.CreateElement("incass-start-date");
            el.InnerText = IPayBox.Incass.incass_date_start.ToString();
            root.InsertAfter(el, root.FirstChild);
            //
            doc.Save(IPayBox.Settings.StartupPath + @"\incass.xml");
        }
        public static void HideExplorer()
        {
            RegistryKey OurKey = Registry.LocalMachine;
            OurKey = OurKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", true);
            OurKey.SetValue("AutoRestartShell", 0);

            System.Diagnostics.Process[] pr = System.Diagnostics.Process.GetProcessesByName("explorer");

            for (int i = 0; i < pr.Length; i++)
            {
                pr[i].Kill();
            }
        }
        public static void LoadIncass()
        {
            Incassation = new zeus.HelperClass.Incass(IPayBox.Settings.StartupPath.TrimEnd('\\') + "\\config\\incass.xml");
            Incassation.AutoCommit = true;

            IPayBox.Import = new zeus.HelperClass.Import();


            if (IPayBox.XML_Statistic == null)
                IPayBox.XML_Statistic = new XmlDocument();
            bool isstart = false;
            try
            {

                IPayBox.XML_Statistic.Load(IPayBox.Settings.StartupPath.TrimEnd('\\') + "\\config\\incass.xml");
                XmlElement root = (XmlElement)IPayBox.XML_Statistic.DocumentElement;

                for (int i = 0; i < root.ChildNodes.Count; i++)
                {
                    XmlElement row = (XmlElement)root.ChildNodes[i];

                    switch (row.Name)
                    {
                        case "count_bill": Incass.count = int.Parse(row.InnerText); break;
                        case "count10": Incass.CountR10 = int.Parse(row.InnerText); break;
                        case "count50": Incass.CountR50 = int.Parse(row.InnerText); break;
                        case "count100": Incass.CountR100 = int.Parse(row.InnerText); break;
                        case "count500": Incass.CountR500 = int.Parse(row.InnerText); break;
                        case "count1000": Incass.CountR1000 = int.Parse(row.InnerText); break;
                        case "count5000": Incass.CountR5000 = int.Parse(row.InnerText); break;
                        case "count_check": Incass.countchecks = int.Parse(row.InnerText); break;
                        case "bytes_read": Incass.bytesRead = int.Parse(row.InnerText); break;
                        case "bytes_send": Incass.bytesSend = int.Parse(row.InnerText); break;
                        case "amount": Incass.incass_amount = int.Parse(row.InnerText); break;
                        case "incass-start-date":
                            isstart = true;
                            if (!DateTime.TryParse(row.InnerText, out Incass.incass_date_start))
                            {
                                Incass.incass_date_start = DateTime.Now;

                            }
                            break;

                        //case "count_bill": Incass.count = int.Parse(row.InnerText); break;
                    }
                }

            }
            catch { }

            if (!isstart)
            {
                Incass.incass_date_start = DateTime.Now;
                FlushStatistic();
            }
        }
        public static void LoadTerminalData()
        {
            // Устанавливаем параметры d 0
            IPayBox.FlushToMain();

            IPayBox.Terminal.bank = "";
            IPayBox.Terminal.jur_inn = "";
            IPayBox.Terminal.jur_name = "";
            IPayBox.Terminal.jur_adress = "";
            IPayBox.Terminal.support_phone = "";
            IPayBox.Terminal.terminal_id = "";
            IPayBox.Terminal.terminal_pass = "";
            IPayBox.Terminal.terms_number = "";
            IPayBox.Terminal.trm_adress = "";
            IPayBox.Terminal.Interface = "";
            IPayBox.Terminal.FiscalMode = false;
            XmlElement root = Ipaybox.terminal_info.DocumentElement;

            for (int i = 0; i < root.ChildNodes.Count; i++)
            {
                XmlElement row = (XmlElement)root.ChildNodes[i];
                switch (row.Name)
                {
                    case "terminal_id":
                        Ipaybox.Terminal.terminal_id = row.InnerText;
                        break;
                    case "interface":
                        Ipaybox.Terminal.Interface = row.InnerText;
                        break;
                    case "password":
                        Ipaybox.Terminal.terminal_pass = row.InnerText;
                        break;
                    case "agent_jur_name":
                        Ipaybox.Terminal.jur_name = row.InnerText;
                        break;
                    case "agent_inn":
                        Ipaybox.Terminal.jur_inn = row.InnerText;
                        break;
                    case "agent_adress":
                        Ipaybox.Terminal.jur_adress = row.InnerText;
                        break;
                    case "bank":
                        Ipaybox.Terminal.bank = row.InnerText;
                        break;
                    case "terms_number":
                        Ipaybox.Terminal.terms_number = row.InnerText;
                        break;
                    case "terminal_adress":
                        Ipaybox.Terminal.trm_adress = row.InnerText;
                        break;
                    case "pin":
                        Ipaybox.Terminal.pincode = row.InnerText;
                        break;
                    case "secret_number":
                        Ipaybox.Terminal.secret_number = row.InnerText;
                        break;
                    case "agent_support_phone":
                        Ipaybox.Terminal.support_phone = row.InnerText;
                        break;
                    case "configuration-id":
                        Ipaybox.Terminal.configuration_id = row.InnerText;
                        break;
                    case "fiscal-mode":
                        if (!bool.TryParse(row.InnerText, out Ipaybox.Terminal.FiscalMode))
                            Ipaybox.Terminal.FiscalMode = false;
                        break;
                }
            }

            try
            {
                Ipaybox.FRS.RemoteFR = false;
                Ipaybox.FRS.headertext = "";
                Ipaybox.FRS.checkWidth = 38;
                Ipaybox.FRS.RemoteFiscalRegisterURL = "";
                Ipaybox.FRS.remoteFRtimeout = 10000;

                XmlNode frsroot = Ipaybox.FRSSettings.DocumentElement.ChildNodes[0];

                for (int i = 0; i < frsroot.ChildNodes.Count; i++)
                {
                    XmlElement row = (XmlElement)frsroot.ChildNodes[i];
                    switch (row.Name)
                    {
                        case "remoteFR":
                            Ipaybox.FRS.RemoteFR = Convert.ToBoolean(row.InnerText);
                            break;
                        case "checkWidth":
                            Ipaybox.FRS.checkWidth = Convert.ToInt32(row.InnerText);
                            break;
                        case "remoteFRtimeout":
                            Ipaybox.FRS.remoteFRtimeout = Convert.ToInt32(row.InnerText);
                            break;
                        case "headertext":
                            Ipaybox.FRS.headertext = row.InnerText;
                            break;
                        case "remoteFRurl":
                            Ipaybox.FRS.RemoteFiscalRegisterURL = row.InnerText;
                            break;
                    }
                }
            }
            catch
            {
                Ipaybox.AddToLog(Logs.Main, "Не удалось применить настройки фискального сервера");
            }
        }
        public static void printDoc_PrintPage(Object sender, PrintPageEventArgs e)
        {
            try
            {
                if (IPayBox.FRS.RemoteFR && !IPayBox.IncassCheck)
                    PrintString = remoteFR.RemoteFiscalRegister.tryFormFicsalCheck(
                        IPayBox.Terminal.jur_name.Trim(), 
                        IPayBox.FRS.headertext, 
                        PrintString, 
                        "Сотовая св.", 
                        IPayBox.Terminal.terminal_id.Trim(), 
                        IPayBox.Terminal.terminal_pass, 
                        IPayBox.curPay.txn_id, 
                        IPayBox.curPay.from_amount.ToString(), 
                        "1", 
                        IPayBox.FRS.RemoteFiscalRegisterURL, 
                        IPayBox.FRS.checkWidth, 
                        IPayBox.FRS.remoteFRtimeout);
            }
            catch (Exception ex)
            {
                IPayBox.AddToLog(Logs.Main, ex.Message);
            }
        }
    }

    #region _____SOUND
    public static class Sound
    {
        private static SoundPlayer player;
        public static void Initialize()
        {
            player = new SoundPlayer();
        }

        public static void Play(string location)
        {
            try
            {
                player.Stop();
            }
            catch
            { }

            try
            {
                player.SoundLocation = location;
                player.Play();
            }
            catch
            { }
        }
    }
    #endregion
}