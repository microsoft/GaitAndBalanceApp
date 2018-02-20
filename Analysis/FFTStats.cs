using System;
using System.Collections.Generic;
using System.Linq;
using ShoNS.Array;
using ShoNS.SignalProc;

namespace GaitAndBalanceApp.Analysis
{
    public class FFTStats
    {
        public DoubleArray Pxx, Fx;
        public ComplexArray FFTResult;
        private DoubleArray dataXList, dataZList;
        private ComplexArray signal;
        private double duration;
        public enum EDirection { X, Z, XZ};
        public FFTStats(Trajectory trajectory, EDirection direction, bool useSlope)
        {
            var points = (useSlope) ? trajectory.slopes : trajectory.points;
            var xList = (direction == EDirection.Z) ? points.Select(p => p.z).ToArray() : points.Select(p => p.x).ToArray();
            var zList = (direction == EDirection.X) ? points.Select(p => p.x).ToArray() : points.Select(p => p.z).ToArray();
            dataXList = DoubleArray.From(xList);
            dataZList = DoubleArray.From(zList);
            signal = getSignal(dataXList, dataZList);
            duration = trajectory.duratation();
            getPxxFx(signal, out Pxx, out Fx);
        }

        public double getPeekFrequency()
        {
            double peek = 0;
            int index = 0;
            bool useHalf = false;

            for (int i = 3; i < Pxx.Length / 2; i++)
            {
                double p = smoothPower(i, false);
                if (p > peek)
                {
                    index = i;
                    useHalf = false;
                    peek = p;
                }
                p = smoothPower(i, true);
                if (p > peek)
                {
                    index = i;
                    useHalf = true;
                    peek = p;
                }
            }
            if (useHalf == false) return Fx[index];
            return 0.5 * (Fx[index] + Fx[index + 1]);
        }

        double smoothPower(int index, bool addHalf = false)
        {
            if (addHalf)
            {
                if (index == 0) return (Pxx[0]+Pxx[1])/2;
                return (Pxx[index - 1] + 2 * Pxx[index] + 2 * Pxx[index + 1] + Pxx[index + 2]) / 6;
            }
            else
            {
                if (index == 0) return Pxx[0];
                return (Pxx[index] + Pxx[index + 1] + Pxx[index - 1]) / 3;
            }

        }

        private ComplexArray getSignal(DoubleArray xList, DoubleArray zList)
        {
            return ComplexArray.From(xList, zList);
        }


        private void getPxxFx(ComplexArray signal, out DoubleArray Pxx, out DoubleArray Fx)
        {
            ComplexArray X = FFTComp.FFTComplex(signal);
            Pxx = X.Abs();
            Fx = getFx(signal.Length);
            X.Dispose();
        }

        private DoubleArray getFx(int numPts)
        {
            List<double> Fx = new List<double>();
            for (int i = 0; i < numPts; i++)
            {
                Fx.Add(i / duration);
            }
            return DoubleArray.From(Fx);
        }
    }
}
