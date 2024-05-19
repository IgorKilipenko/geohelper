using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;

namespace IgorKL.ACAD3.Model.AcadPoints {
    public class PointFactory {
        public static ObjectId CreateFromText(string text, string separator) {
            string[] items = text.Split(new[] { separator }, StringSplitOptions.None);
            try {
                double north = double.Parse(items[0], System.Globalization.NumberStyles.Number, Tools.Culture);
                double east = double.Parse(items[1], System.Globalization.NumberStyles.Number, Tools.Culture);
                double elevation = double.Parse(items[2], System.Globalization.NumberStyles.Number, Tools.Culture);

                DBPoint point = new DBPoint(new Point3d(east, north, elevation));
                using (Transaction trans = Tools.StartOpenCloseTransaction()) {
                    Tools.AppendEntityEx(trans, new[] { point });
                    return point.Id;
                }
            } catch (System.Exception) {
                return ObjectId.Null;
            }
        }
    }
}
