using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace IgorKL.ACAD3.Model.Helpers.Math
{
    public static class Calculator
    {
        public static double Atan(double y, double x)
        {
            if (x > 0)
                return System.Math.Atan(y / x);
            else if (x < 0)
                return System.Math.Atan(y / x) - System.Math.PI;
            else  // x == 0
            {
                if (y > 0)
                    return System.Math.PI;
                else if (y < 0)
                    return -System.Math.PI;
                else // if (y == 0) theta is undefined
                    return 0.0;
            }
        }

        /// <summary>
        /// Computes Angle between current direction
        /// (vector from last vertex to current vertex)
        /// and the last pline segment
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="xdir"></param>
        /// <param name="ucs"></param>
        /// <returns></returns>
        public static double ComputeAngle(
          Point3d startPoint, Point3d endPoint,
          Vector3d xdir, Matrix3d ucs
        )
        {
            var v =
              new Vector3d(
                (endPoint.X - startPoint.X) / 2,
                (endPoint.Y - startPoint.Y) / 2,
                (endPoint.Z - startPoint.Z) / 2
              );

            double cos = v.DotProduct(xdir);
            double sin =
              v.DotProduct(
                Vector3d.ZAxis.TransformBy(ucs).CrossProduct(xdir)
              );

            return Atan(sin, cos);
        }


    }

}

