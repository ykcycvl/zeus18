using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Security;

namespace zeus
{
    public partial class main : Form
    {
        flushform flush = new flushform();

        public main()
        {
            InitializeComponent();
        }

        public static void Remove(string file1, string file2)
        {
            bool removed = false;
            byte trycount = 0;

            if (File.Exists(file1))
            {
                while (!removed && trycount < 3)
                {
                    trycount++;

                    try
                    {
                        //Установка аттрибутов файла если файл существует
                        if (File.Exists(file1))
                        {
                            try
                            {
                                File.SetAttributes(file1, FileAttributes.Normal);
                            }
                            catch
                            {
                                Console.WriteLine("Cannot set file attributes for source file!");
                            }
                        }

                        if (File.Exists(file2))
                        {
                            try
                            {
                                File.SetAttributes(file2, FileAttributes.Normal);
                            }
                            catch
                            {
                                Console.WriteLine("Cannot set file attributes for destination file!");
                            }
                        }

                        Console.Write("Copying " + file1 + " with " + file2 + " ...");
                        File.Copy(file1, file2, true);
                        System.Threading.Thread.Sleep(3000);
                        Console.WriteLine("OK");
                        Console.Write("Deleting " + file1 + " ...");
                        File.Delete(file1);
                        System.Threading.Thread.Sleep(3000);
                        Console.WriteLine("OK");
                        removed = true;
                    }
                    catch (Exception ex) { Console.WriteLine("Error! " + ex.Message); removed = false; System.Threading.Thread.Sleep(3000); }
                }
            }
        }
        public static void InitiateSSLTrust()
        {
            try
            {
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });
            }
            catch (Exception ex)
            {
                IPayBox.AddToLog(IPayBox.Logs.Main, "Не удалось инциализировать SSL: " + ex.Message);
            }
        }
        private void LoadUrl()
        {
            IPayBox.Settings.UpdateUrl = new string[]
            {       
                "http://update1.ipaybox.ru/",
                "http://update2.ipaybox.ru/"
            };

            IPayBox.Settings.ServiceUrl = new string[]
            {
                "https://xml2.ipaybox.ru/xml/",
                "https://xml1.ipaybox.ru/xml/",
                "http://xml2.ipaybox.ru/xml/",
                "http://xml1.ipaybox.ru/xml/"
            };
        }
        private void CheckExistProcess()
        {
            try
            {
                System.Diagnostics.Process[] n = System.Diagnostics.Process.GetProcessesByName("zeus");

                for (int i = 0; i < n.Length - 1; i++)
                {
                    n[i].Kill();
                    n[i].WaitForExit();
                }
            }
            catch (Exception ex)
            {
                IPayBox.AddToLog(IPayBox.Logs.Main, "Не удалось завершить процесс zeus: " + ex.Message);
            }
        }
        private void InitializeIpaybox()
        {
            ((Label)flush.Controls["label1"]).Text = "Инициализация оборудования...";
            Application.DoEvents();
            Sound.Initialize();
            IPayBox.State.Working = true;
            IPayBox.Forms.Count = 0;
            IPayBox.Forms.Index = 0;
            IPayBox.StartForm = this;

            #region Printer
            string[] ComPorts = System.IO.Ports.SerialPort.GetPortNames();
            string descr = "";

            foreach (string s in ComPorts)
                descr += s + " ";

            IPayBox.AddToLog(IPayBox.Logs.Main, "Доступные COM порты: " + descr);

            try
            {
                ((Label)flush.Controls["PrinterInfoLabel"]).Text = "Поиск принтера...";
                Application.DoEvents();

                if (!IPayBox.Settings.AtolDriver)
                    if (!IPayBox.Settings.WindowsPrinter)
                    {
                        if (IPayBox.Devices.Printer == null)
                        {
                            IPayBox.Devices.Printer = new Printers.Printer(IPayBox.Settings.Args);
                            IPayBox.AddToLog(IPayBox.Logs.Main, "Принтер " + IPayBox.Devices.Printer.PrnModel + ", подключен к порту " + IPayBox.Devices.Printer.port);

                            if (IPayBox.Devices.Printer.PrnModel == Printers.Model.NULL)
                            {
                                ((Label)flush.Controls["PrinterInfoLabel"]).Text = "Принтер не найден";
                                ((Label)flush.Controls["PrinterInfoLabel"]).ForeColor = System.Drawing.Color.Red;
                            }
                            else
                                ((Label)flush.Controls["PrinterInfoLabel"]).Text = "Найден принтер " + IPayBox.Devices.Printer.PrnModel + ". Порт " + IPayBox.Devices.Printer.port;

                            Application.DoEvents();
                        }
                    }
                    else
                    {
                        IPayBox.doc = new System.Drawing.Printing.PrintDocument();
                        IPayBox.doc.PrintController = new System.Drawing.Printing.StandardPrintController();
                        IPayBox.doc.PrintPage += new System.Drawing.Printing.PrintPageEventHandler(IPayBox.printDoc_PrintPage);
                        IPayBox.AddToLog(IPayBox.Logs.Main, "Принтер WINDOWS");
                        ((Label)flush.Controls["PrinterInfoLabel"]).Text = "Принтер WINDOWS";
                        Application.DoEvents();
                    }
                else
                {
                    try
                    {
                        IPayBox.Settings.FiscalRegister = true;
                        IPayBox.Devices.FRegister = new FR.Atol();

                        IPayBox.Devices.FRegister.FiscalMode = IPayBox.Settings.FiscalMode;

                        IPayBox.AddToLog(IPayBox.Logs.Main, "ККМ " + IPayBox.Devices.FRegister.Model + " найдена на порту " + IPayBox.Devices.FRegister.ComPort);

                        ((Label)flush.Controls["PrinterInfoLabel"]).Text = "Найден ККМ " + IPayBox.Devices.FRegister.Model + ". Порт " + IPayBox.Devices.FRegister.ComPort;
                        Application.DoEvents();
                    }
                    catch (Exception ex)
                    {
                        IPayBox.AddToLog(IPayBox.Logs.Main, "Невозможно инициализировать драйвер АТОЛ.\r\n" + ex.Message);
                        ((Label)flush.Controls["PrinterInfoLabel"]).Text = "Невозможно инициализировать драйвер АТОЛ";
                        ((Label)flush.Controls["PrinterInfoLabel"]).ForeColor = System.Drawing.Color.Red;
                        Application.DoEvents();

                        if (!IPayBox.Debug)
                            IPayBox.State.Working = false;
                    }
                }
            }
            catch (Exception ex)
            {
                IPayBox.AddToLog(IPayBox.Logs.Main, ex.Message);
            }
            #endregion
            #region Acceptor
            try
            {
                ((Label)flush.Controls["AcceptorInfoLabel"]).Text = "Поиск купюроприемника...";
                Application.DoEvents();

                if (IPayBox.Devices.Acceptor == null)
                {
                    IPayBox.Devices.Acceptor = new Acceptors.Acceptor();

                    if (IPayBox.Devices.Acceptor.model == Acceptors.Model.NULL && !IPayBox.Debug)
                    {
                        IPayBox.AddToLog(IPayBox.Logs.Main, "\tНе найден купюрник. Не работаем.");
                        IPayBox.State.Working = false;
                        ((Label)flush.Controls["AcceptorInfoLabel"]).Text = "Не найден купюроприемник";
                        ((Label)flush.Controls["AcceptorInfoLabel"]).ForeColor = System.Drawing.Color.Red;
                        Application.DoEvents();
                    }
                    else
                    {
                        IPayBox.AddToLog(IPayBox.Logs.Main, "Купюроприемник " + IPayBox.Devices.Acceptor.model + ", подключен к порту ");
                        ((Label)flush.Controls["AcceptorInfoLabel"]).Text = "Купюроприемник " + IPayBox.Devices.Acceptor.model + ", подключен к порту";
                        Application.DoEvents();
                    }
                }
            }
            catch(Exception ex)
            {
                IPayBox.AddToLog(IPayBox.Logs.Main, ex.Message);
            }
            #endregion
            #region WatchDog
            try
            {
                if (IPayBox.Devices.Watchdog == null)
                {
                    IPayBox.Devices.Watchdog = new WD.Watchdog(ComPorts);

                    if (IPayBox.Devices.Watchdog.watch != WD.Watchdog.WD.NULL)
                    {
                        IPayBox.AddToLog(IPayBox.Logs.Main, "Найден WatchDog " + IPayBox.Devices.Watchdog.watch.ToString() + " " + IPayBox.Devices.Watchdog.Version + ", подключен к порту ");
                        ((Label)flush.Controls["WDInfoLabel"]).Text = "Найден WatchDog " + IPayBox.Devices.Watchdog.watch.ToString() + " " + IPayBox.Devices.Watchdog.Version;
                        Application.DoEvents();
                    }
                    else
                    {
                        ((Label)flush.Controls["WDInfoLabel"]).Text = "WatchDog не найден";
                        ((Label)flush.Controls["WDInfoLabel"]).ForeColor = System.Drawing.Color.Red;
                        Application.DoEvents();
                    }
                }
            }
            catch (Exception ex)
            {
                IPayBox.AddToLog(IPayBox.Logs.Main, ex.Message);
            }
            #endregion

            IPayBox.LoadIncass();

            #region ConfigFiles
            ((Label)flush.Controls["label1"]).Text = "Загрузка конфигурационных файлов...";
            Application.DoEvents();
            IPayBox.AddToLog(IPayBox.Logs.Main, "Загрузка информации о терминале.");
            IPayBox.TerminalInfo = new zeus.HelperClass.TerminalInfo().Load();
            IPayBox.RFRSettings = new zeus.HelperClass.RFRSettings().Load();
            IPayBox.TPIN = new zeus.HelperClass.TPIN().Load();

            if (IPayBox.TPIN.Persons == null || IPayBox.TPIN.Persons.Count == 0)
                IPayBox.Settings.MasterPinActive = true;
            else
                IPayBox.Settings.MasterPinActive = false;



            #endregion
        }

        private void main_Load(object sender, EventArgs e)
        {
            flush.Show();
            Application.DoEvents();

            FileInfo updaterFI = new FileInfo(IPayBox.Settings.StartupPath + "\\updater.exe.update");

            if (updaterFI.Exists)
                try
                {
                    Remove(IPayBox.Settings.StartupPath + "\\updater.exe.update", IPayBox.Settings.StartupPath + "\\updater.exe");
                }
                catch (Exception ex)
                {
                    IPayBox.AddToLog(IPayBox.Logs.Main, "Не удалось переименовать файл updater.exe.update: " + ex.Message);
                }

            try
            {
                FileInfo fi = new FileInfo(IPayBox.Settings.StartupPath + @"\debug");
                IPayBox.Debug = fi.Exists;
                InitiateSSLTrust();
                LoadUrl();
                _cursor.Hide();

                if (!IPayBox.Debug)
                    IPayBox.HideExplorer();
            }
            catch (Exception ex)
            {
                IPayBox.AddToLog(IPayBox.Logs.Main, ex.Message);
            }

            string[] args = Environment.CommandLine.Split(' ');

            if (args.Length > 0)
            {
                foreach (string row in args)
                {
                    if (row.ToLower() == "prtwin" || row.ToLower() == "prnwin")
                    {
                        IPayBox.Settings.Args = "prtwin";
                    }
                    if (row.ToLower() == "atol" || row.ToLower() == "atol")
                    {
                        IPayBox.Settings.AtolDriver = true;
                        IPayBox.Settings.Args = "atol";
                    }
                    if (row.ToLower() == "prim21k")
                    {
                        IPayBox.Settings.Args = "prim21k";
                    }
                }
            }

            IPayBox.Settings.UpdateUrlIndex = 0;
            IPayBox.Settings.ServiceUrlIndex = 0;

            IPayBox.Settings.Resolution = new Size(1280, 1024);
            IPayBox.Settings.Inches = 17;

            /*
            if (Screen.PrimaryScreen.Bounds.Width == 1280 && Screen.PrimaryScreen.Bounds.Height == 1024)
            {
                Ipaybox.Resolution = new Size(1280, 1024);
                Ipaybox.Inches = 17;
            }
            else
            {
                //if (Screen.PrimaryScreen.Bounds.Width == 1024 && Screen.PrimaryScreen.Bounds.Height == 768)
                //{
                    Ipaybox.Resolution = new Size(1024, 768);
                    Ipaybox.Inches = 15;
               // }
            }*/


            try
            {
                IPayBox.Settings.RequestTimeout = float.Parse("0,5");
                System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.RealTime;
            }
            catch (Exception ex)
            {
                IPayBox.AddToLog(IPayBox.Logs.Main, ex.Message);
            }

            CheckExistProcess();

            IPayBox.AddToLog(IPayBox.Logs.Main, "Старт приложения. Инициализация.");
            IPayBox.Info.CoreVersion = Application.ProductVersion;
            IPayBox.AddToLog(IPayBox.Logs.Main, "Версия ПО:" + IPayBox.Info.CoreVersion);

            pays_send_timer.Enabled = false;
            conf_update.Enabled = false;
            link_timer.Enabled = false;
            print_timer.Enabled = false;

            // Инициализируем приложение
            try
            {
                InitializeIpaybox();
            }
            catch
            {
                IPayBox.State.Working = false;
                IPayBox.State.NeedToUpdateConfiguration = true;
                IPayBox.State.NeedUpdateCore = true;
            }

            pays_send_timer.Enabled = true;
            conf_update.Enabled = true;
            link_timer.Enabled = true;
            print_timer.Enabled = true;

            this.BackColor = Color.FromArgb(230, 230, 230);

            ((Label)flush.Controls["label1"]).Text = "Проверка модема...";
            Application.DoEvents();

            if (IPayBox.Settings.NetOption == 1)
            {
                try
                {
                    _Modem.Init();
                }
                catch (Exception ex)
                {
                    IPayBox.AddToLog(IPayBox.Logs.Main, ex.Message);
                }
            }

            if (IPayBox.State.IsBlocked)
                IPayBox.State.Working = false;

            try
            {
                ((Label)flush.Controls["label1"]).Text = "Старт приложения...";
                Application.DoEvents();
                flush.Dispose();
                flush = null;
            }
            catch (Exception ex)
            {
                IPayBox.AddToLog(IPayBox.Logs.Main, ex.Message);
            }

            // Запускаем процесс
            Main_Process();
        }

        public void Main_Process()
        { 
        }
    }
}