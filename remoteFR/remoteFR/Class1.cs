using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using Microsoft.Win32;
using System.Reflection;
using System.Web;
using System.Net;
using System.Drawing;
using System.Drawing.Text;
using System.Drawing.Drawing2D;

namespace remoteFR
{
    public static class RemoteFiscalRegister
    {
        private static void AddToLog(string message)
        {
            string dir = "logs\\remoteFR";
            FileInfo fi = null;
            DirectoryInfo di = new DirectoryInfo(dir);

            if (!di.Exists)
                di.Create();

            fi = new FileInfo(dir + "\\" + DateTime.Now.ToString("yyyy-MM-dd") + ".log");

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

        public static bool OK = false;

        public static Stream getFRData(string btype, string terminal_id, string password, string txn_id, string sum, string amount, string url, int Timeout)
        {
            try
            {
                AddToLog("Соединение с ФС. Таймаут: " + Timeout.ToString() + " мс.");   
                // Url запрашиваемого методом POST скрипта
                System.Net.WebRequest req = System.Net.WebRequest.Create(url + "" + "?trm_id=" + terminal_id +
                    "&txn_id=" + txn_id + "&sum=" + sum + "&btype=" + btype + "&amount=" + amount);
                req.Timeout = Timeout;
                System.Net.WebResponse resp = req.GetResponse();
                System.IO.Stream stream = resp.GetResponseStream();
                AddToLog("Данные получены");
                return stream;
            }
            catch (Exception ex)
            {
                AddToLog("Ошибка получения данных с ФС: " + ex.Message);
                return null;
            }
        }
        public static Stream testRemoteFR(string terminal_id, string url)
        {
            try
            {
                AddToLog("Соединение с ФС");
                System.Net.WebRequest req = System.Net.WebRequest.Create(url + "" + "?trm_id=" + terminal_id +
                       "&txn_id=0&sum=0&btype=Тест&amount=1");
                req.Timeout = 5000;
                System.Net.WebResponse resp = req.GetResponse();
                System.IO.Stream stream = resp.GetResponseStream();
                AddToLog("Получены данные c ФС");
                return stream;
            }
            catch (Exception ex)
            {
                AddToLog("Ошибка получения данных с ФС: " + ex.Message);
                return null;
            }
        }
        public static string tryFormFicsalCheck(string header, string headerRekv, string checkText, string btype, string terminal_id, string password, string txn_id, string sum, string amount, string url, int checkWidth, int Timeout)
        {
            OK = false;
            string formattedSum = "";

            string s = "";
            string ack = ack = "~";
            string splitter = "";

            while (splitter.Length < checkWidth - 1)
            {
                splitter += "*";
            }

            while (header.Length < checkWidth - 1)
            {
                header = " " + header + " ";
            }

            try
            {
                AddToLog("Попытка формирования фискального чека");   
                string[] ds = sum.Split(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0]);

                if (ds.Length == 1)
                    formattedSum = sum + System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + "00";
                else
                    if (ds[1].Length == 1)
                        formattedSum = formattedSum + "0";

                formattedSum = "=" + formattedSum;
                formattedSum = formattedSum.Replace(",", ".");

                string tmp = formattedSum;

                while (tmp.Length < checkWidth - 1)
                    tmp = " " + tmp;

                string cc = "КАССОВЫЙ ЧЕК";

                while (cc.Length < checkWidth - 1)
                {
                    cc = " " + cc + " ";
                }

                s = header + "\r\n" + headerRekv + "\r\n" + splitter + "\r\n" + cc + "\r\n" + btype + "\r\n" + tmp + "\r\n" + checkText + "\r\n";

                int n = 0;
                string res = "";

                for (int i = 0; i < s.Length; i++)
                {
                    res += s[i];

                    if (n == checkWidth && s[i] != '\r')
                    {
                        res += "\r\n";
                        n = 0;
                    }
                    if (s[i] == '\n')
                    {
                        n = 0;
                    }

                    n++;
                }

                s = res;
            }
            catch (Exception ex)
            {
                OK = false;
                AddToLog("Ошибка формирования фискального чека: " + ex.Message);
                return s;
            }

            try
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(getFRData(btype, terminal_id, password, txn_id, sum, amount, url, Timeout));
                string checkNumber = xml.GetElementsByTagName("checkNumber")[0].InnerXml;
                string docNumber = xml.GetElementsByTagName("docNumber")[0].InnerXml;
                string checkDateTime = xml.GetElementsByTagName("checkDateTime")[0].InnerXml;
                string INN = xml.GetElementsByTagName("INN")[0].InnerXml + ack;
                string kkmNuber = ack + xml.GetElementsByTagName("kkmNuber")[0].InnerXml;
                string kpk = "  " + xml.GetElementsByTagName("kpkNumber")[0].InnerXml;
                string eklzNumber = xml.GetElementsByTagName("eklzNumber")[0].InnerXml + " ";

                string s1 = "ИТОГ";
                string s2 = "";
                string s3 = "";
                string s4 = "";

                while (s1.Length + formattedSum.Length < checkWidth)
                {
                    s1 += " ";
                }

                s1 += formattedSum;

                s2 += checkNumber;
                s2 += " " + docNumber;

                while (s2.Length + checkDateTime.Length < checkWidth)
                {
                    s2 += " ";
                }

                s2 += checkDateTime;
                s3 += kkmNuber;

                while (s3.Length + INN.Length < checkWidth)
                {
                    s3 += " ";
                }

                s3 += INN;
                s4 += kpk;

                while (s4.Length + eklzNumber.Length < checkWidth)
                {
                    s4 += " ";
                }

                s4 += eklzNumber;
                s += "\r\n" + s1 + "\r\n" + s2 + "\r\n" + s3 + "\r\n" + s4 + "\r\n" + splitter;
                OK = true;
                AddToLog("Фискальный чек успешно сформирован");
                return s;
            }
            catch (Exception ex)
            {
                OK = false;
                AddToLog("Ошибка формирования фискального чека: " + ex.Message);
                return s;
            }
        }
        public static byte[] convertToByteArray(string header, string headerRekv, string checkText, string btype, string terminal_id, string password, string txn_id, string sum, string amount, string url, int checkWidth, int Timeout)
        {
            OK = false;
            List<byte> result = new List<byte>();
            List<byte> ans = new List<byte>();
            string formattedSum = "";

            string s = "";
            string ack = ack = "~";
            string splitter = "";

            while (splitter.Length < checkWidth - 1)
            {
                splitter += "*";
            }

            while (header.Length < checkWidth - 1)
            {
                header = " " + header + " ";
            }

            try
            {
                string[] ds = sum.Split(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0]);

                if (ds.Length == 1)
                    formattedSum = sum + System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + "00";
                else
                    if (ds[1].Length == 1)
                        formattedSum = formattedSum + "0";

                formattedSum = "=" + formattedSum;
                formattedSum = formattedSum.Replace(",", ".");

                string tmp = formattedSum;

                while (tmp.Length < checkWidth - 1)
                    tmp = " " + tmp;

                string cc = "КАССОВЫЙ ЧЕК";

                while (cc.Length < checkWidth - 1)
                {
                    cc = " " + cc + " ";
                }

                s = header + "\r\n" + headerRekv + "\r\n" + splitter + "\r\n" + cc + "\r\n" + btype + "\r\n" + tmp + "\r\n" + checkText + "\r\n" + splitter + "\r\n";

                for (int i = 0; i < s.Length; i++)
                {
                    int index = Array.IndexOf(MyCodeTable.chars, s[i]);

                    if (index > -1)
                    {
                        result.Add((byte)index);
                    }
                }

                s = "";

                int n = 0;

                List<byte> bytes = new List<byte>();

                for (int i = 0; i < result.Count; i++)
                {
                    bytes.Add(result[i]);

                    if (n == checkWidth && result[i] != 13)
                    {
                        bytes.Add(13);
                        bytes.Add(10);
                        n = 0;
                    }
                    if (result[i] == 10)
                    {
                        n = 0;
                    }

                    n++;
                }

                ans = bytes;
            }
            catch
            {
                OK = false;
                return ans.ToArray();
            }

            try
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(getFRData(btype, terminal_id, password, txn_id, sum, amount, url, Timeout));
                string checkNumber = xml.GetElementsByTagName("checkNumber")[0].InnerXml;
                string docNumber = xml.GetElementsByTagName("docNumber")[0].InnerXml;
                string checkDateTime = xml.GetElementsByTagName("checkDateTime")[0].InnerXml;
                string INN = xml.GetElementsByTagName("INN")[0].InnerXml + ack;
                string kkmNuber = ack + xml.GetElementsByTagName("kkmNuber")[0].InnerXml;
                string kpk = "  " + xml.GetElementsByTagName("kpkNumber")[0].InnerXml;
                string eklzNumber = xml.GetElementsByTagName("eklzNumber")[0].InnerXml + " ";

                string s1 = "ИТОГ";
                string s2 = "";
                string s3 = "";
                string s4 = "";

                while (s1.Length + formattedSum.Length < checkWidth - 1)
                    s1 += " ";

                s1 += formattedSum;

                s2 += checkNumber;
                s2 += " " + docNumber;

                while (s2.Length + checkDateTime.Length < checkWidth - 1)
                    s2 += " ";

                s2 += checkDateTime;
                s3 += kkmNuber;

                while (s3.Length + INN.Length < checkWidth - 1)
                    s3 += " ";

                s3 += INN;
                s4 += kpk;

                while (s4.Length + eklzNumber.Length < checkWidth - 1)
                    s4 += " ";

                s4 += eklzNumber;
                s = "\r\n" + s1 + "\r\n" + s2 + "\r\n" + s3 + "\r\n" + s4 + "\r\n";

                for (int i = 0; i < s.Length; i++)
                {
                    int index = Array.IndexOf(MyCodeTable.chars, s[i]);

                    if (index > -1)
                    {
                        result.Add((byte)index);
                    }
                }

                int n = 0;

                List<byte> bytes = new List<byte>();

                for (int i = 0; i < result.Count; i++)
                {
                    bytes.Add(result[i]);

                    if (n == checkWidth && result[i] != 13)
                    {
                        bytes.Add(13);
                        bytes.Add(10);
                        n = 0;
                    }
                    if (result[i] == 10)
                    {
                        n = 0;
                    }

                    n++;
                }

                OK = true;
                return bytes.ToArray();
            }
            catch
            {
                OK = false;
                return ans.ToArray();
            }
        }
        public static string tryTest(string header, string checkText, string btype, string terminal_id, string txn_id, string sum, string amount, string url, int checkWidth)
        {
            try
            {
                string ack = ack = "~";

                string splitter = "";

                while (splitter.Length < checkWidth)
                {
                    splitter += "*";
                }

                while (header.Length < checkWidth)
                {
                    header = " " + header + " ";
                }

                header += "\r\n";

                XmlDocument xml = new XmlDocument();
                //xml.Load(getFRData(btype, terminal_id, txn_id, sum, amount, url));
                string checkNumber = "#12354";
                string docNumber = "23242342";
                string checkDateTime = "15-08-13 13:00";
                string INN = "123456789101" + ack;
                string kkmNuber = ack + "ККМ С ФП 12345678";
                string kpk = "  " + "123456";
                string eklzNumber = "1234567810" + " ";

                string[] ds = sum.Split(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0]);

                if (ds.Length == 1)
                {
                    sum = sum + System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + "00";
                }
                else
                {
                    if (ds[1].Length == 1)
                    {
                        sum = sum + "0";
                    }
                }

                string a = "=";
                byte[] b = new byte[] { 0xF6 };
                byte[] copy = new byte[b.Length];
                for (int i = 0; i < b.Length; copy[i] = b[b.Length - ++i]) ;
                a = Encoding.GetEncoding(866).GetString(copy, 0, copy.Length);

                sum = "=" + sum;

                string tmp = sum;

                while (tmp.Length < checkWidth)
                {
                    tmp = " " + tmp;
                }

                string s = header + "\r\n" + splitter + "\r\n" + btype + "\r\n" + tmp + "\r\n" + checkText;

                string s1 = "ИТОГ";
                string s2 = "";
                string s3 = "";
                string s4 = "";

                while (s1.Length + sum.Length < checkWidth)
                {
                    s1 += " ";
                }

                s1 += sum;

                s2 += checkNumber;
                s2 += " " + docNumber;

                while (s2.Length + checkDateTime.Length < checkWidth)
                {
                    s2 += " ";
                }

                s2 += checkDateTime;
                s3 += kkmNuber;

                while (s3.Length + INN.Length < checkWidth)
                {
                    s3 += " ";
                }

                s3 += INN;
                s4 += kpk;

                while (s4.Length + eklzNumber.Length < checkWidth)
                {
                    s4 += " ";
                }

                s4 += eklzNumber;
                s += "\r\n" + s1 + "\r\n" + s2 + "\r\n" + s3 + "\r\n" + s4 + "\r\n" + splitter + "\r\n";
                return s;
            }
            catch
            {
                return checkText;
            }
        }
    }

    public static class MyCodeTable
    {
        public static char[] chars = new char[]
        {
            '\0',
            '☺',
            '☻',
            '♥',
            '♦',
            '♣',
            '♠',
            '•',
            '◘',
            '○',
            '\n',
            '♂',
            '♀',
            '\r',
            '♫',
            '☼',
            '►',
            '◄',
            '↕',
            '‼',
            '¶',
            '§',
            '▬',
            '↨',
            '↑',
            '↓',
            '→',
            '←',
            '∟',
            '↔',
            '▲',
            '▼',
            ' ',
            '!',
            '"',
            '#',
            '$',
            '%',
            '&',
            '\'',
            '(',
            ')',
            '*',
            '+',
            ',',
            '-',
            '.',
            '/',
            '0',
            '1',
            '2',
            '3',
            '4',
            '5',
            '6',
            '7',
            '8',
            '9',
            ':',
            ';',
            '<',
            '=',
            '>',
            '?',
            '@',
            'A',
            'B',
            'C',
            'D',
            'E',
            'F',
            'G',
            'H',
            'I',
            'J',
            'K',
            'L',
            'M',
            'N',
            'O',
            'P',
            'Q',
            'R',
            'S',
            'T',
            'U',
            'V',
            'W',
            'X',
            'Y',
            'Z',
            '[',
            '\\',
            ']',
            '^',
            '_',
            '`',
            'a',
            'b',
            'c',
            'd',
            'e',
            'f',
            'g',
            'h',
            'i',
            'j',
            'k',
            'l',
            'm',
            'n',
            'o',
            'p',
            'q',
            'r',
            's',
            't',
            'u',
            'v',
            'w',
            'x',
            'y',
            'z',
            '{',
            '|',
            '}',
            '~',
            '⌂',
            'А',
            'Б',
            'В',
            'Г',
            'Д',
            'Е',
            'Ж',
            'З',
            'И',
            'Й',
            'К',
            'Л',
            'М',
            'Н',
            'О',
            'П',
            'Р',
            'С',
            'Т',
            'У',
            'Ф',
            'Х',
            'Ц',
            'Ч',
            'Ш',
            'Щ',
            'Ъ',
            'Ы',
            'Ь',
            'Э',
            'Ю',
            'Я',
            'а',
            'б',
            'в',
            'г',
            'д',
            'е',
            'ж',
            'з',
            'и',
            'й',
            'к',
            'л',
            'м',
            'н',
            'о',
            'п',
            '░',
            '▒',
            '▓',
            '│',
            '┤',
            '╡',
            '╢',
            '╖',
            '╕',
            '╣',
            '║',
            '╗',
            '╝',
            '╜',
            '╛',
            '┐',
            '└',
            '┴',
            '┬',
            '├',
            '─',
            '┼',
            '╞',
            '╟',
            '╚',
            '╔',
            '╩',
            '╦',
            '╠',
            '═',
            '╬',
            '╧',
            '╨',
            '╤',
            '╥',
            '╙',
            '╘',
            '╒',
            '╓',
            '╫',
            '╪',
            '┘',
            '┌',
            '█',
            '▄',
            '▌',
            '▐',
            '▀',
            'р',
            'с',
            'т',
            'у',
            'ф',
            'х',
            'ц',
            'ч',
            'ш',
            'щ',
            'ъ',
            'ы',
            'ь',
            'э',
            'ю',
            'я',
            '≡',
            '±',
            '>',
            '<',
            'Ф',
            'П',
            '÷',
            '≈',
            '°',
            '·',
            '·',
            '√',
            '№',
            '¤',
            '■'
        };
    }
}
