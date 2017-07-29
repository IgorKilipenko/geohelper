using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;

namespace IgorKL.ACAD3.Model.Extensions
{
    public static class CurveExtension
    {
        public static Curve GetCloneWithNewElevation(this Curve curve, double newElevation, Transaction trans)
        {
            Curve newCurve = ((Curve)trans.GetObject(curve.Id, OpenMode.ForRead)).Clone() as Curve;
            Matrix3d mat = Matrix3d.Displacement(new Point3d(curve.StartPoint.X, curve.StartPoint.Y, newElevation) -
                curve.StartPoint);
            newCurve.TransformBy(mat);
            return newCurve;
        }
        public static Curve GetCloneWithNewElevation(this Curve curve, double newElevation)
        {
            using (Transaction trans = Tools.StartTransaction())
            {
                return GetCloneWithNewElevation(curve, newElevation, trans);
            }
        }

        public static void SetElevation(this Curve curve, double newElevation, Transaction trans, bool comit=true)
        {
            Curve newCurve = (Curve)trans.GetObject(curve.Id, OpenMode.ForRead);
            Matrix3d mat = Matrix3d.Displacement(new Point3d(curve.StartPoint.X, curve.StartPoint.Y, newElevation) -
                curve.StartPoint);
            newCurve.UpgradeOpen();
            newCurve.TransformBy(mat);

            if (comit)
                trans.Commit();
        }
        public static void SetElevation(this Curve curve, double newElevation, bool comit = true)
        {
            using (Transaction trans = Tools.StartTransaction())
            {
                SetElevation(curve, newElevation, trans, comit);
            }
        }

        public static Vector3d GetPerpendicularVector(this Curve curve, Point3d point)
        {
            return curve.GetFirstDerivative(point).GetPerpendicularVector();
        }

        public static Curve GetOrthoProjectedCurve(this Curve curve, Matrix3d ucs)
        {
            Plane plane = new Plane(ucs.CoordinateSystem3d.Origin, ucs.CoordinateSystem3d.Yaxis.Negate());
            return curve.GetOrthoProjectedCurve(plane);
        }

        [Obsolete]
        public static Polyline ConvertToPolylineEx(this Curve curve)
        {
            int count = curve.GetNumberOfVertices();
            Polyline pline = new Polyline(count);
            pline.AddVertexes(curve.GetPoints().ToEnumerable());
            return pline;
        }
        
        public static Polyline ConvertToPolyline(this Curve curve)
        {
            if (curve is Polyline)
                return (Polyline)curve;

            else if (curve is Line)
            {
                return ((Line)curve).ConvertToPolyline();
            }
            else if (curve is Arc)
            {
                return ((Arc)curve).ConvertToPolyline();
            }
            else if (curve is Polyline2d)
            {
                return ((Polyline2d)curve).ConvertToPolyline();
            }
            else if (curve is Polyline3d)
            {
                return ((Polyline3d)curve).ConvertToPolyline();
            }
            else
            {
                //return curve.ConvertToPolylineEx();
                Polyline pline = new Polyline();
                pline.ConvertFrom(curve, false);
                return pline;
            }
        }

        // A generalised IsPointOnCurve function that works on all
        // types of Curve (including Polylines), and checks the position
        // of the returned point rather than relying on catching an
        // exception
        public static bool IsPointOnCurveGCP(this Curve curve, Point3d point)
        {
            try
            {
                // Return true if operation succeeds

                Point3d p = curve.GetClosestPointTo(point, false);
                return (p - point).Length <= Tolerance.Global.EqualPoint;
            }
            catch { }

            // Otherwise we return false

            return false;
        }
        
        // A generalised IsPointOnCurve function that works on all
        // types of Curve (including Polylines), but catches an
        // Exception on failure
        public static bool IsPointOnCurveGDAP(this Curve curve, Point3d point)
        {
            try
            {
                // Return true if operation succeeds

                curve.GetDistAtPoint(point);
                return true;
            }
            catch { }

            // Otherwise we return false

            return false;
        }
    }
}
