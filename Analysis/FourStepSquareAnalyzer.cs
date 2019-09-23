using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;

namespace GaitAndBalanceApp.Analysis
{
	public class FourStepSquareAnalyzer : Analyzer
	{
		enum Estate { Square1, Square2, Square3, Square4, Done };

		public override List<Tuple<Trajectory, ESegmentType>> segmentTrajectory(Trajectory trajectory)
		{
			List<Tuple<Trajectory, ESegmentType>> segments = new List<Tuple<Trajectory, ESegmentType>>();
			var state = Estate.Square1;
            var histerthesis = 0.1;
            if (!Double.TryParse(ConfigurationManager.AppSettings["FSSThreshold"], out double threshold))
			{
				threshold = 2.0;
			}

            Trajectory t = new Trajectory
            {
                samplingRate = trajectory.samplingRate
            };
            long trajectoryLength = trajectory.points.Count;

			for (int i = 0; i < trajectoryLength; i++)
			{
				var p = trajectory.points[i];
				var slope = trajectory.slopes[i];

				Estate observedState = state;
				if (p.z > threshold && p.x > 0)
				{
					observedState = Estate.Square1;
				}
				if (p.z < threshold && p.x > 0)
				{
					observedState = Estate.Square2;
				}
				if (p.z < threshold && p.x < 0)
				{
					observedState = Estate.Square3;
				}
				if (p.z > threshold && p.x < 0)
				{
					observedState = Estate.Square4;
				}

				if (observedState != state)
				{
					if (Math.Abs(p.x) > histerthesis || (Math.Abs(p.z - threshold) > histerthesis))
					{
						segments.Add(new Tuple<Trajectory, ESegmentType>(t, GetSegmentType(state)));
                        t = new Trajectory
                        {
                            samplingRate = trajectory.samplingRate
                        };
                        state = observedState;
					}
				}
				t.add(p, slope);
			}

			if (t.points.Any())
			{
				segments.Add(new Tuple<Trajectory, ESegmentType>(t, GetSegmentType(state)));
                t = new Trajectory
                {
                    samplingRate = trajectory.samplingRate
                };
            }

			return segments;
		}

		private ESegmentType GetSegmentType(Estate state)
		{
			ESegmentType stateType;
			switch (state)
			{
				case Estate.Square1: stateType = ESegmentType.Square1; break;
				case Estate.Square2: stateType = ESegmentType.Square2; break;
				case Estate.Square3: stateType = ESegmentType.Square3; break;
				case Estate.Square4: stateType = ESegmentType.Square4; break;
				default: throw new Exception("unexpected segment type ");
			}
			return stateType;
		}

		public override List<Metric> getMetrics(List<Tuple<Trajectory, ESegmentType>> segments, Trajectory trajectory)
		{
			List<Metric> metrics = new List<Metric>();
			double totalDuration = 0.0;
			var walkingTrajectories = segments.Where(s => s.Item2 == ESegmentType.Square1
			|| s.Item2 == ESegmentType.Square2
			|| s.Item2 == ESegmentType.Square3
			|| s.Item2 == ESegmentType.Square4
		   ).Select(s => s.Item1).ToList();

			metrics.Add(new Metric { name = "Time walking forward to square 2", value = segments[0].Item1.duratation() });
			metrics.Add(new Metric { name = "Time walking right to square 3", value = segments[1].Item1.duratation() });
			metrics.Add(new Metric { name = "Time walking backwards to square 4", value = segments[2].Item1.duratation() });
			metrics.Add(new Metric { name = "Time walking left to square 1", value = segments[3].Item1.duratation() });
			metrics.Add(new Metric { name = "Time walking right to square 4", value = segments[4].Item1.duratation() });
			metrics.Add(new Metric { name = "Time walking forward to square 3", value = segments[5].Item1.duratation() });
			metrics.Add(new Metric { name = "Time walking left to square 2", value = segments[6].Item1.duratation() });
			metrics.Add(new Metric { name = "Time walking backwards to square 1", value = segments[7].Item1.duratation() });

			for (int i = 0; i < 8; i++)
			{
				totalDuration += segments[i].Item1.duratation();
			}

			metrics.Add(new Metric
			{
				name = "Total",
				value = totalDuration
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

			return metrics;
		}
	}
}
