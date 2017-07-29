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

namespace IgorKL.ACAD3.Model.Extensions
{
    public static class AcadCollectionExtensions
    {
        /// <summary>
        /// Возвращает колекцию клонированных объектов RXObject.Clone()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entities"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetClonedEntities<T>(this IEnumerable<T> entities)
            where T:RXObject
        {
            return entities.Select(ent => (T)ent.Clone());
        }

        /// <summary>
        /// Возвращает колекцию трансформированных клонов объектов
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entities"></param>
        /// <param name="transform"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetClonedEntities<T>(this IEnumerable<T> entities, Matrix3d transform)
            where T : Entity
        {
            return entities.Select(ent => (T)ent.GetTransformedCopy(transform));
        }
        public static IEnumerable<ObjectId> ToEnumerable(this ObjectIdCollection collection)
        {
            foreach (ObjectId id in collection)
                yield return id;
        }
        public static IEnumerable<Point3d> ToEnumerable(this Point3dCollection points)
        {
            foreach (Point3d p in points)
                yield return p;
        }
        public static IEnumerable<T> GetSelectedItems<T> (this SelectionSet set)
            where T:DBObject
        {
            foreach (SelectedObject so in set)
            {
                T obj = so.ObjectId.GetObjectForRead<DBObject>(false) as T;
                if (obj != null)
                    yield return obj;
            }
        }
    }
}
