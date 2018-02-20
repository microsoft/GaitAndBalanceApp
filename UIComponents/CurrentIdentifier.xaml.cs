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

        public string identifier { get { return _identifier.Text; } set { _identifier.Text = value; } }
        public string exercise { get { return _exercise.Text; } set { _exercise.Text = value; } }
        public string path { get { return _path.Text; } set { _path.Text = value; } }

        public CurrentIdentifier()
        {
            InitializeComponent();
            var exercises = Exercises.getExercises();
            if (exercises != null)
                foreach (var e in exercises)
                    _exercise.Items.Add(e);
        }

        private void selectPath_Click(object sender, RoutedEventArgs e)
        {
            var selectIdentifier = new SelectIdentifier();
            selectIdentifier.directory = path;
            selectIdentifier.Owner = App.Current.MainWindow;
            selectIdentifier.ShowDialog();
            if (selectIdentifier.directory != null)
                path = selectIdentifier.directory;
            if (selectIdentifier.identifier != null)
                identifier = selectIdentifier.identifier;
            if (selectIdentifier.exercise != null)
                exercise = selectIdentifier.exercise;
            notifyIndetifierChanged();
        }


        private void notifyIndetifierChanged()
        {
            RaiseEvent(new RoutedEventArgs(CurrentIdentifier.CurrentIdentifierChangedEvent));
        }

        private void valuesChanged(object sender, EventArgs e)
        {
            notifyIndetifierChanged();
        }

        private void instructions_Click(object sender, RoutedEventArgs e)
        {
            string setup = Exercises.getSetup(exercise);
            if (setup == null) setup = "Please select an exercise";
            if (setup.Length == 0) setup = "Sorry, no setup instructions for this exercise";

            Popup popup = new Popup();
            var textBlock = new TextBlock();
            textBlock.Text = "Guidelines for the " + exercise + " exercise.\n\n" + setup;
            textBlock.Background = Brushes.LightBlue;
            textBlock.Foreground = Brushes.Blue;

            popup.Child = textBlock;
        }

        private void valuesChanged(object sender, SelectionChangedEventArgs e)
        {
            string setup = "Please select and exercise";
            if (e.AddedItems.Count > 0)
            {
                setup = Exercises.getSetup((string)e.AddedItems[0]);
                if (setup == null) setup = "Please select an exercise";
                if (setup.Length == 0) setup = "Sorry, no setup instructions for this exercise";
            }
            instructions.ToolTip = setup;
        }

    }

    public class CurrentIdentifierChangedEventArgs : EventArgs
    {
        public string identifier { get; set; }
        public string exercise { get; set; }
        public string path { get; set; }


    }

}
