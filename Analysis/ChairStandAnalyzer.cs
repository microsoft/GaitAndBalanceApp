using System;
using System.Collections.Generic;
using System.Configuration;

namespace GaitAndBalanceApp.Analysis
{
    public class ChairStandAnalyzer : Analyzer
    {
        enum Estate { Sitting, Standing, StandingUp, SittingDown };

        public override List<Tuple<Trajectory, ESegmentType>> segmentTrajectory(Trajectory trajectory)
        {
            var segments = new List<Tuple<Trajectory, ESegmentType>>();



            Double.TryParse(ConfigurationManager.AppSettings["CHAIRSTANDhysteresis"], out double hysteresis);

            Double.TryParse(ConfigurationManager.AppSettings["CHAIRSTANDsittingZAngle"], out double sittingZAngle) ;

			Double.TryParse(ConfigurationManager.AppSettings["CHAIRSTANDstandingZAngle"], out double standingZAngle) ;

			Double.TryParse(ConfigurationManager.AppSettings["CHAIRSTANDsittingDownZAngle"], out double sittingDownZAngle);

			Double.TryParse(ConfigurationManager.AppSettings["CHAIRSTANDstandingUpZAngle"], out double standingUpZAngle);

			Estate state = Estate.Sitting;
            Trajectory t = new Trajectory
            {
                samplingRate = trajectory.samplingRate
            };
            long trajectoryLength = trajectory.points.Count;

            for (int i = 0; i < trajectoryLength; i++)
            {
                var p = trajectory.points[i];
                var slope = trajectory.slopes[i];
				var lean = trajectory.leans[i];
				var slopeZ = Math.Asin(slope.z) * 180 / Math.PI;

				switch (state)
				{
					case Estate.Standing:
						if (slopeZ > sittingDownZAngle)
						{
							state = Estate.SittingDown;
							segments.Add(new Tuple<Trajectory, ESegmentType>(t, ESegmentType.Standing));
                            t = new Trajectory
                            {
                                samplingRate = trajectory.samplingRate
                            };
                        }
						break;

					case Estate.SittingDown:
						if (slopeZ > sittingZAngle)
						{
							state = Estate.Sitting;
							segments.Add(new Tuple<Trajectory, ESegmentType>(t, ESegmentType.SittingDown));
                            t = new Trajectory
                            {
                                samplingRate = trajectory.samplingRate
                            };
                        }
						break;

					case Estate.Sitting:
						if (slopeZ < standingUpZAngle)
						{
							state = Estate.StandingUp;
							segments.Add(new Tuple<Trajectory, ESegmentType>(t, ESegmentType.Sitting));
                            t = new Trajectory
                            {
                                samplingRate = trajectory.samplingRate
                            };
                        }
						break;

					case Estate.StandingUp:
						if (slopeZ < standingZAngle)
						{
							state = Estate.Standing;
							segments.Add(new Tuple<Trajectory, ESegmentType>(t, ESegmentType.StandingUp));
                            t = new Trajectory
                            {
                                samplingRate = trajectory.samplingRate
                            };
                        }
						break;

				}
				
				t.add(p, slope, lean);
            }
            if (state == Estate.SittingDown)
            {
                segments.Add(new Tuple<Trajectory, ESegmentType>(t, ESegmentType.SittingDown));
                t = new Trajectory
                {
                    samplingRate = trajectory.samplingRate
                };

            }
            return segments;
        }


        public override List<Metric> getMetrics(List<Tuple<Trajectory, ESegmentType>> segments, Trajectory trajectory)
        {
            List<Metric> metrics = new List<Metric>();
			List<double> timeToStandDuration = new List<double>();
			List<double> timeStandingDuration = new List<double>();
			List<double> timeToSitDuration = new List<double>();
			List<double> timeSittingDuration = new List<double>();
			List<double> timeTotalDuration = new List<double>();

			List<double> minSlopeZList = new List<double>();
			List<double> maxSlopeZList = new List<double>();


			double totalDuration = 0.0;


			foreach (var segment in segments)
            {
                var duration = segment.Item1.duratation();
				totalDuration += duration;
				var minSlopeZ = 0.0;
				var maxSlopeZ = 0.0;

				switch (segment.Item2)
				{
					case ESegmentType.StandingUp: timeToStandDuration.Add(duration);
						foreach (var slope in segment.Item1.leans)
						{
							if (maxSlopeZ < slope.z)
							{
								maxSlopeZ = slope.z;
							}
						}
						maxSlopeZList.Add(maxSlopeZ);
						break; 
					case ESegmentType.Sitting: timeSittingDuration.Add(duration);
						foreach (var slope in segment.Item1.leans)
						{
							if (minSlopeZ < slope.z)
							{
								minSlopeZ = slope.z;
							}
						}
						minSlopeZList.Add(minSlopeZ);
						break;
					case ESegmentType.Standing: timeStandingDuration.Add(duration);break;
					case ESegmentType.SittingDown: timeToSitDuration.Add(duration); break;
					default: throw new Exception("unexpected segment type " + Enum.GetName(typeof(ESegmentType), segment.Item2));
				}

            }


			metrics.Add(new Metric { name = "MedianTimeToStand", value = timeToStandDuration.Median() } );
			metrics.Add(new Metric { name = "MedianTimeStanding", value = timeStandingDuration.Median() });
			metrics.Add(new Metric { name = "MedianTimeToSit", value = timeToSitDuration.Median() });
			metrics.Add(new Metric { name = "MedianTimeSitting", value = timeSittingDuration.Median() });

			metrics.Add(new Metric { name = "MedianMinSlope", value = minSlopeZList.Median() });
			metrics.Add(new Metric { name = "MedianMaxSlope", value = maxSlopeZList.Median() });

			metrics.Add(new Metric { name = "NumberOfTimes", value = timeToSitDuration.Count });

			double totalDurationAvg = (totalDuration / timeStandingDuration.Count);
			metrics.Add(new Metric
			{
				name = "Total",
				value = totalDurationAvg
			});

            return metrics;
        }
    }
}
