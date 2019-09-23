using GaitAndBalanceApp.Analysis;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace GaitAndBalanceApp.UIComponents
{
    /// <summary>
    /// Interaction logic for CurrentIdentifier.xaml
    /// </summary>
    public partial class CurrentIdentifier : UserControl
    {

        public static readonly RoutedEvent CurrentIdentifierChangedEvent =
            EventManager.RegisterRoutedEvent("CurrentIdentifierChanged", RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(CurrentIdentifier));

        public event RoutedEventHandler CurrentIdentifierChanged
        {
            add { AddHandler(CurrentIdentifierChangedEvent, value); }
            remove { RemoveHandler(CurrentIdentifierChangedEvent, value); }
        }

        public string Identifier { get { return _identifier.Text; } set { _identifier.Text = value; } }
        public string Exercise { get { return _exercise.Text; } set { _exercise.Text = value; } }
        public string Path { get { return _path.Text; } set { _path.Text = value; } }

        public CurrentIdentifier()
        {
            InitializeComponent();
            var exercises = Exercises.GetExercises();
            if (exercises != null)
                foreach (var e in exercises)
                    _exercise.Items.Add(e);
        }

        private void SelectPath_Click(object sender, RoutedEventArgs e)
        {
            var selectIdentifier = new SelectIdentifier
            {
                directory = Path,
                Owner = App.Current.MainWindow
            };
            selectIdentifier.ShowDialog();
            if (selectIdentifier.directory != null)
                Path = selectIdentifier.directory;
            if (selectIdentifier.identifier != null)
                Identifier = selectIdentifier.identifier;
            if (selectIdentifier.exercise != null)
                Exercise = selectIdentifier.exercise;
            NotifyIndetifierChanged();
        }


        private void NotifyIndetifierChanged()
        {
            RaiseEvent(new RoutedEventArgs(CurrentIdentifier.CurrentIdentifierChangedEvent));
        }

        private void ValuesChanged(object sender, EventArgs e)
        {
            NotifyIndetifierChanged();
        }

        private void Instructions_Click(object sender, RoutedEventArgs e)
        {
            string setup = Exercises.GetSetup(Exercise);
            if (setup == null) setup = "Please select an exercise";
            if (setup.Length == 0) setup = "Sorry, no setup instructions for this exercise";

            Popup popup = new Popup();
            var textBlock = new TextBlock
            {
                Text = "Guidelines for the " + Exercise + " exercise.\n\n" + setup,
                Background = Brushes.LightBlue,
                Foreground = Brushes.Blue
            };

            popup.Child = textBlock;
        }

        private void ValuesChanged(object sender, SelectionChangedEventArgs e)
        {
            string setup = "Please select and exercise";
            if (e.AddedItems.Count > 0)
            {
                setup = Exercises.GetSetup((string)e.AddedItems[0]);
                if (setup == null) setup = "Please select an exercise";
                if (setup.Length == 0) setup = "Sorry, no setup instructions for this exercise";
            }
            instructions.ToolTip = setup;
        }

    }

    public class CurrentIdentifierChangedEventArgs : EventArgs
    {
        public string Identifier { get; set; }
        public string Exercise { get; set; }
        public string Path { get; set; }


    }

}
