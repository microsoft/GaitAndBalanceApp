using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace GaitAndBalanceApp.Analysis
{
    public abstract class Analyzer
    {
        public string name {get; set;}

        public enum ESegmentType { Walking, Standing, Turning, WalkingAwayFromSensor, SittingDown, WalkingToSensor, StandingUp };

        public List<Metric> analyzeAndAnnotate(string inputFileName)
        {
            Logger.log("Analyzer: analyzing {0}", inputFileName);
            try {
                GaitFile file;
                var trajectory = readTrajectory(inputFileName, out file);
                var segments = segmentTrajectory(trajectory);
                var metrics = getMetrics(segments, trajectory);
                var sharedMetrics = analyzeSharedMetrics(segments, trajectory);
                metrics.AddRange(sharedMetrics);
                Metrics.addMetaData(metrics, name);
                Metrics.addMetaData(metrics, "SHARED");

                Logger.log("Analyzer: done analyzing {0}", inputFileName);
                return metrics;
            }
            catch (Exception e)
            {
                Logger.log("Analyzer: caught exeception {0}", e);
                return null;
            }
        }

        private Trajectory readTrajectory(string inputFileName, out GaitFile file)
        {
            var input = (EinputMode)Enum.Parse(typeof(EinputMode), ConfigurationManager.AppSettings["inputMode"], true);
            var projection = (EprojectionMode)Enum.Parse(typeof(EprojectionMode), ConfigurationManager.AppSettings["projectionMode"], true);
            file = new GaitFile();
            Trajectory trajectory = new Trajectory();
            Frame fr;

            file.ReadFile(inputFileName);
            trajectory.points = new List<Point>();
            long frameNum = -1;
            while ((fr = file.GetNextFrame(frameNum)) != null)
            {
                trajectory.add(fr);
                frameNum = fr.FrameNumber;
            }
            if (projection == EprojectionMode.ground)
                trajectory.rotateTrajectory();
            trajectory.samplingRate = (input == EinputMode.wii) ? 100 : 30;
            return trajectory;
        }

        private List<Metric> analyzeSharedMetrics(List<Tuple<Trajectory, ESegmentType>>  segments, Trajectory trajectory)
        {
            List<Metric> metrics = new List<Metric>();

            //Time domain stats
            metrics.Add(new Metric
            {
                name = "Duration",
                value = trajectory.duratation()
            });

            metrics.Add(new Metric
            {
                name = "Frames Per Second",
                value = segments.Sum(s => s.Item1.points.Count) / segments.Sum(s => s.Item1.duratation())
            });

            return metrics;

        }

        public abstract List<Metric> getMetrics(List<Tuple<Trajectory, ESegmentType>> segments, Trajectory trajectory);

        public abstract List<Tuple<Trajectory, ESegmentType>> segmentTrajectory(Trajectory trajectory);

        public Trajectory normalizeTrajectory(Trajectory trajectory, ESegmentType segmentType)
        {
            switch (segmentType)
            {
                case ESegmentType.Walking: return trajectory.removeLinearPath();
                default: return trajectory;
            }
        }
    }
}
