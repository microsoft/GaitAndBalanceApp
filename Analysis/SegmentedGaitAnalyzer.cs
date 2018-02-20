using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;

namespace GaitAndBalanceApp.Analysis
{
    public class SegmentedGaitAnalyzer : Analyzer
    {
        public override List<Tuple<Trajectory, ESegmentType>> segmentTrajectory(Trajectory trajectory)
        {
            List<Tuple<Trajectory, ESegmentType>> segments = new List<Tuple<Trajectory, ESegmentType>>();
            double closeThreshold = 1.5;
            double farThreshold = 7.5;

            Double.TryParse(ConfigurationManager.AppSettings["SegmentedGaitFarThreshold"], out farThreshold);
            Double.TryParse(ConfigurationManager.AppSettings["SegmentedGaitCloseThreshold"], out closeThreshold);

            Trajectory t = null;
            long lastTimeStamp = -1000;
            int pointNumber = -1;
            foreach (var p in trajectory.points)
            {
                pointNumber++;
                if (p.z < closeThreshold) continue;
                if (p.z > farThreshold) continue;
                if (p.timeStamp - lastTimeStamp > 1000)
                {
                    if ((t != null) && (t.duratation() > 1.0))
                        segments.Add(new Tuple<Trajectory, ESegmentType>(t, ESegmentType.Walking));
                    t = new Trajectory();
                    t.samplingRate = trajectory.samplingRate;
                }
                t.add(p, trajectory.slopes[pointNumber]);
                lastTimeStamp = p.timeStamp;
            }
            if ((t != null) && (t.duratation() > 1.0))
                segments.Add(new Tuple<Trajectory, ESegmentType>(t, ESegmentType.Walking));

            return segments;
        }

        public override List<Metric> getMetrics(List<Tuple<Trajectory, ESegmentType>> anotatedSegments, Trajectory trajectory)
        {
            List<Metric> metrics = new List<Metric>();
            List<Trajectory> segments = anotatedSegments.Select(s => s.Item1).ToList();
            metrics.Add(new Metric
            {
                name = "Segments",
                value = segments.Count
            });
            int segmentCounter = 0;
            foreach (var segment in segments)
            {
                segmentCounter++;
                string segmentString = String.Format("#{0,5}", segmentCounter);
                metrics.Add(new Metric
                {
                    name = "Walk time" + segmentString,
                    value = segment.duratation()
                });
                metrics.Add(new Metric
                {
                    name = "COM distance" + segmentString,
                    value = segment.pathLength()
                });

                metrics.Add(new Metric
                {
                    name = "Velocity" + segmentString,
                    value = segment.pathLength() / segment.duratation()
                });
                metrics.Add(new Metric
                {
                    name = "Walking efficiency" + segmentString,
                    value = segment.efficiency()
                });

                var normalizedWalkingTrajectorie = normalizeTrajectory(segment, ESegmentType.Walking);
                metrics.Add(new Metric
                {
                    name = "Lateral sway" + segmentString,
                    value = normalizedWalkingTrajectorie.lateralDeviation()
                });
                metrics.Add(new Metric
                {
                    name = "Temporal sway" + segmentString,
                    value = normalizedWalkingTrajectorie.ventralDeviation()
                });
                metrics.Add(new Metric
                {
                    name = "Total sway" + segmentString,
                    value = normalizedWalkingTrajectorie.totalDeviation()
                });
                metrics.Add(new Metric
                {
                    name = "RMS" + segmentString,
                    value = normalizedWalkingTrajectorie.RMS()
                });
                var ellipse = new Ellipse(segment);
                metrics.Add(new Metric
                {
                    name = "Area" + segmentString,
                    value = ellipse.area
                });
                metrics.Add(new Metric
                {
                    name = "Range A" + segmentString,
                    value = ellipse.a * 2
                });
                metrics.Add(new Metric
                {
                    name = "Range B" + segmentString,
                    value = ellipse.b * 2
                });
                //var freqMetric = new FrequencyStats(segment, true);
                ////var freqMetric = new FrequencyStats(normalizedWalkingTrajectorie);
                //metrics.Add(new Metric
                //{
                //    name = "PWR" + segmentString,
                //    value = freqMetric.totalPower
                //});
                //metrics.Add(new Metric
                //{
                //    name = "F50" + segmentString,
                //    value = freqMetric.F50
                //});

                //metrics.Add(new Metric
                //{
                //    name = "F95" + segmentString,
                //    value = freqMetric.F95
                //});

                //metrics.Add(new Metric
                //{
                //    name = "Centroid Freq" + segmentString,
                //    value = freqMetric.centroidFreq
                //});

                //metrics.Add(new Metric
                //{
                //    name = "Freq Dispersion" + segmentString,
                //    value = freqMetric.FreqDispersion
                //});
                var fftStats = new FFTStats(segment, FFTStats.EDirection.Z, true);
                var stepFreq = fftStats.getPeekFrequency();
                metrics.Add(new Metric
                {
                    name = "step duration" + segmentString,
                    value = 1 / stepFreq
                });
                metrics.Add(new Metric
                {
                    name = "step length" + segmentString,
                    value = segment.progressLength() / (stepFreq * segment.duratation())
                });
            }
            return metrics; 
        }
    }
}
