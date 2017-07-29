using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

using Autodesk.Civil.DatabaseServices;

using IgorKL.ACAD3.Model.Extensions;

namespace IgorKL.ACAD3.Model.Drawing
{
    public class CogoPointsRandomEditor
    {
        private static MainMenu.HostProvider _dataProvider = new MainMenu.HostProvider(new CogoPointsRandomEditor());

        [RibbonCommandButton("Точки COGO случайно", RibbonPanelCategories.Points_Coordinates)]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_EditCogoPointLocation", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void EditCogoPointLocation()
        {//SymbolUtilityServices.
            MethodOfRandomEdition method = MethodOfRandomEdition.ByCoordinate;
            List<CogoPoint> points;
            if (!ObjectCollector.TrySelectObjects(out points, "\nУкажите точки COGO для редактирования: "))
                return;
            Polyline pline = null;
            double tolerance = _dataProvider.Read("tolerance", 0.01);

            PromptDoubleOptions dopt = new PromptDoubleOptions("Укажите максимальное значение (абс.) смещение, м: ");
            dopt.AllowNone = false;
            dopt.AllowNegative = false;
            dopt.DefaultValue = tolerance;

            PromptDoubleResult dres = Tools.GetAcadEditor().GetDouble(dopt);
            if (dres.Status != PromptStatus.OK)
                return;
            _dataProvider.Write("tolerance", dres.Value);
            tolerance = dres.Value;

            PromptKeywordOptions pkwopt = new PromptKeywordOptions("\nУкажите метод расчета: ");
            pkwopt.Keywords.Add("RADius", "ПОРадиусу", "ПОРадиусу");
            pkwopt.Keywords.Add("SIMple", "Координаты", "Координаты");
            pkwopt.Keywords.Add("FROmBaseDirection", "ОТЛинии", "ОТЛинии");
            pkwopt.Keywords.Add("EXit", "ВЫХод", "ВЫХод");
            pkwopt.AllowNone = false;

            PromptResult kwres = Tools.GetAcadEditor().GetKeywords(pkwopt);
            if (kwres.Status !=  PromptStatus.OK)
                return;

            switch (kwres.StringResult)
            {
                case "EXit":
                    {
                        return;
                    }
                case "SIMple":
                    {
                        method = MethodOfRandomEdition.ByCoordinate;
                        break;
                    }
                case "FROmBaseDirection":
                    {
                        method = MethodOfRandomEdition.FromBaseDirection;
                        if (!ObjectCollector.TrySelectAllowedClassObject(out pline, "\nУкажите базовую линию: "))
                            return;
                        break;
                    }
                case "RADius":
                    {
                        method = MethodOfRandomEdition.ByVector;
                        break;
                    }
            }
            
            foreach (var p in points)
            {
                Point3d location = p.Location;
                var rndLoc = _editPointLocationRandomByVector(location, tolerance, method, pline);
                if (rndLoc == null || !rndLoc.HasValue)
                    continue;
                Tools.StartTransaction(() =>
                    {
                        var editedPoint = p.Id.GetObject<CogoPoint>(OpenMode.ForWrite);
                        editedPoint.TransformBy(Matrix3d.Displacement(rndLoc.Value - location));
                    });
            }
            Tools.GetAcadEditor().Regen();
        }

        private Point3d? _editPointLocationRandomByVector(Point3d point, double tolerance, MethodOfRandomEdition method ,Polyline baseDirection = null)
        {
            Random random = new Random(DateTime.Now.Millisecond);
            double originalPointElevation = point.Z;
            Vector3d vector = new Vector3d();
            if (method == MethodOfRandomEdition.ByVector || method == MethodOfRandomEdition.ByCoordinate)
            {
                double x = (random.NextDouble() - 0.5) * 2d;
                double y = (random.NextDouble() - 0.5) * 2d;

                if (method == MethodOfRandomEdition.ByVector)
                {
                    double yMax = Math.Sqrt(Math.Abs(1 - Math.Pow(x, 2)));
                    if (Math.Abs(y) > yMax)
                        y = yMax;
                }

                vector = new Vector3d(x, y, 0);
            }
            else if (method == MethodOfRandomEdition.FromBaseDirection && baseDirection != null && baseDirection.Length > 0)
            {
                point = new Point3d(point.X + Tolerance.Global.EqualPoint, point.Y, 0);
                var pointNormal = baseDirection.GetOrthoNormalPoint(point, new Plane(), true);
                if (pointNormal == null || !pointNormal.HasValue)
                    return null;
                vector = (point - pointNormal.Value);
                if (vector.Length == double.NaN || vector.Length == 0)
                {
                    try
                    {
                        vector = baseDirection.GetSecondDerivative(new Point3d(point.X, point.Y, baseDirection.Elevation));
                    }
                    catch
                    {
                        return null;
                    }
                }

                if (vector.Length > 0)
                {
                    vector = vector.DivideBy(vector.Length);
                    vector = vector.MultiplyBy((random.NextDouble() - 0.5) * 2);
                }
               
            }
            else
                return null;

            point = point.Add(vector.MultiplyBy(tolerance));
            point = new Point3d(point.X, point.Y, originalPointElevation);
            return point;
        }

        public enum MethodOfRandomEdition
        {
            ByCoordinate,
            ByVector, 
            FromBaseDirection
        }

        [Autodesk.AutoCAD.Runtime.CommandMethod("clpb", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public void CopyPoint()
        {
            //CogoPoint p = new CogoPoint();
            //p.LabelLocation = new Point3d();
            // p.GetRXClass();
            var cobj = System.Windows.Clipboard.GetDataObject();
            foreach (string s in cobj.GetFormats())
            {
                string f = s;
            }

            string[] formats = cobj.GetFormats();
            string format = IsAutocadClipbordData(formats);
            System.IO.MemoryStream ms = (System.IO.MemoryStream)cobj.GetData(format);
            //var type = obj.GetType();

            string path = string.Empty;
            using (System.IO.TextReader reader = new System.IO.StreamReader(ms, System.Text.Encoding.Unicode))
            {
                char[] text = new char[260];
                reader.Read(text, 0, 260);
                path = new string(text).TrimEnd('\0');
            }
            if (string.IsNullOrWhiteSpace(path))
                return;
            Database db = OpenDestDatabase(path);
            using (db)
            {
                List<CogoPoint> points = GetObjectsFromDatabase<CogoPoint>(db);
                
                //points[0].DeepClone()
            }
        }

        private Database OpenDestDatabase(string path)
        {
            Database destDb = new Database(false, true);
            try
            {
                destDb.ReadDwgFile(path, FileOpenMode.OpenForReadAndReadShare, false, "");
            }
            catch (Exception ex)
            {
                Tools.GetAcadEditor().WriteMessage("\nОшибка чтения файла {0}.\nСлужебное сообщение: {1}", path, ex.Message);
                destDb.Dispose();
                return null;
            }
            return destDb; 
        }

        private List<T> GetObjectsFromDatabase<T>(Database db)
            where T : Autodesk.AutoCAD.DatabaseServices.DBObject
        {
            List<T> result = new List<T>();
            Transaction trans = db.TransactionManager.StartTransaction();
            using (trans)
            {
                BlockTable bt =
                    (BlockTable)trans.GetObject(
                    db.BlockTableId,
                    OpenMode.ForRead
                    );

                BlockTableRecord btr =
                    (BlockTableRecord)trans.GetObject(
                    bt[BlockTableRecord.ModelSpace],
                    OpenMode.ForRead
                    );

                foreach (ObjectId objId in btr)
                {
                    Autodesk.AutoCAD.DatabaseServices.DBObject dbObj = trans.GetObject(objId, OpenMode.ForRead, false);
                    if (dbObj is T)
                        result.Add((T)dbObj);
                }

            }

            return result;
        }

        private bool IsAutocadClipbordData(string format)
        {
            if (format.StartsWith("AutoCAD." /*19*/))
                return true;
            else
                return false;
        }
        private string IsAutocadClipbordData(string[] formats)
        {
            string format = formats.First(s => IsAutocadClipbordData(s));
            return string.IsNullOrWhiteSpace(format) ? null : format;
        }
    }
}
