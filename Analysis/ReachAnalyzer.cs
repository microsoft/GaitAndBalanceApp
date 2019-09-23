using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GaitAndBalanceApp.Analysis
{
    class ReachAnalyzer : Analyzer
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

            var startValueExtreamZ = trajectory.extreams.Take(10).Min(p => p.minZ);
            var reachValueExtreamZ = trajectory.extreams.Min(p => p.minZ);
            
            //Time domain stats
            metrics.Add(new Metric
            {
                name = "Reach distance",
                value = startValueExtreamZ - reachValueExtreamZ
            });

            metrics.Add(new Metric
            {
                name = "Ventral sway",
                value = trajectory.ventralDeviation()
            });
			
            return metrics;
        }
    }
}

