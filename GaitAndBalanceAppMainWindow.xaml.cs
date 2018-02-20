using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;


namespace GaitAndBalanceApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class GaitAndBalanceAppMainWindow : Window
    {

        public class BuildDateTime
        {
            #region Gets the build date and time (by reading the COFF header)

            // http://msdn.microsoft.com/en-us/library/ms680313

            public struct _IMAGE_FILE_HEADER
            {
                public ushort Machine;
                public ushort NumberOfSections;
                public uint TimeDateStamp;
                public uint PointerToSymbolTable;
                public uint NumberOfSymbols;
                public ushort SizeOfOptionalHeader;
                public ushort Characteristics;

            };

            static public DateTime GetBuildDateTime(Assembly assembly)
            {
                if (File.Exists(assembly.Location))
                {
                    var buffer = new byte[Math.Max(Marshal.SizeOf(typeof(_IMAGE_FILE_HEADER)), 4)];
                    using (var fileStream = new FileStream(assembly.Location, FileMode.Open, FileAccess.Read))
                    {
                        fileStream.Position = 0x3C;
                        fileStream.Read(buffer, 0, 4);
                        fileStream.Position = BitConverter.ToUInt32(buffer, 0); // COFF header offset
                        fileStream.Read(buffer, 0, 4); // "PE\0\0"
                        fileStream.Read(buffer, 0, buffer.Length);
                    }
                    var pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    try
                    {
                        var coffHeader = (_IMAGE_FILE_HEADER)Marshal.PtrToStructure(pinnedBuffer.AddrOfPinnedObject(), typeof(_IMAGE_FILE_HEADER));

                        return TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1) + new TimeSpan(coffHeader.TimeDateStamp * TimeSpan.TicksPerSecond));
                    }
                    finally
                    {
                        pinnedBuffer.Free();
                    }
                }
                return new DateTime();
            }

            #endregion
        }

        public GaitAndBalanceAppMainWindow()
        {
            InitializeComponent();
            footer.Content = String.Format("Version of : {0}. Microsoft Research.", BuildDateTime.GetBuildDateTime(Assembly.GetEntryAssembly()));
        }

        private void CameraViewTab_Selected(object sender, RoutedEventArgs e)
        {
            cameraViewTab.IsCurrentTab = true;
        }

        private void CameraViewTab_Unselected(object sender, RoutedEventArgs e)
        {
            cameraViewTab.IsCurrentTab = false;
        }
    }
}
