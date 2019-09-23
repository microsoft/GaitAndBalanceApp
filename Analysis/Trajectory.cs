using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using ShoNS.Array;

namespace GaitAndBalanceApp.Analysis
{
    public struct Point
    {
        public double x, z;
        public long timeStamp;

        public Point(double x, double z, long timeStamp)
        {
            this.x = x;
            this.z = z;
            this.timeStamp = timeStamp;
        }
    };


    public class Trajectory
    {
        public List<Point> points = new List<Point>();
        public List<Point> slopes = new List<Point>();
        public List<Point> leans = new List<Point>();
        public List<Extreams> extreams = new List<Extreams>();
        public int samplingRate;
        EinputMode input;
        EprojectionMode projection;
        bool useKinectGround;
        double totalXX = 0, totalXZ = 0, totalZZ = 0;

        public Trajectory(EinputMode input, EprojectionMode projection)
        {
            this.input = input;
            this.projection = projection;
            useKinectGround = !(input == EinputMode.silhouetteLine || input == EinputMode.silhouetteMean);
        }

        public Trajectory()
        {
            input = (EinputMode)Enum.Parse(typeof(EinputMode), ConfigurationManager.AppSettings["inputMode"], true);
            projection = (EprojectionMode)Enum.Parse(typeof(EprojectionMode), ConfigurationManager.AppSettings["projectionMode"], true);
            useKinectGround = !(input == EinputMode.silhouetteLine || input == EinputMode.silhouetteMean);

        }

        public bool add(Frame fr)
        {
            float x, z, slopeX, slopeZ;
            Extreams extreamValues;


            if (fr.getCOM(out x, out z, out slopeX, out slopeZ, out extreamValues, input, projection))
            {
                if (Double.IsNaN(x) || Double.IsInfinity(x)) return false;
                if (Double.IsNaN(z) || Double.IsInfinity(z)) return false;

                points.Add(new Point(x, z, fr.FrameTime));
                slopes.Add(new Point(slopeX, slopeZ, fr.FrameTime));
                leans.Add(new Point(fr.LeanX, fr.LeanY, fr.FrameTime));
                extreams.Add(extreamValues);
                var cov = Frame.projectCovariance(fr, useKinectGround);
                totalXX += cov[0, 0];
                totalXZ += cov[0, 2];
                totalZZ += cov[2, 2];
                return true;
            }
            return false;

        }

        public void add(Point p, Point slope, Point? lean = null)
        {
            points.Add(p);
            slopes.Add(slope);
			if (lean != null)
			{
				leans.Add((Point)lean);
			}

        }


        public void add(Point p)
        {
            points.Add(p);
        }

        public void rotateTrajectory()
        {
            using (var cov = DoubleArray.From(new Double[,] {
                {totalXX, totalXZ},
                {totalXZ, totalZZ}}))
            {

                using (Eigen eigen = new Eigen(cov))
                {
                    var V = eigen.V as DoubleArray;
                    var t = V * V.Transpose();
                    var u = V.Transpose() * V;
                    var newPoints = points.Select(p => new Point((p.x * V[0, 0] + p.z * V[0, 1]), (p.x * V[1, 0] + p.z * V[1, 1]), p.timeStamp)).ToList();
                    points = newPoints;
                }
            }
        }

        public void filter()
        {
            int samplingRate = (input == EinputMode.wii) ? 100 : 30;
            ButterworthLowPassFilter xFilter = new ButterworthLowPassFilter(samplingRate);
            ButterworthLowPassFilter zFilter = new ButterworthLowPassFilter(samplingRate);


            var newPoints = points.Where(p => !(Double.IsInfinity(p.x) || Double.IsNaN(p.x) | Double.IsInfinity(p.z) || Double.IsNaN(p.z))).Select(p => new Point(xFilter.Filter(p.x), zFilter.Filter(p.z), p.timeStamp)).ToList();
            points = newPoints;

        }

        public Point mean()
        {
            if (points == null || !points.Any()) return new Point() { x = 0, z = 0, timeStamp = -1 };

            double sumX = 0, sumZ = 0;
            int length = 0;
            foreach (var p in points)
            {
                if (Double.IsInfinity(p.x) || Double.IsNaN(p.x)) continue;
                if (Double.IsInfinity(p.z) || Double.IsNaN(p.z)) continue;
                sumX += p.x;
                sumZ += p.z;
                length++;
            }
            if (length <= 0) length = 1;
            return new Point(sumX / length, sumZ / length, 0);
        }

        public DoubleArray covariance()
        {
            var center = mean();
            double sumXX = 0, sumXZ = 0, sumZZ = 0;
            int length = 0;
            foreach (var p in points)
            {
                if (Double.IsInfinity(p.x) || Double.IsNaN(p.x)) continue;
                if (Double.IsInfinity(p.z) || Double.IsNaN(p.z)) continue;
                var deltaX = p.x - center.x;
                var deltaZ = p.z - center.z;
                sumXX += deltaX * deltaX;
                sumXZ += deltaX * deltaZ;
                sumZZ += deltaZ * deltaZ;
                length++;
            }
            sumXX /= (length - 1);
            sumXZ /= (length - 1);
            sumZZ /= (length - 1);

            double[,] covMatrix = new double[2, 2];
            covMatrix[0, 0] = sumXX;
            covMatrix[0, 1] = sumXZ;
            covMatrix[1, 0] = sumXZ;
            covMatrix[1, 1] = sumZZ;

            return DoubleArray.From(covMatrix);
        }

        public Trajectory removeLinearPath()
        {
            if (points == null || !points.Any()) return this;
            var tOffset = points[0].timeStamp;
            double xt = 0, zt = 0, tt = 0, meanX = 0, meanT = 0, meanZ = 0;
            foreach (var p in points)
            {
                var t = p.timeStamp - tOffset;
                xt += t * p.x;
                zt += t * p.z;
                tt += t * t;
                meanX += p.x;
                meanZ += p.z;
                meanT += t;
            }
            var n = points.Count;
            xt /= n;
            zt /= n;
            tt /= n;
            meanX /= n;
            meanT /= n;
            meanZ /= n;
            double factor = 0;
            if (tt - meanT * meanT != 0) factor = 1.0 / (tt - meanT * meanT);
            double slopeX = (xt - meanX * meanT) * factor;
            double slopeZ = (zt - meanZ * meanT) * factor;
            double offsetX = meanX - slopeX * meanT;
            double offsetZ = meanZ - slopeZ * meanT;

            Trajectory segment = new Trajectory();
            segment.samplingRate = samplingRate;
            foreach (var p in points)
            {
                Point p1 = new Point();
                var t = p.timeStamp - tOffset;
                p1.timeStamp = p.timeStamp;
                p1.x = p.x - offsetX - slopeX * t;
                p1.z = p.z - offsetZ - slopeZ * t;
                segment.add(p1);
            }
            return (segment);
        }

        /// <summary>
        /// computes the duration it took to complete the trajectory
        /// </summary>
        /// <returns>the duration (time) in seconds</returns>
        public double duratation()
        {
            if (points == null || !points.Any()) return 0;
            var first = points[0].timeStamp;
            var last = points.Last().timeStamp;
            return (last - first) / 1000.0 + 1.0 / samplingRate; // the last component is to compensate for half frame on the beginning and end of the trajectory
        }
        public double progressLength()
        {
            double deltaX = points.Last().x - points[0].x;
            double deltaZ = points.Last().z - points[0].z;
            double distance = Math.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
            return distance;

        }
        public double efficiency()
        {
            if (points == null || !points.Any()) return 0;
            double COMdistance = pathLength();
            double distance = progressLength();
            return distance / COMdistance;
        }

        public double lateralDeviation()
        {
            if (points == null || !points.Any()) return 0;

            var mx = points.Select(p => p.x).Median();
            return points.Select(p => Math.Abs(p.x - mx)).Median();
        }

        public double ventralDeviation()
        {
            if (points == null || !points.Any()) return 0;

            var mz = points.Select(p => p.z).Median();
            return points.Select(p => Math.Abs(p.z - mz)).Median();
        }

        public double totalDeviation()
        {
            if (points == null || !points.Any()) return 0;
            var mx = points.Select(p => p.x).Median();
            var mz = points.Select(p => p.z).Median();
            return points.Select(p => Math.Sqrt((p.x - mx) * (p.x - mx) + (p.z - mz) * (p.z - mz))).Median();
        }
        public double pathLength()
        {
            if (points == null || !points.Any()) return 0;
            Point last = points.First();
            double path = 0;
            foreach (Point p in points)
            {
                var deltaX = p.x - last.x;
                var deltaZ = p.z - last.z;
                double distBetweenPoints = Math.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
                path += distBetweenPoints;
                last = p;
            }
            return path;
        }

        public double RMS()
        {
            var center = mean();
            var distanceSquared = points.Select(p => (p.x - center.x) * (p.x - center.x) + (p.z - center.z) * (p.z - center.z)).Average();
            return Math.Sqrt(distanceSquared);
        }


    }

    public class ButterworthLowPassFilter
    {
        //filter fc = 3.5hz, fs = 30hz (kinect) or 100hz (wii)

        private const int LowPassOrder = 5; //actually 4th order

        private double[] inputValueModifier;
        private double[] outputValueModifier;
        private double[] inputValue = null;
        private double[] outputValue = null;
        private int valuePosition = 0;

        public ButterworthLowPassFilter(int fs)
        {
            if (fs == 30) init30Hz();
            else if (fs == 100) init100Hz();
            else throw new Exception(string.Format("filter for fs: {0} not implemented.", fs));
        }

        private void init30Hz()
        {
            inputValueModifier = new double[LowPassOrder];
            inputValueModifier[0] = 0.008114715950794;
            inputValueModifier[1] = 0.032458863803176;
            inputValueModifier[2] = 0.048688295704763;
            inputValueModifier[3] = 0.032458863803176;
            inputValueModifier[4] = 0.008114715950794;

            outputValueModifier = new double[LowPassOrder];
            outputValueModifier[0] = 1.0;
            outputValueModifier[1] = -2.101775724168813;
            outputValueModifier[2] = 1.915053121664871;
            outputValueModifier[3] = -0.823185547634419;
            outputValueModifier[4] = 0.139743605351063;
        }

        private void init100Hz()
        {
            inputValueModifier = new double[LowPassOrder];
            inputValueModifier[0] = 0.000111381075595079;
            inputValueModifier[1] = 0.000445524302380318;
            inputValueModifier[2] = 0.000668286453570477;
            inputValueModifier[3] = 0.000445524302380318;
            inputValueModifier[4] = 0.000111381075595079;

            outputValueModifier = new double[LowPassOrder];
            outputValueModifier[0] = 1.0;
            outputValueModifier[1] = -3.42589475619027;
            outputValueModifier[2] = 4.43679365429833;
            outputValueModifier[3] = -2.57124826103676;
            outputValueModifier[4] = 0.562131460138223;
        }


        public List<double> Filter(List<double> dataList)
        {
            inputValue = null;
            outputValue = null;
            valuePosition = 0;

            List<double> filteredList = new List<double>();
            for (int i = 0; i < dataList.Count; i++)
            {
                filteredList.Add(this.Filter(dataList[i]));
            }
            return filteredList;
        }

        public double Filter(double inputValue)
        {
            if (this.inputValue == null && this.outputValue == null)
            {
                this.inputValue = new double[LowPassOrder];
                this.outputValue = new double[LowPassOrder];

                valuePosition = -1;

                for (int i = 0; i < LowPassOrder; i++)
                {
                    this.inputValue[i] = inputValue;
                    this.outputValue[i] = inputValue;
                }

                return inputValue;
            }
            else if (this.inputValue != null && this.outputValue != null)
            {
                valuePosition = IncrementLowOrderPosition(valuePosition);

                this.inputValue[valuePosition] = inputValue;
                this.outputValue[valuePosition] = 0;

                int j = valuePosition;

                for (int i = 0; i < LowPassOrder; i++)
                {
                    this.outputValue[valuePosition] += inputValueModifier[i] * this.inputValue[j] -
                        outputValueModifier[i] * this.outputValue[j];

                    j = DecrementLowOrderPosition(j);
                }

                return this.outputValue[valuePosition];
            }
            else
            {
                throw new Exception("Both inputValue and outputValue should either be null or not null.  This should never be thrown.");
            }
        }

        private int DecrementLowOrderPosition(int j)
        {
            if (--j < 0)
            {
                j += LowPassOrder;
            }
            return j;
        }

        private int IncrementLowOrderPosition(int position)
        {
            return ((position + 1) % LowPassOrder);
        }
    }


}
