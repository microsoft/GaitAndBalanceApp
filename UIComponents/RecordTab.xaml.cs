using GaitAndBalanceApp.Analysis;
using System;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Timer = System.Timers.Timer;

namespace GaitAndBalanceApp.UIComponents
{
    /// <summary>
    /// Interaction logic for RecordTab.xaml
    /// </summary>

    public partial class RecordTab : UserControl
    {
        enum ErecordState { run, stop, cancel, done, idle };
        BackgroundWorker worker;       
        ErecordState recordState = ErecordState.idle;
        Voice voice = new Voice();
        Timer timeCounter;
        Timer subjectDisplayTimer;
        long lastFrameDisplayed = -1;
        long firstFrame, lastFrame; // the first and last frame to be saved
        string messageFile, notificationFile;
        TabItem parent = null;
        bool isSelected = true;
        Kinect kinect = KinectFactory.instance;
        bool audiobaleWarnings = true;
        bool autoShutDown = false; // in commandline mode allows closing the application when recording is complete
        int recordingsPerSession = 1;
        int countdown = 5;

        RecordingSettingsViewModel recordingSettings = new RecordingSettingsViewModel();


        public RecordTab()
        {
            messageFile = ConfigurationManager.AppSettings["messageFile"];
            notificationFile = ConfigurationManager.AppSettings["notificationFile"];
            InitializeComponent();
            subjectDisplayTimer = new Timer();
            subjectDisplayTimer.AutoReset = true;
            subjectDisplayTimer.Interval = 100;
            subjectDisplayTimer.Elapsed += subjectDisplayTimer_Elapsed;
            subjectDisplayTimer.Enabled = true;
            App.Current.MainWindow.Closed += MainWindow_Closed;
            if (Double.TryParse(ConfigurationManager.AppSettings["widthOfROI"], out kinect.halfWidthOfROI))
                kinect.halfWidthOfROI /= 2;
            Double.TryParse(ConfigurationManager.AppSettings["depthOfROI"], out kinect.depthOfROI);
            Double.TryParse(ConfigurationManager.AppSettings["lowerROI"], out kinect.lowerROI);
            Double.TryParse(ConfigurationManager.AppSettings["upperROI"], out kinect.upperROI);
            Double.TryParse(ConfigurationManager.AppSettings["maximalDistanceFromGroundOfSubject"], out kinect.maximalDistanceFromGroundOfSubject);
            Double.TryParse(ConfigurationManager.AppSettings["minimalDistanceFromCeilingOfSubject"], out kinect.minimalDistanceFromCeilingOfSubject);
            Double.TryParse(ConfigurationManager.AppSettings["minimalHeightOfSubject"], out kinect.minimalHeightOfSubject);
            Int32.TryParse(ConfigurationManager.AppSettings["minimalNumberOfPixelsInSubject"], out kinect.minimalNumberOfPixelsInSubject);
            bool.TryParse(ConfigurationManager.AppSettings["audiobaleWarnings"], out audiobaleWarnings);
            Int32.TryParse(ConfigurationManager.AppSettings["recordingsPerSession"], out recordingsPerSession);
            Int32.TryParse(ConfigurationManager.AppSettings["countdown"], out countdown);

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
                currentIdentifier.identifier = args[1];
            if (args.Length > 2)
                currentIdentifier.exercise = args[2];
            if (args.Length > 3)
                duration.Text = args[3];
            if (args.Length > 4)
            {
                autoShutDown = true;
                if (args[4].ToLower() == "start")
                    Start_Click(null, null);
                if (args[4].ToLower() == "delay")
                {
                   int delay = 0;
                   Int32.TryParse(args[5], out delay);
                   new Thread((ThreadStart)(() =>
                   {
                       Thread.Sleep(TimeSpan.FromSeconds(delay));
                       Start.Dispatcher.Invoke(new Action(delegate () { Start_Click(null, null); }));
                   })).Start();
                }
            }
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            recordState = ErecordState.cancel;
            subjectDisplayTimer.Dispose();
            voice.Dispose();
            kinect.Dispose();
        }

        /// <summary>
        /// This function notifies possible clients of actions being made. This way, we can record simultaneuosly by other sensors
        /// </summary>
        /// <param name="details">the information to inform the clients</param>
        void sendMessageToClients(params string[] details)
        {
            if (messageFile != null) File.WriteAllLines(messageFile, details);
            if (notificationFile != null) File.WriteAllText(notificationFile, DateTime.Now.ToLongTimeString());
        }

        void subjectDisplayTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!isSelected)
            {
                Start.Dispatcher.Invoke(new Action(delegate()
                    {
                        if (parent == null) parent = this.Parent as TabItem;
                        if (parent == null || parent.IsSelected) isSelected = true; // do not display if not selected
                    }));
                return;
            }
            try {
                var frame = kinect.getLastFrame();
                if (frame == null)
                {
                    Start.Dispatcher.Invoke(new Action(delegate ()
                    {
                        Start.IsEnabled = false;
                    }));
                    return;
                }
                if (frame.FrameNumber == lastFrameDisplayed)
                {
                    subjectView.silhouetteColor = Colors.Red;
                    return;
                }
                Start.Dispatcher.Invoke(new Action(delegate ()
                    {
                        Start.IsEnabled = true;
                        if (parent == null) parent = this.Parent as TabItem;
                        if (parent != null && !parent.IsSelected) isSelected = false; // do not display if not selected
                    fps.Content = kinect.fps.ToString("n0") + " FPS";
                    }));
                subjectView.silhouetteColor = Colors.Green;
                subjectView.frame = frame;
            } catch (ArgumentException)
            {
                notify(level.error, "Check SHO installation and make sure that the configuration file is pointing to the directory in which it is installed");
            }
        }

        public enum level { information, warning, error, failure };

        public void notify(level l, string message)
        {
            Brush b = Brushes.Purple;
            string t = "internal error 01";

            switch (l)
            {
                case level.information:
                    b = Brushes.White;
                    t = message;
                    break;
                case level.warning:
                    b = Brushes.Yellow;
                    t = String.Format("WARNING: {0}", message);
                    break;
                case level.error:
                    b = Brushes.Red;
                    t = String.Format("ERROR: {0}", message);
                    break;
                case level.failure:
                    b = Brushes.Red;
                    t = String.Format("FAILURE: {0}", message);
                    break;
            }
            log.Document.Dispatcher.Invoke(new Action(delegate()
            {
                var p = new TextRange(log.Document.ContentStart, log.Document.ContentStart);
                p.Text = t + "\n";
                p.ApplyPropertyValue(TextElement.ForegroundProperty, b);
            }));
        }

        void notify(level l, string message, params object[] rest)
        {
            notify(l, String.Format(message, rest));
            Logger.log(message, rest);
        }

        void clearLog()
        {
            log.Document.Dispatcher.Invoke(new Action(delegate()
            {
                log.Document.Blocks.Clear();
            }));
        }

        int recordingCounter = 0;

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            clearLog();

            kinect.clearMemory();
            var path = currentIdentifier.path;
            var identifier = currentIdentifier.identifier;
            var exercise = currentIdentifier.exercise;
            if (string.IsNullOrEmpty(identifier))
            {
                notify(level.warning, "please specify an identifier");
                return;
            }
            if (string.IsNullOrEmpty(exercise))
            {
                notify(level.warning, "please select the exercise to be performed");
                return;
            }
            int durationInt = -1;
            var flag = Int32.TryParse(duration.Text, out durationInt);
            if (!flag || durationInt <= 0)
            {
                notify(level.warning, "please select the duration of the exercise");
                return;
            }

            notify(level.information, "Start recording");
            var extendedIdentifier = System.IO.Path.Combine(path, String.Format("{0}_{1}", identifier, DateTime.Now.ToString(Tools.dateFormat)));
            string filename = extendedIdentifier + "_" + exercise + "_kinect.xml";
            string metricFileName = extendedIdentifier + "_" + exercise + "_analysis.tsv"; 
            bool readInstructions = recordingSettings.playInstructions == true;
            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += delegate(object s, DoWorkEventArgs args)
            {
                args.Result = record(identifier, exercise, path, durationInt, filename, readInstructions);
            };
            if (recordingCounter <= 0) recordingCounter = recordingsPerSession;
            notify(level.information, "Recordings left in this session: {0}", recordingCounter);
            worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
            {
                switch (recordState)
                {
                    case ErecordState.cancel:
                        notify(level.warning, "Recording canceled");
                        recordingCounter = 0;
                        break;
                    case ErecordState.done:
                    case ErecordState.stop:
                        notify(level.information, "Recording completed");
                        BackgroundWorker analysisWorker = new BackgroundWorker();
                        analysisWorker.DoWork += delegate(object s1, DoWorkEventArgs args1)
                        {
                            kinect.writeToFile(filename, firstFrame, lastFrame);
                            var analyzer = Exercises.getAnalyzer(exercise);
                            var metrics = analyzer.analyzeAndAnnotate(filename);
                            metrics.save(metricFileName);
                        };
                        analysisWorker.RunWorkerAsync();
                        break;

                    default:
                        notify(level.failure, "Internal error: recordState == {0}", Enum.GetName(typeof(ErecordState), recordState));
                        break;
                }
                recordState = ErecordState.idle;
                Start.Visibility = System.Windows.Visibility.Visible;
                Stop.Visibility = System.Windows.Visibility.Hidden;
                if (--recordingCounter > 0)
                    Start_Click(sender, e);
                else if (autoShutDown)
                { // wait for the analysis to terminate and exit
                    Thread.Sleep(5000);
                    Environment.Exit(0);

                }
            };
            worker.RunWorkerAsync();
            Start.Visibility = System.Windows.Visibility.Hidden;
            Stop.Visibility = System.Windows.Visibility.Visible;
        }

        bool record(string identifier, string exercise, string path, int duration, string filename, bool readInstructions)
        {
            TimeSpan gapBetweenCalls = new TimeSpan(0, 0, 5);
            recordState = ErecordState.run;
            if (readInstructions == true) voice.SpeakBlocking(Exercises.getDescription(exercise));
            Thread.Sleep(2000);

            if (recordState != ErecordState.run) return false;
            for (int i = countdown; i > 0; i--)
            {
                voice.Speak(i.ToString());
                Thread.Sleep(1500);
                if (recordState != ErecordState.run) return false;
            }
            kinect.intensityBasedCutoffThreshold = recordingSettings.reflectiveSeperation ? ushort.Parse(ConfigurationManager.AppSettings["reflectiveSeperationCutoff"]) : (ushort)(0);
            voice.Speak("Go!");
            firstFrame = kinect.getLastFrame().FrameNumber;
            sendMessageToClients("record", identifier, exercise, path, duration.ToString(), filename);
            recordState = ErecordState.run;
            timeCounter = new Timer(duration * 1000);
            timeCounter.Elapsed += record_Elapsed;
            timeCounter.Start();
            DateTime startTime = DateTime.Now;
            DateTime nextRead = startTime + gapBetweenCalls;
            try
            {
                while (recordState == ErecordState.run)
                {
                    if (DateTime.Now > nextRead)
                    {
                        var timeCount = (DateTime.Now - startTime).TotalMilliseconds;
                        long collectedKinectFrames = kinect.getLastFrame().FrameNumber - firstFrame;

                        if (audiobaleWarnings && collectedKinectFrames < timeCount * 0.95 * 30.0 / 1000.0)
                            voice.Speak("Alert: too many frames lost!");
                        else voice.Speak(((int)(timeCount / 1000)).ToString());
                        nextRead += gapBetweenCalls;
                    }
                    Thread.Sleep(10);
                }
                voice.SpeakBlocking("done");
            }
            finally
            {
                timeCounter.Dispose();
            }
            return (true);
        }

        void record_Elapsed(object sender, EventArgs e)
        {
            lastFrame = kinect.getLastFrame().FrameNumber;
            recordState = ErecordState.done;
            sendMessageToClients("done");
        }


        private void Remote_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (recordingSettings.remoteControl  && e.Key == System.Windows.Input.Key.VolumeUp)
                switch (recordState)
                {
                    case ErecordState.idle:
                        if (Start.IsEnabled) Start_Click(sender, null);
                        else notify(level.warning, "Can't start recording, no Kinect sensor ready");
                        break;
                    case ErecordState.run: Stop_Click(sender, null); break;
                    default: notify(level.warning, "Cannot handle key press at this time"); break;

                }

        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var w = new RecordingSettings();
            w.bind(recordingSettings);
            w.Show();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            lastFrame = kinect.getLastFrame().FrameNumber;
            recordState = ErecordState.stop;
            sendMessageToClients("stop");
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            lastFrame = -1;
            recordState = ErecordState.cancel;
            sendMessageToClients("cancel");
  
        }

     }
}
