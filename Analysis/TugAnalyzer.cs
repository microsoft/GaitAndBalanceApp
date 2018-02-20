using System;
using System.Collections.Generic;
using System.Configuration;

namespace GaitAndBalanceApp.Analysis
{
    public class TugAnalyzer : Analyzer
    {
        enum Estate { Sit, Stand, Turning, GoingAway, GettingCloser, Sitting, Done };

        public override List<Tuple<Trajectory, ESegmentType>> segmentTrajectory(Trajectory trajectory)
        {
            var segments = new List<Tuple<Trajectory, ESegmentType>>();

            var closeThreshold = 2.5;
            var farThreshold = 4.5;
            var hysteresis = 0.05;
            var sittingZSlope = 0.4;
            Double.TryParse(ConfigurationManager.AppSettings["TUGcloseThreshold"], out closeThreshold);
            Double.TryParse(ConfigurationManager.AppSettings["TUGfarThreshold"], out farThreshold);
            Double.TryParse(ConfigurationManager.AppSettings["TUGhysteresis"], out hysteresis);
            double sittingZAngle;
            if (Double.TryParse(ConfigurationManager.AppSettings["TUGsittingZAngle"], out sittingZAngle))
                sittingZSlope = Math.Sin(sittingZAngle * Math.PI / 180);
            Estate state = Estate.Stand;
            Trajectory t = new Trajectory();
            t.samplingRate = trajectory.samplingRate;
            long trajectoryLength = trajectory.points.Count;

            for (int i = 0; i < trajectoryLength; i++)
            {
                var p = trajectory.points[i];
                var slope = trajectory.slopes[i];

                if (p.z < farThreshold && state == Estate.Stand)
                {
                    state = Estate.GettingCloser;
                    segments.Add(new Tuple<Trajectory, ESegmentType>(t, ESegmentType.StandingUp));
                    t = new Trajectory();
                    t.samplingRate = trajectory.samplingRate;
                }
                if (p.z < closeThreshold - hysteresis && state == Estate.GettingCloser)
                {
                    state = Estate.Turning;
                    segments.Add(new Tuple<Trajectory, ESegmentType>(t, ESegmentType.WalkingToSensor));
                    t = new Trajectory();
                    t.samplingRate = trajectory.samplingRate;
                }
                if (p.z > closeThreshold + hysteresis && state == Estate.Turning)
                {
                    state = Estate.GoingAway;
                    segments.Add(new Tuple<Trajectory, ESegmentType>(t, ESegmentType.Turning));
                    t = new Trajectory();
                    t.samplingRate = trajectory.samplingRate;
                }
                if (p.z > farThreshold && state == Estate.GoingAway)
                {
                    state = Estate.Sitting;
                    segments.Add(new Tuple<Trajectory, ESegmentType>(t, ESegmentType.WalkingAwayFromSensor));
                    t = new Trajectory();
                    t.samplingRate = trajectory.samplingRate;
                }
                if (state == Estate.Sitting)
                {
                    if (slope.z > sittingZSlope)
                    {
                        state = Estate.Done;
                        segments.Add(new Tuple<Trajectory, ESegmentType>(t, ESegmentType.SittingDown));
                        t = new Trajectory();
                        t.samplingRate = trajectory.samplingRate;
                    }
                }

                t.add(p, slope);
            }
            if (state == Estate.Sitting)
            {
                segments.Add(new Tuple<Trajectory, ESegmentType>(t, ESegmentType.SittingDown));
                t = new Trajectory();
                t.samplingRate = trajectory.samplingRate;

            }
            return segments;
        }


        public override List<Metric> getMetrics(List<Tuple<Trajectory, ESegmentType>> segments, Trajectory trajectory)
        {
            List<Metric> metrics = new List<Metric>();
            double totalDuration = 0.0;
            foreach (var segment in segments)
            {
                var duration = segment.Item1.duratation();
                totalDuration += duration;
                switch (segment.Item2)
                {
                    case ESegmentType.StandingUp: metrics.Add(new Metric { name = "Time to stand", value = duration }); break;
                    case ESegmentType.WalkingToSensor: metrics.Add(new Metric { name = "Time to walk before turning", value = duration }); break;
                    case ESegmentType.Turning: metrics.Add(new Metric { name = "Time to turn", value = duration }); break;
                    case ESegmentType.WalkingAwayFromSensor: metrics.Add(new Metric { name = "Time to walk after turning", value = duration }); break;
                    case ESegmentType.SittingDown: metrics.Add(new Metric { name = "Time to sit", value = duration }); break;
                    default: throw new Exception("unexpected segment type " + Enum.GetName(typeof(ESegmentType), segment.Item2));
                }
            }
            metrics.Add(new Metric
            {
                name = "Total",
                value = totalDuration
            });

            return metrics;
        }
    }
}
