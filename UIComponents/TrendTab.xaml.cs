using GaitAndBalanceApp.Analysis;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GaitAndBalanceApp.UIComponents
{
    /// <summary>
    /// Interaction logic for TrendTab.xaml
    /// </summary>
    public partial class TrendTab : UserControl
    {
        DataTable userFiles;

        public TrendTab()
        {
            userFiles = new DataTable();
            InitializeComponent();
            availableFiles.ItemsSource = userFiles.DefaultView;

        }


        private void populateUserFileTable()
        {
            while (userFiles.Columns.Count > 0)
                removeColumn();
            userFiles.Rows.Clear();
            int colIndex;
            char[] underScore = new char[] { '_' };
            if (String.IsNullOrEmpty(currentIdentifier.path) || String.IsNullOrEmpty(currentIdentifier.identifier) || String.IsNullOrEmpty(currentIdentifier.exercise))
                return;

            try
            {
                var list = Directory.GetFiles(currentIdentifier.path, currentIdentifier.identifier + "_*_" + currentIdentifier.exercise + "_analysis.tsv");
                if (list == null) return;
                addColumn("metric", typeof(String));
                Array.Sort(list);
                foreach (var sample in list)
                {
                    DateTime dt;
                    string e, n;
                    if (!Tools.parseFileName(sample, out n, out e, out dt))
                        continue;
                    string dateString = dt.ToString("u");
                    List<Metric> metrics = null;
                    metrics = Metrics.load(sample);
                    colIndex = addColumn(dateString, typeof(double));
                    foreach (var m in metrics)
                    {
                        var rows = userFiles.AsEnumerable().Where(r => ((string)r["metric"] == m.name)).GetEnumerator();
                        if (!rows.MoveNext())
                        {
                            var row = userFiles.NewRow();

                            row["metric"] = m.name;
                            row[colIndex] = m.value.ToString(m.formatting);
                            userFiles.Rows.Add(row);
                        }
                        else
                        {
                            (rows.Current)[dateString] = m.value.ToString(m.formatting);
                        }
                    }
                }
            }
            catch (DirectoryNotFoundException) { }
            updateTrend();
        }

        private int addColumn(string name, Type t)
        {
            var column = new DataColumn();
            column.DataType = t;
            column.ColumnName = name;
            userFiles.Columns.Add(column);
            DataGridTextColumn gridColumn = new DataGridTextColumn();
            gridColumn.Header = name;
            gridColumn.Binding = new System.Windows.Data.Binding(name);
            availableFiles.Columns.Add(gridColumn);
            return column.Ordinal;
        }


        private void removeColumn()
        {
            availableFiles.Columns.RemoveAt(0);
            userFiles.Columns.RemoveAt(0);
        }
        private void updateTrend()
        {
            trend.Children.Clear();
            if (userFiles == null || userFiles.Columns.Count == 0 || userFiles.Rows.Count == 0) return;
            double[] values = new double[userFiles.Columns.Count - 1];
            var step = trend.ActualWidth / (values.Length - 1);
            var scale = trend.ActualHeight;
            int r = 0, g = 0, b = 255;
            int numberOfRows = userFiles.Rows.Count;
            int rowNumber = 0;


            foreach (DataRow row in userFiles.Rows)
            {
                var name = row[0];
                double mx = Double.MinValue, mn = Double.MaxValue;
                for (int i = 0; i < values.Length; i++)
                {
                    var t = row.ItemArray[i + 1] as Double?;
                    if (t == null)
                    {
                        values[i] = Double.NaN;
                        continue;
                    }
                    values[i] = t.Value;
                    if (Double.IsNaN(values[i]) || Double.IsInfinity(values[i])) continue;
                    if (values[i] > mx) mx = values[i];
                    if (values[i] < mn) mn = values[i];
                }
                if (values.Length == 0) continue;
                var range = mx - mn;
                if (range <= 1e-15) range = 1;
                range /= scale;
                for (int i = 0; i < values.Length - 1; i++)
                {
                    if (Double.IsNaN(values[i]) || Double.IsInfinity(values[i])) continue;
                    if (Double.IsNaN(values[i + 1]) || Double.IsInfinity(values[i + 1])) continue;
                    Line ln = new Line();
                    ln.StrokeThickness = 4;
                    ln.X1 = i * step;
                    ln.X2 = (i + 1) * step;
                    ln.Y1 = (mx - values[i]) / range;
                    ln.Y2 = (mx - values[i + 1]) / range;
                    ln.Stroke = new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));
                    ln.ToolTip = name;
                    ln.MouseUp += ln_MouseUp;
                    trend.Children.Add(ln);


                }

                if (rowNumber * 2 < numberOfRows)
                {
                    g += 510 / numberOfRows;
                    r += 510 / numberOfRows;
                    b -= 510 / numberOfRows;
                }
                else
                {
                    g -= 510 / numberOfRows;
                }
                rowNumber++;
            }
        }

        void ln_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Line ln = sender as Line;
            if (ln == null) return;
            {
                foreach (DataRowView row in availableFiles.Items)
                {
                    if ((string)ln.ToolTip == (string)row.Row.ItemArray[0])
                    {
                        availableFiles.SelectedItem = row;
                        break;
                    }
                }
            }
        }

        private void trend_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            availableFiles.UnselectAll();

            updateTrend();
        }

        private void availableFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var removed = e.RemovedItems;
            var added = e.AddedItems;

            var blue = new SolidColorBrush(Colors.Blue);
            var gray = new SolidColorBrush(Colors.Gray);

            HashSet<string> selectedMetrics = new HashSet<string>();
            foreach (DataRowView r in added)
                selectedMetrics.Add((string)r.Row.ItemArray[0]);

            foreach (var ln in trend.Children)
            {
                Line line = ln as Line;
                if (line != null)
                {
                    line.Opacity = (selectedMetrics.Contains((string)line.ToolTip)) ? 1 : 0.05;
                }
            }
        }

        private void currentIdentifier_CurrentIdentifierChanged(object sender, RoutedEventArgs e)
        {
            populateUserFileTable();
        }


    }
}
