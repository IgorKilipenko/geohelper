using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Colors;

namespace IgorKL.ACAD3.Model.CoordinateGeometry
{
    public static class Helper
    {
        public static double GetAngle(Point3d basePoint, Point3d directionPoint, Point3d destinationPoint, Vector3d ZAxis)
        {
            Vector3d baseVector = basePoint.GetVectorTo(directionPoint);
            Vector3d destinationVector = basePoint.GetVectorTo(destinationPoint);

            double angle = baseVector.GetAngleTo(destinationVector, ZAxis);

            return angle;
        }
        public static double GetAngle(Point3d basePoint, Point3d directionPoint, Point3d destinationPoint)
        {
            Vector3d baseVector = basePoint.GetVectorTo(directionPoint);
            Vector3d destinationVector = basePoint.GetVectorTo(destinationPoint);

            double angle = baseVector.GetAngleTo(destinationVector);

            return angle;
        }

        public static double GetAngle(Vector3d baseVector, Vector3d destinationVector, Vector3d ZAxis)
        {
            double angle = baseVector.GetAngleTo(destinationVector, ZAxis);

            return angle;
        }
    }
}
