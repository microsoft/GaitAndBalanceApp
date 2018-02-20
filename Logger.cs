using System.IO;
using System.Configuration;
using System;

namespace GaitAndBalanceApp
{
    static public class Logger
    {
        static string logFileName = ConfigurationManager.AppSettings["logFileName"];

        public static void log(string message)
        {
            try
            {
                File.AppendAllText(logFileName, string.Format("{0}: {1}\n", DateTime.Now, message));
            }
            catch { };
        }

        public static void log(string format, params object[] rest)
        {
            log(string.Format(format, rest));
        }
    }
}
