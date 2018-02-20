using System;
using System.Collections.Generic;
using System.Linq;
using ShoNS.Array;

namespace GaitAndBalanceApp.Analysis
{
    public static class Statistics
    {
        public static List<double> getDiffList(List<double> list, List<double> referenceList)
        {
            if (list.Count != referenceList.Count) throw new Exception("Input lists are of different lengths");

            //Just returns a list that is the difference of the two input lists
            List<double> diffList = new List<double>();
            for (int i = 0; i < list.Count; i++)
            {
                diffList.Add(list[i] - referenceList[i]);
            }
            return diffList;
        }
        public static List<double> getDiffList(List<double> list, double referenceValue, bool absDiff = false)
        {
            //Just returns a list that is the difference of this list and an input, and option for diff to be abs
            List<double> diffList = new List<double>();
            for (int i = 0; i < list.Count; i++)
            {
                double diff = list[i] - referenceValue;
                if (absDiff) diff = Math.Abs(diff);
                diffList.Add(diff);
            }
            return diffList;
        }


        public static double Median(this IEnumerable<double> source)
        {
            if (!source.Any()) return Double.NaN;
            var sortedList = from number in source
                             orderby number
                             select number;

            int count = sortedList.Count();
            int itemIndex = count / 2;
            if (count % 2 == 0) // Even number of items. 
                return (sortedList.ElementAt(itemIndex) +
                        sortedList.ElementAt(itemIndex - 1)) / 2;

            // Odd number of items. 
            return sortedList.ElementAt(itemIndex);
        }



        public static double getDistance(double XPt1, double ZPt1, double XPt2, double ZPt2)
        {
            return Math.Sqrt((XPt1 - XPt2) * (XPt1 - XPt2) + (ZPt1 - ZPt2) * (ZPt1 - ZPt2));
        }



        public static double getMedianAbsoluteDeviation(List<double> statList)
        {
            double median = statList.Median();
            List<double> diffList = getDiffList(statList, median, true);
            return diffList.Median();
        }

        public static double getStandardDeviation(List<double> sessionStatList)
        {
            DoubleArray array = DoubleArray.From(sessionStatList);
            return array.Std();
        }
        


    }
}
