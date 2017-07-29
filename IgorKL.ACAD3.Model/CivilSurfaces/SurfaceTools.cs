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
using Autodesk.Civil.DatabaseServices;

using CivilSurface = Autodesk.Civil.DatabaseServices.Surface;
using AcadEntity = Autodesk.AutoCAD.DatabaseServices.Entity;
using IgorKL.ACAD3.Model.Extensions;

namespace IgorKL.ACAD3.Model.CivilSurfaces
{
    public class SurfaceTools
    {
        public static void Test()
        {
            TinVolumeSurface volSurface;
            if (!ObjectCollector.TrySelectAllowedClassObject(out volSurface))
                return;
            var b = volSurface.BoundariesDefinition[0];
        }

        public static ObjectId CroppingSurface(TinSurface surface, Polyline border)
        {
            double area2d = 0.0;
            using (Database destDb = new Database(true, true))
            {
                Database db = Tools.GetAcadDatabase();
                using (Transaction transSrc = Tools.StartTransaction(db))
                {
                    using (Transaction transDest = Tools.StartTransaction(destDb))
                    {
                        var points = border.GetPoints();
                        HostApplicationServices.WorkingDatabase = destDb;

                        string surfaceName = "Cropped_" + surface.Name + "<[Next Counter(CP)]>";
                        ObjectId newSurfaceId = TinSurface.CreateByCropping(destDb, surfaceName, surface.Id, points);
                        TinSurface newSurface = transDest.GetObject(newSurfaceId, OpenMode.ForRead) as TinSurface;

                        HostApplicationServices.WorkingDatabase = db;
                        try
                        {
                            return TinSurface.CreateByCropping(db, newSurface.Name, newSurface.Id, points);
                        }
                        catch (System.Exception ex)
                        {
                            Tools.GetAcadEditor().WriteMessage("\n" + ex.Message);
                            return ObjectId.Null;
                        }
                    }
                }
            }
        }

        public static ObjectId CroppingSurface2(TinSurface surface, Polyline border)
        {
            Polyline3d newBorder = null;
            ObjectId newBorderId = ObjectId.Null;
            TinSurface newSurface = null;
            using (Database destDb = new Database(true, true))
            {

                Database db = Tools.GetAcadDatabase();
                using (Transaction transSrc = Tools.StartTransaction(db))
                {
                    using (Transaction transDest = Tools.StartTransaction(destDb))
                    {
                        var points = border.GetPoints();
                        HostApplicationServices.WorkingDatabase = destDb;

                        string surfaceName = "Cropped_" + surface.Name + "<[Next Counter(CP)]>";
                        ObjectId newSurfaceId = TinSurface.CreateByCropping(destDb, surfaceName, surface.Id, points);
                        newSurface = transDest.GetObject(newSurfaceId, OpenMode.ForRead) as TinSurface;

                        newBorderId = ((ITerrainSurface)newSurface).GetBounds()[0];





                        HostApplicationServices.WorkingDatabase = db;
                        newBorder = transDest.GetObject(newBorderId.ConvertToRedirectedId(), OpenMode.ForRead) as Polyline3d;
                        newBorder = newBorder.Clone() as Polyline3d;
                        newSurface = newSurface.Clone() as TinSurface;
                    }
                    if (newBorder != null)
                    {
                        try
                        {
                            Tools.AppendEntityEx(transSrc, db, newBorder, true);

                        }
                        catch (System.Exception ex)
                        {
                            Tools.GetAcadEditor().WriteMessage("\n" + ex.Message);
                        }
                    }
                }

            }

            /*if (newBorder != null)
            {
                using (Transaction trans = Tools.StartTransaction())
                {
                    try
                    {
                        newBorder.Id.ConvertToRedirectedId();
                        Tools.AppendEntity(trans, newBorder, true);
                    }
                    catch (System.Exception ex)
                    {
                        Tools.GetAcadEditor().WriteMessage("\n" + ex.Message);
                    }
                }
            }*/
            /*
            if (newSurface != null)
            {
                using (Transaction trans = Tools.GetActiveAcadDocument().TransactionManager.StartTransaction())
                {
                    try
                    { 
                        Tools.AppendEntity(trans ,newSurface, true);
                    }
                    catch (System.Exception ex)
                    {
                        Tools.GetAcadEditor().WriteMessage("\n" + ex.Message);
                    }
                }
            }*/

            return ObjectId.Null;
        }


        public static ObjectId CroppingSurface3(TinSurface surface, Polyline border)
        {
            Polyline3d newBorder = null;
            ObjectId newBorderId = ObjectId.Null;
            TinSurface newSurface = null;
            using (Database destDb = new Database(true, true))
            {

                Database sourceDb = Tools.GetAcadDatabase();
                using (Transaction transSrc = Tools.StartTransaction(sourceDb))
                {
                    using (Transaction transDest = Tools.StartTransaction(destDb))
                    {
                        var points = border.GetPoints();
                        HostApplicationServices.WorkingDatabase = destDb;

                        string surfaceName = "Cropped_" + surface.Name + "<[Next Counter(CP)]>";
                        ObjectId newSurfaceId = TinSurface.CreateByCropping(destDb, surfaceName, surface.Id, points);
                        newSurface = transDest.GetObject(newSurfaceId, OpenMode.ForRead) as TinSurface;

                        newBorderId = ((ITerrainSurface)newSurface).GetBounds()[0];



                        HostApplicationServices.WorkingDatabase = sourceDb;
                        newBorder = transDest.GetObject(newBorderId.ConvertToRedirectedId(), OpenMode.ForRead) as Polyline3d;
                        newBorder = newBorder.Clone() as Polyline3d;
                        newSurface = newSurface.Clone() as TinSurface;
                    }

                }
            }
            try
            {
                newBorder = newBorder.Clone() as Polyline3d;
                Tools.AppendEntityEx(Tools.GetActiveAcadDocument(), newBorder);

            }
            catch (System.Exception ex)
            {
                Tools.GetAcadEditor().WriteMessage("\n" + ex.Message);
            }


            return ObjectId.Null;
        }

        public static ObjectId CroppingSurface4(TinSurface surface, Polyline border)
        {
            Document activeDoc = Tools.GetActiveAcadDocument();
            Database sourceDb = activeDoc.Database;
            var points = border.GetPoints();
            string surfaceName = "Cropped_" + surface.Name + "<[Next Counter(CP)]>";
            using (Database destDb = new Database(true, false))
            {
                HostApplicationServices.WorkingDatabase = destDb;
                ObjectId newSurfaceId = TinSurface.CreateByCropping(destDb, surfaceName, surface.Id, points);

                Tools.CloneObjects(new ObjectIdCollection(new[] { newSurfaceId }), destDb, sourceDb);

                HostApplicationServices.WorkingDatabase = sourceDb;

                //destDb.SaveAs(@"C:\Debug\CroppingSurface4.dwg", DwgVersion.Current);
            }

            return ObjectId.Null;
        }



        public static ObjectId CroppingSurface5(TinSurface surface, Polyline border)
        {
            Document activeDoc = Tools.GetActiveAcadDocument();
            Database sourceDb = activeDoc.Database;
            var points = border.GetPoints();
            string surfaceName = "Cropped_" + surface.Name + "<[Next Counter(CP)]>";
            using (Database destDb = new Database(true, false))
            {
                HostApplicationServices.WorkingDatabase = destDb;
                ObjectId newSurfaceId = TinSurface.CreateByCropping(destDb, surfaceName, surface.Id, points);
                HostApplicationServices.WorkingDatabase = sourceDb;

                using (Transaction transDest = Tools.StartTransaction(destDb))
                {
                    TinSurface newSurface = transDest.GetObject(newSurfaceId, OpenMode.ForRead) as TinSurface;
                    /*newSurface = newSurface.Clone() as TinSurface;

                    newSurface.SetDatabaseDefaults(sourceDb);
                    Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument = activeDoc;
                    Tools.AppendEntity(sourceDb, newSurface);*/

                    //newSurface = newSurface.Clone() as TinSurface;
                    transDest.Commit();
                    newSurface = newSurface.Clone() as TinSurface;
                    newSurface.SetDatabaseDefaults(sourceDb);
                    newSurface.SetToStandard(sourceDb);
                    newSurface.StyleId = surface.StyleId;

                    IntPtr ptr = newSurface.UnmanagedObject;
                    var obj = TinSurface.Create(ptr, false);

                    using (Transaction srcTrans = Tools.StartTransaction(sourceDb))
                    {
                        newSurfaceId = TinSurface.Create(sourceDb, "test_surface");

                        newSurface.SetDatabaseDefaults(sourceDb);
                        newSurface.SetToStandard(sourceDb);
                        newSurface.SetDefaultLayer();
                        newSurface.StyleId = surface.StyleId;

                        //newSurface = srcTrans.GetObject(TinSurface.Create(sourceDb, "test_surface"), OpenMode.ForWrite) as TinSurface;
                        //newSurface.CopyFrom(obj);
                        newSurface.Rebuild();
                        srcTrans.Commit();
                    }
                    using (Transaction srcTrans = Tools.StartTransaction(sourceDb))
                    {
                        newSurface = srcTrans.GetObject(newSurfaceId, OpenMode.ForWrite) as TinSurface;

                        newSurface.UpgradeOpen();

                        newSurface.CopyFrom(obj);
                        //newSurface.Name = "test_surface2";

                        newSurface.SetDatabaseDefaults(sourceDb);
                        newSurface.SetToStandard(sourceDb);
                        newSurface.SetDefaultLayer();

                        newSurface.StyleId = surface.StyleId;
                        newSurface.Rebuild();

                        srcTrans.Commit();

                    }

                    using (Transaction srcTrans = Tools.StartTransaction(sourceDb))
                    {
                        newSurface = srcTrans.GetObject(newSurfaceId, OpenMode.ForWrite) as TinSurface;
                        newSurface.UpgradeOpen();

                        newSurface.SetDatabaseDefaults(sourceDb);
                        newSurface.SetToStandard(sourceDb);
                        newSurface.SetDefaultLayer();

                        srcTrans.Commit();
                        newSurface.Rebuild();
                        //newSurface.RebuildSnapshot();
                        newSurface.CreateSnapshot();
                    }

                    //Tools.AppendEntity(sourceDb, obj as TinSurface);
                }
            }

            return ObjectId.Null;
        }


        public static SurfaceVolumeInfo? GetVolumeInfo(TinVolumeSurface volumeSurface, Polyline border)
        {
            if (!border.Closed)
            {
                Tools.GetAcadEditor().WriteMessage("\nPolyline not closed");
                return null;
            }

            Point3dCollection points = border.GetPoints();
            try
            {
                return volumeSurface.GetBoundedVolumes(points);
            }
            catch (System.Exception ex)
            {
                Tools.GetAcadEditor().WriteMessage("\n" + ex.Message);
                return null;
            }
        }

        public static void ExtractBorder(/*CivilSurface*/ ITerrainSurface surface)
        {
            using (Transaction trans = Tools.StartTransaction())
            {
                var ids = surface.GetBounds();
                foreach (ObjectId id in ids)
                {
                    AcadEntity ent = id.GetObject(OpenMode.ForRead) as AcadEntity;
                    ent = ent.Clone() as AcadEntity;
                    Tools.AppendEntityEx(trans, ent);
                }
            }
        }


        public static ObjectId PromptForSurface(string msg = "\nВыберите поверхность: ")
        {
            PromptEntityOptions options = new PromptEntityOptions(
              msg);
            options.SetRejectMessage(
              "\nВыбранный объект не поверхность.");
            options.AddAllowedClass(typeof(CivilSurface), false);

            PromptEntityResult result = Tools.GetAcadEditor().GetEntity(options);
            if (result.Status == PromptStatus.OK)
            {
                // Everything is cool; we return the selected
                // surface ObjectId.
                return result.ObjectId;
            }
            return ObjectId.Null;   // Indicating error.
        }

        public static ObjectId PromptForTinSurface(string msg = "\nВыберите TIN поверхность: ")
        {
            PromptEntityOptions options = new PromptEntityOptions(
              msg);
            options.SetRejectMessage(
              "\nВыбранный объект не TIN поверхность.");
            options.AddAllowedClass(typeof(TinSurface), false);

            PromptEntityResult result = Tools.GetAcadEditor().GetEntity(options);
            if (result.Status == PromptStatus.OK)
            {
                // Everything is cool; we return the selected
                // surface ObjectId.
                return result.ObjectId;
            }
            return ObjectId.Null;   // Indicating error.
        }

        public static List<string> GetAllSurfaceNames()
        {
            List<string> result = new List<string>();
            using (Transaction trans = Tools.StartTransaction())
            {
                var civilDoc = Tools.GetActiveCivilDocument();
                ObjectIdCollection ids = civilDoc.GetSurfaceIds();
                foreach (ObjectId id in ids)
                {
                    if (id != ObjectId.Null)
                    {
                        var surface = trans.GetObject(id, OpenMode.ForRead) as Autodesk.Civil.DatabaseServices.Surface;
                        if (surface != null)
                        {
                            result.Add(surface.Name);
                        }
                    }
                }
            }
            return result;
        }

        public static Dictionary<string, ObjectId> GetAllVolumeSurfaces()
        {
            Dictionary<string, ObjectId> result = new Dictionary<string, ObjectId>();
            using (Transaction trans = Tools.StartTransaction())
            {
                var civilDoc = Tools.GetActiveCivilDocument();
                ObjectIdCollection ids = civilDoc.GetSurfaceIds();
                foreach (ObjectId id in ids)
                {
                    if (id != ObjectId.Null)
                    {
                        var surface = trans.GetObject(id, OpenMode.ForRead) as Autodesk.Civil.DatabaseServices.Surface;
                        if (surface != null && surface is TinVolumeSurface)
                        {
                            result.Add(surface.Name, surface.Id);
                        }
                    }
                }
            }
            return result;
        }

        /*public void ExportToXml()
        {
            TinSurface surface;
            IntPtr ptr = surface.UnmanagedObject;
            Autodesk.Civil.DatabaseServices.DBObject.Create()
        }*/
    }
}
