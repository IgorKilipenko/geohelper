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

namespace IgorKL.ACAD3.Model.CoordinateSystem
{
    public static class CoordinateTools
    {
        public static ObjectId OpenOrCreateUserCoordinateSystem(string ucsName, bool openIfExists = true)
        {
            using (Transaction trans = Tools.StartTransaction())
            {
                // Open the UCS table for read
                UcsTable ucsTable = (UcsTable)trans.GetObject(Tools.GetAcadDatabase().UcsTableId, OpenMode.ForRead);

                UcsTableRecord ucsTblRec;
                
                // Check to see if the "New_UCS" UCS table record exists
                if (ucsTable.Has(ucsName) == false)
                {
                    ucsTblRec = new UcsTableRecord();
                    ucsTblRec.Name = ucsName;

                    // Open the UCSTable for write
                    ucsTable.UpgradeOpen();

                    // Add the new UCS table record
                    ucsTable.Add(ucsTblRec);
                    trans.AddNewlyCreatedDBObject(ucsTblRec, true);

                    ucsTblRec.Dispose();
                }
                else
                {
                    if (openIfExists)
                        ucsTblRec = trans.GetObject(ucsTable[ucsName],
                                                        OpenMode.ForWrite) as UcsTableRecord;
                    else
                        throw new ArgumentException(string.Format("\nA UCS with this name \"{0}\" alredy exists", ucsName));
                }
                return ucsTblRec.Id;
            }
        }

        public static UcsTableRecord CreateTempUtc(Point3d origin, Vector3d xAxis, Vector3d yAxis)
        {
            UcsTableRecord ucs = new UcsTableRecord();
            ucs.Origin = origin;
            ucs.XAxis = xAxis;
            ucs.YAxis = yAxis;
            return ucs;
        }

        public static double GetAngleFromUcsYAxis(Vector3d vector)
        {
            Matrix3d ucs = GetCurrentUcs();
            return ucs.CoordinateSystem3d.Yaxis.GetAngleTo(vector, ucs.CoordinateSystem3d.Zaxis);
        }

        public static Matrix3d GetWcs()
        {
            return Matrix3d.PlaneToWorld(new Plane());
        }

        public static Matrix3d GetCurrentUcs()
        {
            return Tools.GetActiveAcadDocument().Editor.CurrentUserCoordinateSystem;
        }

        public static void MirrorAtYAxis(Entity ent, Point3d point, bool positive = true)
        {
            Matrix3d mat = Matrix3d.Mirroring(new Line3d(point, new Point3d(point.X, point.Y + (positive ? +1d : -1d), point.Z)));
            ent.TransformBy(mat);
        }

        public static void MirrorAtXAxis(Entity ent, Point3d point, bool positive = true)
        {
            Matrix3d mat = Matrix3d.Mirroring(new Line3d(point, new Point3d(point.X + (positive ? +1d : -1d), point.Y , point.Z)));
            ent.TransformBy(mat);
        }

        public static double RotationByYAxisUcs(Entity ent, Vector3d vector, Point3d center)
        {
            double angle = GetAngleFromUcsYAxis(vector);
            var ucs = GetCurrentUcs();
            ent.TransformBy(Matrix3d.Rotation(angle, ucs.CoordinateSystem3d.Yaxis, center));
            return angle;
        }

        public static void Displacement(Entity ent, Vector3d vector)
        {
            Matrix3d mat = Matrix3d.Displacement(vector);
            ent.TransformBy(mat);
        }

        public static Point3d Displacement(Entity ent, Point3d fromPoint, Point3d toPoint)
        {
            Matrix3d mat = Matrix3d.Displacement(toPoint - fromPoint);
            ent.TransformBy(mat);
            return toPoint;
        }
    }
}
