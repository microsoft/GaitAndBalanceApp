using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GaitAndBalanceApp.UIComponents
{
    /// <summary>
    /// Interaction logic for CameraView.xaml
    /// </summary>
    public partial class CameraView : UserControl
    {
        Frame _frame;
        public Frame frame
        {
            get { return _frame; }
            set
            {
                if (_frame != value)
                {
                    _frame = value;
                    try { drawView(); }
                    catch (TaskCanceledException) { }
                }
            }
        }

        byte[] bgColor, groundColor, bodyColor, otherColor;

        public CameraView()
        {
            bgColor = new byte[] { 255, 255, 255, 255 };
            groundColor = new byte[] { 0, 255, 255, 255 };
            bodyColor = new byte[] { 255, 0, 0, 255 };
            otherColor = new byte[] { 50, 100, 100, 255 };
            InitializeComponent();
        }
        private void drawView()
        {
            if (frame == null || frame.blockTypes == null) return;
            byte[] viewPixels = new byte[4 * frame.blockTypes.Length];
            int width = frame.blockTypes.GetLength(0);
            int height = frame.blockTypes.GetLength(1);
            for (int i = 0, p = 0; i < frame.blockTypes.Length; i++, p+=4)
                switch (frame.blockTypes[i % width, i /width])
                {
                    case Frame.EBlockType.background:
                        for (int j = 0; j < 4; j++) viewPixels[p + j] = bgColor[j];
                        break;
                    case Frame.EBlockType.body:
                        for (int j = 0; j < 4; j++) viewPixels[p + j] = bodyColor[j];
                        break;
                    case Frame.EBlockType.ground:
                        for (int j = 0; j < 4; j++) viewPixels[p + j] = groundColor[j];
                        break;
                    default:
                        for (int j = 0; j < 4; j++) viewPixels[p + j] = otherColor[j];
                        break;

                }

            viewCanvas.Dispatcher.Invoke(new Action(delegate ()
            {
                BitmapSource viewSource = BitmapSource.Create(width, height,100, 100, PixelFormats.Bgra32, null, viewPixels, width * 4);
                viewCanvas.Source = viewSource;
            }));
        }

    }
}
