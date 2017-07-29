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

namespace IgorKL.ACAD3.Model.AcadPoints
{
    public class PointFactory
    {
        public static ObjectId CreateFromTxet(string text, string separator)
        {
            string[] items = text.Split(new[] {separator}, StringSplitOptions.None);
            try
            {
                double north = double.Parse(items[0], System.Globalization.NumberStyles.Number, Tools.Culture);
                double east = double.Parse(items[1], System.Globalization.NumberStyles.Number, Tools.Culture);
                double elevation = double.Parse(items[2], System.Globalization.NumberStyles.Number, Tools.Culture);

                DBPoint point = new DBPoint(new Point3d(east, north, elevation));
                using (Transaction trans = Tools.StartOpenCloseTransaction())
                {
                    Tools.AppendEntityEx(trans, new[] { point });
                    return point.Id;
                }
            }
            catch (System.Exception)
            {
                return ObjectId.Null;
            }
            
        }
    }
}
