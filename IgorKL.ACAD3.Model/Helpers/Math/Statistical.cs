using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using IgorKL.ACAD3.Model.Extensions;

namespace IgorKL.ACAD3.Model.Helpers.Math
{
    public static class Statistical
    {
        /// <summary>
        /// Fits a line to a collection of (x,y) points.
        /// </summary>
        /// <param name="xVals">The x-axis values.</param>
        /// <param name="yVals">The y-axis values.</param>
        /// <param name="inclusiveStart">The inclusive inclusiveStart index.</param>
        /// <param name="exclusiveEnd">The exclusive exclusiveEnd index.</param>
        /// <param name="rsquared">The r^2 value of the line.</param>
        /// <param name="yintercept">The y-intercept value of the line (i.e. y = ax + b, yintercept is b).</param>
        /// <param name="slope">The slop of the line (i.e. y = ax + b, slope is a).</param>
        public static void LinearRegression(double[] xVals, double[] yVals,
                                            int inclusiveStart, int exclusiveEnd,
                                            out double rsquared, out double yintercept,
                                            out double slope)
        {
            System.Diagnostics.Debug.Assert(xVals.Length == yVals.Length);
            double sumOfX = 0;
            double sumOfY = 0;
            double sumOfXSq = 0;
            double sumOfYSq = 0;
            double ssX = 0;
            double ssY = 0;
            double sumCodeviates = 0;
            double sCo = 0;
            double count = exclusiveEnd - inclusiveStart;

            for (int ctr = inclusiveStart; ctr < exclusiveEnd; ctr++)
            {
                double x = xVals[ctr];
                double y = yVals[ctr];
                sumCodeviates += x * y;
                sumOfX += x;
                sumOfY += y;
                sumOfXSq += x * x;
                sumOfYSq += y * y;
            }
            ssX = sumOfXSq - ((sumOfX * sumOfX) / count);
            ssY = sumOfYSq - ((sumOfY * sumOfY) / count);
            double RNumerator = (count * sumCodeviates) - (sumOfX * sumOfY);
            double RDenom = (count * sumOfXSq - (sumOfX * sumOfX))
             * (count * sumOfYSq - (sumOfY * sumOfY));
            sCo = sumCodeviates - ((sumOfX * sumOfY) / count);

            double meanX = sumOfX / count;
            double meanY = sumOfY / count;
            double dblR = RNumerator / System.Math.Sqrt(RDenom);
            rsquared = dblR * dblR;
            yintercept = meanY - ((sCo / ssX) * meanX);
            slope = sCo / ssX;
        }

        public static Line LinearRegression(Polyline pline)
        {
            return LinearRegression(pline, 0, pline.NumberOfVertices);
        }

        public static Line LinearRegression(Polyline pline, int inclusiveStart, int exclusiveEnd)
        {
            double rsquared;
            return LinearRegression(pline, 0, pline.NumberOfVertices, out rsquared);
        }

        public static Line LinearRegression(Polyline pline, int inclusiveStart, int exclusiveEnd, out  double rsquared)
        {
            int count = pline.NumberOfVertices;
            double[] xVals = new double[count];
            double[] yVals = new double[count];

            for (int i = 0; i < count; i++)
            {
                var point = pline.GetPoint2dAt(i);
                xVals[i] = point.X;
                yVals[i] = point.Y;
            }

            double yintercept;
            double slope;
            LinearRegression(xVals, yVals, inclusiveStart, exclusiveEnd, out rsquared, out yintercept, out slope);

            Point3d sp = new Point3d(
                pline.StartPoint.X,
                pline.StartPoint.X * slope + yintercept,
                pline.Elevation
                );
            Point3d ep = new Point3d(
                pline.EndPoint.X,
                pline.EndPoint.X * slope + yintercept,
                pline.Elevation
                );

            Line line = new Line(sp, ep);
            return line;
        }
    }
}
