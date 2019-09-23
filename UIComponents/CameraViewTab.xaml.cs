using System;
using System.Windows.Controls;
using Timer = System.Timers.Timer;


namespace GaitAndBalanceApp.UIComponents
{
    /// <summary>
    /// Interaction logic for CameraViewTab.xaml
    /// </summary>
    public partial class CameraViewTab : UserControl
    {
        Kinect kinect = KinectFactory.Instance;
        Timer subjectDisplayTimer;
        public bool IsCurrentTab
        {
            get { return subjectDisplayTimer.Enabled; }
            set { subjectDisplayTimer.Enabled = value; }
        }
        public CameraViewTab()
        {
            InitializeComponent();
            subjectDisplayTimer = new Timer();
            subjectDisplayTimer.AutoReset = true;
            subjectDisplayTimer.Interval = 100;
            subjectDisplayTimer.Elapsed += updateView;
            subjectDisplayTimer.Enabled = false;

        }

        private void updateView(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                var frame = kinect.GetLastFrame();
                if (frame == null) return;
                cameraView.frame = frame;
            }
            catch (ArgumentException)
            {
            }

        }
    }
}
