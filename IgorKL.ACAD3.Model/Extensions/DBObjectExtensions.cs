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
    public static class DBObjectExtensions
    {
        public static void SafeEdit<T>(this T obj, Action action)
            where T:DBObject
        {
            if (!obj.Id.IsNull)
            {
                obj = obj.Id.GetObjectForRead<T>(false);
                obj.UpgradeOpen();
            }
            
            action();

            if (!obj.Id.IsNull)
                obj.DowngradeOpen();
        }
    }
}
