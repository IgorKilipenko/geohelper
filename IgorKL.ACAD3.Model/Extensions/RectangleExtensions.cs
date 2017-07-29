using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

namespace IgorKL.ACAD3.Model.Extensions
{
    public static class RectangleExtensions
    {
        public static Vector3d GetLeftVerticalVector(this Rectangle3d rectg)
        {
            return rectg.UpperLeft - rectg.LowerLeft;
        }
        public static Vector3d GetRightVerticalVector(this Rectangle3d rectg)
        {
            return rectg.UpperRight - rectg.LowerRight;
        }
        public static Vector3d GetLowertHorizontalVector(this Rectangle3d rectg)
        {
            return rectg.LowerRight - rectg.LowerLeft;
        }
        public static Vector3d GetUpperHorizontalVector(this Rectangle3d rectg)
        {
            return rectg.UpperRight - rectg.UpperLeft;
        }

        public static Polyline ConvertToPolyline(this Rectangle3d rec, Matrix3d transform)
        {
            Polyline pline = new Polyline(5);
            pline.AddVertexAt(0, rec.LowerLeft.TransformBy(transform));
            pline.AddVertexAt(1, rec.UpperLeft.TransformBy(transform));
            pline.AddVertexAt(2, rec.UpperRight.TransformBy(transform));
            pline.AddVertexAt(3, rec.LowerRight.TransformBy(transform));
            pline.AddVertexAt(4, rec.LowerLeft.TransformBy(transform));
            pline.Closed = true;
            return pline;
        }

        public static Polyline ConvertToPolyline(this Rectangle3d rec)
        {
            return ConvertToPolyline(rec, Matrix3d.Identity);
        }

        public static Rectangle3d? ConvertToRectangle(this Polyline pline)
        {
            if (pline.NumberOfVertices < 4)
                return null;

            Point3d lowerLeft = pline.GetPoint3dAt(0);
            Point3d lowerRight = pline.GetPoint3dAt(3);
            Point3d upperLeft = pline.GetPoint3dAt(1);
            Point3d upperRight = pline.GetPoint3dAt(2);

            Rectangle3d rectg = new Rectangle3d(
                lowerLeft: lowerLeft,
                lowerRight: lowerRight,
                upperLeft: upperLeft,
                upperRight: upperRight
                );

            return rectg;
        }

        public static Point3dCollection GetPoints(this Rectangle3d rectg, bool close = false)
        {
            Point3dCollection points = new Point3dCollection(
                new[] {
                    rectg.LowerLeft,
                    rectg.UpperLeft,
                    rectg.UpperRight,
                    rectg.LowerRight
                });
            if (close)
                points.Add(rectg.LowerLeft);
            return points;
        }
    }
}
