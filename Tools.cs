using Microsoft.WindowsAPICodePack.Dialogs;
using System;

namespace GaitAndBalanceApp
{
    static class Tools
    {
        static char[] underScore = new char[] { '_' };
        readonly public static string dateFormat = "yyyyMMddHHmmss";


        public static bool parseFileName(string filename, out string identifier, out string exercise, out DateTime date)
        {
            identifier = null; exercise = null; date = DateTime.Now;
            var f = filename.Split(underScore);
            if (f.Length < 4) return false;
            date = DateTime.ParseExact(f[f.Length - 3], dateFormat, System.Globalization.CultureInfo.InvariantCulture);
            exercise = f[f.Length - 2];
            identifier = f[0];
            for (int i = 1; i < f.Length - 3; i++)
                identifier += "_" + f[i];
            return true;

        }

        public static bool parseSampleFileName(string filename, out string identifier, out string exercise, out DateTime date)
        {
            identifier = null; exercise = null; date = DateTime.Now;
            var f = filename.Split(underScore);
            if (f.Length < 4) return false;
            date = DateTime.ParseExact(f[f.Length - 3], dateFormat, System.Globalization.CultureInfo.InvariantCulture);
            exercise = f[f.Length - 2];
            identifier = f[0];
            for (int i = 1; i < f.Length - 3; i++)
                identifier += "_" + f[i];
            return true;

        }


        public static string getPath(string path)
        {
            var dlg = new CommonOpenFileDialog();
            dlg.Title = "Select data folder";
            dlg.IsFolderPicker = true;
            dlg.InitialDirectory = path;

            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.DefaultDirectory = path;
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var folder = dlg.FileName;
                return folder;
            }
            return null;

        }

    }
}
