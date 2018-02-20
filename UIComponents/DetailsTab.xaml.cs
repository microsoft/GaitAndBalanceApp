using GaitAndBalanceApp.Analysis;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Timer = System.Timers.Timer;

namespace GaitAndBalanceApp.UIComponents
{
    /// <summary>
    /// Interaction logic for DetailsTab.xaml
    /// </summary>
    public partial class DetailsTab : UserControl
    {
        
        public ObservableCollection<AnalysisFile> files {get; set;}
        public DataTable detailsTable;
        ObservableCollection<System.Windows.Point> points = new ObservableCollection<System.Windows.Point>();
        Trajectory trajectory = new Trajectory();
        List<Frame> frames = new List<Frame>();
        Dictionary<long, string> frameToSegmentName = new Dictionary<long, string>();
        Timer playbackTimer = new Timer();
        bool applyLowPassFilterToTimeDomain = true;
        GaitFile gaitFile = null;
 
        double maxX;
        double minX;
        double maxZ;
        double minZ;
        double rangeX;
        double rangeZ;
        double scale;

        public DetailsTab()
        {
            bool.TryParse(ConfigurationManager.AppSettings["applyLowPassFilterToTimeDomain"], out applyLowPassFilterToTimeDomain);
            files = new ObservableCollection<AnalysisFile>();
            detailsTable = new DataTable();
            InitializeComponent();
            recordTimes.ItemsSource = files;
            recordDetails.ItemsSource = detailsTable.DefaultView;
            detailsTable.Columns.Add("Metric", typeof(String));
            detailsTable.Columns.Add("Value", typeof(String));
            detailsTable.Columns.Add("Typical Range", typeof(String));
            detailsTable.Columns.Add("In Range", typeof(bool));
            detailsTable.Columns.Add("Description", typeof(String));
            detailsTable.Columns.Add("Preference", typeof(String));
            App.Current.MainWindow.Closed += MainWindow_Closed;
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            playbackTimer.Dispose();
        }


        private void currentIdentifier_CurrentIdentifierChanged(object sender, RoutedEventArgs e)
        {
            files.Clear();
            var list = Directory.GetFiles(currentIdentifier.path, currentIdentifier.identifier + "_*_" + currentIdentifier.exercise + "_analysis.tsv");
            if (list == null) return;
            foreach (var file in list) files.Add(new AnalysisFile(file));
            if (list.Length > 0) recordTimes.SelectedIndex = 0;
        }

        private void drawTimeDomainGraph()
        {
            if (!trajectory.points.Any()) return;
            maxX = trajectory.points.Select(p => p.x).Max();
            minX = trajectory.points.Select(p => p.x).Min();
            maxZ = trajectory.points.Select(p => p.z).Max();
            minZ = trajectory.points.Select(p => p.z).Min();
            rangeX = maxX - minX;
            rangeZ = maxZ - minZ;
            scale = Math.Min(timeLineCanvas.ActualWidth / rangeZ, timeLineCanvas.ActualHeight / rangeX);
            
            if (Double.IsNaN(rangeZ) || Double.IsNaN(rangeX) || Double.IsNaN(scale) || Double.IsInfinity(scale))
            {
                maxX = minX = maxZ = minZ = rangeX = rangeZ = scale = -1;
            }
     
            timeLine.Stroke = System.Windows.Media.Brushes.SlateGray;
            timeLine.StrokeThickness = 2;
            timeLine.FillRule = FillRule.EvenOdd;



            PointCollection points = new PointCollection();
            foreach (var p in trajectory.points)
            {
                var point = new System.Windows.Point((p.z - minZ) * scale , (p.x - minX) * scale);
                points.Add(point);
            }
            timeLine.Points = points;
            timeLineXMax.Content = maxX.ToString("n3");
            timeLineXMax.Margin = new Thickness(0, timeLineCanvas.ActualHeight - 20, 0, 0);

            timeLineMin.Content = String.Format("  {0}\n{1}", minZ.ToString("n3"),minX.ToString("n3"));
            timeLineMin.Margin = new Thickness(0, 0, 0, 0);

            timeLineZMax.Content = maxZ.ToString("n3");
            timeLineZMax.Margin = new Thickness(rangeZ * scale, 0, 0, 0);


            RoutedPropertyChangedEventArgs<double> e = new RoutedPropertyChangedEventArgs<double>(frameSelector.Value, frameSelector.Value, null);
            frameSelector_ValueChanged(null, e);
        }

        private void frameSelector_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var val = (int)e.NewValue;
            if (val >= trajectory.points.Count)
            {
                stopPlayBack();
                return;
            }
            var p = trajectory.points[val];
            if (!Double.IsNaN(p.z))
            {
                timeLineEllipse.Margin = new Thickness((p.z - minZ) * scale - timeLineEllipse.Width / 2, (p.x - minX) * scale - timeLineEllipse.Height / 2, 0, 0);
                subjectView.frame = frames[val];
            }
            var segmentType = "-";
            frameToSegmentName.TryGetValue(p.timeStamp, out segmentType);
            segmentTypeLabel.Dispatcher.Invoke(new Action(delegate ()
            {
                segmentTypeLabel.Content = segmentType;
            }));
        }

        private void recordTimes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            detailsTable.Clear();
            timeLine.Points.Clear();
            trajectory.points.Clear();
            frames.Clear();
            if (e.AddedItems.Count == 0)
                return;
            string filename = ((AnalysisFile)e.AddedItems[0]).fullName;
            var metrics = Metrics.load(filename);
            foreach (Metric m in metrics)
            {
                var row = detailsTable.NewRow();
                row["Metric"] = m.name;
                row["Value"] = m.value.ToString(m.formatting);
                row["Typical Range"] = String.Format("{0} - {1}", m.lowerTypicalRange.ToString(m.formatting), m.higherTypicalRange.ToString(m.formatting));
                row["Preference"] = Enum.GetName(typeof(Epreference), m.preferedRange);
                row["Description"] = m.description;
                bool inRange = false;
                switch (m.preferedRange)
                {
                    case Epreference.noPreference: inRange = true; break;
                    case Epreference.higherIsBetter: if (m.value >= m.lowerTypicalRange) inRange = true; break;
                    case Epreference.lowerIsBetter: if (m.value <= m.higherTypicalRange) inRange = true; break;
                    case Epreference.inRange: if (m.value <= m.higherTypicalRange && m.value >= m.lowerTypicalRange) inRange = true; break;
                }
                row["In Range"] = inRange;
                detailsTable.Rows.Add(row);
            }
                
            string prefix = System.IO.Path.GetFileName(filename);
            DateTime dt;
            string exercise, identifier;
            if (!Tools.parseFileName(prefix, out identifier, out exercise, out dt))
                return;
            var fileName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filename), identifier) + "_" + dt.ToString(Tools.dateFormat) + "_" + exercise + "_kinect.xml";
            gaitFile = new GaitFile();
            gaitFile.ReadFile(fileName);
            long frameNum = -1;
            Frame fr;
            while ((fr = gaitFile.GetNextFrame(frameNum)) != null)
            {
                if (trajectory.add(fr)) frames.Add(fr);
                frameNum = fr.FrameNumber;
            }
            trajectory.samplingRate = 30;
            if (applyLowPassFilterToTimeDomain) trajectory.filter();

            var analyzer = Exercises.getAnalyzer(exercise);
            var segments = analyzer.segmentTrajectory(trajectory);
            int segmentCounter = 0;
            foreach (var segment in segments)
            {
                segmentCounter++;
                var name = Enum.GetName(typeof(Analyzer.ESegmentType), segment.Item2);
                var str = String.Format("{0}) {1}", segmentCounter, name);
                foreach (var p in segment.Item1.points)
                    frameToSegmentName[p.timeStamp] = str;
            }



            frameSelector.Maximum = trajectory.points.Count - 1;
            frameSelector.Value = 0;
            frameSelector.TickFrequency = 300; // ten seconds
            drawTimeDomainGraph();
        }

        private void stopPlayBack()
        {
            playbackTimer.Enabled = false;
            playbackTimer.Elapsed -= playbackTimer_Elapsed;
            play.Background = Brushes.YellowGreen;
            play.Content = "Play";

        }

        private void play_Click(object sender, RoutedEventArgs e)
        {
            if (playbackTimer.Enabled)
                stopPlayBack();
            else
            { // start playback
                play.Background = Brushes.LightSalmon;
                play.Content = "Pause";
                playbackTimer.Interval = 20;
                playbackTimer.AutoReset = true;
                playbackTimer.Elapsed += playbackTimer_Elapsed;
                playbackTimer.Enabled = true;
            }

        }

        void playbackTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            frameSelector.Dispatcher.Invoke(new Action(delegate()
                {
                    frameSelector.Value = Math.Min(frameSelector.Maximum, frameSelector.Value + 1);
                    if (frameSelector.Value == frameSelector.Maximum)
                    {
                        stopPlayBack();
                        frameSelector.Value = 0;
                    }
                }));

        }

        private void exportTimeAndFreqDomain_Click(object sender, RoutedEventArgs e)
        {

            string filename = ((AnalysisFile)recordTimes.SelectedItem).fullName;

            string prefix = System.IO.Path.GetFileName(filename);
            DateTime dt;
            string exercise, identifier;
            if (!Tools.parseFileName(prefix, out identifier, out exercise, out dt))
                return;
            var timeSeriesFileName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filename), identifier) + "_" + dt.ToString(Tools.dateFormat) + "_" + exercise + "_timeSeries.tsv";

            var lines = trajectory.points.Select(p => String.Format("{0}\t{1}\t{2}", p.timeStamp, p.x, p.z));
            File.WriteAllText(timeSeriesFileName, "timeStamp\tx\tz\n");
            File.AppendAllLines(timeSeriesFileName, lines);

            FrequencyStats fs = new FrequencyStats(trajectory);
            int length = fs.Fx.Length;
            var frequencyDomainFileName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filename), identifier) + "_" + dt.ToString(Tools.dateFormat) + "_" + exercise + "_frequencyDomain.tsv";
            File.WriteAllText(frequencyDomainFileName, "Frequency\tPower\n");
            lines = fs.Fx.Zip(fs.Pxx, (f, p) => String.Format("{0}\t{1}", f, p));
            File.AppendAllLines(frequencyDomainFileName, lines);

            var segmentsFileName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filename), identifier) + "_" + dt.ToString(Tools.dateFormat) + "_" + exercise + "_segments.tsv";
            File.WriteAllText(segmentsFileName, "timeStamp\tx\tz\tnormalized-x\tnormalized-z\tslope-x\tslope-z\tsegment-number\tsegment-type\n");
            var analyzer = Exercises.getAnalyzer(exercise);
            var segments = analyzer.segmentTrajectory(trajectory);
            int segmentCounter = 0;
            foreach (var segment in segments)
            {
                segmentCounter++;
                var normalizedSegment = analyzer.normalizeTrajectory(segment.Item1, segment.Item2);
                var name = Enum.GetName(typeof(Analyzer.ESegmentType), segment.Item2);
                int segmentLength = segment.Item1.points.Count();
                List<string> thisSegmentLines = new List<string>();
                for (int i = 0; i < segmentLength; i++)
                {
                    var p = segment.Item1.points[i];
                    var np = normalizedSegment.points[i];
                    var slp = segment.Item1.slopes[i];
                    thisSegmentLines.Add(String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
                        p.timeStamp, p.x, p.z, np.x, np.z, slp.x, slp.z, segmentCounter, name));
                }
                File.AppendAllLines(segmentsFileName, thisSegmentLines);
            }
            var segmentsFreqFileName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filename), identifier) + "_" + dt.ToString(Tools.dateFormat) + "_" + exercise + "_segments_freq.tsv";
            File.WriteAllText(segmentsFreqFileName, "freq\tpower\tsegment\n");
            segmentCounter = 0;
            foreach (var segment in segments)
            {
                segmentCounter++;
                var sgmntFs = new FFTStats(segment.Item1, FFTStats.EDirection.Z, true);
                lines = fs.Fx.Zip(sgmntFs.Pxx, (f, p) => String.Format("{0}\t{1}\t{2}", f, p, segmentCounter));
                File.AppendAllLines(segmentsFreqFileName, lines);
            }

        }
    }

    public class AnalysisFile : INotifyPropertyChanged
    {

        private String _time;
        public String time {get {return _time;} set {if (_time != value) {_time = value; OnPropertyChanged("timeProperty");}} }
        private String _fullName;
        public String fullName {get {return _fullName;} set {if (_fullName != value) {_fullName = value; OnPropertyChanged("fullNameProperty");}} }
        public event PropertyChangedEventHandler PropertyChanged;

        public AnalysisFile(string fullName)
        {
            string identifier, exercise;
            DateTime date;
            Tools.parseFileName(fullName, out identifier, out exercise, out date);

            this.fullName = fullName;
            this.time = date.ToString("u");
        }

        public override string ToString()
        {
 	         return time;
        }

        protected void OnPropertyChanged(string name)
        {

            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }

        }

    }
}
