using GaitAndBalanceApp.Analysis;
using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace GaitAndBalanceApp
{
    /// <summary>
    /// Interaction logic for SelectUser.xaml
    /// </summary>
    public partial class SelectIdentifier : Window
    {
        public string directory { get { return _directory.Text; } set { _directory.Text = value; } }
        string _identifier = null;
        string _exercise = null;
        public string identifier { get { return _identifier; } }
        public string exercise { get { return _exercise; } }

        DataTable options = new DataTable();

        public SelectIdentifier()
        {

            options.Columns.Add("Identifier", typeof(String));
            options.Columns.Add("Exercise", typeof(String));
            options.Columns.Add("# records", typeof(int));
            options.Columns.Add("last seen", typeof(DateTime));
 
            InitializeComponent();
            participants.ItemsSource = options.DefaultView;

        }

        void populateDataTable()
        {
            options.Rows.Clear();
            string[] list = null;

            try
            {
                list = Directory.GetFiles(_directory.Text, "*_*_*_analysis.tsv");
            }
            catch (DirectoryNotFoundException)
            {
                return;
            }

            if (list == null) return;
            foreach (var sample in list)
            {
                string filename = System.IO.Path.GetFileName(sample);
                DateTime dt;
                string exercise, identifier;
                if (!Tools.parseFileName(filename, out identifier, out exercise, out dt))
                    continue;
                bool found = false;
                identifier = identifier.ToLower();
                foreach (DataRow row in options.Rows)
                {
                    if ((string)row["Identifier"] == identifier && (string)row["Exercise"] == exercise)
                    {
                        row["# records"] = ((int)(row["# records"])) + 1;
                        if (dt > (DateTime)row["last seen"]) row["last seen"] = dt;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    var row = options.NewRow();
                    row["Identifier"] = identifier;
                    row["Exercise"] = exercise;
                    row["# records"] = 1;
                    row["last seen"] = dt;

                    options.Rows.Add(row);
                }
            }
        }

        private void _directory_TextChanged(object sender, TextChangedEventArgs e)
        {
            populateDataTable();
        }

        private void selectDirectory_Click(object sender, RoutedEventArgs e)
        {
            string path = Tools.getPath(_directory.Text);
            if (path != null)
                _directory.Text = path;
        }

        private void selections_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataRowView row = participants.SelectedItem as DataRowView;
            _identifier = (string)row["Identifier"];
            _exercise = (string)row["Exercise"];
            this.Close();

        }

        private void refreshDirectory_Click(object sender, EventArgs e)
        {
            var list = Directory.GetFiles(_directory.Text, "*_kinect.xml");
            Task refreshTask = Task.Factory.StartNew(() =>
            {
                int currentIndex = 0;
                object locker = new object();
                if (list == null || list.Length == 0) return;
                int numberOfThreads = Environment.ProcessorCount - 1;
                if (numberOfThreads < 1) numberOfThreads = 1;
                if (numberOfThreads > 8) numberOfThreads = 8; // prevent chocking the file system.
                Task[] tasks = new Task[numberOfThreads];
                Logger.log("refresh: starting with {0} threads", numberOfThreads);
                for (int i = 0; i < tasks.Length; i++)
                {
                    tasks[i] = Task.Factory.StartNew(() =>
                    {
                        string filename;
                        while (currentIndex < list.Length)
                        {
                            lock (locker)
                            {
                                if (currentIndex < list.Length)
                                    filename = list[currentIndex++];
                                else
                                    filename = "";
                            }
                            if (filename.Length == 0) break;
                            Logger.log("refresh: start file {0}", filename);

                            string prefix = Path.GetFileName(filename);
                            DateTime dt;
                            string exercise, identifier;
                            try
                            {
                                if (!Tools.parseSampleFileName(prefix, out identifier, out exercise, out dt))
                                {
                                    Logger.log("refresh: failed parsing filename for {0}", filename);
                                    continue;
                                }
                            }
                            catch (Exception e2)
                            {
                                Logger.log("refresh: failed parsing filename: {0}:\n{1}", filename, e2);
                                continue;

                            }
                            Analyzer analyzer = null;
                            try
                            {
                                analyzer = Exercises.GetAnalyzer(exercise);
                            }
                            catch (Exception e1)
                            {
                                Logger.log("refresh: failed creating analyzer for the file: {0}:\n{1}", filename, e1);
                                continue;
                            }
                            if (analyzer == null)
                            {
                                Logger.log("refresh: failed analyzing the file: {0}", filename);
                                continue;
                            }

                            var metricFileName = Path.Combine(Path.GetDirectoryName(filename), identifier) + "_" + dt.ToString(Tools.dateFormat) + "_" + exercise + "_analysis.tsv";
                            var metrics = analyzer.analyzeAndAnnotate(filename);
                            Logger.log("refresh: saving metrics for {0}", filename);
                            metrics.save(metricFileName);
                            Logger.log("refresh: done analyzing {0}", filename);
                            notifications.Dispatcher.Invoke(new Action(delegate ()
                            {
                                populateDataTable();
                            }));

                        }
                        Logger.log("refresh: thread complete");
                    });
                }

                Task.WaitAll(tasks);
                notifications.Dispatcher.Invoke(new Action(delegate ()
                {
                    populateDataTable();
                }));

            });
        }

    }
}
