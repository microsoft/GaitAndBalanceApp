using System.ComponentModel;
using System.Configuration;
using System.Windows;

namespace GaitAndBalanceApp.UIComponents
{
    /// <summary>
    /// Interaction logic for RecordingSettings.xaml
    /// </summary>
    public partial class RecordingSettings : Window
    {
        RecordingSettingsViewModel _model = null;
        public RecordingSettings()
        {
            InitializeComponent();
        }

        private void save_Click(object sender, RoutedEventArgs e)
        {
            if (_model != null) _model.save();
            this.Close();
        }

        public void bind(RecordingSettingsViewModel model)
        {
            _model = model;
            DataContext = model;
        }
    }


    public class RecordingSettingsViewModel : INotifyPropertyChanged
    {
        public RecordingSettingsViewModel()
        {
            _remoteControl = ConfigurationManager.AppSettings["allowRemoteControl"] == "true";
            _playInstructions = ConfigurationManager.AppSettings["playInstructions"] == "true";
            _reflectiveSeperation = ConfigurationManager.AppSettings["reflectiveSeperation"] == "true";
            _audioWarnings = ConfigurationManager.AppSettings["audioWarnings"] == "true";
        }

        public void save()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            config.AppSettings.Settings["allowRemoteControl"].Value = _remoteControl ? "true" : "false";
            config.AppSettings.Settings["playInstructions"].Value = _playInstructions ? "true" : "false";
            config.AppSettings.Settings["reflectiveSeperation"].Value = _reflectiveSeperation ? "true" : "false";
            config.AppSettings.Settings["audioWarnings"].Value = _audioWarnings ? "true" : "false";

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");

        }

        public bool remoteControl { get { return _remoteControl; } set { if (_remoteControl != value) { _remoteControl = value; RaisePropertyChanged("remoteControl"); } } }
        public bool playInstructions { get { return _playInstructions; } set { if (_playInstructions != value) { _playInstructions = value; RaisePropertyChanged("playInstructions"); } } }
        public bool reflectiveSeperation { get { return _reflectiveSeperation; } set { if (_reflectiveSeperation != value) { _reflectiveSeperation = value; RaisePropertyChanged("reflectiveSeperation"); } } }

        public bool audioWarnings { get { return _audioWarnings; } set { if (_audioWarnings != value) { _audioWarnings = value; RaisePropertyChanged("audioWarnings"); } } }


        public bool _remoteControl;
        public bool _playInstructions; 
        public bool _reflectiveSeperation;
        public bool _audioWarnings;

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
