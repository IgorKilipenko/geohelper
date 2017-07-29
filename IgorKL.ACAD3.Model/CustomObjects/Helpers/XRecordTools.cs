using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.ApplicationServices;

namespace IgorKL.ACAD3.Model.CustomObjects.Helpers
{
    public static class XRecordTools
    {
        /// <summary>
        /// Retrieve or create an Entry with the given name into the Extension Dictionary of the passed-in object.
        /// </summary>
        /// <param name="id">The object hosting the Extension Dictionary</param>
        /// <param name="entryName">The name of the dictionary entry to get or set</param>
        /// <returns>The ObjectId of the diction entry, old or new</returns>
        public static ObjectId GetSetExtensionDictionaryEntry(ObjectId id, string entryName)
        {
            ObjectId ret = ObjectId.Null;

            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                DBObject obj = (DBObject)tr.GetObject(id, OpenMode.ForRead);
                if (obj.ExtensionDictionary == ObjectId.Null)
                {
                    obj.UpgradeOpen();
                    obj.CreateExtensionDictionary();
                    obj.DowngradeOpen();
                }

                DBDictionary dict = (DBDictionary)tr.GetObject(obj.ExtensionDictionary, OpenMode.ForRead);
                if (!dict.Contains(entryName))
                {
                    Xrecord xRecord = new Xrecord();
                    dict.UpgradeOpen();
                    dict.SetAt(entryName, xRecord);
                    tr.AddNewlyCreatedDBObject(xRecord, true);

                    ret = xRecord.ObjectId;
                }
                else
                    ret = dict.GetAt(entryName);

                tr.Commit();
            }

            return ret;
        }

        /// <summary>
        /// Remove the named Dictionary Entry from the passed-in object if applicable.
        /// </summary>
        /// <param name="id">The object hosting the Extension Dictionary</param>
        /// <param name="entryName">The name of the Dictionary Entry to remove</param>
        /// <returns>True if really removed, false if not there at all</returns>
        public static bool RemoveExtensionDictionaryEntry(ObjectId id, string entryName)
        {
            bool ret = false;

            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                DBObject obj = (DBObject)tr.GetObject(id, OpenMode.ForRead);
                if (obj.ExtensionDictionary != ObjectId.Null)
                {
                    DBDictionary dict = (DBDictionary)tr.GetObject(obj.ExtensionDictionary, OpenMode.ForRead);
                    if (dict.Contains(entryName))
                    {
                        dict.UpgradeOpen();
                        dict.Remove(entryName);
                        ret = true;
                    }
                }

                tr.Commit();
            }

            return ret;
        }

        public static void SetXrecord(ObjectId id, string key, ResultBuffer resbuf)
        {
            Database db = Tools.GetAcadDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (ent != null)
                {
                    ent.UpgradeOpen();
                    ent.CreateExtensionDictionary();
                    DBDictionary xDict = (DBDictionary)tr.GetObject(ent.ExtensionDictionary, OpenMode.ForWrite);
                    Xrecord xRec = new Xrecord();
                    xRec.Data = resbuf;
                    xDict.SetAt(key, xRec);
                    tr.AddNewlyCreatedDBObject(xRec, true);
                }
                tr.Commit();
            }
        }

        public static ResultBuffer GetXrecord(ObjectId id, string key)
        {
            Database db = Tools.GetAcadDatabase();
            ResultBuffer result = new ResultBuffer();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Xrecord xRec = new Xrecord();
                Entity ent = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
                if (ent != null)
                {
                    try
                    {
                        DBDictionary xDict = (DBDictionary)tr.GetObject(ent.ExtensionDictionary, OpenMode.ForRead, false);
                        xRec = (Xrecord)tr.GetObject(xDict.GetAt(key), OpenMode.ForRead, false);
                        return xRec.Data;
                    }
                    catch
                    {
                        return null;
                    }
                }
                else
                    return null;
            }

        }
    }
}
