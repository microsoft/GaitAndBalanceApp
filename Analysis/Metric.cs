using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Configuration;

namespace GaitAndBalanceApp.Analysis
{
    public enum Epreference { lowerIsBetter, inRange, higherIsBetter, noPreference};
    public class Metric
    {
        public string name {get; set;}
        public double value { get; set; }
        public string description { get; set; }
        public Epreference preferedRange {get; set;}
        public double lowerTypicalRange {get; set;}
        public double higherTypicalRange {get; set;}
        public string formatting {get; set;}
    }

    /// <summary>
    /// utilities used with metrics
    /// </summary>
    public static class Metrics
    {
        public static string metricToString(this Metric metric)
        {
            return String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}", metric.name, metric.value, metric.description, Enum.GetName(typeof(Epreference), metric.preferedRange), metric.lowerTypicalRange, metric.higherTypicalRange, metric.formatting);
        }

        public static Metric parseMetric(this string s)
        {
            Metric m = new Metric();
            string[] f = s.Split('\t');

            m.name = f[0];
            m.value = Double.Parse(f[1]);
            m.description = f[2];
            m.preferedRange = (Epreference)Enum.Parse(typeof(Epreference), f[3]);
            m.lowerTypicalRange = Double.Parse(f[4]);
            m.higherTypicalRange = Double.Parse(f[5]);
            m.formatting = f[6];
            return m;
        }

        public static void save(this List<Metric> metrics, string filename)
        {
            try
            {
                File.WriteAllLines(filename, metrics.Select(m => m.metricToString()));
            }
            catch (Exception e)
            {
                Logger.log("Metric: caught exeption when savig to {0}:\n{1}", filename, e);
            }
        }

        public static List<Metric> load(string filename)
        {
            var lines = File.ReadAllLines(filename);
            return lines.Select(l => l.parseMetric()).ToList();
        }

        public static void addMetaData(List<Metric> metrics, string exerciseNameInConfiguration)
        {
            var section = ConfigurationManager.GetSection(exerciseNameInConfiguration) as MetricRangesSection;
            var ranges = section.MetricRangesCollection;
            if (ranges != null)
            {
                foreach (Metric m in metrics)
                {
                    string canonicalName = m.name.Split('#')[0];
                    foreach (var r in ranges)
                    {
                        var t = r as MetricRangesElement;
                        if (t.name == canonicalName)
                        {

                            m.higherTypicalRange = t.higherTypicalRange;
                            m.lowerTypicalRange = t.lowerTypicalRange;
                            m.preferedRange = t.preferedRange;
                            m.description = t.description;
                            m.formatting = t.formatting;
                            break;
                        }
                    }
                }
            }
        }

        public static MetricRangesElement getMetaData(string exerciseNameInConfiguration, string metricName)
        {
            var section = ConfigurationManager.GetSection(exerciseNameInConfiguration) as MetricRangesSection;
            var ranges = section.MetricRangesCollection;
            if (ranges != null)
                foreach (var r in ranges)
                {
                    var t = r as MetricRangesElement;
                    if (t.name == metricName)
                        return t;
                }
            return null;
        }
    }

    public class MetricRangesSection : ConfigurationSection
    {
        [ConfigurationProperty("MetricRangesCollection")]
        [ConfigurationCollection(typeof(MetricRangesCollection), AddItemName = "add")]
        public MetricRangesCollection MetricRangesCollection
        {
            get { return this["MetricRangesCollection"] as MetricRangesCollection; }
        }
    }

    public class MetricRangesCollection : ConfigurationElementCollection
    {
        public void add(MetricRangesElement element)
        {
            this.BaseAdd(element);
        }
        
        protected override ConfigurationElement CreateNewElement()
        {
            var e = new MetricRangesElement();
            return e;
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return((MetricRangesElement)element).name;
        }

    }

    public class MetricRangesElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string name
        {
            get { return (string)this["name"];}
            set { this["name"] = value;}
        }


        [ConfigurationProperty("lowerTypicalRange", DefaultValue = -1.0)]
        public double lowerTypicalRange
        {
            get { return (double)this["lowerTypicalRange"]; }
            set { this["lowerTypicalRange"] = value; }
        }

        [ConfigurationProperty("higherTypicalRange", DefaultValue = -1.0)]
        public double higherTypicalRange
        {
            get { return (double)this["higherTypicalRange"]; }
            set { this["higherTypicalRange"] = value; }
        }

        [ConfigurationProperty("preferedRange", DefaultValue = Epreference.noPreference)]
        public Epreference preferedRange
        {
            get { return (Epreference)this["preferedRange"]; }
            set { this["preferedRange"] = value; }
        }

        [ConfigurationProperty("description")]
        public string description
        {
            get { return (string)this["description"]; }
            set { this["description"] = value; }
        }

        [ConfigurationProperty("formatting", DefaultValue="N4")]
        public string formatting
        {
            get { return (string)this["formatting"]; }
            set { this["formatting"] = value; }
        }

    }
}
