using System;
using System.Collections.Generic;
using System.Linq;
using ShoNS.Array;
using ShoNS.SignalProc;

namespace GaitAndBalanceApp.Analysis
{
    public class FrequencyStats
    {
        public double totalPower, F50, F95, centroidFreq, FreqDispersion;

        public DoubleArray Pxx, Fx;
        public ComplexArray FFTResult;
        private DoubleArray dataXList, dataZList;
        private ComplexArray signal;

        public FrequencyStats(Trajectory trajectory, bool useSlope = false)
        {
            var xList = (useSlope) ? trajectory.slopes.Select(p => p.x).ToArray() : trajectory.points.Select(p => p.x).ToArray();
            var zList = (useSlope) ? trajectory.slopes.Select(p => p.z).ToArray() : trajectory.points.Select(p => p.z).ToArray();
            dataXList = DoubleArray.From(xList);
            dataZList = DoubleArray.From(zList);
            signal = getSignal(dataXList, dataZList);
            getPxxFx(signal, trajectory.samplingRate, out Pxx, out Fx);

            //Discard values before 0.05 Hz (high pass filter)
            highPassFilter(Pxx, Fx, 0.05, out Pxx, out Fx);

            totalPower = Pxx.Sum();
            F50 = getFn(Pxx, Fx, 0.5);
            F95 = getFn(Pxx, Fx, 0.95);
            centroidFreq = getCentroidFreq(Pxx, Fx);
            FreqDispersion = getFreqDispersion(Pxx, Fx);
        }

        private void highPassFilter(DoubleArray Pxx, DoubleArray Fx, double cutOffFreq, out DoubleArray newPxx, out DoubleArray newFx)
        {
            //RemoveAt() is not implemented for DoubleArray, so convert to List and do the removal and convert back.
            List<double> PxxList = Pxx.ToList();
            List<double> FxList = Fx.ToList();

            while (FxList.First() < cutOffFreq) {
                FxList.RemoveAt(0);
                PxxList.RemoveAt(0);
            }
            newPxx = DoubleArray.From(PxxList);
            newFx = DoubleArray.From(FxList);
        }

        private double getFn(DoubleArray Pxx, DoubleArray Fx, double n)
        {
            //returns Fn = the frequency below which n percent of total power is present
            totalPower = Pxx.Sum();
            double curSumPower = 0;
            int i;
            for (i = 0; i < Pxx.Count(); i++)
            {
                curSumPower += Pxx[i];
                if (curSumPower / totalPower > n) break;
            }
            return (i + 1 < Fx.Length) ? Fx[i + 1] : Fx[Fx.Length - 1];
        }

        private ComplexArray getSignal(DoubleArray xList, DoubleArray zList)
        {
            return ComplexArray.From(xList, zList);
        }

        private double getCentroidFreq(DoubleArray Pxx, DoubleArray Fx)
        {
            double u1 = getSumOfFreqkTimesPower(Pxx, Fx, 1);
            double u0 = getSumOfFreqkTimesPower(Pxx, Fx, 0);
            return Math.Sqrt(u1 / u0);
        }

        private double getSumOfFreqkTimesPower(DoubleArray Pxx, DoubleArray Fx, int k)
        {
            return Enumerable.Range(0, Pxx.Count).Select(index => Math.Pow(Fx[index], k) * Pxx[index]).Sum();
        }

        private double getFreqDispersion(DoubleArray Pxx, DoubleArray Fx)
        {
            double u0 = getSumOfFreqkTimesPower(Pxx, Fx, 0);
            double u1 = getSumOfFreqkTimesPower(Pxx, Fx, 1);
            double u2 = getSumOfFreqkTimesPower(Pxx, Fx, 2);
            
            return Math.Sqrt(1 - (u1*u1) / (u0 * u2));
        }

        private void getPxxFx(ComplexArray signal, double samplingRate, out DoubleArray Pxx, out DoubleArray Fx)
        {
            //Implements Matlab's PWelch. Same as [Pxx, Fx] = pwelch(x, w, 0, nfft, Fs), where x = signal, w is calculated below, nfft = signal.count, Fs = samplingRate (hz)
            int Nx = signal.Count;
            DoubleArray w = DoubleArray.From(hamming(Nx));
            double std = w.Std();
            double mean = w.Mean();
            ComplexArray xw = signal.ElementMultiply(w);
            int nfft = Nx;
            ComplexArray X = FFTComp.FFTComplex(xw);
            DoubleArray absX = X.Abs();
            DoubleArray mx = absX.ElementMultiply(absX);

            DoubleArray wt = w.Transpose();
            double res = w.Multiply(wt)[0];
            mx = mx.Divide(res);
            int numUniquePts = nfft / 2 + 1;
            Slice slice = new Slice(0, numUniquePts - 1);
            mx = mx.GetSlice(slice);
            double temp1 = mx[0];
            double temp2 = mx[mx.Count - 1];
            mx = mx.Multiply(2);
            mx[0] = temp1;
            mx[mx.Count - 1] = temp2;

            Pxx = mx.Divide(samplingRate);
            Fx = getFx(numUniquePts, samplingRate, nfft);
            w.Dispose();
            absX.Dispose();
            mx.Dispose();
            wt.Dispose();
        }

        private DoubleArray getFx(int numPts, double Fs, double nfft)
        {
            List<double> Fx = new List<double>();
            for (int i = 0; i < numPts; i++)
            {
                Fx.Add(i * Fs / nfft);
            }
            return DoubleArray.From(Fx);
        }

        private List<double> hamming(int signalLength)
        {
            List<double> w = new List<double>();
            int N = signalLength - 1;
            if (N == 0)
            {
                throw new Exception("signalLength must be > 1");
            }
            for (int i = 0; i < signalLength; i++)
            {
                double wn = 0.54 - 0.46 * Math.Cos(2 * Math.PI * (i * 1.0 / N)); 
                w.Add(wn);
            }
            return w;
        }
    }
}
