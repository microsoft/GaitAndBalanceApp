using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace GaitAndBalanceApp.Analysis
{
    public class SwayAnalyzer : Analyzer
    {
        public override List<Tuple<Trajectory, ESegmentType>> segmentTrajectory(Trajectory trajectory)
        {
            var segments = new List<Tuple<Trajectory, ESegmentType>>();
            segments.Add(new Tuple<Trajectory, ESegmentType>(trajectory, ESegmentType.Standing));
            return segments;
        }

        public override List<Metric> getMetrics(List<Tuple<Trajectory, ESegmentType>> segments, Trajectory trajectory)
        {
            List<Metric> metrics = new List<Metric>();

            Ellipse ellipse = new Ellipse(trajectory);


            //Time domain stats
            metrics.Add(new Metric
            {
                name = "Lateral sway",
                value = trajectory.lateralDeviation()
            });
            metrics.Add(new Metric
            {
                name = "Ventral sway",
                value = trajectory.ventralDeviation()
            });

            metrics.Add(new Metric
            {
                name = "Median Dist",
                value = trajectory.totalDeviation()
            });

            metrics.Add(new Metric
            {
                name = "RMS",
                value = trajectory.RMS()
            });

            metrics.Add(new Metric
            {
                name = "Area",
                value = ellipse.area
            });

            metrics.Add(new Metric
            {
                name = "Range A",
                value = ellipse.a * 2
            });

            metrics.Add(new Metric
            {
                name = "Range B",
                value = ellipse.b * 2
            });

            metrics.Add(new Metric
            {
                name = "Median Lateral Angle",
                value = (Math.Asin(trajectory.slopes.Select(p => p.x).Median()) * 180 / Math.PI)
            });

            metrics.Add(new Metric
            {
                name = "Median Ventral Angle",
                value = (Math.Asin(trajectory.slopes.Select(p => p.z).Median()) * 180 / Math.PI)
            });

            //Frequency domain stats
            var freqMetrics = new FrequencyStats(trajectory);
            metrics.Add(new Metric
            {
                name = "PWR",
                value = freqMetrics.totalPower
            });

            metrics.Add(new Metric
            {
                name = "F50",
                value = freqMetrics.F50
            });

            metrics.Add(new Metric
            {
                name = "F95",
                value = freqMetrics.F95
            });

            metrics.Add(new Metric
            {
                name = "Centroid Freq",
                value = freqMetrics.centroidFreq
            });

            metrics.Add(new Metric
            {
                name = "Freq Dispersion",
                value = freqMetrics.FreqDispersion
            });
            return metrics;
        }
    }
}
