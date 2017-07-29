using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

namespace IgorKL.ACAD3.Model.Extensions
{
    public static class ArcExtensions
    {

        #region Ortho Normal Methods
        public static Point3d? GetOrthoNormalPointEx(this Arc arc, Point3d point, bool nullForOutOfRange = true)
        {
            point = point.OrthoProject(arc.GetPlane());

            Point3d? normalPoint = null;
            Point3d center = arc.Center;
            Vector3d startVector = arc.StartPoint - center;
            Vector3d endVector = arc.EndPoint - center;
            Vector3d vector = point - center;

            double startAngle = Matrix3d.Identity.CoordinateSystem3d.Xaxis.GetAngle2d(startVector);
            double endAngle = Matrix3d.Identity.CoordinateSystem3d.Xaxis.GetAngle2d(endVector);
            double angle = Matrix3d.Identity.CoordinateSystem3d.Xaxis.GetAngle2d(vector);


            if (angle < Math.Max(startAngle, endAngle) &&
                angle > Math.Min(startAngle, endAngle))
            {
                double pointAngle = Math.Abs(startAngle - angle);
                double dist = pointAngle * (arc.StartPoint - center).Length;
                normalPoint = arc.GetPointAtDist(dist);
                return normalPoint;
            }
            else
            {
                if (nullForOutOfRange)
                    return null;
                else
                {
                    if (endAngle < startAngle)
                    {
                        if (angle > startAngle)
                            return arc.StartPoint;
                        if (angle < endAngle)
                            return arc.EndPoint;
                    }
                    else if (endAngle > startAngle)
                    {
                        if (angle < startAngle)
                            return arc.StartPoint;
                        if (angle > endAngle)
                            return arc.EndPoint;
                    }
                }
            }

            return null;
        }

        public static Point3d? GetOrthoNormalPoint(this Arc arc, Point3d point, bool nullForOutOfRange = true)
        {
            point = point.OrthoProject(arc.GetPlane());

            CircularArc3d arc3d = arc.ConvertToCircularArc();
            Vector3d central = point - arc3d.Center;
            PointOnCurve3d pointOn = null;
            try
            {
                pointOn = arc3d.GetNormalPoint(point, Tolerance.Global);
            }
            catch (InvalidOperationException)
            {
                if (nullForOutOfRange)
                    return null;
                else
                {
                    if ((point - arc3d.StartPoint).Length < (point - arc3d.EndPoint).Length)
                        return arc3d.StartPoint;
                    else
                        return arc3d.EndPoint;
                }
            }
            if (pointOn != null)
            {
                return pointOn.Point;
            }



            return null;
        }

        public static Line GetOrthoNormalLine(this Arc arc, Point3d point, bool nullForOutOfRange = true)
        {
            point = point.OrthoProject(arc.GetPlane());

            Point3d destPoint = point;
            Point3d? normalPoint = arc.GetOrthoNormalPoint(point, nullForOutOfRange);
            if (!normalPoint.HasValue)
                return null;

            if (!nullForOutOfRange)
            {
                if (normalPoint.Value.IsEqualTo(arc.StartPoint, Tolerance.Global) ||
                    normalPoint.Value.IsEqualTo(arc.EndPoint, Tolerance.Global))
                {
                    Vector3d vector = point - normalPoint.Value;
                    Vector3d radius = normalPoint.Value - arc.Center;
                    double sin = radius.GetCos2d(vector);
                    destPoint = normalPoint.Value.Add(radius.Normalize().MultiplyBy(sin * vector.Length));
                }
            }

            return new Line(normalPoint.Value, destPoint);
        }
        #endregion

        public static double GetArcBulge(this Arc arc)
        {
            double deltaAng = arc.EndAngle - arc.StartAngle;
            if (deltaAng < 0)
                deltaAng += 2d * Math.PI;
            return Math.Tan(deltaAng * 0.25d);
        }

        public static Polyline ConvertToPolyline(this Arc arc)
        {
            Polyline pline = new Polyline(2);
            pline.AddVertexAt(0, arc.StartPoint, arc.GetArcBulge(), 0, 0);
            pline.AddVertexAt(1, arc.EndPoint, 0, 0, 0);
            return pline;
        }

        public static Arc ConvertToArc(this CircularArc3d arc3d)
        {
            double angle =
                arc3d.ReferenceVector.AngleOnPlane(new Plane(arc3d.Center, arc3d.Normal));
            Arc arc = new Arc(arc3d.Center, arc3d.Normal, arc3d.Radius, arc3d.StartAngle + angle, arc3d.EndAngle + angle);

            return arc;
        }

        public static CircularArc3d ConvertToCircularArc(this Arc arc)
        {
            Vector3d vector = arc.StartPoint - arc.Center;
            CircularArc3d arc3d = new CircularArc3d(arc.Center, arc.Normal, vector.Normalize(), arc.Radius, 0d, arc.EndAngle - arc.StartAngle);
            return arc3d;
        }
    }
}
