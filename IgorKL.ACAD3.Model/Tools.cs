using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

using IgorKL.ACAD3.Model.Extensions;

namespace IgorKL.ACAD3.Model
{
    public delegate ObjectId TransactionProcess<T>(T obj, Transaction trans, bool commit);
    public delegate void TransactionOpenCloseProcess<T>(T obj, Transaction trans, bool commit);

    public static class Tools
    {
        public static System.Globalization.CultureInfo Culture { get { return System.Globalization.CultureInfo.GetCultureInfo("en-US"); } }

        public static Editor GetAcadEditor()
        {
            return Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
        }

        public static Document GetActiveAcadDocument()
        {
            return Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
        }

        public static Autodesk.Civil.ApplicationServices.CivilDocument GetActiveCivilDocument()
        {
            return Autodesk.Civil.ApplicationServices.CivilApplication.ActiveDocument;
        }

        public static Transaction StartTransaction()
        {
            var db = HostApplicationServices.WorkingDatabase;
            return StartTransaction(db);
        }

        public static Transaction StartOpenCloseTransaction()
        {
            var db = HostApplicationServices.WorkingDatabase;
            return StartOpenCloseTransaction(db);
        }

        public static BlockTableRecord GetAcadBlockTableRecordCurrentSpace(OpenMode mode = OpenMode.ForWrite)
        {
            return Tools.GetAcadDatabase().CurrentSpaceId.GetObject<BlockTableRecord>(mode);
        }

        public static BlockTableRecord GetAcadBlockTableRecordCurrentSpace(Transaction trans, OpenMode mode = OpenMode.ForWrite)
        {
            return GetAcadBlockTableRecordCurrentSpace(trans, GetActiveAcadDocument().Database, mode);
        }

        public static BlockTableRecord GetAcadBlockTableRecordCurrentSpace(Transaction trans, Database db, OpenMode mode = OpenMode.ForWrite)
        {
            return (BlockTableRecord)trans.GetObject(db.CurrentSpaceId, mode);
        }

        public static BlockTableRecord GetAcadBlockTableRecordModelSpace(Transaction trans, Database db, OpenMode mode = OpenMode.ForWrite)
        {
            BlockTable acBlkTbl;
            acBlkTbl = trans.GetObject(db.BlockTableId,
                                            OpenMode.ForRead) as BlockTable;
            BlockTableRecord acBlkTblRec;
            acBlkTblRec = trans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                            mode) as BlockTableRecord;

            return acBlkTblRec;
        }

        public static BlockTableRecord GetAcadBlockTableRecordModelSpace(OpenMode mode = OpenMode.ForWrite)
        {
            Database db = Tools.GetAcadDatabase();

            BlockTable acBlkTbl;
            acBlkTbl = db.BlockTableId.GetObjectForRead<BlockTable>();

            BlockTableRecord acBlkTblRec;
            acBlkTblRec = acBlkTbl[BlockTableRecord.ModelSpace].GetObject<BlockTableRecord>(mode);

            return acBlkTblRec;
        }

        public static BlockTableRecord GetAcadBlockTableRecordModelSpace(Transaction trans, OpenMode mode = OpenMode.ForWrite)
        {
            return GetAcadBlockTableRecordModelSpace(trans, GetActiveAcadDocument().Database, mode);
        }

        public static void AppendEntityEx(Transaction trans, IEnumerable<Entity> entities)
        {
            BlockTableRecord btr = GetAcadBlockTableRecordCurrentSpace(trans, OpenMode.ForWrite);
            foreach (var e in entities)
            {
                btr.AppendEntity(e);
                trans.AddNewlyCreatedDBObject(e, true);
            }
            trans.Commit();
        }

        public static ObjectId AppendEntityEx(Transaction trans, Entity entity, bool comit = true)
        {
            Database db = GetActiveAcadDocument().Database;
            return AppendEntityEx(trans, db, entity, comit);
        }

        public static ObjectId AppendEntityEx(Transaction trans, Database db, Entity entity, bool comit = true)
        {
            BlockTableRecord btr = GetAcadBlockTableRecordCurrentSpace(trans, db ,OpenMode.ForWrite);
            ObjectId id = btr.AppendEntity(entity);
            trans.AddNewlyCreatedDBObject(entity, true);
            if (comit)
                trans.Commit();
            return id;
        }

        public static List<ObjectId> AppendEntity<T>(IEnumerable<T> entities)
            where T:Entity
        {
            List<ObjectId> res = new List<ObjectId>();
            Tools.StartTransaction(() =>
                {


                    Transaction trans = HostApplicationServices.WorkingDatabase.TransactionManager.TopTransaction;
                    BlockTableRecord btr = GetAcadBlockTableRecordCurrentSpace(trans, HostApplicationServices.WorkingDatabase, OpenMode.ForWrite);

                    foreach (var ent in entities)
                    {
                        res.Add(btr.AppendEntity(ent));
                        trans.AddNewlyCreatedDBObject(ent, true);
                    }

                });
            return res;
        }

        /*public static ObjectId AppendEntity(Database db, Entity entity)
        {
            using (Transaction trans = StartTransaction(db))
            {
                return AppendEntity(trans, entity, true);
            }
        }*/

        public static ObjectId AppendEntityEx(Document doc, Entity entity)
        {
            Database db = doc.Database;
            return AppendEntityEx(db, entity);
        }

        public static ObjectId AppendEntityEx(Database db, Entity entity)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTbl;
                acBlkTbl = trans.GetObject(db.BlockTableId,
                                                OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = trans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                OpenMode.ForWrite) as BlockTableRecord;

                ObjectId id = acBlkTblRec.AppendEntity(entity);
                trans.AddNewlyCreatedDBObject(entity, true);

                trans.Commit();

                return id;
            }
        }

        public static ObjectId AppendEntityEx(Entity entity)
        {
            var db = Tools.GetAcadDatabase();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTbl;
                acBlkTbl = trans.GetObject(db.BlockTableId,
                                                OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = trans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                OpenMode.ForWrite) as BlockTableRecord;

                ObjectId id = acBlkTblRec.AppendEntity(entity);
                trans.AddNewlyCreatedDBObject(entity, true);

                trans.Commit();

                return id;
            }
        }


        public static Database GetAcadWorkingDatabase()
        {
            return HostApplicationServices.WorkingDatabase;
        }


        public static Database GetAcadDatabase()
        {
            //return Application.DocumentManager.MdiActiveDocument.Database;
            return HostApplicationServices.WorkingDatabase;
        }

        public static Transaction StartTransaction(Database db)
        {
            return db.TransactionManager.StartTransaction();
        }

        public static Transaction StartOpenCloseTransaction(Database db)
        {
            return db.TransactionManager.StartOpenCloseTransaction();
        }

        public static void Write(string msg)
        {
            var _editor = GetAcadEditor();
            _editor.WriteMessage(msg);
        }

        public static void CloneObjects(ObjectIdCollection sourceIds, Database sourceDb, Database destDb)
        {
            ObjectId sourceMsId = SymbolUtilityServices.GetBlockModelSpaceId(sourceDb);
            ObjectId destDbMsId = SymbolUtilityServices.GetBlockModelSpaceId(destDb);

            IdMapping mapping = new IdMapping();

            sourceDb.WblockCloneObjects(sourceIds, destDbMsId, mapping, DuplicateRecordCloning.Ignore, false);
        }

        public static string GetLocalRootPath()
        {
            return Application.GetSystemVariable("LOCALROOTPREFIX") as string;
        }

        public static Document CreateNewDocument()
        {
            // Change the file and path to match a drawing template on your workstation
            string localRoot = Application.GetSystemVariable("LOCALROOTPREFIX") as string;
            string templatePath = localRoot + "Template\\acad.dwt";

            DocumentCollection acDocMgr = Application.DocumentManager;
            Document acNewDoc = acDocMgr.Add(templatePath);
            return acNewDoc;
        }

        public static ObjectId StartTransaction<T>(T state, TransactionProcess<T> transProcess)
        {
            using (Transaction trans = StartTransaction())
            {
                return transProcess(state, trans, true);
            }
        }

        public static void StartOpenCloseTransaction<T>(T state, TransactionOpenCloseProcess<T> transProcess)
        {
            using (Transaction trans = StartOpenCloseTransaction())
            {
                transProcess(state, trans, true);
            }
        }

        public static void StartTransaction(Action process)
        {
            using (Transaction trans = StartTransaction())
            {
                process();
                trans.Commit();
            }
        }
        public static void StartOpenCloseTransaction(Action process)
        {
            using (Transaction trans = StartOpenCloseTransaction())
            {
                process();
            }
        }

        public static Transaction GetTopTransaction()
        {
            return Tools.GetAcadDatabase().TransactionManager.TopTransaction;
        }


        /*public void ToggleHWAcceleration()
        {
            using (Autodesk.AutoCAD.GraphicsSystem.Configuration config =
              new Autodesk.AutoCAD.GraphicsSystem.Configuration())
            {
                bool b = config.IsFeatureEnabled(
                  Autodesk.AutoCAD.GraphicsSystem.HardwareFeature.HardwareAcceleration);
                config.SetFeatureEnabled(
                  Autodesk.AutoCAD.GraphicsSystem.
                  HardwareFeature.HardwareAcceleration, !b);
                config.SaveSettings();
            }
        }

        public void ToggleHWAcceleration()
        {
            using (Autodesk.AutoCAD.GraphicsSystem.Configuration config =
              new Autodesk.AutoCAD.GraphicsSystem.Configuration())
            {
                config.setHardwareAcceleration(true);
            }
        }*/
    }
}
