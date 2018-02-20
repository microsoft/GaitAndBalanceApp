using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;

namespace GaitAndBalanceApp.Analysis
{
    public class GaitAnalyzer : Analyzer
    {
        enum Estate { TurningClose, GoingAway, TurningFar, GettingCloser};

        public override List<Tuple<Trajectory, ESegmentType>> segmentTrajectory(Trajectory trajectory)
        {
            List<Tuple<Trajectory, ESegmentType>> segments = new List<Tuple<Trajectory, ESegmentType>>();
            var state = Estate.GoingAway;
            var closeThreshold = 2.5;
            var farThreshold = 6.5;
            var hysteresis = 0.05;
            Double.TryParse(ConfigurationManager.AppSettings["closeThreshold"], out closeThreshold);
            Double.TryParse(ConfigurationManager.AppSettings["farThreshold"], out farThreshold);
            Double.TryParse(ConfigurationManager.AppSettings["hysteresis"], out hysteresis);
            string initialState = ConfigurationManager.AppSettings["initialState"];
            switch (initialState)
            {
                case "GettingCloser": state = Estate.GettingCloser; break;
                case "GoingAway": state = Estate.GoingAway; break;
                case "TurningClose": state = Estate.TurningClose; break;
                case "TurningFar": state = Estate.TurningFar; break;
            }
            Trajectory t = new Trajectory();
            t.samplingRate = trajectory.samplingRate;
            int pointNumber = -1;
            foreach (var p in trajectory.points)
            {
                pointNumber++;
                if (p.z < closeThreshold - hysteresis && state == Estate.GettingCloser)
                {
                    state = Estate.TurningClose;
                    if (t.duratation() > 0) segments.Add(new Tuple<Trajectory, ESegmentType>(t, ESegmentType.Walking));
                    t = new Trajectory();
                    t.samplingRate = trajectory.samplingRate;
                }
                else if (p.z > farThreshold + hysteresis && state == Estate.GoingAway)
                {
                    state = Estate.TurningFar;
                    if (t.duratation() > 0) segments.Add(new Tuple<Trajectory, ESegmentType>(t, ESegmentType.Walking)); 
                    t = new Trajectory();
                    t.samplingRate = trajectory.samplingRate;
                }
                else if (p.z > closeThreshold + hysteresis && state == Estate.TurningClose)
                {
                    state = Estate.GoingAway;
                    if (t.duratation() > 0) segments.Add(new Tuple<Trajectory, ESegmentType>(t, ESegmentType.Turning)); 
                    t = new Trajectory();
                    t.samplingRate = trajectory.samplingRate;
                }
                else if (p.z < farThreshold - hysteresis && state == Estate.TurningFar)
                {
                    state = Estate.GettingCloser;
                    if (t.duratation() > 0) segments.Add(new Tuple<Trajectory, ESegmentType>(t, ESegmentType.Turning));
                    t = new Trajectory();
                    t.samplingRate = trajectory.samplingRate;
                }
                t.add(p, trajectory.slopes[pointNumber]);
            }
            return segments;
        }

        public override List<Metric> getMetrics(List<Tuple<Trajectory, ESegmentType>> segments, Trajectory trajectory)
        {
            List<Metric> metrics = new List<Metric>();
            var turningTrajectories = segments.Where(s => s.Item2 == ESegmentType.Turning).Select(s => s.Item1).ToList();
            var walkingTrajectories = segments.Where(s => s.Item2 == ESegmentType.Walking).Select(s => s.Item1).ToList();
            metrics.Add(new Metric
            {
                name = "Turns",
                value = turningTrajectories.Count
            });
            metrics.Add(new Metric
            {
                name = "Turn time",
                value = turningTrajectories.Select(trj => trj.duratation()).Median()
            });

            metrics.Add(new Metric
            {
                name = "Walks",
                value = walkingTrajectories.Count
            });
            metrics.Add(new Metric
            {
                name = "Walk time",
                value = walkingTrajectories.Select(trj => trj.duratation()).Median()
            });
            metrics.Add(new Metric
            {
                name = "COM distance",
                value = trajectory.pathLength()
            });
            if (!walkingTrajectories.Any()) return metrics;
            metrics.Add(new Metric
            {
                name = "Velocity",
                value = walkingTrajectories.Select(segment => segment.pathLength() / segment.duratation()).Median()
            });

            metrics.Add(new Metric
            {
                name = "Walking efficiency",
                value = walkingTrajectories.Select(trj => trj.efficiency()).Median()
            });

            var normalizedWalkingTrajectories = walkingTrajectories.Select(trj => normalizeTrajectory(trj, ESegmentType.Walking)).ToList();
            metrics.Add(new Metric
            {
                name = "Lateral sway",
                value = normalizedWalkingTrajectories.Select(trj => trj.lateralDeviation()).Median()
            });
            metrics.Add(new Metric
            {
                name = "Temporal sway",
                value = normalizedWalkingTrajectories.Select(trj => trj.ventralDeviation()).Median()
            });
            metrics.Add(new Metric
            {
                name = "Total sway",
                value = normalizedWalkingTrajectories.Select(trj => trj.totalDeviation()).Median()
            });
            metrics.Add(new Metric
            {
                name = "RMS",
                value = normalizedWalkingTrajectories.Select(trj => trj.RMS()).Median()
            });
            var ellipses = normalizedWalkingTrajectories.Select(trj => new Ellipse(trj)).ToList();
            metrics.Add(new Metric
            {
                name = "Area",
                value = ellipses.Select(e => e.area).Median()
            });
            metrics.Add(new Metric
            {
                name = "Range A",
                value = ellipses.Select(e => e.a * 2).Median()
            });
            metrics.Add(new Metric
            {
                name = "Range B",
                value = ellipses.Select(e => e.b * 2).Median()
            });
            var fftStats = walkingTrajectories.Select(trj => new FFTStats(trj, FFTStats.EDirection.Z, true)).ToList();
            metrics.Add(new Metric
            {
                name = "step duration",
                value = fftStats.Select(stat => 1.0 / stat.getPeekFrequency()).Median()
            });
            metrics.Add(new Metric
            {
                name = "step length",
                value = fftStats.Zip(walkingTrajectories, (stat, trj) => trj.progressLength() / (stat.getPeekFrequency() * trj.duratation())).Median()
            });


            //var freqMetrics = normalizedWalkingTrajectories.Select(trj => new FrequencyStats(trj)).ToList();
            //metrics.Add(new Metric
            //{
            //    name = "PWR",
            //    value = freqMetrics.Select(f => f.totalPower).Median()
            //});

            //metrics.Add(new Metric
            //{
            //    name = "F50",
            //    value = freqMetrics.Select(f => f.F50).Median()
            //});

            //metrics.Add(new Metric
            //{
            //    name = "F95",
            //    value = freqMetrics.Select(f => f.F95).Median()
            //});

            //metrics.Add(new Metric
            //{
            //    name = "Centroid Freq",
            //    value = freqMetrics.Select(f => f.centroidFreq).Median()
            //});

            //metrics.Add(new Metric
            //{
            //    name = "Freq Dispersion",
            //    value = freqMetrics.Select(f => f.FreqDispersion).Median()
            //});
            //double freq = metrics.Where(m => m.name == "Centroid Freq").First().value;
            //double velocity = metrics.Where(m => m.name == "Velocity").First().value;

            //metrics.Add(new Metric
            //{
            //    name = "Stirde Length",
            //    value = velocity / freq
            //});
            return metrics;

        }
    }
}
