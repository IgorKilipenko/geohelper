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
    public static class ToolsExtensions
    {
        public static ObjectId SaveToDatebase<T>(this T obj, Transaction trans, bool comit = true)
            where T : Entity
        {
            var btr = Tools.GetAcadBlockTableRecordCurrentSpace(trans);
            var id = btr.AppendEntity(obj);
            trans.AddNewlyCreatedDBObject(obj, true);
            if (comit)
                trans.Commit();
            return id;
        }

        public static ObjectId SaveToDatebase<T>(this T obj)
            where T : Entity
        {
            using (Transaction trans = Tools.StartTransaction())
            {
                return obj.SaveToDatebase(trans, true);
            }
        }

        public static void SaveToDatebase<T>(this IEnumerable<T> collection, Transaction trans, bool comit = true)
            where T : Entity
        {
            foreach (var ent in collection)
            {
                ent.SaveToDatebase(trans, false);
            }
            if (comit)
                trans.Commit();
        }

        public static void SaveToDatebase<T>(this IEnumerable<T> collection)
            where T : Entity
        {
            using (Transaction trans = Tools.StartTransaction())
            {
                collection.SaveToDatebase(trans, true);
            }
        }

        public static T GetObject<T>(this ObjectId id, OpenMode mode)
            where T:DBObject
        {
            return (T)id.GetObject(mode);
        }

        public static T GetObjectForRead<T>(this ObjectId id, bool openErased = false)
            where T:DBObject
        {
            return (T)id.GetObject(OpenMode.ForRead, openErased, true);
        }

        public static T GetObjectForWrite<T>(this ObjectId id, bool openErased = false)
            where T : DBObject
        {
            return (T)id.GetObject(OpenMode.ForWrite, openErased, true);
        }
    }
}
