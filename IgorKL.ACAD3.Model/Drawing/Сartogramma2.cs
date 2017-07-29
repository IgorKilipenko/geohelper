using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

using CivilSurface = Autodesk.Civil.DatabaseServices.Surface;
using TinSurface = Autodesk.Civil.DatabaseServices.TinSurface;
using AcadEntity = Autodesk.AutoCAD.DatabaseServices.Entity;
using civil = Autodesk.Civil;
//using Autodesk.Civil.DatabaseServices;


using IgorKL.ACAD3.Model.Extensions;

namespace IgorKL.ACAD3.Model.Drawing
{
    public class Сartogramma2
    {
        private SimpleGride gride;
        private double _step;
        private static MainMenu.HostProvider _dataHost = new MainMenu.HostProvider(new Сartogramma2());

#if DEBUG
        [RibbonCommandButton("Картограмма2", RibbonPanelCategories.Coordinates_Scale)]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmdTest_DrawCartogramm2", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
#endif
        public void _drawGridCartogramma2()
        {
            _step = _dataHost.Read("step", 20d);
            KeywordCollection keywords = new KeywordCollection();
            keywords.Add("Step", "Шаг", "Шаг сетки", true, true);
            keywords.Add("View", "Список", "Список поверхностей", true, true);
            Func<PromptEntityResult, PromptStatus> promptStep = per =>
            {
                switch (per.StringResult)
                {
                    case "Step":
                        {
                            PromptDoubleOptions pdo = new PromptDoubleOptions("\nУкажите шаг сетки картограммы: ");
                            pdo.UseDefaultValue = true;
                            pdo.DefaultValue = _step;
                            pdo.AllowZero = false;
                            pdo.AllowNegative = false;
                            pdo.AllowNone = false;

                            var pdr = Tools.GetAcadEditor().GetDouble(pdo);
                            if (pdr.Status == PromptStatus.OK)
                            {
                                _step = pdr.Value;
                                _dataHost.Write("step", _step);
                            }
                            return pdr.Status;
                        }

                }

                return PromptStatus.Error;
            };

            Autodesk.Civil.DatabaseServices.TinVolumeSurface surface;
            if (!ObjectCollector.TrySelectAllowedClassObject(out surface, keywords, promptStep))
                return;


            Polyline polygon = null;
            Tools.StartTransaction(() =>
            {

                surface = surface.Id.GetObjectForRead<CivilSurface>() as Autodesk.Civil.DatabaseServices.TinVolumeSurface;
                if (surface == null)
                    return;

                polygon = surface.ExtractBorders().ConvertToPolyline();
            });

            Matrix3d ucs = Tools.GetAcadEditor().CurrentUserCoordinateSystem;
            polygon.TransformBy(ucs);
            Extents3d bounds = polygon.Bounds.Value;

            Vector3d hVector = Matrix3d.Identity.CoordinateSystem3d.Xaxis.MultiplyBy((bounds.MaxPoint - bounds.MinPoint).X);
            Vector3d vVector = Matrix3d.Identity.CoordinateSystem3d.Yaxis.MultiplyBy((bounds.MaxPoint - bounds.MinPoint).Y);

            ObjectId btrId = ObjectId.Null;
            ObjectId brId = ObjectId.Null;

            List<Entity> rectgs = null;
            CartogrammLabels labelsFactory = null;

            gride = new SimpleGride(bounds.MinPoint, vVector, hVector, _step, _step);
            labelsFactory = new CartogrammLabels(gride);

            int rowsCount = SimpleGride.CalculateCeilingCount(vVector, _step);
            int columnCount = SimpleGride.CalculateCeilingCount(hVector, _step);

            rectgs = new List<Entity>(rowsCount * columnCount);
            for (int r = 0; r < rowsCount; r++)
            {
                for (int c = 0; c < columnCount; c++)
                {
                    Polyline line = gride.CalculateRectagle(r, c, polygon, true);
                    if (line != null)
                    {
                        line.TransformBy(ucs.Inverse());
                        rectgs.Add(line);
                    }
                }

            }

            labelsFactory.CreateTeble(columnCount);

            Tools.StartTransaction(() =>
            {
                rectgs.AddRange(labelsFactory.CreateGridLabels(rectgs.Cast<Polyline>(), surface));
            });

            rectgs.AddRange(labelsFactory.Entities.Select(x => x.GetTransformedCopy(ucs.Inverse())));

            btrId = AcadBlocks.BlockTools.CreateBlockTableRecord("*U", bounds.MinPoint.TransformBy(ucs.Inverse()), rectgs.Cast<Entity>(), AnnotativeStates.NotApplicable, false);
            brId = AcadBlocks.BlockTools.AppendBlockItem(bounds.MinPoint.TransformBy(ucs.Inverse()), btrId, null);

        }

        private double? _getElevationAtPoint(Point3d point, CivilSurface surface, Polyline polygon)
        {
            if (!polygon.IsInsidePolygon(point))
                return null;
            double res = surface.FindElevationAtXY(point.X, point.Y);
            return res;
        }



    }

    public class CartogrammLabels2
    {
        double _tableTextHeight = 3.0;      
        double _grideTextHeight = 2.5;
        double _volumeTextHeight = 3.0;
        double _tableRowHeight = 8.0;
        double _firstColumnWidth = 20d;
        double _preOrPostColumnWidth = 5d;
        string _nullSymbol = "-"; /*'\u2010'.ToString();*/
        System.Globalization.CultureInfo _culture = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");

        private SimpleGride _gride;
        AnnotationScale _scale;
        Vector3d _horizontalVector;
        Vector3d _verticalVector;

        public List<TableField> TopRow { get; private set; }
        public List<TableField> BottomRow { get; private set; }
        public List<TableField> PreTopRow { get; private set; }
        public List<TableField> AmountTopRow { get; private set; }
        public List<TableField> PreBottomRow { get; private set; }
        public List<TableField> AmountBottomRow { get; private set; }
        public IEnumerable<Entity> Entities
        {
            get
            {
                foreach (var topRow in TopRow)
                {
                    yield return topRow.Bounds.ConvertToPolyline();
                    yield return topRow.Text;
                }
                foreach (var bottomRow in BottomRow)
                {
                    yield return bottomRow.Bounds.ConvertToPolyline();
                    yield return bottomRow.Text;
                }

                foreach (var preTopRow in PreTopRow)
                {
                    yield return preTopRow.Bounds.ConvertToPolyline();
                    yield return preTopRow.Text;
                }
                foreach (var postTopRow in AmountTopRow)
                {
                    yield return postTopRow.Bounds.ConvertToPolyline();
                    yield return postTopRow.Text;
                }

                foreach (var preBottomRow in PreBottomRow)
                {
                    yield return preBottomRow.Bounds.ConvertToPolyline();
                    yield return preBottomRow.Text;
                }
                foreach (var postBottomRow in AmountBottomRow)
                {
                    yield return postBottomRow.Bounds.ConvertToPolyline();
                    yield return postBottomRow.Text;
                }
            }
        }

        public CartogrammLabels2(SimpleGride gride)
        {
            _gride = gride;

            TopRow = new List<TableField>();
            BottomRow = new List<TableField>();

            PreTopRow = new List<TableField>();
            AmountTopRow = new List<TableField>();
            PreBottomRow = new List<TableField>();
            AmountBottomRow = new List<TableField>();

            ObjectContextManager ocm = HostApplicationServices.WorkingDatabase.ObjectContextManager;
            ObjectContextCollection occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
            _scale = (AnnotationScale)occ.CurrentContext;

            _tableRowHeight /= _scale.Scale;
            _firstColumnWidth /= _scale.Scale;
            _preOrPostColumnWidth /= _scale.Scale;

            _tableTextHeight /= _scale.Scale;
            _grideTextHeight /= _scale.Scale;
            _volumeTextHeight /= _scale.Scale;

            _horizontalVector = gride.HorizontalVector;
            _verticalVector = gride.VerticalVector;
        }

        public void CalculateSum()
        {
            double topRowSum = _calculateSum(TopRow);
            double bottomRowSum = _calculateSum(BottomRow);

            _setValueToAmount(topRowSum, this.AmountTopRow);
            _setValueToAmount(bottomRowSum, this.AmountBottomRow);
        }

        private double _calculateSum(List<TableField> row)
        {
            double sum = 0d;
            foreach (var field in row)
            {
                if (!string.IsNullOrWhiteSpace(field.Text.TextString)
                    && field.Text.TextString != _nullSymbol)
                {
                    sum += double.Parse(field.Text.TextString, System.Globalization.NumberStyles.Number | System.Globalization.NumberStyles.AllowLeadingSign
                        | System.Globalization.NumberStyles.AllowThousands, _culture);
                }
            }
            return sum;
        }

        private void _createTableColumn(int columnNimber)
        {
            Polyline row1 = _gride.CalculateRectagle(0, columnNimber);
            Vector3d vector = _gride.VerticalVector.Normalize().Negate();

            Matrix3d mat = Matrix3d.Displacement(vector.MultiplyBy(_gride.VerticalStep));
            row1.TransformBy(mat);
            
            mat = Matrix3d.Displacement(vector.MultiplyBy(_gride.VerticalStep*0.1));
            row1.TransformBy(mat);

            mat = Matrix3d.Displacement(vector.Negate().MultiplyBy(_gride.VerticalStep - _tableRowHeight));

            row1.ReplaceVertexAt(0, row1.GetPoint3dAt(0).TransformBy(mat));
            row1.ReplaceVertexAt(3, row1.GetPoint3dAt(3).TransformBy(mat));
            row1.ReplaceVertexAt(4, row1.GetPoint3dAt(4).TransformBy(mat));

            mat = Matrix3d.Displacement(vector.MultiplyBy(_tableRowHeight));

            Polyline row2 = (Polyline)row1.GetTransformedCopy(mat);

            TopRow.Add(new TableField(row1.ConvertToRectangle().Value, "", _tableTextHeight));
            BottomRow.Add(new TableField(row2.ConvertToRectangle().Value, "", _tableTextHeight));

        }

        public void CreateTeble(int columnCount)
        {

            for (int i = 0; i < columnCount; i++)
            {
                _createTableColumn(i);
            }

            Polyline firstTopColumn = TopRow[0].Bounds.ConvertToPolyline();
            Matrix3d mat = Matrix3d.Displacement(TopRow[0].Bounds.GetLowertHorizontalVector().Negate());
            firstTopColumn.TransformBy(mat);
            //TopRow.Insert(0, new TableField(firstTopColumn.ConvertToRectangle().Value, "(+) Насыпь"));
            
            Matrix3d disp = Matrix3d.Displacement(TopRow[0].Bounds.GetLowertHorizontalVector().Negate().Normalize().MultiplyBy(_firstColumnWidth - TopRow[0].Bounds.GetLowertHorizontalVector().Length));
            firstTopColumn.ReplaceVertexAt(0, firstTopColumn.GetPoint3dAt(0).TransformBy(disp));
            firstTopColumn.ReplaceVertexAt(1, firstTopColumn.GetPoint3dAt(1).TransformBy(disp));
            firstTopColumn.ReplaceVertexAt(4, firstTopColumn.GetPoint3dAt(4).TransformBy(disp));

            PreTopRow.Add(new TableField(firstTopColumn.ConvertToRectangle().Value, "Насыпь(+)", _tableTextHeight));

            Polyline firstBottomColumn = BottomRow[0].Bounds.ConvertToPolyline();
            firstBottomColumn.TransformBy(mat);
            firstBottomColumn.ReplaceVertexAt(0, firstBottomColumn.GetPoint3dAt(0).TransformBy(disp));
            firstBottomColumn.ReplaceVertexAt(1, firstBottomColumn.GetPoint3dAt(1).TransformBy(disp));
            firstBottomColumn.ReplaceVertexAt(4, firstBottomColumn.GetPoint3dAt(4).TransformBy(disp));
            
            //BottomRow.Insert(0, new TableField(firsrBottomColumn.ConvertToRectangle().Value, "(-) Выемка"));
            PreBottomRow.Add(new TableField(firstBottomColumn.ConvertToRectangle().Value, "Выемка(-)", _tableTextHeight));


            disp = Matrix3d.Displacement((firstTopColumn.GetPoint3dAt(0) - firstTopColumn.GetPoint3dAt(3)).Normalize().MultiplyBy(_preOrPostColumnWidth));
            Point3d lowerRight = firstBottomColumn.GetPoint3dAt(0);
            Point3d upperRight = firstTopColumn.GetPoint3dAt(1);
            Point3d lowerLeft = lowerRight.TransformBy(disp);
            Point3d upperLeft = upperRight.TransformBy(disp);
            Rectangle3d firstColumn = new Rectangle3d(upperLeft, upperRight, lowerLeft, lowerRight);

            PreTopRow.Add(new TableField(firstColumn, "Итого, м3", _tableTextHeight*0.8, Math.PI/2d));


            mat = Matrix3d.Displacement(TopRow.Last().Bounds.GetLowertHorizontalVector());
            mat = mat.PreMultiplyBy(Matrix3d.Displacement(TopRow.Last().Bounds.GetLowertHorizontalVector().Normalize().MultiplyBy(_preOrPostColumnWidth)));
            Polyline ammountTopColumn = (Polyline)TopRow.Last().Bounds.ConvertToPolyline().GetTransformedCopy(mat);

            disp = Matrix3d.Displacement(TopRow.Last().Bounds.GetLowertHorizontalVector().Normalize().MultiplyBy(_firstColumnWidth - TopRow.Last().Bounds.GetLowertHorizontalVector().Length));
            ammountTopColumn.ReplaceVertexAt(2, ammountTopColumn.GetPoint3dAt(2).TransformBy(disp));
            ammountTopColumn.ReplaceVertexAt(3, ammountTopColumn.GetPoint3dAt(3).TransformBy(disp));

            Polyline ammountBottomColumn = (Polyline)BottomRow.Last().Bounds.ConvertToPolyline().GetTransformedCopy(mat);
            ammountBottomColumn.ReplaceVertexAt(2, ammountBottomColumn.GetPoint3dAt(2).TransformBy(disp));
            ammountBottomColumn.ReplaceVertexAt(3, ammountBottomColumn.GetPoint3dAt(3).TransformBy(disp));

            AmountTopRow.Add(new TableField(ammountTopColumn.ConvertToRectangle().Value, "", _tableTextHeight));
            AmountBottomRow.Add(new TableField(ammountBottomColumn.ConvertToRectangle().Value, "", _tableTextHeight));

            mat = Matrix3d.Displacement(TopRow.Last().Bounds.GetLowertHorizontalVector().Negate().Normalize().MultiplyBy(_preOrPostColumnWidth));
            lowerRight = ammountBottomColumn.GetPoint3dAt(0);
            upperRight = ammountTopColumn.GetPoint3dAt(1);
            lowerLeft = lowerRight.TransformBy(mat);
            upperLeft = upperRight.TransformBy(mat);
            Rectangle3d ammountColumn = new Rectangle3d(upperLeft, upperRight, lowerLeft, lowerRight);

            AmountTopRow.Add(new TableField(ammountColumn, "Всего, м3", _tableTextHeight*0.8 ,Math.PI/2));
        }

        public void SetValueToField(int columnNumber, double value, List<TableField> row)
        {
            string text = "";
            value = Math.Round(value, 1);
            
            if (value == 0d)
            {
                text = _nullSymbol;
                row[columnNumber].Text.TextString = text;
            }
            else if (value > 0)
            {
                text = '+' + value.ToString("#,0.0", _culture);
                row[columnNumber].Text.TextString = text;
            }
            else
            {
                text = value.ToString("#,0.0", _culture);
                row[columnNumber].Text.TextString = text;
            }
        }


        private void _setValueToAmount(double value, List<TableField> row)
        {

            int columnNumber = 0;
            string text = "";
            value = Math.Round(value, 1);

            if (value == 0d)
            {
                text = _nullSymbol;
                row[columnNumber].Text.TextString = text;
            }
            else if (value > 0)
            {
                text = '+' + value.ToString("#,0.0", _culture);
                row[columnNumber].Text.TextString = text;
            }
            else
            {
                text = value.ToString("#,0.0", _culture);
                row[columnNumber].Text.TextString = text;
            }
        }


        public IEnumerable<DBText> CreateElevationLabels(Point3d position, double baseElevation, double comparisonElevation)
        {
            /*var volProps = surface.GetVolumeProperties();
            ObjectId baseSurfaceId = volProps.BaseSurface;
            ObjectId comparisonSurfaceId = volProps.ComparisonSurface;*/

            Vector3d yaxis = (this.AmountTopRow[0].Bounds.UpperLeft - this.AmountTopRow[0].Bounds.LowerLeft).Normalize();
            
            
            DBText topRightText = new DBText();
            topRightText.SetDatabaseDefaults();
            topRightText.Height = _grideTextHeight;
            topRightText.Rotation = 0d;
            topRightText.Position = Point3d.Origin;
            topRightText.HorizontalMode = TextHorizontalMode.TextLeft;
            topRightText.VerticalMode = TextVerticalMode.TextBottom;
            topRightText.Annotative = AnnotativeStates.False;
            //topRightText.AddContext(_scale);
            topRightText.AlignmentPoint = position;
            topRightText.AdjustAlignment(HostApplicationServices.WorkingDatabase);

            topRightText.TextString = comparisonElevation.ToString("#0.00", _culture);


            DBText bottomRightText = new DBText();
            bottomRightText.SetDatabaseDefaults();
            bottomRightText.Height = _grideTextHeight;
            bottomRightText.Rotation = 0d;
            bottomRightText.Position = Point3d.Origin;
            bottomRightText.HorizontalMode = TextHorizontalMode.TextLeft;
            bottomRightText.VerticalMode = TextVerticalMode.TextTop;
            bottomRightText.Annotative = AnnotativeStates.False;
            //bottomRightText.AddContext(_scale);
            bottomRightText.AlignmentPoint = position;
            bottomRightText.AdjustAlignment(HostApplicationServices.WorkingDatabase);

            bottomRightText.TextString = baseElevation.ToString("#0.00", _culture);


            DBText topLeftText = new DBText();
            topLeftText.SetDatabaseDefaults();
            topLeftText.Height = _grideTextHeight;
            topLeftText.Rotation = 0d;
            topLeftText.Position = Point3d.Origin;
            topLeftText.HorizontalMode = TextHorizontalMode.TextRight;
            topLeftText.VerticalMode = TextVerticalMode.TextBottom;
            topLeftText.Annotative = AnnotativeStates.False;
            //topLeftText.AddContext(_scale);
            topLeftText.AlignmentPoint = position;
            topLeftText.AdjustAlignment(HostApplicationServices.WorkingDatabase);

            topLeftText.TextString = (comparisonElevation - baseElevation).ToString("#0.00", _culture);


            return new[] { topRightText, bottomRightText, topLeftText };
        }


        public IEnumerable<DBText> CreateVolumeLabels(Rectangle3d rectg, Autodesk.Civil.DatabaseServices.TinVolumeSurface surface)
        {
            List<DBText> res = new List<DBText>();
            Point3d topPosition = Point3d.Origin;
            Vector3d centralVector = rectg.UpperRight - rectg.LowerLeft;
            topPosition = rectg.LowerLeft.Add(centralVector.MultiplyBy(0.5d));
            Point3d bottomPosition = topPosition;

            Autodesk.Civil.DatabaseServices.SurfaceVolumeInfo volumeInfo = new civil.DatabaseServices.SurfaceVolumeInfo();
            try
            {
                volumeInfo = surface.GetBoundedVolumes(rectg.GetPoints(true));
            }
            catch (ArgumentException)
            {
                return null;
            }
            if (Math.Round(volumeInfo.Fill, 1) > 0d 
                && Math.Round(volumeInfo.Cut,1) > 0d )
            {
                Vector3d yaxis = rectg.GetLeftVerticalVector().Normalize();
                Matrix3d mat = Matrix3d.Displacement(yaxis.MultiplyBy(_volumeTextHeight * 0.7));
                topPosition = topPosition.TransformBy(mat);
                bottomPosition = bottomPosition.TransformBy(mat.Inverse());
            }

            if (Math.Round(volumeInfo.Fill, 1) > 0d)
            {
                DBText topText = new DBText();
                topText.SetDatabaseDefaults();
                topText.Height = _volumeTextHeight;
                topText.Rotation = 0d;
                topText.Position = Point3d.Origin;
                topText.HorizontalMode = TextHorizontalMode.TextCenter;
                topText.VerticalMode = TextVerticalMode.TextVerticalMid;
                topText.Annotative = AnnotativeStates.False;
                //topText.AddContext(_scale);
                topText.AlignmentPoint = topPosition;
                topText.AdjustAlignment(HostApplicationServices.WorkingDatabase);

                topText.TextString = '+' + Math.Round(volumeInfo.Fill).ToString("#0.0", _culture);
                res.Add(topText);
            }
            else
                res.Add(null);

            if (Math.Round(volumeInfo.Cut, 1) > 0d)
            {
                DBText bottomText = new DBText();
                bottomText.SetDatabaseDefaults();
                bottomText.Height = _volumeTextHeight;
                bottomText.Rotation = 0d;
                bottomText.Position = Point3d.Origin;
                bottomText.HorizontalMode = TextHorizontalMode.TextCenter;
                bottomText.VerticalMode = TextVerticalMode.TextVerticalMid;
                bottomText.Annotative = AnnotativeStates.False;
                //bottomText.AddContext(_scale);
                bottomText.AlignmentPoint = bottomPosition; ;
                bottomText.AdjustAlignment(HostApplicationServices.WorkingDatabase);

                bottomText.TextString = Math.Round(-volumeInfo.Cut,1).ToString("#0.0", _culture);
                res.Add(bottomText);
            }
            else
                res.Add(null);

            if (Math.Round(volumeInfo.Fill, 1) == 0d
                && Math.Round(volumeInfo.Cut, 1) == 0d)
            {
                DBText centralText = new DBText();
                centralText.SetDatabaseDefaults();
                centralText.Height = _volumeTextHeight;
                centralText.Rotation = 0d;
                centralText.Position = Point3d.Origin;
                centralText.HorizontalMode = TextHorizontalMode.TextCenter;
                centralText.VerticalMode = TextVerticalMode.TextVerticalMid;
                centralText.Annotative = AnnotativeStates.False;
                //topText.AddContext(_scale);
                centralText.AlignmentPoint = topPosition;
                centralText.AdjustAlignment(HostApplicationServices.WorkingDatabase);

                centralText.TextString = Math.Round(volumeInfo.Fill).ToString("#0.0", _culture);
                res.Add(centralText);
            }

            return res;
        }

        public IEnumerable<Point3d> _getGridElevationPoints(IEnumerable<Polyline> rectgs)
        {
            HashSet<Point3d> points = new HashSet<Point3d>();
            var rectg = rectgs.GetEnumerator();
            while (rectg.MoveNext())
            {
                var ps = rectg.Current.GetPoints();
                foreach (Point3d p in ps)
                    if (!points.Contains(p))
                        points.Add(p);
            }
            return points;
        }

        public IEnumerable<DBText> CreateGridLabels(IEnumerable<Polyline> rectgs, Autodesk.Civil.DatabaseServices.TinVolumeSurface surface)
        {
            List<DBText> res = new List<DBText>();
            surface = surface.Id.GetObjectForRead<Autodesk.Civil.DatabaseServices.TinVolumeSurface>();
            var polygon = surface.ExtractBorders().ConvertToPolyline();

            var points = _getGridElevationPoints(rectgs.Cast<Polyline>());
            foreach (var p in points)
            {
                if (!polygon.IsInsidePolygon(p))
                    continue;

                var baseSurface = surface.GetVolumeProperties().BaseSurface.GetObjectForRead<CivilSurface>();
                var comparisonSurface = surface.GetVolumeProperties().ComparisonSurface.GetObjectForRead<CivilSurface>();

                double baseElevation = baseSurface.FindElevationAtXY(p.X, p.Y);
                double comparisonElevation = comparisonSurface.FindElevationAtXY(p.X, p.Y);
                //double topLeftElevation = surface.FindElevationAtXY(p.X, p.Y);

                var gridLabels = CreateElevationLabels(p, baseElevation, comparisonElevation);
                res.AddRange(gridLabels);
            }

            Dictionary<int, List<double[]>> ammounts = new Dictionary<int,List<double[]>>();
            foreach (var rectg in rectgs)
            {
                var volumeLable = CreateVolumeLabels(rectg.ConvertToRectangle().Value, surface);
                if (volumeLable != null)
                {
                    res.AddRange(volumeLable.Where(x => x!= null));
                    int columnNumber = _gride.GetColumnNumber(rectg.ConvertToRectangle().Value.LowerLeft);
                    double[] buffer = new double[volumeLable.Count()];
                    DBText textBuffer = null;
                    if ((textBuffer = volumeLable.FirstOrDefault()) != null)
                        buffer[0] = double.Parse(textBuffer.TextString, System.Globalization.NumberStyles.Number | System.Globalization.NumberStyles.AllowLeadingSign, _culture);
                    if (volumeLable.Count() > 1)
                        if ((textBuffer = volumeLable.LastOrDefault()) != null)
                            buffer[1] = double.Parse(textBuffer.TextString, System.Globalization.NumberStyles.Number | System.Globalization.NumberStyles.AllowLeadingSign, _culture);
                    if (!ammounts.ContainsKey(columnNumber))
                        ammounts[columnNumber] = new List<double[]>();
                    ammounts[columnNumber].Add(buffer);
                }
            }

            foreach (var column in ammounts.Keys)
            {
                double fillSum = ammounts[column].Sum(x => x.Length > 0 ? x[0] : 0d);
                double cutSum = ammounts[column].Sum(x => x.Length > 1 ? x[1] : 0d);
                SetValueToField(column, fillSum, TopRow);
                SetValueToField(column, cutSum, BottomRow);
            }
            CalculateSum();

            return res;
        }

        [Obsolete]
        private double _getAngleAtXaxis(Vector3d vector)
        {
            return 0d;
        }
    }

    public class TableField2
    {
        double _tableTextHeight = 3.0;
        double _textRotation = 0d;

        public TableField2()
        {

        }
        public TableField2(Rectangle3d bounds, string text, double tableTextHeight, double textRotation = 0d)
            :this()
        {
            _textRotation = textRotation;
            _tableTextHeight = tableTextHeight;
            this.Bounds = bounds;
            Point3d alignment = _getAlignmentPoint(bounds);
            Text = _createDBText(alignment, _tableTextHeight);
            this.Text.TextString = text;
            
        }

        public Rectangle3d Bounds { get; set; }
        public DBText Text { get; set; }
        public IEnumerable<Entity> Entities { get { yield return Bounds.ConvertToPolyline(); yield return Text; } }

        private DBText _createDBText(Point3d alignmentPoint, double height)
        {
            ObjectContextManager ocm = HostApplicationServices.WorkingDatabase.ObjectContextManager;
            ObjectContextCollection occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
            AnnotationScale scale = (AnnotationScale)occ.CurrentContext;

            DBText text = new DBText();
            text.SetDatabaseDefaults();
            text.Height = height; /* * scale.DrawingUnits/ scale.PaperUnits;*/
            text.Annotative = AnnotativeStates.False;
            text.Rotation = _textRotation;
            text.Position = Point3d.Origin;
            text.HorizontalMode = TextHorizontalMode.TextMid;
            text.VerticalMode = TextVerticalMode.TextVerticalMid;
            text.AlignmentPoint = alignmentPoint;
            //text.AddContext(occ.CurrentContext);
            text.AdjustAlignment(HostApplicationServices.WorkingDatabase);

            return text;
        }

        private static Point3d _getAlignmentPoint(Rectangle3d rectg)
        {
            Vector3d vector = (rectg.UpperRight - rectg.LowerLeft);
            Point3d alignment = rectg.LowerLeft.Add(vector.MultiplyBy(0.5d));
            return alignment;
        }


    }

    public class CartogrammGride2:DrawJig
    {
        Point3d _position;
        Point3d _jigBasePoint;
        BlockReference _br;
        Point3d _jigPoint = new Point3d(0,0,0);
        //bool _isJigComplete;

        Action<Point3d> _transformProcessor;

        public CartogrammGride2(Point3d position, BlockReference br, Action<Point3d> transformProcessor)
        {
            _position = position;
            _jigBasePoint = position;
            _br = br;

            _transformProcessor = transformProcessor;
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            JigPromptPointOptions jppo = new JigPromptPointOptions("\nУкажите точку: ");
            jppo.UseBasePoint = true;
            jppo.BasePoint = _position;

            _jigPoint = prompts.AcquirePoint(jppo).Value;
            if (_jigPoint.IsEqualTo(_jigBasePoint))
                return SamplerStatus.NoChange;
            else
            {
                //Matrix3d mat = Matrix3d.Displacement(_jigBasePoint.GetVectorTo(_jigPoint));
                //Entity.TransformBy(mat);
                _transformProcessor(_jigPoint);

                _br.Position = _jigPoint;
                _br.RecordGraphicsModified(true);

                _jigBasePoint = _jigPoint;

                return SamplerStatus.OK;
            }
        }


        public PromptStatus StartJig()
        {
            PromptStatus res = PromptStatus.Other;
            while ((res = Tools.GetAcadEditor().Drag(this).Status) != PromptStatus.Cancel)
            {
                if (res == PromptStatus.OK)
                    return res;
            }
            return res;
        }

        protected override bool WorldDraw(Autodesk.AutoCAD.GraphicsInterface.WorldDraw draw)
        {
            //return draw.Geometry.Draw(_br);
            return true;
        }
    }

}
