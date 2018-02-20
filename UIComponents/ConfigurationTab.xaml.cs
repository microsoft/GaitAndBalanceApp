using System;
using System.Windows.Controls;
using System.Data;
using System.Configuration;

namespace GaitAndBalanceApp.UIComponents
{
    /// <summary>
    /// Interaction logic for configurationTab.xaml
    /// </summary>
    public partial class ConfigurationTab : UserControl
    {
        private DataTable configurationParametersTable;
        public ConfigurationTab()
        {
            configurationParametersTable = new DataTable();
            InitializeComponent();
            parameters.ItemsSource = configurationParametersTable.DefaultView;
            configurationParametersTable.Columns.Add("Key", typeof(String));
            configurationParametersTable.Columns.Add("Value", typeof(String));
            foreach (var key in ConfigurationManager.AppSettings.AllKeys)
            {
                var value = ConfigurationManager.AppSettings[key];
                var row = configurationParametersTable.NewRow();
                row["Key"] = key;
                row["Value"] = value;
                configurationParametersTable.Rows.Add(row);

            }


        }

    }
}
