using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
//using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

using IgorKL.ACAD3.Model.Extensions;

namespace IgorKL.ACAD3.Model.Helpers.Geometry
{
    public static class Intersects
    {
        public static bool IsInsidePolygon(Polyline polygon, Point3d pt)
        {
            int n = polygon.NumberOfVertices;
            double angle = 0;
            Point pt1, pt2;
            double tolerence = System.Math.PI * 0.00001;

            for (int i = 0; i < n; i++)
            {
                pt1.X = polygon.GetPoint2dAt(i).X - pt.X;
                pt1.Y = polygon.GetPoint2dAt(i).Y - pt.Y;
                pt2.X = polygon.GetPoint2dAt((i + 1) % n).X - pt.X;
                pt2.Y = polygon.GetPoint2dAt((i + 1) % n).Y - pt.Y;
                angle += Angle2D(pt1.X, pt1.Y, pt2.X, pt2.Y);
            }

            if (System.Math.Abs(angle) - System.Math.PI < -tolerence)
                return false;
            else
                return true;
        }

        public struct Point
        {
            public double X, Y;
        };

        public static double Angle2D(double x1, double y1, double x2, double y2)
        {
            double dtheta, theta1, theta2;

            theta1 = System.Math.Atan2(y1, x1);
            theta2 = System.Math.Atan2(y2, x2);
            dtheta = theta2 - theta1;
            while (dtheta > System.Math.PI)
                dtheta -= (System.Math.PI * 2);
            while (dtheta < -System.Math.PI)
                dtheta += (System.Math.PI * 2);
            return (dtheta);
        }
    }
}
