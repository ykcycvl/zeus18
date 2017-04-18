﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Printers
{
    public static class log
    {
        private static string logfolder = @"logs\printer_logs\";
        private static string FileName = "PrinterLog_" + DateTime.Today.ToShortDateString() + ".log";

        public static void AddToLog(string message)
        {
            string FullPath = logfolder + FileName;

            DirectoryInfo dt = new DirectoryInfo(@"logs\acceptor_logs");

            if (!dt.Exists)
                dt.Create();

            File.AppendAllText(FullPath, DateTime.Now.ToLongTimeString() + " " + message + "\r\n");
        }
    }
}
