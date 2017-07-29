using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;

namespace IgorKL.ACAD3.Model.Extensions
{
    public static class DBTextExtensions
    {
        public static Rectangle3d? GetTextBoxCorners(this DBText text)
        {
            if (!text.Bounds.HasValue)
                return null;
            DBText cloneText = (DBText)text.Clone();
            
            Matrix3d mat = Matrix3d.Identity;
            mat = mat.PreMultiplyBy(Matrix3d.Rotation(-text.Rotation, text.Normal, text.Position));

            cloneText.TransformBy(mat);
            cloneText.AdjustAlignment(Tools.GetAcadDatabase());
            if (!cloneText.Bounds.HasValue)
                return null;

            Extents3d bounds = cloneText.Bounds.Value;

            Point3d min = bounds.MinPoint;
            Point3d max = bounds.MaxPoint;
#if DEBUG1
            if (min.X >= max.X)
                System.Diagnostics.Debugger.Break();
#endif
            Vector3d diametr = max - min;

            //mat = Matrix3d.Identity.PreMultiplyBy(Matrix3d.Rotation(text.Rotation, text.Normal, text.Position));
            mat = mat.Inverse();

            Point3d upperLeft = new Point3d(min.X, min.Y + diametr.Y, min.Z).TransformBy(mat);
            Point3d upperRight = max.TransformBy(mat);
            Point3d lowerLeft = min.TransformBy(mat);
            Point3d lowerRight = new Point3d(max.X, max.Y - diametr.Y, max.Z).TransformBy(mat);

            Rectangle3d rec = new Rectangle3d(upperLeft, upperRight, lowerLeft, lowerRight);

#if DEBUG1
            if (rec.LowerLeft.X >= rec.UpperRight.X)
                System.Diagnostics.Debugger.Break();
#endif

            return rec;
        }

        public static Extents3d? GetNotRotatedBounds(this DBText text)
        {
            if (!text.Bounds.HasValue)
                return null;
            DBText cloneText = (DBText)text.Clone();

            Matrix3d mat = Matrix3d.Identity;
            mat = mat.PreMultiplyBy(Matrix3d.Rotation(-text.Rotation, text.Normal, text.Position));

            cloneText.TransformBy(mat);
            cloneText.AdjustAlignment(Tools.GetAcadDatabase());
            if (!cloneText.Bounds.HasValue)
                return null;

            Extents3d bounds = cloneText.Bounds.Value;
            return bounds;
        }

        public static Extents3d? GetRotatedBounds(this DBText text)
        {
            if (!text.Bounds.HasValue)
                return null;
            DBText cloneText = (DBText)text.Clone();

            Matrix3d mat = Matrix3d.Identity;
            mat = mat.PreMultiplyBy(Matrix3d.Rotation(-text.Rotation, text.Normal, text.Position));

            cloneText.TransformBy(mat);
            cloneText.AdjustAlignment(Tools.GetAcadDatabase());
            if (!cloneText.Bounds.HasValue)
                return null;

            Extents3d bounds = cloneText.Bounds.Value;
            bounds.TransformBy(mat.Inverse());
            return bounds;
        }
    }
}
