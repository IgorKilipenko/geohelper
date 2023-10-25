//#define ACAD2014
#define ACAD2015

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

namespace IgorKL.ACAD3.Model.CogoPoints {
    public class CogoPointFactory {
        public static ObjectIdCollection CreateCogoPoints(Point3dCollection locations, string pointDescription = "_auto_created") {
            /*CogoPointCollection*/
            dynamic points = Autodesk.Civil.ApplicationServices.CivilApplication.ActiveDocument.CogoPoints;
#if ACAD2015
            if (Application.Version.Major > 19)
                return points.Add(locations, pointDescription, true);
            //#else
            else
                return points.Add(locations, pointDescription);
#endif
        }
        public static List<ObjectId> CreateCogoPoints(IEnumerable<Point3d> locations, string pointDescription = "_auto_created") {
            /*CogoPointCollection*/
            dynamic points = Autodesk.Civil.ApplicationServices.CivilApplication.ActiveDocument.CogoPoints;
            List<ObjectId> result = new List<ObjectId>(locations.Count());

            foreach (var p in locations) {
#if ACAD2015
                if (Application.Version.Major > 19)
                    result.Add(points.Add(p, pointDescription, true));
                //#elif ACAD2014
                else
                    result.Add(points.Add(p, pointDescription));
#endif
            }

            return result;
        }
        public static ObjectId CreateCogoPoints(Point3d location, string pointName, string pointDescription = "_auto_created") {
            dynamic points = Autodesk.Civil.ApplicationServices.CivilApplication.ActiveDocument.CogoPoints;
#if ACAD2015
            ObjectId id = ObjectId.Null;
            if (Application.Version.Major > 19)
                id = points.Add(location, pointDescription, true);
            else
                id = points.Add(location, pointDescription);
#endif

            using (Transaction trans = Tools.StartTransaction()) {
                var point = (CogoPoint)trans.GetObject(id, OpenMode.ForWrite);
                point.StyleId = ObjectId.Null;
                point.LabelStyleId = ObjectId.Null;
                if (!string.IsNullOrWhiteSpace(pointName))
                    point.PointName = pointName;
                trans.Commit();
            }

            return id;
        }

        public static ObjectId CreateCogoPoints(Point3d location, Transaction trans = null, string name = null, string description = null) {
            dynamic points = Autodesk.Civil.ApplicationServices.CivilApplication.ActiveDocument.CogoPoints;

            ObjectId id = ObjectId.Null;
            if (Application.Version.Major > 19) {
                id = !string.IsNullOrWhiteSpace(name) ? points.Add(location, description, true) : points.Add(location, true);
            } else {
                id = !string.IsNullOrWhiteSpace(name) ? points.Add(location, description) : points.Add(location);
            }

            var point = (CogoPoint)trans.GetObject(id, OpenMode.ForWrite);
            point.StyleId = ObjectId.Null;
            point.LabelStyleId = ObjectId.Null;
            if (!string.IsNullOrWhiteSpace(name)) {
                point.PointName = name;
            }

            return id;
        }
    }
}
