using System;
using System.Collections.Generic;
using System.Linq;
using ShoNS.Array;


namespace GaitAndBalanceApp.Analysis
{
    public class Ellipse
    {
        //95% confidence ellipse
        public double area;
        public double a, b;
        public double perimeter;
        public double centerX, centerY; //Center of the ellipse
        public double angle;

        public List<double> ellipseXList; //Sampled points on the ellipse 
        public List<double> ellipseYList;

        public Ellipse(Trajectory trajectory) {

            Point center = trajectory.mean();

            centerX = center.x;
            centerY = center.z;

            //Calculate area
            DoubleArray covMatrix = trajectory.covariance();
            Eigen eigen = new Eigen(covMatrix);
            double chisquare_val = 2.4477;
            DoubleArray eigenvalues = (DoubleArray)eigen.D;
            a = chisquare_val * Math.Sqrt(eigenvalues.Max());
            b = chisquare_val * Math.Sqrt(eigenvalues.Min());
            //Calculate ellipse coordinate points
            DoubleArray eigenvectors = (DoubleArray)eigen.V;
            DoubleArray largestEigenvector = eigenvectors.Max(0);
            DoubleArray smallestEigenvector = eigenvectors.Min(0);
            angle = Math.Atan2(largestEigenvector[1], largestEigenvector[0]);
            //This angle is between -pi and pi.
            //Let's shift it such that the angle is between 0 and 2pi
            if (angle < 0)
            {
                angle = angle + 2 * Math.PI;
            }
            area = Math.PI * a * b;
            perimeter = Math.PI/2 * Math.Sqrt(2*a*a + 2*b*b);
            getEllipseSamplePoints(100);
            covMatrix.Dispose();
            eigenvalues.Dispose();
            eigenvectors.Dispose();
            largestEigenvector.Dispose();
            smallestEigenvector.Dispose();
        }
        private void getEllipseSamplePoints(int numPoints)
        {
            //Calculate n points on the ellipse
            List<double> thetaGrid = getThetaGrid(numPoints);
            double[,] ellipsePointsTemp = new double[thetaGrid.Count, 2];
            for (int i = 0; i < thetaGrid.Count; i++)
            {
                ellipsePointsTemp[i, 0] = a * Math.Cos(thetaGrid[i]);
                ellipsePointsTemp[i, 1] = b * Math.Sin(thetaGrid[i]);
            }

            //Define rotation matrix
            double[,] rotMatrix = { { Math.Cos(angle), Math.Sin(angle) }, { -Math.Sin(angle), Math.Cos(angle) } };
            DoubleArray rotMatrixDA = DoubleArray.From(rotMatrix);

            //Convert to DoubleArray to use its matrix multiply functionality
            DoubleArray data2DDoubleArray = DoubleArray.From(ellipsePointsTemp);
            DoubleArray ellipsePointsDA = data2DDoubleArray.Multiply(rotMatrixDA);

            //Shift the transformed points to the center
            DoubleArray ellipseXCol = ellipsePointsDA.GetCol(0).Add(centerX);
            DoubleArray ellipseYCol = ellipsePointsDA.GetCol(1).Add(centerY);

            ellipseXList = ellipseXCol.ToList();
            ellipseYList = ellipseYCol.ToList();
            rotMatrixDA.Dispose();
            data2DDoubleArray.Dispose();
            ellipsePointsDA.Dispose();
            //ellipseXCol.Dispose();
            //ellipseYCol.Dispose();
        }
        private List<double> getThetaGrid(int n)
        {
            //Returns n numbers evenly spaced from 0 to 2pi
            List<double> thetaGrid = new List<double>();
            double increment = (2 * Math.PI) / (n - 1);
            for (int i = 0; i < n; i++)
            {
                thetaGrid.Add(i * increment);
            }
            return thetaGrid;
        }

    }
}
