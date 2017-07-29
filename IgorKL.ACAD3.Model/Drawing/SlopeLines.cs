using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Colors;

using IgorKL.ACAD3.Model.Extensions;

namespace IgorKL.ACAD3.Model.Drawing
{
    public class SlopeLinesGenerator:Drawing.PerpendicularVectorJigView
    {
        private double _step;
        private double _smallLineLength;

        private Curve _baseLine;
        private Curve _destinationLine;
        private Point3d _startPoint;
        private Point3d _endPoint;
        private Matrix3d _ucs;
        private SlopeModes _slopeMode;
        private bool _startPointComplete;
        private bool _endPointComplete;

        private Line _regressDestLine;
        private Line _regressBaseLine;

        private static MainMenu.HostProvider _dataHost = new MainMenu.HostProvider(new SlopeLinesGenerator());

        private List<Entity> _entitiesInMemory;

        public SlopeLinesGenerator()
            :this(Matrix3d.Identity)
        {

        }

        public SlopeLinesGenerator(Matrix3d ucs)
            :base(ucs)
        {
            if (_dataHost == null)
                _dataHost = new MainMenu.HostProvider(this);

            _ucs = Tools.GetAcadEditor().CurrentUserCoordinateSystem;

            _baseLine = null;
            _destinationLine = null;
            _regressBaseLine = null;
            _regressDestLine = null;
            _startPoint = Point3d.Origin;
            _endPoint = Point3d.Origin;
            _entitiesInMemory = new List<Entity>();
            //_slopeMode = SlopeModes.OwnPerpendicular;
            _startPointComplete = false;
            _endPointComplete = false;

            _step = _dataHost.Read("step", 2.5d);
            _smallLineLength = _dataHost.Read("smallLineLength", 0.5d);
            _slopeMode = _dataHost.Read("slopeMode", SlopeModes.OwnPerpendicular);
        }


        [RibbonCommandButton("Линии откоса", RibbonPanelCategories.Lines_Dimensions)]
        [Autodesk.AutoCAD.Runtime.CommandMethod("iCmd_DrawSlopeLines", Autodesk.AutoCAD.Runtime.CommandFlags.UsePickSet)]
        public static void DrawSlopeLinesCmd()
        {
            Matrix3d ucs = Tools.GetAcadEditor().CurrentUserCoordinateSystem;
            SlopeLinesGenerator mainBlock = new SlopeLinesGenerator(ucs);
            KeywordCollection keywords = new KeywordCollection();
            keywords.Add("SlopeMode", "МЕТод", "МЕТод построения", true, true);
            keywords.Add("Step", "ШАГ", "ШАГ линий", true, true);
            mainBlock.AddKeywords(keywords);
            mainBlock.PromptKeywordAction = (pres) =>
                {
                    switch (pres.StringResult)
                    {
                        case "SlopeMode":
                            {
                                var res = mainBlock.PromptSlopeLineMode();
                                return res;
                            }
                        case "Step":
                            {
                                var res = mainBlock.PromptStep();
                                return res;
                            }
                        case "AllLineLength":
                            {
                                mainBlock._startPoint = mainBlock.BaseCurve.StartPoint;
                                mainBlock._startPointComplete = true;
                                mainBlock._endPoint = mainBlock.BaseCurve.EndPoint;
                                mainBlock._endPointComplete = true;
                                Tools.StartTransaction(() =>
                                {
                                    mainBlock._entitiesInMemory.Clear();
                                    var slopeLines = mainBlock.Calculate(mainBlock._slopeMode);
                                    if (slopeLines != null && slopeLines.Count > 1)
                                    {
                                        mainBlock._entitiesInMemory.AddRange(slopeLines);
                                        mainBlock.SaveAtBlock();
                                    }
                                });
                                goto case "Exit";
                            }
                        case "Exit":
                            {
                                return PromptStatus.Cancel;
                            }
                    }
                    return PromptStatus.Cancel;
                };

            Tools.StartTransaction(() =>
            {
                if (mainBlock.PromptBaseLine() != PromptStatus.OK)
                    return;
                if (mainBlock.PromptDestinationLine() != PromptStatus.OK)
                    return;

                while (mainBlock._baseLine.Id == mainBlock._destinationLine.Id)
                {
                    Tools.GetAcadEditor().WriteMessage("\nЛинии должны отличаться, укажите другую линию");
                    if (mainBlock.PromptDestinationLine() != PromptStatus.OK)
                        return;
                }

                keywords.Add("AllLineLength", "ВсяЛиния", "ВсяЛиния", true, true);

                try
                {
                    if (!mainBlock.PromptStartPoint(keywords, mainBlock.PromptKeywordAction))
                        return;
                    if (mainBlock.PromptEndPoint())
                    {
                        mainBlock.SaveAtBlock();
                    }
                }
                catch (Exception ex)
                {
                    Application.ShowAlertDialog("\nОшибка. \n" + ex.Message);
                }
            });


        }


        private PromptEntityResult _promptLine(string msg = "\nУкажите линию: ", KeywordCollection keywords = null)
        {
            PromptEntityOptions peo = new PromptEntityOptions(msg);
            peo.SetRejectMessage("\nДолжна быть линия");
            peo.AddAllowedClass(typeof(Curve), false);
            peo.AllowNone = false;
            peo.AllowObjectOnLockedLayer = true;
            if (keywords != null)
                foreach (Keyword key in keywords)
                    peo.Keywords.Add(key.GlobalName, key.LocalName, key.DisplayName, key.Visible, key.Enabled);

            PromptEntityResult per = Tools.GetAcadEditor().GetEntity(peo);
            return per;
        }
        public PromptStatus PromptBaseLine()
        {
            KeywordCollection keywords = new KeywordCollection();
            keywords.Add("EditSlope", "РЕДактировать", "РЕДактировать", true, true);

            var per = _promptLine("\nУкажите верхнюю бровку: ", keywords);
            if (per.Status == PromptStatus.Keyword)
            {
                switch (per.StringResult)
                {
                    case "EditSlope":
                        {
                            while (EditCreatedSlopeLines() == PromptStatus.OK)
                            {
                                Tools.GetAcadDatabase().TransactionManager.QueueForGraphicsFlush();
                                //Tools.GetAcadDatabase().UpdateExt(true);
                            }
                            /*if (EditCreatedSlopeLines() != PromptStatus.OK)
                                return PromptStatus.Cancel;*/
                            break;
                        }

                }
            }
            if (per.Status != PromptStatus.OK)
                return per.Status;

            _baseLine = per.ObjectId.GetObjectForRead<Curve>();
            base.BaseCurve = _baseLine;
            _regressBaseLine = IgorKL.ACAD3.Model.Helpers.Math.Statistical.LinearRegression(_baseLine.ConvertToPolyline());
            return PromptStatus.OK;
        }
        public PromptStatus PromptDestinationLine()
        {
            var per = _promptLine("\nУкажите нижнюю бровку: ");
            if (per.Status != PromptStatus.OK)
                return per.Status;

            _destinationLine = per.ObjectId.GetObjectForRead<Curve>();
            _regressDestLine = IgorKL.ACAD3.Model.Helpers.Math.Statistical.LinearRegression(_destinationLine.ConvertToPolyline());
            return PromptStatus.OK;
        }
        public bool PromptStartPoint(KeywordCollection keywords = null, Func<PromptPointResult, PromptStatus> promptAction = null)
        {
            Drawing.PerpendicularVectorJigView vectorView = new PerpendicularVectorJigView(_baseLine, _ucs);
            if (keywords != null)
                vectorView.AddKeywords(keywords);
            if (promptAction != null)
                vectorView.PromptKeywordAction = promptAction;

            if (vectorView.StartJig() == PromptStatus.OK)
            {
                _startPoint = vectorView.NormalPoint;
                _startPointComplete = true;
                return true;
            }
            else
            {
                _startPoint = Point3d.Origin;
                _startPointComplete = false;
                return false;
            }
        }
        public bool PromptEndPoint()
        {
            //SlopeLinesGenerator vectorView = new SlopeLinesGenerator(_ucs);
            if (this.StartJig() == PromptStatus.OK)
            {
                _endPointComplete = true;
                return true;
            }
            _endPointComplete = false;
            return false;
        }

        public PromptStatus PromptSlopeInsideBlock(out PromptNestedEntityResult res, KeywordCollection keywoeds = null,
    Func<PromptNestedEntityResult, PromptStatus> promptAction = null, string msg = "\nВыберите линиюю")
        {
            res = null;

            PromptNestedEntityOptions pneo = new PromptNestedEntityOptions(msg);
            pneo.UseNonInteractivePickPoint = false;
            if (keywoeds != null)
                foreach (Keyword key in keywoeds)
                    pneo.Keywords.Add(key.GlobalName, key.LocalName, key.DisplayName, key.Visible, key.Enabled);

            PromptNestedEntityResult pner = Tools.GetAcadEditor().GetNestedEntity(pneo);
            if (pner.Status == PromptStatus.Keyword)
                if (promptAction != null)
                {
                    var kwRes = promptAction(pner);
                    if (kwRes != PromptStatus.OK)
                        return kwRes;
                }

            if (pner.Status != PromptStatus.OK)
                return pner.Status;

            if (!pner.ObjectId.ObjectClass.IsDerivedFrom(Autodesk.AutoCAD.Runtime.RXClass.GetClass(typeof(Line))))
                return PromptStatus.Error;

            res = pner;

            return PromptStatus.OK;
        }

        public List<Line> Calculate(SlopeModes slopeMode)
        {
            List<Line> slopeLines = new List<Line>();

            Polyline basePline = _baseLine.ConvertToPolyline();
            Polyline destPline = _destinationLine.ConvertToPolyline();
            if (!_startPointComplete)
                _startPoint = basePline.StartPoint;
            if (!_endPointComplete)
                _endPoint = _startPoint;

            double startDist = 0;
            startDist = basePline.GetDistAtPoint(_startPoint);
            double endDist = startDist;
            try
            {
                endDist = basePline.GetDistAtPoint(_endPoint);
            }
            catch { }

            Vector3d startVector = _startPoint - destPline.StartPoint;
            Vector3d endVector = _endPoint - destPline.EndPoint;
            Point3d centralPoint = _startPoint.Add((_endPoint - _startPoint).MultiplyBy(0.5d));

            if ((centralPoint - destPline.StartPoint).Length < startVector.Length &&
                (centralPoint - destPline.EndPoint).Length < endVector.Length)
            {
                Vector3d buffVector = startVector;
                startVector = endVector;
                endVector = startVector;
                destPline = ((Polyline)destPline.Clone());
                destPline.ReverseCurve();
            }



            int sign = endDist >= startDist ? 1 : -1;
            double buffLength = startVector.Length + endVector.Length + basePline.Length + destPline.Length;
            int slopeNumber = 0;
            //for (double dist = startDist; dist <= endDist && dist <= basePline.Length; dist += _step)
            for (double dist = startDist; sign > 0 ? dist <= endDist : dist >= endDist; dist += _step*sign)
            {
                slopeNumber++;
                Point3d point = basePline.GetPointAtDist(dist);

                switch (slopeMode)
                {
                    case SlopeModes.BothPerpendicular:
                        {
                            Point3d? destPoint = destPline.GetOrthoNormalPoint(point, null, true);
                            if (destPoint.HasValue)
                            {
                                Line slope = new Line(point, destPoint.Value);
                                //if ((dist-startDist +_step) % (_step*2d) < Tolerance.Global.EqualPoint)
                                if (slopeNumber % 2d == 0d)
                                    slope = new Line(slope.StartPoint, slope.StartPoint.Add((slope.EndPoint - slope.StartPoint).MultiplyBy(_smallLineLength)));
                                slopeLines.Add(slope);
                            }
                            break;
                        }
                    case SlopeModes.OwnPerpendicular:
                        {
                            Line line = new Line(point, point.Add(_baseLine.GetPerpendicularVector(point).MultiplyBy(buffLength)));
                            Point3dCollection intersects = new Point3dCollection();

                            Point3d? destPoint = _getIntersectPoint(line, destPline);
                            if (!destPoint.HasValue)
                                break;
                            Line slope = new Line(point, destPoint.Value);
                            if (slopeNumber % 2d == 0d)
                                slope = new Line(slope.StartPoint, slope.StartPoint.Add((slope.EndPoint - slope.StartPoint).MultiplyBy(_smallLineLength)));
                            slopeLines.Add(slope);

                            break;
                        }
                    case SlopeModes.EqualStep:
                        {
                            int count = Convert.ToInt32(Math.Floor(Math.Abs(endDist - startDist) / _step));
                            if (count == 0)
                                break;

                            Point3d? startDestPoint = destPline.GetOrthoNormalPoint(_startPoint, null, true);
                            Point3d? endDestPoint = destPline.GetOrthoNormalPoint(point, null, true);
                            if (!startDestPoint.HasValue)
                                startDestPoint = destPline.StartPoint;
                            if (!endDestPoint.HasValue)
                                endDestPoint = destPline.EndPoint;

                            double lengthFactor = destPline.Length / basePline.Length;
                            double destDist = dist * lengthFactor;
                            
                            if (Math.Abs(destDist) <= Tolerance.Global.EqualPoint)
                                destDist = 0d;
                            if (Math.Abs(destDist - destPline.Length) <= Tolerance.Global.EqualPoint)
                                destDist = destPline.Length;

                            Point3d destPoint = destPline.GetPointAtDist(destDist);

                            Line slope = new Line(point, destPoint);
                            if (slopeNumber % 2d == 0d)
                                slope = new Line(slope.StartPoint, slope.StartPoint.Add((slope.EndPoint - slope.StartPoint).MultiplyBy(_smallLineLength)));
                            slopeLines.Add(slope);

                            break;
                        }
                    case SlopeModes.RegressBoth:
                        {
                            Point3d? basePoint = _regressBaseLine.GetOrthoNormalPoint(point, null, true);
                            if (basePoint.HasValue)
                            {
                                Point3d? destPoint = _regressDestLine.GetOrthoNormalPoint(basePoint.Value, null, true);
                                if (destPoint.HasValue)
                                {
                                    Line slope = new Line(basePoint.Value, destPoint.Value);
                                    basePoint = _getIntersectPoint(slope, basePline);
                                    if (basePoint.HasValue)
                                    {
                                        destPoint = _getIntersectPoint(slope, destPline);
                                        if (destPoint.HasValue)
                                        {
                                            slope = new Line(basePoint.Value, destPoint.Value);
                                            if (slopeNumber % 2d == 0d)
                                                slope = new Line(slope.StartPoint, slope.StartPoint.Add((slope.EndPoint - slope.StartPoint).MultiplyBy(_smallLineLength)));
                                            slopeLines.Add(slope);
                                        }
                                    }
                                }
                            }
                            break;
                        }
                }
            }

            return slopeLines;
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var status = base.Sampler(prompts);


            if (status == SamplerStatus.OK)
            {

                _endPoint = base.NormalPoint;
                _endPointComplete = true;
                

                /*_endPoint = base.NormalPoint;
                var slopeLines = Calculate(_slopeMode);
                lock (_entitiesInMemory)
                {
                    if (slopeLines.Count > 0)
                    {
                        foreach (var ent in _entitiesInMemory)
                            if (!ent.IsDisposed)
                                ent.Dispose();
                        _entitiesInMemory.Clear();
                        _entitiesInMemory = new List<Entity>();
                        _entitiesInMemory.AddRange(slopeLines);
                    }
                }*/
            }
            return status;
        }

        public PromptStatus PromptSlopeLineMode(string msg = "\nУкавите метод построения линий откоса")
        {
            PromptKeywordOptions pko = new PromptKeywordOptions(msg);
            pko.Keywords.Add(SlopeModes.OwnPerpendicular.ToString(), "ПРостойПЕРпендикуляр", "ПРостойПЕРпендикуляр", true, true);
            pko.Keywords.Add(SlopeModes.EqualStep.ToString(), "ЕКВивалентныйШаг", "ЕКВивалентныйШаг", true, true);
            pko.Keywords.Add(SlopeModes.BothPerpendicular.ToString(), "ПЕРпендикулярОтНиза", "ПЕРпендикулярОтНиза", true, true);
            pko.Keywords.Add(SlopeModes.RegressBoth.ToString(), "РЕГрессия", "РЕГрессия", true, true);
            pko.Keywords.Add("Exit", "ВЫХод", "ВЫХод", true, true);

            PromptResult pr = Tools.GetAcadEditor().GetKeywords(pko);
            if (pr.Status != PromptStatus.OK)
                return pr.Status;

            if (pr.StringResult == "Exit")
            {
                return PromptStatus.Cancel;
            }
            else if (pr.StringResult == SlopeModes.OwnPerpendicular.ToString())
            {
                _slopeMode = SlopeModes.OwnPerpendicular;
                _dataHost.Write("slopeMode", _slopeMode);
                return PromptStatus.OK;
            }
            else if (pr.StringResult == SlopeModes.EqualStep.ToString())
            {
                _slopeMode = SlopeModes.EqualStep;
                _dataHost.Write("slopeMode", _slopeMode);
                return PromptStatus.OK;
            }
            else if (pr.StringResult == SlopeModes.BothPerpendicular.ToString())
            {
                _slopeMode = SlopeModes.BothPerpendicular;
                _dataHost.Write("slopeMode", _slopeMode);
                return PromptStatus.OK;
            }
            else if (pr.StringResult == SlopeModes.RegressBoth.ToString())
            {
                _slopeMode = SlopeModes.RegressBoth;
                _dataHost.Write("slopeMode", _slopeMode);
                return PromptStatus.OK;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public PromptStatus PromptStep(string msg = "\nУкажите шаг линий, м")
        {
            PromptDoubleOptions pdo = new PromptDoubleOptions(msg);
            pdo.AllowNegative = false;
            pdo.AllowNone = false;
            pdo.AllowZero = false;
            pdo.UseDefaultValue = true;
            pdo.DefaultValue = _step;

            PromptDoubleResult pdr = Tools.GetAcadEditor().GetDouble(pdo);
            if (pdr.Status != PromptStatus.OK)
                return pdr.Status;

            _step = pdr.Value;
            _dataHost.Write("step", _step);
            return PromptStatus.OK;
        }

        protected override bool WorldDraw(Autodesk.AutoCAD.GraphicsInterface.WorldDraw draw)
        {
            lock (_entitiesInMemory)
            {
                bool res = base.WorldDraw(draw);

                if (_endPointComplete)
                {
                    var slopeLines = Calculate(_slopeMode);

                    if (slopeLines.Count > 0)
                    {
                        foreach (var ent in _entitiesInMemory)
                            if (!ent.IsDisposed)
                                ent.Dispose();
                        _entitiesInMemory.Clear();
                        _entitiesInMemory.AddRange(slopeLines);
                    }

                    foreach (var ent in _entitiesInMemory)
                        draw.Geometry.Draw(ent);
                }
                return res;
            }

            
        }

        public ObjectId SaveAtBlock()
        {
            if (_entitiesInMemory != null && _entitiesInMemory.Count > 0)
            {
                _entitiesInMemory.ForEach(ent =>
                {
                    ent.Color = Color.FromColorIndex(ColorMethod.ByBlock, 0);
                    ent.Linetype = "ByBlock";
                    ent.LineWeight = LineWeight.ByBlock;
                });

                ObjectId btrId = AcadBlocks.BlockTools.CreateBlockTableRecord("*U", _startPoint, _entitiesInMemory.GetClonedEntities(), AnnotativeStates.NotApplicable);
                ObjectId brId = AcadBlocks.BlockTools.AppendBlockItem(_startPoint, btrId, null);
                return brId;
            }
            else
                return ObjectId.Null;
        }
        public ObjectId SaveAtBlock(IEnumerable<Line> slopeLines)
        {
            if (_entitiesInMemory == null)
                _entitiesInMemory = new List<Entity>(slopeLines.Count());
            _entitiesInMemory.AddRange(slopeLines);
            return SaveAtBlock();
        }


        public PromptStatus EditCreatedSlopeLines()
        {
            KeywordCollection keywords = new KeywordCollection();
            keywords.Add("Exit", "ВЫХод", "ВЫХод", true, true);
            Func<PromptNestedEntityResult, PromptStatus> promptActon = (pr) =>
                {
                    switch (pr.StringResult)
                    {
                        case "Exit":
                            {
                                return PromptStatus.Cancel;
                            }
                    }
                    return PromptStatus.Cancel;
                };

            PromptNestedEntityResult pner;
            if (PromptSlopeInsideBlock(out pner, keywords, promptActon) != PromptStatus.OK)
                return PromptStatus.Cancel;

            PromptResult res = null;

            Matrix3d brMat = Matrix3d.Identity;

            Tools.StartTransaction(() =>
            {
                var conts = pner.GetContainers();
                foreach (var brId in conts)
                {
                    var br = brId.GetObjectForRead<BlockReference>(false);
                    if (br != null)
                        brMat = brMat.PreMultiplyBy(br.BlockTransform);
                }

                Line slopeLine = pner.ObjectId.GetObjectForWrite<Line>(false);
                slopeLine.TransformBy(brMat);

                MoveSlopeLineJig moveJig = new MoveSlopeLineJig(slopeLine);
                res = Tools.GetAcadEditor().Drag(moveJig);
                if (res.Status == PromptStatus.OK)
                {
                    slopeLine.TransformBy(brMat.Inverse());

                    // Open each of the containers and set a property so that
                    // they each get regenerated
                    foreach (var id in conts)
                    {
                        var ent = id.GetObjectForWrite<Entity>();
                        if (ent != null)
                        {
                            // We might also have called this method:
                            // ent.RecordGraphicsModified(true);
                            // but setting a property works better with undo
                            ent.Visible = ent.Visible;
                        }
                    }
                }
            });
            return res.Status;
        }

        private Point3d? _getIntersectPoint(Line direction, Polyline destLine)
        {
            Point3d bpoint = direction.StartPoint;
            Point3dCollection intersects = new Point3dCollection();
            direction.IntersectWith(destLine, Intersect.ExtendBoth, direction.GetPlane(), intersects, IntPtr.Zero, IntPtr.Zero);

            if (intersects.Count > 0)
            {
                List<Vector3d> vs = new List<Vector3d>();
                var buffer = intersects.ToEnumerable().ToArray();
                List<Point3d> allowedPoints = new List<Point3d>();

                Action processor = () =>
                {
                    foreach (var p in buffer)
                    {
                        try
                        {
                            double testDest = destLine.GetDistAtPoint(p);
                            Point3d elevatedPoint = destLine.GetPointAtDist(testDest);
                            allowedPoints.Add(elevatedPoint);
                        }
                        catch (Autodesk.AutoCAD.Runtime.Exception)
                        { }
                    }
                    allowedPoints.Sort((x1, x2) => Comparer<double>.Default.Compare((x1 - bpoint).Length, (x2 - bpoint).Length));
                };

                processor();
                if (allowedPoints.Count() < 2)
                {
                    direction.ReverseCurve();
                    processor();
                }

                if (allowedPoints.Count() > 0)
                    return allowedPoints.First();

            }
            return null;
        }

        private IEnumerable<Point3d> _getPointsAtStep(Curve curve, Point3d startPoint, Point3d endPoint, int count)
        {
            List<Point3d> points = new List<Point3d>(count);
            if (!curve.IsPointOnCurveGCP(startPoint.OrthoProject(curve.GetPlane())))
                return points;

            if (!curve.IsPointOnCurveGCP(endPoint.OrthoProject(curve.GetPlane())))
                return points;

            double startDist = curve.GetDistAtPoint(startPoint);
            double endDist = curve.GetDistAtPoint(endPoint);

            double step = (endDist - startDist) / count;
            if (step <= Tolerance.Global.EqualPoint)
                return points;
            int sign = endDist >= startDist ? 1 : -1;
            for (double dist = startDist; sign > 0? dist <= endDist : dist >= endDist; dist += step*sign)
            {
                points.Add(curve.GetPointAtDist(dist));
            }

            return points;
        }

        public enum SlopeModes
        {
            BothPerpendicular,
            OwnPerpendicular,
            EqualStep,
            RegressBoth
        }

        private class MoveSlopeLineJig:EntityJig
        {
            private Point3d _pos;
            private Point3d _loc;

            public MoveSlopeLineJig(Line line)
                :base(line)
            {
                _pos = line.EndPoint;
                _loc = _pos;
            }

            protected override bool Update()
            {
                var disp = _pos - _loc;
                _loc = _pos;
                var mat = Matrix3d.Displacement(disp);
                ((Line)Entity).EndPoint = ((Line)Entity).EndPoint.TransformBy(mat);
                return true;
            }

            protected override SamplerStatus Sampler(JigPrompts prompts)
            {
                var opts = new JigPromptPointOptions("\nSelect displacement");
                opts.BasePoint = _pos;
                opts.UserInputControls =
                  UserInputControls.NoZeroResponseAccepted;

                var ppr = prompts.AcquirePoint(opts);
                if (_pos == ppr.Value)
                    return SamplerStatus.NoChange;

                _pos = ppr.Value;

                return SamplerStatus.OK;
            }
        }
    }
}
